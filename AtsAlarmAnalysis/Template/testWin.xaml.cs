using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
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
    /// Interaction logic for testWin.xaml
    /// </summary>
    public partial class testWin : Window
    {
        DataTable dtResult = new DataTable();
        public testWin()
        {
            InitializeComponent();

            dataGrid1.Columns.Add(CreateTextBlockColumn("SEV", 40, "Severity"));
            dataGrid1.Columns.Add(CreateTextBlockColumn("LOC", 40, "Location"));
            dataGrid1.Columns.Add(CreateTextBlockColumn("DESCRIPTION", 300, "Description"));
            dataGrid1.Columns.Add(CreateTextBlockColumn("TOTAL\r\nNO", 40, "Count"));
            dataGrid1.ColumnHeaderHeight = 32d;
            dataGrid1.RowHeight = 50d;
            dataGrid1.FontSize = 10;

            dtResult.Columns.Add("Severity");
            dtResult.Columns.Add("Location");
            dtResult.Columns.Add("Description");
            dtResult.Columns.Add("Count");
            txt1.Margin = new Thickness(0);
        }

        private System.Windows.Controls.DataGridTemplateColumn CreateLabelColumn(string text, int width, string binding)
        {
            System.Windows.Controls.DataGridTemplateColumn column1 = new System.Windows.Controls.DataGridTemplateColumn();
            column1.Header = text;
            column1.Width = width;
            //column1.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            //column1.CanUserSort = true;
            System.Windows.DataTemplate dt1 = new System.Windows.DataTemplate();
            System.Windows.FrameworkElementFactory label = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Label));
            label.SetBinding(System.Windows.Controls.Label.ContentProperty, new System.Windows.Data.Binding(binding));
            label.SetValue(System.Windows.Controls.Label.VerticalContentAlignmentProperty, System.Windows.VerticalAlignment.Center);
            label.SetValue(System.Windows.Controls.Label.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            //label.SetValue(System.Windows.Controls.Label.BackgroundProperty, System.Windows.Media.Brushes.Black);             //must not set when use AlternatingRowBackground
            //label.SetValue(System.Windows.Controls.Label.ForegroundProperty, System.Windows.Media.Brushes.White);
            label.SetValue(System.Windows.Controls.Label.BorderThicknessProperty, new System.Windows.Thickness(0));
            //label.SetValue(System.Windows.Controls.Label.HeightProperty, 40d);            //can set at dataGrid.rowheight

            dt1.DataType = typeof(System.Windows.Controls.Label);
            dt1.VisualTree = label;
            column1.CellTemplate = dt1;

            System.Windows.Style style = new System.Windows.Style();
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
            //style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, System.Windows.FontWeights.Bold));
            //style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 20d));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, System.Windows.Media.Brushes.Silver));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));
            column1.HeaderStyle = style;

            return column1;
        }

        private System.Windows.Controls.DataGridTemplateColumn CreateTextBlockColumn(string text, int width, string binding)
        {
            System.Windows.Controls.DataGridTemplateColumn column1 = new System.Windows.Controls.DataGridTemplateColumn();
            column1.Header = text;
            column1.Width = width;
            //column1.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            //column1.CanUserSort = true;
            System.Windows.DataTemplate dt1 = new System.Windows.DataTemplate();
            System.Windows.FrameworkElementFactory txt = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            txt.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding(binding));
            txt.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            txt.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            txt.SetValue(System.Windows.Controls.TextBlock.TextWrappingProperty, System.Windows.TextWrapping.Wrap);
            //label.SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, System.Windows.Media.Brushes.Black);             //must not set when use AlternatingRowBackground
            //label.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, System.Windows.Media.Brushes.White);
            //txt.SetValue(System.Windows.Controls.TextBlock.BorderThicknessProperty, new System.Windows.Thickness(0));
            //label.SetValue(System.Windows.Controls.TextBlock.HeightProperty, 40d);            //can set at dataGrid.rowheight

            dt1.DataType = typeof(System.Windows.Controls.TextBlock);
            dt1.VisualTree = txt;
            column1.CellTemplate = dt1;

            System.Windows.Style style = new System.Windows.Style();
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
            //style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, System.Windows.FontWeights.Bold));
            //style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 20d));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, System.Windows.Media.Brushes.Silver));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));
            column1.HeaderStyle = style;

            return column1;
        }

        private System.Windows.Controls.DataGridTemplateColumn CreateImageColumn(string text, int width, string binding)
        {
            System.Windows.Controls.DataGridTemplateColumn column1 = new System.Windows.Controls.DataGridTemplateColumn();
            column1.Header = text;
            column1.Width = width;
            System.Windows.DataTemplate dt1 = new System.Windows.DataTemplate();
            System.Windows.FrameworkElementFactory image = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Image));
            image.SetBinding(System.Windows.Controls.Image.SourceProperty, new System.Windows.Data.Binding(binding));
            image.SetValue(System.Windows.Controls.Image.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            image.SetValue(System.Windows.Controls.Image.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            dt1.DataType = typeof(System.Windows.Controls.Image);
            dt1.VisualTree = image;
            column1.CellTemplate = dt1;

            System.Windows.Style style = new System.Windows.Style();
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, System.Windows.FontWeights.Bold));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 20d));
            column1.HeaderStyle = style;

            return column1;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            dataGrid1.Items.Clear();
            dtResult.Rows.Clear();
            for (int i = 1; i <= 10; i++)
            {
                DataRow row = dtResult.NewRow();
                row["Severity"] = i;
                row["Location"] = "DT0" + i.ToString("00");
                row["Description"] = "22kV SWITCHGEAR INTAKE INCOMING CUBICLE Power Factor Reading: 22kV SWITCH RM 2";
                row["Count"] = 10 + i;
                dtResult.Rows.Add(row);
                Console.WriteLine(dtResult.Rows.Count);
            }
            if (dtResult != null)
                FillDataGridWithDataTable(dataGrid1, dtResult);

            
        }

        private void FillDataGridWithDataTable(DataGrid dataGrid, DataTable dt)
        {
            /*dataGrid.Items.Clear();
            foreach (DataColumn column in dt.Columns)
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

        private void TextBlock_MouseEnter_1(object sender, MouseEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            SolidColorBrush brush = txt.Background as SolidColorBrush;
            Color color1 = new Color();
            color1.A = brush.Color.A;
            color1.R = Convert.ToByte(0xff - brush.Color.R);
            color1.G = Convert.ToByte(0xff - brush.Color.G);
            color1.B = Convert.ToByte(0xff - brush.Color.B);
            txt.Background = new SolidColorBrush(color1);
        }

        private void TextBlock_MouseLeave_1(object sender, MouseEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            SolidColorBrush brush = txt.Background as SolidColorBrush;
            Color color1 = new Color();
            color1.A = brush.Color.A;
            color1.R = Convert.ToByte(0xff - brush.Color.R);
            color1.G = Convert.ToByte(0xff - brush.Color.G);
            color1.B = Convert.ToByte(0xff - brush.Color.B);
            txt.Background = new SolidColorBrush(color1);
        }

    }
}
