using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmAnalysisService
{
    //This class is for Oracle Table
    public class PumpStateChange
    {
        public string entityKey;
        public DateTime updateTime;
        public bool value;                  //true: pump running, false: stop
        public string entityName;
        public string locationKey;
        public string name;                 //short time, like DSS01, DST02
    }
}
