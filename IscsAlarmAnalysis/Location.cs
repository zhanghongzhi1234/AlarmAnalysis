using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TemplateProject
{
    public class Location
    {
        public string ID;
        public string Name;
        public string TypeName;             //Occ, Depot or Station
        public string Operational = "1";
        public double PumpThreshold = 0;
    }
}
