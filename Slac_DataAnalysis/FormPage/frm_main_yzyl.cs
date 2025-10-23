using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Http.Headers;
using Slac_DataAnalysis_Bit;
using ServiceStack.Redis.Messaging;
using Slac_DataAnalysis.Common;
using Slac_DataAnalysis.DatabaseSql.DBModel;
using Slac_DataAnalysis.DatabaseSql.DBOper;
using System.Data.SqlTypes;
using System.Reflection.Emit;
using System.Globalization;

namespace Slac_DataAnalysis
{
    /// <summary>
    /// 统计分析
    /// </summary>
    public partial class frm_main_yzyl : Form
    {
        public frm_main_yzyl()
        {
            InitializeComponent();

            this.TopLevel = false;                       //窗体不作为顶级窗体
            this.Dock = DockStyle.Fill;                  //窗体填充父容器
            this.FormBorderStyle = FormBorderStyle.None; //隐藏窗体边框

            this.DoubleBuffered = true; //开启双缓冲，防止窗体闪烁

            this.comboBox1.Visible = false;
            this.dateTimePicker1.Visible = false;
            this.button2.Visible = false;
        }

        public event Action<string, object> UpdateMainFormSettingsInfoEvent; // 更新主界面设置信息
        public event Func<string, object> FetchMainFormSettingsInfoEvent;    // 获取主界面设置信息

        private CancellationTokenSource cts;       // 取消线程
        private Thread thread10 = null;            // 线程
        private bool TimerStatus = true;           // 定时器状态
        private bool thread10State;                // 线程状态
        private static bool isStartExec10 = false; // 是否开始执行线程        
        private volatile bool isAnalyzing = false; // 是否正在分析


        private static string workdate = "";       // 工作日期
        private static string workshift = "1";     // 班次（白夜班）
        private static string startTime = "";      // 查询clickhouse数据库起始时间
        private static string endTime = "";        // 查询clickhouse数据库结束时间        

        private static Int32 lastValue = -1;       // 上一次变化的值
        private static DateTime lastTime;          // 上一次变化的时间        

        public DataSet msglist_rpt = new DataSet();// 查询msglist_report表，存储获取的所有设备信息

        #region 数据库配置参数
        private static string line_id;            // 线体号 line + 线体编号
        private static string companyNum;         // 公司名称（数据库库名）"yzenpack";
        private static string Conn_battery;       // 看板服务器Mysql数据库连接字符串

        private static string CHtable_name = "";  // 非分布式表空白
        private static string isCluster;          // 是否是集群（分布式表）

        private static string CHserver;           // clickhouse数据库IP "172.16.0.30"; 
        private static string CHpasswd;           // clickhouse数据库密码
        private static string CHport;             // clickhouse数据库端口
        private static string CHuser = "default"; // clickhouse数据库用户名
        #endregion

        #region 注释
        //private static string CHserver = ConfigHelper.GetAppConfig("CHserver");  //"172.16.253.3";
        //private static Int32 lastValue = -1;
        //private static DateTime lastTime;
        //private static string workdate = "";
        //private static string workshift = "1";
        //private static string startTime = "";
        //private static string endTime = "";
        ////private static string line_id = "line100105";
        //private static string line_id = "line" + ConfigHelper.GetAppConfig("LineID");
        //private static string companyNum = ConfigHelper.GetAppConfig("factory");  // "yzenpack";
        //private static string CHtable_name = "";   //非分布式表空白
        //private static string CHuser = "default";
        //private static string CHpasswd = "slac1028";

        //private static string CHtable_name = "_all";  //分布式表加all
        //private static string CHuser = "default";
        //private static string CHpasswd = "slac1028#"; 
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

            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog", "Stats", $"获取数据库配置参数异常Error：{ex.ToString()}");
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
            GetParamConfig(); // 获取数据库配置参数

            timer1.Enabled = true; //启动定时器

            this.Text = this.Text + "_" + line_id;
            // client.Password = "slac1028";//密码 
            // client.Db = 2; //选择第1个数据库，0-15

            thread10State = true;
            cts = new CancellationTokenSource();
            thread10 = new Thread(threadStart10);
            thread10.IsBackground = true;
            thread10.Start();

            if (isCluster == "1")
            {
                CHpasswd = "slac1028#";
                CHtable_name = "_all"; ////分布式表加all
            }

        }

        /// <summary>
        /// 获取界面选择日期时间（分白夜班 各12小时）
        /// 根据这个时间段查询click house数据库数据
        /// </summary>
        private void getTodayAndShift()
        {
            this.BeginInvoke(new Action(delegate
            {
                //workdate = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
                workdate = Convert.ToDateTime(FetchMainFormSettingsInfoEvent?.Invoke("统计分析时间")).Date.ToString("yyyy-MM-dd");
                string shift = FetchMainFormSettingsInfoEvent?.Invoke("统计分析班次").ToString();
                if (shift == "白班"/*comboBox1.Text == "白班"*/)
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
            }));
        }

