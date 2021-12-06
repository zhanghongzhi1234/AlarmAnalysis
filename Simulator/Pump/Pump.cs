using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmSimulator
{
    public class Pump
    {
        public string name;
        public string locationKey;
        public string locationName;
        public double flowRate;
        public double runningTimeInSeconds = 0;
        public double volume = 0;           //Liter, all volume need be reset to 0 when a new day start
        public DateTime updateTime;         //volume last update time
    }
}
