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


namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for AlarmFilterWindow.xaml
    /// </summary>
    public partial class AlarmFilterWindow : Window
    {
        public List<string> selectedSystems = new List<string>();
        public List<string> selectedSubsystems = new List<string>();
        public List<string> selectedLocations = new List<string>();
        public List<string> selectedSeveritys = new List<string>();
        public DateTime dtStart;
        public DateTime dtEnd;
        public bool isEnabled = true;

        public AlarmFilterWindow(AlarmFilter alarmFilter)
        {
            InitializeComponent();
            if (alarmFilter != null)
            {
                selectedSystems = alarmFilter.selectedSystems;
                selectedSubsystems = alarmFilter.selectedSubsystems;
                selectedLocations = alarmFilter.selectedLocations;
                selectedSeveritys = alarmFilter.selectedSeveritys;
                dtStart = alarmFilter.dtStart;
                dtEnd = alarmFilter.dtEnd;
            }
            else
            {
                selectedSystems.Add("All");
                selectedSubsystems.Add("All");
                selectedLocations.Add("All");
                selectedSeveritys.Add("All");
                dtStart = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
                dtEnd = DateTime.Now;
            }
            FillAllComboBox();
            dpDateStart.SelectedDate = dtStart.Date;
            dpDateEnd.SelectedDate = dtEnd.Date;
            txtTimeStart.Text = dtStart.Hour.ToString();
            txtTimeEnd.Text = dtEnd.Hour.ToString();
        }

        private void FillAllComboBox()
        {
            {   //System
                Dictionary<string, object> allItems = new Dictionary<string, object>();
                foreach (SystemInfo iterator in AtsCachedMap.Instance.systemList)
                {
                    allItems.Add(iterator.ShortName, iterator.ShortName);
                }
                Dictionary<string, object> selectedItems = new Dictionary<string, object>();
                foreach (string iterator in selectedSystems)
                {
                    selectedItems.Add(iterator, iterator);
                }
                cmbSystem.ItemsSource = allItems;
                cmbSystem.SelectedItems = selectedItems;
            }
            {   //Subsystem
                Dictionary<string, object> allItems = new Dictionary<string, object>();
                foreach (Subsystem iterator in AtsCachedMap.Instance.allSubsystemList)
                {
                    allItems.Add(iterator.Name, iterator.Name);
                }
                Dictionary<string, object> selectedItems = new Dictionary<string, object>();
                foreach (string iterator in selectedSubsystems)
                {
                    selectedItems.Add(iterator, iterator);
                }
                cmbSubsystem.ItemsSource = allItems;
                cmbSubsystem.SelectedItems = selectedItems;
            }
            {   //Location
                Dictionary<string, object> allItems = new Dictionary<string, object>();
                for (int i = 1; i <= 35; i++)
                {
                    string locationName = "DT" + i.ToString("00");
                    allItems.Add(locationName, locationName);
                }
                Dictionary<string, object> selectedItems = new Dictionary<string, object>();
                foreach (string iterator in selectedLocations)
                {
                    selectedItems.Add(iterator, iterator);
                }
                cmbLocation.ItemsSource = allItems;
                cmbLocation.SelectedItems = selectedItems;
            }
            {   //Severity
                Dictionary<string, object> allItems = new Dictionary<string, object>();
                foreach (Severity iterator in AtsCachedMap.Instance.severityList)
                {
                    allItems.Add(iterator.ID, iterator.ID);
                }
                Dictionary<string, object> selectedItems = new Dictionary<string, object>();
                foreach (string iterator in selectedSeveritys)
                {
                    selectedItems.Add(iterator, iterator);
                }
                cmbSeverity.ItemsSource = allItems;
                cmbSeverity.SelectedItems = selectedItems;
            }
        }

        /*private void Date_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DateSelect dateSelect = new DateSelect(selectedDate);
            System.Windows.Point pointToWindow = Mouse.GetPosition(this);
            System.Windows.Point point = PointToScreen(pointToWindow);
            dateSelect.Left = point.X - 5;
            dateSelect.Top = point.Y - 5;
            if (dateSelect.ShowDialog() == true)
            {
                selectedDate = dateSelect.date;
                //day_start.Text = startDate.Day.ToString();
                //Selected_Date.Text = selectedDate.ToString("yyyy-mm-dd", CultureInfo.InvariantCulture);
            }
        }*/

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            int startTime, endTime;
            bool isNumeric = int.TryParse(txtTimeStart.Text, out startTime);
            if (isNumeric == false)
            {
                MessageBox.Show("Start time must be a integer between 0 to 24!");
                txtTimeStart.Focus();
                return;
            }
            if(startTime < 0 || startTime > 24)
            {
                MessageBox.Show("Start time must be a integer between 0 to 24!");
                txtTimeStart.Focus();
                return;
            }
            isNumeric = int.TryParse(txtTimeEnd.Text, out endTime);
            if (isNumeric == false)
            {
                MessageBox.Show("End time must be a integer between 0 to 24!");
                txtTimeEnd.Focus();
                return;
            }
            if (endTime < 0 || endTime > 24)
            {
                MessageBox.Show("End time must be a integer between 0 to 24!");
                txtTimeEnd.Focus();
                return;
            }
            if (startTime >= endTime)
            {
                MessageBox.Show("Start time must be less than end time!");
                txtTimeStart.Focus();
                return;
            }

            selectedSystems = cmbSystem.SelectedItems.Keys.ToList();
            selectedSubsystems = cmbSubsystem.SelectedItems.Keys.ToList();
            selectedLocations = cmbLocation.SelectedItems.Keys.ToList();
            selectedSeveritys = cmbSeverity.SelectedItems.Keys.ToList();
            dtStart = dpDateStart.SelectedDate.Value + new TimeSpan(startTime, 0, 0);
            dtEnd = dpDateEnd.SelectedDate.Value + new TimeSpan(endTime, 0, 0);
            this.DialogResult = true;
            this.Close();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            isEnabled = false;
            Dictionary<string, object> selectedItems = new Dictionary<string, object>();
            selectedItems.Add("All", "All");
            cmbSystem.SelectedItems = selectedItems;
            cmbSubsystem.SelectedItems = selectedItems;
            cmbLocation.SelectedItems = selectedItems;
            cmbSeverity.SelectedItems = selectedItems;
            dpDateStart.SelectedDate = DateTime.Now.Date - new TimeSpan(1, 0, 0, 0);
            dpDateEnd.SelectedDate = DateTime.Now;
            txtTimeStart.Text = "0";
            txtTimeEnd.Text = "24";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
