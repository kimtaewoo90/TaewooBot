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
using System.Configuration;
using System.Windows.Forms.VisualStyles;
using System.Runtime.Remoting.Messaging;

namespace TaewooBot
{
    public partial class Form1 : Form
    {
        // Test version
        bool g_test_checked = false;


        // 비밀번호 전역 변수 설정
        string g_passwd = "0070";

        int g_scr_no = 0;

        // 로그인 전역 변수 설정
        string g_user_id = null;
        string g_accnt_no = null;
        string g_accnt_amt = null;
        string g_accnt_pnl = null;

        // 계좌조회 전역변수 설정 
        int g_flag_acc = 0; // 1이면 요청에 대한 응답 완료
        int g_flag_acc_2 = 0;
        int g_is_next = 0;
        int g_ord_amt_possible = 0; // 총 매수가능금액
        string g_rqname = null; // API에 데이터 수신을 요청할 때 사용할 요청명

        // Thread 전역 변수 설정
        int g_is_thread = 0;    // 0: 스레드 미생성, 1: 스레드 생성
        Thread thread1 = null;  // 생성된 스레드 객체를 담을 변수

        // Trading 관련 전역 변수 설정
        int g_flag_buy = 0; // 매수주문 응답
        int g_flag_sell = 0; // 매도주문 응답
        int g_flag_cancel_sell = 0; // 매도주문 취소 응답
        int g_buy_hoga = 0; // 최우선 매수호가 저장 변수
        int g_flag_hoga = 0; // 최우선 매수호가 조회  1: 조회완료
        int g_cur_price = 0; // 현재가
        int g_flag_cur = 0; // 현재가 조회 플래그, 1: 조회완료

        // Strategy1 전역변수
        List<int> strategy1_lst = new List<int>();
        int g_flag_liq = 0; // 거래량 조회 플래그

