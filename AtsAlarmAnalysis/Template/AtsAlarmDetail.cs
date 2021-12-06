using MyList;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TemplateProject
{
    public partial class AtsAlarmDetail
    {
        List<SystemInfo> systemList = AtsCachedMap.Instance.systemList;
        List<Subsystem> allSubsystemList = AtsCachedMap.Instance.allSubsystemList;
        List<Severity> severityList = AtsCachedMap.Instance.severityList;

        AtsAlarmDashboard dashboardWindow = null;
        string severityFilter = "";                     //start with '!' mean exclude

        public void SetDashboardWindow(AtsAlarmDashboard dashboardWindow)
        {
            this.dashboardWindow = dashboardWindow;
        }

        void InitScript()
        {
            systemName1.Text = systemList[0].Name;
            systemName2.Text = systemList[1].Name;
            systemName3.Text = systemList[2].Name;
            systemName4.Text = systemList[3].Name;
            alarmList1.Tag = systemList[0].Name;
            alarmList2.Tag = systemList[1].Name;
            alarmList3.Tag = systemList[2].Name;
            alarmList4.Tag = systemList[3].Name;
            btnDashboard.Click += btnDashboard_Click;
            btnDashboard.MouseEnter += menuButton_MouseEnter;
            btnDashboard.MouseLeave += menuButton_MouseLeave;
        }

        private void InitAll()
        {
            InitDetailAlarmList(alarmList1);
            InitDetailAlarmList(alarmList2);
            InitDetailAlarmList(alarmList3);
            InitDetailAlarmList(alarmList4);
            if (!string.IsNullOrEmpty(severityFilter))
            {
                if (severityFilter == "Alert")
                {
                    txtTitle1.Text = "Alert Detail";
                }
                /*else
                {
                    txtTitle1.Text += " - " + severityFilter;
                }*/
            }
        }

        public void InitDetailAlarmList(AlarmList subAlarmList)
        {
            subAlarmList.SetHorizontalScrollBarVisibility(ScrollBarVisibility.Hidden);
            subAlarmList.SetVerticalScrollBarVisibility(ScrollBarVisibility.Auto);

            //set header style
            Style style = new Style();
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            style.Setters.Add(new Setter(HeightProperty, 18d));
            style.Setters.Add(new Setter(FontSizeProperty, 11d));
            style.Setters.Add(new Setter(BackgroundProperty, Brushes.Black));
            style.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            //style.Setters.Add(new Setter(BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));         //this parameter have no use here, don't know why
            subAlarmList.SetHeaderStyle(style);

            //set item style
            subAlarmList.AddSetterToItemContainerStyle(new Setter(FontSizeProperty, 11d));
            subAlarmList.AddSetterToItemContainerStyle(new Setter(HeightProperty, 18d));
            subAlarmList.AddSetterToItemContainerStyle(new Setter(BackgroundProperty, Brushes.Black));
            //subAlarmList.AddSetterToItemContainerStyle(new Setter(ForegroundProperty, Brushes.White));
            subAlarmList.AddSetterToItemContainerStyle(new Setter(ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"))));

            //set item trigger
            subAlarmList.RemoveAllItemContainerStyleTriggers();
            DataTrigger trigger1 = new DataTrigger();
            trigger1.Value = "N";
            trigger1.Binding = new Binding() { Path = new PropertyPath("Ack") };
            if (severityFilter == "Alert")
            {
                trigger1.Setters.Add(new Setter(BackgroundProperty, new BrushConverter().ConvertFromString("#FFBF00") as SolidColorBrush));
            }
            else
            {
                trigger1.Setters.Add(new Setter(BackgroundProperty, Brushes.Red));
            }
            trigger1.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            subAlarmList.AddTriggerToItemContainerStyle(trigger1);
            DataTrigger trigger2 = new DataTrigger();
            trigger2.Value = "A";
            trigger2.Binding = new Binding() { Path = new PropertyPath("Ack") };
            trigger2.Setters.Add(new Setter(BackgroundProperty, Brushes.Green));
            trigger2.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            subAlarmList.AddTriggerToItemContainerStyle(trigger2);

            //add column for alarm list
            subAlarmList.gridView.Columns.Clear();
            subAlarmList.AddColumn(" Date/Time", 130, "Time", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Severity", 60, "Severity", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Equipment Code", 150, "EQPT", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Alarm Description", 340, "Description", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Alarm Value", 100, "AlarmValue", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Location", 80, "Location", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Subsystem", 110, "Subsystem", HorizontalAlignment.Center);
            subAlarmList.AddColumn(" Ack", 60, "Ack", HorizontalAlignment.Center);
        }

        //Clear all and reload all alarm from Db, called from outside
        public void ReloadAllAlarmByList(List<AtsAlarm> allAlarmList)
        {   //reset all data to 0
            {   //Dummy data for AlarmList1, AlarmList2, AlarmList3, AlarmList4
                //List<AtsAlarm> alarmList = GetTodayAtsAlarmListBySystemKeyFromDb(-1);
                //List<AtsAlarm> alarmList = DAIHelper.Instance.GetTodayAtsAlarmListBySystemShortNameFromDb();
                UpdateDetailAlarmList(allAlarmList, true);
            }
        }

        //Clear all and reload all alarm from Db
        public void ReloadAllAlarmFromDb()
        {   //reset all data to 0
            {   
                DateTime startTime = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
                DateTime endTime = DateTime.Now.Date + new TimeSpan(23, 59, 59);
                List<AtsAlarm> allAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, endTime);
                UpdateDetailAlarmList(allAlarmList, true);
            }

        }

        private void UpdateDetailAlarmList(List<AtsAlarm> newAlarmList, bool reset = false)
        {
            if (reset == true)
            {
                alarmList1.listView.Items.Clear();
                alarmList2.listView.Items.Clear();
                alarmList3.listView.Items.Clear();
                alarmList4.listView.Items.Clear();
            }
            
            foreach (AtsAlarm alarm in newAlarmList)
            {
                if (!string.IsNullOrEmpty(severityFilter) && alarm.severityID != severityFilter)
                {
                    if (severityFilter.StartsWith("!"))
                    {
                        if (alarm.severityID == severityFilter.TrimStart('!'))
                            continue;
                    }
                    else
                    {
                        if (alarm.severityID != severityFilter)
                            continue;
                    }
                }
                AlarmList alarmList = null;
                Rectangle rectangle = null;
                if (alarm.systemID == systemList[0].ID)
                {   //alarmList1: Rolling Stock
                    alarmList = alarmList1;
                    rectangle = Rectangle1;
                }
                else if (alarm.systemID == systemList[1].ID)
                {   //alarmList2: Signaling
                    alarmList = alarmList2;
                    rectangle = Rectangle2;
                }
                else if (alarm.systemID == systemList[2].ID)
                {   //alarmList3: PSD
                    alarmList = alarmList3;
                    rectangle = Rectangle4;
                }
                else if (alarm.systemID == systemList[3].ID)
                {   //alarmList4: Communication
                    alarmList = alarmList4;
                    rectangle = Rectangle4;
                }
                if (alarmList != null && rectangle != null)
                {
                    AddItemForSubsystemAlarmList(alarmList, rectangle, alarm.sourceTime.ToString("MM-dd-yy HH:mm:ss"), alarm.severityID, alarm.assetName, alarm.description, 
                        alarm.alarmValue, alarm.locationId, AtsCachedMap.Instance.GetSubsystemNameByID(alarm.subsystemID, alarm.systemID), alarm.systemID, alarm.Ack);
                }
            }
        }

        private void AddItemForSubsystemAlarmList(AlarmList almList, Rectangle rectangle,
            string Time, string Severity, string EQPT, string Description, string AlarmValue, string Location, string Subsystem, string systemID, string Ack, string ForeColor = null, string BackColor = null)
        {
            dynamic item = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)item;
            dictionary["Time"] = Time;
            dictionary["Severity"] = Severity;
            dictionary["EQPT"] = EQPT;
            dictionary["Description"] = Description;
            dictionary["AlarmValue"] = AlarmValue;
            dictionary["Location"] = Location;
            dictionary["Subsystem"] = Subsystem;
            dictionary["systemID"] = systemID;
            //dictionary["Ack"] = Ack;
            if (ForeColor != null)
                dictionary["ForeColor"] = ForeColor;
            if (ForeColor != null)
                dictionary["BackColor"] = BackColor;
            if (Ack == "0")
            {
                dictionary["Ack"] = "N";
                rectangle.Fill = Brushes.Red;
            }
            else
            {
                dictionary["Ack"] = "A";
                rectangle.Fill = Brushes.Green;
            }
            almList.listView.Items.Insert(0, item);
        }

        //for testing purpose only
        /*private void LoadDummyHistoryAlarm()
        {   //maybe change in future, currently use dummy data
            {   //Dummy data for AlarmList1
                AddItemForSubsystemAlarmList(alarmList1, Rectangle1, "90020/RSC/DRV/CABOCC", "Cab Selection", "2", "1");
                AddItemForSubsystemAlarmList(alarmList1, Rectangle1, "90020/RSC/EB", "Emergency Brake", "2", "1");
            }
            {   //Dummy data for AlarmList2
                AddItemForSubsystemAlarmList(alarmList2, Rectangle2, "90592/SIG/ATC/ATP_SYS", "Signaling changed to FBSS", "1", "0");
                AddItemForSubsystemAlarmList(alarmList2, Rectangle2, "90412/SIG/ATC/ATP_SYS", "One ATP Lane Failure", "2", "0");
            }
            {   //Dummy data for AlarmList3
                AddItemForSubsystemAlarmList(alarmList3, Rectangle3, "DT28/PSD/PSCC/PLCA", "Door Unlocked Without Sending Open Command At BB Platform", "2", "0");
                AddItemForSubsystemAlarmList(alarmList3, Rectangle3, "DT28/PSD/DRMOD/BB_MRS_1", "BB PSD 1 Manual Release Is Activated", "2", "1");
                AddItemForSubsystemAlarmList(alarmList3, Rectangle3, "DT28/PSD/DRMOD/BB_DCU_3", "BB PSD 3 Obstructed During Closing Phase", "2", "1");
            }
            {   //Dummy data for AlarmList4
                AddItemForSubsystemAlarmList(alarmList4, Rectangle4, "90011/COM/TVSS/WL_CLIENT_A2", "TVSS WLAN Link", "2", "0");
                AddItemForSubsystemAlarmList(alarmList4, Rectangle4, "90011/COM/TVSS/WL_CLIENT_A1", "TVSS WLAN Link", "2", "0");
                AddItemForSubsystemAlarmList(alarmList4, Rectangle4, "90011/COM/TVSS/WL_CLIENT_B1", "TVSS WLAN Link", "2", "0");
            }
        }*/

        public void UpdateAll(List<AtsAlarm> newAlarmList)
        {
            UpdateDetailAlarmList(newAlarmList);
        }

        void LoadScript()
        {
            InitAll();          //Put InitAll here because LoadScript execute after binding all Chart to data, the Axes and legend created already
            ReloadAllAlarmFromDb();           //this function will execute in timerPoll_Tick for the first time
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (dashboardWindow == null)
            {
                MessageBox.Show("Dashboard window not exist!");
            }
            dashboardWindow.Show();
            this.Hide();
        }

        public void SetSeverityFilter(string severityFilter)
        {
            this.severityFilter = severityFilter;
            if (severityFilter == "Alert")
            {
                this.Title = "ATS Alert Detail";
            }
        }
    }
}
