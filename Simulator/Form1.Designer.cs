namespace AlarmSimulator
{
    partial class Mainfrm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.txtInterval2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnTrigger = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnBrowse = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.label3 = new System.Windows.Forms.Label();
            this.txtInterval1 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbMode = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtIntervalPump = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.btnTriggerPump = new System.Windows.Forms.Button();
            this.btnLocation = new System.Windows.Forms.Button();
            this.btnTriggerPumpToSqllite = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 77);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "File Write Interval:";
            // 
            // txtInterval2
            // 
            this.txtInterval2.Location = new System.Drawing.Point(125, 73);
            this.txtInterval2.Name = "txtInterval2";
            this.txtInterval2.Size = new System.Drawing.Size(117, 20);
            this.txtInterval2.TabIndex = 2;
            this.txtInterval2.Text = "5000";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(248, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "millisecond";
            // 
            // btnTrigger
            // 
            this.btnTrigger.Location = new System.Drawing.Point(70, 191);
            this.btnTrigger.Name = "btnTrigger";
            this.btnTrigger.Size = new System.Drawing.Size(139, 23);
            this.btnTrigger.TabIndex = 6;
            this.btnTrigger.Text = "StartTriggerAlarm";
            this.btnTrigger.UseVisualStyleBackColor = true;
            this.btnTrigger.Click += new System.EventHandler(this.btnTrigger_Click);
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(719, 463);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 11;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(125, 110);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(199, 20);
            this.txtPath.TabIndex = 3;
            this.txtPath.Text = "C:\\transactive\\ATSAlarms";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 114);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Path:";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(334, 109);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(30, 23);
            this.btnBrowse.TabIndex = 74;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // listView1
            // 
            this.listView1.FullRowSelect = true;
            this.listView1.Location = new System.Drawing.Point(385, 12);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(767, 409);
            this.listView1.TabIndex = 8;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(248, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "millisecond";
            // 
            // txtInterval1
            // 
            this.txtInterval1.Location = new System.Drawing.Point(125, 37);
            this.txtInterval1.Name = "txtInterval1";
            this.txtInterval1.Size = new System.Drawing.Size(117, 20);
            this.txtInterval1.TabIndex = 1;
            this.txtInterval1.Text = "1000";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 41);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(110, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Alarm Trigger Interval:";
            // 
            // cmbMode
            // 
            this.cmbMode.FormattingEnabled = true;
            this.cmbMode.Location = new System.Drawing.Point(125, 148);
            this.cmbMode.Name = "cmbMode";
            this.cmbMode.Size = new System.Drawing.Size(121, 21);
            this.cmbMode.TabIndex = 5;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 151);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(37, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "Mode:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(248, 278);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 13);
            this.label7.TabIndex = 16;
            this.label7.Text = "millisecond";
            // 
            // txtIntervalPump
            // 
            this.txtIntervalPump.Location = new System.Drawing.Point(125, 275);
            this.txtIntervalPump.Name = "txtIntervalPump";
            this.txtIntervalPump.Size = new System.Drawing.Size(117, 20);
            this.txtIntervalPump.TabIndex = 7;
            this.txtIntervalPump.Text = "1000";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(13, 279);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(111, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Pump Trigger Interval:";
            // 
            // btnTriggerPump
            // 
            this.btnTriggerPump.Location = new System.Drawing.Point(70, 334);
            this.btnTriggerPump.Name = "btnTriggerPump";
            this.btnTriggerPump.Size = new System.Drawing.Size(254, 23);
            this.btnTriggerPump.TabIndex = 8;
            this.btnTriggerPump.Text = "StartTriggerPumpToTransactDB";
            this.btnTriggerPump.UseVisualStyleBackColor = true;
            this.btnTriggerPump.Click += new System.EventHandler(this.btnTriggerPump_Click);
            // 
            // btnLocation
            // 
            this.btnLocation.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnLocation.Location = new System.Drawing.Point(228, 463);
            this.btnLocation.Name = "btnLocation";
            this.btnLocation.Size = new System.Drawing.Size(232, 23);
            this.btnLocation.TabIndex = 10;
            this.btnLocation.Text = "UpdateLocationByConfigFile";
            this.btnLocation.UseVisualStyleBackColor = true;
            this.btnLocation.Click += new System.EventHandler(this.btnLocation_Click);
            // 
            // btnTriggerPumpToSqllite
            // 
            this.btnTriggerPumpToSqllite.Location = new System.Drawing.Point(70, 386);
            this.btnTriggerPumpToSqllite.Name = "btnTriggerPumpToSqllite";
            this.btnTriggerPumpToSqllite.Size = new System.Drawing.Size(254, 23);
            this.btnTriggerPumpToSqllite.TabIndex = 9;
            this.btnTriggerPumpToSqllite.Text = "StartTriggerPumpToSqlliteDB";
            this.btnTriggerPumpToSqllite.UseVisualStyleBackColor = true;
            this.btnTriggerPumpToSqllite.Click += new System.EventHandler(this.btnTriggerPumpToSqllite_Click);
            // 
            // Mainfrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1164, 515);
            this.Controls.Add(this.btnTriggerPumpToSqllite);
            this.Controls.Add(this.btnLocation);
            this.Controls.Add(this.btnTriggerPump);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtIntervalPump);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cmbMode);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtInterval1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnTrigger);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtInterval2);
            this.Controls.Add(this.label1);
            this.Name = "Mainfrm";
            this.Text = "ATS Simulator";
            this.Load += new System.EventHandler(this.Mainfrm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtInterval2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnTrigger;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtInterval1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbMode;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtIntervalPump;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnTriggerPump;
        private System.Windows.Forms.Button btnLocation;
        private System.Windows.Forms.Button btnTriggerPumpToSqllite;
    }
}

