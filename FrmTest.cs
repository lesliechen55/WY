using EmailWY.CommonHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmailWY
{
    public partial class FrmTest : Form
    {
        private string loginUrl = "https://mail.163.com/index_alternate.htm";
        private string appBaseTitle = "网易163测试客户端 - ";
        private bool _isLogin = false;//登陆是否成功
        private DateTime startTime = new DateTime();
        private Timer timer2 = new Timer();

        public FrmTest()
        {
            InitializeComponent();

            BrEmail.ScriptErrorsSuppressed = true;
            BrEmail.Url = new Uri(loginUrl);

            //WebRequestTest();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //在首页获取未读邮件数量,如果大于0个，就点击进去抓取系统退信内容
            //dvContainer下面的  _dvModuleContainer_welcome.WelcomeModule_0 ul  span
            var dvContainerUl = BrEmail.Document.GetElementById("dvContainer").GetElementsByTagName("ul")[0];

            var liTitle = dvContainerUl.GetElementsByTagName("li")[0].GetAttribute("title");
            AddRichTextBoxLog(liTitle);
            if (liTitle == "无未读邮件")
            {
                AddRichTextBoxLog("没有未读邮件,直接去写信...");
                return;
            }

            //var count = dvContainerUl.GetElementsByTagName("span").Count;
            //if (count == 0)
            //{
            //    AddRichTextBoxLog("应该是没有未读邮件...");
            //    return;
            //}
            dvContainerUl.GetElementsByTagName("span")[0].InvokeMember("Click");

            if (!BrEmail.Document.Body.InnerText.Contains("系统退信"))
            {
                var writeEmail = BrEmail.Document.GetElementById("dvNavTop").GetElementsByTagName("ul")[0]
                                .GetElementsByTagName("li")[1].GetElementsByTagName("span")[1];

                writeEmail.InvokeMember("Click");//点击写信
                Task.Delay(3000);
            }

            //AddRichTextBoxLog("开始计时...");
            //startTime = DateTime.Now;

            //timer2.Interval = 1000;//设置中断时间 单位ms
            //timer2.Enabled = true;
            //timer2.Tick += new EventHandler(timer1EventProcessor);//添加事件
        }

        private void timer1EventProcessor(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now.Subtract(startTime);
            int sec = (int)ts.TotalSeconds;
            AddRichTextBoxLog("已计时:" + (sec) + "秒");

            if (sec > 10)
            {
                AddRichTextBoxLog("超过10秒了哦,停止计时");
                timer2.Stop();
                return;
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            //timer2.Stop();
            //AddRichTextBoxLog("停止计时。。。");

            //GetAdapterIndexAndIpAddress(out string ipAddress);
            //AddRichTextBoxLog(ipAddress);
        }

        private void btnOpenEdmLog_Click(object sender, EventArgs e)
        {
            bool isCheckNetWork = WebRequestTest();
            if (!isCheckNetWork)
            {
                AddRichTextBoxLog("即将重置网络。。。");
                EdmApiClientLibrary.Utils.HardwareUtil.ResetNetwork();
            }
        }

        private bool WebRequestTest()
        {
            AddRichTextBoxLog("开始检测网络状况...");
            try
            {
                System.Net.WebRequest myRequest = System.Net.WebRequest.Create(loginUrl);
                System.Net.WebResponse myResponse = myRequest.GetResponse();
            }
            catch (System.Net.WebException)
            {
                AddRichTextBoxLog("网络故障..");
                return false;
            }
            AddRichTextBoxLog("网络连接正常...");
            return true;
        }

        private void BrEmail_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            return;
            Text = $"{appBaseTitle}页面加载中,请稍后..." + BrEmail.ReadyState;
            if (BrEmail.ReadyState != WebBrowserReadyState.Complete)
            {
                Text = $"{appBaseTitle}页面繁忙..." + BrEmail.ReadyState;
                return;
            }

            if (BrEmail.Document.Body.InnerText.Contains("Navigation to the webpage was canceled") || BrEmail.Document.Body.InnerText.Contains("已取消网页导航"))
            {
                Text = $"{appBaseTitle}没有网络,即将重置..." + BrEmail.ReadyState;
                EdmApiClientLibrary.Utils.HardwareUtil.ResetNetwork();
                return;
            }

            if (BrEmail.Url.ToString().Contains(loginUrl))
            {
                //WebRequestTest();
                Text = $"{appBaseTitle}登陆页面加载完成";
                //登陆
                if (!_isLogin)
                {
                    Login();
                    _isLogin = true;//很重要
                }
            }
            else if (BrEmail.Url.ToString().Contains("module=welcome.WelcomeModule"))//首页
            {
                //WebRequestTest();
                Text = $"{appBaseTitle}首页加载完成,即将去到收信箱页面";

                //鼠标左箭头+enter也可以
                SendKeys.Send("{LEFT}");
                Task.Delay(2000);
                SendKeys.Send("{Enter}");

                //var dvNavTreeDiv = BrEmail.Document.GetElementById("dvNavTree").GetElementsByTagName("ul")[0].
                //GetElementsByTagName("li")[0].GetElementsByTagName("div")[0];
                //var dvNavTreeSpan = dvNavTreeDiv.GetElementsByTagName("span")[2];
                //dvNavTreeSpan.InvokeMember("click");//点击去到收信箱页面
            }
            else if (BrEmail.Url.ToString().Contains("module=read.ReadModule"))
            {
                //WebRequestTest();
                Text = $"{appBaseTitle}邮件详情页加载完成";

                var iframe = BrEmail.Document.Window.Frames[0];
                var content = iframe.Document.GetElementById("content");
                var table = content.GetElementsByTagName("table")[0].Parent;
                StringHelper.WriteLog("table", table.InnerHtml);

                AddRichTextBoxLog("成功取得退信内容,,,准备返回....");

                #region 点返回
                var dvContainer = BrEmail.Document.GetElementById("_dvModuleContainer_read.ReadModule_0");
                if (dvContainer == null)
                {
                    dvContainer = BrEmail.Document.GetElementById("dvContainer").GetElementsByTagName("div")[0];
                }
                var dvSpan = dvContainer.GetElementsByTagName("span")[0];
                dvSpan.InvokeMember("click");
                #endregion
            }
            else if (BrEmail.Url.ToString().Contains("module=mbox.ListModule")) //收信箱页面
            {
                //WebRequestTest();
                Text = $"{appBaseTitle}收信箱页面加载完成";

                CheckMail();
            }
        }

        private async void Login()
        {
            await Task.Delay(1000);
            HtmlDocument paydoc = BrEmail.Document;
            paydoc.GetElementById("idInput").SetAttribute("value", "xiache_06339655285@163.com");
            paydoc.GetElementById("pwdInput").SetAttribute("value", "sunza69528");
            paydoc.GetElementById("loginBtn").InvokeMember("Click");//登陆
            await Task.Delay(5000);

            AddRichTextBoxLog("登陆成功..." + "rbry8ELRYb@163.com");
            _isLogin = true;

        }

        private async void CheckMail()
        {
            await Task.Delay(5000);
            try
            {
                Text = "处理中。。。。";

                var ListModule = BrEmail.Document.GetElementById("_dvModuleContainer_mbox.ListModule_0");
                if (ListModule == null)
                {
                    ListModule = BrEmail.Document.GetElementById("dvContainer").GetElementsByTagName("div")[0];
                }

                var divtc0 = ListModule.Children[2];//tc0
                var dg0 = divtc0.Children[0];//dg0
                //StringHelper.WriteLog("dg0", dg0.InnerHtml);

                Text = "获取子节点。。。。";
                HtmlElement tv0 = null;// dg0.GetElementsByTagName("div")[7];//感觉这里会有问题
                foreach (HtmlElement ele in dg0.GetElementsByTagName("div"))
                {
                    if (ele.GetAttribute("className") == "tv0")
                    {
                        tv0 = ele;
                        break;
                    }
                }

                Text = "再次获取子节点。。。。";
                if (tv0 == null)
                {
                    AddRichTextBoxLog("未获取到内容....");
                    return;
                }

                Text = "获取系统退信的。。。。";
                foreach (HtmlElement ele in tv0.GetElementsByTagName("div"))
                {
                    if (ele.GetAttribute("className") == "rF0 kw0 nui-txt-flag0" && !string.IsNullOrEmpty(ele.InnerText))
                    {
                        if (ele.InnerText.Contains("系统退信"))
                        {
                            ele.InvokeMember("Click");
                            Text = "获取系统退信成功。。。。";
                            AddRichTextBoxLog("获取系统退信成功。。。。");
                            await Task.Delay(5000);
                            break;
                        }
                    }
                }

                Text = "获取系统退信成功。。。。";
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("err", ex.ToString());
            }
            await Task.Delay(2000);
        }

        //写log到页面上
        private void AddRichTextBoxLog(string log)
        {
            richTextBox1.SelectionStart = BrEmail.Text.Length;
            richTextBox1.AppendText($@"[{DateTime.Now.ToString("yy-MM-dd HH:mm:ss")}] {log}" + Environment.NewLine);
            richTextBox1.SelectionStart = BrEmail.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        public static void GetAdapterIndexAndIpAddress(out string ipAddress)
        {
            ipAddress = string.Empty;
           
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            try
            {
                var moc = mc.GetInstances();
                foreach (var mo in moc)
                {
                    if (mo["IPAddress"] != null && (bool)mo["IPEnabled"])
                    {
                        ipAddress = ((string[])mo["IPAddress"])[0];

                        mo.Dispose();
                        break;
                    }
                    mo.Dispose();
                }
            }
            catch (Exception ex)
            {
                ipAddress = ex.Message;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var enhttpblock = BrEmail.Document.GetElementById("enhttpblock");
            if (enhttpblock != null)
            {
                if (enhttpblock.InnerText.Contains("登录过程有点慢哦") && !enhttpblock.Style.Contains("none"))
                {
                    StringHelper.WriteLog("loginSlow", BrEmail.Document.GetElementById("mask").Style + "-----" + enhttpblock.InnerHtml);
                    AddRichTextBoxLog("登录过程有点慢哦，3秒后自动尝试普通加密方式登录");

                    Task.Delay(1000);
                    //ReLogin("登陆过程缓慢");//重启
                }
                else
                {
                    AddRichTextBoxLog("正常");
                }
            }
        }
    }
}
