using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Visifire.Charts;

namespace TemplateProject
{
    public partial class IscsAlarmStationDetail
    {
        List<SystemInfo> systemList = IscsCachedMap.Instance.systemList;
        List<Subsystem> subsystemList = null;
        List<string> subIDList = null;
        List<Severity> severityList = IscsCachedMap.Instance.severityList;
        List<Location> locationList = IscsCachedMap.Instance.locationList;
        DataTable tableAlarmList = new DataTable();
        string locationId;
        string systemkey;
        string systemName;
        bool bPaused = false;

        IscsAlarmDashboard dashboardWindow = null;

        public void SetDashboardWindow(IscsAlarmDashboard dashboardWindow)
        {
            this.dashboardWindow = dashboardWindow;
        }

        public void SetLocationId(string locationId)
        {
            this.locationId = locationId;
            txtTitleLocation.Text = "(" + locationId + ")";
        }

        public void SetSystemkey(string systemkey)
        {
            this.systemkey = systemkey;
            SystemInfo systemInfo = systemList.Where(p => p.ID == systemkey).FirstOrDefault();
            systemName = systemInfo.Name;
            subsystemList = systemInfo.subsystemList;
            subIDList = subsystemList.Select(p => p.ID).ToList();
            txtTitleSystem.Text = systemName;
        }

        void InitScript()
        {
            btnPause.MouseEnter += panel_MouseEnter;
            btnPause.MouseLeave += panel_MouseLeave;
            btnPause.MouseLeftButtonUp += btnPause_MouseLeftButtonUp;
            btnDashboard.MouseLeftButtonUp += btnDashboard_MouseLeftButtonUp;
            btnDashboard.MouseEnter += panel_MouseEnter;
            btnDashboard.MouseLeave += panel_MouseLeave;
            //Chart1.MouseDoubleClick += ChartSwitch_MouseDoubleClick;
            btnExport.MouseLeftButtonUp += btnExport_MouseLeftButtonUp;
            btnExport.MouseEnter += panel_MouseEnter;
            btnExport.MouseLeave += panel_MouseLeave;
        }

        void btnExport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            btnExport_Click(null, null);
        }

