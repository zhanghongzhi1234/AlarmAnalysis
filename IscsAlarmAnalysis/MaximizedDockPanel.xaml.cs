using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
using Visifire.Charts;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for MaximizedDockPanel.xaml
    /// </summary>
    public partial class MaximizedDockPanel : UserControl
    {
        private ScaleTransform SchemaScale;
        double width;
        double height;
        public List<List<Legend>> oldLegends = new List<List<Legend>>();            //store old legend enable or size
        public List<List<DataPoint>> oldDataPoints = new List<List<DataPoint>>();   //store all datapoint LabelEnabled, because Series LabelEnabled have no use
        public MaximizedDockPanel()
        {
            InitializeComponent();
            this.SchemaScale = new ScaleTransform();
        }

        public void SetWidget(UIElement uie)
        {
            container.Children.Clear();
            container.Children.Add(uie);
            FrameworkElement fe = uie as FrameworkElement;
            if (fe != null && fe.Tag != null)
                txtTitle.Text = fe.Tag.ToString();
            //enable all chart legend here, store the old legend
            foreach (Chart chart in FindVisualChildren<Chart>(container))
            {
                List<DataPoint> dataPoints = new List<DataPoint>();
                foreach (DataPoint dataPoint in chart.Series[0].DataPoints)
                {
                    DataPoint oldDataPoint = new DataPoint();    //store all property, must use new, if assign directly, just a reference
                    oldDataPoint.LabelEnabled = dataPoint.LabelEnabled;
                    dataPoints.Add(oldDataPoint);
                    dataPoint.LabelEnabled = true;
                }
                oldDataPoints.Add(dataPoints);
                List<Legend> legends = new List<Legend>();
                foreach (Legend legend in chart.Legends)
                {
                    Legend oldLegend = new Legend();    //store all property, must use new, if assign directly, just a reference
                    PropertyInfo[] properties = typeof(Legend).GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if(property.CanWrite && property.CanRead)
                            property.SetValue(oldLegend, property.GetValue(legend));
                    }
                    legends.Add(oldLegend);
                    legend.FontSize = 9d;
                    legend.Enabled = true;
                }
                oldLegends.Add(legends);
            }
        }

        public UIElement GetWidget()
        {
            UIElement ret = null;
            if (container.Children.Count > 0)
            {
                ret = container.Children[0];
            }
            return ret;
        }

        public void RemoveWidget()
        {
            int i = 0;      //restore legend first
            foreach (Chart chart in FindVisualChildren<Chart>(container))
            {
                List<DataPoint> dataPoints = oldDataPoints[i];
                for (int j = 0; j < chart.Series[0].DataPoints.Count(); j++)
                {
                    if (j < dataPoints.Count())
                    {
                        chart.Series[0].DataPoints[j].LabelEnabled = dataPoints[j].LabelEnabled;
                    }
                }
                dataPoints.Clear();
                List<Legend> legends = oldLegends[i];
                for (int j = 0; j < chart.Legends.Count(); j++)
                {
                    PropertyInfo[] properties = typeof(Legend).GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.CanWrite && property.CanRead)
                            property.SetValue(chart.Legends[j], property.GetValue(legends[j]));
                    }
                    //chart.Legends[j].Enabled = false;
                }
                legends.Clear();
                i++;
            }
            oldLegends.Clear();
            oldDataPoints.Clear();
            container.Children.Clear();
        }

        private void TrackBarEdit_EditValueChanged_1(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            OnZoomTackValueChanged(sender, e);      
        }

        private void OnZoomTackValueChanged(object sender, EventArgs e)
        {
            //DevExpress.Xpf.Bars.BarEditItem control = (DevExpress.Xpf.Bars.BarEditItem)sender;
            DevExpress.Xpf.Editors.TrackBarEdit control = (DevExpress.Xpf.Editors.TrackBarEdit)sender;
            double scale = Convert.ToDouble(control.EditValue) / 100.0;
            //scale = 5;
            //DevExpress.XtraEditors.ZoomTrackBarControl control = (DevExpress.XtraEditors.ZoomTrackBarControl)sender;
            //double scale = control.Value / 100.0;
            this.SchemaScale.ScaleX = scale;
            this.SchemaScale.ScaleY = scale;
            //temp.LayoutTransform = this.SchemaScale;
            //chartBorder.LayoutTransform = this.SchemaScale;
            if (container.Children.Count > 0)
            {
                //Visifire.Charts.Chart chart = (Visifire.Charts.Chart)container.Children[0];
                //if (chart != null)
                FrameworkElement element = (FrameworkElement)container.Children[0];
                if(element != null)
                {
                    //chart.LayoutTransform = this.SchemaScale;
                    container.Width = width * this.SchemaScale.ScaleX;
                    container.Height = height * this.SchemaScale.ScaleY;
                }
            }
            txtZoom.Text = control.EditValue.ToString() + "%";
            //wpfSchemaContainer
        }

        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (container.Children.Count > 0)
            {
                //Visifire.Charts.Chart chart = (Visifire.Charts.Chart)container.Children[0];
                FrameworkElement element = (FrameworkElement)container.Children[0];
                //if (chart != null)
                if(element != null)
                {
                    width = container.ActualWidth;
                    height = container.ActualHeight;
                }
            }
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
    }
}
