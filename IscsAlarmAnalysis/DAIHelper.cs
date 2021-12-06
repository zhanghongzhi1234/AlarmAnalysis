using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using System.Data;

namespace TemplateProject
{
    public sealed class DAIHelper
    {
        private static volatile DAIHelper instance;        //singleton
        private static object syncRoot = new Object();

        private SQLiteServer dbServer = null;

        private DAIHelper()
        {
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            
            //typeof(Program).Assembly.GetName().Name;
            CachedMap cachedMap = null;
    
            /*if (assemblyName.StartsWith("Ats", StringComparison.CurrentCultureIgnoreCase))
            {
                cachedMap = AtsCachedMap.Instance;
            }
            else
            {
                cachedMap = IscsCachedMap.Instance;
            }*/
            if (App.runningMode == "Ats")
            {
                cachedMap = AtsCachedMap.Instance;
            }
            else
            {
                cachedMap = IscsCachedMap.Instance;
            }
            DebugUtil.Instance.LOG.Info("Init DAIHelper");
            cachedMap.SetRunParam("DbType", "SQLITE");
            if (cachedMap.isSetRunParam("DbType"))
            {
                //string DbType = AtsCachedMap.Instance.GetRunParam("DbType");
                string DbType = cachedMap.GetRunParam("DbType");
                DebugUtil.Instance.LOG.Info("DbType=" + DbType);
                try
                {
                    if (DbType == "SQLITE")
                    {
                        //dbServer = new SQLiteServer("Local", CachedMap.Instance.AlarmDatabase);
                        dbServer = new SQLiteServer("Local", cachedMap.AlarmDatabase);
                        if (dbServer != null)
                        {
                            dbServer.Init();
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugUtil.Instance.LOG.Error("Cannot connect to database, application will exit, error=" + ex.ToString());
                    throw ex;
                }
                //dbServer = serverMap["TRANSACT"] as DatabaseServer;
                //string sqlstr = "select name from location where pkey>=3 order by pkey asc";
                //DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            }
            else
            {
                DebugUtil.Instance.LOG.Error("Cannot find database setting, application will exit");
                System.Environment.Exit(0);
            }
        }

        public static DAIHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new DAIHelper();
                    }
                }

                return instance;
            }
        }

        public SQLiteServer DbServer
        {
            get
            {
                return dbServer;
            }
        }

        //get alarm number during time which dtStart<time<=dtEnd, where dtStart and dtEnd is local time
        public int GetAlarmNumberBetweenFromDb(DateTime dtStart, DateTime dtEnd, int IsAtsAlarm = 1)
        {
            int startInSeconds = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int endInSeconds = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            string sqlstr = "select count(*) from alarm where sourceTime>=" + startInSeconds + " and sourceTime<" + endInSeconds + " and IsAtsAlarm="  + IsAtsAlarm.ToString();
            return Convert.ToInt32(dbServer.GetSingleData(sqlstr));
        }

        public string CreateSQLForToday()
        {
            DateTime dtEnd = DateTime.Now;
            DateTime dtStart = new DateTime(dtEnd.Year, dtEnd.Month, dtEnd.Day, 0, 0, 0, DateTimeKind.Local);
            /*int epochStart = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int epochEnd = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            string sqlstr = "sourceTime>=" + epochStart + " and sourceTime < " + epochEnd;
            return sqlstr;*/
            return CreateSQLBetween(dtStart, dtEnd);
        }

        public string CreateSQLBetween(DateTime dtStart, DateTime dtEnd)
        {
            int epochStart = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int epochEnd = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            string sqlstr = " and sourceTime>=" + epochStart + " and sourceTime < " + epochEnd;
            return sqlstr;
        }

