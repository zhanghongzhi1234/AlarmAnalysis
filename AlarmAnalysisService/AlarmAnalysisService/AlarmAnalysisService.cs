using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TemplateProject;

namespace AlarmAnalysisService
{
    public partial class AlarmAnalysisService : ServiceBase
    {
        Timer timerPoll;
        int pollInterval = CachedMap.Instance.pollInterval;
        string AlarmFilePath = CachedMap.Instance.AlarmFilePath;
        int archiveKeepDays = CachedMap.Instance.archiveKeepDays;
        int TableOptimizationInterval = CachedMap.Instance.TableOptimizationInterval;

        PumpProcessor pumpProcessor = new PumpProcessor();

        public AlarmAnalysisService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DebugUtil.Instance.LOG.Info("Alarm analysis service starting...");
            //System.Environment.CurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            //DebugUtil.Instance.LOG.Debug("CurrentDirectory = " + System.Environment.CurrentDirectory);
            //if (bFirstStart == true)
            {
                DebugUtil.Instance.LOG.Info("First time start, ClearAllAlarmFilesWhenStart=" + CachedMap.Instance.ClearAllAlarmFilesWhenStart.ToString());
                try
                {
                    if (CachedMap.Instance.ClearAllAlarmFilesWhenStart == true)
                    {
                        DebugUtil.Instance.LOG.Info("Deleting all files in Alarm folder...");
                        System.IO.DirectoryInfo di = new DirectoryInfo(AlarmFilePath);
                        foreach (FileInfo file in di.EnumerateFiles())      //If your directory may have many files, EnumerateFiles() is more efficient than GetFiles()
                        {
                            file.Delete();
                        }
                        foreach (DirectoryInfo dir in di.EnumerateDirectories())
                        {
                            dir.Delete(true);
                        }
                    }
                    else
                    {
                        string[] allFiles = GetAllAlarmFiles();
                        DebugUtil.Instance.LOG.Info("found " + allFiles.Count() + " alarm file not archived");
                        if (allFiles.Count() > 0)
                        {
                            DebugUtil.Instance.LOG.Info("archive all files to database..");
                            ArchiveFiles(allFiles);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugUtil.Instance.LOG.Error(ex.ToString());
                }
                //bFirstStart = false;
            }
            {
                CheckAndDeleteOutdatedRecord();     //when a new day start, delete alarms before archiveKeepDays
                DebugUtil.Instance.LOG.Debug("Service start, delete alarms before archiveKeepDays");
            }

            timerPoll = new Timer(timerPoll_Tick, null, 0, pollInterval);

            pumpProcessor.Start();
        }

        protected override void OnStop()
        {
            timerPoll.Dispose();
            DebugUtil.Instance.LOG.Info("Alarm Analysis Service stop manually.");
        }
        
        private string[] GetAllAlarmFiles()
        {
            string[] Files = Directory.GetFiles(AlarmFilePath, "AtsAlarm-*.xml"); //Getting Text files
            return Files;
        }

        private DateTime previousDay = DateTime.Now.Date;
        private int lastClearCounter = 0;
        private int currentClearCounter = 0;
        private void timerPoll_Tick(object sender)
        {
            DebugUtil.Instance.LOG.Debug("Alarm timerPoll_Tick...");
            if (!Directory.Exists(AlarmFilePath))
            {
                DebugUtil.Instance.LOG.Error("Alarm file path not exist!");
                return;
            }
            if (DateTime.Now.Date != previousDay)
            {
                CheckAndDeleteOutdatedRecord();     //when a new day start, delete alarms before archiveKeepDays
                previousDay = DateTime.Now.Date;
                DebugUtil.Instance.LOG.Debug("New day start, delete alarms before archiveKeepDays");
            }

            {
                if (currentClearCounter - lastClearCounter > TableOptimizationInterval)
                {
                    //delete all record in alarm_new table, this table work as register to improve performance, content is duplicate with alarm but only keep latest record
                    DAIHelper.Instance.DbServer.ExecuteNonQuery("DELETE FROM Alarm_New;");
                    lastClearCounter = 0;
                    currentClearCounter = 0;
                }
                currentClearCounter += pollInterval;
                DebugUtil.Instance.LOG.Debug("clear alarm_new table for optimization");
            }

            //first move table Alarm_New all item to table Alarm
            //DAIHelper.Instance.DbServer.ExecuteNonQuery("INSERT INTO Alarm SELECT * FROM Alarm_New;");

            {//Save all new files to both Alarm and Alarm_New table
                string[] Files = GetAllAlarmFilesOfToday(); //Getting Text files
                if (Files != null && Files.Count() >= 1)
                {
                    string[] tableNames = { "Alarm_New", "Alarm" };
                    ArchiveFiles(Files, tableNames);
                }
                DebugUtil.Instance.LOG.Debug("Archive " + Files.Count() + "  file complete");
            }
        }

        //delete alarms before archiveKeepDays
        private void CheckAndDeleteOutdatedRecord()
        {
            DebugUtil.Instance.LOG.Info("Try to delete outdated record..");
            //reset all for the new day
            //ArchiveFiles(GetAllAlarmFiles());
            //every midnight check db and delete archive max than 60 days
            int timepoint = CommonFunction.ConvertDateTimeLocalToUnixTime(DateTime.Now.Date - new TimeSpan(archiveKeepDays, 0, 0, 0));
            DAIHelper.Instance.DeleteAlarmBeforeTimeStamp(timepoint);
            DebugUtil.Instance.LOG.Info("Delete alarm exceed " + archiveKeepDays + " days");
            //ArchiveHistoryIscsAlarmCount();             //implement in future to improve peformance
        }

        private void ArchiveHistoryIscsAlarmCount()
        {
            DateTime yesterday = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            List<IscsAlarmCount> listIscsAlarmCount = DAIHelper.Instance.GetAlarmNumberGroupBySubsystemKeyAndLocationIdAtDateFromDb(yesterday);
            DataTable dtContent = new DataTable();
            for (int i = 0; i < IscsAlarmCount.columnNames.Count(); i++)
            {
                string columnName = IscsAlarmCount.columnNames[i];
                dtContent.Columns.Add(columnName);
            }

            foreach(IscsAlarmCount item in listIscsAlarmCount)
            {
                DataRow row = dtContent.NewRow();
                row["subsystemkey"] = item.subsystemkey;
                row["locationId"] = item.locationId;
                row["dateCreated"] = item.dateCreated;
                row["count"] = item.count;
                dtContent.Rows.Add(row);
            }

            DataTable dtInsert = dtContent.Clone();
            try
            {
                DAIHelper.Instance.DbServer.WriteDatatableToDb("IscsAlarmCount", dtInsert);       //Insert new alarm
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Exception catched when ArchiveHistoryIscsAlarmCount: " + ex.ToString());
            }
        }

        private void ArchiveFiles(string[] Files, string[] tableNames)
        {
            if (Files == null || Files.Count() == 0)
                return;
            SaveFilesToDatabase(Files, tableNames);
            //foreach (string fileName in Files)
              //  File.Delete(fileName);
        }

        private void ArchiveFiles(string[] Files, string tableName="Alarm")
        {
            if (Files == null || Files.Count() == 0)
                return;
            SaveFilesToDatabase(Files, tableName);
            //foreach (string fileName in Files)
              //  File.Delete(fileName);
        }

        private string[] GetAllAlarmFilesOfToday()
        {
            string format = "AtsAlarm-" + DateTime.Now.ToString("yyyyMMdd") + "*.xml";
            string[] Files = Directory.GetFiles(AlarmFilePath, format); //Getting Text files
            return Files;
        }

        private void SaveFileToDatabase(string tablename, string fileName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);
            XmlNodeList nodeList = xmlDoc.SelectNodes("/body/alarm");
            DataTable dtContent = new DataTable();
            dtContent.Clear();
            for (int i = 0; i < AtsAlarm.columnNames.Count(); i++)
            {
                string columnName = AtsAlarm.columnNames[i];
                dtContent.Columns.Add(columnName);
            }
            foreach (XmlNode alarmNode in nodeList)
            {
                DataRow row = dtContent.NewRow();
                foreach (XmlNode childNode in alarmNode.ChildNodes)
                {
                    string value = childNode.InnerText;
                    if (childNode.Name == "sourceTime")
                    {
                        value = childNode.FirstChild.InnerText;
                    }
                    row[childNode.Name] = value;
                }
                dtContent.Rows.Add(row);
            }
            try
            {
                DAIHelper.Instance.DbServer.WriteDatatableToDb(tablename, dtContent);
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Exception catched when SaveFileToDatabase: " + ex.ToString());
            }
        }

