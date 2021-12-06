using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using System.Data;

namespace AlarmSimulator
{
    public sealed class DAIHelper
    {
        private static volatile DAIHelper instance;        //singleton
        private static object syncRoot = new Object();

        private SQLiteServer dbServer = null;
        private OracleServer dbServerTransact = null;

        private DAIHelper()
        {
            DebugUtil.Instance.LOG.Info("Init DAIHelper");
                   //Create Sqllite DB\
            try
            {
                dbServer = new SQLiteServer("Local", IscsCachedMap.Instance.AlarmDatabase);
                if (dbServer != null)
                {
                    dbServer.Init();
                    DebugUtil.Instance.LOG.Info("SQLiteServer Inited");
                }
                else
                {
                    DebugUtil.Instance.LOG.Info("SQLiteServer created fail");
                }
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Cannot connect to SQLlite database, application will exit, error=" + ex.ToString());
                System.Environment.Exit(0);
            }
                
            try
            {
                {   //Create Transact DB
                    //ServerType TransactDbType = ServerType.ORACLE;
                    /*if (TransactDbType == ServerType.MYSQL)
                    {
                        string name = IscsCachedMap.Instance.GetRunParam("Name");
                        string IP = IscsCachedMap.Instance.GetRunParam("IP");
                        string UserName = IscsCachedMap.Instance.GetRunParam("UserName");
                        string Password = IscsCachedMap.Instance.GetRunParam("Password");
                        string DatabaseName = IscsCachedMap.Instance.GetRunParam("DatabaseName");
                        dbServer = new MysqlServer(name, IP, UserName, Password, DatabaseName);
                    }
                    else if (TransactDbType == ServerType.ORACLE)*/
                    {
                        string TNSName = IscsCachedMap.Instance.TransactDBConfig["TNSName"];
                        string Username = IscsCachedMap.Instance.TransactDBConfig["Username"];
                        string Password = IscsCachedMap.Instance.TransactDBConfig["Password"];
                        DebugUtil.Instance.LOG.Info("Connect to oracle server, TNSName=" + TNSName + ", Username=" + Username + ", Password=" + Password);
                        dbServerTransact = new OracleServer(TNSName, Username, Password);
                        DebugUtil.Instance.LOG.Info("Connect to oracle server successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Cannot connect to TRANSACT database, application will exit, error=" + ex.ToString());
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

        public OracleServer DbServerTransact
        {
            get
            {
                return dbServerTransact;
            }
        }

        #region SqlLite Operation
        #region Alarm Operation
        //get alarm number during time which dtStart<time<=dtEnd, where dtStart and dtEnd is local time
        public int GetAlarmNumberBetweenFromDb(DateTime dtStart, DateTime dtEnd)
        {
            int startInSeconds = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int endInSeconds = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            string sqlstr = "select count(*) from alarm where sourceTime>=" + startInSeconds + " and sourceTime<" + endInSeconds;
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

        //update top 10 alarm table from db
        public DataTable GetTop10AlarmTableFromDb(string strCondition, int IsAtsAlarm = 1)
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
            string sqlstr = "select " + strColumns + " from alarm where IsAtsAlarm=" + IsAtsAlarm.ToString() + strCondition + " group by alarmDescription, alarmSeverity, assetName order by count desc limit 0,10";
            //string sqlstr = "select alarmDescription, alarmSeverity, assetName, count(*) as count from alarm where 1=1" + CreateSQLForToday() + " group by alarmDescription, alarmSeverity, assetName order by count desc limit 0,10";
            DataTable dtResult = dbServer.ExecuteQuery(sqlstr);
            return dtResult;
        }

        public List<string> GetAllAlarmIDFromDb(int IsAtsAlarm = 1, string tableName = "Alarm")
        {
            string sqlstr = "select alarmID from " + tableName + " and IsAtsAlarm=" + IsAtsAlarm.ToString();
            return dbServer.GetVectorData(sqlstr);
        }

        public List<string> GetAllAlarmNewIDFromDb(int IsAtsAlarm = 1)
        {
            return GetAllAlarmIDFromDb(IsAtsAlarm, "Alarm_New");
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
        {   //sqlite db, use int to store time
            string sqlstr = "delete from Alarm where sourceTime < " + timeStamp;
            dbServer.ExecuteNonQuery(sqlstr);
        }
        #endregion

        #region Pump Operation
        //get all pump volume data
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

        public void ResetAllPumpVolume(double newValue = 0)
        {
            string sqlstr = "update PUMP set volume='" + newValue + "',updateTime='" + DateTime.Now.ToString() + "',bChanged='1'";
            dbServer.ExecuteNonQuery(sqlstr);
        }

        public void SetPumpVolume(double newValue, DateTime updateTime, string name, string locationName)
        {
            string sqlstr = "update PUMP set volume='" + newValue + "',updateTime='" + CommonFunction.ConvertDateTimeLocalToUnixTime(updateTime) + "',bChanged='1' where name='" + name + "' and locationName='" + locationName + "'";
            dbServer.ExecuteNonQuery(sqlstr);
        }
        #endregion

        #endregion

        #region Transact Oracle DB Operation
        //get pump state change data
        public List<PumpStateChange> GetTodayPumpStateChangeFromDb()
        {
            string sqlstr = "select p.*,e.name,e.locationkey from PUMP_STATE_CHANGE p left join entity e where to_char(p.updatetime, 'dd-mm-yyyy') = to_char(current_date, 'dd-mm-yyyy')";
            //log.Debug(sqlstr);
            List<PumpStateChange> ret = null;
            DataTable schemaTable = dbServerTransact.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<PumpStateChange>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    PumpStateChange item = new PumpStateChange();
                    item.entityKey = myField["entityKey"].ToString();
                    try
                    {
                        item.updateTime = Convert.ToDateTime(myField["updateTime"]);
                        item.value = Convert.ToBoolean(myField["value"]);
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error("Pump_State_Change record: entityKey=" + myField["entityKey"] + ",updateTime" + myField["updateTime"] + ",value=" + myField["value"] + "cannot convert, reason=" + ex.ToString());
                    }
                    item.entityName = myField["name"].ToString();
                    item.locationKey = myField["locationkey"].ToString();
                    item.name = item.entityKey.Split('.')[3];               //if entityName=DT08.DNG.PLC03.DSS03.diiDSS-Pump2StopRun, name=DSS03
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

        public void DeleteAllPumpStateChange()
        {
            string sqlstr = "delete from PUMP_STATE_CHANGE";
            dbServerTransact.ExecuteNonQuery(sqlstr);
        }

        public void DeletePumpStateChangeBeforeToday()
        {
            string sqlstr = "delete from PUMP_STATE_CHANGE where to_char(p.updatetime, 'dd-mm-yyyy') <> to_char(current_date, 'dd-mm-yyyy')";
            dbServerTransact.ExecuteNonQuery(sqlstr);
        }

        public List<string> GetAllPumpEntityKeyListFromDb(string sqlCondition = "")
        {
            string sqlstr = "select pkey from entity where name like '%DNG%diiDS%-Pump%StopRun' and deleted=0" + sqlCondition;
            //log.Debug(sqlstr);
            List<string> ret = null;
            DataTable schemaTable = dbServerTransact.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<string>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    string pkey = myField["pkey"].ToString();
                    ret.Add(pkey);
                }
            }
            return ret;
        }
        #endregion

    }
}
