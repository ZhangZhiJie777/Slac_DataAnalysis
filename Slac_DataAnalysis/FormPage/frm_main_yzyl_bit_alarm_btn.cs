using ServiceStack.Redis;
using Slac_DataAnalysis.Common;
using Slac_DataAnalysis.DatabaseSql.DBModel;
using Slac_DataAnalysis.DatabaseSql.DBOper;
using Slac_DataAnalysis_Bit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Slac_DataAnalysis.FormPage
{
    public partial class frm_main_yzyl_bit_alarm_btn : UserControl
    {
        public frm_main_yzyl_bit_alarm_btn()
        {
            InitializeComponent();

            //this.TopLevel = false;                       //窗体不作为顶级窗体
            this.Dock = DockStyle.Fill;                  //窗体填充父容器
                                                         // this.FormBorderStyle = FormBorderStyle.None; //隐藏窗体边框

            this.DoubleBuffered = true; //开启双缓冲，防止窗体闪烁

            //this.comboBox1.Visible = false;
            //this.dateTimePicker1.Visible = false;
            this.button2.Visible = false;
        }

        public event Action<string, object> UpdateMainFormSettingsInfoEvent; // 更新主界面设置信息
        public event Func<string, object> FetchMainFormSettingsInfoEvent;    // 获取主界面设置信息

        public CancellationTokenSource cts = new CancellationTokenSource();  // 取消线程(task16,task32)
        public Task task16; // Task线程解析处理16位设备数据
        public Task task32; // Task线程解析处理32位设备数据

        private Thread thread10 = null;
        private static bool isStartExec10 = false; // 是否开始执行线程
        private bool TimerStatus = true;           // 定时器状态
        private bool thread10State;                // 线程状态        

        private volatile string workdate = "";   // 工作日期
        private volatile string workshift = "1"; // 班次（白夜班）
        private volatile string startTime = "";  // 查询clickhouse数据库起始时间
        private volatile string endTime = "";    // 查询clickhouse数据库结束时间        

        private static Int32 lastValue16 = -1; // 16位设备上一次变化值
        private static Int32 lastValue32 = -1; // 32位设备上一次变化值
        private static DateTime lastTime;      // 上一次变化时间

        private volatile bool isAnalyzing;                 // 是否正在分析
        //private volatile string lastAnalyseTime;           // 上一个时间段分析开始时间
        private volatile string lastAnalyseTime_Alarm_Btn; // 上一个时间段分析开始时间（按钮报警）
        private volatile bool isNewVersion = false;        // 是否是最新版本(true:最新版本,采用标志位时间戳，分段分析  false:不是最新版本，直接分析整个班次) 在获取lastAnalyseTime_Alarm_Btn参数成功的情况下，默认是最新版本
        private volatile bool isRightAnalysis = true;      // 这次分析是否是正常分析流程（表示这个时间段需要重新分析）
        private volatile bool isInitRedis = false;         // 是否初始化Redis的键值（若当前时间段有报错，重新分析，需要重置Redis）

        private volatile HttpClient httpClient16;    // 16位设备HttpClient（用于查询点位数据）
        private volatile HttpClient httpClient32;    // 32位设备HttpClient（用于查询点位数据）        
        private volatile HttpClient httpClientTimer; // 定时器HttpClient（用于查询服务器最新数据是否需要开始分析)

        #region 数据库配置参数        
        private static string CHuser = "default"; // click house数据库用户名
        private static string CHserver;           // click house数据库IP // "172.16.253.3"
        private static string CHport;             // click house数据库端口
        private static string CHpasswd;           // click house数据库密码
        private static string isCluster;          // 是否集群(分布式)

        private static string CHtable_name = "";  //非分布式表空白
        private static string device_16bit;       //16位设备（设备从10开始）区分哪些设备是按照16位解析

        private static string RedisServer;  // Redis数据库IP
        private static int RedisPort;       // Redis数据库端口
        private static string RedisPasswd;  // Redis数据库密码

        private static string line_id;      // 线体ID line +线体编号
        private static string companyNum;   // "yzenpack"; //公司名称（数据库名称） 
        private static string Conn_battery; // 看板服务器Mysql数据库连接字符串
        #endregion        

        #region 注释
        //private static string CHserver = ConfigHelper.GetAppConfig("CHserver");  //"172.16.0.30";
        //private static Int32 lastValue = -1;
        //private static DateTime lastTime;
        //private static string workdate = "";
        //private static string workshift = "1";
        //private static string startTime = "";
        //private static string endTime = "";
        //private static string line_id = "line" + ConfigHelper.GetAppConfig("LineID");
        //private static string companyNum = ConfigHelper.GetAppConfig("factory");  // "yzenpack";
        //private static string CHtable_name = "_all"; ////分布式表加all
        //private static string CHuser = "default";
        //private static string CHpasswd = "slac1028#"; 

        //public RedisClient client = new RedisClient("127.0.0.1", 6379);
        #endregion

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

                // 是否开启集群（分布式）
                isCluster = list.Find(e => e.Name.Trim() == "isCluster").Value.Trim();

                // 16位设备（设备从10开始）区分哪些设备是按照16位解析
                device_16bit = list.Find(e => e.Name.Trim() == "device_16bit").Value.Trim();

                // 线体号
                line_id = "line" + list.Find(e => e.Name.Trim() == "LineID").Value.Trim();

                // 数据库库名称（公司名称）
                companyNum = list.Find(e => e.Name.Trim() == "companyNum").Value.Trim();

                // 看板服务器Mysql数据库连接字符串
                Conn_battery = list.Find(e => e.Name.Trim() == "Conn_battery").Value.Trim();

                // click house数据库配置
                CHpasswd = list.Find(e => e.Name.Trim() == "CHserver").Value.Trim().Split('|')[0];
                CHserver = list.Find(e => e.Name.Trim() == "CHserver").Value.Trim().Split('|')[1];
                CHport = list.Find(e => e.Name.Trim() == "CHserver").Value.Trim().Split('|')[2];

                // Radis配置
                RedisServer = list.Find(e => e.Name.Trim() == "RedisServer").Value.Trim().Split('|')[0];
                RedisPort = Convert.ToInt32(list.Find(e => e.Name.Trim() == "RedisServer").Value.Trim().Split('|')[1]);
                RedisPasswd = list.Find(e => e.Name.Trim() == "RedisServer").Value.Trim().Split('|')[2];                              

                // 上一次按钮报警分析时间
                lastAnalyseTime_Alarm_Btn = list.Find(e => e.Name.Trim() == "lastAnalyseTime_Alarm_Btn").Value.Trim();

                // 分段模式下，初始化为班次开始时间
                if (!string.IsNullOrEmpty(lastAnalyseTime_Alarm_Btn) && lastAnalyseTime_Alarm_Btn.Length == 19)
                {
                    DateTime dt;
                    if (DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out dt))
                    {
                        if (dt.Hour < 12)
                        {
                            lastAnalyseTime_Alarm_Btn = lastAnalyseTime_Alarm_Btn.Substring(0, 11) + "00:00:00";
                        }
                        else
                        {
                            lastAnalyseTime_Alarm_Btn = lastAnalyseTime_Alarm_Btn.Substring(0, 11) + "12:00:00";
                        }
                    }
                }

                // 判断界面选择，整班次模式还是分段模式
                if (string.IsNullOrEmpty(MainForm.alarm_Btn_Model) || MainForm.alarm_Btn_Model == "整班次模式")
                {
                    lastAnalyseTime_Alarm_Btn = "0";
                    isNewVersion = false;
                }
                else
                {
                    isNewVersion = true;
                }
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"获取数据库配置参数异常Error：{ex.ToString()}");
                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  获取数据库配置参数失败，请检查数据库配置！");
                //DataReturnEvent?.Invoke("label3", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  获取数据库配置参数失败，请检查数据库配置！");

            }
        }

        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frm_main_Load(object sender, EventArgs e)
        {
            try
            {
                GetParamConfig(); // 获取数据库配置参数

                httpClient16 = new HttpClient();
                httpClient32 = new HttpClient();
                httpClientTimer = new HttpClient();


                this.Text = this.Text + "_" + line_id;

                //client.Password = "slac1028";//密码
                //client.Db = 2; //选择第1个数据库，0-15
                if (isCluster == "1")
                {
                    CHpasswd = "slac1028#";
                    CHtable_name = "_all"; ////分布式表加all
                }

                thread10State = true;
                thread10 = new Thread(threadStart10);
                thread10.IsBackground = true;
                thread10.Start();

                timer1.Enabled = true; // 定时器启动
            }
            catch (Exception)
            {

                throw;
            }

        }

        /// <summary>
        /// 统计报警：开始处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button2_Click(object sender, EventArgs e)
        {
            getTodayAndShift();
            AddListStr("开始处理！ " + DateTime.Now.ToString());

            isStartExec10 = true;
        }

        /// <summary>
        /// 获取界面选择日期时间（分白夜班 各12小时）
        /// 根据这个时间段查询click house数据库数据
        /// </summary>
        private void getTodayAndShift()
        {
            try
            {
                if (!string.IsNullOrEmpty(lastAnalyseTime_Alarm_Btn))
                {
                    #region 分段模式下，根据上次分析时间，计算本次分析时间、班次，以及更新界面显示
                    DateTime dt;
                    if (DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out dt))
                    {
                        isNewVersion = true; // 默认能获取到lastAnalyseTime_Alarm_Btn，是新模式（分段）

                        workdate = dt.Date.ToString("yyyy-MM-dd");
                        startTime = dt.ToString("yyyy-MM-dd HH:mm:ss");              // 开始时间
                        endTime = dt.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss"); // 结束时间                        

                        // 班次
                        if (dt.Hour < 12)
                        {
                            workshift = "1";
                        }
                        else
                        {
                            workshift = "0";
                        }

                        UpdateMainFormSettingsInfoEvent?.Invoke($"按钮报警分析时间", dt);
                        UpdateMainFormSettingsInfoEvent?.Invoke($"按钮报警分析班次", workshift == "1" ? "白班" : "晚班");

                        #endregion
                    }
                    else
                    {
                        //LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"获取数据库配置参数 lastAnalyseTime_Alarm_Btn 错误：{lastAnalyseTime_Alarm_Btn}不是时间格式！");
                        //AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  获取数据库配置参数失败，{lastAnalyseTime_Alarm_Btn}不是时间格式！");

                        #region 查询整个班次模式下，获取界面选择日期时间，班次，计算本次分析时间、班次
                        isNewVersion = false;

                        // lastAnalyseTime_Alarm_Btn参数不是时间格式，则默认按照之前版本的逻辑（直接查询一个班次）获取时间
                        workdate = Convert.ToDateTime(FetchMainFormSettingsInfoEvent?.Invoke("按钮报警分析时间")).Date.ToString("yyyy-MM-dd");
                        //workdate = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
                        string shift = FetchMainFormSettingsInfoEvent?.Invoke("报警分析班次").ToString();

                        if (shift == "白班")
                        {
                            startTime = workdate + " 00:00:00";
                            endTime = workdate + " 12:00:00";
                            workshift = "1";
                        }
                        else
                        {
                            startTime = workdate + " 12:00:00";
                            endTime = Convert.ToDateTime(workdate).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
                            workshift = "0";
                        }

                        #endregion
                    }

                }
                else
                {
                    LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"获取界面日期、班次失败：{lastAnalyseTime_Alarm_Btn}");
                }


                #region 原代码注释
                //workdate = Convert.ToDateTime(FetchMainFormSettingsInfoEvent?.Invoke("报警分析时间")).Date.ToString("yyyy-MM-dd");
                ////workdate = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
                //string shift = FetchMainFormSettingsInfoEvent?.Invoke("报警分析班次").ToString();
                //if (shift == "白班" /*comboBox1.Text == "白班"*/)
                //{
                //    startTime = workdate + " 00:00:00";
                //    endTime = workdate + " 12:00:00";
                //    workshift = "1";
                //}
                //else
                //{
                //    startTime = workdate + " 12:00:00";
                //    endTime = Convert.ToDateTime(workdate).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
                //    workshift = "0";
                //} 
                #endregion

            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"获取界面日期、班次异常：{ex.Message}");
                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 获取界面日期、班次异常");
            }
        }


        /// <summary>
        /// 计时器事件：每隔一秒触发一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (TimerStatus)
                {
                    string type = "报警分析";
                    DateTime nowtime = DateTime.Now;
                    if (!isAnalyzing && (
                       nowtime.ToString("mm:ss") == "01:00" ||
                       //nowtime.ToString("mm:ss") == "16:00" || //nowtime.ToString("mm:ss") == "06:00" || nowtime.ToString("mm:ss") == "16:00" ||
                       nowtime.ToString("mm:ss") == "31:00"
                      // nowtime.ToString("mm:ss") == "46:00"   // || nowtime.ToString("mm:ss") == "26:00" || nowtime.ToString("mm:ss") == "36:00" ||   nowtime.ToString("mm:ss") == "41:00" ||
                      // nowtime.ToString("mm:ss") == "51:00"  //  || nowtime.ToString("mm:ss") == "46:00" || nowtime.ToString("mm:ss") == "56:00"
                      ))
                    {
                        if (nowtime < Convert.ToDateTime("08:30:00"))
                        {
                            //dateTimePicker1.Value = DateTime.Now.AddDays(-1);
                            //comboBox1.Text = "晚班";
                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now.AddDays(-1));
                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                            button2_Click(sender, e);
                        }
                        else if (nowtime > Convert.ToDateTime("20:30:00"))
                        {
                            //nowtime.ToString("HH:mm:ss") == "20:41:00" || nowtime.ToString("HH:mm:ss") == "20:51:00"
                            //dateTimePicker1.Value = DateTime.Now;
                            //comboBox1.Text = "晚班";
                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now);
                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                            button2_Click(sender, e);
                        }
                        else
                        {
                            //dateTimePicker1.Value = DateTime.Now;
                            //comboBox1.Text = "白班";
                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now);
                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "白班");
                            button2_Click(sender, e);
                        }
                    }
                    else
                    {
                        // 新版本且不处于分析状态时，判断数据库最新报警信息时间是否大于上次分析时间+30分钟，大于则执行分析
                        if (isNewVersion && !isAnalyzing)
                        {
                            // 查询数据库最新报警信息的时间（eventtime）
                            string sqlString = $"SELECT eventtime FROM {companyNum}.{line_id}{CHtable_name}  ORDER BY eventtime DESC LIMIT 50 ";
                            string newEventtime = PostResponse(httpClientTimer, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", sqlString.ToString());

                            List<string> Eventtime = newEventtime.Trim().Split('\n').ToList();

                            DateTime newDbTime;// click house 数据库最新报警信息的时间
                            DateTime lastTime; // 上一次分析时间戳时间

                            bool isStartExecl = false;

                            if (Eventtime.Count == 50)
                            {
                                // 遍历最新的五十条报警数据，如果存在一条报警时间小于上次分析时间+30分钟，则不执行分析
                                foreach (var item in Eventtime)
                                {
                                    if (DateTime.TryParse(item.Trim(), out newDbTime) && DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out lastTime))
                                    {
                                        if (lastTime.AddMinutes(30) > newDbTime)
                                        {
                                            isStartExecl = false;
                                            break;
                                        }
                                        else { isStartExecl = true; }
                                    }
                                }
                            }

                            if (isStartExecl)
                            {
                                //button2_Click(sender, e);
                                getTodayAndShift();
                                isStartExec10 = true;
                            }

                            Eventtime.Clear();

                            //if (DateTime.TryParse(newEventtime.Replace("\n", ""), out newDbTime) && DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out lastTime))
                            //{
                            //    if (lastTime.AddMinutes(30) < newDbTime)
                            //    {

                            //        //button2_Click(sender, e);
                            //        getTodayAndShift();
                            //        isStartExec10 = true;

                            //    }
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn_error", $"定时器异常：{ex.Message}\r\n");
            }
        }

        /// <summary>
        /// 线程处理数据10
        /// </summary>
        private void threadStart10()
        {
            while (thread10State)
            {
                // 根据配置的device_16bit，分两种解析方式，查询clickhouse数据库 deviceid、msgid ，解析报警信息
                if (isStartExec10)
                {
                    isAnalyzing = true;     // 分析状态
                    isRightAnalysis = true; // 每一次分析，初始化为true，如果手动关闭，则置为false，重新分析
                    isStartExec10 = false;  // 线程执行一次后，置为false，等待下次执行
                    try
                    {
                        Stopwatch stopwatch = new Stopwatch(); // 耗费总时间
                        stopwatch.Start();

                        LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"开始分析{startTime}~{endTime}时间段内数据");

                        task16 = Task.Run(() =>
                        {
                            try
                            {
                                // chizhoujz.line_id + CHtable_name
                                // 去重查询一个班次的 device_id,msg_id 组合，且 50< msg_id <150，设备号为 16位设备

                                //string ssql_12 = "select distinct device_id,msg_id FROM " + companyNum + "." + line_id + CHtable_name + " WHERE eventtime >='" + startTime
                                //      + "' and eventtime<'" + endTime + "' and device_id in(" + device_16bit + ") and ((msg_id >0 and msg_id <16) or (msg_id >=150 and msg_id <180)) order by device_id,msg_id ";

                                string ssql_12 = $"select distinct device_id,msg_id FROM {companyNum}.{line_id}{CHtable_name} " +
                                                    $"WHERE eventtime >='{startTime}' and eventtime<'{endTime}' " +
                                                    $"and device_id in({device_16bit}) and msg_id >=150 and msg_id <180 " +
                                                    $"order by device_id,msg_id ";

                                string msgIDlist = string.Empty;

                                msgIDlist = PostResponse(httpClient16, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", ssql_12.ToString());

                                string[] alist = msgIDlist.Split(Convert.ToChar("\n"));

                                for (int x = 0; x < alist.Count() - 1; x++)
                                {
                                    if (cts.Token.IsCancellationRequested) // 取消线程任务
                                    {
                                        isRightAnalysis = false; // 设置异常标志,这个时间段需要重新分析(手动关闭也需要重新分析)
                                        break;
                                    }
                                    string[] device_msg_list = alist[x].Split(Convert.ToChar("\t"));
                                    string deviceid = device_msg_list[0];
                                    string msgid = device_msg_list[1];

                                    // 测试
                                    //if (deviceid == "12" && Convert.ToInt32(msgid) >60)
                                    //{
                                    //    break;
                                    //}

                                    //LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"分析设备号{deviceid},msgid: {msgid} 的数据");

                                    getErrorValueFromCH_16bit(line_id, deviceid, deviceid, msgid, cts.Token);  //个别机器的报警信息只计算16位

                                    //
                                    //string[] list_16bit = device_16bit.Split(Convert.ToChar(","));
                                    //if (list_16bit.Contains(deviceid))
                                    //{
                                    //    getErrorValueFromCH_16bit(line_id, deviceid, deviceid, msgid);  //个别机器的报警信息只计算16位
                                    //}
                                    //else
                                    //{
                                    //    getErrorValueFromCH(line_id, deviceid, deviceid, msgid);  //扬州会出现设备ID与采集ID不一致的情况
                                    //}
                                }
                            }
                            catch (Exception ex)
                            {
                                isRightAnalysis = false;
                                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Task_Alarm_Btn", $"分析{startTime}~{endTime}时间段内数据，异步任务 task16 异常：{ex.ToString()}\r\n");
                            }
                        }, cts.Token);

                        task32 = Task.Run(() =>
                        {
                            try
                            {
                                // 去重查询一个班次的 device_id,msg_id 组合，且 50< msg_id <100，设备号为 32位设备

                                //string ssql_other = "select distinct device_id,msg_id FROM " + companyNum + "." + line_id + CHtable_name + " WHERE eventtime >='" + startTime
                                //                         + "' and eventtime<'" + endTime + "'  and device_id not in(" + device_16bit + ") and ((msg_id >0 and msg_id <16) or (msg_id >=150 and msg_id <180)) order by device_id,msg_id ";

                                string ssql_other = $"select distinct device_id,msg_id FROM {companyNum}.{line_id}{CHtable_name} " +
                                                    $"WHERE eventtime >='{startTime}' and eventtime<'{endTime}' " +
                                                    $"and device_id not in({device_16bit}) and msg_id >=150 and msg_id <180 " +
                                                    $"order by device_id,msg_id ";

                                string msgIDlist_other = string.Empty;

                                msgIDlist_other = PostResponse(httpClient32, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", ssql_other.ToString());

                                string[] alist_other = msgIDlist_other.Split(Convert.ToChar("\n"));

                                for (int x = 0; x < alist_other.Count() - 1; x++)
                                {
                                    if (cts.Token.IsCancellationRequested)
                                    {
                                        isRightAnalysis = false; // 设置异常标志,这个时间段需要重新分析(手动关闭也需要重新分析)
                                        break;
                                    }

                                    string[] device_msg_list_other = alist_other[x].Split(Convert.ToChar("\t"));
                                    string deviceid = device_msg_list_other[0];
                                    string msgid = device_msg_list_other[1];
                                    string[] list_16bit = device_16bit.Split(Convert.ToChar(","));

                                    // 测试
                                    //if (deviceid != "12")
                                    //{
                                    //    break;
                                    //}

                                    //LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"分析设备号{deviceid},msgid: {msgid} 的数据");

                                    getErrorValueFromCH(line_id, deviceid, deviceid, msgid, cts.Token);  //扬州会出现设备ID与采集ID不一致的情况
                                }

                            }
                            catch (Exception ex)
                            {
                                isRightAnalysis = false;
                                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Task_Alarm_Btn", $"分析{startTime}~{endTime}时间段内数据，异步任务 task32 异常：{ex.ToString()}\r\n");
                            }
                        }, cts.Token);

                        #region 注释
                        /*
                                       //SetShiftVlaue(nowtime);
                                       for (int m = 50; m <= 54; m++)
                                       {
                                           getErrorValueFromCH(line_id,"10", "10", m.ToString());
                                       }
                                       AddListStr("处理完成！10 ");
                                       //转换台11
                                       for (int m = 50; m <= 63; m++)
                                       {
                                           getErrorValueFromCH(line_id, "11", "11", m.ToString());
                                       }
                                       AddListStr("处理完成！11 ");
                                       //转换台12
                                       for (int m = 50; m <= 55; m++)
                                       {
                                           getErrorValueFromCH(line_id, "12", "12", m.ToString());
                                       }
                                       AddListStr("处理完成！12 ");

                                       for (int m = 50; m <= 59; m++)
                                       {
                                           getErrorValueFromCH(line_id, "13", "13", m.ToString());
                                       }
                                       AddListStr("处理完成！13 ");
                                       //转换台12
                                       for (int m = 50; m <= 63; m++)
                                       {
                                           getErrorValueFromCH(line_id, "14", "14", m.ToString());
                                       }
                                       AddListStr("处理完成！14 ");
                                       //转换台12
                                       for (int m = 50; m <= 53; m++)
                                       {
                                           getErrorValueFromCH(line_id, "15", "15", m.ToString());
                                       }
                                       AddListStr("处理完成！15 ");
                                       //转换台12
                                       for (int m = 50; m <= 63; m++)
                                       {
                                           getErrorValueFromCH(line_id, "16", "16", m.ToString());
                                       }
                                       AddListStr("处理完成！16 ");
                                       //转换台12
                                       for (int m = 50; m <= 54; m++)
                                       {
                                           getErrorValueFromCH(line_id, "17", "17", m.ToString());
                                       }
                                       AddListStr("处理完成！17 ");
                                       //转换台12
                                       for (int m = 50; m <= 63; m++)
                                       {
                                           getErrorValueFromCH(line_id, "18", "18", m.ToString());
                                       }
                                       AddListStr("处理完成！18 ");

                                       for (int m = 50; m <= 56; m++)
                                       {
                                           getErrorValueFromCH(line_id, "19", "19", m.ToString());
                                       }
                                       AddListStr("处理完成！19 ");

                                       for (int m = 50; m <= 57; m++)
                                       {
                                           getErrorValueFromCH(line_id, "20", "20", m.ToString());
                                       }
                                       AddListStr("处理完成！20 ");
                                       */
                        #endregion

                        Task.WaitAll(task16, task32);

                        stopwatch.Stop();
                        LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"分析{startTime}~{endTime}时间段内数据，耗费时间：{stopwatch.Elapsed.TotalSeconds}");


                        #region 两种分析模式下，结束一次分析后，执行不同处理逻辑
                        // 新版本(分段模式)，正常分析流程，更新上次分析时间戳
                        if (isRightAnalysis)
                        {
                            isInitRedis = false;
                            try
                            {
                                if (isNewVersion)
                                {
                                    // 查询数据库最新报警信息的时间（eventtime字段）
                                    string sqlString = $"SELECT eventtime FROM {companyNum}.{line_id}{CHtable_name}  ORDER BY eventtime DESC LIMIT 50 ";

                                    string newEventtime = string.Empty;

                                    using (HttpClient httpClient = new HttpClient())
                                    {
                                        newEventtime = PostResponse(httpClient, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", sqlString.ToString());
                                    }

                                    List<string> Eventtime = newEventtime.Trim().Split('\n').ToList();

                                    DateTime newDbTime; // click house 数据库最新报警信息的时间
                                    DateTime lastTime;  // 上一次分析时间戳时间

                                    // 判断是否需要更新上次分析时间戳，继续下一个时间段分析
                                    bool isContinueNext = false;
                                    if (Eventtime.Count == 50)
                                    {
                                        foreach (var item in Eventtime)
                                        {
                                            if (DateTime.TryParse(item.Trim(), out newDbTime) && DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out lastTime))
                                            {
                                                if (lastTime.AddMinutes(30) > newDbTime)
                                                {
                                                    isContinueNext = false;
                                                    break;
                                                }
                                                else { isContinueNext = true; }
                                            }
                                        }
                                    }
                                    
                                    if (isContinueNext)
                                    {
                                        // 一次分析完成，更新上次分析时间戳（加半小时）
                                        string newlastAnalyseTime_Alarm_Btn = endTime;
                                        DBOper.Init();
                                        DBOper db = new DBOper();
                                        int result = db.UpdateLastAnalyseTime(newlastAnalyseTime_Alarm_Btn, "lastAnalyseTime_Alarm_Btn");
                                        if (result == 1)
                                        {
                                            DateTime.UtcNow.ToString();
                                            AddListStr($"UTC时间段 {startTime}-{endTime} 内报警分析处理完成！ " + DateTime.Now.ToString() + "\r\n");
                                            LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"更新上次分析时间戳成功：{newlastAnalyseTime_Alarm_Btn}\r\n");

                                            // 数据库更新后，再更新内存中的时间戳 lastAnalyseTime_Alarm_Btn
                                            DBSystemConfig dbSystemConfig = new DBSystemConfig();
                                            List<DBSystemConfig> list = db.QueryListCondition(dbSystemConfig, "Name = 'lastAnalyseTime_Alarm_Btn'");
                                            lastAnalyseTime_Alarm_Btn = list[0].Value;
                                        }
                                        else
                                        {
                                            LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"更新上次分析时间戳失败：{newlastAnalyseTime_Alarm_Btn}\r\n");
                                            AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  更新上次分析时间戳失败，请检查数据库配置！");
                                        }
                                    }

                                    #region 注释
                                    //if (DateTime.TryParse(newEventtime.Replace("\n", ""), out newDbTime) && DateTime.TryParse(lastAnalyseTime_Alarm_Btn, out lastTime))
                                    //{
                                    //    // 判断是否需要更新上次分析时间戳
                                    //    if (lastTime.AddMinutes(30) < newDbTime)
                                    //    {
                                    //        // 一次分析完成，更新上次分析时间戳（加半小时）
                                    //        string newlastAnalyseTime_Alarm_Btn = endTime;
                                    //        DBOper.Init();
                                    //        DBOper db = new DBOper();
                                    //        int result = db.UpdateLastAnalyseTime(newlastAnalyseTime_Alarm_Btn, "lastAnalyseTime_Alarm_Btn");
                                    //        if (result == 1)
                                    //        {
                                    //            DateTime.UtcNow.ToString();
                                    //            AddListStr($"UTC时间段 {startTime}-{endTime} 内报警分析处理完成！ " + DateTime.Now.ToString() + "\r\n");
                                    //            LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"更新上次分析时间戳成功：{newlastAnalyseTime_Alarm_Btn}\r\n");

                                    //            // 数据库更新后，再更新内存中的时间戳 lastAnalyseTime_Alarm_Btn
                                    //            DBSystemConfig dbSystemConfig = new DBSystemConfig();
                                    //            List<DBSystemConfig> list = db.QueryListCondition(dbSystemConfig, "Name = 'lastAnalyseTime_Alarm_Btn'");
                                    //            lastAnalyseTime_Alarm_Btn = list[0].Value;
                                    //        }
                                    //        else
                                    //        {
                                    //            LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"更新上次分析时间戳失败：{newlastAnalyseTime_Alarm_Btn}\r\n");
                                    //            AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  更新上次分析时间戳失败，请检查数据库配置！");
                                    //        }

                                    //    }
                                    //} 

                                    #endregion
                                    isStartExec10 = false;
                                    isAnalyzing = false;
                                }
                                else
                                {
                                    // 旧版本(查询一整个班次数据分析),结束后判断若不是当天，则继续查询分析下一个班次数据
                                    AddListStr($"{workdate} & {workshift} ------ 报警分析处理完成 @ " + DateTime.Now.ToString() + " ------\r\n");
                                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"{workdate} & {workshift} ------ 报警分析处理完成 @ " + DateTime.Now.ToString() + " ------\r\n");
                                    // isAnalyzing = false;                            

                                    DateTime ds, de;
                                    DateTime.TryParse(startTime, out ds);
                                    DateTime.TryParse(endTime, out de);
                                    if (de.Day < DateTime.Now.Day)
                                    {
                                        LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"前startTime：{startTime}，前endTime：{endTime} 后startTime：{ds.AddHours(12).ToString()}，后endTime：{de.AddHours(12).ToString()}");

                                        startTime = ds.AddHours(12).ToString();
                                        endTime = de.AddHours(12).ToString();

                                        workdate = ds.AddHours(12).ToString("yyyy-MM-dd");

                                        workshift = (workshift == "0") ? "1" : "0";

                                        if (workshift == "0")
                                        {
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"按钮报警分析时间", ds.AddHours(12));
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"按钮报警分析班次", "晚班");
                                        }
                                        else
                                        {
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"按钮报警分析时间", ds.AddHours(12));
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"按钮报警分析班次", "白班");
                                        }

                                        isStartExec10 = true;
                                        isAnalyzing = true;
                                    }
                                    else
                                    {
                                        isAnalyzing = false;
                                    }


                                }
                            }
                            catch (Exception ex)
                            {
                                isAnalyzing = false;
                                isInitRedis = true;
                                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"分析{startTime}~{endTime}时间段内数据，存在异常：{ex.Message}");
                            }
                        }
                        else
                        {
                            isAnalyzing = false;
                            isInitRedis = true; // 一个时间段结束，若需要重新分析，则重置该时间段的 Redis 键值

                            LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_Btn", $"分析{startTime}~{endTime}时间段内数据，分析时存在报错或手动停止，需要重新分析");
                        }


                        #endregion
                    }
                    catch (ThreadAbortException)
                    {
                        // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
                    }
                    catch (Exception ex)
                    {
                        LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Task_Alarm_Btn", $"{ex.ToString()}\r\n");
                        // System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd") + "_error10.log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                    }
                }
                else
                {
                    //没有任务，休息0.1秒
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// 解析报警信息（解析32位数据）
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="FromdeviceID"></param>
        /// <param name="msgID"></param>
        private void getErrorValueFromCH(string lineID, string deviceID, string FromdeviceID, string msgID, CancellationToken token)
        {
            //统计报警信息的msg，哪些发生了变化

            Stopwatch watch = new Stopwatch();
            watch.Start();
            try
            {
                //测试
                Stopwatch sc = new Stopwatch();
                sc.Start();

                RedisClient client = new RedisClient(RedisServer, RedisPort);
                client.Password = RedisPasswd;    //密码 "slac1028";
                client.Db = Convert.ToInt32(lineID.Substring(lineID.Length - 2)); //根据线体号选择数据库 0-15

                StringBuilder SqlString = new StringBuilder("");
                string sqlhead = "insert into " + lineID + "_alarm_btn (workdate,workshift,line_id,device_id,msg_id,pv,lpv,diffv,pt,lpt,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                string ssql = " SELECT eventtime,bitXor(`data` , 1768515945-device_id*msg_id) as msg_bit from " + companyNum + "." + lineID + CHtable_name + " l where eventtime >= '" + startTime + "' and eventtime< '" + endTime
                 + "' and device_id =" + FromdeviceID + " and msg_id =" + msgID + " order by eventtime  ";

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(httpClient32, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", ssql.ToString());

                if (strResult != "")
                {
                    strResult = strResult.Remove(strResult.Count() - 1, 1);

                    string[] strArray = strResult.Split('\n');

                    #region 读取redis存储的上一次更新的msgID点位的每一位数据，拼接成32位原始数据，若不存在，初始化为 0
                    int initValue = 0;
                    string d = Convert.ToString(initValue, 2).PadLeft(32, '0');

                    List<string> listLastKeyValue = new List<string>(new string[32]); // 存储redis读取的每一位的值，拼接成32位原始数据

                    DateTime.TryParse(startTime, out DateTime dt);
                    string sTime = dt.ToString("HH:mm:ss");

                    // 测试
                    //if (sTime.Equals("06:30:00") && FromdeviceID == "15" && msgID == "50")
                    //{

                    //}

                    // 读取redis中该msgID点位的每一位数据（共32个），反向拼接成32位原始数据，若其中哪一位不存在，初始化为 0
                    for (int j = 0; j < d.Length; j++)
                    {
                        int xm = 31 - j;

                        string lastKeyValue = client.Get<string>("k" + deviceID + "." + msgID + "." + xm.ToString());

                        if (sTime.Equals("00:00:00") || sTime.Equals("12:00:00")) // 每个班次开始时，将redis存储的值都重置为 0
                        {
                            lastKeyValue = string.Empty;
                        }

                        // 如果Redis中不存在该key，则设置初始值为 0
                        if (!string.IsNullOrEmpty(lastKeyValue) && lastKeyValue.Contains("@") && !isInitRedis)
                        {
                            listLastKeyValue[j] = lastKeyValue.Split('@')[0];
                        }
                        else
                        {
                            listLastKeyValue[j] = "0";

                            int result = (initValue >> xm) & 1;
                            client.Set("k" + deviceID + "." + msgID + "." + xm.ToString(), result.ToString() + "@" + startTime);
                        }
                    }

                    initValue = Convert.ToInt32(string.Join("", listLastKeyValue), 2);

                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Redis", $"在{startTime}~{endTime}时间段内，deviceID：{deviceID},msgID：{msgID} 上一次值：{initValue}");
                    #endregion

                    lastValue32 = initValue;

                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", $"Alarm_time{deviceID}", $"分析 {startTime}~{endTime} 时间段内，设备号{deviceID}，msgID {msgID} 查询数据数量：{strArray.Count()}");

                    sc.Stop();
                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", $"Alarm_time{deviceID}", $"设备号{deviceID}，msgID {msgID} 循环前查询耗时：{sc.ElapsedMilliseconds}");


                    //测试
                    Stopwatch se = new Stopwatch();
                    se.Start();

                    int valueChange = 0; // 统计报警信息msgID变化次数

                    for (int i = 0; i < strArray.Count(); i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", "32位解析break跳出");
                            break;
                        }

                        string[] strBit = strArray[i].Split('\t');
                        DateTime nowTime = Convert.ToDateTime(strBit[0]);
                        int nowValue = Convert.ToInt32(strBit[1]);


                        if (nowValue != lastValue32)
                        {
                            valueChange++;

                            //if (deviceID.Equals("18"))
                            //{
                            //    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_value", $"设备号{deviceID}{msgID}:nowValue:{nowValue},lastValue:{lastValue}");
                            //}

                            int xorValue = nowValue ^ lastValue32;
                            string binStr = Convert.ToString(xorValue, 2).PadLeft(32, '0');

                            #region 删除看板服务器数据库 _bit表数据
                            string ssqlDel = string.Empty; // 删除看板服务器数据库 _bit表数据
                            if (isNewVersion)
                            {
                                // 分段查询处理模式，根据时间段更新看板服务器数据库 _bit表
                                ssqlDel = $"delete from {lineID}_alarm_btn where pt >= '{startTime}' and pt < '{endTime}' and workdate = '{workdate}' and workshift = '{workshift}' and line_id = '{lineID}' and device_id = '{deviceID}' and msg_id like '{msgID}%'";
                            }
                            else
                            {
                                // 班次查询处理模式，根据班次更新看板服务器数据库 _bit表
                                ssqlDel = "delete from " + lineID + "_alarm_btn where workdate='" + workdate + "' and workshift='" + workshift + "'  and line_id='" + lineID + "' and device_id='" + deviceID + "' and msg_id like '" + msgID + "%'";
                            }

                            int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                            #endregion

                            #region 拆位解析，更新Redis，拼接Sql语句,插入看板服务器数据库 _bit表
                            for (int j = 0; j < binStr.Length; j++)
                            {

                                string msgBit = j.ToString();
                                //string ssqlDel = "delete from line_bit where workdate='" + workdate + "' and workshift='" + workshift + "' and msg_id='" + msgID + "." + msgBit + "' and line_id='" + line_id + "' and device_id='" + deviceID + "'"; ;
                                //int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                                bool gg = binStr[j].ToString() == "1" ? true : false;
                                if (gg)
                                {

                                    int xn = 31 - j;
                                    int result = (nowValue >> xn) & 1; // 按位与操作（即判断nowValue右移后，最低位结果是否为1，为1则result为1，否则为0）

                                    string lastKeyValue = client.Get<string>("k" + deviceID + "." + msgID + "." + xn.ToString());

                                    string[] lastStrBit = lastKeyValue.Split('@');

                                    //string timeValue = lastStrBit[0].Substring(lastStrBit[0].Length - 8, 8);

                                    if (lastStrBit[0] != result.ToString())
                                    {
                                        client.Set("k" + deviceID + "." + msgID + "." + xn.ToString(), result.ToString() + "@" + strBit[0]);

                                        //if (timeValue.Equals("00:00:00") || timeValue.Equals("00:30:00"))
                                        //{

                                        //}

                                        //System.IO.File.AppendAllText(DateTime.Now.ToString("yyyyMMdd_") + ".log", "k" + msgID + "." + x.ToString() + " ,last: " + lastStrBit[0] + ",lastTime=" + lastStrBit[1] + ",now: " + result.ToString() + ",NowTime=" + strBit[0] + "\n");

                                        //SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "." + xn.ToString() + "','" + result.ToString() + "','" + lastStrBit[0] + "','" + Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastStrBit[1])).TotalSeconds).ToString() + "','" + strBit[0] + "','" + lastStrBit[1] + "',now()),");

                                        SqlString.Append($"('{workdate}','{workshift}','{lineID}','{deviceID}','{msgID}.{xn.ToString()}','{result}','{lastStrBit[0]}'," +
                                                         $"'{Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastStrBit[1])).TotalSeconds)}'," +
                                                         $"'{strBit[0]}','{lastStrBit[1]}',now()),");

                                        //msg_id,pv,lpv,diffv,pt,lpt,indate
                                        Lcount++;
                                    }
                                }

                                if (Lcount > 1000)
                                {
                                    //LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Stats32", "32位解析：每一千条数据上传数据库开始");
                                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                                    SqlString = new StringBuilder(sqlhead);
                                    Lcount = 0;
                                    //LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Stats32", "32位解析：每一千条数据上传数据库完成");

                                }
                            }

                            #endregion

                            //记录上次状态变化的时间
                            lastTime = nowTime;
                            lastValue32 = nowValue;
                        }
                    }

                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", $"Alarm_time{deviceID}", $"设备号{deviceID}，msgID {msgID} 报警变化次数：{valueChange}");

                    se.Stop();
                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", $"Alarm_time{deviceID}", $"设备号{deviceID}，msgID {msgID} 循环整体耗时：{se.ElapsedMilliseconds}\r\n");

                }
                watch.Stop();

                // 不满1000条的数据，最后再处理
                if (Lcount > 0)
                {

                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    ////System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ssql2.ToString() + DateTime.Now.ToString() + "\r\n");
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    // System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd") + ".log", workdate + " & " + workshift + " & " + deviceID + "." + msgID + "报警信息处理完成！" + " @ " + DateTime.Now.ToString() + "\r\n");

                    //LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Stats32", "32位解析：不满一千调数据上传数据库完成");
                }

                AddListStr(workdate + " & " + workshift + " & " + deviceID + "." + msgID + " 报警信息处理完成！" + " @ " + DateTime.Now.ToString() + " 耗时： " + watch.ElapsedMilliseconds.ToString());

            }
            catch (ThreadAbortException) // 线程中止异常
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                isRightAnalysis = false; // 设置异常标志,这个时间段需要重新分析
                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_error", $"deviceID:{deviceID}，msgID:{msgID} 异常：{ex.ToString()}\r\n");

                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 出错了！请查看 Alarm_error日志");
            }
        }

        /// <summary>
        /// 解析报警信息（解析16位数据）
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="FromdeviceID"></param>
        /// <param name="msgID"></param>
        private void getErrorValueFromCH_16bit(string lineID, string deviceID, string FromdeviceID, string msgID, CancellationToken token)
        {
            //统计报警信息的msg，哪些发生了变化

            Stopwatch watch = new Stopwatch();
            watch.Start();
            try
            {
                RedisClient client = new RedisClient(RedisServer, RedisPort);
                client.Password = RedisPasswd;    //密码 //测试
                //client.Password = "slac1028";   //密码
                client.Db = Convert.ToInt32(lineID.Substring(lineID.Length - 2)); //根据线体号选择数据库 0-15

                // 初始化sql语句以及提交数据量（插入看板服务器mysql数据库）
                StringBuilder SqlString = new StringBuilder("");
                string sqlhead = "insert into " + lineID + "_alarm_btn (workdate,workshift,line_id,device_id,msg_id,pv,lpv,diffv,pt,lpt,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                // 获取clickhouse数据库报警信息
                /// 根据device_id，msg_id 查询一个班次内的 eventtime 和 msg_bit（运算得出）
                /// data 和 （1768515945-device_id*msg_id）的值 ，使用 bitXor 操作进行 按位异或 运算，得到 msg_bit
                string ssql = " SELECT eventtime,bitXor(`data` , 1768515945-device_id*msg_id) as msg_bit from " + companyNum + "." + lineID + CHtable_name + " l where eventtime >= '" + startTime + "' and eventtime< '" + endTime
                 + "' and device_id =" + FromdeviceID + " and msg_id =" + msgID + " order by eventtime  ";

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(httpClient16, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", ssql.ToString());

                if (strResult != "")
                {
                    strResult = strResult.Remove(strResult.Count() - 1, 1);

                    string[] strArray = strResult.Split('\n');

                    #region 读取redis存储的上一次更新的msgID点位的每一位数据，拼接成16位原始数据，若不存在，初始化为 0
                    // 初始化Redis存储的值，result 为0，后面比较后，若有变化，修改result的值 
                    int initValue = 0;
                    string d = Convert.ToString(initValue, 2).PadLeft(32, '0'); // 将十进制数转换为二进制数，并补齐32位（前面填充0来补齐）

                    List<string> listLastKeyValue = new List<string>(new string[16]); // 存储redis读取的每一位的值，拼接成16位原始数据

                    DateTime.TryParse(startTime, out DateTime dt);
                    string sTime = dt.ToString("HH:mm:ss");

                    // 测试
                    //if (sTime.Equals("01:30:00") && FromdeviceID == "12" && msgID == "104")
                    //{

                    //}

                    for (int j = 16; j < d.Length; j++)     //32位数据，从左向右，从16个位开始运算。排除掉16-31的高位，保留0-15低位
                    {
                        int xx = 31 - j;

                        string lastKeyValue = client.Get<string>("k" + deviceID + "." + msgID + "." + xx.ToString());

                        if (sTime.Equals("00:00:00") || sTime.Equals("12:00:00")) // 每个班次开始时，将redis存储的值都重置为 0
                        {
                            lastKeyValue = string.Empty;
                        }

                        // 如果Redis中不存在该key，则设置初始值为 0
                        if (!string.IsNullOrEmpty(lastKeyValue) && lastKeyValue.Contains("@") && !isInitRedis)
                        {
                            listLastKeyValue[j - 16] = lastKeyValue.Split('@')[0];
                        }
                        else
                        {
                            listLastKeyValue[j - 16] = "0";

                            int result = (initValue >> xx) & 1;
                            client.Set("k" + deviceID + "." + msgID + "." + xx.ToString(), result.ToString() + "@" + startTime);
                        }
                    }

                    initValue = Convert.ToInt32(string.Join("", listLastKeyValue), 2);
                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Redis", $"在{startTime}~{endTime}时间段内，deviceID：{deviceID},msgID：{msgID} 上一次值：{initValue}");

                    #endregion
                    lastValue16 = initValue;

                    int valueChange = 0; //记录报警msgID变化次数

                    for (int i = 0; i < strArray.Count(); i++)
                    {
                        if (token.IsCancellationRequested) { break; } //取消线程任务                        

                        string[] strBit = strArray[i].Split('\t');
                        DateTime nowTime = Convert.ToDateTime(strBit[0]);
                        int nowValue = Convert.ToInt32(strBit[1]);

                        string binaryString = Convert.ToString(nowValue, 2).PadLeft(32, '0');  // 转换为 32 位二进制字符串

                        // 截取后 16 位
                        string last16Bits = binaryString.Substring(binaryString.Length - 16);

                        // 将最后 16 位二进制字符串转换回整数
                        nowValue = Convert.ToInt32(last16Bits, 2);

                        if (nowValue != lastValue16)
                        {
                            valueChange++;

                            int xorValue = nowValue ^ lastValue16;                            // 按位异或（位相同为0，不同为1）
                            string binStr = Convert.ToString(xorValue, 2).PadLeft(32, '0'); // 将结果转换为二进制，再用 0，左侧补齐为32位

                            #region 删除看板服务器数据库 _bit表数据
                            ///注：是重新分析某时间段数据才会删除旧数据
                            string ssqlDel = string.Empty; // 删除看板服务器数据库 _bit表数据
                            if (isNewVersion)
                            {
                                // 分段查询处理模式，根据时间段更新看板服务器数据库 _bit表
                                ssqlDel = $"delete from {lineID}_alarm_btn where pt >= '{startTime}' and pt < '{endTime}' and workdate = '{workdate}' and workshift = '{workshift}' and line_id = '{lineID}' and device_id = '{deviceID}' and msg_id like '{msgID}%'";
                            }
                            else
                            {
                                // 班次查询处理模式，根据班次更新看板服务器数据库 _bit表
                                ssqlDel = "delete from " + lineID + "_alarm_btn where workdate='" + workdate + "' and workshift='" + workshift + "'  and line_id='" + lineID + "' and device_id='" + deviceID + "' and msg_id like '" + msgID + "%'";
                            }

                            int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                            #endregion

                            #region 拆位解析，更新Redis，拼接Sql语句,更新看板服务器数据库 _bit表
                            for (int j = 16; j < binStr.Length; j++)   //32位数据，从左向右，从16个位开始运算。排除掉16-31的高位，保留0-15低位。
                            {
                                string msgBit = j.ToString();

                                bool gg = binStr[j].ToString() == "1" ? true : false; // 找到32位中，变化的那一位（1）
                                if (gg)
                                {
                                    /// 即：将要检查的位右移到最低位，再获取最低位的值（为 1表示该位的值发生变化，0则不变）
                                    int xy = 31 - j;
                                    int result = (nowValue >> xy) & 1; // 右移 xy 位 ，再和 1 按位与运算（位都为 1 才是1，否则为0）获取最低位的值

                                    /// 再更新 Redis里面的对应键的值，并拼接到更新MySql数据库的 Sql语句中                                   
                                    string lastKeyValue = client.Get<string>("k" + deviceID + "." + msgID + "." + xy.ToString());
                                    string[] lastStrBit = lastKeyValue.Split('@');
                                    if (lastStrBit[0] != result.ToString())
                                    {
                                        client.Set("k" + deviceID + "." + msgID + "." + xy.ToString(), result.ToString() + "@" + strBit[0]);
                                        //System.IO.File.AppendAllText(DateTime.Now.ToString("yyyyMMdd_") + ".log", "k" + msgID + "." + x.ToString() + " ,last: " + lastStrBit[0] + ",lastTime=" + lastStrBit[1] + ",now: " + result.ToString() + ",NowTime=" + strBit[0] + "\n");

                                        SqlString.Append($"('{workdate}','{workshift}','{lineID}','{deviceID}','{msgID}.{xy}','{result}','{lastStrBit[0]}'," +
                                                         $"'{Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastStrBit[1])).TotalSeconds)}'," +
                                                         $"'{strBit[0]}','{lastStrBit[1]}',now()),");

                                        //SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "." + xy.ToString() + "','" + result.ToString() + "','" + lastStrBit[0] + "','" + Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastStrBit[1])).TotalSeconds).ToString() + "','" + strBit[0] + "','" + lastStrBit[1] + "',now()),");                                       
                                        //msg_id,pv,lpv,diffv,pt,lpt,indate
                                        Lcount++;
                                    }
                                }

                                // 报警信息存入看板服务器数据库，1000条存一次
                                if (Lcount > 1000)
                                {
                                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                                    SqlString = new StringBuilder(sqlhead);
                                    Lcount = 0;
                                }
                            }

                            #endregion

                            // 记录上次状态变化的时间
                            lastTime = nowTime;
                            lastValue16 = nowValue;
                        }
                    }

                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", $"Alarm_time{deviceID}", $"设备号{deviceID}，msgID {msgID} 报警变化次数：{valueChange}");

                }
                watch.Stop();

                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    ////System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ssql2.ToString() + DateTime.Now.ToString() + "\r\n");
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    // System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd") + ".log", workdate + " & " + workshift + " & " + deviceID + "." + msgID + "报警信息处理完成！" + " @ " + DateTime.Now.ToString() + "\r\n");
                }

                AddListStr(workdate + " & " + workshift + " & " + deviceID + "." + msgID + " 报警信息处理完成！" + " @ " + DateTime.Now.ToString() + " 耗时： " + watch.ElapsedMilliseconds.ToString());

            }
            catch (ThreadAbortException) // 线程中止异常
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                isRightAnalysis = false; // 设置异常标志,这个时间段需要重新分析                

                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Alarm_error", $"deviceID:{deviceID}，msgID:{msgID} 异常：{ex.ToString()}\r\n");

                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 出错了！请查看 Alarm_error日志");

            }
        }

        /// <summary>
        /// click house数据库 Post请求获取数据
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        private string PostResponse(HttpClient httpClient, string user, string password, string url, string postData)
        {
            AuthenticationHeaderValue authentication = new AuthenticationHeaderValue(
               "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")
               ));
            string result = string.Empty;
            HttpContent httpContent = new StringContent(postData);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";

            httpClient.DefaultRequestHeaders.Authorization = authentication;
            HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
            if (response.IsSuccessStatusCode)
            {
                Task<string> t = response.Content.ReadAsStringAsync();
                result = t.Result;
            }

            return result;
        }

        #region 注释
        //private string PostResponse(string user, string password, string url, string postData)
        //{
        //    AuthenticationHeaderValue authentication = new AuthenticationHeaderValue(
        //       "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")
        //       ));
        //    string result = string.Empty;
        //    HttpContent httpContent = new StringContent(postData);
        //    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        //    httpContent.Headers.ContentType.CharSet = "utf-8";
        //    using (HttpClient httpClient = new HttpClient())
        //    {
        //        httpClient.DefaultRequestHeaders.Authorization = authentication;
        //        HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
        //        if (response.IsSuccessStatusCode)
        //        {
        //            Task<string> t = response.Content.ReadAsStringAsync();
        //            result = t.Result;
        //        }
        //    }
        //    return result;
        //}
        #endregion

        /// <summary>
        /// 页面更新日志
        /// </summary>
        /// <param name="output"></param>
        private void AddListStr(string output)
        {
            if (!checkBox_Alarm.Checked)
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(delegate
                    {
                        if (listBox1.Items.Count > 500)
                        {
                            listBox1.Items.RemoveAt(0);
                        }
                        listBox1.Items.Add(output);
                        // 确保 ListBox 始终滚动到最新项
                        listBox1.TopIndex = listBox1.Items.Count - 1;
                    }));
                }
            }
        }

        /// <summary>
        /// 窗体大小改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frm_main_yzyl_bit_Resize(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;//窗体填充所在容器控件
            listBox1.Width = this.Width;

            //listBox1.Height = this.Height - (comboBox1.Height + comboBox1.Location.Y) - 20;
            //listBox1.Location = new Point(10, comboBox1.Location.Y + comboBox1.Height + 20);

            label1.Location = new Point(10, 10);
            checkBox_Alarm.Location = new Point(this.Width - 15 - checkBox_Alarm.Width, 10);

            listBox1.Height = this.Height - (checkBox_Alarm.Height + checkBox_Alarm.Location.Y);
            listBox1.Location = new Point(0, checkBox_Alarm.Location.Y + checkBox_Alarm.Height + 5);
        }

        /// <summary>
        /// 停止解析
        /// </summary>
        public async Task StopService()
        {
            try
            {
                LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"Task16、Task32开始关闭：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                cts.Cancel();

                thread10State = false; // 停止线程               
                isStartExec10 = false; // 停止执行

                // 关闭定时器，防止触发解析操作
                TimerStatus = false;
                if (timer1 != null && timer1.Enabled)
                {
                    this.timer1.Stop();
                    this.timer1.Dispose();
                    this.timer1 = null;
                }

                httpClientTimer.Dispose(); // 关闭释放定时器中的HttpClient
                httpClientTimer = null;

                if (thread10 != null && thread10.IsAlive)
                {
                    if (!thread10.Join(150))
                    {
                        thread10.Abort();// 若正在解析数据，直接终止线程
                    }
                }
                thread10 = null;
                if (task16 != null && task32 != null)
                {
                    if (task16.Status == TaskStatus.Running || task32.Status == TaskStatus.Running)
                    {
                        await Task.WhenAll(task16, task32);
                    }

                    LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"Task16、Task32结束关闭：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

                    if (task16.Status == TaskStatus.RanToCompletion && task32.Status == TaskStatus.RanToCompletion)
                    {
                        task16.Dispose();
                        task16 = null;
                        task32.Dispose();
                        task32 = null;
                        cts.Dispose();
                        cts = null;
                        LogConfig.Intence.WriteLog("RunLog\\Alarm_Btn", "Alarm_Btn", $"Task16、Task32释放资源成功：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n");
                    }
                }

                httpClient16.Dispose();
                httpClient16 = null;
                httpClient32.Dispose();
                httpClient32 = null;
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Alarm_Btn", "Task", $"停止解析异常：{ex.Message}");
            }
        }
    }
}
