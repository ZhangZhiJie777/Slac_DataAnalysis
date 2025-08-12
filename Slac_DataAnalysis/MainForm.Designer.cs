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
            this.groupBox_Alarm = new System.Windows.Forms.GroupBox();
            this.btn_Alarm = new System.Windows.Forms.Button();
            this.Btn_Form1Stop = new System.Windows.Forms.Button();
            this.comboBox_Alarm = new System.Windows.Forms.ComboBox();
            this.radioButton_Alarm_Sub = new System.Windows.Forms.RadioButton();
            this.dateTimePicker_Alarm = new System.Windows.Forms.DateTimePicker();
            this.radioButton_Alarm_Shift = new System.Windows.Forms.RadioButton();
            this.Btn_Form1 = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btn_Stats = new System.Windows.Forms.Button();
            this.comboBox_Stats = new System.Windows.Forms.ComboBox();
            this.dateTimePicker_Stats = new System.Windows.Forms.DateTimePicker();
            this.Btn_Form2Stop = new System.Windows.Forms.Button();
            this.Btn_Form2 = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.menu_icon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btn_show = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_closeme = new System.Windows.Forms.ToolStripMenuItem();
            this.panel6 = new System.Windows.Forms.Panel();
            this.groupBox_Alarm_Btn = new System.Windows.Forms.GroupBox();
            this.btn_Alarm_Btn = new System.Windows.Forms.Button();
            this.Btn_Form3Stop = new System.Windows.Forms.Button();
            this.comboBox_Alarm_Btn = new System.Windows.Forms.ComboBox();
            this.Btn_Form3 = new System.Windows.Forms.Button();
            this.dateTimePicker_Alarm_Btn = new System.Windows.Forms.DateTimePicker();
            this.radioButton_Alarm_Btn_Sub = new System.Windows.Forms.RadioButton();
            this.radioButton_Alarm_Btn_Shift = new System.Windows.Forms.RadioButton();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.groupBox_Stats = new System.Windows.Forms.GroupBox();
            this.radioButton_Stats = new System.Windows.Forms.RadioButton();
            this.panel2.SuspendLayout();
            this.groupBox_Alarm.SuspendLayout();
            this.panel3.SuspendLayout();
            this.menu_icon.SuspendLayout();
            this.panel6.SuspendLayout();
            this.groupBox_Alarm_Btn.SuspendLayout();
            this.groupBox_Stats.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(259, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(825, 566);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.groupBox_Alarm);
            this.panel2.Location = new System.Drawing.Point(12, 12);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(241, 179);
            this.panel2.TabIndex = 1;
            // 
            // groupBox_Alarm
            // 
            this.groupBox_Alarm.Controls.Add(this.btn_Alarm);
            this.groupBox_Alarm.Controls.Add(this.Btn_Form1Stop);
            this.groupBox_Alarm.Controls.Add(this.comboBox_Alarm);
            this.groupBox_Alarm.Controls.Add(this.radioButton_Alarm_Sub);
            this.groupBox_Alarm.Controls.Add(this.dateTimePicker_Alarm);
            this.groupBox_Alarm.Controls.Add(this.radioButton_Alarm_Shift);
            this.groupBox_Alarm.Controls.Add(this.Btn_Form1);
            this.groupBox_Alarm.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox_Alarm.Location = new System.Drawing.Point(3, 3);
            this.groupBox_Alarm.Name = "groupBox_Alarm";
            this.groupBox_Alarm.Size = new System.Drawing.Size(231, 169);
            this.groupBox_Alarm.TabIndex = 0;
            this.groupBox_Alarm.TabStop = false;
            this.groupBox_Alarm.Text = "报警分析模块";
            // 
            // btn_Alarm
            // 
            this.btn_Alarm.BackColor = System.Drawing.Color.White;
            this.btn_Alarm.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_Alarm.Location = new System.Drawing.Point(158, 91);
            this.btn_Alarm.Name = "btn_Alarm";
            this.btn_Alarm.Size = new System.Drawing.Size(67, 29);
            this.btn_Alarm.TabIndex = 31;
            this.btn_Alarm.Text = "开始";
            this.btn_Alarm.UseVisualStyleBackColor = false;
            this.btn_Alarm.Click += new System.EventHandler(this.btn_Alarm_Click);
            // 
            // Btn_Form1Stop
            // 
            this.Btn_Form1Stop.BackColor = System.Drawing.Color.White;
            this.Btn_Form1Stop.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form1Stop.Location = new System.Drawing.Point(158, 56);
            this.Btn_Form1Stop.Name = "Btn_Form1Stop";
            this.Btn_Form1Stop.Size = new System.Drawing.Size(67, 29);
            this.Btn_Form1Stop.TabIndex = 5;
            this.Btn_Form1Stop.Text = "停止";
            this.Btn_Form1Stop.UseVisualStyleBackColor = false;
            this.Btn_Form1Stop.Click += new System.EventHandler(this.Btn_Form1Stop_Click);
            // 
            // comboBox_Alarm
            // 
            this.comboBox_Alarm.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.comboBox_Alarm.FormattingEnabled = true;
            this.comboBox_Alarm.Items.AddRange(new object[] {
            "白班",
            "晚班"});
            this.comboBox_Alarm.Location = new System.Drawing.Point(6, 126);
            this.comboBox_Alarm.Name = "comboBox_Alarm";
            this.comboBox_Alarm.Size = new System.Drawing.Size(138, 28);
            this.comboBox_Alarm.TabIndex = 28;
            this.comboBox_Alarm.Text = "白班";
            // 
            // radioButton_Alarm_Sub
            // 
            this.radioButton_Alarm_Sub.AutoSize = true;
            this.radioButton_Alarm_Sub.Checked = true;
            this.radioButton_Alarm_Sub.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.radioButton_Alarm_Sub.ForeColor = System.Drawing.SystemColors.ControlText;
            this.radioButton_Alarm_Sub.Location = new System.Drawing.Point(121, 27);
            this.radioButton_Alarm_Sub.Name = "radioButton_Alarm_Sub";
            this.radioButton_Alarm_Sub.Size = new System.Drawing.Size(67, 23);
            this.radioButton_Alarm_Sub.TabIndex = 31;
            this.radioButton_Alarm_Sub.TabStop = true;
            this.radioButton_Alarm_Sub.Text = "分段";
            this.radioButton_Alarm_Sub.UseVisualStyleBackColor = true;
            this.radioButton_Alarm_Sub.CheckedChanged += new System.EventHandler(this.radioButton_Alarm_Sub_CheckedChanged);
            // 
            // dateTimePicker_Alarm
            // 
            this.dateTimePicker_Alarm.CalendarFont = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Alarm.CustomFormat = "";
            this.dateTimePicker_Alarm.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Alarm.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker_Alarm.Location = new System.Drawing.Point(6, 91);
            this.dateTimePicker_Alarm.Name = "dateTimePicker_Alarm";
            this.dateTimePicker_Alarm.Size = new System.Drawing.Size(138, 29);
            this.dateTimePicker_Alarm.TabIndex = 27;
            // 
            // radioButton_Alarm_Shift
            // 
            this.radioButton_Alarm_Shift.AutoSize = true;
            this.radioButton_Alarm_Shift.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.radioButton_Alarm_Shift.Location = new System.Drawing.Point(18, 28);
            this.radioButton_Alarm_Shift.Name = "radioButton_Alarm_Shift";
            this.radioButton_Alarm_Shift.Size = new System.Drawing.Size(87, 23);
            this.radioButton_Alarm_Shift.TabIndex = 30;
            this.radioButton_Alarm_Shift.Text = "整班次";
            this.radioButton_Alarm_Shift.UseVisualStyleBackColor = true;
            // 
            // Btn_Form1
            // 
            this.Btn_Form1.BackColor = System.Drawing.Color.White;
            this.Btn_Form1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form1.Location = new System.Drawing.Point(6, 56);
            this.Btn_Form1.Name = "Btn_Form1";
            this.Btn_Form1.Size = new System.Drawing.Size(138, 29);
            this.Btn_Form1.TabIndex = 1;
            this.Btn_Form1.Text = "报警信息分析";
            this.Btn_Form1.UseVisualStyleBackColor = false;
            this.Btn_Form1.Click += new System.EventHandler(this.Btn_Form1_Click_1);
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Controls.Add(this.groupBox_Stats);
            this.panel3.Location = new System.Drawing.Point(12, 197);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(241, 166);
            this.panel3.TabIndex = 2;
            // 
            // btn_Stats
            // 
            this.btn_Stats.BackColor = System.Drawing.Color.White;
            this.btn_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_Stats.Location = new System.Drawing.Point(157, 89);
            this.btn_Stats.Name = "btn_Stats";
            this.btn_Stats.Size = new System.Drawing.Size(67, 29);
            this.btn_Stats.TabIndex = 31;
            this.btn_Stats.Text = "开始";
            this.btn_Stats.UseVisualStyleBackColor = false;
            this.btn_Stats.Click += new System.EventHandler(this.btn_Stats_Click);
            // 
            // comboBox_Stats
            // 
            this.comboBox_Stats.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.comboBox_Stats.FormattingEnabled = true;
            this.comboBox_Stats.Items.AddRange(new object[] {
            "白班",
            "晚班"});
            this.comboBox_Stats.Location = new System.Drawing.Point(6, 124);
            this.comboBox_Stats.Name = "comboBox_Stats";
            this.comboBox_Stats.Size = new System.Drawing.Size(138, 28);
            this.comboBox_Stats.TabIndex = 28;
            this.comboBox_Stats.Text = "白班";
            // 
            // dateTimePicker_Stats
            // 
            this.dateTimePicker_Stats.CalendarFont = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Stats.CustomFormat = "";
            this.dateTimePicker_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Stats.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker_Stats.Location = new System.Drawing.Point(6, 89);
            this.dateTimePicker_Stats.Name = "dateTimePicker_Stats";
            this.dateTimePicker_Stats.Size = new System.Drawing.Size(138, 29);
            this.dateTimePicker_Stats.TabIndex = 27;
            // 
            // Btn_Form2Stop
            // 
            this.Btn_Form2Stop.BackColor = System.Drawing.Color.White;
            this.Btn_Form2Stop.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form2Stop.Location = new System.Drawing.Point(157, 54);
            this.Btn_Form2Stop.Name = "Btn_Form2Stop";
            this.Btn_Form2Stop.Size = new System.Drawing.Size(67, 29);
            this.Btn_Form2Stop.TabIndex = 6;
            this.Btn_Form2Stop.Text = "停止";
            this.Btn_Form2Stop.UseVisualStyleBackColor = false;
            this.Btn_Form2Stop.Click += new System.EventHandler(this.Btn_Form2Stop_Click);
            // 
            // Btn_Form2
            // 
            this.Btn_Form2.BackColor = System.Drawing.Color.White;
            this.Btn_Form2.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form2.Location = new System.Drawing.Point(6, 54);
            this.Btn_Form2.Name = "Btn_Form2";
            this.Btn_Form2.Size = new System.Drawing.Size(138, 29);
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
            // panel6
            // 
            this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel6.Controls.Add(this.groupBox_Alarm_Btn);
            this.panel6.Location = new System.Drawing.Point(12, 369);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(241, 209);
            this.panel6.TabIndex = 3;
            // 
            // groupBox_Alarm_Btn
            // 
            this.groupBox_Alarm_Btn.Controls.Add(this.btn_Alarm_Btn);
            this.groupBox_Alarm_Btn.Controls.Add(this.Btn_Form3Stop);
            this.groupBox_Alarm_Btn.Controls.Add(this.comboBox_Alarm_Btn);
            this.groupBox_Alarm_Btn.Controls.Add(this.Btn_Form3);
            this.groupBox_Alarm_Btn.Controls.Add(this.dateTimePicker_Alarm_Btn);
            this.groupBox_Alarm_Btn.Controls.Add(this.radioButton_Alarm_Btn_Sub);
            this.groupBox_Alarm_Btn.Controls.Add(this.radioButton_Alarm_Btn_Shift);
            this.groupBox_Alarm_Btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox_Alarm_Btn.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox_Alarm_Btn.Location = new System.Drawing.Point(3, 3);
            this.groupBox_Alarm_Btn.Name = "groupBox_Alarm_Btn";
            this.groupBox_Alarm_Btn.Size = new System.Drawing.Size(231, 199);
            this.groupBox_Alarm_Btn.TabIndex = 29;
            this.groupBox_Alarm_Btn.TabStop = false;
            this.groupBox_Alarm_Btn.Text = "按钮分析模块";
            // 
            // btn_Alarm_Btn
            // 
            this.btn_Alarm_Btn.BackColor = System.Drawing.Color.White;
            this.btn_Alarm_Btn.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_Alarm_Btn.Location = new System.Drawing.Point(158, 87);
            this.btn_Alarm_Btn.Name = "btn_Alarm_Btn";
            this.btn_Alarm_Btn.Size = new System.Drawing.Size(67, 29);
            this.btn_Alarm_Btn.TabIndex = 31;
            this.btn_Alarm_Btn.Text = "开始";
            this.btn_Alarm_Btn.UseVisualStyleBackColor = false;
            this.btn_Alarm_Btn.Click += new System.EventHandler(this.btn_BtnAlarm_Click);
            // 
            // Btn_Form3Stop
            // 
            this.Btn_Form3Stop.BackColor = System.Drawing.Color.White;
            this.Btn_Form3Stop.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form3Stop.Location = new System.Drawing.Point(158, 52);
            this.Btn_Form3Stop.Name = "Btn_Form3Stop";
            this.Btn_Form3Stop.Size = new System.Drawing.Size(67, 29);
            this.Btn_Form3Stop.TabIndex = 6;
            this.Btn_Form3Stop.Text = "停止";
            this.Btn_Form3Stop.UseVisualStyleBackColor = false;
            this.Btn_Form3Stop.Click += new System.EventHandler(this.Btn_Form3Stop_Click);
            // 
            // comboBox_Alarm_Btn
            // 
            this.comboBox_Alarm_Btn.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.comboBox_Alarm_Btn.FormattingEnabled = true;
            this.comboBox_Alarm_Btn.Items.AddRange(new object[] {
            "白班",
            "晚班"});
            this.comboBox_Alarm_Btn.Location = new System.Drawing.Point(6, 122);
            this.comboBox_Alarm_Btn.Name = "comboBox_Alarm_Btn";
            this.comboBox_Alarm_Btn.Size = new System.Drawing.Size(138, 28);
            this.comboBox_Alarm_Btn.TabIndex = 28;
            this.comboBox_Alarm_Btn.Text = "白班";
            // 
            // Btn_Form3
            // 
            this.Btn_Form3.BackColor = System.Drawing.Color.White;
            this.Btn_Form3.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Btn_Form3.Location = new System.Drawing.Point(6, 52);
            this.Btn_Form3.Name = "Btn_Form3";
            this.Btn_Form3.Size = new System.Drawing.Size(138, 29);
            this.Btn_Form3.TabIndex = 2;
            this.Btn_Form3.Text = "按钮开关分析";
            this.Btn_Form3.UseVisualStyleBackColor = false;
            this.Btn_Form3.Click += new System.EventHandler(this.Btn_Form3_Click);
            // 
            // dateTimePicker_Alarm_Btn
            // 
            this.dateTimePicker_Alarm_Btn.CalendarFont = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Alarm_Btn.CustomFormat = "";
            this.dateTimePicker_Alarm_Btn.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dateTimePicker_Alarm_Btn.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker_Alarm_Btn.Location = new System.Drawing.Point(6, 87);
            this.dateTimePicker_Alarm_Btn.Name = "dateTimePicker_Alarm_Btn";
            this.dateTimePicker_Alarm_Btn.Size = new System.Drawing.Size(138, 29);
            this.dateTimePicker_Alarm_Btn.TabIndex = 27;
            // 
            // radioButton_Alarm_Btn_Sub
            // 
            this.radioButton_Alarm_Btn_Sub.AutoSize = true;
            this.radioButton_Alarm_Btn_Sub.Checked = true;
            this.radioButton_Alarm_Btn_Sub.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.radioButton_Alarm_Btn_Sub.ForeColor = System.Drawing.SystemColors.ControlText;
            this.radioButton_Alarm_Btn_Sub.Location = new System.Drawing.Point(121, 23);
            this.radioButton_Alarm_Btn_Sub.Name = "radioButton_Alarm_Btn_Sub";
            this.radioButton_Alarm_Btn_Sub.Size = new System.Drawing.Size(67, 23);
            this.radioButton_Alarm_Btn_Sub.TabIndex = 30;
            this.radioButton_Alarm_Btn_Sub.TabStop = true;
            this.radioButton_Alarm_Btn_Sub.Text = "分段";
            this.radioButton_Alarm_Btn_Sub.UseVisualStyleBackColor = true;
            this.radioButton_Alarm_Btn_Sub.CheckedChanged += new System.EventHandler(this.radioButton_Alarm_Btn_Sub_CheckedChanged);
            // 
            // radioButton_Alarm_Btn_Shift
            // 
            this.radioButton_Alarm_Btn_Shift.AutoSize = true;
            this.radioButton_Alarm_Btn_Shift.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.radioButton_Alarm_Btn_Shift.Location = new System.Drawing.Point(18, 23);
            this.radioButton_Alarm_Btn_Shift.Name = "radioButton_Alarm_Btn_Shift";
            this.radioButton_Alarm_Btn_Shift.Size = new System.Drawing.Size(87, 23);
            this.radioButton_Alarm_Btn_Shift.TabIndex = 29;
            this.radioButton_Alarm_Btn_Shift.Text = "整班次";
            this.radioButton_Alarm_Btn_Shift.UseVisualStyleBackColor = true;
            // 
            // groupBox_Stats
            // 
            this.groupBox_Stats.Controls.Add(this.Btn_Form2Stop);
            this.groupBox_Stats.Controls.Add(this.comboBox_Stats);
            this.groupBox_Stats.Controls.Add(this.btn_Stats);
            this.groupBox_Stats.Controls.Add(this.dateTimePicker_Stats);
            this.groupBox_Stats.Controls.Add(this.radioButton_Stats);
            this.groupBox_Stats.Controls.Add(this.Btn_Form2);
            this.groupBox_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox_Stats.Location = new System.Drawing.Point(4, 5);
            this.groupBox_Stats.Name = "groupBox_Stats";
            this.groupBox_Stats.Size = new System.Drawing.Size(230, 164);
            this.groupBox_Stats.TabIndex = 33;
            this.groupBox_Stats.TabStop = false;
            this.groupBox_Stats.Text = "统计分析模块";
            // 
            // radioButton_Stats
            // 
            this.radioButton_Stats.AutoSize = true;
            this.radioButton_Stats.Checked = true;
            this.radioButton_Stats.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.radioButton_Stats.Location = new System.Drawing.Point(17, 27);
            this.radioButton_Stats.Name = "radioButton_Stats";
            this.radioButton_Stats.Size = new System.Drawing.Size(87, 23);
            this.radioButton_Stats.TabIndex = 32;
            this.radioButton_Stats.TabStop = true;
            this.radioButton_Stats.Text = "整班次";
            this.radioButton_Stats.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1096, 600);
            this.Controls.Add(this.panel6);
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
            this.groupBox_Alarm.ResumeLayout(false);
            this.groupBox_Alarm.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.menu_icon.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.groupBox_Alarm_Btn.ResumeLayout(false);
            this.groupBox_Alarm_Btn.PerformLayout();
            this.groupBox_Stats.ResumeLayout(false);
            this.groupBox_Stats.PerformLayout();
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
        private System.Windows.Forms.DateTimePicker dateTimePicker_Stats;
        private System.Windows.Forms.ComboBox comboBox_Stats;
        private System.Windows.Forms.Button btn_Stats;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Button btn_Alarm_Btn;
        private System.Windows.Forms.ComboBox comboBox_Alarm_Btn;
        private System.Windows.Forms.DateTimePicker dateTimePicker_Alarm_Btn;
        private System.Windows.Forms.Button Btn_Form3Stop;
        private System.Windows.Forms.Button Btn_Form3;
        private System.Windows.Forms.RadioButton radioButton_Alarm_Btn_Shift;
        private System.Windows.Forms.GroupBox groupBox_Alarm_Btn;
        private System.Windows.Forms.RadioButton radioButton_Alarm_Btn_Sub;
        private System.Windows.Forms.GroupBox groupBox_Alarm;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.RadioButton radioButton_Alarm_Sub;
        private System.Windows.Forms.RadioButton radioButton_Alarm_Shift;
        private System.Windows.Forms.GroupBox groupBox_Stats;
        private System.Windows.Forms.RadioButton radioButton_Stats;
    }
}

