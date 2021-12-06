using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Data;

namespace AlarmSimulator
{
    public class PumpTriggerProcessor
    {
        List<string> pumpPkeyList = null;           //for TRANSACT DB
        Timer timerPoll;
        Random random = new Random();
        public bool bTriggerToTransactDB = true;

        List<Pump> pumpList = null;

        public PumpTriggerProcessor()
        {
            try
            {
                pumpList = DAIHelper.Instance.GetAllPumpFromDb();
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error(ex.ToString());
            }
        }

        public void Start(int pollInterval)
        {
            timerPoll = new Timer(timerPoll_Tick, null, 0, pollInterval);
        }

        public void Stop()
        {
            timerPoll.Dispose();
        }

        DateTime previousTime = DateTime.Now.Date;
        private void timerPoll_Tick(object sender)
        {
            DebugUtil.Instance.LOG.Info("timerPoll_Tick...");
            if (bTriggerToTransactDB == true)
            {
                if (pumpPkeyList == null)
                {
                    try
                    {
                        pumpPkeyList = DAIHelper.Instance.GetAllPumpEntityKeyListFromDb(" and locationkey=50 and name like '%DSS01%'");
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error(ex.ToString());
                    }
                }
                TriggerPumpChangeToTransactDB();
            }
            else
            {
                TriggerPumpChangeToSqlliteDB();
            }
        }

        private void TriggerPumpChangeToTransactDB()
        {
            int index = random.Next(0, pumpPkeyList.Count);
            DateTime updateTime = previousTime + new TimeSpan(0, 0, random.Next(5, 20));
            int value = random.Next(0, 2);
            string Pkey = pumpPkeyList[index];

            DataTable dtContent = new DataTable();
            dtContent.Columns.Add("entitykey");
            Type type = updateTime.GetType();
            dtContent.Columns.Add("updatetime", type);
            dtContent.Columns.Add("value");
            DataRow row = dtContent.NewRow();
            row["entitykey"] = Pkey;
            //row["entitykey"] = "10012714";
            row["updatetime"] = updateTime.ToString("dd/MM/yyyy HH:mm:ss");
            row["value"] = (value == 0 ? false : true);
            dtContent.Rows.Add(row);
            string sqlstr = DAIHelper.Instance.DbServerTransact.CreateInsertStatement("PUMP_STATE_CHANGE", dtContent)[0];
            DAIHelper.Instance.DbServerTransact.ExecuteNonQuery(sqlstr);
        }

        private void TriggerPumpChangeToSqlliteDB()
        {
            int index = random.Next(0, pumpList.Count);
            Pump pump = pumpList[index];
            pumpList[index].volume += random.Next(1, 100) / 10.0d;
            DAIHelper.Instance.SetPumpVolume(pump.volume, DateTime.Now, pump.name, pump.locationName);
        }
    }
}
