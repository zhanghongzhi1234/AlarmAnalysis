//using MySql.Data.MySqlClient;
using Oracle.DataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Oracle and MySql can merge together, both use IDbConnection, IDataReader, IDbCommand, just differ with connection string
namespace AlarmSimulator
{
    public class OracleServer : DatabaseServer
    {
        string TNSName;
        string Username;
        string Password;
        private static OracleConnection conn;       //数据库连接

        public OracleServer(string TNSName, string Username, string Password)
        {
            this.TNSName = TNSName;
            this.serverType = ServerType.ORACLE;
            this.Username = Username;
            this.Password = Password;

            ConnectToDatabase(TNSName, Username, Password);
        }

        public override void Init()
        {
            ConnectToDatabase(TNSName, Username, Password);
        }

        public override List<RawTable> GetRawData(string name, bool exactMatch = false)
        {
            string sqlstr = "select XValue,YValue from " + name + " order by pkey asc";
            //log.Debug(sqlstr);
            List<RawTable> ret = null;
            DataTable schemaTable = ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<RawTable>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    object XValue = myField["XValue"];
                    double YValue = Convert.ToDouble(myField["YValue"]);
                    RawTable item = new RawTable(XValue, YValue);
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

        private static void ConnectToDatabase(string TNSName, string UserName, string Password)
        {
            string connString = "Data Source=" + TNSName + "; User Id=" + UserName + "; Password=" + Password;
            conn = new OracleConnection(connString);
            conn.Open();
        }

        public override DataTable GetQueryData(string sqlstr)
        {
            return ExecuteQuery(sqlstr);
        }

        //query by sql
        public override DataTable ExecuteQuery(string sqlstr)
        {
            DataTable table = new DataTable();
            table.Clear();
            OracleDataReader reader = null;
            //log.Debug(sqlstr);
            OracleCommand cmd = new OracleCommand(sqlstr, conn);
            try
            {
                reader = cmd.ExecuteReader();
                //DataTable schemaTable = reader.GetSchemaTable();
                //var columnNames = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    //columnNames.Add(columnName);
                    table.Columns.Add(columnName);
                }
                while (reader.Read())
                {
                    DataRow newRow = table.NewRow();
                    //for (int i = 0; i < reader.FieldCount; i++)
                    int i = 0;
                    foreach (DataColumn column in table.Columns)
                    {
                        //newRow[column.ColumnName] = reader.GetString(i);
                        //newRow[column.ColumnName] = reader.GetInt32(i);
                        newRow[column.ColumnName] = reader[column.ColumnName];
                        i++;
                    }
                    table.Rows.Add(newRow);
                }
                //ret = reader.GetSchemaTable();
            }
            catch (OracleException ex)
            {
                //log.Error(ex.ToString());
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return table;
        }

        //执行sql语句，修改数据库, 为Insert,Update,Delete中的一种
        public override int ExecuteNonQuery(string sqlstr)
        {
            //log.Debug(sqlstr);
            OracleCommand cmd = new OracleCommand(sqlstr, conn);
            return cmd.ExecuteNonQuery();
        }

        public override bool SendData(string command)
        {
            return true;
        }

        public override void Close()
        {
        }

        public override List<string> CreateInsertStatement(string tableName, DataTable dtContent)
        {
            List<string> ret = new List<string>();
            string sqlInsertHeader = "Insert into " + tableName + "(";
            /*[alarmID],[sourceTime],[ackTime],[closeTime],[assetName],[alarmSeverity],[alarmDescription],"
                + "[alarmAcknowledgeBy],[state],[locationId],[parentAlarmID],[avalancheHeadID],[isHeadOfAvalanche],[isChildOfAvalanche],[mmsState],"
                + "[alarmValue],[omAlarm],[alarmType],[subsystemkey],[systemkey],[alarmComments],[strAlarmType],[subsystemType],[systemkeyType],"
                + "[locationKey]) values ";*/

            string[] columnNames = dtContent.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();

            for (int i = 0; i < columnNames.Count(); i++)
            {
                sqlInsertHeader += columnNames[i] + ",";
            }
            sqlInsertHeader = sqlInsertHeader.TrimEnd(',');
            sqlInsertHeader += ") values ";
            for (int i = 0; i < dtContent.Rows.Count; i++)
            {
                string sqlstr;
                string sqlInsertBody = "(";
                for (int j = 0; j < dtContent.Columns.Count; j++)
                {
                    DataColumn col = dtContent.Columns[j];
                    string value = dtContent.Rows[i][j].ToString();
                    if (col.DataType.Name == "DateTime")
                        value = "TO_DATE('" + value + "', 'dd/mm/yyyy hh24:mi:ss')";
                    else
                        value = "'" + value + "'";
                    sqlInsertBody += value + ",";
                }
                sqlInsertBody = sqlInsertBody.TrimEnd(',');
                sqlInsertBody += ")";
                sqlstr = sqlInsertHeader + sqlInsertBody;
                ret.Add(sqlstr);
            }

            return ret;
        }
    }
}
