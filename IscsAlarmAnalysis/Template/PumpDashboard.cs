using MyList;
using System;
using System.Collections.Generic;
using System.Data;
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
    public partial class PumpDashboard
    {
        List<SystemInfo> systemList = IscsCachedMap.Instance.systemList;
        List<Subsystem> allSubsystemList = IscsCachedMap.Instance.allSubsystemList;
        List<Severity> severityList = IscsCachedMap.Instance.severityList;
        List<Location> locationList = IscsCachedMap.Instance.locationList;
        DispatcherTimer timerPoll;
        int pollInterval = IscsCachedMap.Instance.pollInterval;
        List<Pump> pumpList = null;
        double pumpThreshold;
        DispatcherTimer timerBlink;
        int blinkInterval = 500;            //Polling Interval for ATS Alarm Files in millisecond
        List<Brush> oldBrushList_Chart1 = new List<Brush>();
        List<Brush> newBrushList_Chart1 = new List<Brush>();
        List<Brush> oldBrushList_Chart2 = new List<Brush>();
        List<Brush> newBrushList_Chart2 = new List<Brush>();

        Random random = new Random(0);
        IscsAlarmDashboard dashboardWindow = null;

        public void SetDashboardWindow(IscsAlarmDashboard dashboardWindow)
        {
            this.dashboardWindow = dashboardWindow;
        }

        void InitScript()
        {
            DebugUtil.Instance.LOG.Info("InitScript started");
            Chart3.MouseLeftButtonUp += Chart_MouseLeftButtonUp;
            imgMinimize.MouseEnter += image_MouseEnter;
            imgMinimize.MouseLeave += image_MouseLeave;
            imgMinimize.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgMinimize.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgExit.MouseEnter += image_MouseEnter;
            imgExit.MouseLeave += image_MouseLeave;
            imgExit.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgExit.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgReturn.MouseEnter += image_MouseEnter;
            imgReturn.MouseLeave += image_MouseLeave;
            imgReturn.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgReturn.MouseLeftButtonUp += image_MouseLeftButtonUp;
            imgExport.MouseEnter += image_MouseEnter;
            imgExport.MouseLeave += image_MouseLeave;
            imgExport.MouseLeftButtonDown += image_MouseLeftButtonDown;
            imgExport.MouseLeftButtonUp += image_MouseLeftButtonUp;

            imgMinimize.MouseLeftButtonUp += imgMinimize_MouseLeftButtonUp;
            imgExit.MouseLeftButtonUp += imgExit_MouseLeftButtonUp;
            imgReturn.MouseLeftButtonUp += imgReturn_MouseLeftButtonUp;
            imgExport.MouseLeftButtonUp += imgExport_MouseLeftButtonUp;
            btnImport.Click += btnImport_Click;

            DebugUtil.Instance.LOG.Info("InitScript completed");

        }

        void imgExport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            btnExport_Click(null, null);
        }

        void btnImport_Click(object sender, RoutedEventArgs e)
        {
            string sMessageBoxText = "Clear Pump Table Before Import?";
            string sCaption = "Warning";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

            switch (rsltMessageBox)
            {
                case MessageBoxResult.Yes:
                    DAIHelper.Instance.DbServer.ExecuteNonQuery("delete from Pump");
                    break;

                case MessageBoxResult.Cancel:
                    return;
            }
            TextInputWindow win1 = new TextInputWindow();
            if(win1.ShowDialog() == true)
            {
                string textImport = win1.textInput;
                string[] textLines = textImport.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string sqlstr = "Insert into Pump (name, locationName, flowRate) Values ";
                foreach (string textLine in textLines)
                {
                    string[] textArr = textLine.Split(' ');
                    string fullName = textArr[0];
                    string value = textArr[1];
                    string[] nameArr = fullName.Split('/');
                    string locationName = nameArr[0].Split('-')[0];
                    string name = nameArr[nameArr.Length - 1];

                    sqlstr += "('" + name + "','" + locationName + "','" + value + "'),";
                }
                sqlstr = sqlstr.TrimEnd(',');
                DAIHelper.Instance.DbServer.ExecuteNonQuery(sqlstr);
            }
        }

        void imgReturn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dashboardWindow == null)
            {
                MessageBox.Show("Dashboard window not exist!");
            }
            dashboardWindow.Show();
            this.Hide();
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
                    string locationName = dp.XValue.ToString();
                    pumpThreshold = locationList.Where(p => p.Name == locationName).FirstOrDefault().PumpThreshold;
                    ChangeChart1Location(locationName);
                    ChangeChart2Location(locationName);
                    dp.Selected = false;                                //unselect datapoint to allow click to select again
                }
            }
        }

        private void InitAll()
        {
            InitChart3();
        }

        public void InitChart3()
        {
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

            Chart3.Series[0].SelectionEnabled = true;
            Chart3.Series[0].SelectionMode = SelectionModes.Single;
            //Chart3.DataPointWidth = 2.4;
        }

        //Clear all and reload all alarm from Db
        private void ReloadAllAlarmFromDb()
        {   //reset all data to 0
            
        }

        //for testing purpose only
        private void LoadDummyHistoryAlarm()
        {   //maybe change in future, currently use dummy data
            int seed1 = 0;
            
            /*{   //Dummy data for Chart2
                for (int i = 0; i < Chart2.Series[0].DataPoints.Count; i++)
                {
                    Chart2.Series[0].DataPoints[i].YValue = (i + 1) * 10;
                }
            }*/
            {   //Dummy data for Chart3
                for (int i = 0; i < Chart3.Series[0].DataPoints.Count; i++)
                {
                    Chart3.Series[0].DataPoints[i].YValue = Math.Round(random.Next(0, 1000) / 100.0, 2);
                }
            }
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

        //Chart1 is for drainage pump drainage
        private void ChangeChart1Location(string locationName)
        {
            Chart1.Series[0].DataPoints.Clear();
            List<RawTable> data = new List<RawTable>();
            var result = pumpList.Where(p => p.locationName == locationName && p.name.Contains("DSS"));
            foreach (Pump pump in result)
            {
                Brush brush = IscsCachedMap.Instance.GetColorByLocationName(locationName);
                if (brush != null)
                {
                    data.Add(new RawTable(pump.name, Math.Round(pump.volume, 2), brush));
                }
                else
                {
                    data.Add(new RawTable(pump.name, Math.Round(pump.volume, 2)));
                }
            }
            Chart1.Tag = locationName;
            dataSourceMap["Data1"].ReloadChartData(data);

            double totalVolume = Math.Round(result.Sum(p => p.volume), 2);
            Chart1_Title.Text = locationName + " Station Drainage Pump Total Volume (" + totalVolume + "L)";

            AdjustAxisYMaximumInternal(Chart1);

            oldBrushList_Chart1.Clear();
            newBrushList_Chart1.Clear();
            foreach (DataPoint dp in Chart1.Series[0].DataPoints)
            {
                oldBrushList_Chart1.Add(dp.Color);
                Brush brush1 = CommonFunction.ChangeBrushBrightness(dp.Color, -0.3f);
                newBrushList_Chart1.Add(brush1);
            }
            if (pumpThreshold > 0)
            {
                TrendLineCollection trendLines = new TrendLineCollection();
                TrendLine trendLine = new TrendLine();
                trendLine.Orientation = Orientation.Horizontal;
                trendLine.LineColor = Brushes.Red;
                trendLine.LineThickness = 1.5;
                trendLine.Value = pumpThreshold;
                trendLines.Add(trendLine);
                Chart1.TrendLines = trendLines;
            }
        }

        //Chart2 is for tunnel pump drainage
        private void ChangeChart2Location(string locationName)
        {
            Chart2.Series[0].DataPoints.Clear();
            List<RawTable> data = new List<RawTable>();
            var result = pumpList.Where(p => p.locationName == locationName && p.name.Contains("DST"));
            foreach (Pump pump in result)
            {
                Brush brush = IscsCachedMap.Instance.GetColorByLocationName(locationName);
                if (brush != null)
                {
                    data.Add(new RawTable(pump.name, Math.Round(pump.volume, 2), brush));
                }
                else
                {
                    data.Add(new RawTable(pump.name, Math.Round(pump.volume, 2)));
                }
            }
            Chart2.Tag = locationName;
            dataSourceMap["Data2"].ReloadChartData(data);

            double totalVolume = Math.Round(result.Sum(p => p.volume), 2);
            Chart2_Title.Text = locationName + " Tunnel Pump Total Volume (" + totalVolume + "L)";

            AdjustAxisYMaximumInternal(Chart2);

            oldBrushList_Chart2.Clear();
            newBrushList_Chart2.Clear();
            foreach (DataPoint dp in Chart2.Series[0].DataPoints)
            {
                oldBrushList_Chart2.Add(dp.Color);
                Brush brush1 = CommonFunction.ChangeBrushBrightness(dp.Color, -0.3f);
                newBrushList_Chart2.Add(brush1);
            }
            if (pumpThreshold > 0)
            {
                TrendLineCollection trendLines = new TrendLineCollection();
                TrendLine trendLine = new TrendLine();
                trendLine.Orientation = Orientation.Horizontal;
                trendLine.LineColor = Brushes.Red;
                trendLine.LineThickness = 1.5;
                trendLine.Value = pumpThreshold;
                trendLines.Add(trendLine);
                Chart2.TrendLines = trendLines;
            }
        }

        private void UpdateChart1(List<Pump> changedPumpList)
        {
            if (Chart1.Tag != null)
            {
                string locationName = Chart1.Tag.ToString();
                foreach (var dp in Chart1.Series[0].DataPoints)
                {
                    Pump pump = changedPumpList.Where(p => p.locationName == locationName && p.name == dp.XValue.ToString()).FirstOrDefault();
                    if (pump != null)
                    {
                        dp.YValue = Math.Round(pump.volume, 2);
                    }
                }
                AdjustAxisYMaximumInternal(Chart1);

                double totalVolume = Math.Round(Chart1.Series[0].DataPoints.Sum(p => p.YValue), 2);
                Chart1_Title.Text = locationName + " Station Drainage Pump Total Volume (" + totalVolume + "L)";
            }
        }

        private void UpdateChart2(List<Pump> changedPumpList)
        {
            if (Chart2.Tag != null)
            {
                string locationName = Chart1.Tag.ToString();
                foreach (var dp in Chart2.Series[0].DataPoints)
                {
                    Pump pump = changedPumpList.Where(p => p.locationName == locationName && p.name == dp.XValue.ToString()).FirstOrDefault();
                    if (pump != null)
                    {
                        dp.YValue = Math.Round(pump.volume, 2);
                    }
                }
                AdjustAxisYMaximumInternal(Chart2);

                double totalVolume = Math.Round(Chart2.Series[0].DataPoints.Sum(p => p.YValue), 2);
                Chart2_Title.Text = locationName + " Tunnel Pump Total Volume (" + totalVolume + "L)";
            }
        }

        private void UpdateChart3(List<Pump> changedPumpList)
        {
            //var groupResult = pumpList.GroupBy(p => p.locationName).Select(group => new { locationName = group.Key, volume = group.Sum(p => p.volume) }).ToList();
            List<string> changedLocationList = changedPumpList.Select(p => p.locationName).Distinct().ToList();
            foreach (string locationName in changedLocationList)
            {
                DataPoint datapoint = Chart3.Series[0].DataPoints.Where(p => p.XValue.ToString() == locationName).FirstOrDefault();
                if (datapoint != null)
                {
                    datapoint.YValue = pumpList.Where(p => p.locationName == locationName).Sum(p => p.volume);
                }
            }
            AdjustAxisYMaximumInternal(Chart3);
        }


        private void UpdateAll(List<Pump> changedPumpList)
        {
            UpdateChart1(changedPumpList);
            UpdateChart2(changedPumpList);
            UpdateChart3(changedPumpList);
        }

        void LoadScript()
        {
            InitAll();          //Put InitAll here because LoadScript execute after binding all Chart to data, the Axes and legend created already
            if (IscsCachedMap.Instance.LocalTest == false)
            {
                DebugUtil.Instance.LOG.Info("Reload all pumps from db");
                ReloadAllPumpFromDb();           //this function will exectue in timerPoll_Tick for the first time
            }
            else
            {
                DebugUtil.Instance.LOG.Info("Load dummy pump level");
                LoadDummyHistoryAlarm();
            }

            /*if (locationList.Count >= 2)
            {
                ChangeChart1Location(locationList[0].Name);
                ChangeChart2Location(locationList[0].Name);
            }*/

            timerPoll = new DispatcherTimer();
            timerPoll.Interval = TimeSpan.FromMilliseconds(pollInterval);
            timerPoll.Tick += new EventHandler(timerPoll_Tick);
            timerPoll.Start();

            timerBlink = new DispatcherTimer();
            timerBlink.Interval = TimeSpan.FromMilliseconds(blinkInterval);
            timerBlink.Tick += new EventHandler(timerBlink_Tick);
            timerBlink.Start();
        }

        //Clear all and reload all alarm from Db
        private void ReloadAllPumpFromDb()
        {   //reset all data to 0
            pumpList = DAIHelper.Instance.GetAllPumpFromDb();            //avoid duplicate, 0 mean IscsAlarm

            UpdateChart3(pumpList);
        }

        private int previousDay = DateTime.Now.Day;
        private void timerPoll_Tick(object sender, EventArgs e)
        {
            List<Pump> changedPumpList = DAIHelper.Instance.GetAllChangedPumpFromDb();
            DebugUtil.Instance.LOG.Debug("Read total " + changedPumpList.Count() + " pump volume changed");

            if (changedPumpList != null && changedPumpList.Count() > 0)
            {   //update all pump list
                foreach (Pump pumpChanged in changedPumpList)
                {
                    Pump pump = pumpList.Where(p => p.name == pumpChanged.name && p.locationName == pumpChanged.locationName).FirstOrDefault();
                    pump.volume = pumpChanged.volume;
                }
                UpdateAll(changedPumpList);
                DebugUtil.Instance.LOG.Debug("Update Pump GUI complete");
                DAIHelper.Instance.UpdateAllPumpToUnchanged();          //reset changed flag
            }
        }

        int blinkCounter = 0;
        private void timerBlink_Tick(object sender, EventArgs e)
        {
            if (pumpThreshold > 0)
            {
                for (int i = 0; i < Chart1.Series[0].DataPoints.Count(); i++)
                {
                    DataPoint dp = Chart1.Series[0].DataPoints[i];
                    if (dp.YValue >= pumpThreshold)
                    {
                        //if (dp.Color == oldBrushList[i])
                        if (blinkCounter == 0)
                        {
                            dp.Color = newBrushList_Chart1[i];
                            dp.LabelFontColor = Brushes.Gray;
                        }
                        else
                        {
                            dp.Color = oldBrushList_Chart1[i];
                            dp.LabelFontColor = Brushes.White;
                        }
                    }
                }
                for (int i = 0; i < Chart2.Series[0].DataPoints.Count(); i++)
                {
                    DataPoint dp = Chart2.Series[0].DataPoints[i];
                    if (dp.YValue >= pumpThreshold)
                    {
                        //if (dp.Color == oldBrushList[i])
                        if (blinkCounter == 0)
                        {
                            dp.Color = newBrushList_Chart2[i];
                            dp.LabelFontColor = Brushes.Gray;
                        }
                        else
                        {
                            dp.Color = oldBrushList_Chart2[i];
                            dp.LabelFontColor = Brushes.White;
                        }
                    }
                }
                blinkCounter = (blinkCounter + 1) % 2;
            }
        }
    }
}
