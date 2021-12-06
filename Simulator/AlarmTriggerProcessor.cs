using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AlarmSimulator
{
    public class AlarmTriggerProcessor
    {
        string[] descriptions = 
        {
"RTU DT17_RTU1, Service Port Number 4011 ModbusException",
"Plan agent error has occurred while running plan job with ID f3d6e11f-acd0-49ad-a791-26e3b558a44d",
"22kV SWITCHGEAR - RECTIFIER TRANSFORMER FEEDER CUBICLE HR Overcurrent Trip Signal (50): 22kV Switch RM 1",
"GroupCommunicationsFailure- System Controller Group DT17 has failed",
"ProcessFailure- Dt17EventMonitoredProcess on dt17-sms-02 has failed: Fatal Error",
"750VDC SWITCHGEAR - TIE FEEDER CIRCUIT BREAKER CB Position: Tie Breaker RM",
"22kV SWITCHGEAR - SERVICE TRANSFORMER (1 MVA) FEEDER CUBICLE CB Position: 22kV Switch RM 1",
"22 KV SWITCHGEAR PANEL INTERMEDIATE LOOP FEEDER CUBICLE (DP BREAKERS) DS/ES Position: 22kV Switch RM 1",
"22kV SWITCHGEAR -MAIN LOOP FEEDER CUBICLE CB Position: 22kV Switch RM 1",
"ATS Agent - Failed to send data to TWP",
"FailedToExecutePlan- Alarm Agent failed to execute a plan with Plan Id - /DSS/New Plan (2)",
"SEW EJECTOR PUMP 01 Ejector Tank Level: EJECTOR PUMP RM 01",
"FIRE SHUTTER 01 Position: CONC UNPAID AREA",
"INTAKE TRANSFORMER Intake Tranformer Mode: 22kV Switch RM 1A",
"Fire alarm triggered.  Propose emergency modes",
"Wheel fault detected. Train=08 Axle=02 Wheel=05 Timestamp=18/4/2019 10:35:32",
"Unable to establish communications with train 8/8 at location TKK",
"UPS Summary Alarm: UPS RM",
"UPS Low Battery Voltage Alarm: UPS RM",
"MSB Battery Charger DC Power Summary Alarm: LV Switch RM 1",
"SEW EJECTOR PUMP 01 Ejector Pump 01&02 Trip Summary Status: EJECTOR PUMP RM 01",
"TUNNEL SUMP 01 Pit Low Level Alarm: TUNNEL XB CH37+030.00",
"MSB Battery Charger DC Power Summary Alarm: LV Switch RM 1",
"TUNNEL SUMP 01 Pit Low Level Alarm: TUNNEL XB CH37+030.00"
        };
        Random random = new Random();
        List<string> descriptionList = new List<string>();
        List<XmlElement> alarmList = new List<XmlElement>();       //used to keep all alarms created

        public AlarmTriggerProcessor()
        {
            InitDescriptionList();
        }

        private void InitDescriptionList()
        {
            /*descriptionList.Add("Main Fire Alarm Panel MAP Summary Fault");
            descriptionList.Add("Common failure in both bearing sensors in FBSS(ATC Reset)");
            descriptionList.Add("A train movement detected in AM or CM FBSS with PSD open(NR)");
            descriptionList.Add("CBTC or FBSS signaling activation by ATC");
            descriptionList.Add("Status of Activation of FBSS by ATC");
            descriptionList.Add("Signaling changed to FBSS");
            descriptionList.Add("Train Stalled detected by ATP due to no FBSS and CBTC");
            descriptionList.Add("11kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading");
            descriptionList.Add("22kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading");
            descriptionList.Add("33kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading");*/
            descriptionList = descriptions.ToList();
        }

        //create single alarm
        int isClose = 1;
        public XmlElement CreateAlarmElement(XmlDocument xmlDoc, int isAtsAlarm, out string alarmID1, out string alarmDescription, bool closePrevious = true)
        {
            //int isClose = random.Next(0, 100) < 40 ? 0 : 1;
            isClose = 1 - isClose;
            if (closePrevious == true && isClose == 0 && alarmList.Count >= 1)
            {   //close previous alarm
                int index = random.Next(0, alarmList.Count);
                XmlElement oldAlarm = alarmList[index];
                XmlElement closedAlarm = CloseAlarmElement(xmlDoc, oldAlarm);
                alarmID1 = GetXmlElementValue(oldAlarm, "alarmID");
                alarmDescription = GetXmlElementValue(oldAlarm, "alarmDescription");
                return closedAlarm;
            }

            int sequence = random.Next(1, 100);
            string alarmID = Guid.NewGuid().ToString();
            //string alarmID = random.Next(1, 30).ToString();
            //string alarmID = "2";
            int timeStamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            XmlNode root = xmlDoc.DocumentElement;
            XmlElement elAlarm = xmlDoc.CreateElement("alarm");
            string[] excludelocationIDs = { "DT04", "DT05", "DT13", "DT20", "DT22", "DT23", "DT24", "DT25", "DT26" };
            string locationKey = GetRandomLocationKey(isAtsAlarm, excludelocationIDs);
            string locationName = GetLocationNameByPkey(locationKey);
            string[] IscsSystemNames = { "MMS", "Comms", "SCADA", "SMC - Public Light", "SMC - Shutter", "SMC - Lift"};
            string systemID = GetRandomSystemID(isAtsAlarm, IscsSystemNames);
            List<SystemInfo> systemList = null;
            if (isAtsAlarm == 1)
                systemList = AtsCachedMap.Instance.systemList;
            else
                systemList = IscsCachedMap.Instance.systemList;
            SystemInfo systemInfo = systemList.Where(p => p.ID == systemID).FirstOrDefault();
            string subsystemID = GetRandomSubsystemID(systemID, isAtsAlarm);
            Subsystem subsystem = systemInfo.subsystemList.Where(p => p.ID == subsystemID).FirstOrDefault();
            string severityID = GetRandomSeverityID(isAtsAlarm);
            string description = locationName + " (" + subsystem.Name + ", SEV" + severityID + ") " + GetRandomDescription();
            //string description = GetRandomDescription();
            string value = GetRandowmAlarmValue();
            if(isAtsAlarm == 1)
            {
                SeverityMapping severityMapping = GetRandowmAlarmSeverityMapping();
                systemInfo = systemList.Where(p => p.ShortName == severityMapping.systemName).FirstOrDefault();
                subsystem = systemInfo.subsystemList.Where(p => p.Name == severityMapping.subsystemName).FirstOrDefault();
                value = severityMapping.alarmValue;
                description = severityMapping.alarmDescription;
            }
            {   //alarmID
                XmlElement item = xmlDoc.CreateElement("alarmID");
                item.InnerText = alarmID;
                elAlarm.AppendChild(item);
            }
            {   //sourceTime
                XmlElement item = xmlDoc.CreateElement("sourceTime");
                XmlElement item1 = xmlDoc.CreateElement("time");
                item1.InnerText = timeStamp.ToString();
                item.AppendChild(item1);
                XmlElement item2 = xmlDoc.CreateElement("mili");
                item2.InnerText = "0";
                item.AppendChild(item2);
                elAlarm.AppendChild(item);
            }
            {   //ackTime
                XmlElement item = xmlDoc.CreateElement("ackTime");
                item.InnerText = random.Next(0, 3).ToString();
                elAlarm.AppendChild(item);
            }
            {   //closeTime
                XmlElement item = xmlDoc.CreateElement("closeTime");
                item.InnerText = "0";
                elAlarm.AppendChild(item);
            }
            {   //AssetName
                XmlElement item = xmlDoc.CreateElement("assetName");
                item.InnerText = "AssetName- " + sequence;
                elAlarm.AppendChild(item);
            }
            {   //severity
                XmlElement item = xmlDoc.CreateElement("alarmSeverity");
                item.InnerText = severityID;
                elAlarm.AppendChild(item);
            }
            {   //alarmDescription
                XmlElement item = xmlDoc.CreateElement("alarmDescription");
                item.InnerText = description;
                elAlarm.AppendChild(item);
            }
            {   //alarmAcknowledgeBy
                XmlElement item = xmlDoc.CreateElement("alarmAcknowledgeBy");
                elAlarm.AppendChild(item);
            }
            {   //state
                XmlElement item = xmlDoc.CreateElement("state");
                item.InnerText = isClose.ToString();
                elAlarm.AppendChild(item);
            }
            {   //locationId
                XmlElement item = xmlDoc.CreateElement("locationId");
                item.InnerText = locationName;          //AtsAlarm not use this field, only iscsAlarm use
                elAlarm.AppendChild(item);
            }
            {   //parentAlarmID
                XmlElement item = xmlDoc.CreateElement("parentAlarmID");
                elAlarm.AppendChild(item);
            }
            {   //avalancheHeadID
                XmlElement item = xmlDoc.CreateElement("avalancheHeadID");
                elAlarm.AppendChild(item);
            }
            {   //isHeadOfAvalanche
                XmlElement item = xmlDoc.CreateElement("isHeadOfAvalanche");
                item.InnerText = "1";
                elAlarm.AppendChild(item);
            }
            {   //isChildOfAvalanche
                XmlElement item = xmlDoc.CreateElement("isChildOfAvalanche");
                item.InnerText = "1";
                elAlarm.AppendChild(item);
            }
            {   //mmsState
                XmlElement item = xmlDoc.CreateElement("mmsState");
                item.InnerText = "0";
                elAlarm.AppendChild(item);
            }
            {   //alarmValue
                XmlElement item = xmlDoc.CreateElement("alarmValue");
                item.InnerText = value;
                elAlarm.AppendChild(item);
            }
            {   //omAlarm
                XmlElement item = xmlDoc.CreateElement("omAlarm");
                item.InnerText = "79";
                elAlarm.AppendChild(item);
            }
            {   //alarmType
                XmlElement item = xmlDoc.CreateElement("alarmType");
                item.InnerText = "-842150451";
                elAlarm.AppendChild(item);
            }
            {   //subsystemkey
                XmlElement item = xmlDoc.CreateElement("subsystemkey");
                item.InnerText = subsystemID;         //alarmstore cannnot provide subsystemekey and systemkey
                elAlarm.AppendChild(item);
            }
            {   //systemkey
                XmlElement item = xmlDoc.CreateElement("systemkey");
                item.InnerText = systemID;
                elAlarm.AppendChild(item);
            }
            {   //alarmComments
                XmlElement item = xmlDoc.CreateElement("alarmComments");
                elAlarm.AppendChild(item);
            }
            {   //strAlarmType
                XmlElement item = xmlDoc.CreateElement("strAlarmType");
                item.InnerText = "AlarmType- 5";
                elAlarm.AppendChild(item);
            }
            {   //subsytemType
                XmlElement item = xmlDoc.CreateElement("subsystemType");
                //item.InnerText = "SystemName- 5- 2";
                item.InnerText = subsystem.Name;            //AtsAlarm use subsystemType as subsystem name
                elAlarm.AppendChild(item);
            }
            {   //systemkeyType
                XmlElement item = xmlDoc.CreateElement("systemkeyType");
                //item.InnerText = "SystemName- 5";
                item.InnerText = systemInfo.ShortName;      //AtsAlarm use systemType as system name
                elAlarm.AppendChild(item);
            }
            {   //locationKey
                XmlElement item = xmlDoc.CreateElement("locationKey");
                item.InnerText = locationKey;               //AtsAlarm not use this field, only iscsAlarm use
                elAlarm.AppendChild(item);
            }
            {   //IsAtsAlarm
                XmlElement item = xmlDoc.CreateElement("IsAtsAlarm");
                item.InnerText = isAtsAlarm.ToString();
                elAlarm.AppendChild(item);
            }

            /*ListViewItem lstItem = new ListViewItem(new string[] { dtTime.ToString("yyyy-MM-dd HH:mm:ss"), description });
            listView1.Items.Insert(0, lstItem);
            listView1.Update();*/
            alarmID1 = alarmID;
            alarmDescription = description;
            alarmList.Add(elAlarm);

            return elAlarm;
        }

        public XmlElement CloseAlarmElement(XmlDocument xmlDoc, XmlElement el)
        {
            XmlNode node = el as XmlNode;
            //XmlNode nodeNew = node.Clone();
            XmlNode nodeNew = null;
            try
            {
                nodeNew = xmlDoc.ImportNode(node, true);
            }
            catch (Exception ex)
            {
                nodeNew = node.Clone();
            }
            {
                XmlNode node1 = nodeNew.SelectSingleNode("closeTime");
                //XmlElement el1 = el.GetElementsByTagName("closeTime");
                node1.InnerText = ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
            }
            {
                XmlNode node1 = nodeNew.SelectSingleNode("state");
                node1.InnerText = "0";
            }
            return nodeNew as XmlElement;
        }

        public string GetXmlElementValue(XmlElement el, string childName)
        {
            XmlNode node = el as XmlNode;
            XmlNode node1 = node.SelectSingleNode(childName);
            return node1.InnerText;
        }

        public void SetXmlElementValue(XmlElement el, string childName, string value)
        {
            XmlNode node = el as XmlNode;
            XmlNode node1 = node.SelectSingleNode(childName);
            node1.InnerText = value;
        }

        //convert Unix epoch time to DateTime
        private DateTime UnixTimeToDateTime(int unixTimeInSecond)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeInSecond);
            return dtDateTime;
        }

        private string GetRandomSeverityID(int isAtsAlarm = 1)
        {
            List<Severity> severityList = null;
            if (isAtsAlarm == 1)
                severityList = AtsCachedMap.Instance.severityList;
            else
                severityList = IscsCachedMap.Instance.severityList;
            int index = random.Next(0, severityList.Count());
            return severityList[index].ID;
        }

        private string GetRandomSystemID(int isAtsAlarm = 1, string[] IscsSystemNames = null)
        {
            List<SystemInfo> systemList = null;
            int index = 0;
            string systemID = "";
            if (isAtsAlarm == 1)
            {
                systemList = AtsCachedMap.Instance.systemList;
                index = random.Next(0, systemList.Count());
                systemID = systemList[index].ID;
            }
            else
            {
                systemList = IscsCachedMap.Instance.systemList;
                int i = random.Next(0, IscsSystemNames.Count());
                string systemName = IscsSystemNames[i];
                systemID = systemList.Where(p => p.Name == systemName).FirstOrDefault().ID;
            }

            return systemID;
        }

        private string GetRandomSubsystemID(string systemID, int isAtsAlarm = 1)
        {
            List<SystemInfo> systemList = null;
            if (isAtsAlarm == 1)
                systemList = AtsCachedMap.Instance.systemList;
            else
                systemList = IscsCachedMap.Instance.systemList;
            List<Subsystem> subsystemList = systemList.Where(p => p.ID == systemID).FirstOrDefault().subsystemList;
            int index = random.Next(0, subsystemList.Count());
            return subsystemList[index].ID;
        }

        //only Iscs have location requirement
        private string GetRandomLocationKey(int isAtsAlarm = 1, string[] excludelocationIDs = null)
        {
            List<Location> locationList = IscsCachedMap.Instance.locationList.Where(p => excludelocationIDs.Contains(p.Name) == false).ToList();
            int index = random.Next(0, locationList.Count());
            return locationList[index].ID;
        }

        private string GetLocationNameByPkey(string locationKey, int isAtsAlarm = 1)
        {
            return IscsCachedMap.Instance.locationList.Where(p => p.ID == locationKey).FirstOrDefault().Name;
        }

        //only Iscs have location requirement
        private string GetRandomDescription()
        {
            List<string> tempList = descriptionList.Concat(AtsCachedMap.Instance.alarmDescriptionList).ToList();
            int index = random.Next(0, descriptionList.Count());
            return descriptionList[index];
        }

        private string GetRandowmAlarmValue()
        {
            List<string> alarmValueList = AtsCachedMap.Instance.alarmValueList;
            int index = random.Next(0, alarmValueList.Count());
            return alarmValueList[index];
        }

        private SeverityMapping GetRandowmAlarmSeverityMapping()
        {
            var tempList = AtsCachedMap.Instance.severityMappingList;
            //var tempList = AtsCachedMap.Instance.severityMappingList.Where(p => p.alarmSeverity == "Alert").ToList();
            int index = random.Next(0, tempList.Count());
            return tempList[index];
        }
    }
}
