namespace Slac_DataAnalysis
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.btn_Alarm = new System.Windows.Forms.Button();
            this.comboBox_Alarm = new System.Windows.Forms.ComboBox();
            this.dateTimePicker_Alarm = new System.Windows.Forms.DateTimePicker();
            this.Btn_Form1Stop = new System.Windows.Forms.Button();
            this.Btn_Form1 = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.btn_Stats = new System.Windows.Forms.Button();
            this.comboBox_Stats = new System.Windows.Forms.ComboBox();
            this.dateTimePicker_Stats = new System.Windows.Forms.DateTimePicker();
            this.Btn_Form2Stop = new System.Windows.Forms.Button();
            this.Btn_Form2 = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.menu_icon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btn_show = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_closeme = new System.Windows.Forms.ToolStripMenuItem();
            this.panel2.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel5.SuspendLayout();
            this.menu_icon.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(218, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(866, 566);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Controls.Add(this.Btn_Form1Stop);
            this.panel2.Controls.Add(this.Btn_Form1);
            this.panel2.Location = new System.Drawing.Point(12, 12);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(200, 280);
            this.panel2.TabIndex = 1;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.btn_Alarm);
            this.panel4.Controls.Add(this.comboBox_Alarm);
            this.panel4.Controls.Add(this.dateTimePicker_Alarm);
            this.panel4.Enabled = false;
            this.panel4.Location = new System.Drawing.Point(-2, 76);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(200, 202);
            this.panel4.TabIndex = 6;
            // 
            // btn_Alarm
            // 
            this.btn_Alarm.BackColor = System.Drawing.Color.White;
            this.btn_Alarm.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_Alarm.Location = new System.Drawing.Point(43, 114);
            this.btn_Alarm.Name = "btn_Alarm";
            this.btn_Alarm.Size = new System.Drawing.Size(100, 37);
            this.btn_Alarm.TabIndex = 31;
            this.btn_Alarm.Text = "开始分析";
            this.btn_Alarm.UseVisualStyleBackColor = false;
            this.btn_Alarm.Click += new System.EventHandler(this.btn_Alarm_Click);
            // 
            // comboBox_Alarm
            // 
            this.comboBox_Alarm.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.comboBox_Alarm.FormattingEnabled = true;
            this.comboBox_Alarm.Items.AddRange(new object[] {
            "白班",
            "晚班"});
            this.comboBox_Alarm.Location = new System.Drawing.Point(5, 70);
            this.comboBox_Alarm.Name = "comboBox_Alarm";
            this.comboBox_Alarm.Size = new System.Drawing.Size(181, 27);
            this.comboBox_Alarm.TabIndex = 28;
            this.comboBox_Alarm.Text = "白班";
            // 
            // dateTimePicker_Alarm
            // 
            this.dateTimePicker_Alarm.CalendarFont = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Alarm.CustomFormat = "";
            this.dateTimePicker_Alarm.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Alarm.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker_Alarm.Location = new System.Drawing.Point(5, 23);
            this.dateTimePicker_Alarm.Name = "dateTimePicker_Alarm";
            this.dateTimePicker_Alarm.Size = new System.Drawing.Size(181, 29);
            this.dateTimePicker_Alarm.TabIndex = 27;
            // 
            // Btn_Form1Stop
            // 
            this.Btn_Form1Stop.BackColor = System.Drawing.Color.White;
            this.Btn_Form1Stop.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form1Stop.Location = new System.Drawing.Point(144, 15);
            this.Btn_Form1Stop.Name = "Btn_Form1Stop";
            this.Btn_Form1Stop.Size = new System.Drawing.Size(40, 37);
            this.Btn_Form1Stop.TabIndex = 5;
            this.Btn_Form1Stop.Text = "停止";
            this.Btn_Form1Stop.UseVisualStyleBackColor = false;
            this.Btn_Form1Stop.Click += new System.EventHandler(this.Btn_Form1Stop_Click);
            // 
            // Btn_Form1
            // 
            this.Btn_Form1.BackColor = System.Drawing.Color.White;
            this.Btn_Form1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form1.Location = new System.Drawing.Point(3, 15);
            this.Btn_Form1.Name = "Btn_Form1";
            this.Btn_Form1.Size = new System.Drawing.Size(119, 37);
            this.Btn_Form1.TabIndex = 1;
            this.Btn_Form1.Text = "报警信息分析";
            this.Btn_Form1.UseVisualStyleBackColor = false;
            this.Btn_Form1.Click += new System.EventHandler(this.Btn_Form1_Click_1);
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Controls.Add(this.panel5);
            this.panel3.Controls.Add(this.Btn_Form2Stop);
            this.panel3.Controls.Add(this.Btn_Form2);
            this.panel3.Location = new System.Drawing.Point(12, 298);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(200, 280);
            this.panel3.TabIndex = 2;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.btn_Stats);
            this.panel5.Controls.Add(this.comboBox_Stats);
            this.panel5.Controls.Add(this.dateTimePicker_Stats);
            this.panel5.Enabled = false;
            this.panel5.Location = new System.Drawing.Point(-2, 82);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(200, 196);
            this.panel5.TabIndex = 28;
            // 
            // btn_Stats
            // 
            this.btn_Stats.BackColor = System.Drawing.Color.White;
            this.btn_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_Stats.Location = new System.Drawing.Point(43, 117);
            this.btn_Stats.Name = "btn_Stats";
            this.btn_Stats.Size = new System.Drawing.Size(100, 37);
            this.btn_Stats.TabIndex = 31;
            this.btn_Stats.Text = "开始分析";
            this.btn_Stats.UseVisualStyleBackColor = false;
            this.btn_Stats.Click += new System.EventHandler(this.btn_Stats_Click);
            // 
            // comboBox_Stats
            // 
            this.comboBox_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.comboBox_Stats.FormattingEnabled = true;
            this.comboBox_Stats.Items.AddRange(new object[] {
            "白班",
            "晚班"});
            this.comboBox_Stats.Location = new System.Drawing.Point(5, 69);
            this.comboBox_Stats.Name = "comboBox_Stats";
            this.comboBox_Stats.Size = new System.Drawing.Size(181, 27);
            this.comboBox_Stats.TabIndex = 28;
            this.comboBox_Stats.Text = "白班";
            // 
            // dateTimePicker_Stats
            // 
            this.dateTimePicker_Stats.CalendarFont = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Stats.CustomFormat = "";
            this.dateTimePicker_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Stats.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker_Stats.Location = new System.Drawing.Point(5, 21);
            this.dateTimePicker_Stats.Name = "dateTimePicker_Stats";
            this.dateTimePicker_Stats.Size = new System.Drawing.Size(181, 29);
            this.dateTimePicker_Stats.TabIndex = 27;
            // 
            // Btn_Form2Stop
            // 
            this.Btn_Form2Stop.BackColor = System.Drawing.Color.White;
            this.Btn_Form2Stop.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form2Stop.Location = new System.Drawing.Point(144, 20);
            this.Btn_Form2Stop.Name = "Btn_Form2Stop";
            this.Btn_Form2Stop.Size = new System.Drawing.Size(40, 37);
            this.Btn_Form2Stop.TabIndex = 6;
            this.Btn_Form2Stop.Text = "停止";
            this.Btn_Form2Stop.UseVisualStyleBackColor = false;
            this.Btn_Form2Stop.Click += new System.EventHandler(this.Btn_Form2Stop_Click);
            // 
            // Btn_Form2
            // 
            this.Btn_Form2.BackColor = System.Drawing.Color.White;
            this.Btn_Form2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form2.Location = new System.Drawing.Point(3, 20);
            this.Btn_Form2.Name = "Btn_Form2";
            this.Btn_Form2.Size = new System.Drawing.Size(119, 37);
            this.Btn_Form2.TabIndex = 2;
            this.Btn_Form2.Text = "统计信息分析";
            this.Btn_Form2.UseVisualStyleBackColor = false;
            this.Btn_Form2.Click += new System.EventHandler(this.Btn_Form2_Click_1);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.menu_icon;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "设备数据分析";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // menu_icon
            // 
            this.menu_icon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_show,
            this.btn_closeme});
            this.menu_icon.Name = "menu_icon";
            this.menu_icon.Size = new System.Drawing.Size(142, 56);
            // 
            // btn_show
            // 
            this.btn_show.Image = ((System.Drawing.Image)(resources.GetObject("btn_show.Image")));
            this.btn_show.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_show.Name = "btn_show";
            this.btn_show.Size = new System.Drawing.Size(141, 26);
            this.btn_show.Text = "显示界面";
            this.btn_show.Click += new System.EventHandler(this.btn_show_Click);
            // 
            // btn_closeme
            // 
            this.btn_closeme.Image = ((System.Drawing.Image)(resources.GetObject("btn_closeme.Image")));
            this.btn_closeme.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_closeme.Name = "btn_closeme";
            this.btn_closeme.Size = new System.Drawing.Size(141, 26);
            this.btn_closeme.Text = "退出";
            this.btn_closeme.Click += new System.EventHandler(this.btn_closeme_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1096, 590);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "数据预处理与分析系统";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.panel2.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.menu_icon.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button Btn_Form1;
        private System.Windows.Forms.Button Btn_Form2;
        private System.Windows.Forms.Button Btn_Form1Stop;
        private System.Windows.Forms.Button Btn_Form2Stop;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip menu_icon;
        private System.Windows.Forms.ToolStripMenuItem btn_show;
        private System.Windows.Forms.ToolStripMenuItem btn_closeme;
        private System.Windows.Forms.DateTimePicker dateTimePicker_Alarm;
        private System.Windows.Forms.ComboBox comboBox_Alarm;
        private System.Windows.Forms.Button btn_Alarm;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.DateTimePicker dateTimePicker_Stats;
        private System.Windows.Forms.ComboBox comboBox_Stats;
        private System.Windows.Forms.Button btn_Stats;
    }
}

