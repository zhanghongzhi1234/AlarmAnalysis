using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using System.Data;
using TemplateProject;

namespace AlarmAnalysisService
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
            try
            {
                dbServer = new SQLiteServer("Local", CachedMap.Instance.AlarmDatabase);
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
                        string TNSName = CachedMap.Instance.TransactDBConfig["TNSName"];
                        string Username = CachedMap.Instance.TransactDBConfig["Username"];
                        string Password = CachedMap.Instance.TransactDBConfig["Password"];
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
        public int GetAlarmNumberBetweenFromDb(DateTime dtStart, DateTime dtEnd, int IsAtsAlarm = 1)
        {
            int startInSeconds = CommonFunction.ConvertDateTimeLocalToUnixTime(dtStart);
            int endInSeconds = CommonFunction.ConvertDateTimeLocalToUnixTime(dtEnd);
            string sqlstr = "select count(*) from alarm where sourceTime>=" + startInSeconds + " and sourceTime<" + endInSeconds + " and IsAtsAlarm=" + IsAtsAlarm.ToString();
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

        public void DeleteAlarmBeforeTimeStamp(int timeStamp)
        {   //sqlite db, use int to store time
            string sqlstr = "delete from Alarm where sourceTime < " + timeStamp;
            dbServer.ExecuteNonQuery(sqlstr);
        }

        public List<string> GetAllAlarmIDFromDb(string tableName = "Alarm")
        {
            string sqlstr = "select alarmID from " + tableName;
            return dbServer.GetVectorData(sqlstr);
        }

        public List<string> GetAllAlarmNewIDFromDb()
        {
            return GetAllAlarmIDFromDb("Alarm_New");
        }

        //get alarm number by subsystem on certain day
        public List<IscsAlarmCount> GetAlarmNumberGroupBySubsystemKeyAndLocationIdAtDateFromDb(DateTime date)
        {
            DateTime dtStart = date;
            DateTime dtEnd = date + new TimeSpan(23, 59, 59);
            string sqlstr = "select subsystemkey, locationId, count(*) as count from alarm where 1=1 and isAtsAlarm=0 and" + CreateSQLBetween(dtStart, dtEnd) + " group by subsystemkey, locationId";
            List<IscsAlarmCount> ret = null;
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<IscsAlarmCount>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    IscsAlarmCount item = new IscsAlarmCount();
                    item.dateCreated = date;
                    item.subsystemkey = myField["subsystemkey"].ToString();
                    item.locationId = myField["locationId"].ToString();
                    item.count = Convert.ToInt32(myField["count"].ToString());
                    ret.Add(item);
                }
            }
            return ret;
        }
        #endregion

        #region Pump Operation
        //get all pump volume data, the updateTime is timestamp in Sqllite database
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
                        item.flowRate = Convert.ToDouble(myField["flowRate"]);
                        item.volume = Convert.ToDouble(myField["volume"]);
                        item.locationKey = CachedMap.Instance.locationList.Where(p => p.Name == item.locationName).FirstOrDefault().ID;
                        item.updateTime = CommonFunction.ConvertUnixTimeToDateTimeLocal(Convert.ToInt32(myField["updateTime"].ToString()));
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
            string sqlstr = "update PUMP set volume='" + newValue + "',updateTime='" + CommonFunction.ConvertDateTimeLocalToUnixTime(DateTime.Now) + "',bChanged='1'";
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
            string sqlstr = "select p.*,e.name,e.locationkey from PUMP_STATE_CHANGE p left join entity e on p.entitykey=e.pkey where to_char(p.updatetime, 'dd-mm-yyyy') = to_char(current_date, 'dd-mm-yyyy')";
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
                        item.value = CommonFunction.GetBoolValue(myField["value"]);
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error("Pump_State_Change record: entityKey=" + myField["entityKey"] + ",updateTime" + myField["updateTime"] + ",value=" + myField["value"] + "cannot convert, reason=" + ex.ToString());
                    }
                    item.entityName = myField["name"].ToString();
                    item.locationKey = myField["locationkey"].ToString();
                    item.name = item.entityName.Split('.')[3];               //if entityName=DT08.DNG.PLC03.DSS03.diiDSS-Pump2StopRun, name=DSS03
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
            string sqlstr = "delete from PUMP_STATE_CHANGE where to_char(updatetime, 'dd-mm-yyyy') <> to_char(current_date, 'dd-mm-yyyy')";
            dbServerTransact.ExecuteNonQuery(sqlstr);
        }

        public List<string> GetAllPumpEntityKeyListFromDb()
        {
            string sqlstr = "select pkey from entity where name like '%DNG%diiDS%-Pump%StopRun' and deleted=0";
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
