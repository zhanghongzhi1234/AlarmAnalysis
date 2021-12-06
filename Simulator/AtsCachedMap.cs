using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace AlarmSimulator
{
    public sealed class AtsCachedMap
    {
        private static volatile AtsCachedMap instance;        //singleton
        private static object syncRoot = new Object();

        string runParamfile = "config.ini";
        private Dictionary<string, string> runParams = new Dictionary<string, string>();

        string configFile = "AtsAlarmAnalysis.config.xml";
        public List<SystemInfo> systemList = new List<SystemInfo>();
        public List<Subsystem> allSubsystemList = new List<Subsystem>();      //all subsystem list
        public List<Severity> severityList = new List<Severity>();
        public List<string> alarmValueList = new List<string>();
        public List<string> alarmDescriptionList = new List<string>();

        public int pollInterval = 3000;            //Polling Interval for ATS Alarm Files in Seconds, config in config.xml
        public string AlarmFilePath = "";
        public int archiveKeepDays = 60;            //Archive keep days in db in Seconds, config in config.xml
        public int alarmThreshold = 0;
        public int blinkInterval = 500;            //Polling Interval for ATS Alarm Files in millisecond

        string severityMappingFile = "AlarmSeverityMapping.config.csv";
        public List<SeverityMapping> severityMappingList = new List<SeverityMapping>();

        private AtsCachedMap()
        {
            ParseCommandLine();
            ReadRunParamFile();
            ReadConfigFile();
            ReadSeverityMappingFile();
        }

        public static AtsCachedMap Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new AtsCachedMap();
                    }
                }

                return instance;
            }
        }

        /** Add a parameter
		  * @param name Name of parameter
          * @param value Value of parameter
          * Pre: name and value are not NULL
		  */
        public void SetRunParam(string name, string value)
        {
            runParams[name] = value;
        }

        /**Retrieve a parameter value
          * @return Value of parameter
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public string GetRunParam(string name)
        {
            return runParams[name];
        }

        /** Determine whether a parameter with the given name has been set
          * @return True (parameter set), False (parameter not set)
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public bool isSetRunParam(string name)
        {
            bool isSet = runParams.ContainsKey(name);
            return isSet;
        }

        public void ParseCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                try
                {
                    int index = arg.IndexOf('=');
                    //string[] sArray = strReadLine.Split('=');
                    string name = arg.Substring(0, index);  //sArray[0];
                    string value = arg.Substring(index + 1, arg.Length - index - 1); //sArray[1];
                    runParams[name] = value;
                }
                catch (Exception)
                {
                }
            }
        }

        // Read start.ini config file to parse the db connection parameter
        public void ReadRunParamFile()
        {
            // 读取文件的源路径及其读取流
            string strReadFilePath = runParamfile;
            Dictionary<string, string> ret = new Dictionary<string, string>();
            if (File.Exists(strReadFilePath))
            {
                StreamReader srReadFile = new StreamReader(strReadFilePath);

                // 开始解析文件
                try
                {
                    while (!srReadFile.EndOfStream)
                    {
                        string strReadLine = srReadFile.ReadLine();
                        int index = strReadLine.IndexOf('=');
                        //string[] sArray = strReadLine.Split('=');
                        string name = strReadLine.Substring(0, index);  //sArray[0];
                        name = name.TrimStart('-');
                        string value = strReadLine.Substring(index + 1, strReadLine.Length - index - 1); //sArray[1];
                        runParams[name] = value;
                    }
                }
                catch (System.Exception e)
                {
                }
                // 关闭读取流文件
                srReadFile.Close();
            }
        }

        private void ReadConfigFile()
        {
            DebugUtil.Instance.LOG.Error("Read config.xml started");
            systemList.Clear();
            severityList.Clear();
            XDocument doc = XDocument.Load(configFile);
            //parse system/subsystem
            IEnumerable<XElement> rech = doc.Root.Descendants("System");
            int systemID = 1;
            int subsystemID = 1;
            if (rech.Count() != 0)
            {
                foreach (XElement el in rech)
                {
                    SystemInfo systemInfo = new SystemInfo();
                    //systemInfo.ID = el.Attribute("ID").Value;
                    systemInfo.ID = systemID.ToString();
                    systemInfo.Name = el.Attribute("Name").Value;
                    systemInfo.ShortName = el.Attribute("ShortName").Value;
                    systemInfo.Color = el.Attribute("Color").Value;
                    systemInfo.subsystemList = new List<Subsystem>();
                    float delta = 0;
                    int direction = 1;
                    foreach (XElement el1 in el.Elements())
                    {
                        Subsystem subsystem = new Subsystem();
                        //subsystem.ID = el1.Attribute("ID").Value;
                        subsystem.ID = subsystemID.ToString();
                        subsystem.Name = el1.Attribute("Name").Value;
                        float factor = delta * direction;
                        subsystem.brush = CommonFunction.ChangeBrushBrightness(new SolidColorBrush((Color)ColorConverter.ConvertFromString(systemInfo.Color)), factor);
                        systemInfo.subsystemList.Add(subsystem);
                        allSubsystemList.Add(subsystem);
                        subsystemID++;
                        direction = -direction;
                        if (direction < 0)
                            delta += 0.2f;
                    }

                    systemList.Add(systemInfo);
                    systemID++;
                }
            }
            //Parse severity
            rech = doc.Root.Descendants("Severity");
            foreach (XElement el in rech)
            {
                Severity severity = new Severity();
                severity.Name = el.Attribute("Name").Value;
                severity.ID = el.Attribute("ID").Value;
                severity.Color = el.Attribute("Color").Value;
                severityList.Add(severity);
            }
            {   //poll interval
                XElement xElement = doc.Root.Element("PollInterval");
                string strPollInterval = xElement.Attribute("Value").Value;
                try
                {
                    pollInterval = Convert.ToInt32(strPollInterval);
                    if (pollInterval <= 0)
                        throw new Exception();
                }
                catch
                {
                    MessageBox.Show("Poll Interval in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Error("Poll Interval in config file should be an integer greater than 0");
                }
            }
            {   //alarm file path
                XElement elPath = doc.Root.Element("AlarmFilePath");
                AlarmFilePath = elPath.Attribute("Value").Value;
                if (!Directory.Exists(AlarmFilePath))
                {
                    MessageBox.Show("Alarm file path not exist!");
                    DebugUtil.Instance.LOG.Error("Alarm file path not exist");
                }
            }
            {   //poll interval
                XElement xElement = doc.Root.Element("AlarmThreshold");
                string value = xElement.Attribute("Value").Value;
                try
                {
                    alarmThreshold = Convert.ToInt32(value);
                    if (alarmThreshold <= 0)
                        throw new Exception();
                }
                catch
                {
                    MessageBox.Show("Alarm threshold in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Error("Alarm threshold in config file should be an integer greater than 0");
                }
            }
            {   //blink interval
                XElement xElement = doc.Root.Element("BlinkInterval");
                string value = xElement.Attribute("Value").Value;
                try
                {
                    blinkInterval = Convert.ToInt32(value);
                    if (alarmThreshold <= 0)
                        throw new Exception();
                }
                catch
                {
                    MessageBox.Show("Blink interval in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Error("Blink interval in config file should be an integer greater than 0");
                }
            }
            DebugUtil.Instance.LOG.Info("Config file read completed");
        }

        public SystemInfo GetSystemByID(string ID)
        {
            SystemInfo ret = null;
            try
            {
                ret = systemList.Where(p => p.ID == ID).FirstOrDefault();
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find system ID: " + ID);
            }
            return ret;
        }

        public string GetSystemIDByShortName(string ShortName)
        {
            string ret = "";
            try
            {
                ret = systemList.Where(p => p.ShortName == ShortName).FirstOrDefault().ID;
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find system short name: " + ShortName);
            }
            return ret;
        }

        public string GetSubsystemIDByName(string subsystemName)
        {
            string ret = "";
            try
            {
                ret = allSubsystemList.Where(p => p.Name == subsystemName).FirstOrDefault().ID;
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find subsystem name: " + subsystemName);
            }
            return ret;
        }

        public string GetSubsystemNameByID(string subsystemID)
        {
            string ret = "";
            try
            {
                ret = allSubsystemList.Where(p => p.ID == subsystemID).FirstOrDefault().Name;
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find subsystem ID: " + subsystemID);
            }
            return ret;
        }

        // Read SeverityMapping file
        public void ReadSeverityMappingFile()
        {
            if (!File.Exists(severityMappingFile))
            {
                DebugUtil.Instance.LOG.Info(configFile + " do not exist");
                return;
            }
            // Read severity mapping file
            Console.WriteLine("severity mapping file read started");
            DebugUtil.Instance.LOG.Info("severity mapping file read started");
            string strReadFilePath = severityMappingFile;
            Dictionary<string, string> ret = new Dictionary<string, string>();
            if (File.Exists(strReadFilePath))
            {
                StreamReader srReadFile = new StreamReader(strReadFilePath);

                try
                {
                    while (!srReadFile.EndOfStream)
                    {
                        string strReadLine = srReadFile.ReadLine();
                        strReadLine = strReadLine.Trim().Replace("\"", "");
                        if (strReadLine.ToLower().StartsWith("system") || string.IsNullOrEmpty(strReadLine))
                        {   //header line or empty line, ignore it
                            continue;
                        }
                        string systemName = "";
                        string subsystemName = "";
                        string alarmValue = "";
                        string alarmDescription = "";
                        string alarmSeverity = "";

                        string[] sArray = strReadLine.Split(',');   //RSC,PIS,Summary Status of PEC,E
                        if (sArray.Count() < 5)
                        {
                            string strError = "Config file '" + severityMappingFile + "' line format error: " + strReadLine;
                            Console.WriteLine(strError);
                            DebugUtil.Instance.LOG.Error(strError);
                            continue;
                        }
                        else if (sArray.Count() == 5)
                        {
                            systemName = sArray[0].Trim();
                            subsystemName = sArray[1].Trim();
                            alarmValue = sArray[2].Trim();
                            alarmDescription = sArray[3].Trim();
                            alarmSeverity = sArray[4].Trim();
                        }
                        else
                        {   //description may contain comma
                            systemName = sArray[0].Trim();
                            subsystemName = sArray[1].Trim();
                            alarmValue = sArray[2].Trim();
                            alarmSeverity = sArray[sArray.Count() - 1].Trim();
                            alarmDescription = strReadLine.Substring(sArray[0].Length + sArray[1].Length + sArray[2].Length + 3, strReadLine.Length - sArray[0].Length - sArray[1].Length - sArray[2].Length - 5).Trim();     //sArray[1];
                        }
                        if (systemName != "" && subsystemName != "" && alarmDescription != "" && alarmSeverity != "")
                        {
                            if (alarmSeverity.ToLower() == "alert")
                                alarmSeverity = "Alert";
                            else if (alarmSeverity.ToLower() == "e")
                                alarmSeverity = "E";
                            else if (alarmSeverity != "1" && alarmSeverity != "2" && alarmSeverity != "3")
                            {   //if severity is not in one of above value, just record an error log
                                DebugUtil.Instance.LOG.Error("line '" + strReadLine + "' severity field is invalid");
                            }
                            SeverityMapping severityMapping = new SeverityMapping(systemName, subsystemName, alarmValue.Trim(), alarmDescription.Trim(), alarmSeverity);
                            severityMappingList.Add(severityMapping);

                            if (!alarmValueList.Contains(alarmValue))
                                alarmValueList.Add(alarmValue);

                            if (!alarmDescriptionList.Contains(alarmDescription))
                                alarmDescriptionList.Add(alarmDescription);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    DebugUtil.Instance.LOG.Error(ex.ToString());
                }
                // 关闭读取流文件
                srReadFile.Close();
            }
            Console.WriteLine("severity mapping file read completed");
            DebugUtil.Instance.LOG.Info("severity mapping file read completed");
        }
    }
}
