using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TemplateProject
{
    public partial class IscsAlarmStationSummary
    {
        List<SystemInfo> systemList = IscsCachedMap.Instance.systemList;
        List<Subsystem> allSubsystemList = IscsCachedMap.Instance.allSubsystemList;
        List<Severity> severityList = IscsCachedMap.Instance.severityList;
        List<Location> locationList = IscsCachedMap.Instance.locationList;

        List<Border> borderList = null;
        //List<TextBlock> txtValueList = new List<TextBlock>();
        IscsAlarmDashboard dashboardWindow = null;
        bool isEnabledFilter = false;
        DateTime startTime;
        DateTime endTime;
        string subsystemIdSelected = "";

        void InitScript()
        {
            btnDashboard.MouseLeftButtonUp += btnDashboard_MouseLeftButtonUp;
            btnDashboard.MouseEnter += panel_MouseEnter;
            btnDashboard.MouseLeave += panel_MouseLeave;
            btnOK.Click += btnOK_Click;
            btnOK.MouseEnter += menuButton_MouseEnter;
            btnOK.MouseLeave += menuButton_MouseLeave;
            btnClear.Click += btnClear_Click;
            btnClear.MouseEnter += menuButton_MouseEnter;
            btnClear.MouseLeave += menuButton_MouseLeave;
            //cmbSubsystem.SelectionChanged += cmbSubsystem_SelectionChanged;
        }

        void btnOK_Click(object sender, RoutedEventArgs e)
        {
            isEnabledFilter = true;
            GetTimeRange(out startTime, out endTime);
            if (startTime >= endTime)
            {
                MessageBox.Show("Start time must be less than end time!");
                dpDateStart.Focus();
                return;
            }
            ComboBoxItem item = cmbSubsystem.SelectedItem as ComboBoxItem;
            if (item.Tag != null)
            {
                subsystemIdSelected = item.Tag.ToString();
            }
            ReloadAllAlarmFromDb();
        }

        void btnClear_Click(object sender, RoutedEventArgs e)
        {
            isEnabledFilter = false;
            startTime = DateTime.Now.Date;
            endTime = startTime + new TimeSpan(1, 0, 0, 0);
            subsystemIdSelected = "";
            cmbSubsystem.SelectedIndex = 0;
            ReloadAllAlarmFromDb();
        }

        public void SetDashboardWindow(IscsAlarmDashboard dashboardWindow)
        {
            this.dashboardWindow = dashboardWindow;
        }

        private void InitAll()
        {
            startTime = DateTime.Now.Date;
            endTime = startTime + new TimeSpan(1, 0, 0, 0);

            dpDateStart.SelectedDate = DateTime.Now.Date;
            dpDateEnd.SelectedDate = DateTime.Now.Date + new TimeSpan(1, 0, 0, 0);
            for (int i = 0; i <= 23; i++)
            {
                cmbHourStart.Items.Add(i);
                cmbHourEnd.Items.Add(i);
            }
            cmbHourStart.SelectedIndex = 2;
            cmbHourEnd.SelectedIndex = 2;
            InitPanel1();
        }

        public void InitPanel1()
        {
            borderList = FindVisualChildren<Border>(Panel1).ToList();      //use border name to find also ok
            for (int i = 0; i < borderList.Count; i++)
            {
                Border border = borderList[i];
                if (!border.Name.StartsWith("Loc"))
                    break;

                TextBlock[] txtArray = FindVisualChildren<TextBlock>(border).ToArray();
                string locationName = txtArray[0].Text.Trim();
                Location location = locationList.Where(p => p.Name == locationName).FirstOrDefault();
                if (location != null && location.Operational == "0")
                {
                    txtArray[0].Text = "";
                    txtArray[1].Text = "";
                    border.Background = Brushes.Black;
                }
                else
                {
                    txtArray[1].Text = "0";
                    border.Background = Brushes.Green;
                }
                //txtValueList.Add(txtArray[1]);
            }
        }

        //Clear all and reload all alarm from Db
        public void ReloadAllAlarmFromDb()
        {
            DebugUtil.Instance.LOG.Info("ReloadAllAlarmFromDb Enter");
            /*if (isEnabledFilter == true)
            {
                GetTimeRange(out startTime, out endTime);
            }*/
            List<AtsAlarm> allAlarmList;
            if (subsystemIdSelected != "")
            {
                List<string> subsystemkeyList = new List<string>();
                subsystemkeyList.Add(subsystemIdSelected);
                allAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, endTime, 0, "Alarm", "", subsystemkeyList);
            }
            else
            {
                allAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, endTime, 0);
            }
            
            List<AtsAlarm> activeAlarmList = allAlarmList.Where(p => p.state == "1").ToList();
            UpdatePanel1(activeAlarmList, new List<AtsAlarm>(), true);
            DebugUtil.Instance.LOG.Info("ReloadAllAlarmFromDb Exit");
        }

        private void GetTimeRange(out DateTime startTime, out DateTime endTime)
        {
            int startHour = Convert.ToInt32(cmbHourStart.SelectedValue);
            startTime = dpDateStart.SelectedDate.Value.Date + new TimeSpan(startHour, 0, 0);
            int endHour = Convert.ToInt32(cmbHourEnd.SelectedValue);
            endTime = dpDateEnd.SelectedDate.Value.Date + new TimeSpan(endHour, 0, 0);
        }

        private void UpdatePanel1(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            for (int i = 0; i < borderList.Count; i++)
            {
                Border border = borderList[i];
                if (!border.Name.StartsWith("Loc"))
                    break;

                TextBlock[] txtArray = FindVisualChildren<TextBlock>(border).ToArray();
                string locationName = txtArray[0].Text.Trim();
                Location location = locationList.Where(p => p.Name == locationName).FirstOrDefault();
                if (location != null)
                {
                    int upCount = upAlarmList.Where(p => p.locationId == location.Name).Count();       //location Id here is name actually
                    int downCount = downAlarmList.Where(p => p.locationId == location.Name).Count();

                    TextBlock txtValue = txtArray[1];
                    UpdateTextBlock(txtValue, upCount, downCount, bReset);
                    DebugUtil.Instance.LOG.Debug(locationName + "=" + txtValue.Text + ", upCount=" + upCount + ", downCount=" + downCount + ", bReset=" + bReset);

                    if (txtValue.Text == "0")
                        borderList[i].Background = Brushes.Green;
                    else
                        borderList[i].Background = Brushes.Red;
                }
            }
        }

        public void UpdateAll(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            if (isEnabledFilter == true)
            {
                //GetTimeRange(out startTime, out endTime);
                List<AtsAlarm> filteredUpAlarmList;
                List<AtsAlarm> filteredDownAlarmList;
                if (subsystemIdSelected != "")
                {
                    filteredUpAlarmList = upAlarmList.Where(p => p.sourceTime >= startTime && p.sourceTime <= endTime && p.subsystemID == subsystemIdSelected).ToList();
                    filteredDownAlarmList = downAlarmList.Where(p => p.sourceTime >= startTime && p.sourceTime <= endTime).ToList();
                }
                else
                {
                    filteredUpAlarmList = upAlarmList.Where(p => p.sourceTime >= startTime && p.sourceTime <= endTime).ToList();
                    filteredDownAlarmList = downAlarmList.Where(p => p.sourceTime >= startTime && p.sourceTime <= endTime).ToList();
                }
                
                UpdatePanel1(filteredUpAlarmList, filteredDownAlarmList, bReset);
            }
            else
            {
                UpdatePanel1(upAlarmList, downAlarmList, bReset);
            }
        }

        void LoadScript()
        {
            InitAll();          //Put InitAll here because LoadScript execute after binding all Chart to data, the Axes and legend created already
            if (IscsCachedMap.Instance.LocalTest == false)
            {
                DebugUtil.Instance.LOG.Info("Reload all alarms from db");
                ReloadAllAlarmFromDb();           //this function will execute in timerPoll_Tick for the first time
            }
        }

        private void btnDashboard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dashboardWindow == null)
            {
                MessageBox.Show("Dashboard window not exist!");
            }
            dashboardWindow.Show();
            this.Hide();
        }

        private void UpdateTextBlock(TextBlock txtBlock, int upCount, int downCount, bool bReset = false)
        {
            if (txtBlock != null)
            {
                int oldValue = bReset == false ? Convert.ToInt32(txtBlock.Text) : 0;
                int newCount = oldValue + upCount - downCount;
                if (newCount < 0)
                    newCount = 0;

                txtBlock.Text = newCount.ToString();
            }
        }

        //void cmbSubsystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox cmbSubSelect = sender as ComboBox;
        //    ComboBoxItem item = cmbSubSelect.SelectedItem as ComboBoxItem;
        //    string subsystemID = "";
        //    if (item.Tag != null)
        //    {
        //        subsystemID = item.Tag.ToString();
        //    }
        //    if (subsystemID != "")
        //    {
        //        isEnabledFilter = true;
        //    }
        //    else
        //    {
        //        isEnabledFilter = false;
        //    }
        //    ReloadAllAlarmFromDb();
        //}
    }
}
