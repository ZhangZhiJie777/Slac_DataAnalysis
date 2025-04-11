using CommonMonitorClassLibrary.HeartOperator;
using Slac_DataAnalysis.Common;
using Slac_DataAnalysis.DatabaseSql.DBModel;
using Slac_DataAnalysis.DatabaseSql.DBOper;
using Slac_DataAnalysis.FormPage;
using Slac_DataAnalysis_Bit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace Slac_DataAnalysis
{
    public partial class MainForm : Form
    {
        private frm_main_yzyl_bit frm_Main_Yzyl_bit = null; // 报警分析
        private frm_main_yzyl frm_Main_Yzyl = null;         // 统计分析
        private frm_main_yzyl_bit_alarm_btn frm_Main_Yzyl_Bit_Alarm_Btn = null; // 按钮开关分析
        private frm_main_device_state frm_Main_Device_State = null; // 设备状态分析

        public volatile static string alarm_Model = string.Empty;        // 报警模式
        public volatile static string alarm_Btn_Model = string.Empty;    // 按钮开关模式
        public volatile static string device_State_Model = string.Empty; // 设备状态模式
        private static string line_id = string.Empty; // 线体号

        private volatile string lastAnalyseTime;           // 上一个时间段分析开始时间
        private volatile string lastAnalyseTime_Alarm_Btn; // 上一个时间段分析开始时间（按钮开关）
        private volatile string lastAnalyseTime_Device_State; // 上一个时间段分析开始时间（设备状态）

        private static string Alarm_Permissions;        // 是否启用报警分析
        private static string Stats_Permissions;        // 是否启用统计分析
        private static string Alarm_Btn_Permissions;    // 是否启用按钮开关分析
        private static string Device_State_Permissions; // 是否启用设备状态分析

        //private TimedTask _timedTask;

        public MainForm()
        {
            InitializeComponent();
        }

        // 存出上次运行时间文件路径
        private static readonly string LastRunFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lastRun.txt");
        
        /// <summary>
        /// 获取数据库配置参数
        /// </summary>
        public void GetParamConfig()
        {
            try
            {
                DBSystemConfig dbSystemConfig = new DBSystemConfig();   //配置类
                DBOper.Init();
                DBOper db = new DBOper();
                List<DBSystemConfig> list = db.QueryList(dbSystemConfig);

                // 线体号
                line_id = list.Find(e => e.Name.Trim() == "LineID").Value.Trim();

                //上一次报警分析时间
                lastAnalyseTime = list.Find(e => e.Name.Trim() == "lastAnalyseTime").Value.Trim();

                // 上一次按钮开关分析时间
                lastAnalyseTime_Alarm_Btn = list.Find(e => e.Name.Trim() == "lastAnalyseTime_Alarm_Btn").Value.Trim();

                // 上一次设备状态分析时间
                lastAnalyseTime_Device_State = list.Find(e => e.Name.Trim() == "lastAnalyseTime_Device_State").Value.Trim();

                // 是否启用报警分析
                Alarm_Permissions = list.Find(e => e.Name.Trim() == "Alarm_Permissions").Value.Trim();

                // 是否启用统计分析
                Stats_Permissions = list.Find(e => e.Name.Trim() == "Stats_Permissions").Value.Trim();

                // 是否启用按钮开关分析
                Alarm_Btn_Permissions = list.Find(e => e.Name.Trim() == "Alarm_Btn_Permissions").Value.Trim();

                // 是否启用设备状态分析
                Device_State_Permissions = list.Find(e => e.Name.Trim() == "Device_State_Permissions").Value.Trim();

            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog", "Stats", $"获取数据库配置参数异常Error：{ex.ToString()}");
                //AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  获取数据库配置参数失败，请检查数据库配置！");
                //DataReturnEvent?.Invoke("label3", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  获取数据库配置参数失败，请检查数据库配置！");
            }
        }

        /// <summary>
        /// 主页面加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {

            GetParamConfig(); // 获取参数配置

            this.notifyIcon1.Text = $"设备数据分析-{line_id}";
            this.Text = $"数据预处理与分析系统-{line_id}";

            //_timedTask = new TimedTask(5, 1234);
            //// 启动定时任务
            //_timedTask.Start();

            DateTime lastRunTime;
            DateTime bootTime = GetSystemBootTime();
            bool isFirstRunAfterBoot = false;

            // 检查上次运行时间
            if (File.Exists(LastRunFilePath))
            {
                try
                {
                    //lastRunTime = DateTime.Parse(File.ReadAllText(LastRunFilePath));

                    lastRunTime = DateTime.ParseExact(File.ReadAllText(LastRunFilePath), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                    // 如果系统的启动时间晚于上次运行时间，说明是开机后的第一次运行
                    if (bootTime > lastRunTime)
                    {
                        isFirstRunAfterBoot = true;
                    }
                }
                catch (Exception ex)
                {
                    // 如果解析文件时间戳内容失败，则视为开机后的第一次运行
                    isFirstRunAfterBoot = true;
                }
            }
            else
            {
                isFirstRunAfterBoot = true; // 第一次运行
            }

            // 更新上次运行时间
            File.WriteAllText(LastRunFilePath, DateTime.Now.ToString());

            try
            {
                if (isFirstRunAfterBoot) // 开机后第一次运行
                {
                    Thread.Sleep(15000); // 等待15秒，等待开机服务启动
                }

                ChangePanel();
                //Slac_DataReceiveToMQModule.UpdateConnectStateEvent += UpdateConnectStateColor; // ReceiveToMQ订阅更新连接状态事件
                //Slac_DataMQ2CHModule.UpdateConnectStateEvent += UpdateConnectStateColor;       // MQ2CH订阅更新连接状态事件
                //Slac_DataMQ2MQModule.UpdateConnectStateEvent += UpdateConnectStateColor;       // MQ2MQ订阅更新连接状态事件

                if (Alarm_Permissions.Equals("1"))
                {
                    Btn_Form1_Click_1(null, null);
                }
                else { panel2.Enabled = false; }

                if (Stats_Permissions.Equals("1")) 
                {
                    Btn_Form2_Click_1(null, null);
                }
                else { panel3.Enabled = false; }

                if (Alarm_Btn_Permissions.Equals("1"))
                {
                    Btn_Form3_Click(null, null);
                }
                else { panel6.Enabled = false; }                

                if (Device_State_Permissions.Equals("1"))
                {
                    btn_Device_State_Click(null, null);
                }
                else { panel_Device_State.Enabled = false; }
                
                
            }
            catch (Exception ex)
            {
                this.Dispose();
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 获取系统开机时间
        /// </summary>
        private static DateTime GetSystemBootTime()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    var bootTimeString = mo["LastBootUpTime"].ToString();
                    return ManagementDateTimeConverter.ToDateTime(bootTimeString);
                }
            }
            return DateTime.MinValue; // 未能获取到开机时间
        }

        /// <summary>
        /// 改变控件大小
        /// </summary>
        private void ChangePanel()
        {
            double widthPanel = 0.75;
            double heightPanel = 1;

            //计算panel1的新尺寸
            int newWidth = (int)(this.ClientSize.Width * widthPanel) - 5;
            int newHeight = (int)(this.ClientSize.Height * heightPanel) - 20;

            //设置panel1的新尺寸
            panel1.Size = new Size(newWidth, newHeight);

            //设置panel1的新位置
            panel1.Location = new Point((int)(this.ClientSize.Width * 0.25), 10);

            #region 设置左侧菜单尺寸（按比例自适应）

            double widthPanelMenu = 0.25;
            double heightPanelMenu = 1 / 2;

            int newWidthMenu = (int)((this.ClientSize.Width) * widthPanelMenu) - 10;
            int newHeightMenu = (int)((this.ClientSize.Height - 35) / 4);

            panel2.Size = new Size(newWidthMenu, newHeightMenu);
            panel2.Location = new Point(5, 10);

            panel3.Size = new Size(newWidthMenu, newHeightMenu);
            panel3.Location = new Point(5, panel2.Location.Y + newHeightMenu + 5);

            panel6.Size = new Size(newWidthMenu, newHeightMenu);
            panel6.Location = new Point(5, panel3.Location.Y + newHeightMenu + 5); 


            panel_Device_State.Size = new Size(newWidthMenu, newHeightMenu);
            panel_Device_State.Location = new Point(5, panel6.Location.Y + newHeightMenu + 5);


            groupBox_Alarm.Width = panel2.Width - 10;
            groupBox_Alarm.Height = panel2.Height - 10;

            groupBox_Alarm_Btn.Width = panel6.Width - 10;
            groupBox_Alarm_Btn.Height = panel6.Height - 10;

            groupBox_Stats.Width = panel3.Width - 10;
            groupBox_Stats.Height = panel3.Height - 10;

            groupBox_Device_State.Width = panel_Device_State.Width - 10;
            groupBox_Device_State.Height = panel_Device_State.Height - 10;
            #endregion 设置左侧菜单尺寸（按比例自适应）
        }

        /// <summary>
        /// 窗体大小改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            ChangePanel();
        }

        /// <summary>
        /// 切换页面：报警分析信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Form1_Click_1(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl_bit != null)
            {
                frm_Main_Yzyl_bit.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Yzyl_bit);
            }
            else
            {
                #region 模式确认   

                if (radioButton_Alarm_Sub.Checked)
                {
                    alarm_Model = "分段模式";
                }
                else { alarm_Model = "整班次模式"; }

                GetParamConfig();

                DateTime dt;
                if (!DateTime.TryParse(lastAnalyseTime, out dt))
                {
                    radioButton_Alarm_Sub.Checked = false;
                    radioButton_Alarm_Shift.Checked = true;
                    alarm_Model = "整班次模式";
                }

                if (radioButton_Alarm_Shift.Checked)
                {
                    radioButton_Alarm_Sub.Enabled = false;
                    dateTimePicker_Alarm.Enabled = true;
                    comboBox_Alarm.Enabled = true;
                    btn_Alarm.Enabled = true;
                }
                else
                {
                    radioButton_Alarm_Shift.Enabled = false;
                    dateTimePicker_Alarm.Enabled = false;
                    comboBox_Alarm.Enabled = false;
                    btn_Alarm.Enabled = false;
                }


                #endregion

                frm_Main_Yzyl_bit = new frm_main_yzyl_bit();
                frm_Main_Yzyl_bit.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Yzyl_bit);
                Btn_Form1.BackColor = Color.Green;

                frm_Main_Yzyl_bit.UpdateMainFormSettingsInfoEvent += UpdateDateTimePicker;
                frm_Main_Yzyl_bit.FetchMainFormSettingsInfoEvent += FetchMainFormSettingsInfo;
            }
        }

        /// <summary>
        /// 切换页面：统计分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Form2_Click_1(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl != null)
            {
                frm_Main_Yzyl.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Yzyl);
            }
            else
            {
                frm_Main_Yzyl = new frm_main_yzyl();
                frm_Main_Yzyl.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Yzyl);
                //frm_Main_Yzyl.CloseForm += CloseForm;
                Btn_Form2.BackColor = Color.Green;                

                dateTimePicker_Stats.Enabled = true;
                comboBox_Stats.Enabled = true;
                btn_Stats.Enabled = true;

                frm_Main_Yzyl.UpdateMainFormSettingsInfoEvent += UpdateDateTimePicker;
                frm_Main_Yzyl.FetchMainFormSettingsInfoEvent += FetchMainFormSettingsInfo;
            }
        }

        /// <summary>
        /// 切换页面：按钮开关分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Form3_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl_Bit_Alarm_Btn != null)
            {
                frm_Main_Yzyl_Bit_Alarm_Btn.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Yzyl_Bit_Alarm_Btn);
            }
            else
            {
                #region 模式确认
                if (radioButton_Alarm_Btn_Sub.Checked)
                {
                    alarm_Btn_Model = "分段模式";
                }
                else { alarm_Btn_Model = "整班次模式"; }

                GetParamConfig();
                DateTime dt;
                if (!DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out dt))
                {
                    radioButton_Alarm_Btn_Sub.Checked = false;
                    radioButton_Alarm_Btn_Shift.Checked = true;
                    alarm_Btn_Model = "整班次模式";
                }

                if (radioButton_Alarm_Btn_Shift.Checked)
                {
                    radioButton_Alarm_Btn_Sub.Enabled = false;
                    dateTimePicker_Alarm_Btn.Enabled = true;
                    comboBox_Alarm_Btn.Enabled = true;
                    btn_Alarm_Btn.Enabled = true;
                }
                else
                {
                    radioButton_Alarm_Btn_Shift.Enabled = false;
                    dateTimePicker_Alarm_Btn.Enabled = false;
                    comboBox_Alarm_Btn.Enabled = false;
                    btn_Alarm_Btn.Enabled = false;
                }


                #endregion

                frm_Main_Yzyl_Bit_Alarm_Btn = new frm_main_yzyl_bit_alarm_btn();
                frm_Main_Yzyl_Bit_Alarm_Btn.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Yzyl_Bit_Alarm_Btn);
                Btn_Form3.BackColor = Color.Green;

                frm_Main_Yzyl_Bit_Alarm_Btn.UpdateMainFormSettingsInfoEvent += UpdateDateTimePicker;
                frm_Main_Yzyl_Bit_Alarm_Btn.FetchMainFormSettingsInfoEvent += FetchMainFormSettingsInfo;
            }
        }

        /// <summary>
        /// 切换页面：设备状态分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Device_State_Click(object sender, EventArgs e)
        {
            //frm_Main_Device_State
            if (frm_Main_Device_State != null)
            {
                frm_Main_Device_State.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Device_State);
            }
            else
            {
                #region 模式确认
                if (radioButton_Device_State_Sub.Checked)
                {
                    device_State_Model = "分段模式";
                }
                else { device_State_Model = "整班次模式"; }

                GetParamConfig();
                DateTime dt;
                if (!DateTime.TryParse(lastAnalyseTime_Device_State, out dt))
                {
                    radioButton_Device_State_Sub.Checked = false;
                    radioButton_Device_State_Shift.Checked = true;
                    device_State_Model = "整班次模式";
                }

                if (radioButton_Device_State_Shift.Checked)
                {
                    radioButton_Device_State_Sub.Enabled = false;
                    dateTimePicker_Device_State.Enabled = true;
                    comboBox_Device_State.Enabled = true;
                    btn_Device_State_Start.Enabled = true;
                }
                else
                {
                    radioButton_Device_State_Shift.Enabled = false;
                    dateTimePicker_Device_State.Enabled = false;
                    comboBox_Device_State.Enabled = false;
                    btn_Device_State_Start.Enabled = false;
                }


                #endregion

                frm_Main_Device_State = new frm_main_device_state();
                frm_Main_Device_State.Show();
                panel1.Controls.Clear();
                panel1.Controls.Add(frm_Main_Device_State);
                btn_Device_State.BackColor = Color.Green;

                frm_Main_Device_State.UpdateMainFormSettingsInfoEvent += UpdateDateTimePicker;
                frm_Main_Device_State.FetchMainFormSettingsInfoEvent += FetchMainFormSettingsInfo;
            }
        }

        /// <summary>
        /// 关闭报警分析页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Form1Stop_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl_bit != null)
            {
                bool result = false;
                this.Invoke(new Action(() =>
                {
                    if (MessageBox.Show(this, "确定要关闭报警分析吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        result = true;
                    }
                }));

                if (result)
                {
                    try
                    {
                        // 禁用按钮，防止多次点击
                        Btn_Form1Stop.Enabled = false;

                        this.BeginInvoke(new Action(async () =>
                        {
                            await frm_Main_Yzyl_bit.StopService(); // 关闭服务（关闭定时器，解析线程）
                            frm_Main_Yzyl_bit.Close();
                            frm_Main_Yzyl_bit.Dispose();
                            frm_Main_Yzyl_bit = null;
                            Btn_Form1.BackColor = Color.White;
                            Btn_Form1Stop.Enabled = true;

                            dateTimePicker_Alarm.Enabled = false;
                            comboBox_Alarm.Enabled = false;
                            btn_Alarm.Enabled = false;

                            radioButton_Alarm_Shift.Enabled = true;
                            radioButton_Alarm_Sub.Enabled = true;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// 关闭线体分析信息页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Form2Stop_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl != null)
            {
                bool result = false;
                this.Invoke(new Action(() =>
                {
                    if (MessageBox.Show(this, "确定要关闭统计分析吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        result = true;
                    }
                }));

                if (result)
                {
                    Btn_Form2Stop.Enabled = false;
                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            frm_Main_Yzyl.StopService();
                            frm_Main_Yzyl.Close();
                            frm_Main_Yzyl.Dispose();
                            frm_Main_Yzyl = null;
                            Btn_Form2.BackColor = Color.White;
                            Btn_Form2Stop.Enabled = true;

                            dateTimePicker_Stats.Enabled = false;
                            comboBox_Stats.Enabled = false;
                            btn_Stats.Enabled = false;
                            
                        }));

                        //await Task.Run(() =>
                        //{
                        //    //frm_Main_Yzyl_bit.btn_closeme_Click(null, null);
                        //});
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// 关闭按钮开关分析信息页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Form3Stop_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl_Bit_Alarm_Btn != null)
            {
                bool result = false;
                this.Invoke(new Action(() =>
                {
                    if (MessageBox.Show(this, "确定要关闭按钮开关分析吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        result = true;
                    }
                }));

                if (result)
                {
                    try
                    {
                        // 禁用按钮，防止多次点击
                        Btn_Form3Stop.Enabled = false;

                        this.BeginInvoke(new Action(async () =>
                        {
                            await frm_Main_Yzyl_Bit_Alarm_Btn.StopService(); // 关闭服务（关闭定时器，解析线程）
                            //frm_Main_Yzyl_Bit_Alarm_Btn.Close();
                            frm_Main_Yzyl_Bit_Alarm_Btn.Dispose();
                            frm_Main_Yzyl_Bit_Alarm_Btn = null;
                            Btn_Form3.BackColor = Color.White;
                            Btn_Form3Stop.Enabled = true;

                            dateTimePicker_Alarm_Btn.Enabled = false;
                            comboBox_Alarm_Btn.Enabled = false;
                            btn_Alarm_Btn.Enabled = false;

                            radioButton_Alarm_Btn_Shift.Enabled = true;
                            radioButton_Alarm_Btn_Sub.Enabled = true;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// 关闭设备状态分析页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Device_State_Stop_Click(object sender, EventArgs e)
        {
            if (frm_Main_Device_State != null)
            {
                bool result = false;
                this.Invoke(new Action(() =>
                {
                    if (MessageBox.Show(this, "确定要关闭设备状态分析吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        result = true;
                    }
                }));

                if (result)
                {
                    try
                    {
                        // 禁用按钮，防止多次点击
                        btn_Device_State_Stop.Enabled = false;

                        this.BeginInvoke(new Action(() =>
                        {
                            frm_Main_Device_State.StopService(); // 关闭服务（关闭定时器，解析线程）                            
                            frm_Main_Device_State.Dispose();
                            frm_Main_Device_State = null;
                            btn_Device_State.BackColor = Color.White;
                            btn_Device_State_Stop.Enabled = true;

                            dateTimePicker_Device_State.Enabled = false;
                            comboBox_Device_State.Enabled = false;
                            btn_Device_State_Start.Enabled = false;

                            radioButton_Device_State_Shift.Enabled = true;
                            radioButton_Device_State_Sub.Enabled = true;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// 获取界面设置信息
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private object FetchMainFormSettingsInfo(string type)
        {
            object obj = new object();
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.Invoke(new Action(() =>
                    {
                        switch (type)
                        {
                            case "报警分析时间":
                                obj = dateTimePicker_Alarm.Value;
                                obj = (DateTime)obj;
                                break;

                            case "报警分析班次":
                                obj = comboBox_Alarm.Text;
                                obj = obj.ToString();
                                break;

                            case "统计分析时间":
                                obj = dateTimePicker_Stats.Value;
                                obj = (DateTime)obj;
                                break;

                            case "统计分析班次":
                                obj = comboBox_Stats.Text;
                                obj = obj.ToString();
                                break;

                            case "按钮开关分析时间":
                                obj = dateTimePicker_Alarm_Btn.Value;
                                obj = (DateTime)obj;
                                break;

                            case "按钮开关分析班次":
                                obj = comboBox_Alarm_Btn.Text;
                                obj = obj.ToString();
                                break;

                            case "设备状态分析时间":
                                obj = dateTimePicker_Device_State.Value;
                                obj = (DateTime)obj;
                                break;

                            case "设备状态分析班次":
                                obj = comboBox_Device_State.Text;
                                obj = obj.ToString();
                                break;

                            default:
                                obj = null;
                                break;
                        }
                    }));
                }
                else { obj = null; }
            }
            catch (Exception)
            {
                return null;
            }
            return obj;
        }

        /// <summary>
        /// 更新界面时间选择器显示时间
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dateTime"></param>
        private void UpdateDateTimePicker(string type, object obj)
        {
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        switch (type)
                        {
                            case "报警分析时间":
                                dateTimePicker_Alarm.Value = Convert.ToDateTime(obj);
                                break;

                            case "报警分析班次":
                                comboBox_Alarm.Text = obj.ToString();
                                break;

                            case "统计分析时间":
                                dateTimePicker_Stats.Value = Convert.ToDateTime(obj);
                                break;

                            case "统计分析班次":
                                comboBox_Stats.Text = obj.ToString();
                                break;

                            case "按钮开关分析时间":
                                dateTimePicker_Alarm_Btn.Value = Convert.ToDateTime(obj);
                                break;

                            case "按钮开关分析班次":
                                comboBox_Alarm_Btn.Text = obj.ToString();
                                break;

                            case "设备状态分析时间":
                                dateTimePicker_Device_State.Value = Convert.ToDateTime(obj);
                                break;

                            case "设备状态分析班次":
                                comboBox_Device_State.Text = obj.ToString();
                                break;

                            default:
                                break;
                        }
                    }));
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 关闭子窗体,并释放资源
        /// </summary>
        /// <param name="param"></param>
        public void CloseForm(string param)
        {
            this.Invoke(new Action(() =>
            {
                try
                {
                    if (param == "frm_main_yzyl_bit" && frm_Main_Yzyl_bit != null)
                    {
                        frm_Main_Yzyl_bit.Close();
                        frm_Main_Yzyl_bit.Dispose();
                        frm_Main_Yzyl_bit = null;
                        Btn_Form1.BackColor = Color.White;

                        //if (panel1.Controls.Count == 0)
                        //{
                        //    this.BeginInvoke(new Action(() =>
                        //    {
                        //        textBox_MainShowMsg.Show();
                        //        panel1.Controls.Clear();
                        //        panel1.Controls.Add(textBox_MainShowMsg);
                        //    }));
                        //}
                    }
                    else if (param == "frm_mq2ch_V0" && frm_Main_Yzyl != null)
                    {
                        frm_Main_Yzyl.Close();
                        frm_Main_Yzyl.Dispose();
                        frm_Main_Yzyl = null;
                        Btn_Form2.BackColor = Color.White;

                        //if (panel1.Controls.Count == 0)
                        //{
                        //    this.BeginInvoke(new Action(() =>
                        //    {
                        //        textBox_MainShowMsg.Show();
                        //        panel1.Controls.Clear();
                        //        panel1.Controls.Add(textBox_MainShowMsg);
                        //    }));
                        //}
                    }
                    if (param == "MainForm")
                    {
                        this.Dispose();
                        System.Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭子窗体异常：{ex.Message}");
                }
            }));
        }

        /// <summary>
        /// 主窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 确保窗体关闭时停止定时任务
            //_timedTask.Stop();

            if (isFormClosing) { return; } // 防止重复关闭

            bool result = false;
            this.Invoke(new Action(() =>
            {
                if (MessageBox.Show(this, "是否要最小化到托盘？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    result = true;
                }
                else
                {
                    e.Cancel = true;
                }
            }));

            if (result)
            {
                this.WindowState = FormWindowState.Minimized; // 最小化窗体
                this.ShowInTaskbar = true;                    // 显示在系统任务栏

                //this.Hide();
            }

            e.Cancel = true;

            //Btn_Form1Stop_Click(null, null);
            //Btn_Form2Stop_Click(null, null);
            //LogConfig.Intence.StopLog();
        }

        /// <summary>
        /// 主窗体关闭之后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Program.ReleaseMutex(); // 释放互斥锁
        }

        #region 图标

        /// <summary>
        /// 图标双击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal; //窗体回复正常大小
        }

        /// <summary>
        /// 图标显示页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_show_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private bool isFormClosing; // 标志变量，用于避免重复关闭窗体

        /// <summary>
        /// 图标关闭程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_closeme_Click(object sender, EventArgs e)
        {
            isFormClosing = true;

            Btn_Form1Stop_Click(null, null); // 关闭报警分析
            Btn_Form2Stop_Click(null, null); // 关闭统计分析
            Btn_Form3Stop_Click(null, null); // 关闭按钮开关分析
            btn_Device_State_Stop_Click(null, null); // 关闭设备状态分析

            this.Dispose();
            this.Close();   // 手动关闭窗体会重新触发FormClosing事件，所以需要添加标志变量isFormClosing来避免重复关闭窗体
            LogConfig.Intence.StopLog();
            System.Environment.Exit(0);
        }

        #endregion 图标

        /// <summary>
        /// 报警分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Alarm_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl_bit != null)
            {
                frm_Main_Yzyl_bit.button2_Click(null, null); // 开启报警分析
            }
        }

        /// <summary>
        /// 统计分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Stats_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl != null)
            {
                frm_Main_Yzyl.button2_Click(null, null); // 开启统计分析
            }
        }

        /// <summary>
        /// 按钮开关分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_BtnAlarm_Click(object sender, EventArgs e)
        {
            if (frm_Main_Yzyl_Bit_Alarm_Btn != null)
            {
                frm_Main_Yzyl_Bit_Alarm_Btn.button2_Click(null, null); // 开启按钮开关分析
            }
        }

        /// <summary>
        /// 设备状态分析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Device_State_Start_Click(object sender, EventArgs e)
        {
            if (frm_Main_Device_State != null)
            {
                frm_Main_Device_State.button2_Click(null, null); // 开启设备状态分析
            }
        }

        /// <summary>
        /// 按钮开关分析模式选择——分段模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButton_Alarm_Btn_Sub_CheckedChanged(object sender, EventArgs e)
        {
            // 选择分段模式，需要判断数据库该配置字段数据是否是正确的时间数据
            if (radioButton_Alarm_Btn_Sub.Checked)
            {
                GetParamConfig();
                DateTime dt;
                if (!DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out dt))
                {
                    radioButton_Alarm_Btn_Sub.Checked = false;
                    radioButton_Alarm_Btn_Shift.Checked = true;
                    MessageBox.Show(this, "按钮开关分析：无法选择分析模式，请正确配置数据库上一次分析时间戳", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            //if (radioButton_Alarm_Btn_Sub.Checked)
            //{
            //    alarm_Btn_Model = "分段模式";
            //}
            //else { alarm_Btn_Model = "整班次模式"; }

        }

        /// <summary>
        /// 按钮开关分析模式选择——分段模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButton_Alarm_Sub_CheckedChanged(object sender, EventArgs e)
        {
            // 选择分段模式，需要判断数据库该配置字段数据是否是正确的时间数据
            if (radioButton_Alarm_Sub.Checked)
            {
                GetParamConfig();
                DateTime dt;
                if (!DateTime.TryParse(lastAnalyseTime, out dt))
                {
                    radioButton_Alarm_Sub.Checked = false;
                    radioButton_Alarm_Shift.Checked = true;
                    MessageBox.Show(this, "报警分析：无法选择分析模式，请正确配置数据库上一次分析时间戳", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 设备状态分析模式选择——分段模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButton_Device_State_Sub_CheckedChanged(object sender, EventArgs e)
        {
            // 选择分段模式，需要判断数据库该配置字段数据是否是正确的时间数据
            if (radioButton_Device_State_Sub.Checked)
            {
                GetParamConfig();
                DateTime dt;
                if (!DateTime.TryParse(lastAnalyseTime_Device_State, out dt))
                {
                    radioButton_Device_State_Sub.Checked = false;
                    radioButton_Device_State_Shift.Checked = true;
                    MessageBox.Show(this, "报警分析：无法选择分析模式，请正确配置数据库上一次分析时间戳", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}