        void btnPause_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (bPaused == false)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }

        private void Pause()
        {
            bPaused = true;
            txtPause.Text = "PAUSED";
            txtPause.Foreground = CommonFunction.GetInvertedBrush(Brushes.Red as SolidColorBrush);          //Invert now because mouse still in the panel, when leave, color will invert again
        }

        private void Resume()
        {
            bPaused = false;
            txtPause.Text = "PLAY";
            txtPause.Foreground = CommonFunction.GetInvertedBrush(Brushes.Lime as SolidColorBrush);         //Invert now because mouse still in the panel, when leave, color will invert again
            ReloadAllAlarmFromDb();
        }

        private void InitAll()
        {
            InitChart1();
            InitChart2();
            InitAlarmList(alarmList1);
        }

        public void InitChart1()
        {
            //Init all location data to 0 for Chart1
            List<RawTable> data = new List<RawTable>();
            foreach (Severity severity in severityList)
            {
                data.Add(new RawTable(severity.Name, 0, new SolidColorBrush((Color)ColorConverter.ConvertFromString(severity.Color))));
            }
            dataSourceMap["Data1"].ReloadChartData(data);
            Chart1.View3D = false;
        }

        public void InitChart2()
        {
            //Init all location data to 0 for Chart2
            List<RawTable> data = new List<RawTable>();
            foreach (Severity severity in severityList)
            {
                data.Add(new RawTable(severity.Name, 0, new SolidColorBrush((Color)ColorConverter.ConvertFromString(severity.Color))));
            }
            dataSourceMap["Data2"].ReloadChartData(data);
            Chart2.DataPointWidth = 15;
        }

        //available property: HorizontalContentAlignmentProperty,HeightProperty,FontSizeProperty,BackgroundProperty,ForegroundProperty,BorderThicknessProperty,ToolTipProperty
        public void InitAlarmList(DataGrid dataGrid)
        {
            dataGrid.Columns.Add(CreateTextColumn("SEV", 40, "Severity", "Background"));
            dataGrid.Columns.Add(CreateTextColumn("DESCRIPTION", 750, "Description", "Background", TextAlignment.Left, TextWrapping.NoWrap));
            dataGrid.Columns.Add(CreateTextColumn("DATE/TIME", 140, "DateTime", "Background"));
            dataGrid.ColumnHeaderHeight = 32d;

            Style style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0c6fc0"))));
            style.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));         //this parameter have no use here, don't know why
            style.Setters.Add(new Setter(FontWeightProperty, FontWeights.Bold));
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                dataGrid.Columns[i].HeaderStyle = style;
            }

            dataGrid.RowHeight = 30d;
            dataGrid.FontSize = 14;
            dataGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            dataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

            tableAlarmList.Columns.Add("Severity");
            tableAlarmList.Columns.Add("Description");
            tableAlarmList.Columns.Add("DateTime");
        }

        //Clear all and reload all alarm from Db
        public void ReloadAllAlarmFromDb()
        {   
            //DateTime startTime = DateTime.Now.Date;
            //List<AtsAlarm> allAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, DateTime.Now, 0);
            List<AtsAlarm> allAlarmList = DAIHelper.Instance.GetAlarmListToday(0);
            List<AtsAlarm> activeAlarmList = allAlarmList.Where(p => p.state == "1").ToList();
            UpdateAll(activeAlarmList, new List<AtsAlarm>(), true);
        }

        private void UpdateDetailAlarmList(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            if (bReset == true)
                alarmList1.Items.Clear();
            //remove alarm item if closed
            for (int i = alarmList1.Items.Count - 1; i >= 0; i--)
            {
                var dictionary = (IDictionary<string, object>)alarmList1.Items[i];
                AtsAlarm alarm = downAlarmList.Where(p => p.alarmID == dictionary["alarmID"].ToString()).FirstOrDefault();
                if (alarm != null && alarm.state == "0")
                {
                    alarmList1.Items.RemoveAt(i);
                }
            }
            //add active alarm
            List<AtsAlarm> alarmList = upAlarmList.Where(p => p.locationId == locationId && subIDList.Contains(p.subsystemID) && p.state == "1").OrderBy(p => p.sourceTime).ToList();
            foreach (AtsAlarm alarm in alarmList)
            {
                AddItemForSubsystemAlarmList(alarmList1, alarm.alarmID, alarm.severityID, alarm.description, alarm.sourceTime.ToString("MM-dd-yy HH:mm:ss"));
            }
        }

        private void AddItemForSubsystemAlarmList(DataGrid dataGrid, string alarmID, string Severity1, string Description, string Time, string ForeColor = null, string BackColor = null)
        {
            dynamic item = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)item;
            dictionary["alarmID"] = alarmID;
            dictionary["Severity"] = Severity1;
            dictionary["Description"] = Description;
            dictionary["DateTime"] = Time;
            Severity severity = severityList.Where(p => p.ID == Severity1).FirstOrDefault();
            if (severity != null)
            {
                SolidColorBrush brush1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severity.Color));
                dictionary["Background"] = brush1;
            }
            if (ForeColor != null)
                dictionary["ForeColor"] = ForeColor;
            if (BackColor != null)
                dictionary["BackColor"] = BackColor;

            dataGrid.Items.Insert(0, item);
        }

        //for testing purpose only
        private void LoadDummyHistoryAlarm()
        {   //maybe change in future, currently use dummy data
            Random random = new Random();
            {   //Dummy data for Chart1
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < severityList.Count(); i++)
                {
                    data.Add(new RawTable(severityList[i].Name, random.Next(0, 50), new SolidColorBrush((Color)ColorConverter.ConvertFromString(severityList[i].Color))));
                }
                dataSourceMap["Data1"].ReloadChartData(data);
            }
            for(int i = 0; i < 100; i++)
            {
                AddItemForSubsystemAlarmList(alarmList1, i.ToString(), random.Next(1, 6).ToString(), "Alarm Description " + i + ": 22kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading: 22kV SWITCH RM 2", (DateTime.Now - new TimeSpan(0, 0, i)).ToString("MM-dd-yy HH:mm:ss"));
            }
        }

        public void UpdateAll(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            if (bPaused == true)
                return;
            UpdateChart1(upAlarmList, downAlarmList, bReset);
            UpdateChart2(upAlarmList, downAlarmList, bReset);
            UpdateDetailAlarmList(upAlarmList, downAlarmList, bReset);
        }

        private void UpdateChart1(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            for (int i = 0; i < severityList.Count; i++)
            {
                Severity severity = severityList[i];
                int upCount = upAlarmList.Where(p => p.locationId == locationId && subIDList.Contains(p.subsystemID) && p.severityID == severity.ID).Count();
                int downCount = downAlarmList.Where(p => p.locationId == locationId && subIDList.Contains(p.subsystemID) && p.severityID == severity.ID).Count();

                DataPoint dp = Chart1.Series[0].DataPoints[i];      //each severity is a datapoint
                UpdateDatapoint(dp, upCount, downCount, bReset);
            }
            AdjustAxisYMaximumInternal(Chart1);
        }

        private void UpdateChart2(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            for (int i = 0; i < severityList.Count; i++)
            {
                Severity severity = severityList[i];
                int upCount = upAlarmList.Where(p => p.locationId == locationId && subIDList.Contains(p.subsystemID) && p.severityID == severity.ID).Count();
                int downCount = downAlarmList.Where(p => p.locationId == locationId && subIDList.Contains(p.subsystemID) && p.severityID == severity.ID).Count();

                DataPoint dp = Chart2.Series[0].DataPoints[i];      //each severity is a datapoint
                UpdateDatapoint(dp, upCount, downCount, bReset);
            }
            AdjustAxisYMaximumInternal(Chart2);
        }

        void LoadScript()
        {
            InitAll();          //Put InitAll here because LoadScript execute after binding all Chart to data, the Axes and legend created already
            if (IscsCachedMap.Instance.LocalTest == false)
            {
                DebugUtil.Instance.LOG.Info("Reload all alarms from db");
                ReloadAllAlarmFromDb();           //this function will execute in timerPoll_Tick for the first time
            }
            else
            {
                DebugUtil.Instance.LOG.Info("Load dummy alarms");
                LoadDummyHistoryAlarm();
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

        private void AdjustAxisYMaximumInternal(Chart chart)
        {
            int AxisMaxOri = 0;
            if (chart.AxesY[0].AxisMaximum != null)
                AxisMaxOri = Convert.ToInt32(chart.AxesY[0].AxisMaximum);
            int AxisMax = AxisMaxOri;
            if (chart != null)
            {
                foreach (DataSeries dataSeries in chart.Series)
                {
                    if (dataSeries.DataPoints != null)
                    {
                        foreach (DataPoint dp in dataSeries.DataPoints)
                        {
                            while (dp.YValue > AxisMax)
                            {
                                AxisMax = GetSuitableMaxforValue(dp.YValue);
                            }
                        }
                    }
                }
            }
            if (AxisMax > AxisMaxOri)
                chart.AxesY[0].AxisMaximum = AxisMax;
        }

        private int GetSuitableMaxforValue(double value)
        {
            int[] times = { 1, 2, 5 };
            int index = 0;
            int exp = 0;
            int max = times[index] * Convert.ToInt32(Math.Pow(10, exp));
            while (max < value)
            {
                index++;
                if (index >= times.Count())
                {
                    index = 0;
                    exp++;
                }
                max = times[index] * Convert.ToInt32(Math.Pow(10, exp));
            }
            return max;
        }

        void SortDataGrid(DataGrid dataGrid, int columnIndex = 0, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            var column = dataGrid.Columns[columnIndex];

            // Clear current sort descriptions
            dataGrid.Items.SortDescriptions.Clear();

            // Add the new sort description
            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            // Apply sort
            foreach (var col in dataGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = sortDirection;

            // Refresh items to display sort
            dataGrid.Items.Refresh();
        }

        private void UpdateDatapoint(DataPoint dp, int upCount, int downCount, bool bReset = false)
        {
            if (dp != null)
            {
                int oldValue = bReset == false ? Convert.ToInt32(dp.YValue) : 0;
                int newCount = oldValue + upCount - downCount;
                if (newCount < 0)
                    newCount = 0;

                dp.YValue = newCount;
            }
        }
    }
}
