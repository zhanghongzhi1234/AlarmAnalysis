using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmAnalysisService
{
    public class PumpProcessor
    {
        Timer timerPoll;
        int pollInterval = CachedMap.Instance.pollInterval;
        string AlarmFilePath = CachedMap.Instance.AlarmFilePath;
        int archiveKeepDays = CachedMap.Instance.archiveKeepDays;
        int TableOptimizationInterval = CachedMap.Instance.TableOptimizationInterval;
        List<Pump> pumpList = null;

        public PumpProcessor()
        {
        }

        public void Start()
        {
            pumpList = DAIHelper.Instance.GetAllPumpFromDb();           //get all record from SQLite db
            if (DAIHelper.Instance.DbServerTransact == null)
            {
                DebugUtil.Instance.LOG.Error("Cannot connect to TRANSACT db, PumpProcessor won't start");
                return;
            }
            DAIHelper.Instance.DeletePumpStateChangeBeforeToday();      //delete all pump state change before today from TRANSACT db
            timerPoll = new Timer(timerPoll_Tick, null, 0, pollInterval);
        }

        public void Stop()
        {
            if(timerPoll != null)
                timerPoll.Dispose();
            DebugUtil.Instance.LOG.Info("Pump processor stopped.");
        }

        private DateTime previousDay = DateTime.Now.Date;
        //private int lastClearCounter = 0;
        //private int currentClearCounter = 0;
        private void timerPoll_Tick(object sender)
        {
            DebugUtil.Instance.LOG.Info("Pump timerPoll_Tick...");
            if (DateTime.Now.Date != previousDay)
            {
                DAIHelper.Instance.ResetAllPumpVolume(0);     //when a new day start, reset all pump to 0
                foreach (Pump pump in pumpList)
                {
                    pump.volume = 0;
                    foreach (PumpEntity pumpEntity in pump.pumpEntityList)
                    {
                        pumpEntity.runningTimeInSeconds = 0;
                        pumpEntity.volume = 0;
                    }
                }
                previousDay = DateTime.Now.Date;
                DebugUtil.Instance.LOG.Debug("New day start, reset all pump volume to 0");
            }

            /*{
                if (currentClearCounter - lastClearCounter > TableOptimizationInterval)
                {
                    //delete all record in alarm_new table, this table work as register to improve performance, content is duplicate with alarm but only keep latest record
                    DAIHelper.Instance.DbServer.ExecuteNonQuery("DELETE FROM Alarm_New;");
                    lastClearCounter = 0;
                    currentClearCounter = 0;
                }
                currentClearCounter += pollInterval;
                DebugUtil.Instance.LOG.Debug("clear alarm_new table for optimization");
            }*/

            //first move table Alarm_New all item to table Alarm
            //DAIHelper.Instance.DbServer.ExecuteNonQuery("INSERT INTO Alarm SELECT * FROM Alarm_New;");

            {   //Update TRANSACT pump state change to SQLITE pump table
                List<PumpStateChange> pumpStateChangeList = DAIHelper.Instance.GetTodayPumpStateChangeFromDb();
                if (pumpStateChangeList != null && pumpStateChangeList.Count >= 1)
                {
                    UpdatePumpByStateChange(pumpStateChangeList);
                    DAIHelper.Instance.DeleteAllPumpStateChange();
                }
            }
        }

        private void UpdatePumpByStateChange(List<PumpStateChange> pumpStateChangeList)
        {
            if (pumpStateChangeList == null || pumpStateChangeList.Count == 0)
                return;

            IEnumerable<string> changedPumps = pumpStateChangeList.Select(p => p.entityKey + "," + p.locationKey + "," + p.name).Distinct();
            foreach (string pumpName in changedPumps)
            {
                string[] temp = pumpName.Split(',');
                string entityKey = temp[0];
                string locationKey = temp[1];
                string name = temp[2];
                Pump pump = pumpList.Where(p => p.name == name && p.locationKey == locationKey).FirstOrDefault();        //should be locationName
                if (pump == null)
                {
                    DebugUtil.Instance.LOG.Error("Config Error, Entity " + entityKey + ", locationKey=" + locationKey + ", name=" + name + " cannot be found in SQLite Pump table!");
                    continue;
                }
                PumpEntity pumpEntity = pump.pumpEntityList.Where(p => p.entityKey == entityKey).FirstOrDefault();      //same locationname and same name may have multiple pump
                if (pumpEntity == null)
                {
                    pumpEntity = new PumpEntity();
                    pumpEntity.entityKey = entityKey;
                    pumpEntity.updateTime = DateTime.Now;
                    DebugUtil.Instance.LOG.Debug("Created Entity " + entityKey + ", locationKey=" + locationKey + ", name=" + name);
                    pump.pumpEntityList.Add(pumpEntity);
                }
                IEnumerable<PumpStateChange> pumpStateChanges = pumpStateChangeList.Where(p => p.entityKey == entityKey).OrderBy(p => p.updateTime);
                bool bChanged = false;
                foreach (PumpStateChange state in pumpStateChanges)
                {   //if state is same with lastState, ignore it
                    if (pumpEntity.value == false && state.value == true)
                    {
                        pumpEntity.value = state.value;          //pump start again
                        pumpEntity.updateTime = state.updateTime;
                    }
                    else if (pumpEntity.value == true && state.value == false)
                    {
                        double newRunningSeconds = (state.updateTime - pumpEntity.updateTime).TotalSeconds;
                        DebugUtil.Instance.LOG.Debug("Pump(" + entityKey + "-" + pump.locationName + " - " + pump.name + ") newRunningSeconds = " + newRunningSeconds + ", flowRate=" + pump.flowRate);
                        pumpEntity.runningTimeInSeconds += newRunningSeconds;
                        double newVolume = newRunningSeconds * pump.flowRate;
                        pumpEntity.volume += newVolume;
                        pumpEntity.value = state.value;
                        pumpEntity.updateTime = state.updateTime;

                        pump.volume += newVolume;
                        pump.updateTime = state.updateTime;
                        bChanged = true;
                    }
                }
                if (bChanged)
                {
                    DebugUtil.Instance.LOG.Debug("Set Pump(" + pump.locationName + " - " + pump.name + ") new volume = " + pump.volume);
                    DAIHelper.Instance.SetPumpVolume(pump.volume, pump.updateTime, pump.name, pump.locationName);
                }
            }

            /*foreach (Pump pump in pumpList)
            {
                List<PumpStateChange> listTemp = pumpStateChangeList.Where(p => p.name == pump.name && p.locationKey == pump.locationKey).OrderBy(p => p.updateTime).ToList();
                if (listTemp.Count <= 1)
                    continue;
                bool bChanged = false;
                PumpStateChange lastState = listTemp.First();
                foreach (PumpStateChange state in listTemp)
                {   //if state is same with lastState, ignore it
                    if (lastState.value == false && state.value == true)
                    {
                        lastState = state;          //pump start again
                    }
                    else if (lastState.value == true && state.value == false)
                    {
                        double newRunningSeconds = (state.updateTime - lastState.updateTime).TotalSeconds;
                        pump.runningTimeInSeconds += newRunningSeconds;
                        pump.volume += newRunningSeconds * pump.flowRate;
                        pump.updateTime = state.updateTime;
                        bChanged = true;
                        lastState = state;
                    }
                }
                if (bChanged)
                {
                    DAIHelper.Instance.SetPumpVolume(pump.volume, pump.updateTime, pump.name, pump.locationName);
                }
            }*/
        }
    }
}
