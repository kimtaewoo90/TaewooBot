using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// additional using pharse

// 지연함수를 위한 추가적인 부분
using System.Runtime.ExceptionServices;
using System.Security;
using Oracle.ManagedDataAccess.Client;
using System.Xml;
//

using System.Threading;
using System.Drawing.Drawing2D;

namespace TaewooBot
{
    public partial class Form1 : Form
    {

        int g_scr_no = 0;

        // 로그인 전역 변수 설정
        string g_user_id = null;
        string g_accnt_no = null;
        string g_accnt_amt = null;
        string g_accnt_pnl = null;

        // 계좌조회 전역변수 설정 
        int g_flag_acc = 0; // 1이면 요청에 대한 응답 완료
        int g_ord_amt_possible = 0; // 총 매수가능금액
        string g_rqname = null; // API에 데이터 수신을 요청할 때 사용할 요청명


        // Thread 전역 변수 설정
        int g_is_thread = 0;    // 0: 스레드 미생성, 1: 스레드 생성
        Thread thread1 = null;  // 생성된 스레드 객체를 담을 변수


        // Form1 class의 생성자
        public Form1()
        {
            InitializeComponent();

            // 접속 시 바로 로그인
            


            this.axKHOpenAPI1.OnReceiveTrData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEventHandler(this.DKHOpenAPI1_OnReceiveTrData);
            this.axKHOpenAPI1.OnReceiveMsg += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEventHandler(this.DKHOpenAPI1_OnReceiveMsg);
            this.axKHOpenAPI1.OnReceiveChejanData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEventHandler(this.DKHOpenAPI1_OnReceiveChejanData);

        }

        // Event Method 정의
        private void DKHOpenAPI1_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            if(g_rqname.CompareTo(e.sRQName) == 0)
            {
                ;
            }
            else
            {
                write_sys_log("요청한 TR : [" + g_rqname + "]\n", 0);
                write_sys_log("수신한 TR : [" + e.sRQName + "]\n", 0);

                switch (g_rqname)
                {
                    case "증거금세부내역조회요청":
                        g_flag_acc = 1;
                        break;

                    default: break;
                }
                return;
            }

