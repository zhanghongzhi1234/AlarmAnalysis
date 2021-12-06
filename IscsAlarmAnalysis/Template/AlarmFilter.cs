using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class AlarmFilter
    {
        public bool isEnabled = false;
        public static string[] ruleAts = { "alarmDescription", "alarmSeverity", "assetName" };
        public static string[] ruleIscs = { "alarmSeverity", "locationId", "alarmDescription" };

        public List<string> selectedSystems = new List<string>();
        public List<string> selectedSubsystems = new List<string>();
        public List<string> selectedLocations = new List<string>();
        public List<string> selectedSeveritys = new List<string>();
        public DateTime dtStart;
        public DateTime dtEnd;
        public List<string> selectedRules = new List<string>();
        public bool enableDateTime = false;
        public int IsAtsAlarm = 1;

        public AlarmFilter(int IsAtsAlarm = 1)
        {
            this.IsAtsAlarm = IsAtsAlarm;
            Reinitialize();
        }

        public void Reinitialize()
        {
            if (IsAtsAlarm == 0)
            {
                selectedRules = ruleIscs.ToList();
            }
            else
            {
                selectedRules = ruleAts.ToList();
            }
            selectedSystems.Clear();
            selectedSubsystems.Clear();
            selectedLocations.Clear();
            selectedSeveritys.Clear();

            selectedSystems.Add("All");
            selectedSubsystems.Add("All");
            selectedLocations.Add("All");
            selectedSeveritys.Add("All");
            dtStart = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            dtEnd = DateTime.Now.Date + new TimeSpan(1, 0, 0, 0);
        }
    }
}
