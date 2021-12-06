using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace AlarmAnalysisService
{
    public sealed class CachedMap
    {
        private static volatile CachedMap instance;        //singleton
        private static object syncRoot = new Object();

        string assemblyDirPath;
        string runParamfile = "config.ini";
        private Dictionary<string, string> runParams = new Dictionary<string, string>();

        string configFile = "AlarmAnalysisService.config.xml";
        public int pollInterval = 3000;            //Polling Interval for ATS Alarm Files in Seconds, config in config.xml
        public string AlarmFilePath = "";
        public string AlarmDatabase = "";
        public int archiveKeepDays = 60;            //Archive keep days in db in Seconds, config in config.xml
        public int TableOptimizationInterval = 60000;
        public bool ClearAllAlarmFilesWhenStart = false;
        public Dictionary<string, string> TransactDBConfig = new Dictionary<string, string>();
        public List<Location> locationList = new List<Location>();

        string severityMappingFile = "AlarmSeverityMapping.config.csv";
        public List<SeverityMapping> severityMappingList = new List<SeverityMapping>();

        private CachedMap()
        {
            string assemblyFilePath = Assembly.GetExecutingAssembly().Location;
            assemblyDirPath = Path.GetDirectoryName(assemblyFilePath);
            runParamfile = assemblyDirPath + "\\" + runParamfile;       //for some program the file not opened in current binary directory, such as windows service program
            configFile = assemblyDirPath + "\\" + configFile;
            severityMappingFile = assemblyDirPath + "\\" + severityMappingFile;

            ParseCommandLine();
            ReadRunParamFile();
            ReadConfigFile();
            ReadSeverityMappingFile();
        }

        public static CachedMap Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new CachedMap();
                    }
                }

                return instance;
            }
        }

        /** Add a parameter
		  * @param name Name of parameter
          * @param value Value of parameter
          * Pre: name and value are not NULL
		  */
        public void SetRunParam(string name, string value)
        {
            runParams[name] = value;
        }

        /**Retrieve a parameter value
          * @return Value of parameter
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public string GetRunParam(string name)
        {
            return runParams[name];
        }

        /** Determine whether a parameter with the given name has been set
          * @return True (parameter set), False (parameter not set)
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public bool isSetRunParam(string name)
        {
            bool isSet = runParams.ContainsKey(name);
            return isSet;
        }

        public void ParseCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                try
                {
                    int index = arg.IndexOf('=');
                    //string[] sArray = strReadLine.Split('=');
                    string name = arg.Substring(0, index);  //sArray[0];
                    string value = arg.Substring(index + 1, arg.Length - index - 1); //sArray[1];
                    runParams[name] = value;
                }
                catch (Exception)
                {
                }
            }
        }

        // Read start.ini config file to parse the db connection parameter
        public void ReadRunParamFile()
        {
            Console.WriteLine("Read RunParam File");
            string strReadFilePath = runParamfile;
            Dictionary<string, string> ret = new Dictionary<string, string>();
            if (File.Exists(strReadFilePath))
            {
                StreamReader srReadFile = new StreamReader(strReadFilePath);

                // 开始解析文件
                try
                {
                    while (!srReadFile.EndOfStream)
                    {
                        string strReadLine = srReadFile.ReadLine();
                        int index = strReadLine.IndexOf('=');
                        //string[] sArray = strReadLine.Split('=');
                        string name = strReadLine.Substring(0, index);  //sArray[0];
                        name = name.TrimStart('-');
                        string value = strReadLine.Substring(index + 1, strReadLine.Length - index - 1); //sArray[1];
                        runParams[name] = value;
                    }
                }
                catch (System.Exception e)
                {
                }
                // 关闭读取流文件
                srReadFile.Close();
            }
        }

        private void ReadConfigFile()
        {
            if (!File.Exists(configFile))
            {
                DebugUtil.Instance.LOG.Info(configFile + " do not exist");
                return;
            }
            Console.WriteLine("config file read started");
            DebugUtil.Instance.LOG.Info("config file read started");
            XDocument doc = XDocument.Load(configFile);
            {   //poll interval
                XElement xElement = doc.Root.Element("PollInterval");
                string strPollInterval = xElement.Attribute("Value").Value;
                try
                {
                    pollInterval = Convert.ToInt32(strPollInterval);
                    DebugUtil.Instance.LOG.Info("pollInterval=" + pollInterval);
                    if (pollInterval <= 0)
                        throw new Exception();
                }
                catch
                {
                    Console.WriteLine("Poll Interval in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Info("Poll Interval in config file should be an integer greater than 0");
                }
            }
            {   //alarm file path
                XElement elPath = doc.Root.Element("AlarmFilePath");
                AlarmFilePath = elPath.Attribute("Value").Value;
                DebugUtil.Instance.LOG.Info("AlarmFilePath=" + AlarmFilePath);
                if (!Directory.Exists(AlarmFilePath))
                {
                    Console.WriteLine("Alarm file path not exist!");
                    DebugUtil.Instance.LOG.Error("Alarm file path not exist");
                }
            }
            {   //alarm db path
                XElement elPath = doc.Root.Element("AlarmDatabase");
                AlarmDatabase = elPath.Attribute("Value").Value;
                DebugUtil.Instance.LOG.Info("AlarmDatabase=" + AlarmDatabase);
                if (!File.Exists(AlarmDatabase))
                {
                    Console.WriteLine("Alarm database not exist!");
                    DebugUtil.Instance.LOG.Error("Alarm database not exist");
                }
            }
            {   //archive keep days in db
                XElement xElement = doc.Root.Element("ArchiveKeepDays");
                string strValue = xElement.Attribute("Value").Value;
                try
                {
                    archiveKeepDays = Convert.ToInt32(strValue);
                    DebugUtil.Instance.LOG.Info("archiveKeepDays=" + archiveKeepDays);
                    if (pollInterval <= 0)
                        throw new Exception();
                }
                catch
                {
                    Console.WriteLine("Archive keep days in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Error("Archive keep days in config file should be an integer greater than 0");
                }
            }
            {   //optimization db interval, clear table alarm_new
                XElement xElement = doc.Root.Element("TableOptimizationInterval");
                string strValue = xElement.Attribute("Value").Value;
                try
                {
                    TableOptimizationInterval = Convert.ToInt32(strValue);
                    DebugUtil.Instance.LOG.Info("TableOptimizationInterval=" + TableOptimizationInterval);
                    if (TableOptimizationInterval <= 10000)
                        throw new Exception();
                }
                catch
                {
                    Console.WriteLine("Table Optimization Interval in config file should be an integer greater than 10000");
                    DebugUtil.Instance.LOG.Error("Table Optimization Interval in config file should be an integer greater than 10000");
                }
            }
            {   //ClearAllAlarmFilesWhenStart
                XElement xElement = doc.Root.Element("ClearAllAlarmFilesWhenStart");
                string value = xElement.Attribute("Value").Value;
                try
                {
                    ClearAllAlarmFilesWhenStart = Convert.ToBoolean(value);
                    DebugUtil.Instance.LOG.Info("ClearAllAlarmFilesWhenStart=" + ClearAllAlarmFilesWhenStart);
                }
                catch
                {
                    Console.WriteLine("ClearAllAlarmFilesWhenStart in config file should be True or False");
                    DebugUtil.Instance.LOG.Error("ClearAllAlarmFilesWhenStart in config file should be True or False");
                }
            }
            {   //TransactDbConfig
                XElement xElement = doc.Root.Element("TransactDBConfig");
                string DbType = xElement.Attribute("DbType").Value;
                string TNSName = xElement.Attribute("TNSName").Value;
                string Username = xElement.Attribute("Username").Value;
                string Password = xElement.Attribute("Password").Value;
                DebugUtil.Instance.LOG.Info("TransactDBConfig=" + xElement.ToString());     //String.Concat(element.Nodes())
                TransactDBConfig["DbType"] = DbType;
                TransactDBConfig["TNSName"] = TNSName;
                TransactDBConfig["Username"] = Username;
                TransactDBConfig["Password"] = Password;
            }
            //Parse location
            IEnumerable<XElement> rech = doc.Root.Descendants("Location");
            List<Location> tempList = new List<Location>();
            foreach (XElement el in rech)
            {
                Location location = new Location();
                location.ID = el.Attribute("ID").Value;
                location.Name = el.Attribute("Name").Value;
                location.TypeName = el.Attribute("TypeName").Value;
                if (el.Attribute("Operational") != null)
                    location.Operational = el.Attribute("Operational").Value;
                tempList.Add(location);
            }
            locationList.AddRange(tempList.Where(p => p.TypeName == "Occ"));
            locationList.AddRange(tempList.Where(p => p.TypeName == "Depot"));
            locationList.AddRange(tempList.Where(p => p.TypeName == "Station"));
            Console.WriteLine("config file read completed");
            DebugUtil.Instance.LOG.Info("config file read completed");
        }

        // Read SeverityMapping file
        public void ReadSeverityMappingFile()
        {
            if (!File.Exists(severityMappingFile))
            {
                DebugUtil.Instance.LOG.Info(configFile + " do not exist");
                return;
            }
            // Read severity mapping file
            Console.WriteLine("severity mapping file read started");
            DebugUtil.Instance.LOG.Info("severity mapping file read started");
            string strReadFilePath = severityMappingFile;
            Dictionary<string, string> ret = new Dictionary<string, string>();
            if (File.Exists(strReadFilePath))
            {
                StreamReader srReadFile = new StreamReader(strReadFilePath);

                try
                {
                    while (!srReadFile.EndOfStream)
                    {
                        string strReadLine = srReadFile.ReadLine();
                        strReadLine = strReadLine.Trim().Replace("\"", "");
                        if (strReadLine.ToLower().StartsWith("system") || string.IsNullOrEmpty(strReadLine))
                        {   //header line or empty line, ignore it
                            continue;
                        }
                        string systemName = "";
                        string subsystemName = "";
                        string alarmValue = "";
                        string alarmDescription = "";
                        string alarmSeverity = "";
                        
                        string[] sArray = strReadLine.Split(',');   //RSC,PIS,Summary Status of PEC,E
                        if (sArray.Count() < 5)
                        {
                            string strError = "Config file '" + severityMappingFile + "' line format error: " + strReadLine;
                            Console.WriteLine(strError);
                            DebugUtil.Instance.LOG.Error(strError);
                            continue;
                        }
                        else if (sArray.Count() == 5)
                        {
                            systemName = sArray[0].Trim();
                            subsystemName = sArray[1].Trim();
                            alarmValue = sArray[2].Trim();
                            alarmDescription = sArray[3].Trim();
                            alarmSeverity = sArray[4].Trim();
                        }
                        else
                        {   //description may contain comma
                            systemName = sArray[0].Trim();
                            subsystemName = sArray[1].Trim();
                            alarmValue = sArray[2].Trim();
                            alarmSeverity = sArray[sArray.Count() - 1].Trim();
                            alarmDescription = strReadLine.Substring(sArray[0].Length + sArray[1].Length + sArray[2].Length + 3, strReadLine.Length - sArray[0].Length - sArray[1].Length - sArray[2].Length - 5).Trim();     //sArray[1];
                        }
                        if (systemName != "" && subsystemName != "" && alarmDescription != "" && alarmSeverity != "")
                        {
                            if (alarmSeverity.ToLower() == "alert")
                                alarmSeverity = "Alert";
                            else if(alarmSeverity.ToLower() == "e")
                                alarmSeverity = "E";
                            else if (alarmSeverity != "1" && alarmSeverity != "2" && alarmSeverity != "3")
                            {   //if severity is not in one of above value, just record an error log
                                DebugUtil.Instance.LOG.Error("line '" + strReadLine + "' severity field is invalid");
                            }
                            SeverityMapping severityMapping = new SeverityMapping(systemName, subsystemName, alarmValue.Trim(), alarmDescription.Trim(), alarmSeverity);
                            severityMappingList.Add(severityMapping);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    DebugUtil.Instance.LOG.Error(ex.ToString());
                }
                // 关闭读取流文件
                srReadFile.Close();
            }
            Console.WriteLine("severity mapping file read completed");
            DebugUtil.Instance.LOG.Info("severity mapping file read completed");
        }
    }
}