        public string CreateSQLBetweenAndExcludeRvenue(DateTime dtStart, DateTime dtEnd, DateTime dtRvnStart, DateTime dtRvnEnd)
        {
            int epochStart = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int epochEnd = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            int epochRvnStart = CommonFunction.ConvertDateTimeLocalToUnixTime(dtRvnStart);
            int epochRvnEnd = CommonFunction.ConvertDateTimeLocalToUnixTime(dtRvnEnd);
            string sqlstr = " and sourceTime>=" + epochStart + " and sourceTime < " + epochEnd + " and (sourceTime < " + epochRvnStart + " or sourceTime >= " + epochRvnEnd + ")";
            return sqlstr;
        }

        public string CreateSQLFromToday()
        {
            DateTime dtEnd = DateTime.Now;
            DateTime dtStart = new DateTime(dtEnd.Year, dtEnd.Month, dtEnd.Day, 0, 0, 0, DateTimeKind.Local);
            int epochStart = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int epochEnd = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            string sqlstr = " and sourceTime>=" + epochStart;
            return sqlstr;
        }

        //get alarm number of Severity on certain day
        public int GetTodayAlarmNumberOfSeverityFromDb(int severity)
        {
            string sqlstr = "select count(*) from alarm where 1=1" + CreateSQLForToday() + " and alarmSeverity=" + severity;
            return Convert.ToInt32(dbServer.GetSingleData(sqlstr));
        }

        //get alarm number by system on certain day
        public int GetTodayAlarmNumberBySystemKeyFromDb(int systemkey)
        {
            string sqlstr = "select count(*) from alarm where 1=1" + CreateSQLForToday() + " and systemkey=" + systemkey;
            return Convert.ToInt32(dbServer.GetSingleData(sqlstr));
        }

        //get alarm number by system short name on certain day
        public int GetTodayAlarmNumberBySystemShortNameFromDb(string ShortName)
        {
            string sqlstr = "select count(*) from alarm where 1=1" + CreateSQLForToday() + " and systemkeyType='" + ShortName + "'";
            return Convert.ToInt32(dbServer.GetSingleData(sqlstr));
        }

        //get alarm number by subsystem on certain day
        public int GetTodayAlarmNumberBySubsystemKeyFromDb(int subsystemkey)
        {
            string sqlstr = "select count(*) from alarm where 1=1" + CreateSQLForToday() + " and subsystemkey=" + subsystemkey;
            return Convert.ToInt32(dbServer.GetSingleData(sqlstr));
        }

        //get alarm number by subsystem on certain day
        public int GetTodayAlarmNumberBySubsystemNameFromDb(string subsystemName)
        {
            string sqlstr = "select count(*) from alarm where 1=1" + CreateSQLForToday() + " and subsystemType='" + subsystemName + "'";
            return Convert.ToInt32(dbServer.GetSingleData(sqlstr));
        }

        //get all alarm by system key on certain day, if systemkey<0, return all system alarm
        public List<AtsAlarm> GetTodayAtsAlarmListBySystemKeyFromDb(int systemkey)
        {
            string sqlstr = "select * from alarm where 1=1" + CreateSQLForToday();
            if (systemkey >= 0)
                sqlstr += " and systemkey=" + systemkey;
            sqlstr += " order by sourceTime desc";
            return GetAtsAlarmsFromDb(sqlstr);
        }

        //get all alarm by system short name on certain day, if systemkey<0, return all system alarm
        public List<AtsAlarm> GetTodayAtsAlarmListBySystemShortNameFromDb(string ShortName = "", string orderbyTime = "asc")
        {
            string sqlstr = "select * from alarm where 1=1" + CreateSQLForToday();
            if (ShortName != "")
                sqlstr += " and systemkeyType='" + ShortName + "'";
            sqlstr += " order by sourceTime " + orderbyTime;
            return GetAtsAlarmsFromDb(sqlstr);
        }

        public List<AtsAlarm> GetTodayAtsAlarmListAll()
        {
            string sqlstr = "select * from alarm where 1=1" + CreateSQLForToday();
            sqlstr += " order by sourceTime desc";
            return GetAtsAlarmsFromDb(sqlstr);
        }

