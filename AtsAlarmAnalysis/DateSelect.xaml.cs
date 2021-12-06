using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for DateSelect.xaml
    /// </summary>
    public partial class DateSelect : Window
    {
        public DateTime date;

        public DateSelect(DateTime date)
        {
            InitializeComponent();
            this.date = date;
            cal.SelectedDate = this.date;
            cal.DisplayDate = this.date;
            cal.SelectedDatesChanged += Calendar_SelectedDatesChanged_1;
        }

        private void Calendar_SelectedDatesChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // ... Get reference.
            var calendar = sender as Calendar;

            // ... See if a date is selected.
            if (calendar.SelectedDate.HasValue)
            {
                // ... Display SelectedDate in Title.
                date = calendar.SelectedDate.Value;
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
