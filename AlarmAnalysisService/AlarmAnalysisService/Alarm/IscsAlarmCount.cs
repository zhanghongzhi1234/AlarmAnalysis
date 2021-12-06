using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class IscsAlarmCount
    {
        public static string[] columnNames = {"subsystemkey","locationId","dateCreated","count"};

        public string subsystemkey;
        public string locationId;
        public DateTime dateCreated;
        public int count;
    }
}
