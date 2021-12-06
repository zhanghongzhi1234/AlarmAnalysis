using MyList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
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
    public partial class AtsAlarmDashboard : IFilterInterface
    {
        List<SystemInfo> systemList = AtsCachedMap.Instance.systemList;
        List<Subsystem> allSubsystemList = AtsCachedMap.Instance.allSubsystemList;
        List<Severity> severityList = AtsCachedMap.Instance.severityList;
        List<Location> locationList = AtsCachedMap.Instance.locationList;
        DispatcherTimer timerPoll;
        int pollInterval = AtsCachedMap.Instance.pollInterval;
        string AlarmFilePath = AtsCachedMap.Instance.AlarmFilePath;
        int alarmThreshold = AtsCachedMap.Instance.alarmThreshold;
        //List<string> historyFileList = new List<string>();
        List<AtsAlarm> allAlarmList = new List<AtsAlarm>();
        List<AtsAlarm> newAlarmList = new List<AtsAlarm>();
        DataTable tableTop10 = new DataTable();

        Random random = new Random(0);
        //SQLiteServer dbServer;
        bool bFirstStart = true;

        List<string> alarmIDList = new List<string>();       //used to keep all alarmID

        DispatcherTimer timerBlink;
        int blinkInterval = 500;            //Polling Interval for ATS Alarm Files in millisecond
        List<Brush> oldBrushList = new List<Brush>();
        List<Brush> newBrushList = new List<Brush>();
        AlarmFilter alarmFilter = new AlarmFilter(1);
        AlarmFilterWindow1 filterDlg;

        AtsAlarmDetail detailWindow1 = null;            //for severity 1,2,3,Alert
        AtsAlarmDetail detailWindow2 = null;            //for severity Alert

        void InitScript()
        {
            DebugUtil.Instance.LOG.Info("InitScript started");
            //dbServer = serverMap["Local"] as SQLiteServer;
            btnClear.Click += btnClear_Click;
            btnEdit.Click += btnEdit_Click;
            btnEdit.MouseEnter += menuButton_MouseEnter;
            btnEdit.MouseLeave += menuButton_MouseLeave;
            btnDetail.Click += btnDetail_Click;
            btnDetail.MouseEnter += menuButton_MouseEnter;
            btnDetail.MouseLeave += menuButton_MouseLeave;
            btnAlertDetail.Click += btnAlertDetail_Click;
            btnAlertDetail.MouseEnter += menuButton_MouseEnter;
            btnAlertDetail.MouseLeave += menuButton_MouseLeave;
            txtFilter.MouseLeftButtonDown += txtFilter_Click;
            txtFilter.MouseEnter += textBlock_MouseEnter;
            txtFilter.MouseLeave += textBlock_MouseLeave;
            /*imgLogo_Iscs.MouseEnter += image_MouseEnter;
            imgLogo_Iscs.MouseLeave += image_MouseLeave;
            imgLogo_Iscs.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgLogo_Iscs.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgLogo_Iscs.MouseLeftButtonUp += imgLogoIscs_MouseLeftButtonUp;*/
            txtISCS.MouseLeftButtonDown += txtISCS_Click;
            txtISCS.MouseEnter += textBlock_MouseEnter;
            txtISCS.MouseLeave += textBlock_MouseLeave;
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
            //ReadConfigFile();
            if (AtsCachedMap.Instance.isSetRunParam("showClear"))
                btnClear.Visibility = System.Windows.Visibility.Visible;
            DebugUtil.Instance.LOG.Info("InitScript completed");
        }

        private void InitAll()
        {
            InitChart1();
            InitTop10AlarmList();
            InitChart3();
            InitChart5();
        }

        public void InitChart1()
        {
            Chart1.DataPointWidth = 10;
            legend1Fill.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severityList[0].Color));
            legend1Text.Text = severityList[0].Name;
            legend2Fill.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severityList[1].Color));
            legend2Text.Text = severityList[1].Name; 
            legend3Fill.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severityList[2].Color));
            legend3Text.Text = severityList[2].Name; 
            legend4Fill.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severityList[3].Color));
            legend4Text.Text = severityList[3].Name;

            if (alarmThreshold > 0)
            {
                TrendLineCollection trendLines = new TrendLineCollection();
                TrendLine trendLine = new TrendLine();
                trendLine.Orientation = Orientation.Horizontal;
                trendLine.LineColor = Brushes.Red;
                trendLine.LineThickness = 1.5;
                trendLine.Value = alarmThreshold;
                trendLines.Add(trendLine);
                Chart1.TrendLines = trendLines;
            }
        }

        public List<DataTrigger> CreateTriggersForSeverity()
        {
            List<DataTrigger> triggers = new List<DataTrigger>();
            DataTrigger trigger1 = new DataTrigger();
            trigger1.Value = "1";
            trigger1.Binding = new Binding() { Path = new PropertyPath("Severity") };
            trigger1.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("Red"))));
            trigger1.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger1);
            DataTrigger trigger2 = new DataTrigger();
            trigger2.Value = "2";
            trigger2.Binding = new Binding() { Path = new PropertyPath("Severity") };
            //trigger2.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#984807"))));
            trigger2.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("Gray"))));
            trigger2.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger2);
            DataTrigger trigger3 = new DataTrigger();
            trigger3.Value = "3";
            trigger3.Binding = new Binding() { Path = new PropertyPath("Severity") };
            //trigger3.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#632523"))));
            trigger3.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("Coral"))));
            trigger3.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger3);
            DataTrigger trigger4 = new DataTrigger();
            trigger4.Value = "4";
            trigger4.Binding = new Binding() { Path = new PropertyPath("Severity") };
            trigger4.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("Yellow"))));
            trigger4.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger4);

            return triggers;
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
            /*Trigger trigger2 = new Trigger();
            trigger2.Value = 1;
            trigger2.Property = ItemsControl.AlternationIndexProperty;
            trigger2.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"))));//2A568A,984807
            trigger2.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            triggers.Add(trigger2);*/
            return triggers;
        }

        public void InitTop10AlarmList()
        {
            top10AlarmList.SetHorizontalScrollBarVisibility(ScrollBarVisibility.Hidden);
            top10AlarmList.SetVerticalScrollBarVisibility(ScrollBarVisibility.Hidden);

            //set header style
            Style style = new Style();
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(HeightProperty, 32d));
            style.Setters.Add(new Setter(FontSizeProperty, 11d));
            style.Setters.Add(new Setter(BackgroundProperty, Brushes.Black));
            style.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            //style.Setters.Add(new Setter(BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));         //this parameter have no use here, don't know why
            top10AlarmList.SetHeaderStyle(style);

            //set item style
            top10AlarmList.AddSetterToItemContainerStyle(new Setter(FontSizeProperty, 10d));
            top10AlarmList.AddSetterToItemContainerStyle(new Setter(HeightProperty, 25d));
            top10AlarmList.AddSetterToItemContainerStyle(new Setter(VerticalAlignmentProperty, VerticalAlignment.Bottom));	//have no effect
            top10AlarmList.AddSetterToItemContainerStyle(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#303030"))));
            top10AlarmList.AddSetterToItemContainerStyle(new Setter(ForegroundProperty, Brushes.White));

            //set item trigger
            top10AlarmList.RemoveAllItemContainerStyleTriggers();
            //top10AlarmList.SetAllTriggersForItemContainerStyle(CreateTriggersForSeverity());
            //top10AlarmList.SetAllTriggersForItemContainerStyle(CreateTriggersForAlarmCount());
            {
                top10AlarmList.listView.AlternationCount = 3;
                top10AlarmList.SetAllTriggersForItemContainerStyle(CreateAlternatingTriggers());
            }

            //add column for alarm list
            top10AlarmList.gridView.Columns.Clear();
            top10AlarmList.AddColumn("Alarm Description", 295, "Description", HorizontalAlignment.Left);
            top10AlarmList.AddColumn("Sev", 35, "Severity", HorizontalAlignment.Center);
            top10AlarmList.AddColumn("Loc", 35, "LocationId", HorizontalAlignment.Center);
            top10AlarmList.AddColumn("Alarm\r\nCount", 50, "Count", HorizontalAlignment.Center);

            //init data structure
            tableTop10.Columns.Add("Description");
            tableTop10.Columns.Add("Severity");
            tableTop10.Columns.Add("LocationId");
            tableTop10.Columns.Add("Count");

            top10AlarmList.EnableContextMenu(false);
        }

        public void InitChart3()
        {
            {   //Init Chart3
                Title title = new Title();
                title.FontSize = 11;
                title.Text = "Systems";
                Chart3.Titles.Add(title);
                Chart3.Legends[0].FontSize = 8;
            }
            {   //Init Chart3_1, Chart3_2, Chart3_3, Chart3_4
                for (int i = 0; i < systemList.Count; i++)
                {
                    Title title = new Title();
                    title.FontSize = 11;
                    title.Text = systemList[i].Name;
                    string chartName = "Chart3_" + (i + 1);
                    Chart chart = (Chart)this.FindName(chartName);
                    chart.Titles.Add(title);
                    chart.Legends[0].Enabled = false;
                    /*{
                        Brush brush = Chart3.Series[0].DataPoints[i].Color;
                        int totalCount = chart.Series[0].DataPoints.Count;
                        float delta = 1f / totalCount;
                        for (int j = 0; j < totalCount; j++)
                        {
                            Brush subBrush = CommonFunction.ChangeBrushBrightness(brush, 0.5f + delta * j);
                            chart.Series[0].DataPoints[j].Color = subBrush;
                        }
                        
                    }*/
                }
            }
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

        public void InitChart5()
        {
            /*TrendLineCollection trendLines = new TrendLineCollection();
            for (int j = 0; j <= 24; j++)
            {   //add TrendLine for Chart 5
                if (j % 2 == 0)
                {
                    TrendLine trendLine = new TrendLine();
                    trendLine.Orientation = Orientation.Vertical;
                    trendLine.LineColor = Brushes.Gray;
                    trendLine.LineThickness = 0.5;
                    trendLine.Value = j;
                    trendLines.Add(trendLine);
                }
            }
            Chart5.AxesX[0].AxisMinimum = 0;
            Chart5.AxesX[0].AxisMaximum = 23.9;
            Chart5.AxesX[0].Suffix = ":00";
            Chart5.AxesX[0].Interval = 2;
            Chart5.AxesX[0].IntervalType = IntervalTypes.Number;
            Chart5.AxesX[0].ValueFormatString = "";
            Chart5.TrendLines = trendLines;*/
            TrendLineCollection trendLines = new TrendLineCollection();
            DateTime startTime = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            DateTime endTime = DateTime.Now.Date + new TimeSpan(1, 0, 0, 0);
            TimeSpan step = new TimeSpan(2, 0, 0);
            for (DateTime j = startTime; j <= endTime; j += step)
            {   //add TrendLine for Chart 5
                //if (j % 2 == 0)
                {
                    TrendLine trendLine = new TrendLine();
                    trendLine.Orientation = Orientation.Vertical;
                    trendLine.LineColor = Brushes.Gray;
                    trendLine.LineThickness = 0.5;
                    trendLine.Value = j;
                    trendLines.Add(trendLine);
                }
            }
            Chart5.AxesX[0].AxisMinimum = startTime;
            Chart5.AxesX[0].AxisMaximum = endTime;
            Chart5.AxesX[0].Interval = 4;
            Chart5.AxesX[0].IntervalType = IntervalTypes.Hours;
            Chart5.AxesX[0].ValueFormatString = "MM-dd HH:mm";
            //Chart5.AxesX[0].ValueFormatString = "HH:mm";
            Chart5.TrendLines = trendLines;
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            AlarmFilterWindow filter = new AlarmFilterWindow(null);
            filter.ShowDialog();
        }

        private DataRow CreateNewRow(DataTable table, string description, string severity, string location, string count)
        {
            DataRow newRow = table.NewRow();
            newRow["description"] = description;
            newRow["severity"] = severity;
            newRow["locationId"] = location;
            newRow["count"] = count;
            return newRow;
        }

        //Clear all and reload all alarm from Db
        private void ReloadAllAlarmFromDb()
        {   //reset all data to 0
            alarmIDList = DAIHelper.Instance.GetAllAlarmIDFromDb(1);            //avoid duplicate, 1 mean AtsAlar,m
            //allAlarmList = DAIHelper.Instance.GetTodayAtsAlarmListAll();   //load from database
            DateTime startTime = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            DateTime endTime = DateTime.Now.Date + new TimeSpan(23, 59, 59);
            allAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, endTime, 1);  // 1 mean AtsAlarm
            {   //reset data for Chart1
                Chart1.AxesY[0].AxisMaximum = 100;
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < severityList.Count(); i++)
                {
                    //double value = Convert.ToDouble(GetTodayAlarmNumberOfSeverityFromDb(Convert.ToInt32(severityList[i].ID)));
                    List<AtsAlarm> alarmList = allAlarmList.Where(p => p.severityID == severityList[i].ID).ToList();
                    double value = alarmList.Count();
                    string ToolTipText = GetToolTipTextForSeverity(alarmList, severityList[i].ID);
                    data.Add(new RawTable(severityList[i].Name, value, (SolidColorBrush)new BrushConverter().ConvertFromString(severityList[i].Color), null, ToolTipText));
                }
                dataSourceMap["Data1_1"].ReloadChartData(data);
            }
            {   //Dummy data for top 10 alarmlist
                UpdateTodayTop10AlarmTableFromDb();
                FillListViewWithDataTable(top10AlarmList.listView, tableTop10);
            }
            {   //Dummy data for Chart3
                List<RawTable> data = new List<RawTable>();
                //List<Brush> brushList = new List<Brush>() { Brushes.Brown, Brushes.YellowGreen, Brushes.SteelBlue, Brushes.Orange };
                for (int i = 0; i < systemList.Count; i++)
                {
                    string systemID = systemList[i].ID;
                    double value = allAlarmList.Where(p => p.systemID == systemID).Count();
                    //double value = Convert.ToDouble(DAIHelper.Instance.GetTodayAlarmNumberBySystemShortNameFromDb(systemList[i].ShortName));
                    //double value = Convert.ToDouble(GetTodayAlarmNumberBySystemShortNameFromDb(systemList[i].ShortName));     //cannot use systemkey, alarmstore cannot provide
                    if (value != 0d)
                    {
                        //data.Add(new RawTable(systemList[i].ShortName, value, brushList[i], ID));
                        Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(systemList[i].Color));
                        data.Add(new RawTable(systemList[i].ShortName, value, brush, systemID));
                    }
                }
                dataSourceMap["Data3"].ReloadChartData(data);
            }
            {   //Dummy data for Chart3_1, Chart3_2, Chart3_3, Chart3_4
                for (int i = 0; i < systemList.Count; i++)
                {
                    Chart chart = null;
                    DataSource dataSource = null;
                    if (i == 0)
                    {
                        chart = Chart3_1;
                        dataSource = dataSourceMap["Data3_1"];
                    }
                    else if (i == 1)
                    {
                        chart = Chart3_2;
                        dataSource = dataSourceMap["Data3_2"];
                    }
                    else if (i == 2)
                    {
                        chart = Chart3_3;
                        dataSource = dataSourceMap["Data3_3"];
                    }
                    else if (i == 3)
                    {
                        chart = Chart3_4;
                        dataSource = dataSourceMap["Data3_4"];
                    }

                    List<RawTable> data = new List<RawTable>();
                    for (int j = 0; j < systemList[i].subsystemList.Count; j++)
                    {
                        Subsystem subsystem = systemList[i].subsystemList[j];
                        string subsystemID = subsystem.ID;
                        double value = allAlarmList.Count(p => p.subsystemID == subsystemID);
                        //double value = Convert.ToDouble(DAIHelper.Instance.GetTodayAlarmNumberBySubsystemNameFromDb(subsystem.Name));
                        if (value != 0d)
                        {
                            //Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(systemList[i].Color));
                            data.Add(new RawTable(subsystem.Name, value, subsystem.brush, subsystemID));
                        }
                    }
                    dataSource.ReloadChartData(data);
                }
            }
            {   //Dummy data for Chart5
                Chart5.AxesY[0].AxisMaximum = 100;
                List<RawTable> alarmNumberList1 = new List<RawTable>();        //alarm number list yesterday, X is hour number, Y is alarm number
                //List<RawTable> alarmNumberList2 = new List<RawTable>();        //alarm number list today, X is hour number, Y is alarm number
                //int maxHour = 48;       //statistic for every 1 hour
                //int value = 0;
                //int currentHour = DateTime.Now.Hour;
                DateTime dtEnd = DateTime.Now;
                DateTime dtStart = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
                TimeSpan step = new TimeSpan(1, 0, 0);
                //load data for yesterday from db
                for (DateTime dtCursor = dtStart; dtCursor <= dtEnd; dtCursor += step)
                {
                    int YValue = 0;
                    if (dtCursor > dtStart)
                    {
                        YValue = DAIHelper.Instance.GetAlarmNumberBetweenFromDb(dtStart, dtCursor + new TimeSpan(1, 0, 0));     //it is accumulate already
                    }
                    DateTime XValue = dtCursor;
                    RawTable rawTable1 = new RawTable(XValue, YValue);
                    alarmNumberList1.Add(rawTable1);
                }
                //load data for today from db if have
                /*for (int j = 0; j <= today.Hour; j++)
                {
                    if (j > today.Hour)
                        value = 0;
                    else
                    {
                        DateTime dtStart = new DateTime(today.Year, today.Month, today.Day, j, 0, 0, DateTimeKind.Local);
                        DateTime dtEnd = dtStart + new TimeSpan(1, 0, 0);
                        value = DAIHelper.Instance.GetAlarmNumberBetweenFromDb(dtStart, dtEnd);
                    }
                    int XValue = j;
                    RawTable rawTable1 = new RawTable(XValue, value);
                    alarmNumberList1.Add(rawTable1);
                }*/
                dataSourceMap["Data5_1"].ReloadChartData(alarmNumberList1);
                //dataSourceMap["Data5_2"].ReloadChartData(alarmNumberList2);
            }
            AdjustAxisYMaximumInternal(Chart1);
            AdjustAxisYMaximumInternal(Chart5);
            //all sub window only reload all alarm when show window
            if (detailWindow1 != null)
            {
                detailWindow1.ReloadAllAlarmFromDb();
            }
            if (detailWindow2 != null)
            {
                detailWindow2.ReloadAllAlarmFromDb();
            }
        }

        //update top 10 alarm table from db
        private void UpdateTodayTop10AlarmTableFromDb()
        {
            string strCondition = "";
            string strRule = "";
            if (alarmFilter != null && alarmFilter.isEnabled == true)
            {
                strCondition = buildFilterCondition();
                strRule = buildTop10Rule(false);
                txtTop10Title.Text = "Top 10 Alarms (Filtered)";
            }
            else
            {
                strRule = buildTop10Rule(true);
                txtTop10Title.Text = "Top 10 Alarms";
            }
            
            DataTable dtResult = DAIHelper.Instance.GetTop10AlarmTableFromDb(strCondition, strRule);
            if (dtResult != null)
            {
                tableTop10.Rows.Clear();
                //For each field in the table...
                foreach (DataRow row in dtResult.Rows)
                {
                    tableTop10.Rows.Add(CreateNewRow(tableTop10, row[0].ToString(), row[1].ToString(), row[2].ToString(), row[4].ToString()));
                }
            }
        }

        //for testing purpose only
        private void LoadDummyHistoryAlarm()
        {   //maybe change in future, currently use dummy data
            int seed1 = 0;

            {   //Dummy data for Chart1
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < severityList.Count(); i++)
                {
                    data.Add(new RawTable(severityList[i].Name, 50 + Math.Pow(20, i), (SolidColorBrush)new BrushConverter().ConvertFromString(severityList[i].Color)));
                }
                Chart1.DataPointWidth = 10;
                dataSourceMap["Data1_1"].ReloadChartData(data);
            }
            {   //Dummy data for top 10 alarmlist
                tableTop10.Columns.Add("Description");
                tableTop10.Columns.Add("Severity");
                tableTop10.Columns.Add("LocationId");
                tableTop10.Columns.Add("Count");
                //DataRow newRow = tableTop10.NewRow();
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "EB due to loss of speed codes when running in FBSS(NR)", "1", "PMN", "0"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "EB due to a common failure in both bearing sensors in FBSS(ATC Reset)", "1", "PMN", "1"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "EB due to a train movement detected in AM or CM FBSS with PSD open(NR)", "1", "PMN", "0"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "CBTC or FBSS signaling activation by ATC", "2", "PMN", "1"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "Status of Activation of FBSS by ATC", "2", "PMN", "2"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "Signaling changed to FBSS", "2", "PMN", "1"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "Train Stalled detected by ATP due to no FBSS and CBTC", "2", "PMN", "0"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "TDMS-ATP Communication Link Failure", "2", "PMN", "0"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "One ATP Lane Failure", "2", "PMN", "1"));
                tableTop10.Rows.Add(CreateNewRow(tableTop10, "TVSS WLAN link", "2", "3", "PMN"));
                FillListViewWithDataTable(top10AlarmList.listView, tableTop10);
            }
            {   //Dummy data for Chart3
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < systemList.Count; i++)
                {
                    data.Add(new RawTable(systemList[i].ShortName, random.Next(1, 61)));
                }
                dataSourceMap["Data3"].ReloadChartData(data);
                Title title = new Title();
                title.FontSize = 9;
                title.Text = "Systems";
                Chart3.Titles.Add(title);
                Chart3.Legends[0].FontSize = 7;
            }
            {   //Dummy data for Chart3_1
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < systemList[0].subsystemList.Count; i++)
                {
                    data.Add(new RawTable(systemList[0].subsystemList[i].Name, random.Next(1, 61)));
                }
                dataSourceMap["Data3_1"].ReloadChartData(data);
                Title title = new Title();
                title.FontSize = 8;
                title.Text = "Communication";
                Chart3_1.Titles.Add(title);
                //Chart3_1.Legends[0].FontSize = 2;
                Chart3_1.Legends[0].Enabled = false;
            }
            {   //Dummy data for Chart3_2
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < systemList[1].subsystemList.Count; i++)
                {
                    data.Add(new RawTable(systemList[1].subsystemList[i].Name, random.Next(1, 31)));
                }
                dataSourceMap["Data3_2"].ReloadChartData(data);
                Title title = new Title();
                title.FontSize = 8;
                title.Text = "Rolling Stock";
                Chart3_2.Titles.Add(title);
                //Chart3_2.Legends[0].FontSize = 1;
                Chart3_2.Legends[0].Enabled = false;
            }
            {   //Dummy data for Chart3_3
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < systemList[2].subsystemList.Count; i++)
                {
                    data.Add(new RawTable(systemList[2].subsystemList[i].Name, random.Next(1, 71)));
                }
                dataSourceMap["Data3_3"].ReloadChartData(data);
                Title title = new Title();
                title.FontSize = 8;
                title.Text = "Platform Screen Door";
                Chart3_3.Titles.Add(title);
                //Chart3_3.Legends[0].FontSize = 5;
                Chart3_3.Legends[0].Enabled = false;
            }
            {   //Dummy data for Chart3_4
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < systemList[3].subsystemList.Count; i++)
                {
                    data.Add(new RawTable(systemList[3].subsystemList[i].Name, random.Next(1, 71)));
                }
                dataSourceMap["Data3_4"].ReloadChartData(data);
                Title title = new Title();
                title.FontSize = 8;
                title.Text = "Signaling";
                Chart3_4.Titles.Add(title);
                //Chart3_4.Legends[0].FontSize = 4;
                Chart3_4.Legends[0].Enabled = false;
            }
            {   //Dummy data for Chart5
                List<RawTable> alarmNumberList1 = new List<RawTable>();        //alarm number list yesterday, X is hour number, Y is alarm number
                List<RawTable> alarmNumberList2 = new List<RawTable>();        //alarm number list today, X is hour number, Y is alarm number
                int maxHour = 24;
                int value = 15000;
                int currentHour = DateTime.Now.Hour;
                TrendLineCollection trendLines = new TrendLineCollection();
                for (int j = 0; j < maxHour; j++)
                {
                    if (j <= 2)
                        value -= random.Next(2000, 3000);
                    else if (j > 2 && j <= 8)
                        value += random.Next(2000, 2500);
                    else if (j > 8 && j <= 12)
                        value -= random.Next(2000, 2500);
                    else
                        value += random.Next(0, 5000) - 1800;
                    int vibration = random.Next(0, 5000) - 2500;
                    //DateTime XValue = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, j, 0, 0);
                    int XValue = j;
                    RawTable rawTable1 = new RawTable(XValue, value);
                    alarmNumberList1.Add(rawTable1);
                    if (j <= currentHour)
                    {
                        RawTable rawTable2 = new RawTable(XValue, value + vibration);
                        alarmNumberList2.Add(rawTable2);
                    }
                    //add TrendLine for Chart 5
                    if (j % 2 == 0)
                    {
                        TrendLine trendLine = new TrendLine();
                        trendLine.Orientation = Orientation.Vertical;
                        trendLine.LineColor = Brushes.Gray;
                        trendLine.LineThickness = 0.5;
                        trendLine.Value = XValue;
                        trendLines.Add(trendLine);
                    }
                }
                dataSourceMap["Data5_1"].ReloadChartData(alarmNumberList1);
                dataSourceMap["Data5_2"].ReloadChartData(alarmNumberList2);
                /*DateTime axisMin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                DateTime axisMax = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                Chart5.AxesX[0].AxisMinimum = axisMin;
                Chart5.AxesX[0].AxisMaximum = axisMax;*/

                Chart5.AxesX[0].AxisMinimum = 0;
                Chart5.AxesX[0].AxisMaximum = 23.9;
                Chart5.AxesX[0].Suffix = ":00";
                Chart5.AxesX[0].Interval = 2;
                Chart5.AxesX[0].IntervalType = IntervalTypes.Number;
                Chart5.AxesX[0].ValueFormatString = "";
                Chart5.TrendLines = trendLines;
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
            int AxisMaxOri = 0;
            if(chart.AxesY[0].AxisMaximum != null)
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

        private void UpdateChart1(List<AtsAlarm> newAlarmList)
        {
            for (int i = 0; i < severityList.Count(); i++)
            {
                List<AtsAlarm> alarmList = allAlarmList.Where(p => p.severityID == severityList[i].ID).ToList();
                int newCount = alarmList.Count();
                
                int oldCount = Convert.ToInt32(Chart1.Series[0].DataPoints[i].YValue);
                Chart1.Series[0].DataPoints[i].YValue = newCount;
                Chart1.Series[0].DataPoints[i].ToolTipText = GetToolTipTextForSeverity(alarmList, severityList[i].ID);
                AdjustAxisYMaximumByNewValue(Chart1, newCount);
                /*if (alarmThreshold > 0 && newCount >= alarmThreshold)
                {
                    timerBlink.Start();
                }*/
            }
        }

        int blinkCounter = 0;
        private void timerBlink_Tick(object sender, EventArgs e)
        {
            if (alarmThreshold > 0)
            {
                for (int i = 0; i < Chart1.Series[0].DataPoints.Count(); i++)
                {
                    DataPoint dp = Chart1.Series[0].DataPoints[i];
                    if (dp.YValue >= alarmThreshold)
                    {
                        //if (dp.Color == oldBrushList[i])
                        if (blinkCounter == 0)
                        {
                            dp.Color = newBrushList[i];
                            dp.LabelFontColor = Brushes.Gray;
                        }
                        else
                        {
                            dp.Color = oldBrushList[i];
                            dp.LabelFontColor = Brushes.White;
                        }
                    }
                }
                blinkCounter = (blinkCounter + 1) % 2;
            }
        }

        private void BlinkChart(Chart chart)
        {
            if (chart.Background == Brushes.Black)
            {
                //chart.Opacity = 0.5;
                chart.Background = Brushes.White;
            }
            else
            {
                //chart.Opacity = 1;
                chart.Background = Brushes.Black;
            }
        }

        private string GetToolTipTextForSeverity(List<AtsAlarm> alarmList, string severityID)
        {
            string label = null;
            var groupResult = alarmList.GroupBy(p => p.systemID).Select(p => new { Metirc = p.Key, Count = p.Count() }).ToList();
            int sumCount = groupResult.Sum(p => p.Count);

            foreach (var group in groupResult)
            {
                string percent = Math.Round(group.Count * 100.0 / sumCount, 0) .ToString();
                string sysShortName = "";
                SystemInfo system = AtsCachedMap.Instance.GetSystemByID(group.Metirc);
                if(system != null)
                {
                    sysShortName = system.ShortName;
                }
                label += sysShortName + " - " + group.Count + " (" + percent + "%)\r\n";
            }
            return label;
        }
                                                                               
        //private void UpdateTop10AlarmList(List<AtsAlarm> newAlarmList)
        private void UpdateTop10AlarmList()
        {
            UpdateTodayTop10AlarmTableFromDb();
            FillListViewWithDataTable(top10AlarmList.listView, tableTop10);
        }

        private void UpdateChart3(List<AtsAlarm> newAlarmList)
        {
            for (int i = 0; i < systemList.Count; i++)
            {   //system
                int newCount = newAlarmList.Where(p => p.systemID == systemList[i].ID).Count();
                if (newCount == 0)
                    continue;
                int ID = Convert.ToInt32(systemList[i].ID);
                DataPoint dp = Chart3.Series[0].DataPoints.Where(p => Convert.ToInt32(p.Tag) == ID).FirstOrDefault();
                if (dp == null)
                {
                    Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(systemList[i].Color));
                    dataSourceMap["Data3"].AddDataPoint(systemList[i].ShortName, newCount, brush, ID);
                }
                else
                {
                    int oldValue = Convert.ToInt32(dp.YValue);
                    dp.YValue = oldValue + newCount;
                }
            }
            UpdateSubsystem(Chart3_1, newAlarmList);
            UpdateSubsystem(Chart3_2, newAlarmList);
            UpdateSubsystem(Chart3_3, newAlarmList);
            UpdateSubsystem(Chart3_4, newAlarmList);
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

        private int preHour = DateTime.Now.Hour;
        DataPoint preTrendDP = null;
        private void UpdateChart5(List<AtsAlarm> newAlarmList)
        {
            if (preTrendDP == null)
            {   //first time update
                preTrendDP = Chart5.Series[0].DataPoints.Where(p => (Convert.ToDateTime(p.XValue).Hour == DateTime.Now.Hour && Convert.ToDateTime(p.XValue).Date == DateTime.Now.Date)).FirstOrDefault();
                if (preTrendDP == null)
                {   //usually program won't come here if ReloadAllAlarmFromDb() work normally
                    preTrendDP = dataSourceMap["Data5_1"].AddDataPoint(DateTime.Now, 0);

                }
                preTrendDP.LabelEnabled = true;         //ReloadAllAlarmFromDb won't enable label
            }

            DataPoint curTrendDP = preTrendDP;
            if (DateTime.Now.Hour != preHour)
            {
                preTrendDP.LabelEnabled = false;
                curTrendDP = dataSourceMap["Data5_1"].AddDataPoint(DateTime.Now, preTrendDP.YValue);
                curTrendDP.LabelEnabled = true;
                if (DateTime.Now.Hour == 0)
                {   //avoid be covered by tha Axis
                    curTrendDP.LabelText = "     " + curTrendDP.LabelText.TrimStart();
                }

                preTrendDP = curTrendDP;
            }
            
            if (newAlarmList == null)
                return;

            int newCount = newAlarmList.Where(p => (p.sourceTime.Hour == DateTime.Now.Hour && p.sourceTime.Date == DateTime.Now.Date)).Count();
            if(newCount > 0)
            {
                curTrendDP.YValue += newCount;
                AdjustAxisYMaximumByNewValue(Chart5, curTrendDP.YValue);
            }
        }

        private void UpdateAll(List<AtsAlarm> newAlarmList)
        {
            UpdateChart1(newAlarmList);
            //UpdateTop10AlarmList(newAlarmList);
            if (newAlarmList.Count > 0)
            {
                UpdateTop10AlarmList();
            }
            UpdateChart3(newAlarmList);
            UpdateChart5(newAlarmList);
            if (detailWindow1 != null)
            {
                detailWindow1.UpdateAll(newAlarmList);
            }
            if (detailWindow2 != null)
            {
                detailWindow2.UpdateAll(newAlarmList);
            }
        }

        void LoadScript()
        {
            InitAll();          //Put InitAll here because LoadScript execute after binding all Chart to data, the Axes and legend created already
            ReloadAllAlarmFromDb();           //this function will exectue in timerPoll_Tick for the first time
            DebugUtil.Instance.LOG.Info("Reload all alarms from db");
            if (!Directory.Exists(AlarmFilePath))
            {
                DebugUtil.Instance.LOG.Error("Alarm file path not exist!");
            }
            //ChangeChart3Color();

            /*if (bFirstStart == true)
            {
                DebugUtil.Instance.LOG.Info("First time start");
                //fisr time start, need read both table Alarm and Alarm_New
                string[] allFiles = GetAllAlarmFiles();
                if (allFiles.Count() > 0)
                {
                    ArchiveFiles(allFiles);
                    ReloadAllAlarmFromDb();         //Reload All alarm from DB if there are unarchived files
                    DebugUtil.Instance.LOG.Info("Reload all from db for new archive");
                }
                bFirstStart = false;
            }*/

            timerPoll = new DispatcherTimer();
            timerPoll.Interval = TimeSpan.FromMilliseconds(pollInterval);
            timerPoll.Tick += new EventHandler(timerPoll_Tick);
            timerPoll.Start();

            foreach (DataPoint dp in Chart1.Series[0].DataPoints)
            {
                oldBrushList.Add(dp.Color);
                Brush brush1 = CommonFunction.ChangeBrushBrightness(dp.Color, -0.3f);
                newBrushList.Add(brush1);
            }
            timerBlink = new DispatcherTimer();
            timerBlink.Interval = TimeSpan.FromMilliseconds(blinkInterval);
            timerBlink.Tick += new EventHandler(timerBlink_Tick);
            timerBlink.Start();
        }

        private string[] GetAllAlarmFilesOfToday()
        {
            string format = "AtsAlarm-" + DateTime.Now.ToString("yyyyMMdd") + "*.xml";
            string[] Files = Directory.GetFiles(AlarmFilePath, format); //Getting Text files
            return Files;
        }

        private string[] GetAllAlarmFiles()
        {
            string[] Files = Directory.GetFiles(AlarmFilePath, "AtsAlarm-*.xml"); //Getting Text files
            return Files;
        }

        private int previousDay = DateTime.Now.Day;
        //private int previousHour = DateTime.Now.Hour;
        private void timerPoll_Tick(object sender, EventArgs e)
        {
            //check if a new day start, if a new day start need reset all and check archive
            if (DateTime.Now.Day != previousDay)
            {
                DebugUtil.Instance.LOG.Info("A new day start");
                //reset all for the new day
                //ArchiveFiles(GetAllAlarmFiles());
                InitChart5();
                ReloadAllAlarmFromDb();
                DebugUtil.Instance.LOG.Info("Reset all");
                //every midnight check db and delete archive max than 60 days
                //int temp = CommonFunction.ConvertDateTimeLocalToUnixTime(DateTime.Now - new TimeSpan(archiveKeepDays, 0, 0, 0));
                //DAIHelper.Instance.DeleteAlarmBeforeTimeStamp(temp);
                previousDay = DateTime.Now.Day;
                //previousHour = DateTime.Now.Hour;
                //DebugUtil.Instance.LOG.Info("Delete alarm exceed " + archiveKeepDays + " days");
            }
            /*else if (DateTime.Now.Hour != previousHour && DateTime.Now.Hour >= 1)
            {
                DebugUtil.Instance.LOG.Info("A new hour start");
                DataPoint dpNow = Chart5.Series[0].DataPoints.Where(p => (Convert.ToDateTime(p.XValue).Hour == DateTime.Now.Hour && Convert.ToDateTime(p.XValue).Date == DateTime.Now.Date)).FirstOrDefault();
                DataPoint dpPrevious = Chart5.Series[0].DataPoints.Where(p => (Convert.ToDateTime(p.XValue).Hour == previousHour && Convert.ToDateTime(p.XValue).Date == DateTime.Now.Date)).FirstOrDefault();
                if (dpNow != null && dpPrevious != null)
                {
                    dpNow.YValue = dpPrevious.YValue;
                }

                previousHour = DateTime.Now.Hour;
            }*/

            newAlarmList.Clear();
            DateTime startTime = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            DateTime endTime = DateTime.Now.Date + new TimeSpan(23, 59, 59);
            List<AtsAlarm> tempAlarmList = DAIHelper.Instance.GetAlarmListBetween(startTime, endTime, 1, "Alarm_New");  // 1 mean AtsAlarm
            if (tempAlarmList != null)
            {
                foreach (AtsAlarm alarm in tempAlarmList)
                {
                    if (alarmIDList.Contains(alarm.alarmID))
                    {   //duplicate alarmID, ignore
                        continue;
                    }
                    else
                    {
                        newAlarmList.Add(alarm);
                        alarmIDList.Add(alarm.alarmID);
                    }
                }
            }
            DebugUtil.Instance.LOG.Debug("Read total " + newAlarmList.Count() + " new alarms");

            if (newAlarmList != null && newAlarmList.Count() > 0)
            {
                allAlarmList = allAlarmList.Concat(newAlarmList).ToList();
                UpdateAll(newAlarmList);
                DebugUtil.Instance.LOG.Debug("Update GUI complete");
            }
            else
            {
                UpdateChart5(null);     //Chart5 is trending, if no new alarm, just call update to check if a new hour start
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            DAIHelper.Instance.DeleteAlarmForToday();
            MessageBox.Show("Clear today alarm completed!");
            ReloadAllAlarmFromDb();
        }

        private void Chart1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Chart chart = (Chart)sender;
            if (chart.Series[0].RenderAs == RenderAs.Column)
            {
                chart.Series[0].RenderAs = RenderAs.Pie;
                chart.View3D = true;
                chart.Legends[0].Enabled = false;
            }
            else
            {
                chart.Series[0].RenderAs = RenderAs.Column;
                chart.View3D = false;
            }
        }

        private void txtFilter_Click(object sender, RoutedEventArgs e)
        {
            //testWin filter = new testWin();
            //filter.Show();
            if (AtsCachedMap.Instance.PopupFilter == true)
            {
                filterDlg = new AlarmFilterWindow1(alarmFilter, AtsCachedMap.Instance);
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
                    filterDlg = new AlarmFilterWindow1(alarmFilter, AtsCachedMap.Instance);
                }
                filterDlg.SetMainWindow(this);
                filterDlg.Show();
                this.Hide();
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
                if (systemList.Count > 0 && !alarmFilter.selectedSystems.Contains("All"))
                {
                    strCondition += " and systemkeyType in ('" + String.Join("','", alarmFilter.selectedSystems) + "')";
                }
                if (allSubsystemList.Count > 0 && !alarmFilter.selectedSubsystems.Contains("All"))
                {
                    strCondition += " and subsystemType in ('" + String.Join("','", alarmFilter.selectedSubsystems) + "')";
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
                DateTime dtStart = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
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
                strRule += String.Join(",", AlarmFilter.ruleAts);
            }

            return strRule;
        }

        private void btnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (detailWindow1 == null)
            {
                detailWindow1 = new AtsAlarmDetail();
                detailWindow1.SetDashboardWindow(this);
                detailWindow1.SetSeverityFilter("!Alert");
            }
            detailWindow1.Show();
            this.Hide();
        }

        private void btnAlertDetail_Click(object sender, RoutedEventArgs e)
        {
            if (detailWindow2 == null)
            {
                detailWindow2 = new AtsAlarmDetail();
                detailWindow2.SetDashboardWindow(this);
                detailWindow2.SetSeverityFilter("Alert");
            }
            detailWindow2.Show();
            this.Hide();
        }

        private void imgLogoIscs_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            /*string dir = Directory.GetCurrentDirectory();
            string dir1 = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
            Console.WriteLine(dir);
            Console.WriteLine(dir1);*/
            Process ps = new Process();
            string filename = "F:/WPF/EMS/AlarmAnalysis/Source/AlarmAnalysis/code/IscsAlarmAnalysis/bin/Release/IscsAlarmAnalysis.exe";
            string workingDir = "F:/WPF/EMS/AlarmAnalysis/Source/AlarmAnalysis/code/IscsAlarmAnalysis/bin/Release/";
            ps.StartInfo.FileName = filename;
            ps.StartInfo.WorkingDirectory = workingDir;
            ps.Start();
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void txtISCS_Click(object sender, RoutedEventArgs e)
        {
            Process ps = new Process();
            string filename = TemplateProject.AtsCachedMap.Instance.IscsAlarmAnalysisBinary;
            string workingDir = System.IO.Path.GetDirectoryName(filename);
            ps.StartInfo.FileName = filename;
            ps.StartInfo.WorkingDirectory = workingDir;
            ps.Start();
            this.WindowState = System.Windows.WindowState.Minimized;
        }
    }
}