            if (e.sRQName == "증거금세부내역조회요청")
            {
                g_ord_amt_possible = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, 0, "100주문가능금액").Trim());
                axKHOpenAPI1.DisconnectRealData(e.sScrNo);
                g_flag_acc = 1;
            }
        }
        
        private void DKHOpenAPI1_OnReceiveMsg(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEvent e)
        {

        }
        
        private void DKHOpenAPI1_OnReceiveChejanData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
        {

        }
        
        // 계좌 테이블 세팅 메서드
        public void set_tb_accnt()
        {
            int for_cnt = 0;
            int for_flag = 0;

            write_sys_log("계좌테이블(TB_ACCNT) 세팅 시작\n", 0);

            for_flag = 0;
            for(; ; )
            {
                axKHOpenAPI1.SetInputValue("계좌번호", g_accnt_no);
                axKHOpenAPI1.SetInputValue("비밀번호", "0070");

                g_rqname = "";
                g_rqname = "증거금세부내역조회요청"; // 요청명 정의
                g_flag_acc = 0; // 요청 중

                String scr_no = null; // 화면번호를 담을 변수 선언
                scr_no = "";
                scr_no = get_scr_no(); // 화면번호 채번
                axKHOpenAPI1.CommRqData(g_rqname, "oow0013", 0, scr_no);  // Open API로 데이터 요청

                for_cnt = 0; 
                for(; ;) // 요청 후 대기 시작
                {
                    if (g_flag_acc == 1)
                    {
                        delay(1000);
                        axKHOpenAPI1.DisconnectRealData(scr_no);
                        for_flag = 1;
                        break;
                    }

                    else
                    {
                        write_sys_log("'증거금세부내역조회요청' 데이터 수신 대기 중 ...\n", 0);
                        delay(1000);
                        for_cnt++;
                        if (for_cnt == 1) // 한번이라도 실패하면 무한루프를 빠져나감(비밀번호 오류 방지)
                        {
                            for_flag = 1;
                            break;
                        }

                        else
                        {
                            continue;
                        }
                    }
                }

                axKHOpenAPI1.DisconnectRealData(scr_no);

                if(for_flag == 1) // 요청에 대한 응답을 받았으므로 무한루프에서 빠져나옴
                {
                    break;
                }

                else if( for_flag == 0)
                {
                    delay(1000);
                    break; //비밀번호 5회 오류 방지
                }

                delay(1000);
            }

            // 주문가능금액 입력
            write_msg_log("주문가능금액 : [" + g_ord_amt_possible.ToString() + "]\n", 0);
        }


        //  현재시간 불러오기
        public string get_cur_tm()
        {
            DateTime cur_time;
            string cur_tm;

            cur_time = DateTime.Now;
            cur_tm = cur_time.ToString("HH:mm:ss");

            return cur_tm;
        }


        // 종목 이름 가져오기
        public string get_jongmok_nm(string jongmok_cd)
        {
            string jongmok_nm = null;
            jongmok_nm = axKHOpenAPI1.GetMasterCodeName(jongmok_cd);

            return jongmok_nm;
        }

        // 메시지 로그 함수 구현
        public void write_msg_log(String text, int is_clear)
        {
            DateTime cur_time;
            String cur_dt;
            String cur_tm;
            String cur_dtm;

            cur_dt = "";
            cur_tm = "";

            cur_time = DateTime.Now;
            cur_dt = cur_time.ToString("yyyy-") + cur_time.ToString("MM-") + cur_time.ToString("dd");
            cur_tm = get_cur_tm();

            cur_dtm = "\r\n[" + cur_dt + " " + cur_tm + "]";

            if (is_clear == 1)
            {
                if (this.textBox_msg_log.InvokeRequired)
                {
                    textBox_msg_log.BeginInvoke(new Action(() => textBox_msg_log.Clear()));
                }
                else
                {
                    this.textBox_msg_log.Clear();
                }
            }

            else
            {
                if (this.textBox_msg_log.InvokeRequired)
                {
                    textBox_msg_log.BeginInvoke(new Action(() => textBox_msg_log.AppendText(cur_dtm + text)));
                }

                else
                {
                    this.textBox_msg_log.AppendText(cur_dtm + text);
                }
            }
        }

        // 시스템 로그 함수 구현
        public void write_sys_log(String text, int is_Clear)
        {
            DateTime cur_time;
            String cur_dt; 
            String cur_tm;
            String cur_dtm;

            cur_dt = "";
            cur_tm = "";

            cur_time = DateTime.Now;
            cur_dt = cur_time.ToString("yyyy-") + cur_time.ToString("MM-") + cur_time.ToString("dd");
            cur_tm = get_cur_tm();

            cur_dtm = "\r\n[" + cur_dt + " " + cur_tm + "]";

            if (is_Clear == 1)
            {
                if (this.textBox_err_log.InvokeRequired)
                {
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.Clear()));
                }
                else
                {
                    this.textBox_err_log.Clear();
                }
            }

            else
            {
                if (this.textBox_err_log.InvokeRequired)
                {
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.AppendText(cur_dtm + text)));
                }

                else
                {
                    this.textBox_err_log.AppendText(cur_dtm + text);
                }
            }
        }

        // 지연함수 구현
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public DateTime delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                try
                {
                    unsafe
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
                catch (AccessViolationException ex)
                {
                    write_msg_log("delay() ex.Message : [" + ex.Message + "]\n", 0);
                }

                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }

        // 요청번호 부여 함수 구현
        private string get_scr_no()
        {
            if (g_scr_no < 9999)
            {
                g_scr_no++;
            }
            else g_scr_no = 1000;

            return g_scr_no.ToString();
        }

        // Connection DB
        private OracleConnection connect_db()
        {
            String conninfo = @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))
