using MyList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Visifire.Charts;

namespace TemplateProject
{
    public partial class IscsAlarmDashboard : IFilterInterface
    {
        List<SystemInfo> systemList = IscsCachedMap.Instance.systemList;
        List<Subsystem> allSubsystemList = IscsCachedMap.Instance.allSubsystemList;
        List<Severity> severityList = IscsCachedMap.Instance.severityList;
        List<Location> locationList = IscsCachedMap.Instance.locationList;
        DispatcherTimer timerPoll;
        DispatcherTimer timerDelay;
        int pollInterval = IscsCachedMap.Instance.pollInterval;
        string AlarmFilePath = IscsCachedMap.Instance.AlarmFilePath;
        int alarmThreshold = IscsCachedMap.Instance.alarmThreshold;
        //List<string> historyFileList = new List<string>();
        List<AtsAlarm> allAlarmList = new List<AtsAlarm>();
        List<AtsAlarm> activeAlarmList = new List<AtsAlarm>();          //alarm.state is 1, open alarm list
        List<AtsAlarm> historyAlarmList = new List<AtsAlarm>();          //alarm.state is 1, open alarm list in past half year exclude today
        List<IscsAlarmCount> historyActiveAlarmCountList = new List<IscsAlarmCount>();           //key is date, value is alarm count of this day
        int nRetry = 0;
        bool bYesterdayAlarmLoaded = false;
        //List<AtsAlarm> newAlarmList = new List<AtsAlarm>();
        //List<AtsAlarm> existAlarmList = new List<AtsAlarm>();
        DataTable tableTop10 = new DataTable();
        int nlastUpdateHour = -1;      // record last update hour, just for top 10 update every hour

        Random random = new Random(0);
        //SQLiteServer dbServer;
        bool bFirstStart = true;
        List<string> alarmIDList = new List<string>();       //used to keep all alarmID
        AlarmFilter alarmFilter = new AlarmFilter(0);
        AlarmFilterWindow1 filterDlg;
        IscsAlarmStationSummary stationSummaryWindow = null;
        IscsAlarmStationDetail stationDetailWindow = null;
        PumpDashboard pumpWindow = null;

        bool bTileInRealTimeMode = true;       //Tile have 2 mode: realtime mode and history mode
        bool excludeRevenueHour = true;        //exclude revenue hour(00:30 - 04:30 or not)

        DateTime dtTileOld = DateTime.Today;
        string tileTypeOld = "Daily";
        DateTime dtTile = DateTime.Today;
        string tileType = "Daily";

        void InitScript()
        {
            DebugUtil.Instance.LOG.Info("InitScript started");
            //dbServer = serverMap["Local"] as SQLiteServer;
            /*btnClear.Click += btnClear_Click;
            btnEdit.Click += btnEdit_Click;
            btnEdit.MouseEnter += menuButton_MouseEnter;
            btnEdit.MouseLeave += menuButton_MouseLeave;
            btnDetail.Click += btnDetail_Click;
            btnDetail.MouseEnter += menuButton_MouseEnter;
            btnDetail.MouseLeave += menuButton_MouseLeave;*/
            cmbSubSelect1.SelectionChanged += cmbSubSelect_SelectionChanged;
            cmbSubSelect2.SelectionChanged += cmbSubSelect_SelectionChanged;
            cmbSubSelect3.SelectionChanged += cmbSubSelect_SelectionChanged;
            cmbSubsystem3.MouseEnter += control_MouseEnter;
            cmbSubsystem3.MouseLeave += control_MouseLeave;
            cmbSubsystem3.SelectionChanged += cmbSubsystem3_SelectionChanged;
            cmbSubsystem4.MouseEnter += control_MouseEnter;
            cmbSubsystem4.MouseLeave += control_MouseLeave;
            cmbSubsystem4.SelectionChanged += cmbSubsystem4_SelectionChanged;
            txtFilter.MouseLeftButtonDown += txtFilter_Click;
            txtFilter.MouseEnter += textBlock_MouseEnter;
            txtFilter.MouseLeave += textBlock_MouseLeave;
            txtClearFilter.MouseLeftButtonDown += txtClearFilter_Click;
            txtClearFilter.MouseEnter += textBlock_MouseEnter;
            txtClearFilter.MouseLeave += textBlock_MouseLeave;
            btnStationSummary.Click += btnStationSummary_Click;
            btnStationSummary.MouseEnter += menuButton_MouseEnter;
            btnStationSummary.MouseLeave += menuButton_MouseLeave;
            Chart3.MouseLeftButtonUp += Chart_MouseLeftButtonUp;
            Chart3_Title.MouseLeftButtonDown += Chart_Title_MouseLeftButtonDown;
            Chart4.MouseLeftButtonUp += Chart_MouseLeftButtonUp;
            Chart4_Title.MouseLeftButtonDown += Chart_Title_MouseLeftButtonDown;
            /*imgLogo_Ats.MouseEnter += image_MouseEnter;
            imgLogo_Ats.MouseLeave += image_MouseLeave;
            imgLogo_Ats.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgLogo_Ats.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgLogo_Ats.MouseLeftButtonUp += imgLogoAts_MouseLeftButtonUp;*/
            txtATS.MouseLeftButtonDown += txtATS_Click;
            txtATS.MouseEnter += textBlock_MouseEnter;
            txtATS.MouseLeave += textBlock_MouseLeave;
            imgPump.MouseEnter += image_MouseEnter;
            imgPump.MouseLeave += image_MouseLeave;
            imgPump.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgPump.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgPump.MouseLeftButtonUp += imgPump_MouseLeftButtonUp;
            imgMinimize.MouseEnter += image_MouseEnter;
            imgMinimize.MouseLeave += image_MouseLeave;
            imgMinimize.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgMinimize.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgMinimize.MouseLeftButtonUp += imgMinimize_MouseLeftButtonUp;
            imgExit.MouseEnter += image_MouseEnter;
            imgExit.MouseLeave += image_MouseLeave;
            imgExit.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgExit.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgExit.MouseLeftButtonUp += imgExit_MouseLeftButtonUp;

            cmbTileType.SelectionChanged += cmbTileType_SelectionChanged;
            /*if (IscsCachedMap.Instance.isSetRunParam("showClear"))
                btnClear.Visibility = System.Windows.Visibility.Visible;*/
            DebugUtil.Instance.LOG.Info("InitScript completed");

        }

        void cmbSubSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmbSubSelect = sender as ComboBox;
            ComboBoxItem item = cmbSubSelect.SelectedItem as ComboBoxItem;
            string subsystemID = "";
            if (item.Tag != null)
            {
                subsystemID = item.Tag.ToString();
            }
            string index = cmbSubSelect.Name.Substring(cmbSubSelect.Name.Count() - 1, 1);
            TextBlock txtSubValue = FindTextBlockByName(Panel1, "txtSubValue" + index);
            txtSubValue.Tag = subsystemID;
            int count = CalculateTotalAlarmCount(bTileInRealTimeMode, dtTile, tileType, subsystemID);

            txtSubValue.Text = count.ToString();
            if (count == 0)
                txtSubValue.Foreground = Brushes.Lime;
            else
                txtSubValue.Foreground = Brushes.Red;
        }

