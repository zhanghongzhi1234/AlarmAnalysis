using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace TemplateProject
{
    [ValueConversion(typeof(string), typeof(SolidColorBrush))]
    public class MyColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SolidColorBrush ret = null;
            try
            {
                string colorName = (string)value;
                ret = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
            }
            catch(Exception)    {}
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
