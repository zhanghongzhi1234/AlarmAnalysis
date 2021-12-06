using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public abstract class CachedMap
    {
        public string AlarmDatabase = "";
        public List<SystemInfo> systemList = new List<SystemInfo>();
        public List<Subsystem> allSubsystemList = new List<Subsystem>();      //all subsystem list
        public List<Severity> severityList = new List<Severity>();
        public List<Location> locationList = new List<Location>();
        public bool PopupFilter = true;

        /** Add a parameter
		  * @param name Name of parameter
          * @param value Value of parameter
          * Pre: name and value are not NULL
		  */
        public abstract void SetRunParam(string name, string value);

        /**Retrieve a parameter value
          * @return Value of parameter
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public abstract string GetRunParam(string name);

        /** Determine whether a parameter with the given name has been set
          * @return True (parameter set), False (parameter not set)
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public abstract bool isSetRunParam(string name);
    }
}