        private void SaveFilesToDatabase(string[] Files, string tablename)
        {
            DataTable dtContent = GetAlarmTableFromFiles(Files);
            if (dtContent.Rows.Count > 0)
            {
                SaveDataToTable(dtContent, tablename);
            }
        }

        private void SaveFilesToDatabase(string[] Files, string[] tablenames)
        {
            DataTable dtContent = GetAlarmTableFromFiles(Files);
            if (dtContent.Rows.Count > 0)
            {
                foreach (string tablename in tablenames)
                {
                    SaveDataToTable(dtContent, tablename);
                }
            }
        }

        //insert new record and update existing record if alarmID found
        private void SaveDataToTable(DataTable dtContent, string tablename)
        {
            List<string> alarmIDList = DAIHelper.Instance.GetAllAlarmIDFromDb(tablename);            //avoid duplicate
            //split dtContent to insert and update
            DataTable dtInsert = dtContent.Clone();
            DataTable dtUpdate = dtContent.Clone();
            foreach (DataRow row in dtContent.Rows)
            {
                if (alarmIDList.Contains(row["alarmID"]))
                {
                    dtUpdate.ImportRow(row);
                }
                else
                {
                    dtInsert.ImportRow(row);
                }
            }
            try
            {
                DAIHelper.Instance.DbServer.WriteDatatableToDb(tablename, dtInsert);       //Insert new alarm
                DAIHelper.Instance.DbServer.UpdateDatatableToDb(tablename, dtUpdate, new string[]{"state"});       //update existing alarm by alarmID
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Exception catched when SaveFileToDatabase: " + ex.ToString());
            }
        }

