using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Visifire.Charts;
using System.Data;

namespace TemplateProject
{
    public class SCADAServer : Server
    {
        const string dllPath = "datapointaccess.dll";
        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void init([In] string cmdLine);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr createDataPoint([In] string name);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string getName([In] IntPtr dp);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string getStringValue([In] IntPtr dp);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool getBoolValue([In] IntPtr dp);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern double getNumValue([In] IntPtr dp);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string updatedDataPoints();

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getQuality([In] IntPtr dp);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenEventA(uint lpEventAttributes, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr handle, uint dwMilliseconds);

        //DispatcherTimer timerPoll;
        //int pollInterval = 2000;            //Polling interval for server
        //List<RawTable> data = new List<RawTable>();
        public List<string> dpNameList = new List<string>();
        Chart chart;

        public SCADAServer(string name)
        {
            this.name = name;
            this.serverType = ServerType.SCADA;
            //log.Info("Init SCADA Server");
        }

        public override void Init()
        {
            string bin = "", lib = "";
            if (AtsCachedMap.Instance.isSetRunParam("bin"))
                bin = AtsCachedMap.Instance.GetRunParam("bin");
            if (AtsCachedMap.Instance.isSetRunParam("lib"))
                lib = AtsCachedMap.Instance.GetRunParam("lib");
            string path = Environment.GetEnvironmentVariable("PATH");
            path += @";" + bin + ";" + lib;
            log.Info("Path=" + path);
            Environment.SetEnvironmentVariable("PATH", path);

            try
            {
                init("--run-param-file=config.ini --debug-file=transactive.log --debug-level=DEBUG");
                //Start();
            }
            catch (Exception ex)
            {
                log.Error("SCADA init fail: " + ex.ToString());
            }

        }

        public void SetObserver(Chart chart, string dpNames)
        {
            //dpNameList.Clear();
            string[] temp = dpNames.Split(',');

            dpNameList.AddRange(temp);
            if (dpNameList.Count > 0)
            {
                StartSCADA();
            }
            this.chart = chart;
        }

        public override DataTable GetQueryData(string sqlstr)
        {
            return null;
        }

        public override bool SendData(string command)
        {
            return true;
        }

        public override List<RawTable> GetChartData(string name)
        {
            /*if (data.Count == 0)
            {
                string[] dpNames = name.Split(',');
                foreach (string dpName in dpNames)
                {
                    RawTable rawTable = new RawTable(dpName, 10);
                    data.Add(rawTable);
                }
            }*/
            return null;
        }

        private async void StartSCADA()
        {
            try
            {
                await Task.Run(() =>
                {
                    //IntPtr dp1 = createDataPoint("OCC.TIS.STIS.SEV.aiiTISC-CurrentSTISLibraryVersion");
                    //IntPtr dp2 = createDataPoint("OCC.TIS.STIS.SEV.aiiTISC-NextSTISLibraryVersion");
                    foreach (string dpName in dpNameList)
                    {
                        createDataPoint(dpName);
                    }
                    IntPtr e = OpenEventA(0x000F0000u | 0x00100000u | 0x3u, false, "DATAPOINT_UPDATE");

                    while (true && dpNameList.Count > 0)
                    {
                        WaitForSingleObject(e, 0xFFFFFFFFu);
                        string dps = updatedDataPoints();
                        foreach (string s in dps.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            for (int i = 0; i < dpNameList.Count; i++)
                            {
                                if (dpNameList[i] ==s )
                                {
                                    IntPtr d = createDataPoint(s);
                                    //this.Dispatcher.Invoke(() =>
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        double value = getNumValue(d);
                                        chart.Series[0].DataPoints[i].YValue = value;
                                    });
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                log.Error("update datapoint fail: " + ex.ToString());
            }
        }

        public override void Close()
        {
        }
    }
}
