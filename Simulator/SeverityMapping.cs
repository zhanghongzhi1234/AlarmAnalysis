using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmSimulator
{
    public class SeverityMapping
    {
        public string systemName;              //systemkeyType field in raw table
        public string subsystemName;           //subsystemType field in raw table
        public string alarmDescription;
        public string alarmValue;
        public string alarmSeverity;           //new severity is 1,2,3,Alert,E, severity E will be ignored

        public SeverityMapping(string systemName, string subsystemName, string alarmValue, string alarmDescription, string alarmSeverity)
        {
            this.systemName = systemName;
            this.subsystemName = subsystemName;
            this.alarmValue = alarmValue;
            this.alarmDescription = alarmDescription;
            this.alarmSeverity = alarmSeverity;
        }
    }
}
