using System;
using System.Collections.Generic;
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
    /// Interaction logic for testWin2.xaml
    /// </summary>
    public partial class testWin2 : Window
    {
        List<Severity> severityList = IscsCachedMap.Instance.severityList;
        ListCollectionView lcv;
        string marginleft = " ";

        public testWin2()
        {
            InitializeComponent();
            InitAlarmList(dataGrid1);

            List<dynamic> items = new List<dynamic>();
            //CreateDynamicItem(string Severity1, string Description, string Time, string ForeColor = null, string BackColor = null)
            for(int i = 1; i <= 15; i++)
            {
                items.Add(CreateDynamicItem((i % 5 + 1).ToString(), marginleft + "Description" + i, (DateTime.Now - new TimeSpan(0, 0, i)).ToString(), "White", "Black"));
            }

            lcv = new ListCollectionView(items);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("SEV"));

            this.dataGrid1.ItemsSource = lcv;
            dataGrid1.IsReadOnly = true;
        }

        //available property: HorizontalContentAlignmentProperty,HeightProperty,FontSizeProperty,BackgroundProperty,ForegroundProperty,BorderThicknessProperty,ToolTipProperty
        public void InitAlarmList(DataGrid dataGrid)
        {
            dataGrid.Columns.Add(CreateTextColumn("SEV", 40, "Severity", TextAlignment.Center));
            dataGrid.Columns.Add(CreateTextColumn("DESCRIPTION", 200, "Description", TextAlignment.Left));
            dataGrid.Columns.Add(CreateTextColumn("DATE/TIME", 140, "DateTime", TextAlignment.Center));
            dataGrid.ColumnHeaderHeight = 32d;

            Style headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            headerStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0c6fc0"))));
            headerStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            headerStyle.Setters.Add(new Setter(BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));         //default column header have no split, must add
            headerStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.Bold));
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                dataGrid.Columns[i].HeaderStyle = headerStyle;        //also can set individually in function CreateTextColumn(), but usually header style is same, so set here better
            }

            dataGrid.RowHeight = 30d;
            dataGrid.FontSize = 14;
            dataGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            dataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            dataGrid.RowHeaderWidth = 0;
        }

        private System.Windows.Controls.DataGridTextColumn CreateTextColumn(string headerText, int columnWidth, string textBinding, TextAlignment txtAlign = TextAlignment.Center, bool canUserSort = true, string backgroundBinding = "Background")
        {
            DataGridTextColumn columnNew = new DataGridTextColumn();
            columnNew.Header = headerText;
            columnNew.Width = columnWidth;

            columnNew.Binding = new Binding(textBinding);
            Style style = new Style();
            style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Stretch));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, txtAlign));
            if(!String.IsNullOrEmpty(backgroundBinding))
                style.Setters.Add(new Setter(BackgroundProperty, new Binding(backgroundBinding)));

            columnNew.CellStyle = style;
            columnNew.CanUserSort = canUserSort;           //default CanUserSort for TextColumn is true

            return columnNew;
        }

        private dynamic CreateDynamicItem(string Severity1, string Description, string Time, string ForeColor = null, string BackColor = null)
        {
            dynamic item = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)item;
            dictionary["Severity"] = Severity1;
            dictionary["Description"] = Description;
            dictionary["DateTime"] = Time;
            Severity severity = severityList.Where(p => p.ID == Severity1).FirstOrDefault();
            if (severity != null)
            {
                SolidColorBrush brush1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(severity.Color));
                dictionary["Background"] = brush1;
                //dictionary["Background"] = severity.Color;
            }
            if (ForeColor != null)
                dictionary["ForeColor"] = ForeColor;
            if (BackColor != null)
                dictionary["BackColor"] = BackColor;

            return item;
        }

        int newIndex = 1;
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            lcv.AddNewItem(CreateDynamicItem(rnd.Next(1, 6).ToString(), marginleft + "new Item" + (newIndex++), DateTime.Now.ToString(), "White", "Black"));
            lcv.CommitNew();
        }
    }
}
