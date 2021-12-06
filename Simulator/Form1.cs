using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace AlarmSimulator
{
    public partial class Mainfrm : Form
    {
        int fileSequence = 1;
        int alarmCount = 0;       //no alarm created now
        List<XmlElement> alarmElementList = new List<XmlElement>();
        int alarmTriggerInterval = 1000;
        int fileWriteInterval = 5000;
        string xmlPath = "";
        XmlDocument xmlDoc = null;
        int writeFrequency = 5;
        AlarmTriggerProcessor alarmTriggerProcessor = null;
        PumpTriggerProcessor pumpTriggerProcessor = null;
        int pumpTriggerInterval = 1000;
        Random random = new Random();

        public Mainfrm()
        {
            InitializeComponent();
            initListView();
            cmbMode.Items.Add("Trigger Ats alarm only");
            cmbMode.Items.Add("Trigger Iscs alarm only");
            cmbMode.Items.Add("Trigger mix alarm");
            cmbMode.SelectedIndex = 2;
        }

        private void Mainfrm_Load(object sender, EventArgs e)
        {
            alarmTriggerProcessor = new AlarmTriggerProcessor();
            pumpTriggerProcessor = new PumpTriggerProcessor();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void initListView()
        {
            // Add columns
            listView1.Columns.Add("Date", 150, HorizontalAlignment.Left);
            listView1.Columns.Add("alarmID", 150, HorizontalAlignment.Left);
            listView1.Columns.Add("description", 600, HorizontalAlignment.Left);
        }
        
        //start to trgger alarma and save to xml
        private void btnTrigger_Click(object sender, EventArgs e)
        {
            if (btnTrigger.Text == "StartTriggerAlarm")
            {
                xmlPath = txtPath.Text;
                if (!Directory.Exists(xmlPath))
                {
                    MessageBox.Show("Path not exist!");
                    txtPath.Focus();
                    return;
                }

                try
                {
                    alarmTriggerInterval = Convert.ToInt32(txtInterval1.Text);
                    if (alarmTriggerInterval <= 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    MessageBox.Show("Please enter a valid alarm trigger interval");
                    txtInterval1.Focus();
                    return;
                }
                
                try
                {
                    fileWriteInterval = Convert.ToInt32(txtInterval2.Text);
                    if (fileWriteInterval <= 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    MessageBox.Show("Please enter a valid file write interval");
                    txtInterval2.Focus();
                    return;
                }
                
                writeFrequency = fileWriteInterval / alarmTriggerInterval;
                timer1.Interval = alarmTriggerInterval;
                timer1.Start();
                btnTrigger.Text = "Stop";
            }
            else
            {
                timer1.Stop();
                btnTrigger.Text = "StartTriggerAlarm";
            }
        }

        //choose save path
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = txtPath.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = dlg.SelectedPath;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Triger alarm interval
            if (xmlDoc == null)
            {
                xmlDoc = new XmlDocument();
            }
            int isAtsAlarm = 0;
            if (cmbMode.SelectedIndex == 0)
                isAtsAlarm = 1;
            else if (cmbMode.SelectedIndex == 1)
                isAtsAlarm = 0;
            else
                isAtsAlarm = random.Next(0, 2);

            string description, alarmID1;
            alarmElementList.Add(alarmTriggerProcessor.CreateAlarmElement(xmlDoc, isAtsAlarm, out alarmID1, out description, false));        //create AtsAlarm or IscsAlarm
            ListViewItem lstItem = new ListViewItem(new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), alarmID1, description });
            listView1.Items.Insert(0, lstItem);
            listView1.Update();
            alarmCount++;

            if (alarmCount % writeFrequency == 0)
            {   //write xml document now
                XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(docNode);
                XmlElement elRoot = xmlDoc.CreateElement("body");
                xmlDoc.AppendChild(elRoot);
                foreach (XmlElement el in alarmElementList)
                {
                    try
                    {
                        elRoot.AppendChild(el);
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Instance.LOG.Error(ex.ToString());
                    }
                }
                //save file
                string fileName = "AtsAlarm-" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + fileSequence.ToString() + ".xml";
                string fileFullPath = Path.Combine(xmlPath, fileName);
                xmlDoc.Save(fileFullPath);
                fileSequence++;
                alarmElementList.Clear();
                xmlDoc = null;
            }
        }

        private void btnTriggerPump_Click(object sender, EventArgs e)
        {
            if (btnTriggerPump.Text == "StartTriggerPumpToTransactDB")
            {
                pumpTriggerProcessor.bTriggerToTransactDB = true;
                try
                {
                    pumpTriggerInterval = Convert.ToInt32(txtIntervalPump.Text);
                    if (pumpTriggerInterval <= 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    MessageBox.Show("Please enter a valid pump trigger interval");
                    txtIntervalPump.Focus();
                    return;
                }
                pumpTriggerProcessor.Start(pumpTriggerInterval);
                btnTriggerPump.Text = "Stop";
            }
            else
            {
                pumpTriggerProcessor.Stop();
                btnTriggerPump.Text = "StartTriggerPumpToTransactDB";
            }
        }

        private void btnTriggerPumpToSqllite_Click(object sender, EventArgs e)
        {
            if (btnTriggerPumpToSqllite.Text == "StartTriggerPumpToSqlliteDB")
            {
                pumpTriggerProcessor.bTriggerToTransactDB = false;
                try
                {
                    pumpTriggerInterval = Convert.ToInt32(txtIntervalPump.Text);
                    if (pumpTriggerInterval <= 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    MessageBox.Show("Please enter a valid pump trigger interval");
                    txtIntervalPump.Focus();
                    return;
                }
                pumpTriggerProcessor.Start(pumpTriggerInterval);
                btnTriggerPumpToSqllite.Text = "Stop";
            }
            else
            {
                pumpTriggerProcessor.Stop();
                btnTriggerPumpToSqllite.Text = "StartTriggerPumpToSqlliteDB";
            }
        }

        private void btnLocation_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Update TRANSACT Db location display name by config file? Operation is irrevocable", "Warning", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                int num = 0;
                try
                {
                    foreach (Location location in IscsCachedMap.Instance.locationList)
                    {
                        string sqlstr = "update location set display_name='" + location.Name + "' where pkey=" + location.ID;
                        Console.WriteLine(sqlstr);
                        DAIHelper.Instance.DbServerTransact.ExecuteNonQuery(sqlstr);
                        num++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }

                MessageBox.Show("Total update " + num + " record");
            }
        }

    }
}
