using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmAnalysisService
{
    public class AtsAlarm
    {
        public static string[] columnNames = {"alarmID","sourceTime","ackTime","closeTime","assetName","alarmSeverity","alarmDescription",
                "alarmAcknowledgeBy","state","locationId","parentAlarmID","avalancheHeadID","isHeadOfAvalanche","isChildOfAvalanche","mmsState",
                "alarmValue","omAlarm","alarmType","subsystemkey","systemkey","alarmComments","strAlarmType","subsystemType","systemkeyType",
                "locationKey", "IsAtsAlarm"};
        /*public string alarmID;
        public DateTime sourceTime;
        public string systemID;
        public string subsystemID;
        public string severityID;
        public string Ack;      //1: ack, 2: non ack
        public string assetName;
        public string description;
        public string alarmValue;
        public string locationId;*/
        //public string systemkeyType;            //system name
        //public string subsystemType;            //subsystem name
    }

    public class AtsAlarmRawTable
    {
        public string alarmID;
        public string sourceTime;
        public string ackTime;
        public string closeTime;
        public string assetName;
        public string alarmSeverity;
        public string alarmDescription;
        public string alarmAcknowledgeBy;
        public string state;
        public string locationId;
        public string parentAlarmID;
        public string avalancheHeadID;
        public string isHeadOfAvalanche;
        public string isChildOfAvalanche;
        public string mmsState;
        public string alarmValue;
        public string omAlarm;
        public string alarmType;
        public string subsystemkey;
        public string systemkey;
        public string alarmComments;
        public string strAlarmType;
        public string subsystemType;
        public string systemkeyType;
        public string locationKey;
        public string IsAtsAlarm;
    }
}
