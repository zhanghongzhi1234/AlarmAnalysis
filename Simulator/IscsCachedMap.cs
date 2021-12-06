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
    public sealed class IscsCachedMap
    {
        private static volatile IscsCachedMap instance;        //singleton
        private static object syncRoot = new Object();

        string runParamfile = "config.ini";
        private Dictionary<string, string> runParams = new Dictionary<string, string>();

        string configFile = "IscsAlarmAnalysis.config.xml";
        public List<SystemInfo> systemList = new List<SystemInfo>();
        public List<Subsystem> allSubsystemList = new List<Subsystem>();      //all subsystem list
        public List<Severity> severityList = new List<Severity>();
        public List<Location> locationList = new List<Location>();
        public int pollInterval = 3000;            //Polling Interval for ATS Alarm Files in Seconds, config in config.xml
        public string AlarmFilePath = "";
        public string AlarmDatabase = "";
        public int archiveKeepDays = 60;            //Archive keep days in db in Seconds, config in config.xml
        public int alarmThreshold = 0;
        public int blinkInterval = 500;            //Polling Interval for ATS Alarm Files in millisecond
        public Dictionary<string, string> TransactDBConfig = new Dictionary<string, string>();
        public bool LocalTest = false;

        private IscsCachedMap()
        {
            ParseCommandLine();
            ReadRunParamFile();
            ReadConfigFile();
        }

        public static IscsCachedMap Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new IscsCachedMap();
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
            if (rech.Count() != 0)
            {
                foreach (XElement el in rech)
                {
                    SystemInfo systemInfo = new SystemInfo();
                    systemInfo.ID = el.Attribute("ID").Value;
                    systemInfo.Name = el.Attribute("Name").Value;
                    systemInfo.subsystemList = new List<Subsystem>();
                    if (el.Elements().Count() > 0)
                    {
                        foreach (XElement el1 in el.Elements())
                        {
                            Subsystem subsystem = new Subsystem();
                            subsystem.ID = el1.Attribute("ID").Value;
                            subsystem.Name = el1.Attribute("Name").Value;
                            if (el1.Attribute("Color") != null)
                            {
                                string colorValue = el1.Attribute("Color").Value;
                                subsystem.brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(el1.Attribute("Color").Value));
                            }
                            systemInfo.subsystemList.Add(subsystem);
                            allSubsystemList.Add(subsystem);
                        }
                    }
                    else
                    {   //if not have subsystem, add itself as a subsystem
                        Subsystem subsystem = new Subsystem();
                        subsystem.ID = systemInfo.ID;
                        subsystem.Name = systemInfo.Name;
                        systemInfo.subsystemList.Add(subsystem);
                        allSubsystemList.Add(subsystem);
                    }
                    systemList.Add(systemInfo);
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
            //Parse location
            rech = doc.Root.Descendants("Location");
            List<Location> tempList = new List<Location>();
            foreach (XElement el in rech)
            {
                Location location = new Location();
                location.ID = el.Attribute("ID").Value;
                location.Name = el.Attribute("Name").Value;
                location.TypeName = el.Attribute("TypeName").Value;
                if (el.Attribute("Operational") != null)
                    location.Operational = el.Attribute("Operational").Value;
                tempList.Add(location);
            }
            locationList.AddRange(tempList.Where(p => p.TypeName == "Occ"));
            locationList.AddRange(tempList.Where(p => p.TypeName == "Depot"));
            locationList.AddRange(tempList.Where(p => p.TypeName == "Station"));
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
            {   //alarm db path
                XElement elPath = doc.Root.Element("AlarmDatabase");
                AlarmDatabase = elPath.Attribute("Value").Value;
                DebugUtil.Instance.LOG.Info("AlarmDatabase=" + AlarmDatabase);
                if (!File.Exists(AlarmDatabase))
                {
                    Console.WriteLine("Alarm database not exist!");
                    DebugUtil.Instance.LOG.Error("Alarm database not exist");
                }
            }
            {   //archive keep days in db
                XElement xElement = doc.Root.Element("ArchiveKeepDays");
                string strValue = xElement.Attribute("Value").Value;
                try
                {
                    archiveKeepDays = Convert.ToInt32(strValue);
                    if (pollInterval <= 0)
                        throw new Exception();
                }
                catch
                {
                    MessageBox.Show("Archive keep days in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Error("Archive keep days in config file should be an integer greater than 0");
                }
            }
            {   //alarm threshold
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
                }
                catch
                {
                    MessageBox.Show("Blink interval in config file should be an integer greater than 0");
                    DebugUtil.Instance.LOG.Error("Blink interval in config file should be an integer greater than 0");
                }
            }
            {   //TransactDbConfig
                XElement xElement = doc.Root.Element("TransactDBConfig");
                string DbType = xElement.Attribute("DbType").Value;
                string TNSName = xElement.Attribute("TNSName").Value;
                string Username = xElement.Attribute("Username").Value;
                string Password = xElement.Attribute("Password").Value;
                DebugUtil.Instance.LOG.Info("TransactDBConfig=" + xElement.ToString());     //String.Concat(element.Nodes())
                TransactDBConfig["DbType"] = DbType;
                TransactDBConfig["TNSName"] = TNSName;
                TransactDBConfig["Username"] = Username;
                TransactDBConfig["Password"] = Password;
            }
            {   //localtest mode
                XElement xElement = doc.Root.Element("LocalTest");
                if (xElement != null && xElement.Attribute("Value") != null)
                {
                    string value = xElement.Attribute("Value").Value;
                    try
                    {
                        LocalTest = Convert.ToBoolean(value);
                        DebugUtil.Instance.LOG.Info("LocalTest" + LocalTest.ToString());
                    }
                    catch
                    {
                        DebugUtil.Instance.LOG.Info("LocalTest = false");
                    }
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
    }
}
