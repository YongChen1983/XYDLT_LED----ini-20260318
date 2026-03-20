using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading;
namespace XYDLT_LED
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
     

        IniFiles iniFile;
        /// <summary>
        /// 生产1/2/3线作息时间点
        /// </summary>
        string[] iniFile_ANDON_TIMEPOINT_L1 = new string[20];
        /// <summary>
        /// P32R线作息时间点
        /// </summary>
        string[] iniFile_ANDON_TIMEPOINT_L4 = new string[20];
        /// <summary>
        /// 安灯默认显示字符
        /// </summary>
        string Andon_text_str;
        private void Form1_Load(object sender, EventArgs e)
        {
            StrConn = ConfigurationManager.ConnectionStrings["StrConn"].ConnectionString;
            Andon_text_str = ConfigurationManager.AppSettings["Andon_text_str"].ToString();
            string iniPath = Path.Combine(Application.StartupPath, "Setting.ini");
            iniFile = new IniFiles(iniPath);
            // 优先从 Setting.ini 的 [ANDON] Text 读取滚动显示内容（UTF-8 读取避免乱码）
            if (iniFile.ExistINIFile())
            {
                string iniText = GetAndonTextFromIniUtf8(iniPath);
                if (string.IsNullOrEmpty(iniText))
                {
                    string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                    if (!string.IsNullOrEmpty(exeDir) && exeDir != Application.StartupPath)
                        iniText = GetAndonTextFromIniUtf8(Path.Combine(exeDir, "Setting.ini"));
                }
                if (!string.IsNullOrEmpty(iniText))
                    Andon_text_str = iniText.Trim();
            }
            // 原数据从右侧开始，以便从右向左滚动显示
            ANDON_Mess.Text = ANDON_Mess_2.Text = Andon_text_str;
            int startX = this.ClientSize.Width;
            ANDON_Mess.Location = new System.Drawing.Point(startX, ANDON_Mess.Location.Y);
            ANDON_Mess_2.Location = new System.Drawing.Point(startX, ANDON_Mess_2.Location.Y);
            // 独立定时器做平滑滚动：约 50ms 一帧，每帧 2 像素，避免一卡一卡
            timerAndonScroll = new System.Windows.Forms.Timer(this.components);
            timerAndonScroll.Interval = 50;
            timerAndonScroll.Tick += (s, ev) => { if (!ANDON_HasData) StringMove(2); };
            timerAndonScroll.Start();
            timer2_Tick(new object (),new EventArgs ());
        }

        /// <summary>
        /// 用 UTF-8 编码从 ini 中读取 [ANDON] 节的 Text 键，避免中文乱码
        /// </summary>
        private static string GetAndonTextFromIniUtf8(string iniPath)
        {
            try
            {
                if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath))
                    return "";
                // 用 UTF-8 读取，并去掉 BOM，避免首行解析错误
                byte[] bytes = File.ReadAllBytes(iniPath);
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    bytes = bytes.Skip(3).ToArray();
                string content = Encoding.UTF8.GetString(bytes);
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool inAndon = false;
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith(";"))
                        continue;
                    if (trimmed.StartsWith("["))
                    {
                        inAndon = string.Equals(trimmed, "[ANDON]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if (!inAndon) continue;
                    // 支持 Text=值 或 Text = 值
                    int idx = trimmed.IndexOf('=');
                    if (idx < 0) continue;
                    string key = trimmed.Substring(0, idx).Trim();
                    if (!string.Equals(key, "Text", StringComparison.OrdinalIgnoreCase)) continue;
                    return trimmed.Substring(idx + 1).Trim();
                }
            }
            catch { }
            return "";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string a, b, c;

            a = DateTime.Now.Month.ToString();
            b = DateTime.Now.Day.ToString();
            c = DateTime.Now.ToShortTimeString();
            DateTXT.Text = a + "-" + b;
            DateTXT2.Text = DateTXT.Text;
            TimeTxt.Text = c;
            TimeTxt2.Text = TimeTxt.Text;
            // 滚动由 timerAndonScroll 单独处理，此处只更新时间
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            int Y = 18;
            Graphics g = this.CreateGraphics();
            Pen p = new Pen(Color.Green, 1);
            for (int i = 1; i <= 6; i++)
            {
                g.DrawLine(p, 0, Y * i, this.Width, Y * i);
            }
            for (int i = 1; i <= 6; i++)
            {
                g.DrawLine(p, 0, Y * i + 128, this.Width, Y * i + 128);
            }

        }


        /// <summary>
        /// 有 ANDON 呼叫数据时为 true，停止滚动并居中显示；无数据时为 false，显示原内容并滚动
        /// </summary>
        bool ANDON_HasData = false;
        /// <summary>
        /// 滚动文字用定时器，短间隔小步长实现平滑滚动
        /// </summary>
        System.Windows.Forms.Timer timerAndonScroll;
        /// <summary>
        /// 字符移动动画（从右到左滚动，循环）
        /// </summary>
        /// <param name="x">每次移动的像素个数</param>
        private void StringMove(int x)
        {
            int tem = ANDON_Mess.Location.X;
            tem -= x;   // 从右到左
            if (tem < -ANDON_Mess.Width)
                tem = this.ClientSize.Width;   // 移出左边界后从右侧重新出现
            ANDON_Mess.Location = new System.Drawing.Point(tem, ANDON_Mess.Location.Y);
            ANDON_Mess_2.Location = new System.Drawing.Point(tem, ANDON_Mess_2.Location.Y);
        }


        string StrConn;
        /// <summary>
        /// 当班计划查询
        /// </summary>
        
        string SQL_str_DBJH = " select VEHICLE_CLASS_CODE AS 车型, COUNT (VEHICLE_CLASS_CODE) AS 当班计划 from(select DISTINCT  VEHICLE_CLASS_CODE, CAR_NO from [MES_XY_OTR].[MES].TT_APS_WORK_ORDER where DateDiff(dd, PRODUCT_DATE, GETDATE())=0 and STATUS !='90' and STATUS !='130') TEM group by VEHICLE_CLASS_CODE";
        /// <summary>
        /// 当前实际入库
        /// </summary>
        string SQL_str_DQSJ = "SELECT VEHICLE_CLASS_CODE AS 车型, (case VEHICLE_CLASS_CODE when  'L53H'  THEN  count(*) when  'L42P' THEN count(*)  when  'P32R' THEN count(*) when   'P42R'  THEN count(*)/3  when   'P42Q'  THEN count(*)/3   END) AS 入库数量 from(SELECT VEHICLE_CLASS_CODE, TRAY_NO FROM [MES_XY_OTR].[MES].[TT_WM_ROLL_STOCK] where DateDiff(dd,[IN_STOCK_TIME],GETDATE())=0  group by  TRAY_NO,VEHICLE_CLASS_CODE) Tem GROUP BY VEHICLE_CLASS_CODE";
       /// <summary>
       /// 获取按车号排列的当班计划列表（包括P32R）
       /// </summary>
        string SQL_str_DBJH_list = "select DISTINCT  VEHICLE_CLASS_CODE as 车型,CAR_NO as 车号 from [MES_XY_OTR].[MES].TT_APS_WORK_ORDER where DateDiff(dd,PRODUCT_DATE,GETDATE())=0  and STATUS != '90' and STATUS !='130' order by CAR_NO";
        string SQL_str_GetUPH = "SELECT ASSEMBLY_LINE  ,ASSEMBLY_LINE_NAME,JPH as UPH FROM [MES_XY_OTR].[MES].[TM_BAS_ASSEMBLY_LINE] WHERE  VALID_FLAG=1 and  ID  IN (10012,10020)";
        
        /// <summary>
        /// 生产1/2/3车型当班计划
        /// </summary>
        int DBJH_L1_COUNT = 0;
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (Conn_DB())
            {
                //   Get_Setting(out ANDON_TimePoint_L1, out ANDON_TimePoint_L4, out Takt_L1, out Takt_L4);
                Get_Setting(out ANDON_TimePoint_L1, out ANDON_TimePoint_L4 );

                //Get_DQJH_Count(Convert.ToInt32(DBJH_R.Text), Takt_L4, ANDON_TimePoint_L4, out ANDON_DQJH_L4);//  P32R计划
                //Get_DQJH_Count(Convert.ToInt32(DBJH_R.Text), Takt_L1, ANDON_TimePoint_L1, out ANDON_DQJH_L1);  //20251025 BY CY
                Get_DQJH_Count(DBJH_L1_COUNT, Takt_L1, ANDON_TimePoint_L1, out ANDON_DQJH_L1);//  1/2/3线计划
                DQJH_L1_COUNT(ds.Tables["DQJH_L1_LIST"]);
                UI_Refresh();
            }
        }
        DataSet ds;
        /// <summary>
        /// 连接MES数据库，获取数据
        /// </summary>
        private bool  Conn_DB()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(StrConn))
                {
                     ds = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter(SQL_str_DBJH, connection);
                    adapter.Fill(ds, "DBJH");//当班计划                                        
                     
                    adapter.SelectCommand.CommandText = SQL_str_DQSJ;
                    adapter.Fill(ds, "DQSJ");//当前实际入库
                 
                  adapter.SelectCommand.CommandText = SQL_str_DBJH_list;
                  adapter.Fill(ds, "DQJH_L1_LIST");//生产1/2/3线的车型清单

                    adapter.SelectCommand.CommandText = SQL_str_GetUPH;
                    adapter.Fill(ds, "UPH");//生产1/2/3线的车型清单

                    GET_DBJH(ds);
                    GET_DQSJ(ds);
                    GET_UPH(ds.Tables ["UPH"]);
                    UI_Refresh();
                }
                if (ANDON_Mess.Text == "连接服务器出错") ANDON_Mess.Text = "";
                return true ;
            }
            catch (Exception ex)
            {
                ANDON_Mess.Text = "连接服务器出错";
                ANDON_Mess.ForeColor = Color.Red;
                foreach (Control item in this.Controls)
                {
                    if (item is Label)
                    {
                        Label tem = (Label)item;
                        if ((string)tem.Tag == "int")
                        {
                            tem.Text = "/";
                            tem.ForeColor = Color.White;
                        }
                    }
                }
                return false;
            }
        }

        public string str_DBJH_H, str_DBJH_M, str_DBJH_R, str_DBJH_Q;
        public string str_DQSJ_H, str_DQSJ_M, str_DQSJ_R, str_DQSJ_Q;
        public int int_DQJH_H, int_DQJH_M, int_DQJH_Q,int_DQJH_R;
        /// <summary>
        /// 获取并分解当班计划
        /// </summary>
        /// <param name="ds"></param>
        private void GET_DBJH(DataSet ds)
        {
            str_DBJH_H = str_DBJH_M = str_DBJH_Q = str_DBJH_R = "0";
            if (ds.Tables ["DBJH"].Rows.Count==0)
            {              
                return;
            }
            DataRow[] rows = ds.Tables["DBJH"].Select();
            foreach (DataRow DR in rows)
            {
                switch (DR["车型"].ToString())
                {
                    case "P42R":
                        str_DBJH_H = DR["当班计划"].ToString ();
                        break;
                    case "L42P":
                        str_DBJH_M = DR["当班计划"].ToString();
                        break;
                    case "P42Q":
                        str_DBJH_Q = DR["当班计划"].ToString();
                        break;
                    case "P32R":
                        str_DBJH_R = DR["当班计划"].ToString();
                        break;
                }
            }
            DBJH_L1_COUNT = Convert.ToInt32(str_DBJH_H) + Convert.ToInt32(str_DBJH_M) + Convert.ToInt32(str_DBJH_Q);
        }
        /// <summary>
        /// 将当前计划3车型分解
        /// </summary>
        /// <param name="dt"></param>
        private void DQJH_L1_COUNT(DataTable dt)
        {
            int_DQJH_H = int_DQJH_M = int_DQJH_Q = 0;
            if (DBJH_L1_COUNT ==0|| dt.Rows.Count ==0)
            {
                return;
            }
            int Andon_L1 =(int) Math.Floor (ANDON_DQJH_L1);
            if (Andon_L1 >= DBJH_L1_COUNT)
            {
                Andon_L1 = DBJH_L1_COUNT;
                
            }
            for (int i = 0; i < Andon_L1; i++)
            {
                switch (dt .Rows [i]["车型"].ToString ())
                {
                    case "P42R":
                        //str_DBJH_H = DR["当班计划"].ToString();
                        int_DQJH_H++;
                        break;
                    case "L42P":
                        int_DQJH_M++;
                        //str_DBJH_M = DR["当班计划"].ToString();
                        break;
                    case "P42Q":
                        int_DQJH_Q++;
                        //  str_DBJH_Q = DR["当班计划"].ToString();
                        break;
                    /*case "P32R":
                        int_DQJH_R++;
                        break;*/
                }
            }
        }
        private void GET_UPH(DataTable DT)
        {

            float  L1_tem =0, L4_tem=0;//获得MES中的UPH
            DataRow[] rows = DT.Select();
            foreach (DataRow DR in rows)
            {
                switch (DR["ASSEMBLY_LINE"].ToString())
                {
                    case "FS01":
                        L1_tem = Convert.ToSingle(DR["UPH"].ToString());
                        break;
                    case "RS04":
                        L4_tem = Convert.ToSingle(DR["UPH"].ToString());
                        
                        break;
                }
            }
            Takt_L1 = 60 / L1_tem;//获得1/2/3线生产节拍
            Takt_L4 = 60 / L4_tem;//获得4线生产节拍
            L1ToolStripMenuItem.Text = "L1/L2/L3:" + Takt_L1.ToString();
            l4ToolStripMenuItem.Text ="L4:"+ Takt_L4.ToString();
        }

        /// <summary>
        /// 获取当天实际入库数量
        /// </summary>
        /// <param name="ds"></param>
        private void GET_DQSJ(DataSet ds)
        {
            str_DQSJ_H = str_DQSJ_M = str_DQSJ_Q = str_DQSJ_R = "0";
            
            if (ds.Tables["DQSJ"].Rows.Count == 0)
            {               
                return;
            }
            DataRow[] rows = ds.Tables["DQSJ"].Select();
            foreach (DataRow DR in rows)
            {
                switch (DR["车型"].ToString())
                {
                    case "P42R":
                        str_DQSJ_H = DR["入库数量"].ToString();
                        break;
                   case "L42P":
                        str_DQSJ_M = DR["入库数量"].ToString();
                        break;
                    case "P42Q":
                        str_DQSJ_Q = DR["入库数量"].ToString();
                        break;
                    /*case "P32R":
                       str_DQSJ_R = DR["入库数量"].ToString();
                        break;*/
                }
            }
        }
        /// <summary>
        /// 生产1/2/3线生产时间点
        /// </summary>
        public TimeSpan[] ANDON_TimePoint_L1 = new TimeSpan[20];
        /// <summary>
        /// 生产4线生产时间点
        /// </summary>
        public TimeSpan[] ANDON_TimePoint_L4 = new TimeSpan[20];

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public int Andon_index;//多条信息时指针
        private void GetANDON_MESSG_Tick(object sender, EventArgs e)//获取呼叫信息并展示
        {
            DataSet DS_ANDON = new DataSet();
            DataTable DT;
            
            string str = @"select PLAY_TEXT as 呼叫内容,START_TIME as 开始时间,END_TIME AS 停止时间 ,ASSEMBLY_LINE AS 线体,(case ANDON_TYPE  WHEN '40' THEN '质量呼叫'  WHEN '30' THEN '缺料呼叫'  WHEN '10' THEN '维修呼叫'   WHEN '20' THEN '呼叫班长' END) AS 呼叫类型 from [MES].[TL_SYS_ANDON_LOG] where ANDON_STATUS =20  order by 开始时间 ";
            try
            {
                using (SqlConnection connection = new SqlConnection(StrConn))
                {

                    SqlDataAdapter adapter = new SqlDataAdapter(str, connection);
                    adapter.Fill(DS_ANDON, "ANDON");//获取andon呼叫实时信息   

                    DT = DS_ANDON.Tables["ANDON"];

                    if ( DT.Rows.Count  >= 1)
                    {
                        ANDON_HasData = true;
                        string[] ANDON_MESS = new string[DT.Rows.Count];
                        for (int i = 0; i < DT.Rows.Count; i++)
                        {
                            ANDON_MESS[i] = DT.Rows[i]["呼叫内容"].ToString();
                        }
                       
                        
                        ANDON_Mess.ForeColor = ANDON_Mess_2.ForeColor= Color.Red ;
                        if ((DT.Rows.Count ==1) || (Andon_index > (DT.Rows.Count - 1)))
                        {
                            Andon_index =0;
                        }

                        //根据指示刷新显示
                        if (DT.Rows.Count > 1)
                        {
                            ANDON_Mess.Text = ANDON_Mess_2.Text = "";
                            this.Refresh();
                            Thread.Sleep(100);
                            Application.DoEvents();
                          
                        }
                        ANDON_Mess.Text = ANDON_Mess_2.Text = ANDON_MESS[Andon_index].ToString();


                        Andon_index++;
                        // 有数据时停止滚动，居中显示
                        int cx = Math.Max(0, (this.ClientSize.Width - ANDON_Mess.Width) / 2);
                        ANDON_Mess.Location = new System.Drawing.Point(cx, ANDON_Mess.Location.Y);
                        ANDON_Mess_2.Location = new System.Drawing.Point(cx, ANDON_Mess_2.Location.Y);
                    }
                    else
                    {
                        bool wasHasData = ANDON_HasData;
                        ANDON_HasData = false;
                        ANDON_Mess.Text = ANDON_Mess_2.Text = Andon_text_str;
                        ANDON_Mess.ForeColor = ANDON_Mess_2.ForeColor = Color.Yellow;
                        // 仅当从“有数据”变为“无数据”时从右侧开始滚动，避免每 4 秒重置导致无法连续滚动
                        if (wasHasData)
                        {
                            int startX = this.ClientSize.Width;
                            ANDON_Mess.Location = new System.Drawing.Point(startX, ANDON_Mess.Location.Y);
                            ANDON_Mess_2.Location = new System.Drawing.Point(startX, ANDON_Mess_2.Location.Y);
                        }
                    }
                }

            }
            catch
            {
                ANDON_HasData = true;
                ANDON_Mess.Text = ANDON_Mess_2.Text = "连接服务器出错";
                ANDON_Mess.ForeColor = ANDON_Mess_2.ForeColor = Color.Red;
                // 错误信息居中显示，停止滚动
                int cx = Math.Max(0, (this.ClientSize.Width - ANDON_Mess.Width) / 2);
                ANDON_Mess.Location = new System.Drawing.Point(cx, ANDON_Mess.Location.Y);
                ANDON_Mess_2.Location = new System.Drawing.Point(cx, ANDON_Mess_2.Location.Y);
            }
         }

        private void ANDON_Mess_2_Click(object sender, EventArgs e)
        {

        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            //参观版当前实际
            //DQSJ_H.Text = DQSJ_H_2.Text = int_DQJH_H.ToString();
            //DQSJ_M.Text = DQSJ_M_2.Text = int_DQJH_M.ToString();
           // DQSJ_R.Text = DQSJ_R_2.Text = Convert.ToString((int)ANDON_DQJH_L4);
           // DQSJ_Q.Text = DQSJ_Q_2.Text = DBJH_Q.Text;
            //timer3.Enabled = false;

            //当前差值           
           // GZJS(DQJH_H, DQSJ_H, DQCZ_H, DQCZ_H_2);
           // GZJS(DQJH_M, DQSJ_M, DQCZ_M, DQCZ_M_2);
            //GZJS(DQJH_R, DQSJ_R, DQCZ_R, DQCZ_R_2);
           //GZJS(DQJH_Q, DQSJ_Q, DQCZ_Q, DQCZ_Q_2);

        }

        private void ANDON_Mess_Click(object sender, EventArgs e)
        {

        }

        private void DQCZ_R_Click(object sender, EventArgs e)
        {

        }

        private void DQJH_R_Click(object sender, EventArgs e)
        {

        }

        private void DQSJ_M_2_Click(object sender, EventArgs e)
        {

        }

        private void DQJH_H_2_Click(object sender, EventArgs e)
        {

        }

        private void DQJH_Q_2_Click(object sender, EventArgs e)
        {

        }

        private void label44_Click(object sender, EventArgs e)
        {

        }

        private void DQJH_R_2_Click(object sender, EventArgs e)
        {

        }

        public float Takt_L1, Takt_L4;
        /// <summary>
        /// 获取设置
        /// </summary>
        /// <param name="ANDON_TIMEPOINT_L1">生产1/2/3线生产时间点</param>
        /// <param name="ANDON_TIMEPOINT_L4">生产4线生产时间点</param>

        private void Get_Setting(out TimeSpan[] ANDON_TIMEPOINT_L1, out TimeSpan[] ANDON_TIMEPOINT_L4)
        {
            String[] str = new string[14];
           
            
            
            if (iniFile.ExistINIFile())
            {
                str = IniFiles.INIGetAllItems(Application.StartupPath + @"\Setting.ini", "ANDON-L1");
            }
            ANDON_TIMEPOINT_L1 = new TimeSpan[str.Length];
           
            for (int i = 0; i < str.Length; i++)
            {
                string[] sArray = str[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (sArray.Length == 2)
                {                    
                    ANDON_TIMEPOINT_L1[i] = TimeSpan.Parse(sArray[1].ToString());
                }
                else break;
            }

            str = IniFiles.INIGetAllItems(Application.StartupPath + @"\Setting.ini", "ANDON-L4");
            ANDON_TIMEPOINT_L4 = new TimeSpan[str.Length];
            // 获取4线时间点
            for (int i = 0; i < str.Length ; i++)
            {
                string[] sArray = str[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (sArray.Length == 2)
                {
                    ANDON_TIMEPOINT_L4[i] = TimeSpan.Parse(sArray[1].ToString());
                }
                else break;
            }
            //获取节拍
            try
            {
                //takt_L1 = (float)Convert.ToDouble(IniFiles.INIGetStringValue(Application.StartupPath + @"\Setting.ini", "TaktTime", "L1", " "));
                //takt_L4 = (float)Convert.ToDouble(IniFiles.INIGetStringValue(Application.StartupPath + @"\Setting.ini", "TaktTime", "L4", " "));
            }
            catch (Exception)
            {

                throw;
            }
            

        }
        
        /// <summary>
        /// P32R线的当前计划
        /// </summary>
        double ANDON_DQJH_L4;
 
        /// <summary>
        /// 生产1/2/3线的当前计划
        /// </summary>
        double ANDON_DQJH_L1;

        

        /// <summary>
        /// 获取开班后按节拍的当前计划
        /// </summary>
        /// <param name="DBJH">排产计划数量</param>
        /// <param name="Time_point">设置的生产及休息时间体制</param>
        /// <param name="count">返回当前计划应生产台数</param>
        /// <returns></returns>
        private bool Get_DQJH_Count(int DBJH,double takt, TimeSpan[] Time_point, out double count)
        {
           double  count_TotalMin = 0;
            count = 0;
            if (DBJH == 0)
            {                
                return false;                
            }
            double count_10 = ExecDateMinuteDiff(Time_point[0], Time_point[1]);//截至10：00分钟数
            double count_12 = ExecDateMinuteDiff(Time_point[2], Time_point[3]) + count_10;//截至12：00分钟数
            double count_15 = ExecDateMinuteDiff(Time_point[4], Time_point[5]) + count_12;//截至15：00分钟数
            double count_17 = ExecDateMinuteDiff(Time_point[6], Time_point[7]) + count_15; //截至17：00分钟数
            double count_19 = ExecDateMinuteDiff(Time_point[8], Time_point[9]) + count_17;//截至19：00分钟数
            TimeSpan CurrentTime = DateTime.Now.TimeOfDay;
            TimeSpan StartTime = Time_point[0];
            TimeSpan EndTime= Time_point[0];
            foreach (TimeSpan item in Time_point)
            {
                if (TimeSpan.Compare(item, EndTime) >= 0)
                {
                    EndTime = item.Duration();
                }
            }

            if (TimeSpan.Compare(CurrentTime, StartTime) == -1 ) //未到生产时间
            {            
                return false;
            }
            if (TimeSpan.Compare(CurrentTime, EndTime) >= 0) //已过生产时间
            {
                count = count_19 / takt;
                if (count > DBJH)
                {
                    count = DBJH;
                } 



                return false;
            }
            
            if (TimeSpan.Compare(CurrentTime, StartTime) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[1]) <= 0)//8:00[0]--10:00
            {
                count_TotalMin = ExecDateMinuteDiff( StartTime, CurrentTime);
            }
             
            if (TimeSpan.Compare(CurrentTime, Time_point[1]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[2]) <= 0)//10:00[1]--10:10 休息时间
            {
                count_TotalMin = count_10;
            }
           
            if (TimeSpan.Compare(CurrentTime, Time_point[2]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[3]) <= 0)//10:10[2]--12:00[3]
            {
                count_TotalMin = count_10 + ExecDateMinuteDiff(Time_point[2],CurrentTime);
            }
           
            if (TimeSpan.Compare(CurrentTime, Time_point[3]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[4]) <= 0)//12:00[3]--13:00[4] 休息时间
            {                
                count_TotalMin = count_12;
            }
            if (TimeSpan.Compare(CurrentTime, Time_point[4]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[5]) <= 0)//13:00[4]--15:00[5]
            {
                count_TotalMin =  ExecDateMinuteDiff(Time_point[4], CurrentTime)+ count_12;
            }
            
            if (TimeSpan.Compare(CurrentTime, Time_point[5]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[6]) <= 0)//15:00[5]--15:10[6] 休息时间
            {
                
                count_TotalMin = count_15 ;
            }
            if (TimeSpan.Compare(CurrentTime, Time_point[6]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[7]) <= 0)//15:10[6]--17:10[7]
            {
                count_TotalMin = ExecDateMinuteDiff(Time_point[6], CurrentTime) + count_15;
            }
            //
            if (TimeSpan.Compare(CurrentTime, Time_point[7]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[8]) <= 0)//17:10[7]--17:30[8] 休息时间
            {

                count_TotalMin = count_17;
            }
            if (TimeSpan.Compare(CurrentTime, Time_point[8]) >= 0 && TimeSpan.Compare(CurrentTime, Time_point[9]) <= 0)//17:30[8]--18:30[9]
            {
                count_TotalMin = ExecDateMinuteDiff(Time_point[8], CurrentTime) + count_17;
            }

            count = count_TotalMin / takt;
            if (count >= DBJH )
            {
                count = DBJH;
            }
            return true;
        }
        private void UI_Refresh()
        {
            //当班计划
            DBJH_H.Text = DBJH_H_2.Text = str_DBJH_H;
            DBJH_M.Text = DBJH_M_2.Text = str_DBJH_M;
            DBJH_R.Text = DBJH_R_2.Text = str_DBJH_R;
            DBJH_Q.Text = DBJH_Q_2.Text = str_DBJH_Q;
          

            //当前实际
            DQSJ_H.Text = DQSJ_H_2.Text = str_DQSJ_H;
            DQSJ_M.Text = DQSJ_M_2.Text = str_DQSJ_M;
            DQSJ_R.Text = DQSJ_R_2.Text = str_DQSJ_R;
            DQSJ_Q.Text = DQSJ_Q_2.Text = str_DQSJ_Q;

            //当前计划
            //DQJH_R.Text = DQJH_R_2.Text = Convert.ToString((int)ANDON_DQJH_L4); 
            //DQJH_R.Text = DQJH_R_2.Text = int_DQJH_R.ToString(); //修改2025/10/15 BY CY
            DQJH_R.Text = DQJH_R_2.Text = "0"; //修改2025/10/24 BY CY

            DQJH_H.Text = DQJH_H_2.Text = int_DQJH_H.ToString();
            DQJH_M.Text = DQJH_M_2.Text = int_DQJH_M.ToString();
           
            DQJH_Q.Text = DQJH_Q_2.Text = int_DQJH_Q.ToString();
            //DQJH_Q.Text = DQJH_Q_2.Text = DBJH_Q.Text;





            //当前差值           
            GZJS(DQJH_H, DQSJ_H, DQCZ_H, DQCZ_H_2);
            GZJS(DQJH_M, DQSJ_M, DQCZ_M, DQCZ_M_2);
            //GZJS(DQJH_R, DQSJ_R, DQCZ_R, DQCZ_R_2);
            GZJS(DQJH_Q, DQSJ_Q, DQCZ_Q, DQCZ_Q_2);

            DQCZ_R_2.Text = DQCZ_R.Text = "0";
            /* int a = Convert.ToInt32(DBJH_R_2.Text);


            int b = Convert.ToInt32(DQJH_R_2.Text);
            int c = a - b;
            DQCZ_R_2.Text = c.ToString();
            DQCZ_R.Text = c.ToString(); */


        }
        /// <summary>
        /// 差值计算
        /// </summary>
        /// <param name="DQJH"></param>
        /// <param name="DQSJ"></param>
        /// <param name="DQCZ"></param>
        /// <param name="DQCZ2"></param>
        public void  GZJS(Label DQJH, Label DQSJ, Label DQCZ,  Label DQCZ2)
        {
            
            int int_DQJH = Convert.ToInt32(DQJH.Text);
            int int_DQSJ = Convert.ToInt32(DQSJ.Text);
            int int_DQCZ= int_DQSJ - int_DQJH;
            DQCZ.Text = int_DQCZ.ToString();
            DQCZ2.Text = DQCZ.Text;
            if (int_DQCZ < 0)
            {
                DQCZ.ForeColor = Color.Lime;
            }
            else
            {
                DQCZ.ForeColor = Color.Lime ;
            } 
            DQCZ2.Text = DQCZ.Text;
            DQCZ2.ForeColor = DQCZ.ForeColor;
            
        }
        /// <summary>
        /// 获取时间差
        /// </summary>
        /// <param name="dateBegin"></param>
        /// <param name="dateEnd"></param>
        /// <returns></returns>
        
        //
        public static double ExecDateMinuteDiff(TimeSpan dateBegin, TimeSpan dateEnd)
        {

            TimeSpan ts3 = dateBegin.Subtract(dateEnd).Duration();
            return ts3.TotalMinutes;
        }

    }
}

