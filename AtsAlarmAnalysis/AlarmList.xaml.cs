using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyList
{
    public class Alarm
    {
        public enum ESeverity { MINOR, MAJOR, CRITICAL };
        public DateTime TimeStamp { get; set; }
        public string TimeString { get; set; }
        public string Description { get; set; }
        public ESeverity Severity { get; set; }
        public string Equipment { get; set; }
        public Alarm(DateTime timeStamp, string description, ESeverity severity, string eqpt)
        {
            TimeStamp = timeStamp;
            TimeString = TimeStamp.ToString("dd MMM yyyy HH:mm:ss");
            Description = description;
            Severity = severity;
            Equipment = eqpt;
        }
    }
    /// <summary>
    /// Interaction logic for AlarmList.xaml
    /// </summary>
    public partial class AlarmList : UserControl
    {
        //header property
        public double headerFontSize = 8d;
        public double headerHeight = 30d;
        //item property
        public double contentFontSize = 8d;
        public double contentHeight = 23d;

        public string itemGridColor = "#FF000000";

        //use variable to control the style better, modify at here will change all AlarmList style
        public AlarmList()
        {
            InitializeComponent();
            listView.SelectionMode = SelectionMode.Single;
            //ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Visible);       //this mean top10AlarmList, it have no use
            //ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Hidden);
            //ScrollViewer.SetVerticalScrollBarVisibility(listView, ScrollBarVisibility.Auto);
            //ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Hidden);
            //gridView.Columns[0].Header = "hongzhi";
            RemoveAllColumns();     //default column is only for sample
        }

        //default VerticalScrollBarVisibility is Auto, set in xaml
        public void SetVerticalScrollBarVisibility(ScrollBarVisibility visibility)
        {
            ScrollViewer.SetVerticalScrollBarVisibility(listView, visibility);
        }

        //default HorizontalSScrollBarVisibility is Hidden, set in xaml
        public void SetHorizontalScrollBarVisibility(ScrollBarVisibility visibility)
        {
            ScrollViewer.SetHorizontalScrollBarVisibility(listView, visibility);
        }

        public void SetHeaderHeight(double height)
        {
            this.headerHeight = height;
        }

        public void SetHeaderStyle(Style style)
        {
            //Modify header style, ItemContainerStyle is sealed, cannot modify, must replay all
            gridView.ColumnHeaderContainerStyle = style;
        }

        public Style GetItemContentStyle()
        {
            return listView.ItemContainerStyle;
        }

        public void SetItemContainerStyle(Style style)
        {
            listView.ItemContainerStyle = style;
        }

        public void RemoveAllItemContainerStyleSetters()
        {
            listView.ItemContainerStyle.Setters.Clear();
        }

        public void AddSetterToItemContainerStyle(Setter setter)
        {
            listView.ItemContainerStyle.Setters.Add(setter);
        }

        public void RemoveAllItemContainerStyleTriggers()
        {
            listView.ItemContainerStyle.Triggers.Clear();
        }

        public void AddTriggerToItemContainerStyle(Trigger trigger)
        {
            listView.ItemContainerStyle.Triggers.Add(trigger);
        }

        public void AddTriggerToItemContainerStyle(DataTrigger trigger)
        {
            listView.ItemContainerStyle.Triggers.Add(trigger);
        }

        public void SetAllTriggersForItemContainerStyle(List<DataTrigger> triggers)
        {
            listView.ItemContainerStyle.Triggers.Clear();
            //listView.ItemContainerStyle.Triggers.Concat(triggers);    //it will fail because Concat won't change List<DataTrigger> to List<Trigger>
            foreach (DataTrigger trigger in triggers)
            {
                listView.ItemContainerStyle.Triggers.Add(trigger);
            }
        }

        public void SetAllTriggersForItemContainerStyle(List<Trigger> triggers)
        {
            listView.ItemContainerStyle.Triggers.Clear();
            //listView.ItemContainerStyle.Triggers.Concat(triggers);    //it will fail because Concat won't change List<DataTrigger> to List<Trigger>
            foreach (Trigger trigger in triggers)
            {
                listView.ItemContainerStyle.Triggers.Add(trigger);
            }
        }

        public void SetAllFontSize(double fontSize)
        {
            this.headerFontSize = fontSize;
            this.contentFontSize = fontSize;
        }

        public void RemoveAllColumns()
        {
            gridView.Columns.Clear();
        }

        public void AddColumn(string text, int width, string bindingPath, HorizontalAlignment horizontalAlighment = HorizontalAlignment.Center)
        {
            GridViewColumn column = new GridViewColumn();
            column.Header = text;
            column.Width = width;
            /*Border border = new Border();
            border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            border.BorderThickness = new Thickness(1, 1, 0, 0);
            border.Margin = new Thickness(-6, -2, -6, -2);
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(6, 2, 6, 2);
            TextBlock textblock = new TextBlock();
            textblock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            textblock.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            textblock.TextAlignment = TextAlignment.Center;
            textblock.TextWrapping = TextWrapping.Wrap;
            textblock.SetBinding(TextBlock.TextProperty, bindingPath);
            panel.Children.Add(textblock);
            border.Child = panel;*/

            DataTemplate template = new DataTemplate();
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(itemGridColor)));
            borderFactory.SetValue(Border.HeightProperty, contentHeight);       //Generally set at ItemContainerStyle, not set at here
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1, 1, 0, 0));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(-6, -2, -6, -2));
            
            FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));
            gridFactory.SetValue(StackPanel.MarginProperty, new Thickness(6, 2, 6, 2));

            FrameworkElementFactory txtFactory = new FrameworkElementFactory(typeof(TextBlock));
            txtFactory.SetValue(TextBlock.HorizontalAlignmentProperty, horizontalAlighment);
            txtFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            txtFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            txtFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            //txtFactory.SetValue(TextBlock.FontSizeProperty, contentFontSize);
            txtFactory.SetBinding(TextBlock.TextProperty, new Binding(bindingPath));

            //template.VisualTree = txtFactory;
            template.VisualTree = borderFactory;
            borderFactory.AppendChild(gridFactory);       //so complex just for the grid
            gridFactory.AppendChild(txtFactory);

            column.CellTemplate = template;
            column.DisplayMemberBinding = new Binding(bindingPath);
            gridView.Columns.Add(column);
        }

        public void AddColumn(GridViewColumn column)
        {
            gridView.Columns.Add(column);
        }

        public void RemoveColumnAt(int index)
        {
            gridView.Columns.RemoveAt(index);
        }

        private void ListViewItem_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item == null)
                return;

            var dataItem = item.DataContext as Alarm;
            if (dataItem == null)
                return;

            //MessageBox.Show(dataItem.Description);
        }

        private void EditIVD_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveIVD_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuItem_Response_Click(object sender, RoutedEventArgs e)
        {
            ExpandoObject selectedItem = listView.SelectedItem as ExpandoObject;
            var dictionary = (IDictionary<string, object>)selectedItem;
            if (selectedItem != null)
            {
                string subsystemName = dictionary["Subsystem"].ToString();
                if (string.IsNullOrEmpty(subsystemName))
                {
                    MessageBox.Show("Subsystem name is empty!", "Alarm Analysis");
                    return;
                }
                string systemID = dictionary["systemID"].ToString();
                if (string.IsNullOrEmpty(systemID))
                {
                    MessageBox.Show("systemID is empty!", "Alarm Analysis");
                    return;
                }
                TemplateProject.Subsystem subsystem = TemplateProject.AtsCachedMap.Instance.GetSubsystemByName(subsystemName, systemID);
                if (subsystem == null)
                {
                    MessageBox.Show("Subsystem " + subsystemName + " is not configed!", "Alarm Analysis");
                    return;
                }
                if (String.IsNullOrEmpty(subsystem.ResponseFile))
                {
                    MessageBox.Show("Alarm response sheet of subsystem " + subsystemName + " is not configed!", "Alarm Analysis");
                    return;
                }
                string dir = TemplateProject.AtsCachedMap.Instance.AlarmResponseSheetPath;
                string fileName = System.IO.Path.Combine(dir, subsystem.ResponseFile);
                if (System.IO.File.Exists(fileName))
                {
                    Process wordProcess = new Process();
                    wordProcess.StartInfo.FileName = fileName;
                    wordProcess.StartInfo.UseShellExecute = true;
                    wordProcess.Start();
                }
                else
                {
                    MessageBox.Show("Response file \"" + fileName + "\" does not exist!", "Alarm Analysis");
                }
            }
        }

        //private void menuItem_CopyPassword_Click(object sender, RoutedEventArgs e)
        //{
        //    Process wordProcess = new Process();
        //    string fileName = TemplateProject.AtsCachedMap.Instance.AlarmResponseSheetPath
        //    wordProcess.StartInfo.FileName = fileName;
        //    wordProcess.StartInfo.UseShellExecute = true;
        //    wordProcess.Start();
        //}

        public void EnableContextMenu(bool bEnable = true)
        {
            if (bEnable == false)
            {
                Style style = this.GetItemContentStyle();
                for (int i = 0; i < style.Setters.Count; i++)
                {
                    Setter setter = style.Setters[i] as Setter;
                    if (setter.Property.Name == "ContextMenu")
                    {
                        style.Setters.RemoveAt(i);
                        break;
                    }
                }
            }
        }

    }
}