        //IsAtsAlarm: 1 mean AtsAlarm, 0 mean IscsAlarm
        public List<AtsAlarm> GetAlarmNewListBetween(DateTime startTime, DateTime endTime, int IsAtsAlarm = 1)
        {
            return GetAlarmListBetween(startTime, endTime, IsAtsAlarm, "Alarm_New");
        }

        //IsAtsAlarm: 1 mean AtsAlarm, 0 mean IscsAlarm
        public List<AtsAlarm> GetAlarmListBetween(DateTime startTime, DateTime endTime, int IsAtsAlarm = 1, string tableName = "Alarm", string locationId="", List<string> subsystemkeyList = null)
        {
            string strCondition = CreateSQLBetween(startTime, endTime);
            if (locationId != "")
                strCondition += " and locationId='" + locationId + "'";
            if (subsystemkeyList != null && subsystemkeyList.Count > 0)
            {
                string subIDList = string.Join("','", subsystemkeyList.ToArray());
                strCondition += " and subsystemkey in ('" + subIDList + "')";
            }
            string sqlstr = "select * from " + tableName + " where IsAtsAlarm="  + IsAtsAlarm.ToString() + strCondition;
            sqlstr += " order by sourceTime desc";
            return GetAtsAlarmsFromDb(sqlstr, IsAtsAlarm);
        }

        //IsAtsAlarm: 1 mean AtsAlarm, 0 mean IscsAlarm
        public List<AtsAlarm> GetAlarmListToday(int IsAtsAlarm = 1, string tableName = "Alarm", string locationId = "", List<string> subsystemkeyList = null)
        {
            DateTime startTime = DateTime.Now.Date;
            DateTime endTime = DateTime.Now.Date + new TimeSpan(23, 59, 59);
            return GetAlarmListBetween(startTime, endTime, IsAtsAlarm, tableName, locationId, subsystemkeyList);
        }

        //update top 10 alarm table from db
        public DataTable GetTop10AlarmTableFromDb(string strCondition, string strRule, int IsAtsAlarm = 1)
        {
            string strColumns = "";
            if (IsAtsAlarm == 1)
            {
                strColumns = "alarmDescription, alarmSeverity, assetName, count(*) as count";
            }
            else
            {
                strColumns = "alarmSeverity, locationId, alarmDescription, count(*) as count";
            }
            string sqlstr = "select " + strColumns + " from alarm where IsAtsAlarm=" + IsAtsAlarm.ToString() + " and state=1" + strCondition + " group by " + strRule + " order by count desc limit 0,10";
            //string sqlstr = "select " + strColumns + " from alarm where IsAtsAlarm=" + IsAtsAlarm.ToString() + strCondition + " group by alarmDescription, alarmSeverity, assetName order by count desc limit 0,10";
            DataTable dtResult = dbServer.ExecuteQuery(sqlstr);
            return dtResult;
        }

        public List<string> GetAllAlarmIDFromDb(int IsAtsAlarm = 1, string tableName = "Alarm")
        {
            string sqlstr = "select alarmID from " + tableName + " where IsAtsAlarm=" + IsAtsAlarm.ToString();
            return dbServer.GetVectorData(sqlstr);
        }

