using MySqlX.XDevAPI.Common;
using ServiceStack.Messaging.Rcon;
using ServiceStack.Redis;
using ServiceStack.Redis.Messaging;
using Slac_DataAnalysis.Common;
using Slac_DataAnalysis.DatabaseSql.DBModel;
using Slac_DataAnalysis.DatabaseSql.DBOper;
using Slac_DataAnalysis_Bit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Slac_DataAnalysis.FormPage
{

    /// <summary>
    /// 设备状态点位分析
    /// </summary>
    public partial class frm_main_device_state : UserControl
    {
        public static string type = string.Empty; // 功能模块类型
        public event Action<string, object> UpdateMainFormSettingsInfoEvent; // 更新主界面设置信息
        public event Func<string, object> FetchMainFormSettingsInfoEvent;    // 获取主界面设置信息

        private volatile string workdate = "";   // 工作日期
        private volatile string workshift = "1"; // 班次（白夜班）
        private volatile string startTime = "";  // 查询clickhouse数据库起始时间
        private volatile string endTime = "";    // 查询clickhouse数据库结束时间
        private StringBuilder sqlConditions;     // sql查询条件（msg_id的范围条件）

        private volatile HttpClient httpClient16;    // 16位设备HttpClient（用于查询点位数据）
        private volatile HttpClient httpClientTimer; // 定时器HttpClient（用于查询服务器最新数据是否需要开始分析)

        private Thread thread10 = null;            // 线程
        private bool thread10State;                // 线程状态
        private static bool isStartExec10 = false; // 是否开始执行线程
        private bool TimerStatus = true;           // 定时器状态
        public CancellationTokenSource cts = new CancellationTokenSource();  // 取消线程

        private volatile bool isNewVersion = false;        // 是否是最新版本(true:最新版本,采用标志位时间戳，分段分析  false:不是最新版本，直接分析整个班次) 在获取lastAnalyseTime_Device_State参数成功的情况下，默认是最新版本
        private volatile bool isAnalyzing;                 // 是否正在分析
        private volatile bool isRightAnalysis = true;      // 这次分析是否是正常分析流程（表示这个时间段需要重新分析）
        private volatile bool isInitRedis = false;         // 是否初始化Redis的键值（若当前时间段有报错，重新分析，需要重置Redis）        

        public DataSet msglist_rpt = new DataSet();// 查询msglist_report表，存储获取的所有设备信息

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

        private static string DeviceStatusRange; // 设备状态点位范围
        private volatile string lastAnalyseTime_Device_State; // 上一个时间段分析开始时间（设备状态）
        #endregion


        public frm_main_device_state()
        {
            InitializeComponent();

            this.Dock = DockStyle.Fill; // 窗体填充父容器
            this.DoubleBuffered = true; // 开启双缓冲，防止窗体闪烁
            this.button2.Visible = false;
        }

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
                //device_16bit = list.Find(e => e.Name.Trim() == "device_16bit").Value.Trim();

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

                // 设备状态点位范围，格式：(1250,1255)|(1350,1355)|(1450,1455)|(1550,1555)
                DeviceStatusRange = list.Find(e => e.Name.Trim() == "DeviceStatusRange").Value.Trim();

                string[] range = DeviceStatusRange.Split('|');

                // 构建sql条件(获取点位范围)
                sqlConditions = new StringBuilder();
                for (int i = 0; i < range.Length; i++)
                {
                    //range[1].Trim('(', ')'); // 去除括号
                    string[] startEnd = range[i].Trim('(', ')').Split(',');

                    if (startEnd.Length == 2)
                    {
                        sqlConditions.Append($"(msg_id >= {startEnd[0]} And msg_id <= {startEnd[1]})");

                        if (i < range.Length - 1)
                        {
                            sqlConditions.Append(" OR "); // 不是最后一个范围，尾部增加 OR
                        }
                    }
                }

                // 上一次按钮开关分析时间
                lastAnalyseTime_Device_State = list.Find(e => e.Name.Trim() == "lastAnalyseTime_Device_State").Value.Trim();

                // 判断界面选择，整班次模式还是分段模式
                if (string.IsNullOrEmpty(MainForm.device_State_Model) || MainForm.device_State_Model == "整班次模式")
                {
                    lastAnalyseTime_Device_State = "0";
                    isNewVersion = false;
                }
                else
                {
                    isNewVersion = true;
                }
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"获取数据库配置参数异常Error：{ex.ToString()}");
                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  获取数据库配置参数失败，请检查数据库配置！");
            }
        }

        /// <summary>
        /// 页面加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frm_main_device_status_Load(object sender, EventArgs e)
        {
            type = "设备状态分析";

            GetParamConfig(); // 获取数据库配置参数

            httpClient16 = new HttpClient();
            //httpClient32 = new HttpClient();
            httpClientTimer = new HttpClient();

            if (isCluster == "1")
            {
                CHpasswd = "slac1028#";
                CHtable_name = "_all"; ////分布式表加all
            }

            thread10State = true;
            thread10 = new Thread(threadStart10);
            thread10.IsBackground = true;
            thread10.Start();

            timer1.Enabled = true; // 启动定时器
        }

        private bool isProcessing = false; // 定时器是否正在处理        

        /// <summary>
        /// 计时器事件：每隔一秒触发一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (isProcessing || isAnalyzing)
                    return;  // 如果正在处理，跳过当前的事件

                isProcessing = true;  // 设置为正在处理

                // 其中执行查询操作，可能耗时，异步执行，避免阻塞UI线程
                await Task.Run(() =>
                {
                    if (TimerStatus)
                    {
                        //string type = "按钮开关分析";
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
                                UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now.AddDays(-1));
                                UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                                button2_Click(sender, e);
                            }
                            else if (nowtime > Convert.ToDateTime("20:30:00"))
                            {
                                UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now);
                                UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                                button2_Click(sender, e);
                            }
                            else
                            {
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
                                string sqlString = $"SELECT eventtime FROM {companyNum}.{line_id}{CHtable_name} " +
                                                   $"WHERE eventtime >= '{DateTime.Now.AddHours(-8).Date}' " +
                                                   $"And ({sqlConditions.ToString()}) ORDER BY eventtime DESC LIMIT 100 ";
                                string newEventtime = PostResponse(httpClientTimer, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", sqlString.ToString());

                                List<string> Eventtime = newEventtime.Trim().Split('\n').ToList();

                                DateTime newDbTime;// click house 数据库最新报警信息的时间
                                DateTime lastTime; // 上一次分析时间戳时间

                                bool isStartExecl = false;

                                if (Eventtime.Count == 100)
                                {
                                    // 遍历最新的一百条报警数据，如果存在一条报警时间小于上次分析时间+30分钟，则不执行分析
                                    foreach (var item in Eventtime)
                                    {
                                        if (DateTime.TryParse(item.Trim(), out newDbTime) && DateTime.TryParse(lastAnalyseTime_Device_State, out lastTime))
                                        {
                                            if (lastTime.AddMinutes(31) > newDbTime)
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

                                    LogConfig.Intence.WriteLog("RunLog", "Alarm", $"最新{Eventtime.Count}条数据，大于上一次分析时间31分钟，开始下一次分析");
                                }

                                Eventtime.Clear();

                            }
                        }
                    }
                });


                isProcessing = false;  // 定时器处理结束
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State_error", $"定时器异常：{ex.Message}\r\n");
            }
        }

        /// <summary>
        /// 线程处理数据10
        /// </summary>
        private void threadStart10()
        {
            while (thread10State)
            {
                if (isStartExec10)
                {
                    isAnalyzing = true;     // 分析状态：是否正在分析中
                    isStartExec10 = false;  // 线程执行一次后，置为false，等待下次执行
                    isRightAnalysis = true; // 每一次分析，初始化为true，如果手动关闭，则置为false，重新分析

                    try
                    {
                        Stopwatch stopwatch = new Stopwatch(); // 耗费总时间
                        stopwatch.Start();

                        AddListStr($"开始分析 {startTime} ~ {endTime} 时间段内数据  @ {DateTime.Now.ToString()}");
                        LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State", $"开始分析{startTime}~{endTime}时间段内数据");

                        #region 获取 msglist_report配置表：虚拟线体号、线体号、虚拟设备号、设备号、报警信息msg_id集合
                        // 查询看板服务器数据库上面的 msglist_report 表，获取所有设备信息
                        string ssql = "	select from_line_id,line_id,from_device_id,device_id,alarm_msg_id,device_analysis_bit,qty_msg_id,type,bit_type,status_a_msg_id,status_a_bit_id,status_b_msg_id,status_b_bit_id from msglist_report_new where from_line_id='" + line_id + "'";
                        msglist_rpt = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssql);
                        DataTable dt_msglist = msglist_rpt.Tables[0];
                        LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State", $"开始设备状态分析，查询msglist_report表行数：{dt_msglist.Rows.Count}");

                        // 虚拟线体号、线体号、虚拟设备号、设备号、报警信息msg_id集合
                        List<Tuple<string, string, string, string, string>> list_tuple_16 = new List<Tuple<string, string, string, string, string>>();
                        List<Tuple<string, string, string, string, string>> list_tuple_32 = new List<Tuple<string, string, string, string, string>>();

                        for (int i = 0; i < dt_msglist.Rows.Count; i++)
                        {
                            Tuple<string, string, string, string, string> tuple = new Tuple<string, string, string, string, string>
                                (
                                    dt_msglist.Rows[i]["from_line_id"].ToString().Trim(),
                                    dt_msglist.Rows[i]["line_id"].ToString().Trim(),
                                    dt_msglist.Rows[i]["from_device_id"].ToString().Trim(),
                                    dt_msglist.Rows[i]["device_id"].ToString().Trim(),
                                    dt_msglist.Rows[i]["alarm_msg_id"].ToString().Trim()
                                );

                            if (dt_msglist.Rows[i]["device_analysis_bit"].ToString() == "16")
                            {
                                list_tuple_16.Add(tuple);
                            }
                            else if (dt_msglist.Rows[i]["device_analysis_bit"].ToString() == "32")
                            {
                                list_tuple_32.Add(tuple);
                            }
                        }
                        #endregion

                        foreach (var Item in list_tuple_16)
                        {
                            string from_line_id = Item.Item1;
                            string line_id = Item.Item2;
                            string from_device_id = Item.Item3;
                            string device_id = Item.Item4;

                            string sqlString = $"select distinct device_id,msg_id FROM {companyNum}.{from_line_id}{CHtable_name} " +
                                                    $"WHERE eventtime >='{startTime}' and eventtime<'{endTime}' " +
                                                    $"and device_id = '{from_device_id}' " +
                                                    $"and ({sqlConditions.ToString()})" +
                                                    $"order by device_id,msg_id ";

                            string msgIDlist = string.Empty;

                            msgIDlist = PostResponse(httpClient16, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", sqlString.ToString());

                            string[] alist = msgIDlist.Split(Convert.ToChar("\n"));

                            for (int i = 0; i < alist.Length - 1; i++)
                            {
                                string[] device_msg_list = alist[i].Split(Convert.ToChar("\t"));
                                string deviceid = device_msg_list[0];
                                string msgid = device_msg_list[1];

                                GetDeviceStateValueFromCH(line_id, from_line_id, device_id, from_device_id, msgid, cts.Token);
                            }
                        }


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
                                    string sqlString = $"SELECT eventtime FROM {companyNum}.{line_id}{CHtable_name} " +
                                        $"WHERE eventtime >='{DateTime.Now.AddHours(-8).Date}' " +
                                        $"And ({sqlConditions.ToString()}) ORDER BY eventtime DESC LIMIT 100 ";

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
                                    if (Eventtime.Count == 100)
                                    {
                                        foreach (var item in Eventtime)
                                        {
                                            if (DateTime.TryParse(item.Trim(), out newDbTime) && DateTime.TryParse(lastAnalyseTime_Device_State, out lastTime))
                                            {
                                                if (lastTime.AddMinutes(31) > newDbTime)
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
                                        string newlastAnalyseTime_Device_State = endTime;
                                        DBOper.Init();
                                        DBOper db = new DBOper();
                                        int result = db.UpdateLastAnalyseTime(newlastAnalyseTime_Device_State, "lastAnalyseTime_Device_State");
                                        if (result == 1)
                                        {
                                            DateTime.UtcNow.ToString();
                                            AddListStr($"UTC时间段 {startTime}-{endTime} 内报警分析处理完成！ " + DateTime.Now.ToString() + "\r\n");
                                            LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State", $"更新上次分析时间戳成功：{newlastAnalyseTime_Device_State}\r\n");

                                            // 数据库更新后，再更新内存中的时间戳 lastAnalyseTime_Device_State
                                            DBSystemConfig dbSystemConfig = new DBSystemConfig();
                                            List<DBSystemConfig> list = db.QueryListCondition(dbSystemConfig, "Name = 'lastAnalyseTime_Device_State'");
                                            lastAnalyseTime_Device_State = list[0].Value;
                                        }
                                        else
                                        {
                                            isInitRedis = true;
                                            LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"更新上次分析时间戳失败：{newlastAnalyseTime_Device_State}\r\n");
                                            AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}  更新上次分析时间戳失败，请检查数据库配置！");
                                        }
                                    }

                                    isStartExec10 = false;
                                    isAnalyzing = false;
                                }
                                else
                                {
                                    // 旧版本(查询一整个班次数据分析),结束后判断若不是当天，则继续查询分析下一个班次数据
                                    AddListStr($"{workdate} & {workshift} ------ 设备状态分析处理完成 @ " + DateTime.Now.ToString() + " ------\r\n");
                                    LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State", $"{workdate} & {workshift} ------ 设备状态分析处理完成 @ " + DateTime.Now.ToString() + " ------\r\n");
                                    // isAnalyzing = false;                            

                                    DateTime ds, de;
                                    DateTime.TryParse(startTime, out ds);
                                    DateTime.TryParse(endTime, out de);
                                    DateTime nowTime = DateTime.Now.ToUniversalTime(); // 当前UTC时间+30分钟
                                    if (de < nowTime)
                                    {
                                        LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State", $"前startTime：{startTime}，前endTime：{endTime} 后startTime：{ds.AddHours(12).ToString()}，后endTime：{de.AddHours(12).ToString()}");

                                        startTime = ds.AddHours(12).ToString();
                                        endTime = de.AddHours(12).ToString();

                                        workdate = ds.AddHours(12).ToString("yyyy-MM-dd");

                                        workshift = (workshift == "0") ? "1" : "0";

                                        if (workshift == "0")
                                        {
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", ds.AddHours(12));
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                                        }
                                        else
                                        {
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", ds.AddHours(12));
                                            UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "白班");
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
                                LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"分析{startTime}~{endTime}时间段内数据，存在异常：{ex.Message}");
                            }
                        }
                        else
                        {
                            isAnalyzing = false;
                            isInitRedis = true; // 一个时间段结束，若需要重新分析，则重置该时间段的 Redis 键值

                            LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"分析{startTime}~{endTime}时间段内数据，分析时存在报错或手动停止，需要重新分析");
                        }


                        #endregion
                    }
                    catch (Exception ex)
                    {
                        LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"{ex.StackTrace}\r\n");
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
        /// 解析设备状态点位数据
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="from_line_id"></param>
        /// <param name="deviceID"></param>
        /// <param name="fromdeviceID"></param>
        /// <param name="msgID"></param>
        private void GetDeviceStateValueFromCH(string lineID, string from_line_id, string deviceID, string fromdeviceID, string msgID, CancellationToken token)
        {
            Stopwatch watch = new Stopwatch(); // 获取整体耗时时间
            watch.Start();
            try
            {
                RedisClient client = new RedisClient(RedisServer, RedisPort);
                client.Password = RedisPasswd;    //密码 //测试
                //client.Password = "slac1028";   //密码
                client.Db = Convert.ToInt32(lineID.Substring(lineID.Length - 2)); //根据线体号选择数据库 0-15

                // 初始化sql语句以及提交数据量（插入看板服务器mysql数据库）
                StringBuilder SqlString = new StringBuilder("");
                string sqlhead = "insert into " + lineID + "_device_state (workdate,workshift,line_id,device_id,msg_id,pv,lpv,diffv,pt,lpt,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                // 获取clickhouse数据库报警信息
                /// 根据device_id，msg_id 查询一个班次内的 eventtime 和 msg_bit（运算得出）
                /// data 和 （1768515945-device_id*msg_id）的值 ，使用 bitXor 操作进行 按位异或 运算，得到 msg_bit
                string ssql = " SELECT eventtime,bitXor(`data` , 1768515945-device_id*msg_id) as msg_bit from " + companyNum + "." + from_line_id + CHtable_name + " l where eventtime >= '" + startTime + "' and eventtime< '" + endTime
                 + "' and device_id =" + fromdeviceID + " and msg_id =" + msgID + " order by eventtime  ";

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(httpClient16, CHuser, CHpasswd, $"http://{CHserver}:{CHport}/", ssql.ToString());

                if (strResult != "")
                {
                    strResult = strResult.Remove(strResult.Count() - 1, 1);

                    string[] strArray = strResult.Split('\n');

                    List<int> listLastDataValueInt = new List<int>(new int[4]);       // 存储数据变化的上一次值

                    #region 读取redis存储的上一次更新的msgID点位的按位解析存储的数据，若不存在，初始化为 0
                    // 初始化Redis存储的值，result 为0，后面比较后，若有变化，修改result的值 
                    int initValue = 0;
                    string d = Convert.ToString(initValue, 2).PadLeft(32, '0'); // 将十进制数转换为二进制数，并补齐32位（前面填充0来补齐）                                        

                    DateTime.TryParse(startTime, out DateTime dt);
                    string sTime = dt.ToString("HH:mm:ss");

                    for (int j = 16; j < d.Length; j += 4)     // 32位数据，从左向右，从16个位开始运算。排除掉16-31的高位，保留0-15低位
                    {
                        int xx = (32 - j) / 4 - 1;

                        string lastKeyValue = client.Get<string>("k" + deviceID + "." + msgID + "." + xx.ToString());

                        if (sTime.Equals("00:00:00") || sTime.Equals("12:00:00")) // 每个班次开始时，将redis存储的值都重置为 0
                        {
                            lastKeyValue = string.Empty;
                        }

                        // 将redis里面存储的数据，存入listLastDataValueInt，作为该msgID的初始值
                        if (!string.IsNullOrEmpty(lastKeyValue) && lastKeyValue.Contains("@") && !isInitRedis)
                        {
                            listLastDataValueInt[xx] = Convert.ToInt32(lastKeyValue.Split('@')[0]);
                        }
                        else
                        {
                            // 如果Redis中不存在该key，则设置初始值为 0
                            listLastDataValueInt[xx] = 0;

                            int result = initValue;
                            client.Set("k" + deviceID + "." + msgID + "." + xx.ToString(), result.ToString() + "@" + startTime);
                        }
                    }

                    LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State",
                        $"在{startTime}~{endTime}时间段内，deviceID：{deviceID},msgID：{msgID}\r\n上一次值：" +
                        $"第00位：{listLastDataValueInt[0]}，" +
                        $"第01位：{listLastDataValueInt[1]}，" +
                        $"第02位：{listLastDataValueInt[2]}，" +
                        $"第03位：{listLastDataValueInt[3]}");

                    #endregion


                    int[] valueChange = new int[4] { 0, 0, 0, 0 }; // 记录设备状态msgID变化次数（每个msgID都有四个数据）

                    for (int i = 0; i < strArray.Count(); i++)
                    {
                        if (token.IsCancellationRequested) { break; } // 取消线程任务                        

                        string[] strBit = strArray[i].Split('\t');    // 0:时间  1:数据
                        DateTime nowTime = Convert.ToDateTime(strBit[0]);

                        // 将查询的原始数据，按位进行解析，获取解析后的十进制数据
                        List<int> listNowValueInt = AnalysisByBit.AnalysisByBitMethod(4, strBit[1]);

                        if (listNowValueInt.Any())
                        {
                            for (int j = 0; j < listNowValueInt.Count; j++)
                            {

                                // 比较当前数据和上一次的数据，若不相等，则表示该点位发生变化，需要记录
                                if (listNowValueInt[j] != listLastDataValueInt[j])
                                {
                                    // 记录变化次数
                                    valueChange[j] = valueChange[j] + 1;

                                    // 删除看板服务器数据库 _device_state表数据
                                    string ssqlDel = $"delete from {lineID}_device_state " +
                                                     $"where workdate='{workdate}' and workshift='{workshift}' " +
                                                     $"and line_id='{lineID}' and device_id='{deviceID}' " +
                                                     $"and msg_id = '{msgID}.{j}'";

                                    int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                                    // 获取redis中存储的上一次的变化值
                                    string lastKeyValue = client.Get<string>("k" + deviceID + "." + msgID + "." + j.ToString());
                                    string[] lastStrBit = lastKeyValue.Split('@');

                                    // 更新redis里面存储的值，即存储变化值
                                    client.Set($"k{deviceID}.{msgID}.{j}", $"{listNowValueInt[j]}@{strBit[0]}");

                                    // 构建插入语句（插入_device_state表）
                                    SqlString.Append($"('{workdate}','{workshift}','{lineID}','{deviceID}','{msgID}.{j}','{listNowValueInt[j]}','{lastStrBit[0]}'," +
                                                             $"'{Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastStrBit[1])).TotalSeconds)}'," +
                                                             $"'{strBit[0]}','{lastStrBit[1]}',now()),");
                                    Lcount++;
                                }

                                // 报警信息存入看板服务器数据库，1000条存一次
                                if (Lcount > 1000)
                                {
                                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                                    SqlString = new StringBuilder(sqlhead);
                                    Lcount = 0;
                                }

                                // 更新上次的变化值
                                listLastDataValueInt[j] = listNowValueInt[j];
                            }
                        }
                        else
                        {
                            LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State_error", $"deviceID:{deviceID}，msgID:{msgID} 异常：按位解析数据异常，当前数据为空\r\n");
                        }

                    }

                    LogConfig.Intence.WriteLog("RunLog\\Device_State", $"Device_State{deviceID}",
                        $"在{startTime}~{endTime}时间段内，设备号{deviceID}，msgID {msgID}\r\n设备状态变化次数：" +
                        $"第00位：{valueChange[0]}，" +
                        $"第01位：{valueChange[1]}，" +
                        $"第02位：{valueChange[2]}，" +
                        $"第03位：{valueChange[3]}");

                }
                watch.Stop();

                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                }

                AddListStr(workdate + " & " + workshift + " & " + deviceID + "." + msgID + " 设备状态处理完成！" + " @ " + DateTime.Now.ToString() + " 耗时： " + watch.ElapsedMilliseconds.ToString());

            }
            catch (ThreadAbortException) // 线程中止异常
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                isRightAnalysis = false; // 设置异常标志,这个时间段需要重新分析                

                LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State_error", $"deviceID:{deviceID}，msgID:{msgID} 异常：{ex.ToString()}\r\n");

                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 出错了！请查看 Alarm_error日志");

            }
        }

        /// <summary>
        /// 获取界面选择日期时间（分白夜班 各12小时）
        /// 根据这个时间段查询click house数据库数据
        /// </summary>
        private void getTodayAndShift()
        {
            try
            {
                if (!string.IsNullOrEmpty(lastAnalyseTime_Device_State))
                {

                    DateTime dt;
                    if (DateTime.TryParse(lastAnalyseTime_Device_State, out dt))
                    {
                        #region 分段模式下，根据上次分析时间，计算本次分析时间、班次，以及更新界面显示
                        isNewVersion = true; // 默认能获取到lastAnalyseTime_Device_State，是新模式（分段）

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

                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", dt);
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", workshift == "1" ? "白班" : "晚班");

                        #endregion
                    }
                    else
                    {
                        #region 查询整个班次模式下，获取界面选择日期时间，班次，计算本次分析时间、班次
                        isNewVersion = false;

                        // lastAnalyseTime_Device_State参数不是时间格式，则默认按照之前版本的逻辑（直接查询一个班次数据）获取时间
                        workdate = Convert.ToDateTime(FetchMainFormSettingsInfoEvent?.Invoke($"{type}时间")).Date.ToString("yyyy-MM-dd");

                        string shift = FetchMainFormSettingsInfoEvent?.Invoke($"{type}班次").ToString();

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
                    LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"获取界面日期、班次失败：{lastAnalyseTime_Device_State}");
                }

            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Device_State", $"获取界面日期、班次异常：{ex.Message}");
                AddListStr($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 获取界面日期、班次异常");
            }
        }

        /// <summary>
        /// 开始执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button2_Click(object sender, EventArgs e)
        {
            if (!isAnalyzing)
            {
                getTodayAndShift();
                AddListStr("开始处理！ " + DateTime.Now.ToString());

                isStartExec10 = true;
            }
            else
            {
                AddListStr("正在处理中，请稍后！ " + DateTime.Now.ToString());
            }
        }

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

        /// <summary>
        /// 停止解析
        /// </summary>
        public void StopService()
        {
            try
            {
                LogConfig.Intence.WriteLog("RunLog\\Device_State", "Device_State", $"线程开始关闭：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
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
                        thread10.Abort(); // 若正在解析数据，直接终止线程
                    }
                }
                thread10 = null;

                httpClient16.Dispose();
                httpClient16 = null;

            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Device_State", "Task", $"停止解析异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 页面大小改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frm_main_device_state_Resize(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;//窗体填充所在容器控件
            listBox1.Width = this.Width;

            //listBox1.Height = this.Height - (comboBox1.Height + comboBox1.Location.Y) - 20;
            //listBox1.Location = new Point(10, comboBox1.Location.Y + comboBox1.Height + 20);

            label1.Location = new Point(listBox1.Width / 3, 10);
            checkBox_Alarm.Location = new Point(this.Width - 15 - checkBox_Alarm.Width, 10);

            listBox1.Height = this.Height - (checkBox_Alarm.Height + checkBox_Alarm.Location.Y);
            listBox1.Location = new Point(0, checkBox_Alarm.Location.Y + checkBox_Alarm.Height + 5);
        }
    }
}
