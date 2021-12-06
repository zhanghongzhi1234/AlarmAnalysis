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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for NavWindow.xaml
    /// </summary>
    public partial class NavWindow : Window
    {
        public Dictionary<string, string> dicTemplate = new Dictionary<string, string>();
        Window currentWindow;
        public NavWindow(Window currentWindow)
        {
            InitializeComponent();
            this.currentWindow = currentWindow;
            dicTemplate["全线电力监控图"] = "TemplateProject.Overview";
            dicTemplate["进线实时电能趋势图"] = "TemplateProject.GZL6_page18_jinxian_realtime";
            dicTemplate["设备实时电能趋势图"] = "TemplateProject.GZL6_page19_shebei_realtime";
            dicTemplate["电能质量数据查询"] = "TemplateProject.GZL6_page23_query_power";
            dicTemplate["能耗数据查询"] = "TemplateProject.GZL6_page25_query_energy";
            dicTemplate["全线电能汇总"] = "TemplateProject.GZL6_page27_fullline_power";
            dicTemplate["日分类能耗统计"] = "TemplateProject.GZL6_page28_day_power";
            dicTemplate["月分类能耗统计"] = "TemplateProject.GZL6_page29_month_power";
            dicTemplate["设备能耗趋势图"] = "TemplateProject.GZL6_page30_trend_power";
            
            this.Left = -this.Width;
            int i = 0;
            foreach (KeyValuePair<string,string> pair in dicTemplate)
            {
                ListViewItem item = new ListViewItem();// { Text = entry.Value };
                item.Content = pair.Key;
                item.Tag = pair.Value;
                if (i % 2 == 0)
                    item.Background = Brushes.Gray;
                else
                    item.Background = Brushes.DarkGray;
                item.Height = 40;
                list1.Items.Add(item);
                i++;
            }
            this.Topmost = true;
        }

        //The reason not to handle the PreviewMouseLeftButtonDown event is that, by the time when you handle the event, the ListView's SelectedItem may still be null.
        private void stationList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //selectedStation = stationList.SelectedValue.ToString();
            ListViewItem item = (ListViewItem)list1.SelectedItem;
            string fileName = item.Tag.ToString();
            //stationWindow.SetStationName(selectedStation);
            Type t = Type.GetType(fileName);
            Window win = (Window)Activator.CreateInstance(t);
            win.Show();
            this.Hide();
            currentWindow.Close();
        }

        public void AnimationShow()
        {
            DoubleAnimation animation = new DoubleAnimation(-this.Width, 0, TimeSpan.FromSeconds(0.3));
            //animation.RepeatBehavior = 1;
            //animation.AutoReverse = true;
            Storyboard.SetTargetName(animation, "Nav");
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.LeftProperty));
            // Create a storyboard to contain the animation.
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.Show();
            this.Activate();
            storyboard.Begin(this);
        }

        public void AnimationHide()
        {
            //this.Show();
            DoubleAnimation animation = new DoubleAnimation(0, -this.Width, TimeSpan.FromSeconds(0.3));
            Storyboard.SetTargetName(animation, "Nav");
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.LeftProperty));
            // Create a storyboard to contain the animation.
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.Show();
            this.Activate();
            storyboard.Begin(this);
        }

        private void Nav_Deactivated(object sender, EventArgs e)
        {
            this.AnimationHide();
            this.Hide();
        }
    }
}