        /// <summary>
        /// 加载窗体时，启动线程
        /// </summary>
        private void threadStart10()
        {
            while (thread10State)
            {
                if (isStartExec10)
                {
                    isStartExec10 = false;
                    isAnalyzing = true;  // 分析状态
                    try
                    {
                        // 查询看板服务器数据库上面的 msglist_report 表，获取所有设备信息
                        string ssql = "	select from_line_id,line_id,from_device_id,device_id,qty_msg_id,type,bit_type,status_a_msg_id,status_a_bit_id,status_b_msg_id,status_b_bit_id from msglist_report where line_id='" + line_id + "'";
                        msglist_rpt = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssql);
                        DataTable dt_msglist = msglist_rpt.Tables[0];
                        LogConfig.Intence.WriteLog("RunLog", "Stats", $"开始统计分析，查询msglist_report表行数：{dt_msglist.Rows.Count}");
                        for (int m = 0; m < dt_msglist.Rows.Count; m++)
                        {
                            if (cts.Token.IsCancellationRequested) // 判断是否需要停止线程
                            {
                                break;
                            }

                            DataRow dr_msg = dt_msglist.Rows[m];

                            string device_id = dr_msg["from_device_id"].ToString();

                            //统计产量 //getQtyFromCHbyMinute(line_id, "10", "10", "3009");
                            getQtyFromCHbyMinute(dr_msg["line_id"].ToString(), dr_msg["from_line_id"].ToString(), dr_msg["from_device_id"].ToString(), dr_msg["device_id"].ToString(), dr_msg["qty_msg_id"].ToString());

                            LogConfig.Intence.WriteLog("RunLog", "Stats", $"统计分析{startTime}~{endTime}时间段内数据 设备号{device_id} 产量统计分析完成");

                            //计算停机信息  //getStopValueFromCH(line_id, "10", "10","201","25");
                            if (dr_msg["type"].ToString() == "a")
                            {
                                getStopValueFromCH_New(dr_msg["line_id"].ToString(), dr_msg["from_line_id"].ToString(), dr_msg["from_device_id"].ToString(), dr_msg["device_id"].ToString(), dr_msg["status_a_msg_id"].ToString(), dr_msg["status_a_bit_id"].ToString());
                            }
                            else if (dr_msg["type"].ToString() == "ab")
                            {    //getStopValueFromCH(line_id, "12", "12","1250","0","1250","2");
                                getStopValueFromCH_AB(dr_msg["line_id"].ToString(), dr_msg["from_line_id"].ToString(), dr_msg["from_device_id"].ToString(), dr_msg["device_id"].ToString(), dr_msg["status_a_msg_id"].ToString(), dr_msg["status_a_bit_id"].ToString(), dr_msg["status_b_msg_id"].ToString(), dr_msg["status_b_bit_id"].ToString());
                            }
                            LogConfig.Intence.WriteLog("RunLog", "Stats", $"统计分析{startTime}~{endTime}时间段内数据 设备号{device_id} 计算停机信息完成");

                            //按小时统计停机   //getStopMinuteByHours(line_id, "20", "206.16");
                            getStopMinuteByHours(dr_msg["line_id"].ToString(), dr_msg["device_id"].ToString(), dr_msg["status_a_msg_id"].ToString() + "." + dr_msg["status_a_bit_id"].ToString());

                            LogConfig.Intence.WriteLog("RunLog", "Stats", $"统计分析{startTime}~{endTime}时间段内数据 设备号{device_id} 按小时统计停机完成");

                            // 统计班次报表   // getReportByShift(line_id, "10", "201.25", "3000", "0");  
                            getReportByShift(dr_msg["line_id"].ToString(), dr_msg["device_id"].ToString(), dr_msg["status_a_msg_id"].ToString() + "." + dr_msg["status_a_bit_id"].ToString(), dr_msg["qty_msg_id"].ToString(), "0");

                            LogConfig.Intence.WriteLog("RunLog", "Stats", $"统计分析{startTime}~{endTime}时间段内数据 设备号{device_id} 统计班次报表完成");


                        }

                        #region 注释
                        /*
                                       //getQtyFromCHbyMinute(line_id, "10", "10", "3009");//统计产量- 基本盖
                                       //1-2线
                                       //string qtyMsgId_10 = "3000";
                                       //string qtyMsgId_11 = "3089";
                                       //string qtyMsgId_12 = "3198";
                                       //string qtyMsgId_13 = "3061";
                                       //string qtyMsgId_14 = "3003";
                                       //string qtyMsgId_15 = "3075";
                                       //string qtyMsgId_16 = "3002";
                                       //string qtyMsgId_17 = "3000";
                                       //string qtyMsgId_18 = "3009";
                                       //string qtyMsgId_19 = "3002";
                                       //string qtyMsgId_20 = "3129";

                                       ////--chizhou-line3\4
                                       //string qtyMsgId_10 = "3001";
                                       //string qtyMsgId_11 = "3006";
                                       //string qtyMsgId_12 = "1139";
                                       //string qtyMsgId_13 = "3005";
                                       //string qtyMsgId_14 = "3001";
                                       //string qtyMsgId_15 = "3040";
                                       //string qtyMsgId_16 = "3187";
                                       //string qtyMsgId_17 = "3020";
                                       //string qtyMsgId_18 = "3008";
                                       //string qtyMsgId_19 = "3000";
                                       //string qtyMsgId_20 = "3129";

                                       ////统计产量
                                       //getQtyFromCHbyMinute(line_id, "10", "10", qtyMsgId_10);//统计产量- 
                                       //getQtyFromCHbyMinute(line_id, "11", "11", qtyMsgId_11);//统计产量- 
                                       //getQtyFromCHbyMinute(line_id, "12", "12", qtyMsgId_12);//统计产量- 
                                       //getQtyFromCHbyMinute(line_id, "13", "13", qtyMsgId_13);//统计产量- 
                                       //getQtyFromCHbyMinute(line_id, "14", "14", qtyMsgId_14);//统计产量-
                                       //getQtyFromCHbyMinute(line_id, "15", "15", qtyMsgId_15);//统计产量-
                                       //getQtyFromCHbyMinute(line_id, "16", "16", qtyMsgId_16);//统计产量-
                                       //getQtyFromCHbyMinute(line_id, "17", "17", qtyMsgId_17);//统计产量-
                                       //getQtyFromCHbyMinute(line_id, "18", "18", qtyMsgId_18);//统计产量-
                                       //getQtyFromCHbyMinute(line_id, "19", "19", qtyMsgId_19);//统计产量-
                                       //getQtyFromCHbyMinute(line_id, "20", "20", qtyMsgId_20);//统计产量-

                                       //计算停机信息
                                       //getStopValueFromCH(line_id, "10", "10", spMsgid, spMsgBit);   

                                       //--line1\2 
                                       //getStopValueFromCH(line_id, "10", "10", "201", "25");   
                                       //getStopValueFromCH(line_id, "11", "11", "206", "10");
                                       //getStopValueFromCH(line_id, "12", "12", "207", "14");
                                       //getStopValueFromCH(line_id, "13", "13", "203", "31");
                                       //getStopValueFromCH(line_id, "14", "14", "203", "9");
                                       //getStopValueFromCH(line_id, "15", "15", "204", "11");
                                       //getStopValueFromCH(line_id, "16", "16", "200", "0");
                                       //getStopValueFromCH(line_id, "17", "17", "201", "27");
                                       //getStopValueFromCH(line_id, "18", "18", "100", "4");

                                       ////--line 3\4
                                       //getStopValueFromCH(line_id, "10", "10","201","25");
                                       //getStopValueFromCH(line_id, "11", "11","206","10");
                                       //getStopValueFromCH(line_id, "12", "12","205","20");
                                       //getStopValueFromCH(line_id, "13", "13","200","0");
                                       //getStopValueFromCH(line_id, "14", "14","200","8");
                                       //getStopValueFromCH(line_id, "15", "15","204","25");
                                       //getStopValueFromCH(line_id, "16", "16","200","0");
                                       //getStopValueFromCH(line_id, "17", "17","202","3");
                                       //getStopValueFromCH(line_id, "18", "18","200","4");
                                       //getStopValueFromCH(line_id, "19", "19","200","0");
                                       //getStopValueFromCH(line_id, "20", "20","206","16");

                                       //---line1\2
                                       //getStopMinuteByHours(line_id, "10", "201.25");   
                                       //getStopMinuteByHours(line_id, "11", "206.10");
                                       //getStopMinuteByHours(line_id, "12", "207.14");
                                       //getStopMinuteByHours(line_id, "13", "203.31");
                                       //getStopMinuteByHours(line_id, "14", "203.9");
                                       //getStopMinuteByHours(line_id, "15", "204.11");
                                       //getStopMinuteByHours(line_id, "16", "200.0");
                                       //getStopMinuteByHours(line_id, "17", "201.27");
                                       //getStopMinuteByHours(line_id, "18", "100.4");

                                       ////---line3\4
                                       //getStopMinuteByHours(line_id, "10", "201.25");
                                       //getStopMinuteByHours(line_id, "11", "206.10");
                                       //getStopMinuteByHours(line_id, "12", "205.20");
                                       //getStopMinuteByHours(line_id, "13", "200.0");
                                       //getStopMinuteByHours(line_id, "14", "200.8");
                                       //getStopMinuteByHours(line_id, "15", "204.25");
                                       //getStopMinuteByHours(line_id, "16", "200.0");
                                       //getStopMinuteByHours(line_id, "17", "202.3");
                                       //getStopMinuteByHours(line_id, "18", "200.4");
                                       //getStopMinuteByHours(line_id, "19", "200.0");
                                       //getStopMinuteByHours(line_id, "20", "206.16");

                                       //--line1-2
                                       //getReportByShift(line_id, "10", "201.25", "3000", "0");   //
                                       //getReportByShift(line_id, "11", "206.10", "3089", "0");     //
                                       //getReportByShift(line_id, "12", "207.14", "3198", "0");     //
                                       //getReportByShift(line_id, "13", "203.31", "3061", "0"); //
                                       //getReportByShift(line_id, "14", "203.9", "3003", "0"); //
                                       //getReportByShift(line_id, "15", "204.11", "3075", "0");//
                                       //getReportByShift(line_id, "16", "200.0", "3002", "0");//
                                       //getReportByShift(line_id, "17", "201.27", "3000", "0");//
                                       //getReportByShift(line_id, "18", "100.4", "3009", "0");//

                                       ////-- line 3-4
                                       //getReportByShift(line_id, "10", "201.25", qtyMsgId_10, "0");
                                       //getReportByShift(line_id, "11", "206.10", qtyMsgId_11, "0");
                                       //getReportByShift(line_id, "12", "205.20", qtyMsgId_12, "0");
                                       //getReportByShift(line_id, "13", "200.0", qtyMsgId_13, "0");
                                       //getReportByShift(line_id, "14", "200.8", qtyMsgId_14, "0");
                                       //getReportByShift(line_id, "15", "204.25", qtyMsgId_15, "0");
                                       //getReportByShift(line_id, "16", "200.0", qtyMsgId_16, "0");
                                       //getReportByShift(line_id, "17", "202.3", qtyMsgId_17, "0");
                                       //getReportByShift(line_id, "18", "200.4", qtyMsgId_18, "0");
                                       //getReportByShift(line_id, "19", "200.0", qtyMsgId_19, "0");
                                       //getReportByShift(line_id, "20", "206.16", qtyMsgId_20, "0");
                                       */
                        #endregion

                        AddListStr($"{workdate} & {workshift} ------ 统计分析处理完成 @ " + DateTime.Now.ToString() + " ------\r\n");
                        LogConfig.Intence.WriteLog("RunLog", "Stats", $"{workdate} & {workshift} ------ 统计分析处理完成 @ " + DateTime.Now.ToString() + " ------\r\n");
                        isAnalyzing = false;
                    }
                    catch (ThreadAbortException)
                    {
                        // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
                    }
                    catch (Exception ex)
                    {
                        isAnalyzing = false;
                        System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd") + "_error10.log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
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
        /// 计时器事件：每隔一秒触发一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (TimerStatus)
            {
                string type = "统计分析";
                DateTime nowtime = DateTime.Now;

                nowtime.ToString("yyyy-MM-dd HH:mm:ss");

                if (!isAnalyzing && (
                   nowtime.ToString("mm:ss") == "02:00" || nowtime.ToString("mm:ss") == "12:00" || //nowtime.ToString("mm:ss") == "06:00" || nowtime.ToString("mm:ss") == "16:00" ||
                   nowtime.ToString("mm:ss") == "22:00" || nowtime.ToString("mm:ss") == "32:00" ||  // nowtime.ToString("mm:ss") == "26:00" || nowtime.ToString("mm:ss") == "36:00" ||
                   nowtime.ToString("mm:ss") == "42:00" || nowtime.ToString("mm:ss") == "52:00" //|| nowtime.ToString("mm:ss") == "46:00" || nowtime.ToString("mm:ss") == "56:00"
                  ))
                {
                    if (nowtime < Convert.ToDateTime("08:33:00"))
                    {
                        //dateTimePicker1.Value = DateTime.Now.AddDays(-1);
                        //comboBox1.Text = "晚班";
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now.AddDays(-1));
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                        isStartExec10 = true;
                    }
                    else if (nowtime > Convert.ToDateTime("20:33:00"))
                    {
                        //nowtime.ToString("HH:mm:ss") == "20:41:00" || nowtime.ToString("HH:mm:ss") == "20:51:00"

                        //dateTimePicker1.Value = DateTime.Now;
                        //comboBox1.Text = "晚班";
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now);
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "晚班");
                        isStartExec10 = true;
                        //  button4_Click(sender, e);
                        //  button1_Click(sender, e);
                        ////  button2_Click(sender, e);
                        //  button3_Click(sender, e);
                        //  button5_Click(sender, e);
                    }
                    else
                    {
                        //dateTimePicker1.Value = DateTime.Now;
                        //comboBox1.Text = "白班";
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}时间", DateTime.Now);
                        UpdateMainFormSettingsInfoEvent?.Invoke($"{type}班次", "白班");
                        isStartExec10 = true;

                        //  button4_Click(sender, e);
                        //  button1_Click(sender, e);
                        ////  button2_Click(sender, e);
                        //  button3_Click(sender, e);
                        //  button5_Click(sender, e);
                    }
                }

