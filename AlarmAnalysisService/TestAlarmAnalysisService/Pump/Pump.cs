using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmAnalysisService
{
    //This class is for SQLite table
    public class Pump
    {
        public string name;
        public string locationKey;
        public string locationName;
        public double flowRate;
        public double volume = 0;           //Liter, all volume need be reset to 0 when a new day start
        public DateTime updateTime;         //volume last update time
        public List<PumpEntity> pumpEntityList = new List<PumpEntity>();
    }

    public class PumpEntity
    {
        public string entityKey;
        public double runningTimeInSeconds = 0;
        public double volume = 0;           //Liter, all volume need be reset to 0 when a new day start
        public DateTime updateTime;         //volume last update time
        public bool value;                  //true: pump running, false: stop
    }
}
