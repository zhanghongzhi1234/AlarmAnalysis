using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Visifire.Charts;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.Windows.Media.Animation;


namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for Template.xaml
    /// </summary>
    public partial class PumpDashboard : Window
    {
        public static readonly log4net.ILog log = DebugUtil.Instance.LOG;

        private Dictionary<string, Server> serverMap = new Dictionary<string, Server>();
        private Dictionary<string, DataSource> dataSourceMap = new Dictionary<string, DataSource>();
        private Dictionary<string, ChartData> chartDataMap = new Dictionary<string, ChartData>();
        //private List<TextData> textDataList = new List<TextData>();         //TextBox binding
        private Dictionary<string, string> variableMap = new Dictionary<string, string>();      //Variable mapping
        private Dictionary<string, string> mappingMap = new Dictionary<string, string>();
        private Dictionary<string, TriggerData> triggerMap = new Dictionary<string, TriggerData>();

        DispatcherTimer timerRefresh;
        private int refreshInterval = 3000;

        DispatcherTimer timerAutoSwitch;
        private int autoSwtichInterval = 6000;

        private TextBox currentEditableTB = null;
        private NavWindow navWindow;
        private string currentFileName = "PumpDashboard.xaml.cs";

        public PumpDashboard()
        {
            InitializeComponent();
            navWindow = new NavWindow(this);

            try
            {
                ParseTemplate(currentFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                System.Environment.Exit(0);
            }

            this.Loaded += new RoutedEventHandler(Window1_Loaded);
            //Start the timerPoll
            timerRefresh = new DispatcherTimer();
            timerRefresh.Interval = TimeSpan.FromMilliseconds(refreshInterval);
            timerRefresh.Tick += new EventHandler(timerRefresh_Tick);
            timerRefresh.Start();

            //Start the timerAutoSwitch
            timerAutoSwitch = new DispatcherTimer();
            timerAutoSwitch.Interval = TimeSpan.FromMilliseconds(autoSwtichInterval);
            timerAutoSwitch.Tick += new EventHandler(timerAutoSwitch_Tick);
            timerAutoSwitch.Start();
            foreach (Button child in FindVisualChildren<Button>(Canvas))
            {
                if (child.Name == "btnExport")
                {
                    //child.Click += new RoutedEventHandler(btnExport_Click);
                    child.Click += btnExport_Click;
                    child.MouseEnter += menuButton_MouseEnter;
                    child.MouseLeave += menuButton_MouseLeave;
                }
                else if (child.Name == "btnExit")
                {
                    child.Click += btnExit_Click;
                    child.MouseEnter += menuButton_MouseEnter;
                    child.MouseLeave += menuButton_MouseLeave;
                }
                else if (child.Name == "btnNavigation")
                    child.Click += btnNavigation_Click;
            }
            foreach (TextBlock child in FindVisualChildren<TextBlock>(Canvas))
            {
                if (child.Tag != null && child.Tag.ToString().Contains("Maximize"))
                {
                    child.MouseLeftButtonDown += btnMaximize_Click;
                    child.MouseEnter += textBlock_MouseEnter;
                    child.MouseLeave += textBlock_MouseLeave;
                }
            }
            foreach (var item in triggerMap)
            {
                object UIElement = this.FindName(item.Key);
                if (UIElement is Button)
                {
                    Button btnQuery = UIElement as Button;
                    btnQuery.Click += btnQuery_Click;
                }
            }
            InitScript();
            this.Closed += Window_Closed_1;
        }

        private bool ParseTemplate(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            //string thisFile = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
            //string defFile = thisFile.Replace(".xaml.cs", ".xml.def");
            string defFile = fileName.Replace(".xaml.cs", ".xml.def");
            string variableResourceName = "TemplateProject.Template." + System.IO.Path.GetFileName(defFile);     //Namespace + folder + filename

            //var variableResourceName = "TemplateProject.Template.xml";          //use embedded resource
            Stream streamVariable = assembly.GetManifestResourceStream(variableResourceName);
            if (streamVariable == null)
                return false;
            XmlDocument xmlDocVariable = new XmlDocument();
            xmlDocVariable.Load(streamVariable);

            /*var schemaResourceName = "TemplateProject.Template.xaml";          //use embedded resource
            Stream streamSchema = assembly.GetManifestResourceStream(schemaResourceName);
            XmlDocument xmlDocSchema = new XmlDocument();
            xmlDocSchema.Load(streamSchema);*/

            //Parse Server
            XmlNodeList nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Server");
            foreach (XmlNode childNode in nodeList)
            {
                Server server = null;
                string name = "";
                if (childNode.Attributes["Type"].Value == "MYSQL")
                {
                    name = childNode.Attributes["Name"].Value;
                    string IP = childNode.Attributes["IP"].Value;
                    string username = childNode.Attributes["UserName"].Value;
                    string password = childNode.Attributes["Password"] != null ? childNode.Attributes["Password"].Value : "";
                    string databaseName = childNode.Attributes["DatabaseName"].Value;
                    server = new MysqlServer(name, IP, username, password, databaseName);
                    serverMap[name] = server;
                }
                else if (childNode.Attributes["Type"].Value == "TCPIP")
                {
                    name = childNode.Attributes["Name"].Value;
                    string IP = childNode.Attributes["IP"].Value;
                    string port = childNode.Attributes["Port"].Value;
                    server = new TCPIPServer(name, IP, port);

                }
                else if (childNode.Attributes["Type"].Value == "SCADA")
                {
                    name = childNode.Attributes["Name"].Value;
                    server = new SCADAServer(name);
                }
                else if (childNode.Attributes["Type"].Value == "SQLITE")
                {
                    name = childNode.Attributes["Name"].Value;
                    string databaseName = childNode.Attributes["DatabaseName"].Value;
                    server = new SQLiteServer(name, databaseName);
                    serverMap[name] = server;
                }

                if (server != null)
                {
                    server.Init();
                    serverMap[name] = server;
                }
            }

            //Parse DataSource
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/DataSource");
            foreach (XmlNode childNode in nodeList)
            {
                string name = childNode.Attributes["Name"].Value;
                string serverName = childNode.Attributes["Server"] != null ? childNode.Attributes["Server"].Value : null;
                Server server = null;
                if (serverName != null)
                {
                    if (serverMap.ContainsKey(serverName) == false)
                    {
                        log.Info("Error: Cannot find Server:" + serverName + " for DataSource:" + name);
                        continue;
                    }
                    server = serverMap[serverName];
                }
                string table = childNode.Attributes["Channel"] != null ? childNode.Attributes["Channel"].Value : null;
                string type = childNode.Attributes["Type"] != null ? childNode.Attributes["Type"].Value : "Static";
                string ChartType = childNode.Attributes["ChartType"] != null ? childNode.Attributes["ChartType"].Value : null;
                string LegendText = childNode.Attributes["LegendText"] != null ? childNode.Attributes["LegendText"].Value : null;
                string color = childNode.Attributes["Color"] != null ? childNode.Attributes["Color"].Value : null;
                string LineThickness = childNode.Attributes["LineThickness"] != null ? childNode.Attributes["LineThickness"].Value : null;
                string LabelEnabled = childNode.Attributes["LabelEnabled"] != null ? childNode.Attributes["LabelEnabled"].Value : null;
                string LabelFormat = childNode.Attributes["LabelFormat"] != null ? childNode.Attributes["LabelFormat"].Value : null;
                string LabelStyle = childNode.Attributes["LabelStyle"] != null ? childNode.Attributes["LabelStyle"].Value : null;
                string LabelSuffix = childNode.Attributes["LabelSuffix"] != null ? childNode.Attributes["LabelSuffix"].Value : null;
                string Exploded = childNode.Attributes["Exploded"] != null ? childNode.Attributes["Exploded"].Value : null;
                string AxisYType = childNode.Attributes["AxisYType"] != null ? childNode.Attributes["AxisYType"].Value : null;
                string MarkerType = childNode.Attributes["MarkerType"] != null ? childNode.Attributes["MarkerType"].Value : null;
                string MarkerEnabled = childNode.Attributes["MarkerEnabled"] != null ? childNode.Attributes["MarkerEnabled"].Value : null;
                string XValueType = childNode.Attributes["XValueType"] != null ? childNode.Attributes["XValueType"].Value : null;
                string ShowInLegend = childNode.Attributes["ShowInLegend"] != null ? childNode.Attributes["ShowInLegend"].Value : null;
                DataSource dataSource = new DataSource(name, serverName, table, type, ChartType, LegendText, color, LineThickness, LabelEnabled, LabelFormat, LabelStyle, LabelSuffix, Exploded, AxisYType, MarkerType, MarkerEnabled, XValueType, ShowInLegend);
                dataSource.SetServer(server);
                //if (ChartType != null)
                //    dataSource.Reload(data);        //Load Data for Datasource, only for Chart
                dataSourceMap[name] = dataSource;
            }

            //Parse Variable
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Variable");
            foreach (XmlNode childNode in nodeList)
            {
                variableMap[childNode.Attributes["Name"].Value] = childNode.Attributes["Source"].Value;
            }

            //Parse Mapping
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Mapping");
            foreach (XmlNode childNode in nodeList)
            {
                mappingMap[childNode.Attributes["UIElement"].Value] = childNode.Attributes["DataSource"].Value;
            }

            //Parse Trigger
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Trigger");
            foreach (XmlNode childNode in nodeList)
            {
                //TriggerData triggerData = new TriggerData(childNode.Attributes["Button"].Value, childNode.Attributes["DataSource"].Value, childNode.Attributes["Target"].Value);
                TriggerData triggerData = new TriggerData(childNode.Attributes["Button"].Value, childNode.Attributes["DataSource"].Value);
                triggerMap[childNode.Attributes["Button"].Value] = triggerData;
            }

            //Parse Chart from schema
            /** Chart1 will autoswitch bewteen 2 datasource, but Chart2 will show several data source together
             *  <Mapping UIElement="Chart1" DataSource="Data1"/>
             *  <Mapping UIElement="Chart1" DataSource="Data2"/>
                <Mapping UIElement="Chart2" DataSource="Rule3,Rule4,Rule5,Rule6,Rule7"/>
             **/
            //XmlNodeList nodeList = xmlDocVariable.SelectNodes("/Canvas/Chart");
            //nodeList = xmlDocSchema.GetElementsByTagName("charts:Chart");
            //foreach (XmlNode childNode in nodeList)
            foreach (Chart chart in FindVisualChildren<Chart>(Canvas))
            {
                if (mappingMap.ContainsKey(chart.Name))     //Check Chart DataSource
                {
                    ChartData chartData = new ChartData();
                    chartData.name = chart.Name;
                    //chartData.FreeText = childNode.Attributes["FreeText"] != null ? childNode.Attributes["FreeText"].Value : null;
                    string dataSourceArray = mappingMap[chartData.name];
                    if (dataSourceArray != null && dataSourceArray.Length > 0)
                    {
                        chartData.dataSourceList.AddRange(dataSourceArray.Split(','));
                        chartDataMap[chartData.name] = chartData;
                        SetChart(chartData.name, chartData);
                    }
                }
            }

            //Parse TextBlock
            //XmlNodeList nodeList = xmlDocVariable.SelectNodes("/Canvas/Chart");
            /*nodeList = xmlDocVariable.GetElementsByTagName("TextBlock");
            foreach (XmlNode childNode in nodeList)
            {
                if (childNode.Attributes["DataSource"] != null)
                {
                    string name = childNode.Attributes["Name"].Value;
                    string dataSourceName = childNode.Attributes["DataSource"].Value;
                    TextData textData = new TextData(name, dataSourceName);
                    textDataList.Add(textData);
                }
            }*/

            return true;
        }

        void SetChart(string chartName, ChartData chartData)
        {
            Chart chart = (Chart)this.FindName(chartName);

            //test code
            /*            DataSeries dataSeries = new DataSeries();
                        DataPoint dataPoint = new DataPoint();
                        dataPoint.XValue = 1;
                        dataPoint.AxisXLabel = "照明";
                        dataPoint.YValue = 50;
                        dataSeries.DataPoints.Add(dataPoint);
                        dataPoint = new DataPoint();
                        dataPoint.XValue = 2;
                        dataPoint.AxisXLabel = "电力";
                        dataPoint.YValue = 60;
                        dataSeries.DataPoints.Add(dataPoint);
                        dataPoint = new DataPoint();
                        dataPoint.XValue = 3;
                        dataPoint.AxisXLabel = "动力";
                        dataPoint.YValue = 40;
                        dataSeries.DataPoints.Add(dataPoint);
                        dataSeries.RenderAs = RenderAs.Pie;
                        Chart1.Series.Add(dataSeries);
                        return;*/
            // test code end

            chart.IsHitTestVisible = false;     //Don't accept mouse event when set chart data
            chart.Series.Clear();
            try
            {
                foreach (string dataSourceName in chartData.dataSourceList)
                {
                    if (dataSourceName == "Data_SCADA")
                    {
                        DataSource dataSource = dataSourceMap[dataSourceName];
                        SCADAServer scadaServer = dataSource.server as SCADAServer;
                        if (scadaServer != null)
                        {
                            scadaServer.SetObserver(chart, dataSource.table);
                            chart.Series.Add(dataSource.dataSeries);
                        }
                    }
                    else
                    {
                        DataSource dataSource = dataSourceMap[dataSourceName];
                        chart.Series.Add(dataSource.dataSeries);
                    }
                }
                /*if (chartData.FreeText == "true")
                {
                    chart.MouseLeftButtonDown -= new MouseButtonEventHandler(mouseLeftBtnDown);
                    chart.MouseLeftButtonDown += new MouseButtonEventHandler(mouseLeftBtnDown);
                }*/
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            chart.IsHitTestVisible = true;          //resume mouse event
        }

        //loadAll = true, reload all data, loadAll = false, reload dynamic data only
        private void Reload(bool loadAll = false)
        {
            foreach (KeyValuePair<string, DataSource> entry in dataSourceMap)
            {
                if (loadAll == true || (loadAll == false && entry.Value.type == "Dynamic"))
                {   //for chart
                    entry.Value.ReloadChartData();
                }
            }
            //for other control
            foreach (var item in mappingMap)
            {
                object UIElement = this.FindName(item.Key);
                if (!(UIElement is Chart))      //Chart maybe have many dataSource in item.value, it will crash use dataSourceMap[item.Value
                {
                    DataSource dataSource = dataSourceMap[item.Value];
                    if (UIElement is TextBlock)
                    {   //TextBlock will always be dynamic
                        TextBlock textBlock = UIElement as TextBlock;
                        textBlock.Text = dataSource.GetSingleData();
                    }
                    else if (UIElement is ListView && loadAll == true)
                    {
                        ListView listView1 = UIElement as ListView;
                        DataTable dt = dataSource.GetData();
                        FillListViewWithDataTable(listView1, dt);
                    }
                    else if (UIElement is DataGrid && loadAll == true)
                    {
                        DataGrid dataGrid1 = UIElement as DataGrid;
                        DataTable dt = dataSource.GetData();
                        FillDataGridWithDataTable(dataGrid1, dt);
                    }
                    else if (UIElement is TreeView && loadAll == true)
                    {
                        TreeView treeView1 = UIElement as TreeView;
                        //DataTable dt = dataSource.GetData();              //to be done
                        //FillTreeViewWithDataTable(treeView1, dt);
                    }
                    else if (UIElement is ComboBox && loadAll == true)
                    {
                        ComboBox comboBox = UIElement as ComboBox;
                        List<string> list = dataSource.GetSingularData();
                        foreach (string str in list)
                        {
                            comboBox.Items.Add(str);
                        }
                        comboBox.SelectedIndex = 0;
                    }
                }

            }
        }

        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            Reload(true);
            LoadScript();
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            Reload(false);
        }

        private void timerAutoSwitch_Tick(object sender, EventArgs e)
        {
            /*foreach (KeyValuePair<string, ChartDataCollection> entry in chartDataMap)
            {
                ChartDataCollection chartDataCollection = entry.Value;
                if (chartDataCollection.chartDataList.Count() > 1)
                {
                    ChartData chartData = chartDataCollection.GetNextChartDataCycle();
                    SetChart(entry.Key, chartData);
                }
            }*/
        }

        private void mouseLeftBtnDown(object sender, MouseButtonEventArgs e)
        {
            if (currentEditableTB == null)
            {
                Point p = e.GetPosition(Canvas);
                currentEditableTB = new TextBox();
                currentEditableTB.Margin = new Thickness(p.X, p.Y, Canvas.ActualWidth - 100 - p.X, Canvas.ActualHeight - p.Y - 20);
                Canvas.Children.Add(currentEditableTB);
            }
            else
            {
                var textBlock = new TextBlock();
                textBlock.Text = currentEditableTB.Text;
                textBlock.Margin = currentEditableTB.Margin;
                Canvas.Children.Remove(currentEditableTB);
                currentEditableTB = null;
                Canvas.Children.Add(textBlock);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void menuButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;
            btn.Foreground = Brushes.Black;
        }

        private void menuButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;
            btn.Foreground = Brushes.White;
        }

        private double GetRandom(double minimum, double maximum, Random rnd = null)
        {
            double ret = minimum;
            double range = maximum - minimum;
            int rangeMin = 1000;
            if (rnd == null)
                rnd = new Random();
            if (range == 0d)
            {
                ret = minimum;
            }
            else if (range < rangeMin)
            {
                double zoom = rangeMin / range;
                int temp = rnd.Next(0, rangeMin);
                ret = minimum + temp / zoom;
            }
            if (range <= 1 && range > 0)
                ret = Math.Round(ret, 2);
            else
                ret = Math.Round(ret, 0);
            return ret;
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj, string tag) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is FrameworkElement)
                    {
                        FrameworkElement el = child as FrameworkElement;
                        if (el.Tag != null && el.Tag.ToString() == tag)
                            yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child, tag))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            string fileName = @".\new.xlsx";
            if (this.Title != "")
                fileName = this.Title + ".xlsx";
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = fileName; // Default file name
            dlg.DefaultExt = ".xlsx"; // Default file extension
            dlg.Filter = "Excel documents (.xlsx)|*.xlsx"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {   // Save document
                fileName = dlg.FileName;
            }
            else
                return;

            FileInfo newFile = new FileInfo(fileName);
            if (newFile.Exists)
            {
                try
                {
                    newFile.Delete();  // ensures we create a new workbook
                    newFile = new FileInfo(fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Export");
                ws.Cells.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                ws.Cells.Style.Font.SetFromFont(new System.Drawing.Font("Courier New", 10));

                Tuple<int, int> blankRowRange = new Tuple<int, int>(1, 100);
                Tuple<int, int> blankColRange = new Tuple<int, int>(1, 100);

                var TransformToPixelX = new Func<double, int>(unitX =>
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    {
                        return (int)((g.DpiX / 96) * unitX);
                    }
                });

                var TransformToPixelY = new Func<double, int>(unitY =>
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    {
                        return (int)((g.DpiY / 96) * unitY);
                    }
                });

                double defaultCellWidth = 0.66; //inch
                double perCellPointWidthInInch = defaultCellWidth / 8.43;

                foreach (ListView child in FindVisualChildren<ListView>(Canvas))
                {
                    Point pos = child.PointToScreen(new Point(0, 0));
                    GridView gView = child.View as GridView;
                    if (gView == null)
                        continue;

                    double top = 0;
                    int beginRow = 1, beginCol = 1;
                    while (top / 72 < pos.Y / 96)
                    {
                        top += ws.Row(beginRow).Height;
                        ++beginRow;
                    }

                    //ws.Row(beginRow - 1).Height -= (top / 72 - pos.Y / 96) * 72;

                    double cols = Math.Ceiling(pos.X / 96 / defaultCellWidth);
                    beginCol = Convert.ToInt32(cols) + 1;

                    //ws.Column(beginCol - 1).Width *= ((pos.X / 96 / defaultCellWidth) - Math.Floor(pos.X / 96 / defaultCellWidth));

                    int i = beginRow, j = beginCol;
                    if (i - 1 < blankRowRange.Item2)
                        blankRowRange = new Tuple<int, int>(blankRowRange.Item1, i - 1);

                    if (j - 1 < blankColRange.Item2)
                        blankColRange = new Tuple<int, int>(blankColRange.Item1, j - 1);

                    Dictionary<GridViewColumn, Tuple<int, int>> colIndexMap = new Dictionary<GridViewColumn, Tuple<int, int>>();

                    bool titleSet = false;
                    foreach (GridViewColumn col in gView.Columns)
                    {
                        if (!titleSet)
                        {
                            ws.Cells[i - 1, j].Value = ((child.Parent as FrameworkElement).Parent as FrameworkElement).Tag;
                            titleSet = true;
                        }
                        ws.Cells[i, j].Value = col.Header as string;
                        //ws.Column(j).Width = col.ActualWidth / 96 / perCellPointWidthInInch;
                        int jj = j + Convert.ToInt32(Math.Ceiling(col.ActualWidth / 96 / defaultCellWidth));
                        Tuple<int, int> t = new Tuple<int, int>(j, jj - 1);
                        colIndexMap.Add(col, t);
                        j = jj;
                    }
                    foreach (object obj in child.Items)
                    {
                        ++i;
                        foreach (GridViewColumn col in gView.Columns)
                        {
                            Binding bd = col.DisplayMemberBinding as Binding;
                            //object val = obj.GetType().GetProperty(bd.Path.Path).GetValue(obj, null);         //Cannot use for ExpandoObject
                            object val = (obj as IDictionary<string, object>)[bd.Path.Path];
                            if (val != null)
                            {
                                ws.Cells[i, colIndexMap[col].Item1].Value = val;
                            }
                        }
                    }
                    //ws.Cells[beginRow, beginCol, i, j].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    foreach (var z in colIndexMap.Values)
                    {
                        ws.Cells[beginRow, beginCol, i, z.Item2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }
                }

                const int loadRowIndex = 200;
                int loadColIndex = 200;
                foreach (Chart child in FindVisualChildren<Chart>(Canvas))
                {
                    if (child.Series.Count == 0)
                        continue;

                    Point pos = child.PointToScreen(new Point(0, 0));
                    Vector szVec = child.PointToScreen(new Point(child.ActualWidth, child.ActualHeight)) - child.PointToScreen(new Point(0, 0));
                    Size sz = new Size(szVec.X, szVec.Y);

                    OfficeOpenXml.Drawing.Chart.eChartType ChartType = 0;
                    if (child.Series[0].RenderAs == RenderAs.Column)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.ColumnStacked;
                    else if (child.Series[0].RenderAs == RenderAs.Line)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.Line;
                    else if (child.Series[0].RenderAs == RenderAs.Pie)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.Pie;
                    else if (child.Series[0].RenderAs == RenderAs.Bar)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.BarStacked;
                    else if (child.Series[0].RenderAs == RenderAs.Area)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.Area;
                    else if (child.Series[0].RenderAs == RenderAs.Doughnut)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.DoughnutExploded;
                    else if (child.Series[0].RenderAs == RenderAs.StackedColumn)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.ColumnStacked;
                    else if (child.Series[0].RenderAs == RenderAs.StackedColumn100)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.ColumnStacked100;
                    else if (child.Series[0].RenderAs == RenderAs.StackedBar)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.BarStacked;
                    else if (child.Series[0].RenderAs == RenderAs.StackedBar100)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.BarStacked100;
                    else if (child.Series[0].RenderAs == RenderAs.StackedArea)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.AreaStacked;
                    else if (child.Series[0].RenderAs == RenderAs.StackedArea100)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.AreaStacked100;
                    else if (child.Series[0].RenderAs == RenderAs.Bubble)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.Bubble;
                    else if (child.Series[0].RenderAs == RenderAs.Point)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.Bubble;          //Point not implemented
                    else if (child.Series[0].RenderAs == RenderAs.StreamLineFunnel)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.PyramidBarStacked;
                    else if (child.Series[0].RenderAs == RenderAs.SectionFunnel)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.CylinderCol;
                    else if (child.Series[0].RenderAs == RenderAs.Stock)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.StockHLC;
                    else if (child.Series[0].RenderAs == RenderAs.CandleStick)
                        ChartType = OfficeOpenXml.Drawing.Chart.eChartType.StockVHLC;

                    var chartObj = ws.Drawings.AddChart(child.Name, ChartType);
                    var pieChar = chartObj as ExcelPieChart;
                    if (pieChar != null)
                    {
                        pieChar.DataLabel.ShowPercent = true;
                    }

                    chartObj.SetPosition(TransformToPixelY(pos.Y), TransformToPixelX(pos.X));
                    chartObj.SetSize(TransformToPixelX(sz.Width), TransformToPixelY(sz.Height));
                    chartObj.Legend.Position = eLegendPosition.Bottom;
                    if (chartObj.From.Column - 1 < blankColRange.Item2)
                        blankColRange = new Tuple<int, int>(blankColRange.Item1, chartObj.From.Column - 1);
                    if (chartObj.From.Row - 1 < blankRowRange.Item2)
                        blankRowRange = new Tuple<int, int>(blankRowRange.Item1, chartObj.From.Row - 1);

                    if (child.ToolTip != null)
                        chartObj.Title.Text = child.ToolTip.ToString();
                    else if (child.Tag != null)
                        chartObj.Title.Text = child.Tag.ToString();

                    var chartAxesX = child.AxesX[0];
                    foreach (DataSeries ds in child.Series)
                    {
                        List<double> values = new List<double>();
                        List<string> xvalues = new List<string>();
                        foreach (DataPoint dp in ds.DataPoints)
                        {
                            values.Add(dp.YValue);
                            /*if (child.Name == "Chart1")      //show tooltip
                                xvalues.Add(dp.XValue.ToString() + chartAxesX.Suffix + " - " + dp.YValue + "\r\n" + dp.ToolTipText);
                            else*/
                                xvalues.Add(dp.XValue.ToString() + chartAxesX.Suffix);
                        }

                        if (values.Count == 0)
                            continue;

                        ws.Cells[loadRowIndex, loadColIndex].LoadFromCollection(values);
                        ws.Cells[loadRowIndex, loadColIndex + 1].LoadFromCollection(xvalues);
                        ExcelChartSerie s = chartObj.Series.Add(ExcelRange.GetAddress(loadRowIndex, loadColIndex, values.Count() + loadRowIndex - 1, loadColIndex),
                            ExcelRange.GetAddress(loadRowIndex, loadColIndex + 1, values.Count() + loadRowIndex - 1, loadColIndex + 1));

                        loadColIndex += 2;

                        s.Header = ds.LegendText;
                        if (ds.Color == null)
                            continue;

                        byte r = ((SolidColorBrush)ds.Color).Color.R;
                        byte g = ((SolidColorBrush)ds.Color).Color.G;
                        byte b = ((SolidColorBrush)ds.Color).Color.B;
                        s.Fill.Color = System.Drawing.Color.FromArgb((int)r, (int)g, (int)b);
                    }
                }

                if (blankRowRange.Item2 >= blankRowRange.Item1 && blankRowRange.Item1 >= 0)
                {
                    foreach (int i in Enumerable.Range(blankRowRange.Item1, blankRowRange.Item2))
                        ws.Row(i).Hidden = true;
                }
                if (blankColRange.Item2 >= blankColRange.Item1 && blankColRange.Item1 >= 0)
                {
                    foreach (int i in Enumerable.Range(blankColRange.Item1, blankColRange.Item2))
                        ws.Column(i).Hidden = true;
                }
                package.SaveAs(newFile);
            }
        }

        private List<string> ParseRule(string rule)
        {   //get all variable name from rule
            List<string> ret = new List<string>();
            string pattern = @"\?([^)]*?)\?";
            foreach (Match match in Regex.Matches(rule, pattern))
            {
                ret.Add(match.Value);
            }
            return ret;
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            //test code

            //Chart chart = Chart1;// UIElementTarget as Chart;
            /*Chart1.Series.Clear();
            //DataSeries dataSeries = Chart1.Series[0];
            //dataSeries.DataPoints.Clear();
            DataSeries dataSeries = new DataSeries();
            DataPoint dataPoint = new DataPoint();
            dataPoint.XValue = 1;
            dataPoint.AxisXLabel = "照明";
            dataPoint.YValue = 50;
            dataSeries.DataPoints.Add(dataPoint);
            dataPoint = new DataPoint();
            dataPoint.XValue = 2;
            dataPoint.AxisXLabel = "电力";
            dataPoint.YValue = 60;
            dataSeries.DataPoints.Add(dataPoint);
            dataPoint = new DataPoint();
            dataPoint.XValue = 3;
            dataPoint.AxisXLabel = "动力";
            dataPoint.YValue = 40;
            dataSeries.DataPoints.Add(dataPoint);
            dataSeries.RenderAs = RenderAs.Pie;
            Chart1.Series.Add(dataSeries);
            return;*/

            if (sender is Button)
            {
                Button btnQuery = sender as Button;
                if (triggerMap.ContainsKey(btnQuery.Name))
                {
                    TriggerData triggerData = triggerMap[btnQuery.Name];
                    string[] dataSourceNameArray = triggerData.DataSourceName.Split(',');
                    foreach (string dataSourceName in dataSourceNameArray)
                    {
                        foreach (var mapping in mappingMap)
                        {   //go through all mapping to see if contain this datasource
                            if (mapping.Value.Contains(dataSourceName) && dataSourceMap.ContainsKey(dataSourceName))
                            {
                                DataSource dataSource = dataSourceMap[dataSourceName];
                                string rule = dataSource.table;
                                List<string> variableList = ParseRule(rule);
                                foreach (string variable in variableList)       //go throught all variable
                                {
                                    string value = "";
                                    string var = variable.Trim('?');
                                    if (!variableMap.ContainsKey(var))
                                    {
                                        MessageBox.Show("Variable " + var + " do not exist in rule: " + rule, "Error");
                                        return;
                                    }
                                    object UIElement = this.FindName(variableMap[var]);
                                    if (UIElement == null)
                                    {
                                        MessageBox.Show("Control: " + variableMap[var] + " do not exist mapping to variable: " + var + " in rule: " + rule, "Error");
                                        return;
                                    }
                                    if (UIElement is ComboBox)
                                        value = (UIElement as ComboBox).SelectedValue.ToString();
                                    else if (UIElement is TextBox)
                                        value = (UIElement as TextBox).Text;
                                    else if (UIElement is DatePicker)
                                        value = ((UIElement as DatePicker).SelectedDate ?? DateTime.Now).ToString("yyyy-M-d H:m:s");

                                    rule = rule.Replace(variable, value);
                                }

                                DataTable dt = dataSource.GetQueryData(rule);
                                if (dt != null)
                                {
                                    object UIElementTarget = this.FindName(mapping.Key);
                                    if (UIElementTarget is ListView)
                                    {
                                        ListView listView1 = UIElementTarget as ListView;
                                        FillListViewWithDataTable(listView1, dt);
                                    }
                                    else if (UIElementTarget is DataGrid)
                                    {
                                        DataGrid dataGrid1 = UIElementTarget as DataGrid;
                                        FillDataGridWithDataTable(dataGrid1, dt);
                                    }
                                    else if (UIElementTarget is Chart)
                                    {
                                        dataSource.ReloadChartData(dt);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FillListViewWithDataTable(ListView lstView, DataTable dt)
        {
            lstView.Items.Clear();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dynamic item = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)item;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    string columnName = dt.Columns[j].ColumnName;
                    dictionary[columnName] = dt.Rows[i][j];
                }
                lstView.Items.Add(item);
            }
        }

        private void FillDataGridWithDataTable(DataGrid dataGrid, DataTable dt)
        {
            dataGrid.Items.Clear();
            /*foreach (DataColumn column in dt.Columns)
            {
                int width = (int)dataGrid1.ActualWidth / dt.Columns.Count;
                dataGrid1.Columns.Add(CreateLabelColumn(column.ColumnName, width, column.ColumnName));
            }*/
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dynamic item = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)item;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    string columnName = dt.Columns[j].ColumnName;
                    dictionary[columnName] = dt.Rows[i][j];
                }
                dataGrid.Items.Add(item);
            }
        }

        private void btnNavigation_Click(object sender, RoutedEventArgs e)
        {
            navWindow.AnimationShow();
        }

        private void item_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            {
                Cursor cursor = CursorHelper.CreateCursor(e.Source as UIElement);
                e.UseDefaultCursors = false;
                Mouse.SetCursor(cursor);
            }

            e.Handled = true;
        }

        private Chart GetNearestChart(FrameworkElement fe)
        {
            Chart ret = null;
            FrameworkElement currentNode = fe;
            bool bFound = false;
            while (bFound == false)
            {
                DependencyObject dep = currentNode.Parent;
                if (dep != null && dep is FrameworkElement)
                {
                    FrameworkElement parentNode = dep as FrameworkElement;
                    IEnumerable<Chart> charts = FindVisualChildren<Chart>(parentNode);
                    if (charts.Count() >= 1)
                    {
                        ret = charts.FirstOrDefault();
                        bFound = true;
                    }
                    else
                        currentNode = parentNode;
                }
                else
                    break;
            }
            return ret;
        }

        Panel container;     //make sure RootGrid have only 1 child all time, so the MaximizedDockPanel can be added as the second child
        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            // Get to the DockPanel
            /*Button maximizeButton = (Button)sender;
            Border border = (Border)maximizeButton.Parent;
            gridContainer = (Grid)border.Parent;
            UIElement widget = gridContainer.Children[2];*/

            FrameworkElement fe = (FrameworkElement)sender;
            string commandText = fe.Tag.ToString();
            string[] cmdArray = commandText.Split('?');
            if (cmdArray.Count() == 0)
                return;
            else if (cmdArray[0] != "Maximize")
                return;

            //find target
            FrameworkElement widget = null;
            if (cmdArray.Count() >= 2)
            {
                string[] cmdTarget = cmdArray[1].Split('=');
                if (cmdTarget.Count() >= 2 && cmdTarget[0].Trim() == "Target")
                {   //find target name to be maximized
                    widget = (FrameworkElement)this.FindName(cmdTarget[1].Trim());
                }
            }
            if (widget == null)
                widget = GetNearestChart(fe);
            //Chart widget = GetNearestChart(fe);
            container = widget.Parent as Panel;
            if (container != null)
                container.Children.Remove(widget);

            // Create a new DockPanel so that we can place the widget in it and a minimize button.
            MaximizedDockPanel tmp = new MaximizedDockPanel();
            tmp.SetWidget(widget);
            tmp.btnMinimize.Click += btnMinimize_Click;
            // Finally add the new DockPanel containing our widget
            // to the root so that it's maximized.
            RootGrid.Children.Add(tmp);
            // Create a nice fade in effect when widget is maximized 
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = 0;
            myDoubleAnimation.To = 1;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(.75));
            tmp.BeginAnimation(DockPanel.OpacityProperty, myDoubleAnimation);
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            /*Button minimizeButton = (Button)sender;
            Border border = (Border)minimizeButton.Parent;
            Grid tmp = (Grid)border.Parent;
            Border border1 = (Border)tmp.Children[2];
            UIElement widget = border1.Child;

            border1.Child = null;*/
            MaximizedDockPanel tmp = null;
            foreach (UIElement uie in RootGrid.Children)
            {
                if (uie is MaximizedDockPanel)
                {
                    tmp = uie as MaximizedDockPanel;
                    RootGrid.Children.Remove(tmp);
                    break;
                }
            }
            //RootGrid.Children.RemoveAt(1);
            UIElement widget = tmp.GetWidget();
            tmp.RemoveWidget();
            if (container != null)
            {
                container.Children.Add(widget);
                container = null;
            }

            // Create a nice fade out effect when widget is minimized
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = 0;
            myDoubleAnimation.To = 1;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(.75));
            RootGrid.BeginAnimation(Grid.OpacityProperty, myDoubleAnimation);
        }

        private void textBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            txt.Foreground = Brushes.Black;
        }

        private void textBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            txt.Foreground = Brushes.White;
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, Server> pair in this.serverMap)
            {
                pair.Value.Close();
            }
        }

        private void control_MouseEnter(object sender, MouseEventArgs e)
        {
            Control ctrl = (Control)sender;
            ctrl.Foreground = Brushes.Black;
        }

        private void control_MouseLeave(object sender, MouseEventArgs e)
        {
            Control ctrl = (Control)sender;
            ctrl.Foreground = Brushes.White;
        }

        private void ChartSwitch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Chart chart = (Chart)sender;
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

        private void image_MouseEnter(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            string imgPath = "/images/" + img.Name.Substring(3).ToLower() + "_hover.png";
            img.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), imgPath));
        }

        private void image_MouseLeave(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            string imgPath = "/images/" + img.Name.Substring(3).ToLower() + "_normal.png";
            img.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), imgPath));
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image img = (Image)sender;
            string imgPath = "/images/" + img.Name.Substring(3).ToLower() + "_pressed.png";
            img.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), imgPath));
        }

        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image img = (Image)sender;
            string imgPath = "/images/" + img.Name.Substring(3).ToLower() + "_hover.png";
            img.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), imgPath));
        }

        private void imgMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void imgExit_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Environment.Exit(0);
        }
		
		private void panel_MouseEnter(object sender, MouseEventArgs e)
        {
            Panel panel = (Panel)sender;
            InvertPanelColor(panel);
        }

        private void panel_MouseLeave(object sender, MouseEventArgs e)
        {
            Panel panel = (Panel)sender;
            InvertPanelColor(panel);
        }

        private void InvertPanelColor(Panel panel)
        {
            panel.Background = CommonFunction.GetInvertedBrush(panel.Background as SolidColorBrush);
            foreach (UIElement element in panel.Children)
            {
                Shape shape = element as Shape;
                if (shape != null)
                {
                    shape.Stroke = CommonFunction.GetInvertedBrush(shape.Stroke as SolidColorBrush);
                    shape.Fill = CommonFunction.GetInvertedBrush(shape.Fill as SolidColorBrush);
                }
                TextBlock text = element as TextBlock;
                if (text != null)
                {
                    text.Foreground = CommonFunction.GetInvertedBrush(text.Foreground as SolidColorBrush);
                    text.Background = CommonFunction.GetInvertedBrush(text.Background as SolidColorBrush);
                }
            }
        }

    }
}