                getTodayAndShift();
            }

        }

        /// <summary>
        /// 统计产量
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="fromdeviceID"></param>
        /// <param name="msgID"></param>
        private void getQtyFromCHbyMinute(string lineID, string fromLineID, string fromdeviceID, string deviceID, string msgID)
        {
            if (cts.Token.IsCancellationRequested) { return; }

            //按分钟处理msgID的数据，计算行间差值，并保存mysql数据库
            //  Stopwatch watch = new Stopwatch();
            // watch.Start();

            // getTodayAndShift();

            StringBuilder SqlString = new StringBuilder("");
            string sqlhead = "insert into " + lineID + "_qty (workdate,workshift,line_id,device_id,msg_id,pv,lpv,diffv,pt,lpt,indate) values ";
            SqlString = new StringBuilder(sqlhead);
            int Lcount = 0;

            try
            {
                //string ssqlDel = "delete from " + lineID + "_qty where workdate='" + workdate + "' and workshift='" + workshift + "'  and device_id='" + deviceID + "' and msg_id='" + msgID + "'";
                string ssqlDel = $"delete from {lineID}_qty where workdate='{workdate}' and workshift='{workshift}' and device_id='{deviceID}'";

                int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                /// 对特定的device_id和msg_id，在指定的时间范围内，对每分钟的数据进行分组，并计算每组中经过特定按位异或操作后的数据的最小值
                string ssql = " SELECT toStartOfMinute(eventtime) as timeStart,MIN(bitXor(`data` , 1768515945-device_id*msg_id)) as dataMax from " + companyNum + "." + fromLineID + CHtable_name + " l where eventtime >= '" + startTime + "' and eventtime<= '" + endTime
                    + "' and device_id =" + fromdeviceID + " and msg_id =" + msgID + " group by timeStart  order by timeStart   ";

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(CHuser, CHpasswd, $"http://{CHserver}:8123/", ssql.ToString());
                // listBox1.Items.Add(ssql.ToString() + "！ ");
                // listBox1.Items.Add(CHserver.ToString() + "！ ");

                if (strResult != "")
                {
                    // listBox1.Items.Add("查询！ ");
                    strResult = strResult.Remove(strResult.Count() - 1, 1);

                    string[] strArray = strResult.Split('\n');

                    string[] firstBit = strArray[0].Split('\t');
                    DateTime firstTime = Convert.ToDateTime(firstBit[0]);
                    int firstValue = Convert.ToInt32(firstBit[1]);

                    // DateTime firstTime = Convert.ToDateTime(startTime);
                    // int firstValue = 0;
                    lastValue = firstValue;
                    lastTime = firstTime;

                    //  listBox1.Items.Add(strArray.Count().ToString());

                    for (int i = 1; i < strArray.Count(); i++)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        string[] strBit = strArray[i].Split('\t');
                        DateTime nowTime = Convert.ToDateTime(strBit[0]);
                        int nowValue = Convert.ToInt32(strBit[1]);

                        int diffData = nowValue - lastValue;
                        if (diffData < 0) { diffData = nowValue; }
                        //   listBox1.Items.Add(strBit[0]);
                        SqlString.Append(" ('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + diffData.ToString() + "','" + strBit[0] + "','" + lastTime.ToString() + "',now()),");
                        //msg_id,pv,lpv,diffv,pt,lpt,indate

                        //listBox1.Items.Add(SqlString.Remove(SqlString.Length - 1, 1).ToString());
                        Lcount++;
                        //记录上次状态变化的时间
                        lastTime = nowTime;
                        lastValue = nowValue;

                    }

                }
                // watch.Stop();


                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    AddListStr(workdate + " & " + workshift + " & " + deviceID + "产量统计完成！ " + DateTime.Now.ToString());



                }


            }
            catch (ThreadAbortException)
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                AddListStr("出错了！ ");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                // System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", SqlString.ToString());
            }
        }

        /// <summary>
        /// 计算停机信息
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="fromdeviceID"></param>
        /// <param name="msgID"></param>
        /// <param name="msgBit"></param>
        private void getStopValueFromCH(string lineID, string fromLineID, string fromdeviceID, string deviceID, string msgID, string msgBit)
        {
            if (cts.Token.IsCancellationRequested) { return; }

            //2.14  计算停机信息
            //Stopwatch watch = new Stopwatch();
            //watch.Start();

            // getTodayAndShift();

            StringBuilder SqlString = new StringBuilder("");
            try
            {

                string sqlhead = "insert into " + lineID + "_state (workdate,workshift,line_id,device_id,msg_id,pv,lpv,diffv,pt,lpt,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                string ssqlDel = "delete from " + lineID + "_state where workdate='" + workdate + "' and workshift='" + workshift + "'  and device_id='" + deviceID + "'";
                int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                string ssql = " SELECT eventtime,bitTest(bitXor(`data` , 1768515945-device_id*msg_id)," + msgBit + ") as msg_bit from " + companyNum + "." + fromLineID + CHtable_name + " l where eventtime >= '" + startTime + "' and eventtime< '" + endTime
                    + "' and device_id =" + fromdeviceID + " and msg_id =" + msgID + " order by eventtime  ";

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(CHuser, CHpasswd, $"http://{CHserver}:8123/", ssql.ToString());
                if (strResult != "")
                {
                    strResult = strResult.Remove(strResult.Count() - 1, 1);
                    string[] strArray = strResult.Split('\n');

                    string[] firstBit = strArray[0].Split('\t');

                    DateTime firstTime = Convert.ToDateTime(firstBit[0]);
                    int firstValue = Convert.ToInt32(firstBit[1]);

                    lastValue = firstValue;
                    lastTime = firstTime;

                    for (int i = 1; i < strArray.Count(); i++)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        string[] strBit = strArray[i].Split('\t');
                        DateTime nowTime = Convert.ToDateTime(strBit[0]);
                        int nowValue = Convert.ToInt32(strBit[1]);

                        if (nowValue != lastValue)
                        {
                            // client.Set("k"+ deviceID+"." + msgID + "." + msgBit, nowValue.ToString() + "@" + strBit[0]);
                            SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "." + msgBit + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastTime)).TotalSeconds).ToString() + "','" + strBit[0] + "','" + lastTime.ToString() + "',now()),");
                            //msg_id,pv,lpv,diffv,pt,lpt,indate
                            Lcount++;
                            //记录上次状态变化的时间
                            lastTime = nowTime;
                            lastValue = nowValue;
                        }

                        if (i == strArray.Count() - 1)
                        {
                            nowValue = lastValue == 1 ? 0 : 1;
                            // client.Set("k" + deviceID + "."+ msgID + "." + msgBit, nowValue.ToString() + "@" + endTime);
                            if (DateTime.Now.ToUniversalTime() > Convert.ToDateTime(endTime))
                            {
                                SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "." + msgBit + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + Math.Round((Convert.ToDateTime(endTime) - Convert.ToDateTime(lastTime)).TotalSeconds).ToString() + "','" + endTime + "','" + lastTime.ToString() + "',now()),");
                                //client.Set("k"+ deviceID+"." + msgID + "." + msgBit, lastValue.ToString() + "@" + endTime);
                            }
                            else
                            {
                                SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "." + msgBit + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + Math.Round((DateTime.Now.ToUniversalTime() - Convert.ToDateTime(lastTime)).TotalSeconds).ToString() + "','" + DateTime.Now.ToUniversalTime().ToString() + "','" + lastTime.ToString() + "',now()),");
                                // client.Set("k" + deviceID + "." + msgID + "." + msgBit, lastValue.ToString() + "@" + DateTime.Now.ToUniversalTime().ToString());
                            }

                            //msg_id,pv,lpv,diffv,pt,lpt,indate
                            Lcount++;
                        }


                        if (Lcount > 1000)
                        {
                            string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                            int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                            SqlString = new StringBuilder(sqlhead);
                            Lcount = 0;
                        }

                    }

                }


                // watch.Stop();
                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    AddListStr(workdate + " & " + workshift + " & " + deviceID + "停机信息处理完成！ " + DateTime.Now.ToString());


                }

                //if (deviceID.Equals("10"))
                //{

                //    LogConfig.Intence.WriteLog("ErrLog", "Stop", $"当前msgID：{msgID} \r\n SqlString:{SqlString} \r\n");
                //}
            }
            catch (ThreadAbortException)
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                AddListStr("出错了！ ");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + "SQL.log", SqlString.ToString() + DateTime.Now.ToString() + "\r\n");
            }
        }

        /// <summary>
        /// 获取停机信息
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="fromdeviceID"></param>
        /// <param name="msgID_A"></param>
        /// <param name="msgBit_A"></param>
        /// <param name="msgID_B"></param>
        /// <param name="msgBit_B"></param>
        private void getStopValueFromCH_AB(string lineID, string fromLineID, string fromdeviceID, string deviceID, string msgID_A, string msgBit_A, string msgID_B, string msgBit_B)
        {
            if (cts.Token.IsCancellationRequested) { return; }

            //2.14  计算停机信息
            //Stopwatch watch = new Stopwatch();
            //watch.Start();

            // getTodayAndShift();

            StringBuilder SqlString = new StringBuilder("");
            try
            {

                string sqlhead = "insert into " + lineID + "_state (workdate,workshift,line_id,device_id,msg_id,pv,lpv,diffv,pt,lpt,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                string ssqlDel = "delete from " + lineID + "_state where workdate='" + workdate + "' and workshift='" + workshift + "'  and device_id='" + deviceID + "'";
                int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                string ssql = " SELECT eventtime,bitXor(`data` , 1768515945-device_id*msg_id) as msg_data from " + companyNum + "." + fromLineID + CHtable_name + " l where eventtime >= '" + startTime + "' and eventtime< '" + endTime
                    + "' and device_id =" + fromdeviceID + " and msg_id =" + msgID_A + " order by eventtime  ";

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(CHuser, CHpasswd, $"http://{CHserver}:8123/", ssql.ToString());
                if (strResult != "")
                {
                    strResult = strResult.Remove(strResult.Count() - 1, 1);
                    string[] strArray = strResult.Split('\n');

                    string[] firstBit = strArray[0].Split('\t');

                    DateTime firstTime = Convert.ToDateTime(firstBit[0]);
                    int firstValue = Convert.ToInt32(firstBit[1]);

                    int modle_a = (firstValue << 28) >> 28;  //左移28，右移28，得出第0个4位的值
                    int status_a = (firstValue << 24) >> 28;  //左移24，右移28，得出第1个4位的值
                    int modle_b = (firstValue << 20) >> 28;  //左移20，右移28，得出第2个4位的值
                    int status_b = (firstValue << 16) >> 28;  //左移16，右移28，得出第3个4位的值
                    int lastValue_a = modle_a == 1 & status_a == 15 ? 1 : 0;  //如果模式为1，状态为15，则运行状态=1，否则0
                    int lastValue_b = modle_b == 1 & status_b == 15 ? 1 : 0;  //如果模式为1，状态为15，则运行状态=1，否则0
                    lastValue = lastValue_a & lastValue_b;  //1\2-14工位同时为1，机器为正常运行。有1个为0即停机。

                    lastTime = firstTime;

                    for (int i = 1; i < strArray.Count(); i++)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        string[] strBit = strArray[i].Split('\t');
                        DateTime nowTime = Convert.ToDateTime(strBit[0]);
                        int msgValue = Convert.ToInt32(strBit[1]);

                        modle_a = (msgValue << 28) >> 28;  //左移28，右移28，得出第0个4位的值
                        status_a = (msgValue << 24) >> 28;  //左移24，右移28，得出第1个4位的值
                        modle_b = (msgValue << 20) >> 28;  //左移20，右移28，得出第2个4位的值
                        status_b = (msgValue << 16) >> 28;  //左移16，右移28，得出第3个4位的值
                        int Value_a = modle_a == 1 & status_a == 15 ? 1 : 0;  //如果模式为1，状态为15，则运行状态=1，否则0
                        int Value_b = modle_b == 1 & status_b == 15 ? 1 : 0;  //如果模式为1，状态为15，则运行状态=1，否则0
                        int nowValue = Value_a & Value_b;  //1\2-14工位同时为1，机器为正常运行。有1个为0即停机。

                        if (nowValue != lastValue)
                        {
                            // client.Set("k"+ deviceID+"." + msgID + "." + msgBit, nowValue.ToString() + "@" + strBit[0]);
                            SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID_A + "." + msgBit_A + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + Math.Round((Convert.ToDateTime(strBit[0]) - Convert.ToDateTime(lastTime)).TotalSeconds).ToString() + "','" + strBit[0] + "','" + lastTime.ToString() + "',now()),");
                            //msg_id,pv,lpv,diffv,pt,lpt,indate
                            Lcount++;
                            //记录上次状态变化的时间
                            lastTime = nowTime;
                            lastValue = nowValue;
                        }

                        if (i == strArray.Count() - 1)
                        {
                            nowValue = lastValue == 1 ? 0 : 1;
                            // client.Set("k" + deviceID + "."+ msgID + "." + msgBit, nowValue.ToString() + "@" + endTime);
                            if (DateTime.Now.ToUniversalTime() > Convert.ToDateTime(endTime))
                            {
                                SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID_A + "." + msgBit_A + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + Math.Round((Convert.ToDateTime(endTime) - Convert.ToDateTime(lastTime)).TotalSeconds).ToString() + "','" + endTime + "','" + lastTime.ToString() + "',now()),");
                                //client.Set("k"+ deviceID+"." + msgID + "." + msgBit, lastValue.ToString() + "@" + endTime);
                            }
                            else
                            {
                                SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID_A + "." + msgBit_A + "','" + nowValue.ToString() + "','" + lastValue.ToString() + "','" + Math.Round((DateTime.Now.ToUniversalTime() - Convert.ToDateTime(lastTime)).TotalSeconds).ToString() + "','" + DateTime.Now.ToUniversalTime().ToString() + "','" + lastTime.ToString() + "',now()),");
                                // client.Set("k" + deviceID + "." + msgID + "." + msgBit, lastValue.ToString() + "@" + DateTime.Now.ToUniversalTime().ToString());
                            }

                            //msg_id,pv,lpv,diffv,pt,lpt,indate
                            Lcount++;
                        }


                        if (Lcount > 1000)
                        {
                            string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                            int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                            SqlString = new StringBuilder(sqlhead);
                            Lcount = 0;
                        }

                    }

                }


                // watch.Stop();
                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    AddListStr(workdate + " & " + workshift + " & " + deviceID + "停机信息处理完成！ " + DateTime.Now.ToString());
                }
            }
            catch (ThreadAbortException)
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                AddListStr("出错了！ ");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + "SQL.log", SqlString.ToString() + DateTime.Now.ToString() + "\r\n");
            }
        }

        /// <summary>
        /// 获取更新state表的sql
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="msgID"></param>
        /// <param name="nowTime"></param>
        /// <param name="lastState"></param>
        /// <param name="nowState"></param>
        /// <returns></returns>
        private string GetDeviceStateSqlStr(string lineID, string deviceID, string msgID, DateTime nowTime, MachineStatus lastState, MachineStatus nowState)
        {
            int lastBit = (int)lastState; // 上次状态,对应点位的位
            int nowBit = (int)nowState;   // 当前状态,对应点位的位                        

            double diffv = (nowTime - Convert.ToDateTime(lastTime)).TotalSeconds; // 持续时间

            StringBuilder sqlPartStr = new StringBuilder("");


            // 更新上一个点位变化状态
            sqlPartStr.Append(
                $"('{workdate}','{workshift}','{lineID}','{deviceID}','{msgID}.{lastBit}'," +
                $"'{lastBit}','{0}','{1}','{diffv}'," +
                $"'{nowTime:yyyy-MM-dd HH:mm:ss.fff}','{lastTime:yyyy-MM-dd HH:mm:ss.fff}',now()),"
                );

            // 更新当前点位变化状态
            sqlPartStr.Append(
                 $"('{workdate}','{workshift}','{lineID}','{deviceID}','{msgID}.{nowBit}'," +
                 $"'{3}','{1}','{0}','{diffv}'," +
                 $"'{nowTime:yyyy-MM-dd HH:mm:ss.fff}','{lastTime:yyyy-MM-dd HH:mm:ss.fff}',now()),"
                );

            return sqlPartStr.ToString();
        }

        /// <summary>
        /// 计算停机信息_新
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="FromdeviceID"></param>
        /// <param name="msgID"></param>
        /// <param name="msgBit"></param>
        private void getStopValueFromCH_New(string lineID, string fromLineID, string fromdeviceID, string deviceID, string msgID, string msgBit)
        {
            if (cts.Token.IsCancellationRequested) { return; }

            //Stopwatch watch = new Stopwatch();
            //watch.Start();

            StringBuilder SqlString = new StringBuilder("");
            try
            {
                // 前后各延长一分钟，处理跨班的设备状态
                string startTimeSubtractOneMinute = Convert.ToDateTime(startTime).AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss");
                string endTimeAddOneMinute = Convert.ToDateTime(endTime).AddMinutes(1).ToString("yyyy-MM-dd HH:mm:ss");

                string sqlhead = "insert into " + lineID + "_state (workdate,workshift,line_id,device_id,msg_id,state_label,pv,lpv,diffv,pt,lpt,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                string ssqlDel = "delete from " + lineID + "_state where workdate='" + workdate + "' and workshift='" + workshift + "'  and device_id='" + deviceID + "'";
                int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                string ssql = $"SELECT eventtime," +
                              $"bitTest(bitXor(`data`, 1768515945 - device_id * msg_id), {msgBit}) AS bit_0, " +
                              $"bitTest(bitXor(`data`, 1768515945 - device_id * msg_id), {1}) AS bit_1, " +
                              $"bitTest(bitXor(`data`, 1768515945 - device_id * msg_id), {2}) AS bit_2 " +
                              $"FROM {companyNum}.{fromLineID}{CHtable_name} l " +
                              $"WHERE eventtime >= '{startTimeSubtractOneMinute}' AND eventtime < '{endTimeAddOneMinute}' " +
                              $"AND device_id = {fromdeviceID} AND msg_id = {msgID} " +
                              $"ORDER BY eventtime";

                //LogConfig.Intence.WriteLog("RunLog\\S", "S", $"222：{ssql}");

                byte[] postData = Encoding.ASCII.GetBytes(ssql.ToString());
                string strResult = PostResponse(CHuser, CHpasswd, $"http://{CHserver}:8123/", ssql.ToString());
                if (strResult != "")
                {
                    //LogConfig.Intence.WriteLog("RunLog\\S", "S", $"111");

                    strResult = strResult.Remove(strResult.Count() - 1, 1);
                    string[] strArray = strResult.Split('\n');

                    MachineStatus machineStatus = MachineStatus.None; // 默认状态
                    int bitRunValue;     // 运行点位值
                    int bitStopValue;    // 故障停机点位值
                    int bitStandbyValue; // 待机点位值

                    // 初始化lastTime,为开始时间的前一分钟，不属于当班次时间
                    lastTime = Convert.ToDateTime(startTime).AddMinutes(-1);

                    // 找出当前时间段中，三个点位存在为1的第一个时间点数据，作为上一次数据
                    foreach (string str in strArray)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        string[] bitValue = str.Split('\t');

                        if (bitValue[1].Equals("1") || bitValue[2].Equals("1") || bitValue[3].Equals("1"))
                        {
                            bitRunValue = Convert.ToInt32(bitValue[1]);
                            bitStopValue = Convert.ToInt32(bitValue[2]);
                            bitStandbyValue = Convert.ToInt32(bitValue[3]);

                            /*
                             * 判断哪个为1，赋予对应枚举值（顺序可调整）
                             * 如果可能同时有多个值为 1，这里默认按优先顺序：Run > Stop > Standby；                             
                            */
                            if (bitRunValue == 1)
                            {
                                machineStatus = MachineStatus.Run;
                            }
                            else if (bitStopValue == 1)
                            {
                                machineStatus = MachineStatus.Stop;
                            }
                            else if (bitStandbyValue == 1)
                            {
                                machineStatus = MachineStatus.Standby;
                            }


                            #region 跨班次数据处理—开始时间
                            if (Convert.ToDateTime(bitValue[0]) <= Convert.ToDateTime(startTime))
                            {
                                // 遍历开始时间前一分钟内最接近开始时间之前的数据，时间设为开始时间
                                lastTime = Convert.ToDateTime(startTime);
                            }
                            else
                            {
                                // 遍历到开始时间之后的数据，如果lastTime!=开始时间
                                // 表示前一分钟没有遍历到数据，将lastTime设为当前数据时间
                                if (lastTime != Convert.ToDateTime(startTime))
                                {
                                    lastTime = DateTime.ParseExact(bitValue[0], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                                }

                                break; // 确定第一个数据，跳出循环
                            }

                            #endregion
                        }

                    }

                    // 遍历所有数据，计算每个状态持续时间
                    for (int i = 1; i < strArray.Count(); i++)
                    {
                        //if (deviceID == "14")
                        //{
                        //    LogConfig.Intence.WriteLog("RunLog\\S", "S", $"总数据量：{strArray.Count()}、 eventtime：{strArray[0]}、运行：{strArray[1]}、停机：{strArray[2]}、待机：{strArray[3]}\r\n\r\n");
                        //}

                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        string[] strBit = strArray[i].Split('\t');

                        DateTime nowTime = Convert.ToDateTime(strBit[0]);

                        #region 跨班次数据处理——结束时间
                        if (Convert.ToDateTime(strBit[0]) <= Convert.ToDateTime(startTime))
                        {
                            continue; // 不属于当班次的数据，跳过
                        }

                        if (Convert.ToDateTime(strBit[0]) >= Convert.ToDateTime(endTime))
                        {
                            // 遍历结束时间后一分钟内最接近结束时间之后的数据，时间设为结束时间
                            nowTime = Convert.ToDateTime(endTime);
                        }

                        #endregion

                        int nowRunValue = Convert.ToInt32(strBit[1]);     // 当前运行点位值
                        int nowStopValue = Convert.ToInt32(strBit[2]);    // 当前故障停机点位值
                        int nowStandbyValue = Convert.ToInt32(strBit[3]); // 当前待机点位值

                        // 判断当前各个状态哪个为1，且不是上一个状态
                        // 则更新状态，并记录状态变化时间，更新数据，更新当前所处状态
                        // 每次插入的是两条数据，一条是上一个状态，一条是当前状态
                        if (nowRunValue == 1 && machineStatus != MachineStatus.Run)
                        {
                            string sql = GetDeviceStateSqlStr(lineID, deviceID, msgID, nowTime, machineStatus, MachineStatus.Run);
                            SqlString.Append(sql);
                            Lcount += 2;

                            lastTime = nowTime;
                            machineStatus = MachineStatus.Run;
                        }
                        else if (nowStopValue == 1 && machineStatus != MachineStatus.Stop)
                        {
                            string sql = GetDeviceStateSqlStr(lineID, deviceID, msgID, nowTime, machineStatus, MachineStatus.Stop);
                            SqlString.Append(sql);
                            Lcount += 2;

                            lastTime = nowTime;
                            machineStatus = MachineStatus.Stop;
                        }
                        else if (nowStandbyValue == 1 && machineStatus != MachineStatus.Standby)
                        {
                            string sql = GetDeviceStateSqlStr(lineID, deviceID, msgID, nowTime, machineStatus, MachineStatus.Standby);
                            SqlString.Append(sql);
                            Lcount += 2;

                            lastTime = nowTime;
                            machineStatus = MachineStatus.Standby;
                        }

                        /*
                         * 当前遍历完最后一条数据后，再额外插入一条数据
                         * 插入数据：当前状态的点位数据，时间为最后一条数据的时间
                         * 即为当前查询时间段内，最后一个状态的持续时间
                         * 防止状态一直没变化导致当班次无数据                         
                        */
                        if (i == strArray.Count() - 1 || Convert.ToDateTime(strBit[0]) >= Convert.ToDateTime(endTime))
                        {
                            int nowBit = (int)machineStatus; // 状态位

                            double diffv = (nowTime - Convert.ToDateTime(lastTime)).TotalSeconds; // 持续时间

                            SqlString.Append(
                                $"('{workdate}','{workshift}','{lineID}','{deviceID}','{msgID}.{nowBit}'," +
                                $"'{nowBit}','1','1','{diffv}'," +
                                $"'{nowTime:yyyy-MM-dd HH:mm:ss.fff}','{lastTime:yyyy-MM-dd HH:mm:ss.fff}',now()),"
                                );

                            Lcount++;
                        }

                        if (Lcount > 1000)
                        {
                            string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                            int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                            SqlString = new StringBuilder(sqlhead);
                            Lcount = 0;
                        }

                        if (Convert.ToDateTime(strBit[0]) >= Convert.ToDateTime(endTime))
                        {
                            break; // 遍历到数据时间大于结束时间，结束遍历
                        }
                    }

                }


                // watch.Stop();
                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    AddListStr(workdate + " & " + workshift + " & " + deviceID + "停机信息处理完成！ " + DateTime.Now.ToString());

                }


            }
            catch (ThreadAbortException)
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                AddListStr("出错了！ ");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + "SQL.log", SqlString.ToString() + DateTime.Now.ToString() + "\r\n");
            }
        }

        /// <summary>
        /// 按小时统计停机
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="msgID"></param>
        private void getStopMinuteByHours(string lineID, string deviceID, string msgID)
        {
            if (cts.Token.IsCancellationRequested) { return; }

            //按小时统计停机时间
            // select* from line1001_state a where workdate = '2021-12-21' and workshift = 'night' and lpv = 0
            // getTodayAndShift();
            StringBuilder SqlString = new StringBuilder("");

            try
            {

                string sqlhead = "insert into " + lineID + "_stoptime (workdate,workshift,line_id,device_id,msg_id,lpv,pv,diffv,lpt,pt,stop_time,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                string ssqlDel = "delete from " + lineID + "_stoptime where workdate='" + workdate + "' and workshift='" + workshift + "'  and device_id='" + deviceID + "'";
                int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);

                string ssql = "  select lpv,pv,diffv,lpt,pt from " + lineID + "_state a where workdate='" + workdate + "' and device_id='" + deviceID + "' and msg_id='" + msgID + "' and workshift='" + workshift + "' and lpv = 0";
                DataSet ds = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssql);
                DataTable dt = ds.Tables[0];

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        DateTime lastStartTime = Convert.ToDateTime(dt.Rows[i]["lpt"]);
                        DateTime lastEndTime = Convert.ToDateTime(dt.Rows[i]["pt"]);

                        if (lastStartTime.Hour != lastEndTime.Hour)
                        {
                            DateTime newTimeStart = lastStartTime.Date.AddHours(lastStartTime.Hour);
                            DateTime newTimeEnd = lastStartTime.Date.AddHours(lastStartTime.Hour + 1);
                            SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "','0','0','" + Math.Round((newTimeEnd - lastStartTime).TotalSeconds).ToString() + "','" + lastStartTime.ToString() + "','" + newTimeEnd.ToString() + "','" + lastStartTime.ToString() + "',now()),");
                            for (int k = 1; k < 13; k++)
                            {
                                newTimeStart = lastStartTime.Date.AddHours(lastStartTime.Hour + k);
                                if (lastEndTime > lastStartTime.Date.AddHours(lastStartTime.Hour + k + 1))
                                {
                                    newTimeEnd = lastStartTime.Date.AddHours(lastStartTime.Hour + k + 1);
                                    SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "','0','0','" + Math.Round((newTimeEnd - newTimeStart).TotalSeconds).ToString() + "','" + newTimeStart.ToString() + "','" + newTimeEnd.ToString() + "','" + lastStartTime.ToString() + "',now()),");
                                    Lcount++;
                                }
                                else
                                {
                                    SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "','0','1','" + Math.Round((lastEndTime - newTimeStart).TotalSeconds).ToString() + "','" + newTimeStart.ToString() + "','" + lastEndTime.ToString() + "','" + lastStartTime.ToString() + "',now()),");
                                    Lcount++;
                                    k = 13;
                                }
                            }

                        }
                        else
                        {
                            SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + msgID + "','0','1','" + dt.Rows[i]["diffv"].ToString() + "','" + lastStartTime.ToString() + "','" + lastEndTime.ToString() + "','" + lastStartTime.ToString() + "',now()),");
                            Lcount++;
                        }


                        //msg_id,pv,lpv,diffv,pt,lpt,indate

                        //记录上次状态变化的时间

                        if (Lcount > 1000)
                        {
                            string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                            int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                            SqlString = new StringBuilder(sqlhead);
                            Lcount = 0;
                        }

                    }

                }

                if (Lcount > 0)
                {
                    string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                    int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);

                    AddListStr(workdate + " & " + workshift + " & " + deviceID + "按小时统计停机信息处理完成！ " + DateTime.Now.ToString());
                }

            }
            catch (ThreadAbortException)
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                AddListStr("出错了！ ");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + "SQL.log", SqlString.ToString() + DateTime.Now.ToString() + "\r\n");
            }
        }

        /// <summary>
        /// 统计班次报表
        /// </summary>
        /// <param name="lineID"></param>
        /// <param name="deviceID"></param>
        /// <param name="StopMsgID"></param>
        /// <param name="yield_leftMsgID"></param>
        /// <param name="yield_rightMsgID"></param>
        private void getReportByShift(string lineID, string deviceID, string StopMsgID, string yield_leftMsgID, string yield_rightMsgID)
        {
            if (cts.Token.IsCancellationRequested) { return; }

            StringBuilder SqlString = new StringBuilder("");
            try
            {
                //生成班次报表
                // getTodayAndShift();
                Stopwatch watch = new Stopwatch();
                watch.Start();

                string sqlhead = "insert into " + lineID + "_report (workdate,workshift,line_id,device_id,report_interval,stop_times,stop_duration,yield_left,yield_right, " +
                   " yield_total,electric_used,short_left,short_right,speed_left,speed_right,speed_total,stop_duration_linecontrol,stop_duration_shortcan,indate) values ";
                SqlString = new StringBuilder(sqlhead);
                int Lcount = 0;

                string ssql = "  select workshift,start_time,end_time,start_add_days,end_add_days,total_minute from shift_list a where  workshift='" + workshift + "' ";
                DataSet ds = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssql);
                DataTable dt = ds.Tables[0];


                string ssqlDel = "delete from " + lineID + "_report where workdate='" + workdate + "' and workshift='" + workshift + "'  and device_id='" + deviceID + "'"; ;
                int execCount = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssqlDel);


                if (dt.Rows.Count > 0)
                {

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        DateTime start_time = Convert.ToDateTime(workdate + " " + dt.Rows[i]["start_time"]).AddDays(Convert.ToInt32(dt.Rows[i]["start_add_days"])).ToUniversalTime();
                        DateTime end_time = Convert.ToDateTime(workdate + " " + dt.Rows[i]["end_time"]).AddDays(Convert.ToInt32(dt.Rows[i]["end_add_days"])).ToUniversalTime();
                        int total_minute = Convert.ToInt32(dt.Rows[i]["total_minute"]);
                        string report_interval = dt.Rows[i]["start_time"].ToString().Substring(0, 5) + "--" + dt.Rows[i]["end_time"].ToString().Substring(0, 5);


                        string stop_times = "0";
                        string stop_duration = "0";
                        string yield_left = "0";
                        string yield_right = "0";
                        string yield_total = "0";
                        string electric_used = "0";
                        string short_left = "0";
                        string short_right = "0";
                        string speed_left = "0";
                        string speed_right = "0";
                        string speed_total = "0";
                        string stop_duration_linecontrol = "0";
                        string stop_duration_shortcan = "0";


                        //查询停机次数
                        string ssqlQuery = "  select count(pv) as qty from " + lineID + "_state a " +
                            " where workdate = '" + workdate + "' and device_id='" + deviceID + "' and workshift = '" + workshift + "' and lpv = 0 and lpt>= '" + start_time + "' and lpt<= '" + end_time + "' and diffv>30 ";
                        DataSet dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        stop_times = dsQuery.Tables[0].Rows[0]["qty"].ToString();
                        //查询停机时长
                        ssqlQuery = "  select ifnull(sum(diffv),0)/60.0 as qty from " + lineID + "_stoptime a " +
                           " where workdate = '" + workdate + "' and device_id='" + deviceID + "' and workshift = '" + workshift + "' and msg_id = '" + StopMsgID + "' and lpt>= '" + start_time + "' and lpt< '" + end_time + "' and diffv>30  ";
                        dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        stop_duration = dsQuery.Tables[0].Rows[0]["qty"].ToString();
                        //线控引起的停机
                        //ssqlQuery = " select ifnull(sum(d.diffv),0)/60.0 as qty from line_bit a " +
                        //           " left join(select lpt, diffv from line1001_state " +
                        //          "  where workdate = '" + workdate + "' and workshift = '" + workshift + "' and lpv = 0) b on a.lpt < addtime(b.lpt, 1.5) and a.lpt > subtime(b.lpt, 1.5)" +
                        //          " left join line1001_stoptime d on b.lpt=d.stop_time " +
                        //          "  where a.workdate = '" + workdate + "' and a.workshift = '" + workshift + "' and a.lpv = 1" +
                        //          "  and b.diffv > 0 and a.msg_id = '"+linectlStopMsgID+"'" +
                        //          "  and d.lpt >='" + start_time + "' and d.lpt < '" + end_time + "' ";
                        //dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        //stop_duration_linecontrol = dsQuery.Tables[0].Rows[0]["qty"].ToString();
                        //断罐引起的停机
                        //ssqlQuery = " select ifnull(sum(d.diffv),0)/60.0 as qty from line_bit a " +
                        //           " left join(select lpt, diffv from line1001_state " +
                        //          "  where workdate = '" + workdate + "' and workshift = '" + workshift + "' and lpv = 0) b on a.lpt < addtime(b.lpt, 1.5) and a.lpt > subtime(b.lpt, 1.5)" +
                        //          "  left join line1001_stoptime d on b.lpt=d.stop_time" +
                        //          "  where a.workdate = '" + workdate + "' and a.workshift = '" + workshift + "' and a.lpv = 1" +
                        //          "  and b.diffv > 0 and a.msg_id in('21.12','21.27') " +
                        //          "  and d.lpt >='" + start_time + "' and d.lpt < '" + end_time + "' ";
                        //dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        //stop_duration_shortcan = dsQuery.Tables[0].Rows[0]["qty"].ToString();

                        //查询左产量351
                        ssqlQuery = "  select ifnull(sum(diffv),0) as qty from " + lineID + "_qty a " +
                           " where workdate = '" + workdate + "' and device_id='" + deviceID + "' and workshift = '" + workshift + "' and msg_id in (" + yield_leftMsgID + ") and pt>= '" + start_time + "' and  pt< '" + end_time + "' ";
                        dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        yield_left = dsQuery.Tables[0].Rows[0]["qty"].ToString();

                        //查询右产量352
                        ssqlQuery = "  select ifnull(sum(diffv),0) as qty from " + lineID + "_qty a " +
                           " where workdate = '" + workdate + "' and device_id='" + deviceID + "' and workshift = '" + workshift + "' and msg_id in (" + yield_rightMsgID + ") and pt>= '" + start_time + "' and  pt< '" + end_time + "' ";
                        dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        yield_right = dsQuery.Tables[0].Rows[0]["qty"].ToString();

                        yield_total = (Convert.ToInt32(yield_left) + Convert.ToInt32(yield_right)).ToString();

                        //查询用电量330
                        //ssqlQuery = "  select ifnull(sum(diffv),0) as qty from line1001_qty a " +
                        //   " where workdate = '" + workdate + "' and workshift = '" + workshift + "' and msg_id = '330' and pt>= '" + start_time + "' and  pt< '" + end_time + "' ";
                        //dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        //electric_used = (Convert.ToDouble(dsQuery.Tables[0].Rows[0]["qty"]) / 100.0).ToString();

                        //查询左断罐357
                        //ssqlQuery = "  select ifnull(sum(diffv),0) as qty from line1001_qty a " +
                        //   " where workdate = '" + workdate + "' and workshift = '" + workshift + "' and msg_id = '357' and pt>= '" + start_time + "' and  pt< '" + end_time + "' ";
                        //dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        //short_left = dsQuery.Tables[0].Rows[0]["qty"].ToString();
                        //查询右断罐358
                        //ssqlQuery = "  select ifnull(sum(diffv),0) as qty from line1001_qty a " +
                        //  " where workdate = '" + workdate + "' and workshift = '" + workshift + "' and msg_id = '358' and pt>= '" + start_time + "' and  pt< '" + end_time + "' ";
                        //dsQuery = ConfigHelper.GetDataSet(Conn_battery, CommandType.Text, ssqlQuery);
                        //short_right = dsQuery.Tables[0].Rows[0]["qty"].ToString();

                        if (total_minute - Convert.ToDouble(stop_duration) != 0)
                        {
                            speed_left = (Convert.ToInt32(yield_left) / (total_minute - Convert.ToDouble(stop_duration))).ToString();
                            speed_right = (Convert.ToInt32(yield_right) / (total_minute - Convert.ToDouble(stop_duration))).ToString();
                            speed_total = (Convert.ToInt32(yield_total) / (total_minute - Convert.ToDouble(stop_duration))).ToString();
                        }
                        else
                        {
                            speed_left = "0";
                            speed_right = "0";
                            speed_total = "0";
                        }

                        // workdate,workshift,report_interval,stop_times,stop_duration,
                        // yield_left,yield_right,yield_total,electric_used,short_left,short_right
                        // ,speed_left,speed_right,speed_total,indate
                        SqlString.Append("('" + workdate + "','" + workshift + "','" + lineID + "','" + deviceID + "','" + report_interval + "','" + stop_times + "','" + stop_duration + "','"
                            + yield_left + "','" + yield_right + "','" + yield_total + "','" + electric_used + "','" + short_left + "','" + short_right
                            + "','" + speed_left + "','" + speed_right + "','" + speed_total + "','" + stop_duration_linecontrol + "','" + stop_duration_shortcan + "',now()),");
                        Lcount++;

                    }

                    watch.Stop();
                    if (Lcount > 0)
                    {
                        string ssql2 = SqlString.Remove(SqlString.Length - 1, 1).ToString();
                        int aa = ConfigHelper.ExecuteNonQuery(Conn_battery, CommandType.Text, ssql2);
                    }

                    AddListStr(workdate + " & " + workshift + " & " + deviceID + "班次报表处理完成！ " + DateTime.Now.ToString());


                }
            }
            catch (ThreadAbortException)
            {
                // 忽略中止线程异常（关闭时，直接中止线程会抛出 ThreadAbortException异常）
            }
            catch (Exception ex)
            {
                AddListStr("出错了！ ");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + ".log", ex.ToString() + DateTime.Now.ToString() + "\r\n");
                System.IO.File.AppendAllText(".\\" + DateTime.Now.ToString("yyyyMMdd_") + "SQL.log", SqlString.ToString() + DateTime.Now.ToString() + "\r\n");
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
        private string PostResponse(string user, string password, string url, string postData)
        {
            AuthenticationHeaderValue authentication = new AuthenticationHeaderValue(
               "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")
               ));
            string result = string.Empty;
            HttpContent httpContent = new StringContent(postData);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = authentication;
                HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    Task<string> t = response.Content.ReadAsStringAsync();
                    result = t.Result;
                }
            }
            return result;
        }


        /// <summary>
        /// 统计停机
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            #region 注释
            //计算停机信息
            //getStopValueFromCH(line_id, "10", "10", "200", "4");   //基本盖
            //getStopValueFromCH(line_id, "11", "11", "203", "1");
            //getStopValueFromCH(line_id, "12", "12", "203", "1");
            //getStopValueFromCH(line_id, "13", "1", "200", "27");
            //getStopValueFromCH(line_id, "14", "1", "200", "28");
            //getStopValueFromCH(line_id, "15", "1", "200", "29");
            //getStopValueFromCH(line_id, "16", "1", "200", "30");
            //getStopValueFromCH(line_id, "17", "1", "200", "31");
            //getStopValueFromCH(line_id, "18", "1", "201", "0");
            //getStopValueFromCH(line_id, "19", "19", "200", "15");
            //getStopValueFromCH(line_id, "20", "20", "200", "15");
            //getStopValueFromCH(line_id, "21", "21", "201", "14");
            //getStopValueFromCH(line_id, "22", "22", "201", "14");
            //getStopValueFromCH(line_id, "23", "23", "201", "14");
            //getStopValueFromCH(line_id, "24", "24", "201", "14"); 
            #endregion

        }

        /// <summary>
        /// 开始统计
        /// </summary>
        public void button2_Click(object sender, EventArgs e)
        {
            if (!isAnalyzing)
            {
                getTodayAndShift();
                isStartExec10 = true;

                AddListStr("开始处理！ " + DateTime.Now.ToString());

            }
            else
            {
                AddListStr("正在处理中，请稍后！ " + DateTime.Now.ToString());
            }


        }

        /// <summary>
        /// 按小时统计停机
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            #region 注释
            ////按小时统计停机
            //getTodayAndShift();

            //getStopMinuteByHours(line_id, "10", "200.4");   //基本盖
            //getStopMinuteByHours(line_id, "11", "203.1");
            //getStopMinuteByHours(line_id, "12", "203.1");
            //getStopMinuteByHours(line_id, "13", "200.27");
            //getStopMinuteByHours(line_id, "14", "200.28");
            //getStopMinuteByHours(line_id, "15", "200.29");
            //getStopMinuteByHours(line_id, "16", "200.30");
            //getStopMinuteByHours(line_id, "17", "200.31");
            //getStopMinuteByHours(line_id, "18", "201.0");
            //getStopMinuteByHours(line_id, "19", "200.15");
            //getStopMinuteByHours(line_id, "20", "200.15");

            //getStopMinuteByHours(line_id, "21", "201.14");
            //getStopMinuteByHours(line_id, "22", "201.14");
            //getStopMinuteByHours(line_id, "23", "201.14");
            //getStopMinuteByHours(line_id, "24", "201.14"); 
            #endregion
        }

        /// <summary>
        /// 统计产量
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            #region 注释
            //统计产量

            //getQtyFromCHbyMinute(line_id, "10", "10", "3009");//统计产量- 基本盖

            ////  GetQtyByMinute(parm, "11", "3182");//统计产量-转换台A
            ////  GetQtyByMinute(parm, "12", "3182");//统计产量-转换台B

            //getQtyFromCHbyMinute(line_id, "13", "1", "3110");//统计产量-注胶机1
            //getQtyFromCHbyMinute(line_id, "14", "1", "3111");//统计产量-注胶机2
            //getQtyFromCHbyMinute(line_id, "15", "1", "3112");//统计产量-注胶机3
            //getQtyFromCHbyMinute(line_id, "16", "1", "3113");//统计产量-注胶机4
            //getQtyFromCHbyMinute(line_id, "17", "1", "3114");//统计产量-注胶机5
            //getQtyFromCHbyMinute(line_id, "18", "1", "3115");//统计产量-注胶机6

            //getQtyFromCHbyMinute(line_id, "19", "19", "3032");//统计产量-组合冲1
            //getQtyFromCHbyMinute(line_id, "19", "19", "3033");//统计产量-组合冲1
            //getQtyFromCHbyMinute(line_id, "20", "20", "3032");//统计产量-组合冲2
            //getQtyFromCHbyMinute(line_id, "20", "20", "3033");//统计产量-组合冲2

            //getQtyFromCHbyMinute(line_id, "21", "21", "3198");//统计产量-打包机1
            //getQtyFromCHbyMinute(line_id, "22", "22", "3198");//统计产量-打包机2
            //getQtyFromCHbyMinute(line_id, "23", "23", "3198");//统计产量-打包机3
            //getQtyFromCHbyMinute(line_id, "24", "24", "3198");//统计产量-打包机4

            //getQtyFromCHbyMinute(line_id, "21", "21", "3199");//统计产量-打包机1
            //getQtyFromCHbyMinute(line_id, "22", "22", "3199");//统计产量-打包机2
            //getQtyFromCHbyMinute(line_id, "23", "23", "3199");//统计产量-打包机3
            //getQtyFromCHbyMinute(line_id, "24", "24", "3199");//统计产量-打包机4

            //getQtyFromCHbyMinute(line_id, "21", "21", "3200");//统计产量-打包机1
            //getQtyFromCHbyMinute(line_id, "22", "22", "3200");//统计产量-打包机2
            //getQtyFromCHbyMinute(line_id, "23", "23", "3200");//统计产量-打包机3
            //getQtyFromCHbyMinute(line_id, "24", "24", "3200");//统计产量-打包机4 
            #endregion
        }

        /// <summary>
        /// 班次报表
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            #region 注释
            ////班次报表
            //getTodayAndShift();

            //getReportByShift(line_id, "10", "200.4", "3009", "0");   //基本盖
            //getReportByShift(line_id, "11", "203.1", "0", "0");     //
            //getReportByShift(line_id, "12", "203.1", "0", "0");     //
            //getReportByShift(line_id, "13", "200.27", "3110", "0"); //注胶机1
            //getReportByShift(line_id, "14", "200.28", "3111", "0"); //注胶机2
            //getReportByShift(line_id, "15", "200.29", "3112", "0");//注胶机3
            //getReportByShift(line_id, "16", "200.30", "3113", "0");//注胶机4
            //getReportByShift(line_id, "17", "200.31", "3114", "0");//注胶机5
            //getReportByShift(line_id, "18", "201.0", "3115", "0");//注胶机6
            //getReportByShift(line_id, "19", "200.15", "3032", "3033"); //组合冲1
            //getReportByShift(line_id, "20", "200.15", "3032", "3033");  //组合冲2

            //getReportByShift(line_id, "21", "201.14", "3198,3199,3200", "0"); //打包机1
            //getReportByShift(line_id, "22", "201.14", "3198,3199,3200", "0");//打包机2
            //getReportByShift(line_id, "23", "201.14", "3198,3199,3200", "0");//打包机3
            //getReportByShift(line_id, "24", "201.14", "3198,3199,3200", "0");//打包机4 
            #endregion

        }

        /// <summary>
        /// 界面日志显示
        /// </summary>
        /// <param name="output"></param>
        private void AddListStr(string output)
        {
            if (!checkBox_Stats.Checked)
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(delegate
                    {
                        if (listBox1.Items.Count > 300)
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
        private void frm_main_yzyl_Resize(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;//窗体填充所在容器控件
            listBox1.Width = this.Width;
            //listBox1.Height = this.Height - (comboBox1.Height + comboBox1.Location.Y) - 20;
            //listBox1.Location = new Point(10, comboBox1.Location.Y + comboBox1.Height + 20);

            label1.Location = new Point(10, 10);
            checkBox_Stats.Location = new Point(this.Width - 15 - checkBox_Stats.Width, 10);

            listBox1.Height = this.Height - (checkBox_Stats.Height + checkBox_Stats.Location.Y);
            listBox1.Location = new Point(0, checkBox_Stats.Location.Y + checkBox_Stats.Height + 5);
        }

        /// <summary>
        /// 停止解析，关闭线程
        /// </summary>
        public void StopService()
        {
            try
            {
                cts.Cancel();          // 通知线程取消操作
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

                if (thread10 != null && thread10.IsAlive)
                {
                    //thread10.Join();
                    if (!thread10.Join(150))
                    {
                        thread10.Abort(); // 若正在解析数据，直接终止线程
                    }
                }
                thread10 = null;
                cts.Dispose();
                cts = null;
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog", "Thread", $"{ex.Message}");
            }
        }
    }


    /// <summary>
    /// 枚举：设备运行状态
    /// </summary>
    public enum MachineStatus
    {
        Run = 0,       // 运行
        Stop = 1,      // 故障停机
        Standby = 2,   // 待机
        None = 3
    }
}