        //should remove duplicate alarmID, and only keep the last record if alarmID duplicated
        //2019.11.19, add alarm severity mapping
        private DataTable GetAlarmTableFromFiles(string[] Files)
        {
            if (Files == null || Files.Count() == 0)
                return null;
            DataTable dtContent = new DataTable();
            for (int i = 0; i < AtsAlarm.columnNames.Count(); i++)
            {
                string columnName = AtsAlarm.columnNames[i];
                dtContent.Columns.Add(columnName);
            }
            foreach (string fileName in Files)
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(fileName);
                }
                catch (Exception ex)
                {
                    DebugUtil.Instance.LOG.Error(ex.ToString());
                    continue;
                }
                XmlNodeList nodeList = xmlDoc.SelectNodes("/body/alarm");
                foreach (XmlNode alarmNode in nodeList)
                {
                    DataRow row = null;
                    //alarmNode.ChildNodes.OfType<XmlElement>().Select(p=>p.Name == )
                    string alarmID = alarmNode.SelectSingleNode("alarmID").InnerText;
                    //string alarmID = row["alarmID"].ToString();
                    var temp = dtContent.AsEnumerable().Select(p => p.Field<string>("alarmID")).ToList();
                    int index = temp.IndexOf(alarmID);
                    if (index == -1)
                    {
                        row = dtContent.NewRow();       //if alarmId duplicate, should update row instead of newRow
                    }
                    else
                    {
                        row = dtContent.Rows[index];
                    }
                    foreach (XmlNode childNode in alarmNode.ChildNodes)
                    {
                        string value = childNode.InnerText;
                        if (childNode.Name == "sourceTime")
                        {
                            value = childNode.FirstChild.InnerText;
                        }
                        row[childNode.Name] = value;
                    }
                    if (index == -1)
                    {
                        try
                        {
                            bool bFound = MappingSeverity(row);
                            if (bFound == true && row["alarmSeverity"].ToString() != "E")      //ignore E level severity
                                dtContent.Rows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            DebugUtil.Instance.LOG.Error(ex.ToString());
                        }
                    }
                }
            }

            foreach (string fileName in Files)
                File.Delete(fileName);

            return dtContent;
        }

        //for AtsAlarm only, column IsAtsAlarm=1, column subsystemType is subsystem name, column systemkeyType is system short name
        private bool MappingSeverity(DataRow row)
        {
            bool ret = true;
            if (row["isAtsAlarm"].ToString() == "1")
            {
                string systemName = row["systemkeyType"].ToString();
                string subsystemName = row["subsystemType"].ToString();
                string alarmValue = row["alarmValue"].ToString();
                string alarmDescription = row["alarmDescription"].ToString();

                SeverityMapping severityMapping = CachedMap.Instance.severityMappingList.Where(p => p.systemName == systemName && p.subsystemName == subsystemName && p.alarmValue == alarmValue.Trim() && p.alarmDescription == alarmDescription.Trim()).FirstOrDefault();
                if (severityMapping != null)
                {
                    string alarmSeverity = severityMapping.alarmSeverity;
                    row["alarmSeverity"] = alarmSeverity;
                    ret = true;
                }
                else
                {
                    DebugUtil.Instance.LOG.Error("Cannot find severity mapping for AtsAlarm: system=" + systemName + ",subsystem=" + subsystemName + ",value=" + alarmValue + ",description=" + alarmDescription);
                    ret = false;
                }
            }
            return ret;
        }
    }
}