        public List<string> GetAllAlarmNewIDFromDb(int IsAtsAlarm = 1)
        {
            return GetAllAlarmIDFromDb(IsAtsAlarm, "Alarm_New");
        }
        //get alarm data
        public List<AtsAlarm> GetAtsAlarmsFromDb(string sqlstr, int IsAtsAlarm = 1)
        {
            List<AtsAlarm> ret = null;
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<AtsAlarm>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    AtsAlarm item = new AtsAlarm();
                    item.alarmID = myField["alarmID"].ToString();
                    item.sourceTime = CommonFunction.ConvertUnixTimeToDateTimeLocal(Convert.ToInt32(myField["sourceTime"].ToString()));
                    string ackTime = myField["ackTime"].ToString();
                    if (ackTime != null && ackTime != "" && ackTime != "0")
                        item.Ack = "1";
                    else
                        item.Ack = "0";
                    try
                    {
                        item.closeTime = Convert.ToInt64(myField["closeTime"].ToString());
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error(ex.ToString());
                    }
                    item.assetName = myField["assetName"].ToString();
                    item.severityID = myField["alarmSeverity"].ToString();
                    item.description = myField["alarmDescription"].ToString();
                    item.locationId = myField["locationId"].ToString();        //subsystemkey and systemkey no more use
                    item.alarmValue = myField["alarmValue"].ToString();
                    item.state = myField["state"].ToString();
                    if (IsAtsAlarm == 1)
                    {
                        item.subsystemID = AtsCachedMap.Instance.GetSubsystemIDByName(myField["subsystemType"].ToString());
                        item.systemID = AtsCachedMap.Instance.GetSystemIDByShortName(myField["systemkeyType"].ToString());
                    }
                    else
                    {
                        item.subsystemID = myField["subsystemkey"].ToString();
                        item.systemID = myField["systemkey"].ToString();

                    }
                    ret.Add(item);
                    //For each property of the field...
                    /*foreach (DataColumn myProperty in schemaTable.Columns)
                    {
                        //Display the field name and value.
                        Console.WriteLine(myProperty.ColumnName + " = " + myField[myProperty].ToString());
                    }*/
                }
            }
            return ret;
        }

        //get alarm data
        public List<AtsAlarmRawTable> GetAtsAlarmRawTableFromDb(string sqlstr)
        {
            //log.Debug(sqlstr);
            List<AtsAlarmRawTable> ret = null;
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<AtsAlarmRawTable>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    AtsAlarmRawTable item = new AtsAlarmRawTable();
                    item.alarmID = myField["alarmID"].ToString();
                    item.sourceTime = myField["sourceTime"].ToString();
                    item.ackTime = myField["ackTime"].ToString();
                    item.closeTime = myField["closeTime"].ToString();
                    item.assetName = myField["assetName"].ToString();
                    item.alarmSeverity = myField["alarmSeverity"].ToString();
                    item.alarmDescription = myField["alarmDescription"].ToString();
                    item.alarmAcknowledgeBy = myField["alarmAcknowledgeBy"].ToString();
                    item.state = myField["state"].ToString();
                    item.locationId = myField["locationId"].ToString();
                    item.parentAlarmID = myField["parentAlarmID"].ToString();
                    item.avalancheHeadID = myField["avalancheHeadID"].ToString();
                    item.isHeadOfAvalanche = myField["isHeadOfAvalanche"].ToString();
                    item.isChildOfAvalanche = myField["isChildOfAvalanche"].ToString();
                    item.mmsState = myField["mmsState"].ToString();
                    item.alarmValue = myField["alarmValue"].ToString();
                    item.omAlarm = myField["omAlarm"].ToString();
                    item.alarmType = myField["alarmType"].ToString();
                    item.subsystemkey = myField["subsystemkey"].ToString();
                    item.systemkey = myField["systemkey"].ToString();
                    item.alarmComments = myField["alarmComments"].ToString();
                    item.strAlarmType = myField["strAlarmType"].ToString();
                    item.subsystemType = myField["subsystemType"].ToString();
                    item.systemkeyType = myField["systemkeyType"].ToString();
                    item.locationKey = myField["locationKey"].ToString();
                    ret.Add(item);
                    //For each property of the field...
                    /*foreach (DataColumn myProperty in schemaTable.Columns)
                    {
                        //Display the field name and value.
                        Console.WriteLine(myProperty.ColumnName + " = " + myField[myProperty].ToString());
                    }*/
                }
            }
            return ret;
        }

        public void DeleteAlarmForToday()
        {
            string sqlstr = "delete from Alarm where 1=1" + CreateSQLFromToday();
            dbServer.ExecuteNonQuery(sqlstr);
        }

        public void DeleteAlarmBeforeTimeStamp(int timeStamp)
        {
            string sqlstr = "delete from Alarm where sourceTime < " + timeStamp;
            dbServer.ExecuteNonQuery(sqlstr);
        }

        //get active alarm count list history exclude today
        /*public List<IscsAlarmCount> GetActiveIscsAlarmCountListByYesterday()
        {
            DateTime dtStart = DateTime.Today.AddDays(-180);            //stage 7 require statistic half year
            DateTime dtEnd = DateTime.Today;
            string sqlstr = "select subsystemkey as subsystemID, locationId, count(*) as count from alarm where state=1 and isAtsAlarm=0" + CreateSQLBetween(dtStart, dtEnd) + " group by subsystemkey, locationId";
            List<IscsAlarmCount> ret = null;
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<IscsAlarmCount>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    IscsAlarmCount item = new IscsAlarmCount();
                    item.dateCreated = dtStart;
                    item.subsystemID = myField["subsystemID"].ToString();
                    item.locationId = myField["locationId"].ToString();
                    item.count = Convert.ToInt32(myField["count"].ToString());
                    ret.Add(item);
                }
            }
            return ret;
        }*/

        //get alarm list in past 6 months exclude today
        public List<AtsAlarm> GetAlarmListHistory(int IsAtsAlarm = 1, string tableName = "Alarm", string locationId = "", List<string> subsystemkeyList = null)
        {
            DateTime startTime = DateTime.Today.AddDays(-180);            //stage 7 require statistic half year
            DateTime endTime = DateTime.Today;
            return GetAlarmListBetween(startTime, endTime, IsAtsAlarm, tableName, locationId, subsystemkeyList);
        }
        #region pump operation
        public List<Pump> GetAllPumpFromDb()
        {
            string sqlstr = "select * from PUMP";
            //log.Debug(sqlstr);
            List<Pump> ret = null;
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<Pump>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    Pump item = new Pump();
                    item.name = myField["name"].ToString();
                    item.locationName = myField["locationName"].ToString();
                    try
                    {
                        item.volume = Convert.ToDouble(myField["volume"]);
                        item.updateTime = CommonFunction.ConvertUnixTimeToDateTimeLocal(Convert.ToInt32(myField["updateTime"]));
                        item.locationKey = IscsCachedMap.Instance.locationList.Where(p => p.Name == item.locationName).First().ID;
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error("Pump record: name" + myField["name"] + ",locationName=" + myField["locationName"] + " have error when loading from DB, reason=" + ex.ToString());
                        continue;
                    }
                    ret.Add(item);
                }
            }
            return ret;
        }

        public List<Pump> GetAllChangedPumpFromDb()
        {
            string sqlstr = "select * from PUMP where bChanged=1";
            //log.Debug(sqlstr);
            List<Pump> ret = null;
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<Pump>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    Pump item = new Pump();
                    item.name = myField["name"].ToString();
                    item.locationName = myField["locationName"].ToString();
                    try
                    {
                        item.volume = Convert.ToDouble(myField["volume"]);
                        item.updateTime = CommonFunction.ConvertUnixTimeToDateTimeLocal(Convert.ToInt32(myField["updateTime"]));
                        item.locationKey = IscsCachedMap.Instance.locationList.Where(p => p.Name == item.locationName).First().ID;
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error("Pump record: entityKey=" + myField["entityKey"] + ",name" + myField["name"] + ",locationKey=" + myField["locationKey"] + " have error when loading from DB, reason=" + ex.ToString());
                        continue;
                    }
                    ret.Add(item);
                }
            }
            return ret;
        }

        public void UpdateAllPumpToUnchanged()
        {
            string sqlstr = "update pump set bChanged=0";
            dbServer.ExecuteNonQuery(sqlstr);
        }
        #endregion
    }
}