(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=orcl)));User Id=c##TAEWOOBOT;Password=tang4381;";

            OracleConnection conn = new OracleConnection(conninfo);

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("connect_db() FAIL!" + ex.Message, " 오류발생");
                conn = null;
            }

            return conn;
        }

        // 로그인 함수 구현
        private void 로그인ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int ret = 0;
            int ret2 = 0;

            String accno = null;
            String accno_cnt = null;
            string[] accno_arr = null;

            ret = axKHOpenAPI1.CommConnect(); // 로그인 창 호출

            toolStripStatusLabel1.Text = "로그인 중..."; // 화면 하단 상태란에 메시지 출력

            for (; ; )
            {
                ret2 = axKHOpenAPI1.GetConnectState(); // 로그인 완료 여부를 가져옴
                if (ret2 == 1)
                {
                    // 로그인 성공
                    break;
                }
                else
                {
                    // 로그인 대기
                    delay(1000); // 1초 지연
                }
            } // end for

            toolStripStatusLabel1.Text = "로그인 완료"; // 화면 하단 상태란에 메시지 출력

            g_user_id = "";
            g_user_id = axKHOpenAPI1.GetLoginInfo("USER_ID").Trim(); // 사용자 아이디를 가져와서 클래스 변수(전역변수)에 저장
            textBox1.Text = g_user_id; // 전역변수에 저장한 아이디를 텍스트박스에 출력

            accno_cnt = "";
            accno_cnt = axKHOpenAPI1.GetLoginInfo("ACCOUNT_CNT").Trim(); // 사용자의 증권계좌 수를 가져옴

            // TODO : Error
            accno_arr = new string[int.Parse(accno_cnt)];


            accno = "";
            accno = axKHOpenAPI1.GetLoginInfo("ACCNO").Trim();

            accno_arr = accno.Split(';');  // API에서 ';'를 구분자로 여러개의 계좌번호를 던져준다.

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(accno_arr);  // N개의 계좌번호를 콤보박스에 저장
            comboBox1.SelectedIndex = 0;  // 첫번째 계좌번호가 초기 선택으로 설정
            g_accnt_no = comboBox1.SelectedItem.ToString().Trim(); // 선택된 증권계좌 번호를 클래스 변수에 저장.

            // end for

        }


        // 로그인 성공 메시지 함수 
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            g_accnt_no = comboBox1.SelectedItem.ToString().Trim();
            write_sys_log("로그인 성공 성투하세요!.\n", 0);
            write_msg_log(
                "사용자 아이디   : [" + g_user_id + "]" +
                "사용자 계좌번호 : [" + g_accnt_no + "]\n", 0);
        }

        // 거래종목 조회
        private void button1_Click(object sender, EventArgs e)
        {
            OracleCommand cmd;
            OracleConnection conn;
            OracleDataReader reader = null;

            string sql;

            string jongmok_cd;
            string jongmok_nm;
            int priority;
            int buy_amt;
            int buy_price;
            int target_price;
            int cut_loss_price;
            string buy_trd_yn;
            string sell_trd_yn;
            int seq = 0;
            string[] arr = null;

            conn = null;
            conn = connect_db();

            cmd = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            sql = null;
            sql = "SELECT                 " + // 거래종목 테이블 조회 SQL 작성
                  "    JONGMOK_CD    ,    " +
                  "    JONGMOK_NM    ,    " +
                  "    PRIORITY      ,    " +
                  "    BUY_AMT       ,    " +
                  "    BUY_PRICE     ,    " +
                  "    TARGET_PRICE  ,    " +
                  "    CUT_LOSS_PRICE,    " +
                  "    BUY_TRD_YN    ,    " +
                  "    SELL_TRD_YN        " +
                  "FROM                   " +
                  "    TB_TRD_JONGMOK     " +
                  "    WHERE USER_ID = " + "'" + g_user_id + "' order by PRIORITY  ";

            cmd.CommandText = sql;

            this.Invoke(new MethodInvoker(
                delegate ()
                {
                    dataGridView1.Rows.Clear();  // 그리드 뷰 초기화
                }));

            try
            {
                reader = cmd.ExecuteReader(); // SQL 수행
            }
            catch (Exception ex)
            {
                write_sys_log("SELECT TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\n", 0);
            }

            // Data 변수 초기화
            jongmok_cd = "";
            jongmok_nm = "";
            priority = 0;
            buy_amt = 0;
            buy_price = 0;
            target_price = 0;
            cut_loss_price = 0;
            buy_trd_yn = "";
            sell_trd_yn = "";

            // Get data from DB
            while (reader.Read())
            {
                seq++;
                jongmok_cd = "";
                jongmok_nm = "";
                priority = 0;
                buy_amt = 0;
                buy_price = 0;
                target_price = 0;
                cut_loss_price = 0;
                buy_trd_yn = "";
                sell_trd_yn = "";
                seq = 0;

                // 각 컬럼 값 저장
                jongmok_cd = reader[0].ToString().Trim();
                jongmok_nm = reader[1].ToString().Trim();
                priority = int.Parse(reader[2].ToString().Trim());
                buy_amt = int.Parse(reader[3].ToString().Trim());
                buy_price = int.Parse(reader[4].ToString().Trim());
                target_price = int.Parse(reader[5].ToString().Trim());
                cut_loss_price = int.Parse(reader[6].ToString().Trim());
                buy_trd_yn = reader[7].ToString().Trim();
                sell_trd_yn = reader[8].ToString().Trim();

                arr = null;
                arr = new string[]
                {
                    seq.ToString(),
                    jongmok_cd,
                    jongmok_nm,
                    priority.ToString(),
                    buy_amt.ToString(),
                    buy_price.ToString(),
                    target_price.ToString(),
                    cut_loss_price.ToString(),
                    buy_trd_yn,
                    sell_trd_yn
                };
                this.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        dataGridView1.Rows.Add(arr);  // 데이터그리드뷰에 데이터 추가
                    }));

                write_sys_log("거래종목(TB_TRD_JONGMOK)이 조회되었습니다. \n", 0);
            }
        }


        // 거래종목 삽입
        private void button2_Click(object sender, EventArgs e)
        {
            OracleCommand cmd;
            OracleConnection conn;

            string sql;

            string jongmok_cd;
            string jongmok_nm;
            int priority;
            int buy_amt;
            int buy_price;
            int target_price;
            int cut_loss_price;
            string buy_trd_yn;
            string sell_trd_yn;

            foreach(DataGridViewRow row in dataGridView1.Rows)
            {
                if(Convert.ToBoolean(row.Cells[check.Name].Value) != true)
                {
                    continue;
                }
                if (Convert.ToBoolean(row.Cells[check.Name].Value) == true)
                {
                    jongmok_cd = row.Cells[1].Value.ToString();
                    jongmok_nm = row.Cells[2].Value.ToString();
                    priority = int.Parse(row.Cells[3].Value.ToString());
                    buy_amt = int.Parse(row.Cells[4].Value.ToString());
                    buy_price = int.Parse(row.Cells[5].Value.ToString());
                    target_price = int.Parse(row.Cells[6].Value.ToString());
                    cut_loss_price = int.Parse(row.Cells[7].Value.ToString());
                    buy_trd_yn = row.Cells[8].Value.ToString();
                    sell_trd_yn = row.Cells[9].Value.ToString();

                    conn = null;
                    conn = connect_db();

                    cmd = null;
                    cmd = new OracleCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;

                    sql = null;
                    sql = "insert into TB_TRD_JONGMOK values " +
                           "(" +
                               "'" + g_user_id + "'" + "," +
                               "'" + jongmok_cd + "'" + "," +
                               "'" + jongmok_nm + "'" + "," +
                               "'" + priority + "'" + "," +
                               "'" + buy_amt + "'" + "," +
                               "'" + buy_price + "'" + "," +
                               "'" + target_price + "'" + "," +
                               "'" + cut_loss_price + "'" + "," +
                               "'" + buy_trd_yn + "'" + "," +
                               "'" + sell_trd_yn + "'" + "," +
                               "'" + g_user_id + "'" + "," +
                               "sysdate " + "," +
                               "NULL" + "," +
                               "NULL" +
                            ")";

                    cmd.CommandText = sql;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        write_sys_log("종목삽입(insert TB_TRD_JONGMOK) 중 에러발생 : ["
                                       + ex.Message + "]", 0);
                    }

                    write_sys_log("종목코드 : [" + jongmok_cd + "]" + "(이)가 삽입되었습니다\n", 0);
                }
            }
        }


        // 거래종목 수정
        private void button3_Click(object sender, EventArgs e)
        {
            OracleCommand cmd;
            OracleConnection conn;

            string sql;

            string jongmok_cd;
            string jongmok_nm;
            int priority;
            int buy_amt;
            int buy_price;
            int target_price;
            int cut_loss_price;
            string buy_trd_yn;
            string sell_trd_yn;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if(Convert.ToBoolean(row.Cells[check.Name].Value) != true)
                {
                    continue;
                }

                if (Convert.ToBoolean(row.Cells[check.Name].Value) == true)
                {
                    jongmok_cd = row.Cells[1].Value.ToString();
                    jongmok_nm = row.Cells[2].Value.ToString();
                    priority = int.Parse(row.Cells[3].Value.ToString());
                    buy_amt = int.Parse(row.Cells[4].Value.ToString());
                    buy_price = int.Parse(row.Cells[5].Value.ToString());
                    target_price = int.Parse(row.Cells[6].Value.ToString());
                    cut_loss_price = int.Parse(row.Cells[7].Value.ToString());
                    buy_trd_yn = row.Cells[8].Value.ToString();
                    sell_trd_yn = row.Cells[9].Value.ToString();

                    conn = null;
                    conn = connect_db();

                    cmd = null;
                    cmd = new OracleCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;

                    sql = null;
                    sql = "UPDATE TB_TRD_JONGMOK " +
                          "SET" +
                            "JONGMOK_NM = " + "'" + jongmok_nm + "'" + "," +
                            "PRIORITY = " + "'" + priority + "'" + "," +
                            "BUY_AMT = " + "'" + buy_amt + "'" + "," +
                            "BUY_PRICE = " + "'" + buy_price + "'" + "," +
                            "TARGET_PRICE = " + "'" + target_price + "'" + "," +
                            "CUT_LOSS_PRICE = " + "'" + cut_loss_price + "'" + "," +
                            "BUY_TRD_YN = " + "'" + buy_trd_yn + "'" + "," +
                            "SELL_TRD_YN = " + "'" + sell_trd_yn + "'" + "," +
                            "UPDT_ID = " + "'" + g_user_id + "'" + "," +
                            "UPDT_DTM = " + "SYSDATE" +
                         "WHERE JONGMOK_CD = " + "'" + jongmok_cd + "'" +
                         "AND USER_ID = " + "'" + g_user_id + "'";

                    cmd.CommandText = sql;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        write_msg_log("거래종목 수정(Update TB_JONGMOK) 중 에러발생 : [" + ex.Message + "]", 0);
                    }

                    write_msg_log("종목코드 [" + jongmok_cd + "] 가 수정되었습니다\n", 0);
                }
            }
        }


        // 거래종목 삭제
        private void button4_Click(object sender, EventArgs e)
        {
            OracleCommand cmd;
            OracleConnection conn;

            string sql;
            string jongmok_cd = null;

            foreach(DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(row.Cells[check.Name].Value) != true)
                {
                    continue;
                }

                if(Convert.ToBoolean(row.Cells[check.Name].Value) == true)
                {
                    jongmok_cd = row.Cells[1].Value.ToString();

                    conn = null;
                    conn = connect_db();

                    cmd = null;
                    cmd = new OracleCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;

                    sql = null;
                    sql = "DELETE FROM TB_TRD_JONGMOK " +
                            "WHERE JONGMOK_CD = " + "'" + jongmok_cd + "'" +
                            "AND USER_ID = " + "'" + g_user_id + "'";
                    cmd.CommandText = sql;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        write_msg_log("거래종목 삭제(DELTE TB_JONGMOK) 중 에러발생 : [" + ex.Message + "]", 0);
                    }

                    write_msg_log("종목코드 [" + jongmok_cd + "] 가 삭제되었습니다\n", 0);
                }
            }
        }


        // 자동매매 시작
        private void button5_Click(object sender, EventArgs e)
        {
            if(g_is_thread == 1) // 스레드가 이미 생성된 상태라면
            {
                write_sys_log("AUTO TRADING SYSTEM is on already. \n", 0);
                return;
            }

            write_sys_log("AUTO TRADING SYSTEM is just started \n", 0);
            g_is_thread = 1;
            thread1 = new Thread(new ThreadStart(m_thread1));
            thread1.Start();
        }


        public void m_thread1()
        {
            string cur_tm = null;
            int set_tb_accnt_flag = 0; // 1이면 호출 완료

            if(g_is_thread == 0)
            {
                g_is_thread = 1;
                //write_sys_log("Start AUTO Trading Thread\n", 0);
            }

            for(; ; )  // 장전 30분 무한루프 실행
            {
                cur_tm = get_cur_tm(); // 현재시각 조회

                // test //
                if (set_tb_accnt_flag == 0)
                {
                    set_tb_accnt_flag = 1;
                    set_tb_accnt();
                }
                // test //

                if (cur_tm.CompareTo("083001") >= 0)
                {
                    // 계좌조회, 계좌정보 조회, 보유종목 매도주문 수행
                    if(set_tb_accnt_flag == 0)
                    {
                        set_tb_accnt_flag = 1;
                        set_tb_accnt();
                    }
                }

                if(cur_tm.CompareTo("090001") >= 0)
                {
                    for(; ; )  // 장 중 무한루프 실행
                    {
                        cur_tm = get_cur_tm();
                        if(cur_tm.CompareTo("153001") >= 0)
                        {
                            break;
                        }

                        // 장 중 매수 op 매도 실행
                        delay(2000);  // 장중 무한루프 2초씩 sleep

                    }
                }

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if(g_is_thread == 0)
            {
                write_sys_log("자동매매가 시작되지 않아 중지하지 않습니다\n", 0);
                return;
            }

            write_sys_log("Stop AUTO TRADING\n", 0);

            try
            {
                thread1.Abort();
            }

            catch (Exception ex)
            {
                write_sys_log("자동매매 중지 중 오류발생 : [" + ex.Message + "]\n", 0);
            }

            this.Invoke(new MethodInvoker(() =>
            {
                if (thread1 != null)
                {
                    thread1.Interrupt();
                    thread1 = null;
                }
            }));

            g_is_thread = 0;
            write_sys_log("Stopped AUTO TRADING\n", 0);
        }

    }
}