        void Chart_Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock txtTitle = sender as TextBlock;
            Chart chart = GetNearestChart(txtTitle);
            if (chart.Series[0].RenderAs == RenderAs.Column)
            {
                chart.Series[0].RenderAs = RenderAs.Pie;
                chart.View3D = true;
                chart.Legends[0].Enabled = true;
            }
            else
            {
                chart.Series[0].RenderAs = RenderAs.Column;
                chart.View3D = false;
                chart.Legends[0].Enabled = false;
            }
        }

        //click on location datapoint will jump to station detail page
        void Chart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Chart chart = (Chart)sender;
            for (int i = 0; i < chart.Series[0].DataPoints.Count; i++)
            {
                DataPoint dp = chart.Series[0].DataPoints[i];
                if (dp.Selected)
                {
                    bool bFirstStart = false;
                    if (stationDetailWindow == null)
                    {
                        stationDetailWindow = new IscsAlarmStationDetail();
                        stationDetailWindow.SetDashboardWindow(this);
                        bFirstStart = true;
                    }
                    stationDetailWindow.SetLocationId(dp.XValue.ToString());
                    stationDetailWindow.SetSystemkey(chart.Tag.ToString());
                    stationDetailWindow.Show();
                    if(bFirstStart == false)
                        stationDetailWindow.ReloadAllAlarmFromDb();     //When first start, LoadScript() in subwindow reload all alarm already, so no need double reload.
                    dp.Selected = false;                                //unselect datapoint to allow click to select again

                    this.Hide();
                }
            }
        }

        private void InitAll()
        {
            InitPanel1();
            InitTop10AlarmList();
            InitChart3();
            InitChart4();
            InitPanel5();
        }

        public void InitPanel1()
        {
            InitCmbSubSelect(cmbSubSelect1);
            InitCmbSubSelect(cmbSubSelect2);
            InitCmbSubSelect(cmbSubSelect3);
            dpDateTile.SelectedDate = DateTime.Now.Date;
            dpDateTile.SelectedDateChanged += dpDateTile_SelectedDateChanged;           //will call dpDateTile_SelectedDateChanged twice if put ahead of SelectedDate assignment, wpf BUG
            dpDateTile.DisplayDateStart = DateTime.Now.Date - new TimeSpan(180, 0, 0, 0);       //set history range to half year
            List<SystemInfo> tempList = systemList.OrderBy(p => p.OrderID).Take(9).ToList();
            for (int i = 0; i < 9; i++)            //block 1,2,3 reserved for subsystem
            {
                string labelControlName = "txtSubLabel" + (i + 4).ToString();
                TextBlock txtLabel = FindTextBlockByName(Panel1, labelControlName);
                if (i < tempList.Count)
                {
                    txtLabel.Text = tempList[i].Name;
                }
                else
                {
                    txtLabel.Text = "";
                }
                //txtBlockList.Where(p => p.Name == labelControlName).FirstOrDefault().Text = tempList[i].Name;

                string valueControlName = "txtSubValue" + (i + 4).ToString();
                TextBlock txtValue = FindTextBlockByName(Panel1, valueControlName);
                if (i < tempList.Count)
                {
                    txtValue.Text = "0";
                    txtValue.Foreground = Brushes.Lime;
                    txtValue.Tag = tempList[i].ID;
                }
                else
                {
                    txtValue.Text = "";
                }
            }
        }

        public void InitCmbSubSelect(ComboBox cmbSubSelect)
        {
            cmbSubSelect.SelectionChanged -= cmbSubSelect_SelectionChanged;
            cmbSubSelect.Items.Clear();
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = "Select";
                cmbSubSelect.Items.Add(item);
            }
            foreach (Subsystem iterator in allSubsystemList)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = iterator.Name;
                item.Tag = iterator.ID;
                cmbSubSelect.Items.Add(item);
            }
            cmbSubSelect.SelectedIndex = 0;
            cmbSubSelect.SelectionChanged += cmbSubSelect_SelectionChanged;
        }

        public void InitPanel5()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i >= severityList.Count)
                    break;

                List<Border> borderList = FindVisualChildren<Border>(Panel5).ToList();
                string borderControlName = "sevBorder" + (i + 1).ToString();
                Border border = borderList.Where(p => p.Name == borderControlName).FirstOrDefault();
                if (i < severityList.Count)
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severityList[i].Color));
                }

                string labelControlName = "txtSevLabel" + (i + 1).ToString();
                TextBlock txtLabel = FindTextBlockByName(Panel5, labelControlName);
                if (i < severityList.Count)
                {
                    txtLabel.Text = severityList[i].Name;
                }
                
                string valueControlName = "txtSevValue" + (i + 1).ToString();
                TextBlock txtValue = FindTextBlockByName(Panel5, valueControlName);
                if (i < severityList.Count)
                {
                    txtValue.Text = "0";
                }
            }
        }

        public void InitChart3()
        {
            //Init all location data to 0 for Chart3
            List<RawTable> data = new List<RawTable>();
            foreach (Location location in locationList)
            {
                Brush brush = IscsCachedMap.Instance.GetColorByLocationName(location.Name);
                if (brush != null)
                {
                    data.Add(new RawTable(location.Name, 0, brush));
                }
                else
                {
                    data.Add(new RawTable(location.Name, 0));
                }
            }
            dataSourceMap["Data3"].ReloadChartData(data);

            cmbSubsystem3.SelectionChanged -= cmbSubsystem3_SelectionChanged;
            cmbSubsystem3.Items.Clear();
            foreach (SystemInfo info in systemList)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = info.Name;
                item.Tag = info.ID;
                cmbSubsystem3.Items.Add(item);
            }
            cmbSubsystem3.SelectedIndex = cmbSubsystem3.Items.Count - 1;
            Chart3.Tag = systemList[cmbSubsystem3.SelectedIndex].ID;
            cmbSubsystem3.SelectionChanged += cmbSubsystem3_SelectionChanged;

            Chart3.View3D = true;
            Chart3.Series[0].SelectionEnabled = true;
            Chart3.Series[0].SelectionMode = SelectionModes.Single;
            //Chart3.ToolTipText = "Click chart for location detail view";
        }

        public void InitChart4()
        {
            //Init all location data to 0 for Chart4
            List<RawTable> data = new List<RawTable>();
            foreach (Location location in locationList)
            {
                Brush brush = IscsCachedMap.Instance.GetColorByLocationName(location.Name);
                if (brush != null)
                {
                    data.Add(new RawTable(location.Name, 0, brush));
                }
                else
                {
                    data.Add(new RawTable(location.Name, 0));
                }
            }
            dataSourceMap["Data4"].ReloadChartData(data);

            cmbSubsystem4.SelectionChanged -= cmbSubsystem4_SelectionChanged;
            cmbSubsystem4.Items.Clear();
            foreach (SystemInfo info in systemList)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = info.Name;
                item.Tag = info.ID;
                cmbSubsystem4.Items.Add(item);
            }
            cmbSubsystem4.SelectedIndex = cmbSubsystem4.Items.Count - 2;
            Chart4.Tag = systemList[cmbSubsystem4.SelectedIndex].ID;
            cmbSubsystem4.SelectionChanged += cmbSubsystem4_SelectionChanged;

            //Chart4.DataPointWidth = 2.0;
            Chart4.Series[0].SelectionEnabled = true;
            Chart4.Series[0].SelectionMode = SelectionModes.Single;
            Chart4.Series[0].Bevel = false;
            //Chart4.ToolTipText = "Click chart for location detail view";
        }

        public List<DataTrigger> CreateTriggersForAlarmCount()
        {
            List<DataTrigger> triggers = new List<DataTrigger>();
            DataTrigger trigger1 = new DataTrigger();
            trigger1.Value = "0";
            trigger1.Binding = new Binding() { Path = new PropertyPath("Count") };
            //trigger1.Setters.Add(new Setter(BackgroundProperty, Brushes.Blue));
            trigger1.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A568A"))));//2A568A,984807
            trigger1.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger1);
            return triggers;
        }

        public List<Trigger> CreateAlternatingTriggers()
        {
            List<Trigger> triggers = new List<Trigger>();
            Trigger trigger1 = new Trigger();
            trigger1.Value = 1;             //use 0 not "0", or will crash
            trigger1.Property = ItemsControl.AlternationIndexProperty;
            trigger1.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A568A"))));//2A568A,984807
            trigger1.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger1);
            return triggers;
        }

        public void InitTop10AlarmList()
        {
            top10AlarmList.Columns.Add(CreateTextBlockColumn("SEV", 40, "Severity"));
            top10AlarmList.Columns.Add(CreateTextBlockColumn("LOC", 40, "Location"));
            top10AlarmList.Columns.Add(CreateTextBlockColumn("DESCRIPTION", 295, "Description"));
            top10AlarmList.Columns.Add(CreateTextBlockColumn("TOTAL\r\n  NO.", 50, "Count"));
            top10AlarmList.ColumnHeaderHeight = 32d;

            Style style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4272c4"))));
            style.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));         //this parameter have no use here, don't know why
            //style.Setters.Add(new Setter(ToolTipService.ToolTipProperty, "Your tool tip here"));
            for (int i = 0; i < top10AlarmList.Columns.Count; i++)
            {
                top10AlarmList.Columns[i].HeaderStyle = style;
            }

            top10AlarmList.RowHeight = 42d;
            top10AlarmList.FontSize = 12;
            top10AlarmList.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            top10AlarmList.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

            tableTop10.Columns.Add("Severity");
            tableTop10.Columns.Add("Location");
            tableTop10.Columns.Add("Description");
            tableTop10.Columns.Add("Count");

            txtTop10Title.ToolTip = "Top 10 rule is configed in filter dialog";
        }

        /*public void InitDetailAlarmList(AlarmList subAlarmList)
        {
            subAlarmList.SetHorizontalScrollBarVisibility(ScrollBarVisibility.Hidden);
            subAlarmList.SetVerticalScrollBarVisibility(ScrollBarVisibility.Auto);

            //set header style
            Style style = new Style();
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            style.Setters.Add(new Setter(HeightProperty,18d));
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
            trigger1.Value = "0";
            trigger1.Binding = new Binding() { Path = new PropertyPath("Ack") };
            trigger1.Setters.Add(new Setter(BackgroundProperty, Brushes.Red));
            trigger1.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            subAlarmList.AddTriggerToItemContainerStyle(trigger1);
            DataTrigger trigger2 = new DataTrigger();
            trigger2.Value = "1";
            trigger2.Binding = new Binding() { Path = new PropertyPath("Ack") };
            trigger2.Setters.Add(new Setter(BackgroundProperty, Brushes.Green));
            trigger2.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            subAlarmList.AddTriggerToItemContainerStyle(trigger2);

            //add column for alarm list
            subAlarmList.gridView.Columns.Clear();
            subAlarmList.AddColumn("\tEquipment Code", 150, "EQPT", HorizontalAlignment.Left);
            subAlarmList.AddColumn("\t\t\tAlarm Description", 300, "Description", HorizontalAlignment.Left);
            subAlarmList.AddColumn("Severity", 100, "Severity", HorizontalAlignment.Left);
        }*/

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            AlarmFilterWindow filter = new AlarmFilterWindow(null);
            filter.ShowDialog();
        }

        private DataRow CreateNewRow(DataTable table, string severity, string location, string description, string count)
        {
            DataRow newRow = table.NewRow();
            newRow["severity"] = severity;
            newRow["location"] = location;
            newRow["description"] = description;
            newRow["count"] = count;
            return newRow;
        }

        //Clear all and reload all alarm from Db
        private void ReloadAllAlarmFromDb(bool bLoadHistory = false)
        {   //reset all data to 0
            alarmIDList = DAIHelper.Instance.GetAllAlarmIDFromDb(0);            //avoid duplicate, 0 mean IscsAlarm
            //allAlarmList = DAIHelper.Instance.GetTodayAtsAlarmListAll();   //load from database
            DateTime startTime = DateTime.Now.Date;
            allAlarmList = DAIHelper.Instance.GetAlarmListToday(0);  // 1 mean IscsAlarm
            activeAlarmList = allAlarmList.Where(p => p.state == "1").ToList();
            if (bLoadHistory)
            {
                EnsureLoadHistoryAlarm(true);
            }

            UpdateAll(activeAlarmList, new List<AtsAlarm>(), true);
        }

        //update top 10 alarm table from db, revenue time 00:30-04:30 not included
        private void UpdateTodayTop10AlarmTableFromDb()
        {
            string strCondition = "";
            if (excludeRevenueHour)
            {
                DateTime dtRvnStart = DateTime.Now.Date + new TimeSpan(0, 30, 0);
                DateTime dtRvnEnd = DateTime.Now.Date + new TimeSpan(4, 30, 0);
                strCondition = DAIHelper.Instance.CreateSQLBetweenAndExcludeRvenue(DateTime.Now.Date, DateTime.Now, dtRvnStart, dtRvnEnd);
            }
            else
            {
                strCondition = DAIHelper.Instance.CreateSQLBetween(DateTime.Now.Date, DateTime.Now);
            }
            string strRule = "";
            if (alarmFilter != null && alarmFilter.isEnabled == true)
            {
                strCondition += buildFilterCondition();
                strRule = buildTop10Rule(false);
                txtTop10Title.Text = "Top 10 Alarms (Filtered)";
                borderTop10Title.Background = Brushes.Goldenrod;
            }
            else
            {
                strRule = buildTop10Rule(true);
                txtTop10Title.Text = "Top 10 Alarms";
                borderTop10Title.Background = Brushes.Black;
            }
            DataTable dtResult = DAIHelper.Instance.GetTop10AlarmTableFromDb(strCondition, strRule, 0);
            if (dtResult != null)
            {
                tableTop10.Rows.Clear();
                //For each field in the table...
                foreach (DataRow row in dtResult.Rows)
                {
                    tableTop10.Rows.Add(CreateNewRow(tableTop10, row[0].ToString(), row[1].ToString(), row[2].ToString(), row[3].ToString()));
                }
            }
        }

        //for testing purpose only
        private void LoadDummyHistoryAlarm()
        {   //maybe change in future, currently use dummy data
            int seed1 = 0;
            {   //Dummy data for Chart3
                List<RawTable> data = new List<RawTable>();
                foreach (Location location in locationList)
                {
                    data.Add(new RawTable(location.Name, random.Next(0, 100)));
                }
                dataSourceMap["Data3"].ReloadChartData(data);
            }
            {   //Dummy data for Chart4
                List<RawTable> data = new List<RawTable>();
                foreach (Location location in locationList)
                {
                    data.Add(new RawTable(location.Name, random.Next(0, 100)));
                }
                dataSourceMap["Data4"].ReloadChartData(data);
            }
            {   //Dummy data for top 10 alarmlist
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "1", "DT02", "EB due to loss of speed codes when running in FBSS(NR)", "1"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "3", "DT03", "EB due to a common failure in both bearing sensors in FBSS(ATC Reset)", "2"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "2", "DT17", "EB due to a train movement detected in AM or CM FBSS with PSD open(NR)", "3"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "3", "DT18", "CBTC or FBSS signaling activation by ATC", "6"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "4", "DT18", "Status of Activation of FBSS by ATC", "20"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "5", "DT18", "Signaling changed to FBSS", "42"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "2", "DT03", "Train Stalled detected by ATP due to no FBSS and CBTC", "12"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "3", "DT03", "22kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading: 22kV SWITCH RM 2", "12"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "4", "DT03", "22kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading: 22kV SWITCH RM 2", "12"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "5", "DT03", "22kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading: 22kV SWITCH RM 2", "12"));
                //FillListViewWithDataTable(top10AlarmList.listView, tableTop10);
                FillDataGridWithDataTable(top10AlarmList, tableTop10);
            }
        }

        private void AddItemForSubsystemAlarmList(AlarmList almList, Rectangle rectangle, string EQPT, string Description, string Severity, string Ack, string ForeColor = null, string BackColor = null)
        {
            dynamic item = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)item;
            dictionary["EQPT"] = EQPT;
            dictionary["Description"] = Description;
            dictionary["Severity"] = Severity;
            dictionary["Ack"] = Ack;
            if (ForeColor != null)
                dictionary["ForeColor"] = ForeColor;
            if (ForeColor != null)
                dictionary["BackColor"] = BackColor;
            almList.listView.Items.Insert(0, item);
            if (Ack == "0")
                rectangle.Fill = Brushes.Red;
            else
                rectangle.Fill = Brushes.Green;

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

        private void AdjustAxisYMaximumInternal(Chart chart)
        {
            /*int AxisMaxOri = 0;
            if(chart.AxesY[0].AxisMaximum != null)
                AxisMaxOri = Convert.ToInt32(chart.AxesY[0].AxisMaximum);
            int AxisMax = AxisMaxOri;*/
            int AxisMax = 0;
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
            //if (AxisMax > AxisMaxOri)
                chart.AxesY[0].AxisMaximum = AxisMax;
        }

        private void AdjustAxisYMaximumByNewValue(Chart chart, double newValue)
        {
            AdjustAxisYMaximumByNewValue(chart, Convert.ToInt32(newValue));
        }

        private void AdjustAxisYMaximumByNewValue(Chart chart, int newValue)
        {
            int oriYMax = 0;
            if (chart.AxesY[0].AxisMaximum != null)
            {
                oriYMax = Convert.ToInt32(chart.AxesY[0].AxisMaximum);
            }
            if (newValue > oriYMax)
                chart.AxesY[0].AxisMaximum = GetSuitableMaxforValue(newValue);
        }

        private string GetToolTipTextForSeverity(List<AtsAlarm> alarmList, string severityID)
        {
            string label = null;
            var groupResult = alarmList.GroupBy(p => p.systemID).Select(p => new { Metirc = p.Key, Count = p.Count() }).ToList();
            int sumCount = groupResult.Sum(p => p.Count);

            foreach (var group in groupResult)
            {
                string percent = Math.Round(group.Count * 100.0 / sumCount, 0) .ToString();
                label += IscsCachedMap.Instance.GetSystemByID(group.Metirc).ShortName + " - " + group.Count + " (" + percent + "%)\r\n";
            }
            return label;
        }

        private TextBlock FindTextBlockByName(DependencyObject parentPanel, string Name)
        {
            List<TextBlock> txtBlockList = FindVisualChildren<TextBlock>(parentPanel).ToList();
            return txtBlockList.Where(p => p.Name == Name).FirstOrDefault();
        }

        //if this function is called, must be in real time mode
        private void UpdatePanel1(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList)
        {
            LogListSystem(upAlarmList, downAlarmList);
            for (int i = 0; i < 12; i++)
            {
                if (i >= systemList.Count)
                    break;

                //first get 
                string valueControlName = "txtSubValue" + (i + 1).ToString();
                TextBlock txtValue = FindTextBlockByName(Panel1, valueControlName);
                int upCount = 0;
                int downCount = 0;
                int totalCount = 0;
                if (txtValue.Tag != null)
                {
                    string ID = txtValue.Tag.ToString();
                    List<string> subIDList = new List<string>();
                    if (i < 3)
                    {   //ID is subsystemID
                        subIDList.Add(ID);
                    }
                    else
                    {   //ID is systemID
                        SystemInfo systemInfo = systemList.Where(p => p.ID == ID).FirstOrDefault();
                        subIDList = systemInfo.subsystemList.Select(p => p.ID).ToList();
                    }
                    upCount = upAlarmList.Where(p => subIDList.Contains(p.subsystemID)).Count();
                    downCount = downAlarmList.Where(p => subIDList.Contains(p.subsystemID)).Count();
                    //totalCount = activeAlarmList.Where(p => subIDList.Contains(p.subsystemID)).Count();
                    /*if (bReset)
                    {
                        int historyCount = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID)).Sum(p => p.count);
                        upCount += historyCount;
                    }*/
                }
                UpdateTextBlock(txtValue, upCount, downCount);
                if (i == 3)
                {
                    DebugUtil.Instance.LOG.Debug(valueControlName + "=" + txtValue.Text + ", upCount=" + upCount + ", downCount=" + downCount);
                    if (txtValue.Text != totalCount.ToString())
                    {
                        DebugUtil.Instance.LOG.Error("wrong");
                    }
                }

                if (txtValue.Text == "0")
                    txtValue.Foreground = Brushes.Lime;
                else
                    txtValue.Foreground = Brushes.Red;
            }
        }

        private void UpdatePanel5(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            //LogList(upAlarmList, downAlarmList);
            for (int i = 0; i < 5; i++)
            {
                if (i >= severityList.Count)
                    break;

                int upCount = upAlarmList.Where(p => p.severityID == severityList[i].ID).Count();
                int downCount = downAlarmList.Where(p => p.severityID == severityList[i].ID).Count();

                string valueControlName = "txtSevValue" + (i + 1).ToString();
                TextBlock txtValue = FindTextBlockByName(Panel5, valueControlName);
                UpdateTextBlock(txtValue, upCount, downCount, bReset);
                //DebugUtil.Instance.LOG.Debug(valueControlName + "=" + txtValue.Text + ", upCount=" + upCount + ", downCount=" + downCount + ", bReset=" + bReset);
            }
        }

        private void LogListSeverity(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList)
        {
            string log = "";
            log += "upAlarmList severity:location=" + String.Join(",", upAlarmList.Select(p => p.severityID + ":" + p.locationId));
            log += "; downAlarmList severity=" + String.Join(",", downAlarmList.Select(p => p.severityID + ":" + p.locationId));
            DebugUtil.Instance.LOG.Debug(log);
        }

        private void LogListSystem(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList)
        {
            string log = "";
            log += "upAlarmList systemID=" + String.Join(",", upAlarmList.Select(p => p.systemID));
            log += "; downAlarmList systemID=" + String.Join(",", downAlarmList.Select(p => p.systemID));
            DebugUtil.Instance.LOG.Debug(log);
        }

        //private void UpdateTop10AlarmList(List<AtsAlarm> newAlarmList)
        private void UpdateTop10AlarmList()
        {
            nlastUpdateHour = DateTime.Now.Hour;
            UpdateTodayTop10AlarmTableFromDb();
            //FillListViewWithDataTable(top10AlarmList.listView, tableTop10);
            FillDataGridWithDataTable(top10AlarmList, tableTop10);
        }

        private void UpdateChart3(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            List<string> subIDList = GetComboBoxSelectedSubsystemIDList(cmbSubsystem3);
            for (int i = 0; i < locationList.Count; i++)
            {   //system
                Location location = locationList[i];
                int upCount = upAlarmList.Where(p => p.locationId == location.Name && subIDList.Contains(p.subsystemID)).Count();
                int downCount = downAlarmList.Where(p => p.locationId == location.Name && subIDList.Contains(p.subsystemID)).Count();

                DataPoint dp = Chart3.Series[0].DataPoints[i];      //each location is a datapoint
                UpdateDatapoint(dp, upCount, downCount, bReset);
            }
            AdjustAxisYMaximumInternal(Chart3);
        }

        private void UpdateChart4(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            List<string> subIDList = GetComboBoxSelectedSubsystemIDList(cmbSubsystem4);
            for (int i = 0; i < locationList.Count; i++)
            {   //system
                Location location = locationList[i];
                int upCount = upAlarmList.Where(p => p.locationId == location.Name && subIDList.Contains(p.subsystemID)).Count();
                int downCount = downAlarmList.Where(p => p.locationId == location.Name && subIDList.Contains(p.subsystemID)).Count();

                DataPoint dp = Chart4.Series[0].DataPoints[i];      //each location is a datapoint
                UpdateDatapoint(dp, upCount, downCount, bReset);
            }
            AdjustAxisYMaximumInternal(Chart4);
        }

        //for subsystem comboBox
        private List<string> GetComboBoxSelectedSubsystemIDList(ComboBox comboBox)
        {
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            string systemID = item.Tag.ToString();
            SystemInfo info = systemList.Where(p => p.ID == systemID).FirstOrDefault();
            List<string> subIDList = info.subsystemList.Select(p => p.ID).ToList();
            return subIDList;
        }
    
        private void UpdateSubsystem(Chart chart, List<AtsAlarm> newAlarmList)
        {
            int index  = Convert.ToInt32(chart.Name.Substring(chart.Name.Length - 1, 1));
            SystemInfo systemInfo = systemList[index - 1];
            List<Subsystem> list = systemInfo.subsystemList;
            for (int i = 0; i < list.Count; i++)
            {   //itinerary subsystem
                Subsystem subsystem = list[i];
                int newCount = newAlarmList.Where(p => p.subsystemID == subsystem.ID).Count();
                if (newCount == 0)
                    continue;
                int ID = Convert.ToInt32(subsystem.ID);
                DataPoint dp = chart.Series[0].DataPoints.Where(p => Convert.ToInt32(p.Tag) == ID).FirstOrDefault();
                if (dp == null)
                {
                    //Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(systemInfo.Color));
                    dataSourceMap["Data3_" + index].AddDataPoint(subsystem.Name, newCount, subsystem.brush, ID);
                }
                else
                {
                    int oldValue = Convert.ToInt32(dp.YValue);
                    dp.YValue = oldValue + newCount;
                }
            }
        }
        /*private void UpdateSubsystem(Chart chart, List<AtsAlarm> newAlarmList)
        {
            for (int i = 0; i < chart.Series[0].DataPoints.Count; i++)
            {   //subsystem1
                string subystemName = chart.Series[0].DataPoints[i].XValue.ToString();
                string subsystemID = allSubsystemList.Where(p => p.Name == subystemName).FirstOrDefault().ID;
                int oldValue = Convert.ToInt32(chart.Series[0].DataPoints[i].YValue);
                //int newCount = allSubsystemList.Where(p => p.Name == subystemName).Count();
                int newCount = newAlarmList.Where(p => p.subsystemID == subsystemID).Count();
                chart.Series[0].DataPoints[i].YValue = oldValue + newCount;
            }
        }*/

        /*private void UpdateDetailAlarmList(List<AtsAlarm> newAlarmList, bool reset = false)
        {
            if (reset == true)
            {
                alarmList1.listView.Items.Clear();
                alarmList2.listView.Items.Clear();
                alarmList3.listView.Items.Clear();
                alarmList4.listView.Items.Clear();
            }
            foreach(AtsAlarm alarm in newAlarmList)
            {
                if (alarm.systemID == systemList[0].ID)
                {   //alarmList1: Rolling Stock
                    AddItemForSubsystemAlarmList(alarmList1, Rectangle1, alarm.assetName, alarm.description, alarm.severityID, alarm.Ack);
                }
                else if (alarm.systemID == systemList[1].ID)
                {   //alarmList2: Signaling
                    AddItemForSubsystemAlarmList(alarmList2, Rectangle2, alarm.assetName, alarm.description, alarm.severityID, alarm.Ack);
                }
                else if (alarm.systemID == systemList[2].ID)
                {   //alarmList3: PSD
                    AddItemForSubsystemAlarmList(alarmList3, Rectangle3, alarm.assetName, alarm.description, alarm.severityID, alarm.Ack);
                }
                else if (alarm.systemID == systemList[3].ID)
                {   //alarmList4: Communication
                    AddItemForSubsystemAlarmList(alarmList4, Rectangle4, alarm.assetName, alarm.description, alarm.severityID, alarm.Ack);
                }
            }
        }*/

        //for real time update, history mode, some module will ignore this
        private void UpdateAll(List<AtsAlarm> upAlarmList, List<AtsAlarm> downAlarmList, bool bReset = false)
        {
            DebugUtil.Instance.LOG.Debug("UpdateAll start, real time mode: " + bTileInRealTimeMode);
            if (bTileInRealTimeMode)        //Panel1, Char3 and Chart4 no need update in history mode because it will be fixed. other module have no history mode
            {
                if (bReset == false)
                {
                    UpdatePanel1(upAlarmList, downAlarmList);
                    DebugUtil.Instance.LOG.Debug("UpdatePanel1 complete");
                    UpdateChart3(upAlarmList, downAlarmList);
                    DebugUtil.Instance.LOG.Debug("UpdateChart3 complete");
                    UpdateChart4(upAlarmList, downAlarmList);
                    DebugUtil.Instance.LOG.Debug("UpdateChart4 complete");
                }
                else
                {
                    ResetRealTimeAndHistoryPanel(dtTile, tileType);
                }
            }
            
            //if (upAlarmList.Count > 0 || downAlarmList.Count > 0 || bReset == true)
            if(nlastUpdateHour != DateTime.Now.Hour || bReset == true)
            {
                UpdateTop10AlarmList();
            }
            DebugUtil.Instance.LOG.Debug("UpdateTop10AlarmList complete");
            UpdatePanel5(upAlarmList, downAlarmList, bReset);
            DebugUtil.Instance.LOG.Debug("UpdatePanel5 complete");

            if (stationSummaryWindow != null)
            {
                stationSummaryWindow.UpdateAll(upAlarmList, downAlarmList, bReset);
            }
            DebugUtil.Instance.LOG.Debug("Update stationSummaryWindow complete");
            if (stationDetailWindow != null)
            {
                stationDetailWindow.UpdateAll(upAlarmList, downAlarmList, bReset);
            }
            DebugUtil.Instance.LOG.Debug("Update stationDetailWindow complete");
        }

        void LoadScript()
        {
            InitAll();          //Put InitAll here because LoadScript execute after binding all Chart to data, the Axes and legend created already
            if (IscsCachedMap.Instance.LocalTest == false)
            {
                DebugUtil.Instance.LOG.Info("Reload all alarms from db");
                ReloadAllAlarmFromDb(true);           //this function will execute in timerPoll_Tick for the first time
            }
            else
            {
                DebugUtil.Instance.LOG.Info("Load dummy alarms");
                LoadDummyHistoryAlarm();
            }
            if (!Directory.Exists(AlarmFilePath))
            {
                DebugUtil.Instance.LOG.Error("Alarm file path not exist!");
            }

            timerPoll = new DispatcherTimer();
            timerPoll.Interval = TimeSpan.FromMilliseconds(pollInterval);
            timerPoll.Tick += new EventHandler(timerPoll_Tick);
            timerPoll.Start();

            timerDelay = new DispatcherTimer();
            timerDelay.Interval = TimeSpan.FromMilliseconds(100);
            timerDelay.Tick += timerDelay_Tick;
            timerDelay.Start();
        }

        void timerDelay_Tick(object sender, EventArgs e)
        {
            Chart3.Series[0].RenderAs = RenderAs.Pie;           //Is use Pie or Doughnut in the beginning and all DataPoint value is 0, this chart will become not selectable and not clickable
            timerDelay.Stop();
        }

        private int previousDay = DateTime.Now.Day;
        private int previousMinute = DateTime.Now.Minute;
        private void timerPoll_Tick(object sender, EventArgs e)
        {
            //check if a new day start, if a new day start need reset all and check archive
            if (DateTime.Now.Day != previousDay)
            {
                DebugUtil.Instance.LOG.Info("A new day start");
                ReloadAllAlarmFromDb(true);
                DebugUtil.Instance.LOG.Info("Reset all");
                //every midnight check db and delete archive max than 60 days
                //int temp = CommonFunction.ConvertDateTimeLocalToUnixTime(DateTime.Now - new TimeSpan(archiveKeepDays, 0, 0, 0));
                //DAIHelper.Instance.DeleteAlarmBeforeTimeStamp(temp);
                previousDay = DateTime.Now.Day;
                nRetry = 0;
                //DebugUtil.Instance.LOG.Info("Delete alarm exceed " + archiveKeepDays + " days");
            }
            else if (DateTime.Now.Minute != previousMinute)
            {
                DebugUtil.Instance.LOG.Info("ReloadAll for synchronize");
                ReloadAllAlarmFromDb();
                //every midnight check db and delete archive max than 60 days
                //int temp = CommonFunction.ConvertDateTimeLocalToUnixTime(DateTime.Now - new TimeSpan(archiveKeepDays, 0, 0, 0));
                //DAIHelper.Instance.DeleteAlarmBeforeTimeStamp(temp);
                previousMinute = DateTime.Now.Minute;
            }
            else
            {
                //DateTime startTime = DateTime.Now.Date;
                //List<AtsAlarm> newAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, DateTime.Now, 0, "Alarm_New");  // 0 mean IscsAlarm
                List<AtsAlarm> newAlarmList = DAIHelper.Instance.GetAlarmListToday(0, "Alarm_New");  // 0 mean IscsAlarm
                DebugUtil.Instance.LOG.Debug("Read total " + newAlarmList.Count() + " new alarms");

                //to improve performance, need classify alarmList
                if (newAlarmList != null && newAlarmList.Count() > 0)
                {
                    List<AtsAlarm> upAlarmList = new List<AtsAlarm>();
                    List<AtsAlarm> downAlarmList = new List<AtsAlarm>();
                    foreach (AtsAlarm newAlarm in newAlarmList)
                    {
                        //if found same alarmID: 
                        //1. old close, new close, no change; 2. old open, new open, no change; 3. old close, new open, count + 1; 4 old open, new close, count - 1
                        if (alarmIDList.Contains(newAlarm.alarmID))
                        {   //duplicate alarmID, just update exist alarm, usually used for alarm close
                            int index = allAlarmList.FindIndex(p => p.alarmID == newAlarm.alarmID);
                            AtsAlarm oldAlarm = allAlarmList[index];
                            //AtsAlarm updatedAlarm = oldAlarm;
                            DebugUtil.Instance.LOG.Debug("found alarmID=" + newAlarm.alarmID + " in exist alarmList, oldAlarm state=" + oldAlarm.state);
                            DebugUtil.Instance.LOG.Debug("oldAlarm.state=" + oldAlarm.state + ", newAlarm.state=" + newAlarm.state);
                            if (isClose(oldAlarm) && !isClose(newAlarm))
                            {
                                oldAlarm.state = newAlarm.state;
                                upAlarmList.Add(oldAlarm);
                                DebugUtil.Instance.LOG.Debug("insert into upAlarmList");
                            }
                            else if (!isClose(oldAlarm) && isClose(newAlarm))
                            {
                                oldAlarm.state = newAlarm.state;
                                downAlarmList.Add(oldAlarm);
                                DebugUtil.Instance.LOG.Debug("insert oldAlarm to downAlarmList");
                            }
                            //oldAlarm = newAlarm;                 //update alarm status in allAlarmList, but it doesnt work
                            //allAlarmList[index] = newAlarm;       //this will cause logic confuse if new alarm totally different with old alarm only alarmID duplicate
                            //allAlarmList[index].closeTime = newAlarm.closeTime;         //AlarmAnalysisService also only update closeTime
                        }
                        //if not found same alarmID: 1. new close, no change; 2 new open, count + 1
                        else
                        {
                            DebugUtil.Instance.LOG.Debug("newAlarm ID=" + newAlarm.alarmID + ", newAlarm.state=" + newAlarm.state);
                            if (!isClose(newAlarm))
                            {
                                upAlarmList.Add(newAlarm);
                                DebugUtil.Instance.LOG.Debug("insert newAlarm to upAlarmList");
                            }
                            allAlarmList.Add(newAlarm);
                            alarmIDList.Add(newAlarm.alarmID);
                        }
                    }
                    activeAlarmList = allAlarmList.Where(p => p.state == "1").ToList();
                    //UpdateAll(activeAlarmList, newAlarmList, false);         //only count exist alarm
                    if (upAlarmList.Count > 0 || downAlarmList.Count > 0)
                    {
                        UpdateAll(upAlarmList, downAlarmList);         //count all alarm
                        DebugUtil.Instance.LOG.Debug("Update GUI complete");
                    }
                    else
                    {
                        DebugUtil.Instance.LOG.Debug("No Update for GUI");
                    }
                }
            }
            /*DebugUtil.Instance.LOG.Debug("Read total " + newAlarmList.Count() + " new alarms");

            if (newAlarmList != null && newAlarmList.Count() > 0)
            {
                allAlarmList = allAlarmList.Concat(newAlarmList).ToList();
                UpdateAll(newAlarmList);
                DebugUtil.Instance.LOG.Debug("Update GUI complete");
            }*/
        }

        private bool isClose(AtsAlarm alarm)
        {
            if (alarm.state == "0")
                return true;
            else
                return false;
        }

        /*private void ArchiveFiles(string[] Files)
        {
            if (Files == null || Files.Count() == 0)
                return;
            SaveFilesToDatabase("Alarm", Files);
            foreach (string fileName in Files)
                File.Delete(fileName);
        }

        private void SaveFileToDatabase(string tablename, string fileName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);
            XmlNodeList nodeList = xmlDoc.SelectNodes("/body/alarm");
            DataTable dtContent = new DataTable();
            dtContent.Clear();
            for (int i = 0; i < AtsAlarm.columnNames.Count(); i++)
            {
                string columnName = AtsAlarm.columnNames[i];
                dtContent.Columns.Add(columnName);
            }
            foreach (XmlNode alarmNode in nodeList)
            {
                DataRow row = dtContent.NewRow();
                foreach (XmlNode childNode in alarmNode.ChildNodes)
                {
                    string value = childNode.InnerText;
                    if (childNode.Name == "sourceTime")
                    {
                        value = childNode.FirstChild.InnerText;
                    }
                    row[childNode.Name] = value;
                }
                dtContent.Rows.Add(row);
            }
            try
            {
                DAIHelper.Instance.DbServer.WriteDatatableToDb(tablename, dtContent);
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Exception catched when SaveFileToDatabase: " + ex.ToString());
            }
        }

        private void SaveFilesToDatabase(string tablename, string[] Files)
        {
            if (Files == null || Files.Count() == 0)
                return;
            DataTable dtContent = new DataTable();
            for (int i = 0; i < AtsAlarm.columnNames.Count(); i++)
            {
                string columnName = AtsAlarm.columnNames[i];
                dtContent.Columns.Add(columnName);
            }
            foreach (string fileName in Files)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                XmlNodeList nodeList = xmlDoc.SelectNodes("/body/alarm");
                foreach (XmlNode alarmNode in nodeList)
                {
                    DataRow row = dtContent.NewRow();
                    foreach (XmlNode childNode in alarmNode.ChildNodes)
                    {
                        string value = childNode.InnerText;
                        if (childNode.Name == "sourceTime")
                        {
                            value = childNode.FirstChild.InnerText;
                        }
                        row[childNode.Name] = value;
                    }
                    dtContent.Rows.Add(row);
                }
            }
            try
            {
                DAIHelper.Instance.DbServer.WriteDatatableToDb(tablename, dtContent);
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Exception catched when SaveFileToDatabase: " + ex.ToString());
            }
        }*/

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            DAIHelper.Instance.DeleteAlarmForToday();
            MessageBox.Show("Clear today alarm completed!");
            ReloadAllAlarmFromDb();
        }

        private void txtFilter_Click(object sender, RoutedEventArgs e)
        {
            //testWin filter = new testWin();
            //filter.Show();
            if (IscsCachedMap.Instance.PopupFilter == true)
            {
                filterDlg = new AlarmFilterWindow1(alarmFilter, IscsCachedMap.Instance);
                if (filterDlg.ShowDialog() == true)
                {
                    FillFilterByFilterGUI(alarmFilter, filterDlg);
                    UpdateTop10AlarmList();
                }
            }
            else
            {
                if (filterDlg == null)
                {
                    filterDlg = new AlarmFilterWindow1(alarmFilter, IscsCachedMap.Instance);
                }
                filterDlg.SetMainWindow(this);
                filterDlg.Show();
                this.Hide();
            }
        }

        private void txtClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (alarmFilter.isEnabled == true)
            {
                alarmFilter.isEnabled = false;
                UpdateTop10AlarmList();
            }
        }

        public void FilterCallback(bool update)
        {
            this.Show();
            if (update == true)
            {
                FillFilterByFilterGUI(alarmFilter, filterDlg);
                UpdateTop10AlarmList();
            }
        }

        private void FillFilterByFilterGUI(AlarmFilter alarmFilter, AlarmFilterWindow1 filterDlg)
        {
            alarmFilter.selectedSystems = filterDlg.selectedSystems;
            alarmFilter.selectedSubsystems = filterDlg.selectedSubsystems;
            alarmFilter.selectedLocations = filterDlg.selectedLocations;
            alarmFilter.selectedSeveritys = filterDlg.selectedSeveritys;
            alarmFilter.enableDateTime = filterDlg.enableDateTime;
            alarmFilter.dtStart = filterDlg.dtStart;
            alarmFilter.dtEnd = filterDlg.dtEnd;
            alarmFilter.selectedRules = filterDlg.selectedRules;
            alarmFilter.isEnabled = filterDlg.isEnabled;
        }
        
        private string buildFilterCondition()
        {
            string strCondition = "";
            if (alarmFilter != null)
            {
                if (!alarmFilter.selectedSystems.Contains("All") && !alarmFilter.selectedSubsystems.Contains("All"))
                {
                    IEnumerable<Subsystem> Subsystems1 = systemList.Where(p => alarmFilter.selectedSystems.Contains(p.ShortName)).SelectMany(p => p.subsystemList).Distinct();
                    IEnumerable<Subsystem> Subsystems2 = allSubsystemList.Where(p => alarmFilter.selectedSubsystems.Contains(p.Name));
                    IEnumerable<string> subIDs = Subsystems1.Union(Subsystems2).Select(p => p.ID);
                    strCondition += " and subsystemkey in ('" + String.Join("','", subIDs) + "')";
                }
                if (locationList.Count > 0 && !alarmFilter.selectedLocations.Contains("All"))
                {
                    strCondition += " and locationId in ('" + String.Join("','", alarmFilter.selectedLocations) + "')";
                }
                if (severityList.Count > 0 && !alarmFilter.selectedSeveritys.Contains("All"))
                {
                    strCondition += " and alarmSeverity in ('" + String.Join("','", alarmFilter.selectedSeveritys) + "')";
                }
                if (alarmFilter.enableDateTime)
                {
                    DateTime dtStart = alarmFilter.dtStart;
                    DateTime dtEnd = alarmFilter.dtEnd;
                    strCondition += DAIHelper.Instance.CreateSQLBetween(dtStart, dtEnd);
                }
            }
            /*else
            {
                DateTime dtStart = DateTime.Now.Date;// -new TimeSpan(1, 0, 0, 0);
                DateTime dtEnd = DateTime.Now;
                strCondition += DAIHelper.Instance.CreateSQLBetween(dtStart, dtEnd);
            }*/
            //string sqlstr = "select alarmDescription, alarmSeverity, assetName, count(*) as count from alarm where " + strCondition + " group by alarmDescription, alarmSeverity, assetName order by count desc limit 0,10";

            return strCondition;
        }


        private string buildTop10Rule(bool defaultRule = true)
        {
            string strRule = "";
            if (alarmFilter != null && defaultRule == false)
            {
                strRule += String.Join(",", alarmFilter.selectedRules);
            }
            else
            {
                //strRule += "alarmDescription, alarmSeverity, assetName";
                strRule += String.Join(",", AlarmFilter.ruleIscs);
            }

            return strRule;
        }

        private void btnStationSummary_Click(object sender, RoutedEventArgs e)
        {
            bool bFirstStart = false;
            if (stationSummaryWindow == null)
            {
                stationSummaryWindow = new IscsAlarmStationSummary();
                stationSummaryWindow.SetDashboardWindow(this);
                bFirstStart = true;
            }
            stationSummaryWindow.Show();
            if (bFirstStart == false)
                stationSummaryWindow.ReloadAllAlarmFromDb();        //When first start, LoadScript() in subwindow reload all alarm already, so no need double reload.

            this.Hide();
        }

        private void cmbSubsystem3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (sender as ComboBox).SelectedItem as ComboBoxItem;
            string subsystemID = item.Tag.ToString();
            Chart3.Tag = subsystemID;
            if (IscsCachedMap.Instance.LocalTest == true)
            {
                for (int i = 0; i < Chart3.Series[0].DataPoints.Count; i++)
                {
                    Chart3.Series[0].DataPoints[i].YValue = random.Next(0, 100);
                }
            }
            else
            {
                //UpdateChart3(activeAlarmList, new List<AtsAlarm>(), true);
                ResetChart3(dtTile, tileType);
            }
        }

        private void cmbSubsystem4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (sender as ComboBox).SelectedItem as ComboBoxItem;
            string subsystemID = item.Tag.ToString();
            Chart4.Tag = subsystemID;
            if (IscsCachedMap.Instance.LocalTest == true)
            {
                for (int i = 0; i < Chart4.Series[0].DataPoints.Count; i++)
                {
                    Chart4.Series[0].DataPoints[i].YValue = random.Next(0, 100);
                }
            }
            else
            {
                //UpdateChart4(activeAlarmList, new List<AtsAlarm>(), true);
                ResetChart4(dtTile, tileType);
            }
        }

        private void imgPump_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (pumpWindow == null)
            {
                pumpWindow = new PumpDashboard();
                pumpWindow.SetDashboardWindow(this);
                bFirstStart = true;
            }
            pumpWindow.Show();
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

        private void imgLogoAts_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process ps = new Process();
            string filename = "F:/WPF/EMS/AlarmAnalysis/Source/AlarmAnalysis/code/AtsAlarmAnalysis/bin/Release/AtsAlarmAnalysis.exe";
            string workingDir = "F:/WPF/EMS/AlarmAnalysis/Source/AlarmAnalysis/code/AtsAlarmAnalysis/bin/Release/";
            ps.StartInfo.FileName = filename;
            ps.StartInfo.WorkingDirectory = workingDir;
            ps.Start();
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void dpDateTile_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            dtTile = dpDateTile.SelectedDate.Value.Date;
            UpdateTileMode(dtTile, tileType);
        }

        private void cmbTileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = cmbTileType.SelectedItem as ComboBoxItem;
            if (item.Tag != null)
            {
                tileType = item.Tag.ToString();
                UpdateTileMode(dtTile, tileType);
            }
        }

        private void UpdateTileMode(DateTime dtTile, string tileType)
        {
            bool bModeOld = bTileInRealTimeMode;
            EnsureLoadHistoryAlarm();
            switch (tileType)
            {
                case "Daily":
                    if (dpDateTile.SelectedDate.Value < DateTime.Now.Date)
                    {
                        bTileInRealTimeMode = false;
                    }
                    else
                    {
                        bTileInRealTimeMode = true;
                    }
                    break;
                case "Weekly":
                    CultureInfo cultureInfo = new CultureInfo("en-SG");
                    System.Globalization.Calendar cal = cultureInfo.Calendar;
                    int tileWeekNumber = cal.GetWeekOfYear(dtTile, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    int nowWeekNumber = cal.GetWeekOfYear(DateTime.Now.Date, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                    if (tileWeekNumber < nowWeekNumber)
                    {
                        bTileInRealTimeMode = false;
                    }
                    else
                    {
                        bTileInRealTimeMode = true;
                    }

                    break;
                case "Monthly":
                    if (dpDateTile.SelectedDate.Value.Month < DateTime.Now.Date.Month)
                    {
                        bTileInRealTimeMode = false;
                    }
                    else
                    {
                        bTileInRealTimeMode = true;
                    }
                    break;
            }

            if (dtTile != dtTileOld || tileType != tileTypeOld)
            {
                ResetRealTimeAndHistoryPanel(dtTile, tileType);
                dtTileOld = dtTile;
                tileTypeOld = tileType;
            }
        }

        private void ParseHistoryAlarm()
        {
            historyActiveAlarmCountList.Clear();
            for(int i = 0; i < 180; i++)
            {
                DateTime dtEnd = DateTime.Today.AddDays(-i);
                DateTime dtStart = dtEnd.AddDays(-1);
                /*var query = historyAlarmList.Where(p => p.sourceTime >= dtStart && p.sourceTime < dtEnd).GroupBy(
                    p => p.subsystemID,
                    p => p.locationId,
                    (subsystemID, locationId) => new
                    {
                        dateCreated = dtStart,
                        subsystemID = subsystemID,
                        locationId = locationId,
                        count = subsystemID.Count()
                    });*/
                var groups = historyAlarmList.Where(p => p.sourceTime >= dtStart && p.sourceTime < dtEnd).GroupBy(p => new {p.subsystemID, p.locationId});

                foreach (var group in groups)
                {
                    IscsAlarmCount item = new IscsAlarmCount();
                    item.dateCreated = dtStart;
                    item.subsystemID = group.Key.subsystemID;
                    item.locationId = group.Key.locationId;
                    item.count = group.Count();
                    historyActiveAlarmCountList.Add(item);
                }
            }
        }

        private bool EnsureLoadHistoryAlarm(bool bForce = false)
        {
            if ((bYesterdayAlarmLoaded == false && nRetry < 3) || bForce == true)
            {
                historyAlarmList = DAIHelper.Instance.GetAlarmListHistory(0).Where(p => p.state == "1").ToList();
                ParseHistoryAlarm();
                historyAlarmList.Clear();

                DateTime yesterday = DateTime.Today.AddDays(-1);
                int nCount = historyActiveAlarmCountList.Count(p => p.dateCreated == yesterday);
                if ( nCount == 0)       //yesterday alarm count not loaded
                {
                    bYesterdayAlarmLoaded = false;
                }
                else if(nCount == 1)
                {
                    bYesterdayAlarmLoaded = true;
                }
                /*else
                {
                    bYesterdayAlarmLoaded = true;
                    DebugUtil.log.Error("There are " + nCount + " history alarm count in list");
                }*/

                nRetry++;
            }

            return bYesterdayAlarmLoaded;
        }

        private int CalculateTotalAlarmCount(bool bTileInRealTimeMode, DateTime dtTile, string tileType, string subsystemID, string locationId = "")
        {
            List<string> subIDList = new List<string>();
            subIDList.Add(subsystemID);
            int count = CalculateTotalAlarmCount(bTileInRealTimeMode, dtTile, tileType, subIDList, locationId);
            subIDList.Clear();
            subIDList = null;
            return count;
        }

        private int CalculateTotalAlarmCount(bool bTileInRealTimeMode, DateTime dtTile, string tileType, List<string> subIDList, string locationId = "")
        {
            int count = GetHistoryActiveAlarmCount(dtTile, tileType, subIDList, locationId);
            if (bTileInRealTimeMode)
                count += activeAlarmList.Where(p => subIDList.Contains(p.subsystemID) && (locationId == "" ? true : p.locationId == locationId)).Count();      //realtime need include today data

            return count;
        }

        //tileType: Daily, Weekly, Monthly specified in the dtTile
        private int GetHistoryActiveAlarmCount(DateTime dtTile, string tileType, List<string> subIDList, string locationId = "")
        {
            EnsureLoadHistoryAlarm();
            int count = 0;
            switch (tileType)
            {
                case ("Daily"):
                    if(locationId == "")
                        count = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID) && p.dateCreated == dtTile).Count();
                    else
                        count = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID) && p.dateCreated == dtTile && p.locationId == locationId).Count();
                    break;
                case ("Weekly"):
                    CultureInfo myCI = new CultureInfo("en-SG");
                    System.Globalization.Calendar mycal = myCI.Calendar;
                    int weekNum = mycal.GetWeekOfYear(dtTile, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    if (locationId == "")
                        count = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID) && mycal.GetWeekOfYear(p.dateCreated, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == weekNum).Count();
                    else
                        count = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID) && mycal.GetWeekOfYear(p.dateCreated, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == weekNum && p.locationId == locationId).Count();
                    break;
                case ("Monthly"):
                    if (locationId == "")
                        count = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID) && p.dateCreated.Month == dtTile.Month).Count();
                    else
                        count = historyActiveAlarmCountList.Where(p => subIDList.Contains(p.subsystemID) && p.dateCreated.Month == dtTile.Month && p.locationId == locationId).Count();
                    break;
            }
            return count;
        }

        private void ResetRealTimeAndHistoryPanel(DateTime dtTile, string tileType)
        {
            EnsureLoadHistoryAlarm();
            ResetPanel1(dtTile, tileType);
            ResetChart3(dtTile, tileType);
            ResetChart4(dtTile, tileType);
        }

        private void ResetPanel1(DateTime dtTile, string tileType)
        {
            DebugUtil.Instance.LOG.Debug("ResetPanel1, date=" + dtTile.ToShortDateString() + ", type=" + tileType);
            //LogListSystem(upAlarmList, downAlarmList);
            for (int i = 0; i < 12; i++)
            {
                if (i >= systemList.Count)
                    break;

                //first get 
                string valueControlName = "txtSubValue" + (i + 1).ToString();
                TextBlock txtValue = FindTextBlockByName(Panel1, valueControlName);
                int totalCount = 0;
                if (txtValue.Tag != null)
                {
                    string ID = txtValue.Tag.ToString();
                    List<string> subIDList = new List<string>();
                    if (i < 3)
                    {   //ID is subsystemID
                        subIDList.Add(ID);
                    }
                    else
                    {   //ID is systemID
                        SystemInfo systemInfo = systemList.Where(p => p.ID == ID).FirstOrDefault();
                        subIDList = systemInfo.subsystemList.Select(p => p.ID).ToList();
                    }

                    totalCount = CalculateTotalAlarmCount(bTileInRealTimeMode, dtTile, tileType, subIDList);
                }
                UpdateTextBlock(txtValue, totalCount, 0, true);
                DebugUtil.Instance.LOG.Debug(valueControlName + "=" + txtValue.Text + ", totalCount=" + totalCount);

                if (txtValue.Text == "0")
                    txtValue.Foreground = Brushes.Lime;
                else
                    txtValue.Foreground = Brushes.Red;
            }
        }

        private void ResetChart3(DateTime dtTile, string tileType)
        {
            DebugUtil.Instance.LOG.Debug("ResetChart3, date=" + dtTile.ToShortDateString() + ", type=" + tileType);
            List<string> subIDList = GetComboBoxSelectedSubsystemIDList(cmbSubsystem3);
            for (int i = 0; i < locationList.Count; i++)
            {   //system
                Location location = locationList[i];
                int totalCount = CalculateTotalAlarmCount(bTileInRealTimeMode, dtTile, tileType, subIDList, location.Name);
                DataPoint dp = Chart3.Series[0].DataPoints[i];      //each location is a datapoint
                UpdateDatapoint(dp, totalCount, 0, true);
            }
            AdjustAxisYMaximumInternal(Chart3);
        }

        private void ResetChart4(DateTime dtTile, string tileType)
        {
            List<string> subIDList = GetComboBoxSelectedSubsystemIDList(cmbSubsystem4);
            for (int i = 0; i < locationList.Count; i++)
            {   //system
                Location location = locationList[i];
                int totalCount = CalculateTotalAlarmCount(bTileInRealTimeMode, dtTile, tileType, subIDList, location.Name);

                DataPoint dp = Chart4.Series[0].DataPoints[i];      //each location is a datapoint
                UpdateDatapoint(dp, totalCount, 0, true);
            }
            AdjustAxisYMaximumInternal(Chart4);
        }

        private void txtATS_Click(object sender, RoutedEventArgs e)
        {
            //if (System.Diagnostics.Process.GetProcessesByName("AtsAlarmAnalysis").ToList().Count == 0)
            {
                Process ps = new Process();
                string filename = TemplateProject.IscsCachedMap.Instance.AtsAlarmAnalysisBinary;
                string workingDir = System.IO.Path.GetDirectoryName(filename);
                ps.StartInfo.FileName = filename;
                ps.StartInfo.WorkingDirectory = workingDir;
                ps.Start();
                this.WindowState = System.Windows.WindowState.Minimized;
            }
            //else
            {

            }
        }
    }
}