        // Form1 class의 생성자
        public Form1()
        {
            InitializeComponent();

            // TODO
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
                // Temp code
                // g_flag_acc_2 = 1;
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

                    case "계좌평가현황요청":
                        g_flag_acc_2 = 1;
                        break;

                    case "호가조회":
                        g_flag_hoga = 1;
                        break;

                    case "현재가조회":
                        g_flag_cur = 1; // 1로 설정하여 요청하는 쪽이 무한루프에 빠지지 않게 함
                        break;

                    case "거래량조회":
                        g_flag_liq = 1;
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

            if(e.sRQName == "계좌평가현황요청")
            {
                int repeat_cnt = 0;
                int ii = 0;

                string user_id = null;
                string jongmok_cd = null;
                string jongmok_nm = null;

                int own_stock_cnt = 0;
                int buy_price = 0;
                int own_amt = 0;

                repeat_cnt = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName); // 보유종목수 가져오기

                write_sys_log("테이블 (TB_ACCNT_INFO) 설정 시작 \n", 0);
                //write_msg_log("보유종목 수 : " + repeat_cnt.ToString() + "\n", 0);

                for(ii=0; ii<repeat_cnt; ii++)
                {
                    user_id = "";
                    jongmok_cd = "";
                    own_stock_cnt = 0;
                    buy_price = 0;
                    own_amt = 0;

                    user_id = g_user_id;
                    jongmok_cd = axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "종목코드").Trim().Substring(1, 6);
                    jongmok_nm = axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "종목명").Trim();
                    own_stock_cnt = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "보유수량").Trim());
                    buy_price = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "평균단가").Trim());
                    own_amt = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "매입금액").Trim());

                    write_msg_log("종목 순번 : [ " + ii + " ]\n", 0);
                    write_msg_log("종목코드 : [" + jongmok_cd + " ]\n", 0);
                    write_msg_log("종목명   : [" + jongmok_nm + " ]\n", 0);
                    write_msg_log("보유주식수 : [" + own_stock_cnt.ToString() + " ]\n", 0);

                    if(own_stock_cnt == 0)
                    {
                        continue; 
                    }
                    insert_tb_accnt_info(jongmok_cd, jongmok_nm, buy_price, own_stock_cnt, own_amt);
                }

                if (ii == 0)
                {
                    write_msg_log("보유종목 수 : " + repeat_cnt.ToString() + "\n", 0);
                }

                write_sys_log("테이블 (TB_ACCNT_INFO) 설정 완료 \n", 0);
                axKHOpenAPI1.DisconnectRealData(e.sScrNo);

                if(e.sPrevNext.Length == 0)
                {
                    g_is_next = 0;
                }
                else
                {
                    g_is_next = int.Parse(e.sPrevNext);
                }
                g_flag_acc_2 = 1;
            }

            if(e.sRQName == "호가조회")
            {
                int cnt = 0;
                int ii = 0;
                int buy_hoga = 0;

                cnt = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);

                for (ii=0; ii < cnt; ii++)
                {
                    buy_hoga = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, " 매수최우선호가").Trim());
                    buy_hoga = System.Math.Abs(buy_hoga);
                }

                g_buy_hoga = buy_hoga;

                axKHOpenAPI1.DisconnectRealData(e.sScrNo);
                g_flag_hoga = 1;
            }

            if(e.sRQName == "현재가조회")
            {
                g_cur_price = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, 0, "현재가").Trim());
                g_cur_price = System.Math.Abs(g_cur_price);
                axKHOpenAPI1.DisconnectRealData(e.sScrNo);

                g_flag_cur = 1;
            }

            if(e.sRQName == "거래량조회")
            {
                strategy1_lst.Add(int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, 0, "거래량").Trim()));
                axKHOpenAPI1.DisconnectRealData(e.sScrNo);
            }

        }
        
        private void DKHOpenAPI1_OnReceiveMsg(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEvent e)
        {
            if(e.sRQName == "매수주문")
            {
                write_msg_log("\n=========== 매수주문 원장 응답정보 출력 시작 ===========\n", 0);
                write_msg_log("sScrNo  : [ " + e.sScrNo + " ]" + "\n", 0);
                write_msg_log("sRQName : [ " + e.sRQName + " ]" + "\n", 0);
                write_msg_log("sTrCode : [ " + e.sTrCode + " ]" + "\n", 0);
                write_msg_log("sMsg    : [ " + e.sMsg + " ]" + "\n", 0);
                write_msg_log("\n=========== 매수주문 원장 응답정보 출력 종료 ===========\n", 0);

                g_flag_buy = 1;
            }

            if (e.sRQName == "매도주문")
            {
                write_msg_log("\n=========== 매도주문 원장 응답정보 출력 시작 ===========\n", 0);
                write_msg_log("sScrNo  : [ " + e.sScrNo + " ]" + "\n", 0);
                write_msg_log("sRQName : [ " + e.sRQName + " ]" + "\n", 0);
                write_msg_log("sTrCode : [ " + e.sTrCode + " ]" + "\n", 0);
                write_msg_log("sMsg    : [ " + e.sMsg + " ]" + "\n", 0);
                write_msg_log("\n=========== 매수주문 원장 응답정보 출력 종료 ===========\n", 0);

                g_flag_sell = 1;
            }

            if (e.sRQName == "매도취소주문")
            {
                write_msg_log("\n=========== 매도취소주문 원장 응답정보 출력 시작 ===========\n", 0);
                write_msg_log("sScrNo  : [ " + e.sScrNo + " ]" + "\n", 0);
                write_msg_log("sRQName : [ " + e.sRQName + " ]" + "\n", 0);
                write_msg_log("sTrCode : [ " + e.sTrCode + " ]" + "\n", 0);
                write_msg_log("sMsg    : [ " + e.sMsg + " ]" + "\n", 0);
                write_msg_log("\n=========== 매도취소주문 원장 응답정보 출력 종료 ===========\n", 0);

                g_flag_cancel_sell = 1;
            }
        }
        
        private void DKHOpenAPI1_OnReceiveChejanData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
        {
            if(e.sGubun == "0")  // sGubun 값이 "0"이면 주문내역 및 체결내역 수신 성공
            {
                string chejan_gb = "";
                chejan_gb = axKHOpenAPI1.GetChejanData(913).Trim();  // 주문내역 or 체결내역 인지 구분하는 변수

                if(chejan_gb == "접수") // 주문내역
                {
                    string user_id = null;
                    string jongmok_cd = null;
                    string jongmok_nm = null;
                    string ord_gb = null;
                    string ord_no = null;
                    string org_ord_no = null;
                    string ref_dt = null;
                    int ord_price = 0;
                    int ord_stock_cnt = 0;
                    int ord_amt = 0;
                    string ord_dtm = null;

                    user_id = g_user_id;
                    jongmok_cd = axKHOpenAPI1.GetChejanData(9001).Trim().Substring(1, 6);
                    jongmok_nm = get_jongmok_nm(jongmok_cd);
                    ord_gb = axKHOpenAPI1.GetChejanData(907).Trim();
                    ord_no = axKHOpenAPI1.GetChejanData(9203).Trim();
                    org_ord_no = axKHOpenAPI1.GetChejanData(904).Trim();
                    ord_price = int.Parse(axKHOpenAPI1.GetChejanData(901).Trim());
                    ord_stock_cnt = int.Parse(axKHOpenAPI1.GetChejanData(900).Trim());
                    ord_amt = ord_price * ord_stock_cnt;

                    DateTime CurTime;
                    string CurDt;
                    CurTime = DateTime.Now;
                    CurDt = CurTime.ToString("yyyy") + CurTime.ToString("MM") + CurTime.ToString("dd");
                    ref_dt = CurDt;
                    ord_dtm = CurDt + axKHOpenAPI1.GetChejanData(908).Trim();

                    write_msg_log("========== 주문내역 ==========\n", 0);
                    write_msg_log("종목코드 : [ " + jongmok_cd + " ]" + "\n", 0);
                    write_msg_log("종목명 : [ " + jongmok_nm + " ]" + "\n", 0);
                    write_msg_log("주문구분 : [ " + ord_gb + " ]" + "\n", 0);
                    write_msg_log("주문번호 : [ " + ord_no + " ]" + "\n", 0);
                    write_msg_log("원주문번호 : [ " + org_ord_no + " ]" + "\n", 0);
                    write_msg_log("주문금액(1주) : [ " + ord_price.ToString() + " ]" + "\n", 0);
                    write_msg_log("주문주식수 : [ " + ord_stock_cnt.ToString() + " ]" + "\n", 0);
                    write_msg_log("주문금액(총) : [ " + ord_amt.ToString() + " ]" + "\n", 0);
                    write_msg_log("주문시간 : [ " + ord_dtm + " ]" + "\n", 0);

                    insert_tb_ord_lst(ref_dt, jongmok_cd, jongmok_nm, ord_gb, ord_no, org_ord_no, ord_price, ord_stock_cnt, ord_amt, ord_dtm);

                    // 체결이 아니고 주문접수 일 때 업데이트를 하는건가?
                    if(ord_gb == "2") // 매수주문일 경우
                    {
                        update_tb_accnt(ord_gb, ord_amt);
                    }
                }

                else if(chejan_gb == "체결")
                {
                    string user_id = null;
                    string jongmok_cd = null;
                    string jongmok_nm = null;
                    string chegyul_gb = null;
                    int chegyul_no = 0;
                    int chegyul_price = 0;
                    int chegyul_cnt = 0;
                    int chegyul_amt = 0;
                    string chegyul_dtm = null;
                    string ord_no = null;
                    string org_ord_no = null;
                    string ref_dt = null;

                    user_id = g_user_id;
                    jongmok_cd = axKHOpenAPI1.GetChejanData(9001).Trim().Substring(1, 6);
                    jongmok_nm = get_jongmok_nm(jongmok_cd);
                    chegyul_gb = axKHOpenAPI1.GetChejanData(907).Trim();  // 2:buy, 1:sell
                    chegyul_no = int.Parse(axKHOpenAPI1.GetChejanData(909).Trim());
                    chegyul_price = int.Parse(axKHOpenAPI1.GetChejanData(910).Trim());
                    chegyul_cnt = int.Parse(axKHOpenAPI1.GetChejanData(911).Trim());
                    chegyul_amt = chegyul_price * chegyul_cnt;
                    org_ord_no = axKHOpenAPI1.GetChejanData(904).Trim();

                    DateTime CurTime;
                    string CurDt;

                    CurTime = DateTime.Now;
                    CurDt = CurTime.ToString("yyyy") + CurTime.ToString("MM") + CurTime.ToString("dd");
                    ref_dt = CurDt;
                    chegyul_dtm = CurDt + axKHOpenAPI1.GetChejanData(908).Trim();
                    ord_no = axKHOpenAPI1.GetChejanData(9203).Trim();

                    write_msg_log("========== 체결내역 ==========\n", 0);
                    write_msg_log("종목코드 : [ " + jongmok_cd + " ]" + "\n", 0);
                    write_msg_log("종목명 : [ " + jongmok_nm + " ]" + "\n", 0);
                    write_msg_log("체결구분 : [ " + chegyul_gb + " ]" + "\n", 0);
                    write_msg_log("체결번호 : [ " + chegyul_no.ToString() + " ]" + "\n", 0);
                    write_msg_log("체결가 : [ " + chegyul_price.ToString() + " ]" + "\n", 0);
                    write_msg_log("체결주식수 : [ " + chegyul_cnt.ToString() + " ]" + "\n", 0);
                    write_msg_log("체결금액 : [ " + chegyul_amt.ToString() + " ]" + "\n", 0);
                    write_msg_log("체결시간 : [ " + chegyul_dtm + " ]" + "\n", 0);
                    write_msg_log("주문번호 : [ " + ord_no + " ]" + "\n", 0);
                    write_msg_log("원주문번호 : [ " + org_ord_no + " ]" + "\n", 0);

                    // 체결내역 저장
                    insert_tb_chegyul_lst(ref_dt, jongmok_cd, jongmok_nm, chegyul_gb, chegyul_no, 
                        chegyul_price, chegyul_cnt, chegyul_amt, chegyul_dtm, ord_no, org_ord_no);

                    if (chegyul_gb == "1")
                    {
                        update_tb_accnt(chegyul_gb, chegyul_amt);
                    }
                }
            }  // if(e.sGubun == "0") 종료

            if(e.sGubun == "1") // 1: 계좌정보 수신
            {
                string user_id = null;
                string jongmok_cd = null;

                int boyu_cnt = 0;
                int boyu_price = 0;
                int boyu_amt = 0;

                user_id = g_user_id;
                jongmok_cd = axKHOpenAPI1.GetChejanData(9001).Trim().Substring(1, 6);
                boyu_cnt = int.Parse(axKHOpenAPI1.GetChejanData(930).Trim());
                boyu_price = int.Parse(axKHOpenAPI1.GetChejanData(931).Trim());
                boyu_amt = int.Parse(axKHOpenAPI1.GetChejanData(932).Trim());

                string jongmok_nm = null;
                jongmok_nm = get_jongmok_nm(jongmok_cd);

                write_msg_log("종목코드 : [ " + jongmok_cd + " ]" + "\n", 0);
                write_msg_log("보유주식수 : [ " + boyu_cnt.ToString() + " ]" + "\n", 0);
                write_msg_log("보유가 : [ " + boyu_price.ToString() + " ]" + "\n", 0);
                write_msg_log("보유금액 : [ " + boyu_amt.ToString() + " ]" + "\n", 0);

                merge_tb_accnt_info(jongmok_cd, jongmok_nm, boyu_cnt, boyu_price, boyu_amt);  // 계좌정보(보유정보) 저장.

            }  // if(e.sGubun == "1") 종료
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

        // 계좌 정보 테이블 설정
        public void set_tb_accnt_info()
        {
            OracleCommand cmd;
            OracleConnection conn;
            string sql;
            int for_cnt = 0;
            int for_flag = 0;

            sql = null;
            cmd = null;

            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;   // DB 연결 시작
            cmd.CommandType = CommandType.Text;

            sql = @"DELETE FROM TB_ACCNT_INFO WHERE ref_dt = to_char(sysdate, 'yyyymmdd') AND user_id = " + "'" + g_user_id + "'"; // 당일기준 계좌정보 삭제

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_sys_log("당일기준 계좌정보 삭제 중 다음 에러 : [" + ex.Message + "]", 0);
            }

            conn.Close();  // DB 연결 종료

            g_is_next = 0;
            for(; ; )
            {
                for_flag = 0;
                for(; ; )
                {
                    axKHOpenAPI1.SetInputValue("계좌번호", g_accnt_no);
                    axKHOpenAPI1.SetInputValue("비밀번호", g_passwd);
                    axKHOpenAPI1.SetInputValue("상장폐지조회구분", "1");
                    axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");

                    g_flag_acc_2 = 0;
                    g_rqname = "계좌평가현황요청";

                    String scr_no = get_scr_no();

                    // 계좌정보 테이터 수신 요청
                    axKHOpenAPI1.CommRqData(g_rqname, "OPW00004", g_is_next, scr_no);  // OnReceiveTrData 함수 호출

                    for_cnt = 0;
                    for(; ; )
                    {
                        if(g_flag_acc_2 == 1)
                        {
                            delay(1000);
                            axKHOpenAPI1.DisconnectRealData(scr_no);
                            for_flag = 1;

                            break;
                        }
                        else
                        {
                            delay(1000);
                            for_cnt++;
                            if(for_cnt == 5)
                            {
                                for_flag = 0;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    delay(1000);
                    axKHOpenAPI1.DisconnectRealData(scr_no);

                    if(for_flag == 1)
                    {
                        break;
                    }
                    else if(for_flag == 0)
                    {
                        delay(1000);
                        continue;
                    }
                }

                if(g_is_next == 0)
                {
                    break;
                }
                delay(1000);
            }
        }

        // 계좌 정보 테이블 삽입
        public void insert_tb_accnt_info(string jongmok_cd, string jongmok_nm, int buy_price, int own_stock_cnt, int own_amt)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 계좌정보 테이블 삽입
            sql = @"INSERT INTO TB_ACCNT_INFO VALUES ( " +
                    "'" + g_user_id + "'" + "," +
                    "'" + g_accnt_no + "'" + "," +
                    "to_char(sysdate, 'yyyymmdd')" + "," +
                    "'" + jongmok_cd + "'" + "," +
                    "'" + jongmok_nm + "'" + "," +
                    "'" + buy_price + "'" + "," +
                    "'" + own_stock_cnt + "'" + "," +
                    "'" + own_amt + "'" + "," +
                    "'taewoobot'" + "," +
                    "SYSDATE" + "," +
                    "null" + "," +
                    "null" + ") ";

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_sys_log("계좌정보 테이블 삽입 중 다음 에러 발생 : [" + ex.Message + "]\n", 0);
            }

            conn.Close();  // DB 접속 종료.
        }

        // 주문내역 저장 메서드
        public void insert_tb_ord_lst(string ref_dt, string jongmok_cd, string jongmok_nm, string ord_gb, string ord_no, string org_ord_no, 
                                      int ord_price, int ord_stock_cnt, int ord_amt, string ord_dtm)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            sql = null;
            cmd = null;
            conn = null;

            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 주문내역 저장
            sql = "insert into tb_ord_lst values ( " +
                "'" + g_user_id + "'" + "," +
                "'" + g_accnt_no + "'" + "," +
                "'" + ref_dt + "'" + "," +
                "'" + jongmok_cd + "'" + "," +
                "'" + jongmok_nm + "'" + "," +
                "'" + ord_gb + "'" + "," +
                "'" + ord_no + "'" + "," +
                "'" + org_ord_no + "'" + "," +
                ord_price + "," +
                ord_stock_cnt + "," +
                ord_amt + "," +
                "'" + ord_dtm + "'" + "," +
                "'taewoobot'" + "," +
                "SYSDATE" + "," +
                "null" + "," +
                "null" + ") ";

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
                write_sys_log("주문내역 저장 성공\n", 0);
            }

            catch (Exception ex)
            {
                write_sys_log("주문내역 저장 중 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
            }

            conn.Close();
        }

        // 계좌테이블 Update 메서드 (주문가능금액 수정)
        public void update_tb_accnt(string chegyul_gb, int chegyul_amt)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            sql = null;
            cmd = null;
            conn = null;

            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            if(chegyul_gb == "2") // 매수인 경우 주문가능금액에서 체결금액 빼기
            {
                sql = @"update TB_ACCNT set ORD_POSSIBLE_AMT =  ord_possible_amt - "
                    + chegyul_amt + ", updt_dtm = SYSDATE, updt_id = 'taewoobot' " +
                    " where user_id = " + "'" + g_user_id + "'" +
                    " and accnt_no = " + "'" + g_accnt_no + "'" +
                    " and ref_dt = to_char(sysdate, 'yyyymmdd') ";
            }

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
                write_sys_log("매수체결 후 주문가능금액 수정 완료\n", 0);
            }

            catch (Exception ex)
            {
                write_sys_log("주문가능금액 수정 중 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
            }

            conn.Close();
        }

        // 체결내역 저장 메서드
        public void insert_tb_chegyul_lst(string ref_dt, string jongmok_cd, string jongmok_nm, string chegyul_gb,
                                            int chegyul_no, int chegyul_price, int chegyul_stock_cnt, int chegyul_amt, string chegyul_dtm, string ord_no, string org_ord_no)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            sql = null;
            cmd = null;
            conn = null;

            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            sql = "insert into tb_chegyul_lst values ( " +
                    "'" + g_user_id + "'" + "," +
                    "'" + g_accnt_no + "'" + "," +
                    "'" + ref_dt + "'" + "," +
                    "'" + jongmok_cd + "'" + "," +
                    "'" + jongmok_nm + "'" + "," +
                    "'" + chegyul_gb + "'" + "," +
                    "'" + ord_no + "'" + "," +
                    "'" + chegyul_gb + "'" + "," +
                    chegyul_no + "," +
                    chegyul_price + "," +
                    chegyul_stock_cnt + "," +
                    chegyul_amt + "," +
                    "'" + chegyul_dtm + "'" + "," +
                    "'taewoobot'" + "," +
                    "SYSDATE" + "," +
                    "null" + "," +
                    "null" + ") ";

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
                write_sys_log("체결내역 저장 성공\n", 0);
            }

            catch (Exception ex)
            {
                write_sys_log("체결내역 저장 중 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
            }

            conn.Close();
        }

        // 계좌정보 테이블 세팅 메서드
        public void merge_tb_accnt_info(string jongmok_cd, string jongmok_nm, int boyu_cnt, int boyu_price, int boyu_amt)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            sql = null;
            cmd = null;
            conn = null;

            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 계좌정보 테이블 세팅, 기존에 보유한 종목이면 갱신, 보유하지 않았으면 신규로 삽입
            sql = @"merge into TB_ACCNT_INFO a 
                    using (
                         select nvl(max(user_id), '0') user_id, 
                                nvl(max(ref_dt), '0') ref_dt
                                nvl(max(jongmok_cd), '0') jongmok_cd
                                nvl(max(jongmok_nm), '0') jongmok_nm
                         from TB_ACCNT_INFO
                         where user_id = '" + g_user_id + "'" +
                         " and ACCNT_NO = '" + g_accnt_no + "'" +
                         " and jongmok_cd = '" + jongmok_cd + "'" +
                         " and ref_dt = to_char(sysdate, 'yyyymmdd') " +
                         " ) b " +
                         " on (a.user_id = b.user_id and a.jongmok_cd = b.jongmok_cd and a.ref_dt = b.ref_dt) " +
                         "when matched then update " +
                         " set OWN_STOCK_CNT = " + boyu_cnt + "," +
                         " BUY_PRICE = " + boyu_price + "," +
                         " OWN_AMT = " + boyu_amt + "," +
                         " updt_dtm = SYSDATE " + "," +
                         " updt_id = 'taewoobot'" +
                         " when not matched then isert " +
                         "(a.user_id, a.accnt_no, a.ref_dt, a.jongmok_cd, a.jongmok.nm, a.BUY_PRICE, a.OWN_STOCK_CNT, a.OWN_AMT, a.inst_dtm, a.inst_id)" +
                         " values ( " +
                         " '" + g_user_id + "'" + "," +
                         " '" + g_accnt_no + "'" + "," +
                         " to_char(sysdate, 'yyyymmdd')" + "," +
                         " '" + jongmok_cd + "'" + "," +
                         " '" + jongmok_nm + "'" + "," +
                         boyu_price + "," +
                         boyu_cnt + "," +
                         boyu_amt + "," +
                         " SYSDATE" + "," +
                         " 'taewoobot'" +
                         " ) ";

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
                write_sys_log("계좌정보 테이블 수정 완료\n", 0);
            }

            catch (Exception ex)
            {
                write_sys_log("계좌정보 테이블 업데이트 중 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
            }

            conn.Close();
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

        // 계좌정보 보유종목의 매도주문 메서드
        public void sell_ord_first()
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            string jongmok_cd = null;
            int buy_price = 0;
            int own_stock_cnt = 0;
            int target_price = 0;

            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // TB_ACCNT_INFO와 TB_TRD_JONGMOK 테이블 조인하여 매도대상 종목 조회
            sql = "SELECT " +
                  "     A.JONGMOK_CD, " +
                  "     A.BUY_PRICE, " +
                  "     A.OWN_STOCK_CNT, " +
                  "     B.TARGET_PRICE " +
                  " FROM TB_ACCNT_INFO A, " +
                  "      TB_TRD_JONGMOK B " +
                  " WHERE A.USER_ID = " + "'" + g_user_id + "' " +
                  " AND   A.ACCNT_NO = " + "'" + g_accnt_no + "' " +
                  " AND   A.REF_DT = " + "TO_CHAR(SYSDATE, 'yyyymmdd') " +
                  " AND   A.USER_ID = " + "B.USER_ID " +
                  " AND   A.JONGMOK_CD = " + "B.JONGMOK_CD " +
                  " AND   B.SELL_TRD_YN = 'Y' AND " + "A.OWN_STOCK_CNT > 0 ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                jongmok_cd = "";
                buy_price = 0;
                own_stock_cnt = 0;
                target_price = 0;

                jongmok_cd = reader[0].ToString().Trim();
                buy_price = int.Parse(reader[1].ToString().Trim());
                own_stock_cnt = int.Parse(reader[2].ToString().Trim());
                target_price = int.Parse(reader[3].ToString().Trim());

                write_msg_log("======= 장 시작전 매도대상 종목 조회 ========\n", 0);
                write_msg_log("종목명 : [" + get_jongmok_nm(jongmok_cd) + "]\n", 0);
                write_msg_log("매입가 : [" + buy_price + "]\n", 0);
                write_msg_log("보유주식수 : [" + own_stock_cnt + "]\n", 0);
                write_msg_log("목표가 : [" + target_price + "]\n", 0);

                int new_target_price = 0;
                new_target_price = get_hoga_unit_price(target_price, jongmok_cd, 0);

                g_flag_sell = 0;
                g_rqname = "매도주문";

                string scr_no = null;
                scr_no = "";
                scr_no = get_scr_no();

                int ret = 0;

                // 매도주문 요청
                ret = axKHOpenAPI1.SendOrder("매도주문", scr_no, g_accnt_no, 2, jongmok_cd, own_stock_cnt, new_target_price, "00", "");
                if(ret == 0)
                {
                    write_msg_log("매도주문 호출 성공\n", 0);
                    write_msg_log("종목코드 : [ " + get_jongmok_nm(jongmok_cd) + " ]\n", 0);
                }
                else
                {
                    write_msg_log("매도주문 호출 실패\n", 0);
                    write_msg_log("종목코드 : [ " + get_jongmok_nm(jongmok_cd) + " ]\n", 0);
                }

                delay(200);

                for(; ; )
                {
                    if(g_flag_sell == 1)
                    {
                        delay(200);
                        axKHOpenAPI1.DisconnectRealData(scr_no);
                        break;
                    }
                    else
                    {
                        write_msg_log("'매도주문' 완료 대기중...\n", 0);
                        delay(200);
                        break;
                    }
                }
                axKHOpenAPI1.DisconnectRealData(scr_no);
            }
            reader.Close();
            conn.Close();

        }

        // 실시간 매수주문 메서드
        public void real_buy_ord()
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            string jongmok_cd = null;
            int buy_amt = 0;
            int buy_price = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 거래종목 테이블 조회
            sql = "SELECT " +
                    " A.JONGMOK_CD, " +
                    " A.BUY_AMT, " +
                    " A.BUY_PRICE, " +
                  "FROM TB_TRD_JONGMOK A " +
                  "WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                  "   AND A.BUY_TRD_YN = 'Y' " +
                  "ORDER BY A.PRIORITY ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                jongmok_cd = "";
                buy_amt = 0;
                buy_price = 0;

                jongmok_cd = reader[0].ToString().Trim(); // 종목코드
                buy_amt = int.Parse(reader[1].ToString().Trim());  // 매수금액
                buy_price = int.Parse(reader[2].ToString().Trim()); // 매수가

                int buy_price_tmp = 0;
                buy_price_tmp = get_hoga_unit_price(buy_price, jongmok_cd, 1); // 매수호가 구하기

                int buy_ord_stock_cnt = 0;
                buy_ord_stock_cnt = (int)(buy_amt / buy_price_tmp); // 매수주문 주식 수 구하기

                write_msg_log("종목코드 : [ " + jongmok_cd + " ]\n", 0);
                write_msg_log("종목명   : [ " + get_jongmok_nm(jongmok_cd) + " ]\n", 0);
                write_msg_log("매수금액 : [ " + buy_amt.ToString() + " ]\n", 0);
                write_msg_log("매수가   : [ " + buy_price.ToString() + " ]\n", 0);

                // 이미 존재하는 종목( from TB_ACCNT_INFO ) 이면 매수 안함
                int own_stock_cnt = 0;
                own_stock_cnt = get_own_stock_cnt(jongmok_cd);

                // 여기에 최우선 매수호가 조회 메서드 추가?

                if (own_stock_cnt > 0)
                {
                    write_msg_log("보유주식이라 추가 매수 안함\n", 0);
                    continue;
                }

                // 매수주문 미체결 수량이 있으면 매수주문 실행 안함
                string buy_not_chegyul_yn = null;
                buy_not_chegyul_yn = get_buy_not_chegyul_yn(jongmok_cd);

                // 최우선 매수호가 조회
                int for_flag = 0;
                int for_cnt = 0;
                for_flag = 0;
                g_buy_hoga = 0;

                for(; ; )
                {
                    g_rqname = "";
                    g_rqname = "호가조회";
                    g_flag_hoga = 0;
                    axKHOpenAPI1.SetInputValue("종목코드", jongmok_cd);

                    string scr_no_2 = null;
                    scr_no_2 = "";
                    scr_no_2 = get_scr_no();

                    axKHOpenAPI1.CommRqData(g_rqname, "opt10004", 0, scr_no_2);

                    try
                    {
                        for_cnt = 0;
                        for(; ; )
                        {
                            if(g_flag_hoga == 1)
                            {
                                delay(200);
                                axKHOpenAPI1.DisconnectRealData(scr_no_2);
                                for_flag = 1;
                                break;
                            }

                            else
                            {
                                write_sys_log("'호가조회' 완료 대기중...\n", 0);
                                delay(200);
                                for_cnt++;
                                if(for_cnt == 5)
                                {
                                    for_flag = 0;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        write_sys_log("최우선 호가조회 중 다음 에러 발생 : [" + ex.Message + " ]\n", 0);
                    }

                    axKHOpenAPI1.DisconnectRealData(scr_no_2);

                    if(for_flag == 1)
                    {
                        break;
                    }
                    else if(for_flag == 0)
                    {
                        delay(200);
                        continue;
                    }

                    delay(200);
                }
                

                if(buy_not_chegyul_yn == "Y")
                {
                    write_sys_log("해당 종목에 대한 미체결 매수주문이 큐에 남아있으므로 매수하지 않음\n", 0);
                    continue;
                }

                // 매수주문 전에 매수가와 최우선 매수호가를 비교하여 매수가가 더 크다면 매수하지 않도록 함
                if(buy_price > g_buy_hoga)
                {
                    write_sys_log("해당 종목의 매수가가 최우선 매수호가보다 크므로 매수주문 하지 않음 \n", 0);
                    continue;
                }

                g_flag_buy = 0;
                g_rqname = "매수주문";

                string scr_no = null;
                scr_no = "";
                scr_no = get_scr_no();

                int ret = 0;

                // 매수주문 요청
                ret = axKHOpenAPI1.SendOrder("매수주문", scr_no, g_accnt_no, 1, jongmok_cd, buy_ord_stock_cnt, buy_price, "00", "");

                if(ret == 0)
                {
                    write_sys_log("매수주문 SendOrder 호출 성공\n", 0);
                    write_sys_log("종목코드 : [ " + jongmok_cd + " ]\n", 0);
                }

                else
                {
                    write_sys_log("매수주문 SendOrder 호출 실패\n", 0);
                    write_sys_log("종목코드 : [ " + jongmok_cd + " ]\n", 0);
                }

                delay(200); // 0.2초 지연

                for (; ; )
                {
                    if (g_flag_buy == 1)
                    {
                        delay(200);
                        axKHOpenAPI1.DisconnectRealData(scr_no);
                        break;
                    }
                    else
                    {
                        write_sys_log("매수주문 완료 대기중..\n", 0);
                        delay(200);
                        break;
                    }
                }

                axKHOpenAPI1.DisconnectRealData(scr_no);
                delay(1000);
            }

            reader.Close();
            conn.Close();
        }

        // 실시간 매도주문
        public void real_sell_ord()
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            string jongmok_cd = null;
            int target_price = 0;
            int own_stock_cnt = 0;

            write_sys_log("실시간 매도 주문 시작\n", 0);
            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 거래종목 및 계좌정보 테이블 조회
            sql = @" SELECT " +
                   "    A.JONGMOK_CD, " +
                   "    A.TARGET_PRICE, " +
                   "    A.OWN_STOCK_CNT, " +
                   " FROM " +
                   "    TB_TRD_JONGMOK A, " +
                   "    TB_ACCNT_INFO B " +
                   " WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                   " AND A.JONGMOK_CD = B.JONGMOK_CD " +
                   " AND B.ACCNT_NO " + "'" + g_accnt_no + "'" +
                   " AND B.REF_DT = TO_CHAR(SYSDATE, 'YYYYMMDD') " +
                   " AND A.SELL_TRD_YN = 'Y' AND B.OWN_STOCK_CNT > 0 ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                jongmok_cd = "";
                target_price = 0;

                jongmok_cd = reader[0].ToString().Trim();
                target_price = int.Parse(reader[1].ToString().Trim());
                own_stock_cnt = int.Parse(reader[2].ToString().Trim());

                write_msg_log("종목코드 : [" + jongmok_cd + "]\n", 0);
                write_msg_log("종목명 : [" + get_jongmok_nm(jongmok_cd) + "]\n", 0);
                write_msg_log("목표가 : [" + target_price.ToString() + "]\n", 0);
                write_msg_log("보유주식수 : [" + own_stock_cnt.ToString() + "]\n", 0);

                int sell_not_chegyul_ord_stock_cnt = 0;
                sell_not_chegyul_ord_stock_cnt = get_sell_not_chegyul_ord_stock_cnt(jongmok_cd);

                if(sell_not_chegyul_ord_stock_cnt == own_stock_cnt)  // 매도하지 않음, 이미 매도주문 완료
                {
                    continue;
                }
                else // 매도주문 생성
                {
                    int sell_ord_stock_cnt_tmp = 0;
                    sell_ord_stock_cnt_tmp = own_stock_cnt - sell_not_chegyul_ord_stock_cnt;

                    if(sell_ord_stock_cnt_tmp <= 0)
                    {
                        continue;
                    }

                    int new_target_price = 0;
                    new_target_price = get_hoga_unit_price(target_price, jongmok_cd, 0);  // 매도호가를 구함

                    g_flag_sell = 0;
                    g_rqname = "매도주문";

                    string scr_no = null;
                    scr_no = "";
                    scr_no = get_scr_no();

                    int ret = 0;

                    // 매도주문 요청
                    ret = axKHOpenAPI1.SendOrder("매도주문", scr_no, g_accnt_no, 2, jongmok_cd, sell_ord_stock_cnt_tmp, new_target_price, "00", "");

                    if (ret == 0)
                    {
                        write_sys_log("매도주문 요청 성공\n", 0);
                        write_sys_log("매도주문 요청 성공 종목코드 : [ " + jongmok_cd + " ]\n", 0);
                    }
                    else
                    {
                        write_sys_log("매도주문 요청 실패\n", 0);
                        write_sys_log("매도주문 요청 실패 종목코드 : [ " + jongmok_cd + " ]\n", 0);
                    }

                    delay(200);

                    for (; ; )
                    {
                        if(g_flag_sell == 1)
                        {
                            delay(200);
                            axKHOpenAPI1.DisconnectRealData(scr_no);
                            break;
                        }
                        else
                        {
                            write_sys_log("매도주문 완료 대기중 ... \n", 0);
                            delay(200);
                            break;
                        }
                    }

                    axKHOpenAPI1.DisconnectRealData(scr_no);

                }
            }  // end of while(reader.Read())

            reader.Close();
            conn.Close();
        }

        // 보유주식수 Call 메서드
        public int get_own_stock_cnt(string jongmok_cd)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            int own_stock_cnt = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 계좌정보 테이블 조회
            sql = @"SELECT NVL(MAX(OWN_STOCK_CNT), 0) OWN_STOCK_CNT
                        FROM
                    TB_ACCNT_INFO
                        WHERE USER_ID = " + "'" + g_user_id + "'" +
                        " AND JONGMOK_CD = " + "'" + jongmok_cd + "'" +
                        " AND ACCNT_NO = " + "'" + g_accnt_no + "'" +
                        " AND REF_DT = TOCHAR(SYSDATE, 'YYYYMMDD') ";
            cmd.CommandText = sql;

            reader = cmd.ExecuteReader();
            reader.Read();

            own_stock_cnt = int.Parse(reader[0].ToString());  // 보유 주식수 구함

            reader.Close();
            conn.Close();

            return own_stock_cnt;
        }

        // 매수주문 전 미체결 매수주문 확인 메서드
        // TODO: sql 확인 작업 필요
        public string get_buy_not_chegyul_yn(string jongmok_cd)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            int buy_not_chegyul_ord_stock_cnt = 0;
            string buy_not_chegyul_yn = null;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 주문내역과 체결내역 테이블 조회
            sql = @"SELECT
                        NVL(sum(ord_stock_cnt - CHEGYUL_STOCK_CNT), 0) buy_not_chegyul_ord_stock_cnt
                    FROM
                        (
                            SELECT ord_stock_cnt,
                                ( select nvl(max(b.CHEGYUL_STOCK_CNT), 0) CHEGYUL_STOCK_CNT
                                    from tb_chegyul_lst b
                                     where b.user_id = a.user_id
                                       and b.accnt_no = a.accnt_no
                                       and b.ref_dt = a.ref_dt
                                       and b.jongmok_cd = a.jongmok_cd
                                       and b.ord_gb = a.ord_gb
                                       and b.ord_no = a.ord_no
                                )
                            FROM
                            TB_ORD_LST a
                            where a.ref_dt = TO_CHAR(SYSDATE, 'YYYYMMDD')
                            and a.user_id = " + "'" + g_user_id + "'" +
                            " and a.ACCNT_NO = " + g_accnt_no + "'" +
                            " and a.jongmok_cd = " + jongmok_cd + "'" +
                            " and a.ord_gb = '2' " +
                            " and a.org_ord_no = '0000000' " +
                            " and not exists ( select '1' " +
                            "                   from TB_ORD_LST b" +
                            "                   where b.user_id = a.user_id " +
                            "                   and b.accnt_no = a.accnt_no " +
                            "                   and b.ref_dt = a.ref_dt " +
                            "                   and b.jongmok_cd = a.jongmok_cd " +
                            "                   and b.ord_gb = a.ord_gb " +
                            "                   and b.org_no = a.ord_no " +
                        " ) " +
                    ") x ";

            cmd.CommandText = sql;

            reader = cmd.ExecuteReader();
            reader.Read();

            buy_not_chegyul_ord_stock_cnt = int.Parse(reader[0].ToString());  // 미체결 매수주문 주식수 구하기

            reader.Close();
            conn.Close();

            if (buy_not_chegyul_ord_stock_cnt > 0)
            {
                buy_not_chegyul_yn = "Y";
            }
            else
            {
                buy_not_chegyul_yn = "N";
            }

            return buy_not_chegyul_yn;
        }

        // 매도 주문전 미체결 매도주문 주식수 확인 메서드
        public int get_sell_not_chegyul_ord_stock_cnt(string jongmok_cd)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            int sell_not_chegyul_ord_stock_cnt = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 주문내역과 체결내역 테이블 조회
            sql = @"
                     SELECT 
                        nvl(sum(ord_stock_cnt - CHEGYUL_STOCK_CNT),0) sell_not_chegyul_ord_stock_cnt
                     FROM
                        (
                        select
                            ord_stock_cnt ord_stock_cnt,
                              ( select nvl(max(b.CHEGYUL_STOCK_CNT),0) CHEGYUL_STOCK_CNT
                                from tb_chegyul_lst b
                                where b.user_id = a.user_id
                                and b.accnt_no = a.accnt_no
                                and b.ref_dt = a.ref_dt
                                and b.jongmok_cd = a.jongmok_cd
                                and b.ord_gb = a.ord_gb
                                and b.ord_no = a.ord_no
                              ) CHEGYUL_STOCK_CNT
                        from TB_ORD_LST a
                        where a.ref_dt = TO_CHAR(SYSDATE, 'YYYYMMDD')
                        and a.user_id = " + "'" + g_user_id + "'" +
                        " and a.jongmok_cd = " + "'" + jongmok_cd + "'" +
                        " and a.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                        " and a.ord_gb = '1' " +
                        " and a.org_ord_no = '0000000 " +
                        " and not exists ( select '1' " +
                        "                   from TB_ORD_LST b " +
                        "                   where b.user_id = a.user_id " +
                        "                   and b.accnt_no = a.accnt_no " +
                        "                   and b.ref_dt = a.ref_dt " +
                        "                   and b.jongmok_cd = a.jongmok_cd " +
                        "                   and b.ord_gb = a.ord_gb " +
                        "                   and b.org_ord_no = a.ord_no " +
                        "                   )) ";

            cmd.CommandText = sql;

            reader = cmd.ExecuteReader();
            sell_not_chegyul_ord_stock_cnt = int.Parse(reader[0].ToString()); // 미체결 매도주문 주식수 가져오기

            reader.Close();
            conn.Close();

            return sell_not_chegyul_ord_stock_cnt;
        }

        // 실시간 손절주문 대상 조회
        public void real_cut_loss_ord()
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            string jongmok_cd = null;
            int cut_loss_price = 0;
            int own_stock_cnt = 0;

            write_sys_log("손절종목 조회 시작\n", 0);
            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 거래종목과 계좌정보 테이블 조회
            sql = @"SELECT " +
                  "     A.JONGMOK_CD, " +
                  "     A.CUT_LOSS_PRICE, " +
                  "     B.OWN_STOCK_CNT " +
                  "FROM " +
                  "     TB_TRD_JONGMOK A, " +
                  "     TB_ACCNT_INFO B " +
                  "WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                  " AND A.JONGMOK_CD = B.JONGMOK_CD " +
                  " AND B.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                  " AND B.REF_DT = TO_CHAR(SYSDATE, 'YYYYMMDD') " +
                  "AND A.SELL_TRD_YN = 'Y' AND B.OWN_STOCK_CNT > 0 ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                jongmok_cd = "";
                cut_loss_price = 0;

                jongmok_cd = reader[0].ToString().Trim();
                cut_loss_price = int.Parse(reader[1].ToString().Trim());
                own_stock_cnt = int.Parse(reader[2].ToString().Trim());

                write_sys_log("종목코드 : [ " + jongmok_cd + " ]\n", 0);
                write_sys_log("종목명 : [ " + get_jongmok_nm(jongmok_cd) + " ]\n", 0);
                write_sys_log("손절가 : [ " + cut_loss_price.ToString() + " ]\n", 0);
                write_sys_log("보유주식수 : [ " + own_stock_cnt.ToString() + " ]\n", 0);

                int for_flag = 0;
                int for_cnt = 0;
                g_cur_price = 0;

                for(; ; )
                {
                    g_rqname = "";
                    g_rqname = "현재가조회";
                    g_flag_cur = 0;
                    axKHOpenAPI1.SetInputValue("종목코드", jongmok_cd);

                    string scr_no = null;
                    scr_no = "";
                    scr_no = get_scr_no();

                    // 현재가 조회 요청
                    axKHOpenAPI1.CommRqData(g_rqname, "opt10001", 0, scr_no);
                    try
                    {
                        for_cnt = 0;
                        for(; ; )
                        {
                            if(g_flag_cur == 1)
                            {
                                delay(200);
                                axKHOpenAPI1.DisconnectRealData(scr_no);
                                for_flag = 1;
                                break;
                            }
                            else
                            {
                                write_sys_log("현재가조회 완료 대기중...\n", 0);
                                delay(200);
                                for_cnt++;
                                if(for_cnt == 5)
                                {
                                    for_flag = 0;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        write_sys_log("현재가 조회중 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
                    }

                    axKHOpenAPI1.DisconnectRealData(scr_no);

                    if(for_flag == 1)
                    {
                        break;
                    }
                    else if (for_flag == 0)
                    {
                        delay(200);
                        continue;
                    }
                    delay(200);

                }  // end of 현재가 조회

                if(g_cur_price < cut_loss_price)
                {
                    sell_canc_ord(jongmok_cd);

                    g_flag_sell = 0;
                    g_rqname = "매도주문";

                    string scr_no = null;
                    scr_no = "";
                    scr_no = get_scr_no();

                    int ret = 0;
                    // 매도주문 요청
                    ret = axKHOpenAPI1.SendOrder("매도주문", scr_no, g_accnt_no, 2, jongmok_cd, own_stock_cnt, 0, "03", "");

                    if(ret == 0)
                    {
                        write_sys_log("손절 매도 주문 호출 성공\n", 0);
                        write_sys_log("종목코드 : [ " + jongmok_cd + " ]\n", 0);
                    }

                    else
                    {
                        write_sys_log("손절 매도 주문 호출 실패\n", 0);
                        write_sys_log("종목코드 : [ " + jongmok_cd + " ]\n", 0);
                    }

                    delay(200);

                    for(; ; )
                    {
                        if(g_flag_sell == 1)
                        {
                            delay(200);
                            axKHOpenAPI1.DisconnectRealData(scr_no);
                            break;
                        }
                        else
                        {
                            write_sys_log("손절 매도주문 완료 대기중 ...\n", 0);
                            delay(200);
                            break;
                        }
                    }

                    axKHOpenAPI1.DisconnectRealData(scr_no);
                    update_tb_trd_jongmok(jongmok_cd);
                }
             
            }  // end of while

            reader.Close();
            conn.Close();

        }

        // 매도취소주문 메서드( 매도주문 미체결을 취소하고 시장가로 바로 던진다)
        public void sell_canc_ord(string jongmok_cd)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;
            OracleDataReader reader = null;

            string rid = null;
            string l_jongmok_cd = null;
            int ord_stock_cnt = 0;
            int ord_price = 0;
            string ord_no = null;
            string org_ord_no = null;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            // 주문내역과 체결내역 테이블 조회
            sql = @"SLECT
                        rowid rid.
                        jongmok_cd,
                        (ord_stock_cnt - (select nvl(max(b.CHEGYUL_STOCK_CNT),0) CHEGYUL_STOCK_CNT from tb_chegyul_lst b where b.user_id = a.user_id
                                                                                                                           and b.accnt_no = a.accnt_no
                                                                                                                           and b.ref_dt = a.ref_dt
                                                                                                                           and b.jongmok_cd = a.jongmok_cd
                                                                                                                           and b.ord_gb = a.ord_gb
                                                                                                                           and b.ord_no = a.ord_no )) 
                        sell_not_chegyul_ord_stock_cnt,
                        ord_price,
                        ord_no,
                        org_ord_no
                    FROM
                        TB_ORD_LST a
                    WHERE a.ref_dt = TO_CHAR(SYSDATE, 'YYYYMMDD')
                      and a.user_id = " + "'" + g_user_id + "'" +
                    " and a.accnt_no = " + "'" + g_accnt_no + "'" +
                    " and a.jongmok_cd = " + "'" + jongmok_cd + "'" +
                    " and a.ord_gb = '1' " +
                    " and a.org_ord_no = '0000000' " +
                    " and not exists (select '1' " +
                    "                 from TB_ORD_LST b " +
                    "                 where b.user_id = a.user_id" +
                    "                   and b.accnt_no = a.accnt_no" +
                    "                   and b.ref_dt = a.ref_dt" +
                    "                   and b.jongmok_cd = a.jongmok_cd" +
                    "                   b.ord_gb = a.ord_gb" +
                    "                   and b.org_ord_no = a.org_ord_no" +
                    "                   )";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                rid = "";
                l_jongmok_cd = "";
                ord_stock_cnt = 0;
                ord_price = 0;
                ord_no = "";
                org_ord_no = "";

                rid = reader[0].ToString().Trim();
                l_jongmok_cd = reader[1].ToString().Trim();
                ord_stock_cnt = int.Parse(reader[2].ToString().Trim());
                ord_price = int.Parse(reader[3].ToString().Trim());
                ord_no = reader[4].ToString().Trim();
                org_ord_no = reader[5].ToString().Trim();
                ///////////////////////////////////////////////////////////////////

                g_flag_cancel_sell = 0;
                g_rqname = "매도취소주문";

                string scr_no = null;
                scr_no = "";
                scr_no = get_scr_no();

                int ret = 0;
                // 매도취소주문 요청
                ret = axKHOpenAPI1.SendOrder("매도취소주문", scr_no, g_accnt_no, 4, l_jongmok_cd, ord_stock_cnt, 0, "03", ord_no);

                if(ret == 0)
                {
                    write_sys_log("매도취소주문 호출 성공\n", 0);
                    write_sys_log("종목코드 : [ " + l_jongmok_cd + " ]\n", 0);
                }

                else
                {
                    write_sys_log("매도취소주문 호출 실패\n", 0);
                    write_sys_log("종목코드 : [ " + l_jongmok_cd + " ]\n", 0);
                }

                delay(200);

                for(; ; )
                {
                    if(g_flag_cancel_sell == 1)
                    {
                        delay(200);
                        axKHOpenAPI1.DisconnectRealData(scr_no);                    
                        break;
                    }
                    else
                    {
                        write_sys_log("매도취소주문 완료 대기중 ...\n", 0);
                        delay(200);
                        break;
                    }
                }

                axKHOpenAPI1.DisconnectRealData(scr_no);
                delay(1000);
            }

            reader.Close();
            conn.Close();
        }

        // 손절 종목 DB에서 매수거래여부 'N'으로 설정하는 메서드
        public void update_tb_trd_jongmok(string jongmok_cd)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            sql = @"UPDATE TB_TRD_JONGMOK set buy_trd_yn = 'N', updt_dtm = SYSDATE, updt_id = 'c##TAEWOOBOT' " +
                    "WHERE user_id = " + "'" + g_user_id + "'" +
                    " and jongmok_cd = " + "'" + jongmok_cd + "'";

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                write_sys_log("종목매수여부 업데이트 시 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
            }

            conn.Close();
        }

        // 호가가격단위 가져오기 메서드
        public int get_hoga_unit_price(int price, string jongmok_cd, int hoga_unit_jump)
        {
            int market_type;
            int rest;

            market_type = 0;

            try
            {
                // 시장구분 가져오기
                market_type = int.Parse(axKHOpenAPI1.GetMarketType(jongmok_cd).ToString());
            }

            catch (Exception ex)
            {
                write_sys_log("호가단위 가져오는 중 다음 에러 발생 : [ " + ex.Message + "]\n", 0);
            }

            // 천원 미만
            if(price < 1000)
            {
                return price + (hoga_unit_jump * 1);
            }
            // 천원 이상 오천원 미만
            else if(1000 <= price && price < 5000)
            {
                rest = price % 5;
                if(rest == 0)
                {
                    return price + (hoga_unit_jump * 5);
                }
                else if (rest < 3)
                {
                    return (price - rest) + (hoga_unit_jump * 5);
                }
                else
                {
                    return (price + (5-rest)) + (hoga_unit_jump *5);
                }
            }
            // 오천원 이상 만원 미만
            else if(price >= 5000 && price < 10000)
            {
                rest = price % 10;
                if(rest == 0)
                {
                    return price + (hoga_unit_jump * 10);
                }
                else if (rest < 5)
                {
                    return (price - rest) + (hoga_unit_jump * 10);
                }
                else
                {
                    return (price + (10 - rest)) + (hoga_unit_jump * 10);
                }
            }
            // 만원 이상 오만원 미만
            else if(price >= 10000 && price < 50000)
            {
                rest = price % 50000;
                if(rest == 0)
                {
                    return price + (hoga_unit_jump * 50);
                }
                else if(rest < 25)
                {
                    return (price - rest) + (hoga_unit_jump * 50);
                }
                else
                {
                    return (price + (50 - rest)) + (hoga_unit_jump * 50);
                }
            }
            // 오만원 이상 십만원 미만
            else if(price >= 50000 && price < 100000)
            {
                rest = price % 100;
                if(rest == 0)
                {
                    return price + (hoga_unit_jump * 100);
                }
                else if(rest < 50)
                {
                    return (price - rest) + (hoga_unit_jump * 100);
                }
                else
                {
                    return (price + (100 - rest)) + (hoga_unit_jump * 100);
                }
            }
            // 십만원 이상 오십만원 미만 (장 구분)
            else if(price >= 100000 && price < 500000)
            {
                if(market_type == 10)
                {
                    rest = price % 100;
                    if(rest == 0)
                    {
                        return price + (hoga_unit_jump * 100);
                    }
                    else if (rest < 50)
                    {
                        return (price - rest) + (hoga_unit_jump * 100);
                    }
                    else
                    {
                        return (price + (100 - rest)) + (hoga_unit_jump * 100);
                    }
                }
                else
                {
                    rest = price % 500;
                    if(rest == 0)
                    {
                        return price + (hoga_unit_jump * 500);
                    }
                    else if( rest < 250)
                    {
                        return (price - rest) + (hoga_unit_jump * 500);
                    }
                    else
                    {
                        return (price + (500 - rest)) + (hoga_unit_jump * 500);
                    }
                }
            }
            // 50만원 이상
            else if (price >= 500000)
            {
                if (market_type == 10)
                {
                    rest = price % 100;
                    if (rest == 0)
                    {
                        return price + (hoga_unit_jump * 100);
                    }
                    else if (rest < 50)
                    {
                        return (price - rest) + (hoga_unit_jump * 100);
                    }
                    else
                    {
                        return (price + (100 - rest)) + (hoga_unit_jump * 100);
                    }
                }
                else
                {
                    rest = price % 1000;
                    if (rest == 0)
                    {
                        return price + (hoga_unit_jump * 1000);
                    }
                    else if (rest < 500)
                    {
                        return (price - rest) + (hoga_unit_jump * 1000);
                    }
                    else
                    {
                        return (price + (1000 - rest)) + (hoga_unit_jump * 1000);
                    }
                }
            }

            return 0;
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

            cur_dtm = "[" + cur_dt + " " + cur_tm + "]";

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
                    textBox_msg_log.BeginInvoke(new Action(() => textBox_msg_log.AppendText("\n" + cur_dtm + text + Environment.NewLine)));
                    
                }

                else
                {
                    this.textBox_msg_log.AppendText("\n" + cur_dtm + text + Environment.NewLine);
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

            cur_dtm = "[" + cur_dt + " " + cur_tm + "]";

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
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.AppendText("\n" + cur_dtm + text + Environment.NewLine)));
                }

                else
                {
                    this.textBox_err_log.AppendText("\n" + cur_dtm + text + Environment.NewLine);
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
                    write_msg_log("delay() ex.Message : [" + ex.Message + "]\r\n", 0);
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
            write_msg_log("사용자 아이디   : [" + g_user_id + "]\n", 0);
            write_msg_log("사용자 계좌번호 : [" + g_accnt_no + "]\n", 0);
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
                write_sys_log("SELECT TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\r\n", 0);
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

                write_sys_log("거래종목(TB_TRD_JONGMOK)이 조회되었습니다. \r\n", 0);
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
                               "NULL" +
                            ")";

                    cmd.CommandText = sql;
                    try
                    {
                        cmd.ExecuteNonQuery();
                        write_sys_log("종목코드 : [" + jongmok_cd + "]" + "(이)가 삽입되었습니다\r\n", 0);
                    }
                    catch (Exception ex)
                    {
                        write_sys_log("종목삽입(insert TB_TRD_JONGMOK) 중 에러발생 : ["
                                       + ex.Message + "]", 0);
                    }
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

                    write_msg_log("종목코드 [" + jongmok_cd + "] 가 수정되었습니다\r\n", 0);
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

                    write_msg_log("종목코드 [" + jongmok_cd + "] 가 삭제되었습니다\r\n", 0);
                }
            }
        }

        // Test 체크박스
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                g_test_checked = true;
            }
            else
            {
                g_test_checked = false;
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

            write_sys_log("AUTO TRADING SYSTEM is just started \r\n", 0);
            g_is_thread = 1;
            thread1 = new Thread(new ThreadStart(m_thread1));
            thread1.Start();
        }


        // -------------------- Strategy1 --------------------  //
        // 코스닥 종목 조회 메서드
        public void get_Kosdaq_lst()
        {
            string ret = null;

            ret = "";
            ret = axKHOpenAPI1.GetCodeListByMarket("10");
            string[] jongmok_lst = ret.Split(';');

     
            for (int i = 0; i < jongmok_lst.Length; i++)
            {
                //write_msg_log(i + "번째 종목 : [ " + jongmok_lst[i] + " ][ " + get_jongmok_nm(jongmok_lst[i]) + " ]\n", 0);

                for (; ; )
                {
                    g_rqname = "";
                    g_rqname = "거래량조회";
                    g_flag_cur = 0;
                    axKHOpenAPI1.SetInputValue("종목코드", jongmok_lst[i]);

                    int for_cnt = 0;
                    int for_flag = 0;

                    string scr_no = null;
                    scr_no = "";
                    scr_no = get_scr_no();

                    // 현재가 조회 요청
                    axKHOpenAPI1.CommRqData(g_rqname, "opt10001", 0, scr_no);
                    try
                    {
                        for_cnt = 0;
                        for (; ; )
                        {
                            if (g_flag_cur == 1)
                            {
                                delay(200);
                                axKHOpenAPI1.DisconnectRealData(scr_no);
                                for_flag = 1;
                                break;
                            }
                            else
                            {
                                write_sys_log("거래량조회 완료 대기중...\n", 0);
                                delay(200);
                                for_cnt++;
                                if (for_cnt == 5)
                                {
                                    for_flag = 0;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        write_sys_log("거래량 조회중 다음 에러 발생 : [ " + ex.Message + " ]\n", 0);
                    }

                    axKHOpenAPI1.DisconnectRealData(scr_no);

                    if (for_flag == 1)
                    {
                        break;
                    }
                    else if (for_flag == 0)
                    {
                        delay(200);
                        continue;
                    }
                    delay(200);

                }  // end of 거래량 조회
            }

            Insert_strategy1(jongmok_lst);
        }

        public void Insert_strategy1(string[] jongmok_lst)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            string sql = null;

            conn = null;
            conn = connect_db();

            cmd = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            for (int i = 0; i < strategy1_lst.Count; i++)
            {
                if (strategy1_lst.Count != jongmok_lst.Length)
                {
                    write_sys_log("거래량 리스트의 갯수와 종목 리스트의 갯수가 다름.\n", 0);
                    break;
                }

                sql = @"Insert into strategy1 (jongmok_cd, jongmok_nm, liquidity, buy_amt, sell_amt, update_time) 
                        Values (" + "'" + jongmok_lst[i] + "', " +
                                    "'" + get_jongmok_nm(jongmok_lst[i]) + "', " +
                                          strategy1_lst[i] + "," + "0, 0, to_char(sysdate, 'yyyymmdd')  )";

                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    write_sys_log("Strategy1 테이블 업데이트 중 다음 에러 발생 : [" + ex.Message + "]", 0);
                }

                write_sys_log("거래종목 : [ " + jongmok_lst[i] + " ] / 거래량 : [ " + strategy1_lst[i] + " ]\n", 0);

            }
        }

        // 거래량 호출 메서드 (1,000,000 거래량 이상 sorting)

        // 매도잔량 vs 매수잔량 비교 메서드
        // TB_TRD_JONGMOK 업데이트( Insert )

        // -------------------- Strategy1 --------------------  //


        public void m_thread1()
        {
            string cur_tm = null;
            int set_tb_accnt_flag = 0; // 1이면 호출 완료
            int set_tb_accnt_info_flag = 0; // 1이면 호출 완료

            int sell_ord_first_flag = 0; // 장시작전 매도주문 flag.

            int flag_strategy1 = 0;

            if (g_is_thread == 0)
            {
                g_is_thread = 1;
                //write_sys_log("Start AUTO Trading Thread\n", 0);
            }

            for(; ; )  // 장전 30분 무한루프 실행
            {
                cur_tm = get_cur_tm(); // 현재시각 조회
                toolStripStatusLabel1.Text = cur_tm; // 화면 하단 상태란에 메시지 출력
                                                     

                if (g_test_checked == true)
                {
                    //--------------------------------- test --------------------------//
                    if (set_tb_accnt_flag == 0)
                    {
                        get_Kosdaq_lst();
                        delay(2000);
                        set_tb_accnt_flag = 1;
                        set_tb_accnt();
                    }

                    if (set_tb_accnt_info_flag == 0)
                    {
                        set_tb_accnt_info_flag = 1;
                        set_tb_accnt_info();
                    }
                    //--------------------------------- test --------------------------//

                }

                if (cur_tm.CompareTo("083001") >= 0)
                {
                    
                    // 계좌조회, 계좌정보 조회, 보유종목 매도주문 수행
                    if (set_tb_accnt_flag == 0)
                    {
                        set_tb_accnt_flag = 1;
                        set_tb_accnt();
                    }
                    if(set_tb_accnt_info_flag == 0)
                    {
                        set_tb_accnt_info_flag = 1;
                        set_tb_accnt_info();
                    }
                    if(sell_ord_first_flag == 0)
                    {
                        sell_ord_first(); // 보유종목 매도 => 보유종목을 왜 매도 하는거지?
                        sell_ord_first_flag = 1;
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

                        // 10분에 한번씩 거래량 체크
                        if(int.Parse(cur_tm.Substring(2,3)) % 5 == 0 && flag_strategy1 == 0)
                        {
                            get_Kosdaq_lst();
                            flag_strategy1 = 1;
                        }
                        else if(int.Parse(cur_tm.Substring(2, 3)) % 5 != 0)
                        {
                            flag_strategy1 = 0;
                        }


                        // 장 중 매수 or 매도 실행
                        real_buy_ord();  // 실시간 매수주문 메서드 호출
                        
                        delay(200);  // 장중 무한루프 0.2초씩 sleep
                        real_sell_ord();  // 실시간 매도주문 메서드 호출

                        delay(200);
                        real_cut_loss_ord();  // 실시간 손절 주문 메서드 호출


                    }
                }

                delay(200);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if(g_is_thread == 0)
            {
                write_sys_log("자동매매가 시작되지 않아 중지하지 않습니다\n", 0);
                return;
            }

           // write_sys_log("Stop AUTO TRADING \r\n", 0);

            try
            {
                thread1.Abort();
            }

            catch (Exception ex)
            {
                write_sys_log("자동매매 중지 중 오류발생 : [" + ex.Message + "]\r\n", 0);
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
            write_sys_log("Stopped AUTO TRADING \r\n", 0);
        }


    }
}
