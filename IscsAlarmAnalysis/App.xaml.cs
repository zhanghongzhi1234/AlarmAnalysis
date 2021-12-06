using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //string libPath = "C:\\Program Files\\DashboardEditor";
        public static string runningMode = "Iscs";

        private const string UniqueEventName = "IscsAlarmAnalysisEvent";
        private const string UniqueMutexName = "IscsAlarmAnalysisMutex";
        private EventWaitHandle eventWaitHandle;
        private Mutex mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                if (runningMode == "Ats")
                    this.StartupUri = new System.Uri("Template/AtsAlarmDashboard.xaml", System.UriKind.Relative);
                else
                    this.StartupUri = new System.Uri("Template/IscsAlarmDashboard.xaml", System.UriKind.Relative);
                //this.StartupUri = new System.Uri("Template/testWin2.xaml", System.UriKind.Relative);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }

            bool isOwned;
            this.mutex = new Mutex(true, UniqueMutexName, out isOwned);
            this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);
            GC.KeepAlive(this.mutex);
            if (isOwned)
            {
                var thread = new Thread(
                    () =>
                    {
                        while (this.eventWaitHandle.WaitOne())
                        {
                            Current.Dispatcher.BeginInvoke((Action)(() =>
                                //((MainWindow)Current.MainWindow).BringToForeground()));
                                {
                                    //could be set or removed anytime
                                    var mw = Current.MainWindow;
                                    if (mw.WindowState == WindowState.Minimized || mw.Visibility != Visibility.Visible)
                                    {
                                        mw.Show();
                                        mw.WindowState = WindowState.Normal;
                                    }

                                    //According to some sources these steps are required to be sure it went to foreground.
                                    mw.Activate();
                                    mw.Topmost = true;
                                    mw.Topmost = false;
                                    mw.Focus();
                                }));
                        }
                    });
                thread.IsBackground = true;
                thread.Start();
                return;
            }
            this.eventWaitHandle.Set();
            this.Shutdown();
        }


        /*[STAThread]
        public static void Main()
        {
            string libPath = "C:\\Program Files\\DashboardEditor";
            try
            {
                RegistryKey hklm;
                if (Environment.Is64BitProcess == true)
                    hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                else
                    hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey rsg = hklm.OpenSubKey("SOFTWARE\\DashboardEditor", false);
                libPath = rsg.GetValue("Path").ToString();
                rsg.Close();

                //AppDomain.CurrentDomain.AppendPrivatePath("Lib");
                if (AppDomain.CurrentDomain.FriendlyName != "MyApp")
                {
                    AppDomainSetup domainInfo = new AppDomainSetup();
                    //domainInfo.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                    domainInfo.ApplicationBase = libPath;
                    domainInfo.PrivateBinPath = "Lib";
                    var domain = AppDomain.CreateDomain("MyApp", null, domainInfo);
                    domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location);
                    AppDomain.Unload(domain);
                    return;
                }

                //Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var application = new App();
                application.InitializeComponent();
                application.Run();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                //MessageBox.Show("Please Install Dashboard Framework Before Run The Application!");
            }
        }
        */
    }
}
