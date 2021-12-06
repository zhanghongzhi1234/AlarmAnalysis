using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace TemplateProject
{
    public sealed class AtsCachedMap : CachedMap
    {
        private static volatile AtsCachedMap instance;        //singleton
        private static object syncRoot = new Object();

        string runParamfile = "config.ini";
        private Dictionary<string, string> runParams = new Dictionary<string, string>();
        
        string configFile = "AtsAlarmAnalysis.config.xml";
        public int pollInterval = 3000;            //Polling Interval for ATS Alarm Files in Seconds, config in config.xml
        public string AlarmFilePath = "";
        public int alarmThreshold = 0;
        public int blinkInterval = 500;            //Polling Interval for ATS Alarm Files in millisecond
        public bool LocalTest = false;
        public string AlarmResponseSheetPath = "";
        public string IscsAlarmAnalysisBinary = "";

        private AtsCachedMap()
        {
            try
            {
                ParseCommandLine();
                ReadRunParamFile();
                ReadConfigFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Config error: " + ex.ToString() + ", Program will end");
                DebugUtil.Instance.LOG.Error(ex.ToString());
                System.Environment.Exit(0);
            }
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
        public override void SetRunParam(string name, string value)
        {
            runParams[name] = value;
        }

        /**Retrieve a parameter value
          * @return Value of parameter
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public override string GetRunParam(string name)
        {
            return runParams[name];
        }

        /** Determine whether a parameter with the given name has been set
          * @return True (parameter set), False (parameter not set)
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public override bool isSetRunParam(string name)
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
                    float step = 1.0f / el.Elements().Count();          //dynamic calculate in case there are too many subsystems, because if factor > 1, color will change to opposite
                    int direction = 1;
                    foreach (XElement el1 in el.Elements())
                    {
                        Subsystem subsystem = new Subsystem();
                        //subsystem.ID = el1.Attribute("ID").Value;
                        subsystem.ID = subsystemID.ToString();
                        subsystem.systemID = systemInfo.ID;
                        subsystem.Name = el1.Attribute("Name").Value;
                        float factor = delta * direction;
                        subsystem.brush = CommonFunction.ChangeBrushBrightness(new SolidColorBrush((Color)ColorConverter.ConvertFromString(systemInfo.Color)), factor);
                        if (el1.Attribute("ResponseFile") != null)
                            subsystem.ResponseFile = el1.Attribute("ResponseFile").Value;
                        systemInfo.subsystemList.Add(subsystem);
                        allSubsystemList.Add(subsystem);
                        subsystemID++;
                        direction = -direction;
                        if (direction < 0)
                        {
                            //delta += 0.05f;		//make sure all subsystem have same color, only differ in brightness
                            delta += step;		//make sure all subsystem have same color, only differ in brightness
                        }
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
            {   //alarm response sheet path
                XElement elPath = doc.Root.Element("AlarmResponseSheetPath");
                AlarmResponseSheetPath = elPath.Attribute("Value").Value;
                if (!Directory.Exists(AlarmResponseSheetPath))
                {
                    MessageBox.Show("Alarm response sheet path not exist!");
                    DebugUtil.Instance.LOG.Error("Alarm response sheet path not exist");
                }
            }
            {   //alarm db path
                XElement elPath = doc.Root.Element("AlarmDatabase");
                AlarmDatabase = elPath.Attribute("Value").Value;
                if (!File.Exists(AlarmDatabase))
                {
                    Console.WriteLine("Alarm database not exist!");
                    DebugUtil.Instance.LOG.Error("Alarm database not exist");
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
            {   //pop up filter
                XElement xElement = doc.Root.Element("PopupFilter");
                if (xElement != null && xElement.Attribute("Value") != null)
                {
                    string value = xElement.Attribute("Value").Value;
                    try
                    {
                        PopupFilter = Convert.ToBoolean(value);
                        DebugUtil.Instance.LOG.Info("PopupFilter" + PopupFilter.ToString());
                    }
                    catch
                    {
                        DebugUtil.Instance.LOG.Info("PopupFilter = false");
                    }
                }
            }
            {   //Iscs alarm analysis binary
                XElement elPath = doc.Root.Element("IscsAlarmAnalysisBinary");
                IscsAlarmAnalysisBinary = elPath.Attribute("Value").Value;
                if (!File.Exists(IscsAlarmAnalysisBinary))
                {
                    MessageBox.Show("Iscs AlarmAnalysis Binary not exist!");
                    DebugUtil.Instance.LOG.Error("Iscs AlarmAnalysis Binary not exist!");
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

        public string GetSubsystemIDByName(string subsystemName, string systemID)
        {
            string ret = "";
            try
            {
                ret = allSubsystemList.Where(p => p.Name == subsystemName && p.systemID == systemID).FirstOrDefault().ID;
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find subsystem name: " + subsystemName);
            }
            return ret;
        }

        public string GetSubsystemNameByID(string subsystemID, string systemID)
        {
            string ret = "";
            try
            {
                ret = allSubsystemList.Where(p => p.ID == subsystemID && p.systemID == systemID).FirstOrDefault().Name;
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find subsystem ID: " + subsystemID);
            }
            return ret;
        }

        public Subsystem GetSubsystemByName(string subsystemName, string systemID)
        {
            Subsystem ret = null;
            try
            {
                ret = allSubsystemList.Where(p => p.Name == subsystemName && p.systemID == systemID).FirstOrDefault();
            }
            catch (Exception)
            {
                DebugUtil.Instance.LOG.Error("Cannot find subsystem Name: " + subsystemName);
            }
            return ret;
        }
    }
}
