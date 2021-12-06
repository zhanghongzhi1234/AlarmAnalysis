using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;
using System.Dynamic;
using System.Windows.Controls.Primitives;


namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for AlarmFilterWindow.xaml
    /// </summary>
    public partial class AlarmFilterWindow1 : Window
    {
        public List<string> selectedSystems = new List<string>();
        public List<string> selectedSubsystems = new List<string>();
        public List<string> selectedLocations = new List<string>();
        public List<string> selectedSeveritys = new List<string>();
        public List<string> selectedRules = new List<string>();
        public DateTime dtStart;
        public DateTime dtEnd;
        CachedMap cachedMap;
        int IsAts = 0;
        IFilterInterface mainWindow = null;
        public bool enableDateTime = false;
        public bool isEnabled = false;

        public AlarmFilterWindow1(AlarmFilter alarmFilter, CachedMap cachedMap, bool enableDateTime = false, int IsAts = 1)
        {
            DebugUtil.Instance.LOG.Info("Function Enter: AlarmFilterWindow1");
            InitializeComponent();
            this.cachedMap = cachedMap;

            selectedSystems = alarmFilter.selectedSystems;
            selectedSubsystems = alarmFilter.selectedSubsystems;
            selectedLocations = alarmFilter.selectedLocations;
            selectedSeveritys = alarmFilter.selectedSeveritys;
            selectedRules = alarmFilter.selectedRules;
            dtStart = alarmFilter.dtStart;
            dtEnd = alarmFilter.dtEnd;
            this.isEnabled = alarmFilter.isEnabled;

            this.Loaded += AlarmFilterWindow1_Loaded;
            dpDateStart.SelectedDate = dtStart.Date;
            dpDateEnd.SelectedDate = dtEnd.Date;
            txtTimeStart.Text = dtStart.Hour.ToString();
            txtTimeEnd.Text = dtEnd.Hour.ToString();
            this.enableDateTime = enableDateTime;
            this.IsAts = alarmFilter.IsAtsAlarm;
            chkEnable.IsChecked = this.isEnabled;

            if (cachedMap.PopupFilter == false)
            {
                this.WindowStyle = System.Windows.WindowStyle.None;
                this.WindowState = System.Windows.WindowState.Maximized;
            }

            DebugUtil.Instance.LOG.Info("Function Exit: AlarmFilterWindow1");
        }

        public void SetMainWindow(IFilterInterface mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        void AlarmFilterWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            DebugUtil.Instance.LOG.Info("Function Enter: AlarmFilterWindow1_Loaded");
            InitAll();
            DebugUtil.Instance.LOG.Info("Function Exit: AlarmFilterWindow1_Loaded");
        }

        private CheckBox CreateCheckBox(string text, bool IsChecked = false)
        {
            DebugUtil.Instance.LOG.Info("Function Enter: CreateCheckBox");
            CheckBox chkBox = new CheckBox();
            chkBox.Content = text;
            chkBox.ToolTip = text;
            chkBox.Foreground = Brushes.White;
            chkBox.FontSize = 14;
            chkBox.Margin = new Thickness(5);
            chkBox.Width = 110;
            chkBox.IsChecked = IsChecked;
            DebugUtil.Instance.LOG.Info("Function Exit: CreateCheckBox");
            return chkBox;
        }

        private void CreateCheckBoxGroup(List<string> nameList, Panel panel, RowDefinition row, List<string> selectedNameList)
        {
            DebugUtil.Instance.LOG.Info("Function Enter: CreateCheckBoxGroup");
            if (nameList != null && nameList.Count > 0)
            {   //System
                CheckBox chkBoxAll = CreateCheckBox("All");
                chkBoxAll.Checked += chkBoxAll_CheckChanged;
                chkBoxAll.Unchecked += chkBoxAll_CheckChanged;
                panel.Children.Add(chkBoxAll);
                foreach (string name in nameList)
                {
                    bool IsChecked = selectedNameList.Contains(name);
                    panel.Children.Add(CreateCheckBox(name, IsChecked));
                }
                double newHeight = CalActualPanelHeight(panel) + 2 + 10;            //2 is top and bottom border thickness, 10 is top and bottom margin
                double delta = newHeight - row.Height.Value;
                row.Height = new GridLength(newHeight);
                this.Height += delta;
                if (selectedNameList.Contains("All"))
                {
                    chkBoxAll.IsChecked = true;
                    //chkBoxAll_Click(chkBoxAll, null);
                }
            }
            DebugUtil.Instance.LOG.Info("Function Exit: CreateCheckBoxGroup");
        }

        void chkBoxAll_CheckChanged(object sender, RoutedEventArgs e)
        {
            CheckBox chkBoxAll = sender as CheckBox;
            Panel panel = chkBoxAll.Parent as Panel;
            foreach (UIElement element in panel.Children)
            {
                CheckBox chkBox = element as CheckBox;
                if (chkBox != null && chkBox != chkBoxAll)
                {
                    chkBox.IsChecked = chkBoxAll.IsChecked;
                }
            }
        }

        private void InitAll()
        {
            DebugUtil.Instance.LOG.Info("Function Enter: InitAll");
            panel1.Children.Clear();
            panel2.Children.Clear();
            panel3.Children.Clear();
            panel4.Children.Clear();

            CreateCheckBoxGroup(cachedMap.systemList.Select(p => p.ShortName).ToList(), panel1, row1, selectedSystems);
            CreateCheckBoxGroup(cachedMap.allSubsystemList.Select(p => p.Name).ToList(), panel2, row2, selectedSubsystems);
            CreateCheckBoxGroup(cachedMap.locationList.Where(p => p.Operational != "0").Select(p => p.Name).ToList(), panel3, row3, selectedLocations);
            CreateCheckBoxGroup(cachedMap.severityList.Select(p => p.ID).ToList(), panel4, row4, selectedSeveritys);

            if (enableDateTime == false)
            {
                GridLength len0 = new GridLength(0);
                rowDate.Height = len0;
                RowTime.Height = len0;
                this.Height -= 40;
            }

            InitPanelRule();
            MoveWindowToCenter();
            DebugUtil.Instance.LOG.Info("Function Exit: InitAll");
        }

        private void MoveWindowToCenter()
        {
            this.Left = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2;
            this.Top = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2;
        }

        private void InitPanelRule()
        {
            DebugUtil.Instance.LOG.Info("Function Enter: InitPanelRule");
            foreach (UIElement el in panelRule.Children)
            {
                ToggleButton tglButton = el as CheckBox;
                if (tglButton == null)
                    tglButton = el as RadioButton;
                if (tglButton != null && selectedRules.Contains(tglButton.Tag.ToString()))
                {
                    tglButton.IsChecked = true;
                }
            }
            DebugUtil.Instance.LOG.Info("Function Exit: InitPanelRule");
        }

        private double CalActualPanelHeight(Panel panel)
        {
            DebugUtil.Instance.LOG.Info("Function Enter: CalActualPanelHeight");
            double totalWidth = 0d;
            double totalHeight = 0d;
            foreach (UIElement el in panel.Children)
            {
                CheckBox checkBox = el as CheckBox;
                if (checkBox != null)
                {
                    totalWidth += checkBox.Width + checkBox.Margin.Left + checkBox.Margin.Right;
                }
            }
            //double width = grid.ColumnDefinitions[1].Width.Value + grid.ColumnDefinitions[2].Width.Value + grid.ColumnDefinitions[3].Width.Value;
            //int ratio = (int)Math.Ceiling(totalWidth / panel.ActualWidth);
            double ratio = totalWidth / panel.ActualWidth + 0.7;
            totalHeight = panel.ActualHeight * ratio;
            DebugUtil.Instance.LOG.Info("Function Exit: CalActualPanelHeight");
            return totalHeight;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DebugUtil.Instance.LOG.Info("Function Enter: btnOK_Click");
            int startTime, endTime;
            bool isNumeric = int.TryParse(txtTimeStart.Text, out startTime);
            if (isNumeric == false)
            {
                MessageBox.Show("Start time must be a integer between 0 to 23!");
                txtTimeStart.Focus();
                return;
            }
            if (startTime < 0 || startTime > 23)
            {
                MessageBox.Show("Start time must be a integer between 0 to 23!");
                txtTimeStart.Focus();
                return;
            }
            isNumeric = int.TryParse(txtTimeEnd.Text, out endTime);
            if (isNumeric == false)
            {
                MessageBox.Show("End time must be a integer between 0 to 23!");
                txtTimeEnd.Focus();
                return;
            }
            if (endTime < 0 || endTime > 23)
            {
                MessageBox.Show("End time must be a integer between 0 to 23!");
                txtTimeEnd.Focus();
                return;
            }
            dtStart = dpDateStart.SelectedDate.Value + new TimeSpan(startTime, 0, 0);
            dtEnd = dpDateEnd.SelectedDate.Value + new TimeSpan(endTime, 0, 0);
            if (dtStart >= dtEnd)
            {
                MessageBox.Show("Start time must be less than end time!");
                txtTimeStart.Focus();
                return;
            }

            selectedSystems = GetSelectedItems(panel1);
            selectedSubsystems = GetSelectedItems(panel2);
            selectedLocations = GetSelectedItems(panel3);
            selectedSeveritys = GetSelectedItems(panel4);
            selectedRules = GetSelectedItemTags(panelRule);
            //isEnabled = chkEnable.IsChecked.Value;
            isEnabled = true;       //click OK mean enable
            if (cachedMap.PopupFilter == true)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                if(mainWindow != null)
                    mainWindow.FilterCallback(true);
                this.Hide();
            }
            DebugUtil.Instance.LOG.Info("Function Exit: btnOK_Click");
        }

        private List<string> GetSelectedItems(Panel panel)
        {
            List<string> ret = new List<string>();
            foreach (UIElement el in panel.Children)
            {
                CheckBox checkBox = el as CheckBox;
                if (checkBox != null && checkBox.Content.ToString() != "All" && checkBox.IsChecked == true)
                {
                    ret.Add(checkBox.Content.ToString());
                }
            }

            return ret;
        }

        private List<string> GetSelectedItemTags(Panel panel)
        {
            List<string> ret = new List<string>();
            foreach (UIElement el in panel.Children)
            {
                ToggleButton tglButton = el as CheckBox;
                if(tglButton == null)
                    tglButton = el as RadioButton;
                if (tglButton != null && tglButton.Content.ToString() != "All" && tglButton.IsChecked == true)
                {
                    ret.Add(tglButton.Tag.ToString());
                }
            }

            return ret;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            CheckAllCheckBox(panel1, true);
            CheckAllCheckBox(panel2, true);
            CheckAllCheckBox(panel3, true);
            CheckAllCheckBox(panel4, true);
            dpDateStart.SelectedDate = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            dpDateEnd.SelectedDate = DateTime.Now;
            
            txtTimeStart.Text = "0";
            txtTimeEnd.Text = (DateTime.Now.Hour + 1).ToString();
        }

        private void CheckAllCheckBox(Panel panel, bool IsChecked)
        {
            foreach (UIElement el in panel.Children)
            {
                CheckBox checkBox = el as CheckBox;
                if (checkBox != null)
                {
                    checkBox.IsChecked = IsChecked;
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DebugUtil.Instance.LOG.Info("Function Exit: btnCancel_Click");
            if (cachedMap.PopupFilter == true)
            {
                this.DialogResult = false;
                this.Close();
            }
            else
            {
                if (mainWindow != null)
                    mainWindow.FilterCallback(false);
                this.Hide();
            }
            DebugUtil.Instance.LOG.Info("Function Exit: btnCancel_Click");
        }
    }
}
