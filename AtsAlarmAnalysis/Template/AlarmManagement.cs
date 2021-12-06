using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TemplateProject
{
    public partial class AlarmManagement
    {
        void InitScript()
        {
            btnEdit.Click += btnEdit_Click;
            btnEdit.MouseEnter += menuButton_MouseEnter;
            btnEdit.MouseLeave += menuButton_MouseLeave;

        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            AlarmFilterWindow filter = new AlarmFilterWindow(null);
            filter.ShowDialog();
        }

        void LoadScript()
        {
            int seed1 = 0;
            Random random1 = new Random(seed1);
            {   //Dummy data for Chart1
                List<RawTable> data = new List<RawTable>();
                int maxHour = 24;
                for (int j = 0; j <= maxHour; j++)
                {
                    int value = random1.Next(0, 50000);
                    RawTable rawTable = new RawTable(j, value);
                    data.Add(rawTable);
                }
                dataSourceMap["Data1_1"].ReloadChartData(data);
            }
            {   //Dummy data for Chart2
                List<RawTable> data = new List<RawTable>();
                data.Add(new RawTable("COM", 5));
                data.Add(new RawTable("RSC", 40));
                data.Add(new RawTable("SIG", 60));
                dataSourceMap["Data2_1"].ReloadChartData(data);
            }
            {   //Dummy data for Chart3
                List<RawTable> data = new List<RawTable>();
                data.Add(new RawTable("ATC", 45));
                data.Add(new RawTable("ATS", 3));
                data.Add(new RawTable("DCHK", 19));
                data.Add(new RawTable("DRS", 26));
                data.Add(new RawTable("DRV", 2));
                data.Add(new RawTable("PIS", 2));
                data.Add(new RawTable("RCS", 2));
                data.Add(new RawTable("TIM", 2));
                dataSourceMap["Data3_1"].ReloadChartData(data);
            }
            {   //Dummy data for Chart4
                List<RawTable> data = new List<RawTable>();
                data.Add(new RawTable("90010/RSC/DRS/ALL", 4));
                data.Add(new RawTable("90070/RSC/DRS/ALL", 5));
                data.Add(new RawTable("90080/RSC/DRS/ALL", 6));
                data.Add(new RawTable("90110/RSC/DRS/ALL", 3));
                data.Add(new RawTable("90150/RSC/DRS/ALL", 4));
                data.Add(new RawTable("90160/RSC/DRS/ALL", 5));
                data.Add(new RawTable("90170/RSC/DRS/ALL", 3));
                data.Add(new RawTable("90190/RSC/DRS/ALL", 4));
                data.Add(new RawTable("90200/RSC/DRS/ALL", 6));
                data.Add(new RawTable("90210/RSC/DRS/ALL", 2));
                data.Add(new RawTable("90240/RSC/DRS/ALL", 3));
                data.Add(new RawTable("90250/RSC/DRS/ALL", 4));
                data.Add(new RawTable("90270/RSC/DRS/ALL", 5));
                data.Add(new RawTable("90300/RSC/DRS/ALL", 6));
                data.Add(new RawTable("90320/RSC/DRS/ALL", 2));
                data.Add(new RawTable("90330/RSC/DRS/ALL", 4));
                data.Add(new RawTable("90350/RSC/DRS/ALL", 3));
                data.Add(new RawTable("90370/RSC/DRS/ALL", 2));
                data.Add(new RawTable("90380/RSC/DRS/ALL", 5));
                data.Add(new RawTable("90750/RSC/DRS/ALL", 4));
                data.Add(new RawTable("90790/RSC/DRS/ALL", 3));
                data.Add(new RawTable("others(356)", 4));
                dataSourceMap["Data4_1"].LegendText = "";
                dataSourceMap["Data4_1"].ReloadChartData(data);
            }
            {   //Dummy data for Chart5
                List<RawTable> data1 = new List<RawTable>();
                List<RawTable> data2 = new List<RawTable>();
                List<RawTable> data3 = new List<RawTable>();
                int maxHour = 24;
                for (int j = 0; j <= maxHour; j++)
                {
                    data1.Add(new RawTable(j, random1.Next(0, 0)));
                    data2.Add(new RawTable(j, random1.Next(0, 50000)));
                    data3.Add(new RawTable(j, random1.Next(0, 10000)));
                }
                dataSourceMap["Data5_1"].ReloadChartData(data1);
                dataSourceMap["Data5_2"].ReloadChartData(data2);
                dataSourceMap["Data5_3"].ReloadChartData(data3);
            }
        }
    }
}
