using EdmApiClientLibrary;
using EdmApiClientLibrary.Enums;
using EdmApiClientLibrary.Models;
using EdmApiClientLibrary.Utils;
using EmailWY.CommonHelper;
using mshtml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace EmailWY
{
    public partial class MainFrm : Form
    {
        #region 全局变量
        private string loginUrl = "https://mail.163.com/index_alternate.htm";
        private string appBaseTitle = "163邮箱客户端 - ";
        private string Sid = string.Empty;

        private bool _isLogin = false;//登陆是否成功
        //private bool _isWrite = false;//是否点击了"写信"按钮
        private bool _isRunning = false;//是否进入发邮件流程

        private Order order = null;
        private static DeploymentTarget dTarget = DeploymentTarget.Qy;
        private int target = (int)dTarget;//如果为1,就用假数据

        private IEdmClient EdmClient = null;
        private string vNo = "1.0.0.25";//备用

        private System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
        private DateTime startTime = new DateTime();
        #endregion

        #region 重启用
        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        public const int WM_CLOSE = 0x10;
        #endregion

        public MainFrm()
        {
            InitializeComponent();

            BrEmail.ScriptErrorsSuppressed = true;
            BrEmail.Url = new Uri(loginUrl);
            //btnStart.Enabled = false;//禁用

            WriteConfig();//写入Json配置文件
            ShowVersion();//显示版本号
            GetClient();//实例化客户端
        }

        //登陆方法
        private async void Login()
        {
            if (_isLogin) return;
            try
            {
                while (order == null)
                {
                    await Task.Delay(1000);

                    #region 取单
                    order = await GetOrderCommon();//取单
                    if (order == null)
                    {
                        Text = $@"{appBaseTitle}等待15秒-重新取单登陆";
                        AddRichTextBoxLog("等待15秒-重新取单登陆");
                        await WhileTime(15, "重新取单登陆");
                        continue;
                    }
                    if (string.IsNullOrEmpty(order.SenderEmailAddress) || string.IsNullOrEmpty(order.SenderPassword) || order.SenderEmailAddress == "undefined" || order.SenderPassword == "undefined")
                    {
                        AddRichTextBoxLog("账号密码不能为空"); await WhileTime(10, "账号密码不能为空");
                        continue;
                    }
                    #endregion

                    #region 获取登录参数,及点击登陆按钮
                    HtmlDocument paydoc = BrEmail.Document;
                    if (paydoc.GetElementById("idInput") == null)
                    {
                        await WhileTime(5, "未获取到登陆框");
                        continue;
                    }
                    paydoc.GetElementById("idInput").SetAttribute("value", order.SenderEmailAddress);

                    if (paydoc.GetElementById("pwdInput") == null)
                    {
                        await WhileTime(5, "未获取到密码框");
                        continue;
                    }
                    paydoc.GetElementById("pwdInput").SetAttribute("value", order.SenderPassword);

                    if (paydoc.GetElementById("loginBtn") == null)
                    {
                        await WhileTime(5, "未获取到登陆按钮");
                        continue;
                    }
                    paydoc.GetElementById("loginBtn").InvokeMember("Click");//登陆

                    await Task.Delay(5000);
                    #endregion

                    #region 特殊情况

                    var errDiv = paydoc.GetElementById("bubbleLayerWrap");
                    if (errDiv != null && !string.IsNullOrEmpty(errDiv.InnerText))
                    {
                        await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, errDiv.InnerHtml, null, "帐号或密码错误");
                        if (errDiv.InnerText.Contains("帐号或密码错误") || errDiv.InnerText.Contains("冻结"))
                        {
                            AddRichTextBoxLog(order.SenderEmailAddress + ",帐号或密码错误...");

                            paydoc.GetElementById("idInput").SetAttribute("value", "");
                            paydoc.GetElementById("pwdInput").SetAttribute("value", "");
                            paydoc.GetElementById("idInput").Focus();

                            //密码输错，冻结
                            await EdmClient.UpdateSenderStatus(order.SenderEmailAddress, SenderStatus.WrongPassword);
                            continue;
                        }
                    }
                    else
                    {
                        AddRichTextBoxLog("登陆成功..." + order.SenderEmailAddress);
                        _isLogin = true;
                        break;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                AddRichTextBoxLog("登陆出错:" + ex.Message);
                StringHelper.WriteLog("LoginErr", ex.ToString());
                await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "登陆出错:" + ex.Message);
                ReLogin("登陆出错:" + ex.Message);//重启
            }
        }

        //Webbrowser加载
        private void BrEmail_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                Text = $"{appBaseTitle}页面加载中,请稍后..." + BrEmail.ReadyState;

                //while (true)
                //{
                //    Thread.Sleep(100);
                //    if (BrEmail.ReadyState == WebBrowserReadyState.Complete)
                //        break;
                //}

                if (BrEmail.ReadyState != WebBrowserReadyState.Complete)
                {
                    Text = $"{appBaseTitle}页面繁忙..." + BrEmail.ReadyState;

                    //主要是Interactive和Loading
                    //if (BrEmail.IsBusy || BrEmail.ReadyState == WebBrowserReadyState.Interactive || BrEmail.ReadyState == WebBrowserReadyState.Loading)
                    //{
                        StringHelper.WriteLog("Interactive", BrEmail.ReadyState + ",开始计时...");

                        startTime = DateTime.Now;
                        timer1.Interval = 1000;
                        timer1.Enabled = true;
                        timer1.Tick += new EventHandler(timer1EventProcessor);
                    //}
                    return;
                }

                timer1.Stop();//停止计时

                if (BrEmail.Document.Body.InnerText.Contains("Navigation to the webpage was canceled") ||
                    BrEmail.Document.Body.InnerText.Contains("已取消网页导航") || BrEmail.Document.Body.InnerText.Contains("无法显示此页"))
                {
                    Text = $"{appBaseTitle}没有网络,即将重置...";
                    AddRichTextBoxLog("没有网络,即将重置...");
                    StringHelper.WriteLog("NoNetWork", "无网络:" + BrEmail.Document.Body.InnerHtml);

                    HardwareUtil.ResetNetwork();
                    Task.Delay(7000);
                    BrEmail.Navigate(loginUrl);
                    Task.Delay(3000);
                    return;
                }

                if (BrEmail.Url.ToString().Contains(loginUrl))
                {
                    Text = $"{appBaseTitle}登陆页面加载完成,登陆状态:" + _isLogin;
                    //btnStart.Enabled = true;

                    var enhttpblock = BrEmail.Document.GetElementById("enhttpblock");
                    if (enhttpblock != null)
                    {
                        if (enhttpblock.InnerText.Contains("登录过程有点慢哦") && !enhttpblock.Style.Contains("none"))
                        {
                            var mask = BrEmail.Document.GetElementById("mask") == null ? " " : BrEmail.Document.GetElementById("mask").Style;
                            StringHelper.WriteLog("loginSlow", mask + "-----" + enhttpblock.InnerHtml);
                            AddRichTextBoxLog("登录过程有点慢哦，3秒后自动尝试普通加密方式登录");

                            //BrEmail.Document.GetElementById("idLoginBtn").InvokeMember("Click");//点击重试没有用,换重启吧
                            EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, enhttpblock.InnerHtml, null, "登录过程有点慢哦，可能是由于网络问题造成的");
                            Task.Delay(1000);
                            ReLogin("登陆过程缓慢");//重启
                        }
                    }

                    #region 如果已登录,去除之前留下的用户名
                    if (!_isLogin)
                    {
                        if (BrEmail.Document.GetElementById("idInput") != null)
                        {
                            //处理可能会出现undefined问题
                            BrEmail.Document.GetElementById("idInput").SetAttribute("value", " ");
                            BrEmail.Document.GetElementById("idInput").Focus();
                        }
                        Login();
                        _isLogin = true;//很重要
                    }
                    #endregion
                }
                else if (BrEmail.Url.ToString().Contains("https://mail.163.com/index.htm?errorType=Login_Timeout"))
                {
                    AddRichTextBoxLog("登陆超时,即将重启");
                    Text = $"{appBaseTitle}登陆超时,即将重启";
                    EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "登陆超时,即将重启");
                    ReLogin("登陆超时");//重启
                }
                else if (BrEmail.Url.ToString().Contains("mail.163.com/js6/main.jsp?sid=") && BrEmail.Url.ToString().Contains("module=welcome.WelcomeModule"))//首页
                {
                    Text = $"{appBaseTitle}首页加载完成,登陆状态:" + _isLogin;
                    if (_isLogin) //一定要登陆成功才可以
                    {
                        //btnStart.Enabled = false;//禁用

                        #region 去除引导页
                        if (BrEmail.Document.GetElementById("dvGuideLayer") != null)
                        {
                            EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.GetElementById("dvGuideMask").InnerHtml, null, "获取引导页面");
                            if (BrEmail.Document.GetElementById("dvGuideLayer").GetElementsByTagName("a").Count > 0)
                            {
                                BrEmail.Document.GetElementById("dvGuideLayer").GetElementsByTagName("a")[0].InvokeMember("Click");
                            }
                            Task.Delay(2000);
                            AddRichTextBoxLog("引导页已去除...");
                        }
                        #endregion

                        #region 设置姓名
                        if (BrEmail.Document.Body.InnerText.Contains("您还没设置姓名"))
                        {
                            AddRichTextBoxLog("账号异常:您还没设置姓名");
                            EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "设置姓名页面");
                            var table = BrEmail.Document.GetElementsByTagName("table")[0];
                            if (table != null)
                            {
                                var iptName = table.GetElementsByTagName("input")[0];
                                if (iptName != null)
                                {
                                    iptName.Focus();
                                    iptName.SetAttribute("value", order.SenderEmailAddress);
                                    Task.Delay(2000);
                                    SendKeys.Send("{Enter}"); AddRichTextBoxLog("已设置姓名");
                                }
                            }
                            Task.Delay(2000);
                        }
                        #endregion

                        #region 点击写信
                        if (BrEmail.Document.GetElementById("dvMultiTab").GetElementsByTagName("ul")[0].GetElementsByTagName("li").Count > 8)
                        {
                            BrEmail.Document.GetElementById("dvMultiTab").GetElementsByTagName("ul")[0].GetElementsByTagName("li")[6].GetElementsByTagName("a")[0].InvokeMember("Click");//关闭写信窗口
                            Task.Delay(3000);
                            StringHelper.WriteLog("CloseWrite", BrEmail.Document.GetElementById("dvMultiTab").GetElementsByTagName("ul")[0].GetElementsByTagName("li").Count.ToString());
                        }

                        try
                        {
                            var writeEmail = BrEmail.Document.GetElementById("dvNavTop").GetElementsByTagName("ul")[0]
                                .GetElementsByTagName("li")[1].GetElementsByTagName("span")[1];

                            writeEmail.InvokeMember("Click");//点击写信
                            Task.Delay(3000);
                            //_isWrite = true;
                        }
                        catch (Exception ex)
                        {
                            StringHelper.WriteLog("ElementErr", ex.ToString());
                            ReLogin("获取写信按钮异常");//重启
                        }
                        #endregion
                    }
                }
                else if (BrEmail.Url.ToString().Contains("mail.163.com/js6/main.jsp?sid=") && BrEmail.Url.ToString().Contains("module=compose.ComposeModule"))
                {
                    Text = $"{appBaseTitle}写信页面加载完成,登陆状态" + _isLogin;
                    if (_isLogin) //一定要点击了"写信"才可以_isWrite
                    {
                        Text = $"{appBaseTitle}即将进入发邮件流程,状态" + _isRunning;
                        if (!_isRunning)
                        {
                            MainWorkflow();
                            _isRunning = true;
                        }
                    }
                }
                else if (BrEmail.Url.ToString().Contains("https://mail.163.com/logout.htm?"))
                {
                    StringHelper.WriteLog("logout", BrEmail.Document.Body.InnerHtml);
                    EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "用户已注销");
                    ReLogin("用户已注销");
                }
                else if (BrEmail.Url.ToString().Contains("email2.163.com"))
                {
                    StringHelper.WriteLog("email2—163", BrEmail.Document.Body.InnerHtml);
                    BrEmail.Navigate(loginUrl);
                    _isLogin = false;
                }
                else if (BrEmail.Url.ToString().Contains("https://reg.163.com/logins.jsp"))
                {
                    AddRichTextBoxLog(order.SenderEmailAddress + ",该账号被风控将被冻结");
                    Text = $"{appBaseTitle}账号被风控,将被冻结";
                    StringHelper.WriteLog("reg163-logins", BrEmail.Document.Body.InnerHtml);
                    EdmClient.UpdateSenderStatus(order.SenderEmailAddress, SenderStatus.TemporarilyLocked);
                    ReLogin("账号风控");//重启
                }
            }
            catch (Exception ex)
            {
                AddRichTextBoxLog("服务器正忙:" + ex.Message);
                StringHelper.WriteLog("DocumentCompletedErr", ex.ToString());
                EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "服务器正忙:" + ex.Message);
                ReLogin("服务器正忙:" + ex.Message);//重启
            }
        }

        //主流程
        private async void MainWorkflow()
        {
            await Task.Delay(1000);

            try
            {
                Text = $"{appBaseTitle}开始发邮件";
                while (true)
                {
                    string privateEmail = "";//密送
                    try
                    {
                        #region 判断参数
                        if (order == null)
                        {
                            order = await GetOrderCommon();//取单
                            Text = $"{appBaseTitle}取单中...";
                        }
                        if (order == null)
                        {
                            await WhileTime(15, "重新取单进入发邮件流程");
                            Text = $@"{appBaseTitle}等待15秒-重新取单进入发邮件流程"; AddRichTextBoxLog("等待15秒-重新取单进入发邮件流程");
                            continue;
                        }
                        if (string.IsNullOrEmpty(order.ReceiverEmailAddress) && order.BccReceiverEmailList == null)
                        {
                            AddRichTextBoxLog("收件人、密送人为空,无法开始发邮件流程...");
                            break;
                        }
                        if (string.IsNullOrEmpty(order.MailSubject) && string.IsNullOrEmpty(order.MailBody))
                        {
                            AddRichTextBoxLog("主题、内容为空,无法开始发邮件流程...");
                            break;
                        }
                        #endregion

                        #region 如果当前寄件人已经改变了,就要重启(测试环境不适用)
                        if (target != 1)
                        {
                            try
                            {
                                var spnUid = BrEmail.Document.GetElementById("spnUid");
                                if (spnUid != null && order != null)
                                {
                                    if (order.SenderEmailAddress.ToUpper() != spnUid.InnerText.Trim().ToUpper())
                                    {
                                        StringHelper.WriteLog("ChangeUser", "现登陆人:" + order.SenderEmailAddress.ToUpper() + ";;原登陆人:" + spnUid.InnerText.Trim().ToUpper());
                                        Text = $"{appBaseTitle}即将重启切换登陆人";
                                        ReLogin("当前寄件人已经改变");//重启
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                StringHelper.WriteLog("ChangeUserErr", ex.ToString());
                            }
                        }
                        #endregion

                        #region  如果有密送人的话
                        int clickEmail = 0;//计算是否给发送人/密送人、主题赋值了
                        if (order.BccReceiverEmailList != null)
                        {
                            if (order.BccReceiverEmailList.Count > 0)
                            {
                                privateEmail = string.Join(",", order.BccReceiverEmailList.ToArray());
                            }
                        }

                        if (!string.IsNullOrEmpty(privateEmail))
                        {
                            var divs = GetElement("获取密送按钮异常");//获取密送按钮
                            if (divs == null)
                            {
                                AddRichTextBoxLog("未获取到密送按钮");
                                break;
                            }
                            //AddRichTextBoxLog(divs.InnerText);
                            if (!divs.InnerText.Contains("删除密送"))
                            {
                                //点击密送
                                divs.InvokeMember("Click");
                                Text = $"{appBaseTitle}点击密送...";
                                await Task.Delay(2000);
                            }
                            else
                            {
                                AddRichTextBoxLog("未点击到密送按钮");
                                break;
                            }
                        }
                        #endregion

                        #region 给收件人、密送人、主题、内容赋值
                        try
                        {
                            while (BrEmail.Document.GetElementById("dvContainer") == null)
                            {
                                StringHelper.WriteLog("GetdvContainerErrHtml", BrEmail.Document.GetElementById("dvContainer").InnerHtml);
                                AddRichTextBoxLog("未获取到dvContainer");
                                await Task.Delay(1000);
                            }

                            var divModule = BrEmail.Document.GetElementById("dvContainer").GetElementsByTagName("div")[0];
                            var divSection = divModule.GetElementsByTagName("section")[0];
                            var divHeader = divSection.GetElementsByTagName("header")[0];

                            foreach (HtmlElement ele in divHeader.GetElementsByTagName("input"))
                            {
                                //收件人和密送人
                                if (ele.GetAttribute("className").Contains("nui-editableAddr-ipt"))
                                {
                                    var title = ele.Parent.Parent.GetAttribute("title");
                                    if (!string.IsNullOrEmpty(order.ReceiverEmailAddress))
                                    {
                                        if (title == "发给多人时地址请以分号隔开")
                                        {
                                            ele.Focus();
                                            //设置收件人的值(可能为空)
                                            ele.SetAttribute("value", order.ReceiverEmailAddress == null ? "" : order.ReceiverEmailAddress);
                                            Text = $"{appBaseTitle}设置收件人的值...";
                                            await Task.Delay(3000);
                                            ele.RemoveFocus();
                                            clickEmail++;
                                        }
                                    }

                                    if (title == "该地址对于其他收件人是不可见的")
                                    {
                                        ele.Focus();
                                        ele.SetAttribute("value", privateEmail);
                                        Text = $"{appBaseTitle}设置密送人的值...";
                                        await Task.Delay(6000);
                                        ele.RemoveFocus();
                                        clickEmail++;
                                    }
                                }

                                if (!string.IsNullOrEmpty(order.MailSubject))
                                {
                                    if (ele.GetAttribute("id").Contains("subjectInput"))
                                    {
                                        ele.SetAttribute("value", order.MailSubject);//标题
                                        await Task.Delay(3000);
                                        Text = $"{appBaseTitle}设置标题...";
                                        clickEmail++;
                                        break;//跳出循环
                                    }
                                }
                            }

                            await Task.Delay(4000);

                            if (!string.IsNullOrEmpty(order.MailBody))
                            {
                                //更多发送选项
                                var divFooter = divSection.GetElementsByTagName("footer")[0];
                                var divFooterA = divFooter.GetElementsByTagName("a")[1];

                                if (divFooterA.InnerText.Trim().Contains("更多"))//发送选项
                                {
                                    divFooterA.InvokeMember("Click");
                                    Text = $"{appBaseTitle}点击更多发送选项...";
                                    await Task.Delay(2000);
                                }

                                var section2 = divModule.GetElementsByTagName("section")[1].NextSibling.GetElementsByTagName("b")[3];
                                if (section2 != null)
                                {
                                    section2.InvokeMember("Click");
                                    Text = $"{appBaseTitle}点击纯文本...";
                                    await Task.Delay(1000);
                                }

                                //将此邮件转换为纯文本将会遗失某些格式
                                if (BrEmail.Document.Body.InnerText.Contains("将此邮件转换为纯文本将会遗失某些格式"))
                                {
                                    var zoomMonitorDiv = BrEmail.Document.GetElementById("zoomMonitorDiv");
                                    if (zoomMonitorDiv != null)
                                    {
                                        var next = zoomMonitorDiv.NextSibling;
                                        foreach (HtmlElement ele in next.GetElementsByTagName("span"))
                                        {
                                            if (ele.GetAttribute("className").Contains("nui-btn-text") && ele.InnerText.Contains("确 定"))
                                            {
                                                ele.InvokeMember("Click");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        SendKeys.Send("{Enter}");
                                    }
                                    await Task.Delay(3000);
                                }

                                //邮件内容
                                var textarea = divModule.GetElementsByTagName("textarea")[0];
                                if (textarea != null)
                                {
                                    textarea.SetAttribute("value", StringHelper.NoHTML(order.MailBody));
                                    Text = $"{appBaseTitle}设置邮件内容...";
                                    clickEmail++;
                                    await Task.Delay(5000);
                                }
                                else
                                {
                                    StringHelper.WriteLog("textareaErr", divFooterA.InnerHtml);
                                    AddRichTextBoxLog("设置邮件内容失败");
                                }

                                //再次点击"纯文本"
                                divModule.GetElementsByTagName("section")[1].NextSibling.GetElementsByTagName("b")[3].InvokeMember("Click");
                                await Task.Delay(3000);

                                //将此邮件转换为纯文本将会遗失某些格式
                                if (BrEmail.Document.Body.InnerText.Contains("将此邮件转换为纯文本将会遗失某些格式"))
                                {
                                    var zoomMonitorDiv = BrEmail.Document.GetElementById("zoomMonitorDiv");
                                    if (zoomMonitorDiv != null)
                                    {
                                        var next = zoomMonitorDiv.NextSibling;
                                        foreach (HtmlElement ele in next.GetElementsByTagName("span"))
                                        {
                                            if (ele.GetAttribute("className").Contains("nui-btn-text") && ele.InnerText.Contains("确 定"))
                                            {
                                                ele.InvokeMember("Click");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        SendKeys.Send("{Enter}");
                                    }
                                    await Task.Delay(3000);
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            StringHelper.WriteLog("GetWriterErr", ex.ToString());
                            AddRichTextBoxLog("GetWriterErr错误:" + ex.Message);
                            clickEmail = 0;
                            break;
                        }
                        #endregion

                        #region 点击发送
                        if (clickEmail >= 2)
                        {
                            var divs = GetElement("获取发送按钮异常");//获取发送按钮
                            if (divs != null)
                            {
                                divs.InvokeMember("Click");//点击发送
                                Text = $"{appBaseTitle}邮件发送中...";
                                await Task.Delay(5000);
                            }
                            else
                            {
                                AddRichTextBoxLog("未点击到发送按钮");
                                break;
                            }
                        }
                        else
                        {
                            StringHelper.WriteLog("SendFail", clickEmail + "," + order.SenderEmailAddress);
                            AddRichTextBoxLog("发送失败");
                            break;
                        }
                        #endregion

                        #region 特殊情况
                        await SpecialHandle(BrEmail.Document);//特殊情况处理

                        if (BrEmail.Document.Body.InnerText.Contains("确定真的不需要写主题吗"))
                        {
                            SendKeys.Send("{Enter}"); await Task.Delay(3000);
                        }
                        #endregion

                        #region 邮箱地址无效处理
                        if (BrEmail.Document.Body.InnerText.Contains("以下邮箱地址无效"))
                        {
                            try
                            {
                                await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "邮箱地址无效"); //句柄错误
                                //StringHelper.WriteLog("GetErrorEmailHtml", "邮箱地址无效页面" + order.OrderNo+"------"+BrEmail.Document.Body.InnerHtml);
                                Text = $"{appBaseTitle}获取错误邮箱地址...";
                                var html = new HtmlAgilityPack.HtmlDocument();
                                html.LoadHtml(BrEmail.Document.Body.InnerHtml);
                                var document = html.DocumentNode;

                                var urlnode = document.SelectNodes("//div[@class='nui-msgbox-iconText-text']")[0];
                                var chid = urlnode.SelectNodes("label");
                                var count = chid.Count;//错误条数

                                //把错误的收件人记录下来,后面再出现就直接排除了
                                string errorEmail = string.Empty;
                                foreach (var item in chid)
                                {
                                    if (!string.IsNullOrEmpty(item.InnerText))
                                    {
                                        errorEmail += item.InnerText + ";";
                                    }
                                    await EdmClient.ReportWrongReceiver(item.InnerText.Trim());//回报错误收件人
                                }
                                StringHelper.WriteLog("GetErrorEmailList", errorEmail);

                                if (order.BccReceiverEmailList != null)
                                {
                                    //如果密送人全错(比较少见,所以重启吧)
                                    if (count == order.BccReceiverEmailList.Count)
                                    {
                                        ReLogin("密送人错误"); break;
                                    }
                                }
                                if (!string.IsNullOrEmpty(order.ReceiverEmailAddress) && count == 1)
                                {
                                    ReLogin("收件人错误"); break;
                                }

                                SendKeys.Send("{Enter}");
                                await Task.Delay(3000);
                                await SpecialHandle(BrEmail.Document);//特殊情况处理
                                await Task.Delay(2000);

                                //因为错误邮件一次只显示三条,所以必须循环处理
                                if (count >= 3)
                                {
                                    int i = 0;
                                    while (BrEmail.Document.Body.InnerText.Contains("以下邮箱地址无效") && i < 5)
                                    {
                                        SendKeys.Send("{Enter}");
                                        await Task.Delay(4000);
                                        await SpecialHandle(BrEmail.Document);//特殊情况处理
                                        await Task.Delay(3000);
                                        i++;
                                        StringHelper.WriteLog("EnterErrorEmail", "点击次数:" + i);
                                    }
                                }
                                Text = $"{appBaseTitle}获取错误邮箱地址结束...";
                            }
                            catch (Exception ex)
                            {
                                StringHelper.WriteLog("GetErrorEmailErr", ex.ToString());
                                break;
                            }
                        }
                        #endregion

                        #region 发送成功
                        await Task.Delay(3000);

                        if (BrEmail.Url.ToString().Contains("df=mail163_letter") || BrEmail.Document.Body.InnerText.Contains("发送成功"))
                        {
                            AddRichTextBoxLog("邮件已成功发送,订单号:" + order.OrderNo);
                            StringHelper.WriteLog("SendSuccess", order.MailSubject + ",邮件已成功发送,订单号" + order.OrderNo);
                            await EdmClient.ReportOrderHasBeenSent(order.OrderNo);//修改单状态

                            try
                            {
                                var divModule2 = BrEmail.Document.GetElementById("dvContainer");
                                var divSection = divModule2.GetElementsByTagName("section")[2];//sQ1
                                foreach (HtmlElement ele in divModule2.GetElementsByTagName("a"))
                                {
                                    var text = ele.InnerText;
                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        if (text.Trim().Contains("继续写信"))
                                        {
                                            ele.InvokeMember("Click");
                                            await Task.Delay(3000);
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //刷新,继续写信 BrEmail.Refresh();
                                BrEmail.Document.ExecCommand("Refresh", false, null);
                                StringHelper.WriteLog("Refresh", "写完邮件后刷新页面异常:" + ex.ToString());
                            }

                            order = null; privateEmail = string.Empty;
                            continue;//不然会导致一直发重复邮件
                        }
                        else
                        {
                            AddRichTextBoxLog(order.MailSubject + ",邮件发送不成功,订单号:" + order.OrderNo);
                            StringHelper.WriteLog("SendFail", "邮件发送不成功,订单号:" + (order == null ? "" : order.OrderNo));
                            break;
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        StringHelper.WriteLog("MainWorkflowErr", ex.ToString());
                        await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "发邮件异常:" + ex.Message);
                        ReLogin("发邮件异常");//重启
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                AddRichTextBoxLog(ex.Message);
                StringHelper.WriteLog("MainWorkErr", ex.ToString());
                await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "主流程异常:" + ex.Message);
                ReLogin("主流程异常");//重启
            }

            //await WhileTime(10, "重新开始发邮件流程");//10秒后重新取单
            int nextTime = 10;
            while (nextTime-- > 1)
            {
                Text = $@"{appBaseTitle}等待{nextTime}秒-{"重新开始发邮件流程"}";
                //CloseWrite();//关闭写信页面
                await Task.Delay(1000);
            }

            //去到写信页面(首页加载东西太多容易卡死)
            _isRunning = false;
            _isLogin = true;
            await ReturnHome();
        }

        #region 获取元素及取单
        //舍弃
        private HtmlElement GetComposeModule()
        {
            HtmlElement ComposeModule = null;
            for (int i = 0; i < 5; i++)
            {
                ComposeModule = BrEmail.Document.GetElementById("_dvModuleContainer_compose.ComposeModule_" + i);
                if (ComposeModule != null)
                {
                    break;
                }
            }
            if (ComposeModule != null)
            {
                return ComposeModule.GetElementsByTagName("header")[0];
            }
            else
            {
                return null;
            }
        }

        //舍弃
        private HtmlElement GetDivContainer()
        {
            try
            {
                Task.Delay(1000);

                var dvContainer = BrEmail.Document.GetElementById("dvContainer");
                if (dvContainer == null)
                {
                    dvContainer = GetComposeModule();
                }
                if (dvContainer == null)
                {
                    return null;
                }

                var divModule = dvContainer.GetElementsByTagName("div")[0];
                return divModule;
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("GetDivContainerErr", ex.ToString());
                return null;
            }
        }

        private HtmlElement GetElement(string msg)
        {
            try
            {
                var divModule = BrEmail.Document.GetElementById("dvContainer");
                if (divModule == null)
                {
                    //AddRichTextBoxLog("divModule为空");
                    divModule = GetHtmlElement(divModule);
                }
                //if (divModule == null)
                //{
                //    divModule = GetDivContainer();
                //}

                var divContainer = divModule.GetElementsByTagName("div")[0];
                if (divContainer == null)
                {
                    //AddRichTextBoxLog("divContainer为空");
                    divContainer = GetHtmlElement(divContainer);
                }
                //StringHelper.WriteLog("divContainer", divContainer.InnerHtml);
                var divHeader = divContainer.GetElementsByTagName("header")[0];
                if (divHeader == null)
                {
                    //AddRichTextBoxLog("divHeader为空");
                    divHeader = GetHtmlElement(divHeader);
                }

                var divHeaderDiv = divHeader.GetElementsByTagName("div")[0];
                if (divHeaderDiv == null)
                {
                    //AddRichTextBoxLog("divHeaderDiv为空");
                    divHeaderDiv = GetHtmlElement(divHeaderDiv);
                }

                if (msg == "获取发送按钮异常")
                {
                    return divHeaderDiv.GetElementsByTagName("span")[1];//发送按钮
                }
                else if (msg == "获取密送按钮异常")
                {
                    //return divHeaderDiv.GetElementsByTagName("div")[12].FirstChild;
                    return divHeaderDiv.GetElementsByTagName("a")[3];//密送按钮
                }
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("GetElementErr", msg + "," + ex.ToString());
            }
            return null;
        }

        //循环获取元素
        private HtmlElement GetHtmlElement(HtmlElement divModule)
        {
            int i = 0;
            try
            {
                while (divModule == null && i <= 4)
                {
                    Task.Delay(1000);
                    i++;
                }

                return divModule;
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("GetHtmlElementErr", ex.ToString());
                return null;
            }
        }

        //封装取单方法
        private async Task<Order> GetOrderCommon()
        {
            try
            {
                if (order == null)
                {
                    AddRichTextBoxLog("取单中,请稍后....");
                    Text = $"{appBaseTitle}取单中,请稍后....";
                    if (target == 1) //测试环境
                    {
                        return new DataHelper().Orders.First();
                    }
                    else
                    {
                        await Task.Delay(1000);
                        GetClient();//实例化客户端
                        if (EdmClient == null)
                        {
                            EdmClient = new EdmClient(dTarget);
                        }
                        var emailList = await EdmClient.GetOrder(SenderType.Mail163);//这里为什么会有问题呢?
                        if (emailList.Success)
                        {
                            var email = emailList.Data.FirstOrDefault();
                            if (email != null)
                            {
                                AddRichTextBoxLog(emailList.Message);
                                return email;
                            }
                        }
                        else
                        {
                            AddRichTextBoxLog("取单失败：" + emailList.Message);
                            return null;
                        }
                    }
                }
                else
                {
                    return order;
                }
            }
            catch (Exception ex)
            {
                AddRichTextBoxLog("取单出错:" + ex.Message);
                StringHelper.WriteLog("GetOrderCommon", "取单出错: " + ex.ToString());
                await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "取单出错:" + ex.Message);
            }
            AddRichTextBoxLog("取单完成....");
            Text = $"{appBaseTitle}取单完成....";
            return null;
        }
        #endregion

        #region 辅助方法

        //写log到页面上
        private void AddRichTextBoxLog(string log)
        {
            richTextBox1.SelectionStart = BrEmail.Text.Length;
            richTextBox1.AppendText($@"[{DateTime.Now.ToString("yy-MM-dd HH:mm:ss")}] {log}" + Environment.NewLine);
            richTextBox1.SelectionStart = BrEmail.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        //等待
        private async Task WhileTime(int nextTime, string msg)
        {
            while (nextTime-- > 1)
            {
                Text = $@"{appBaseTitle}等待{nextTime}秒-{msg}";
                await Task.Delay(1000);
            }
        }

        //特殊情况处理
        private async Task SpecialHandle(HtmlDocument hDoc)
        {
            try
            {
                var brEmailBody = hDoc.Body;

                #region 网络情况检测
                if (brEmailBody.InnerText.Contains("网络连接可能不正常") || brEmailBody.InnerText.Contains("网络连接已断开"))
                {
                    AddRichTextBoxLog("网络连接可能不正常");
                    await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "网络连接可能不正常");
                    await Task.Delay(3000);
                    ReLogin("网络连接可能不正常");//重启
                    return;
                }
                #endregion

                #region 设置姓名
                if (brEmailBody.InnerText.Contains("您还没设置姓名"))
                {
                    AddRichTextBoxLog("账号异常:您还没设置姓名");
                    await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "设置姓名页面");
                    var table = BrEmail.Document.GetElementsByTagName("table")[0];
                    if (table != null)
                    {
                        var iptName = table.GetElementsByTagName("input")[0];
                        if (iptName != null)
                        {
                            iptName.Focus();
                            iptName.SetAttribute("value", order.SenderEmailAddress);
                            await Task.Delay(2000);
                            SendKeys.Send("{Enter}");
                        }
                    }
                    await Task.Delay(2000);
                    return;
                }
                #endregion

                #region 验证码识别
                await IdentifyCode();
                #endregion

                #region 禁言
                if (brEmailBody.InnerText.Contains("您的帐号目前被禁止发信"))
                {
                    AddRichTextBoxLog("您的帐号目前被禁止发信");
                    await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "您的帐号目前被禁止发信");
                    await EdmClient.UpdateSenderStatus(order.SenderEmailAddress, SenderStatus.Frozen);//冻结
                    await Task.Delay(3000);
                    ReLogin("您的帐号目前被禁止发信");//重启
                    return;
                }
                #endregion

                #region 短时间发送对象过多
                if (brEmailBody.InnerText.Contains("短时间发送对象过多"))
                {
                    AddRichTextBoxLog("短时间发送对象过多");
                    await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "短时间发送对象过多");
                    await Task.Delay(3000);
                    ReLogin("短时间发送对象过多");//重启
                    return;
                }
                #endregion
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("SpecialHandleErr", ex.ToString());
                return;
            }
        }

        //识别验证码
        private async Task IdentifyCode()
        {
            #region 验证码
            if (BrEmail.Document.Body.InnerText.Contains("系统认为您需要输入验证码才能继续完成发信"))
            {
                Text = $"{appBaseTitle}系统认为您需要输入验证码才能继续完成发信，验证码辨识中....";
                AddRichTextBoxLog("系统认为您需要输入验证码才能继续完成发信，验证码辨识中....");
                await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "系统认为您需要输入验证码才能继续完成发信");
                await Task.Delay(2000);

                try
                {
                    Image vCode = ImageHelper.GetRegCodePic(BrEmail, "imgMsgBoxVerify");
                    var arr1 = ImageHelper.ImageToBytes(vCode);
                    var nCaptchaId = VcodeHelper.GetYunVcode2(arr1);

                    AddRichTextBoxLog(@"验证码辨识完成:" + nCaptchaId.ToUpper());
                    Text = $"{appBaseTitle}验证码辨识完成";
                    await Task.Delay(2000);

                    var iptCode = BrEmail.Document.GetElementsByTagName("table")[0].GetElementsByTagName("input")[0];
                    if (iptCode != null)
                    {
                        iptCode.SetAttribute("value", nCaptchaId.ToUpper());
                        await Task.Delay(1000);//设置验证码的值
                    }

                    SendKeys.Send("{Enter}");
                    await Task.Delay(4000);
                }
                catch (Exception ex)
                {
                    await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "验证码识别错误");
                    SendKeys.Send("{Enter}");
                    await Task.Delay(2000);
                }
            }
            #endregion
        }

        //回到写信页面
        private async Task ReturnHome()
        {
            await CloseWrite();//关闭多余的写信窗口

            if (BrEmail.Document.Body.InnerText.Contains("您正在写信中"))
            {
                await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "您正在写信中");
                HtmlDocument paydoc = BrEmail.Document;
                foreach (HtmlElement ele in paydoc.GetElementsByTagName("span"))
                {
                    if (ele.GetAttribute("className") == "nui-btn-text" && ele.InnerText == "离开并存草稿")
                    {
                        ele.InvokeMember("click");
                        break;
                    }
                }
                await Task.Delay(2000);
            }

            if (!string.IsNullOrEmpty(Sid))
            {
                AddRichTextBoxLog("即将回首页...");
                string faUrl = "https://hwwebmail.mail.163.com/js6/main.jsp?sid=" + Sid + "&df=mail163_letter#module=welcome.WelcomeModule|{}";//首页

                //重新回到写信页面
                //string faUrl = "https://hwwebmail.mail.163.com/js6/main.jsp?sid=" + Sid + "&df=webmail#module=compose.ComposeModule|{\"type\":\"compose\",\"fullScreen\":true}";
                BrEmail.Url = new Uri(faUrl);
                await Task.Delay(3000);
            }
        }

        //关闭写信窗口
        private async Task CloseWrite()
        {
            #region 关闭多余的写信窗口
            try
            {
                var tabDiv = BrEmail.Document.GetElementById("dvMultiTab"); if (tabDiv == null) return;
                var tabUl = tabDiv.GetElementsByTagName("ul")[0]; if (tabUl == null) return;
                var tabCount = tabUl.GetElementsByTagName("li");

                if (tabCount.Count > 8)
                {
                    AddRichTextBoxLog("关闭多余的写信窗口....");
                    StringHelper.WriteLog("CloseWrite", tabUl.GetElementsByTagName("li")[6].GetElementsByTagName("a")[0].InnerHtml);
                    tabUl.GetElementsByTagName("li")[6].GetElementsByTagName("a")[0].InvokeMember("Click");//关闭写信窗口
                    await Task.Delay(3000);

                    //这里也写一次
                    if (BrEmail.Document.Body.InnerText.Contains("您正在写信中"))
                    {
                        await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "您正在写信中");
                        HtmlDocument paydoc = BrEmail.Document;
                        foreach (HtmlElement ele in paydoc.GetElementsByTagName("span"))
                        {
                            if (ele.GetAttribute("className") == "nui-btn-text" && ele.InnerText == "离开并存草稿")
                            {
                                ele.InvokeMember("click");
                                break;
                            }
                        }
                        await Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("CloseWriteErr", "关闭写信窗口异常:" + ex.ToString());
            }
            #endregion
        }

        //检测网络
        private bool WebRequestCheck()
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
        #endregion

        #region 获取版本号等
        //获取版本号
        private string GetVersion()
        {
            var version = ApplicationDeployment.IsNetworkDeployed
                   ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                   : FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            if (version == "1.0.0.0")
            {
                version = GetAppConfigValue("Version");
            }
            if (string.IsNullOrEmpty(version)) return vNo;
            return version;
        }

        //显示版本号
        private void ShowVersion()
        {
            string version = GetVersion();

            if (target == 1)
            {
                SLblVersion.Text = $@"版本：{version}｜Dev";
            }
            else
            {
                SLblVersion.Text = $@"版本：{version}｜{dTarget}";
            }
        }

        //根据配置文件中的EdmClientType实例化EdmClient
        private void GetClient()
        {
            try
            {
                var type = GetAppConfigValue("EdmClientType");
                if (string.IsNullOrEmpty(type)) type = "1";

                lbClientType.Text = "客户端类型：" + (type == "1" ? "一般" : "产品");
                SlbRemark.Text = "客户端类型(可点击单选按钮切换)";

                switch (type)
                {
                    case "1":
                        rbTypeOne.Checked = true;
                        EdmClient = new EdmClient(dTarget);
                        break;
                    case "2":
                        rbTypeTwo.Checked = true;
                        EdmClient = new EdmProductClient(dTarget);
                        break;
                }
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("GetAppConfigErr", ex.ToString());
                EdmClient = new EdmClient(dTarget);
            }
        }
        #endregion

        #region 打开档案、加载事件处理
        private void btnStart_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确定要重启客户端吗?", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                ReLogin("手动重启");//重启
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            string myPath = System.AppDomain.CurrentDomain.BaseDirectory;
            Process.Start("explorer.exe", myPath);
        }

        private void btnOpenEdmLog_Click(object sender, EventArgs e)
        {
            try
            {
                string strFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\EdmApiClientLog";
                Process.Start("explorer.exe", strFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); return;
            }
        }

        private void rbTypeOne_Click(object sender, EventArgs e)
        {
            if (GetAppConfigValue("EdmClientType") == "2")
            {
                SetAppConfigValue(1, GetVersion());
                lbClientType.Text = "客户端类型：一般";
            }
        }

        private void rbTypeTwo_Click(object sender, EventArgs e)
        {
            if (GetAppConfigValue("EdmClientType") == "1")
            {
                SetAppConfigValue(2, GetVersion());
                lbClientType.Text = "客户端类型：产品";
            }
        }


        private void BrEmail_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            try
            {
                HtmlDocument doc = BrEmail.Document;
                HtmlElement head = doc.GetElementsByTagName("head")[0];
                HtmlElement s = doc.CreateElement("script");
                s.SetAttribute("text", "function cancelOut() { window.onbeforeunload = null; window.alert = function () { }; window.confirm=function () { }}");
                head.AppendChild(s);
                BrEmail.Document.InvokeScript("cancelOut");
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("NavigatedErr", "禁止弹窗出错: " + ex.ToString());
            }
            try
            {
                if (e.Url.ToString().Contains("mail.163.com/js6/main.jsp?sid="))
                {
                    var url = e.Url.ToString();
                    Sid = StringHelper.GetSid(url);
                    //return;
                }
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("BrEmail_NavigatedErr", ex.ToString());
            }
        }

        private void BrEmail_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            try
            {
                if (e.Url.ToString().Contains("mail.163.com/js6/main.jsp?sid="))
                {
                    var url = e.Url.ToString();
                    Sid = StringHelper.GetSid(url);
                    //return;
                }
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("BrEmail_NavigatingErr", ex.ToString());
            }
        }

        private void btnStart_Enter(object sender, EventArgs e)
        {
            ActiveControl = null; //这样就不会响应enter事件了
        }

        private void btnOpenEdmLog_Enter(object sender, EventArgs e)
        {
            ActiveControl = null; //这样就不会响应enter事件了
        }

        private void btnOpen_Enter(object sender, EventArgs e)
        {
            ActiveControl = null; //这样就不会响应enter事件了
        }

        private void timer1EventProcessor(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now.Subtract(startTime);
            int sec = (int)ts.TotalSeconds;
            StringHelper.WriteLog("Interactive", "已计时:" + (sec) + "秒");
            if (sec > 31)
            {
                timer1.Stop();
                bool isCheckNetWork = WebRequestCheck();
                StringHelper.WriteLog("Interactive", "页面卡死已超过30s,停止计时,网络质量:" + isCheckNetWork);
                if (!isCheckNetWork)
                {
                    ReLogin("页面卡死太久");
                }
                else
                {
                    BrEmail.Refresh();
                }
                return;
            }
        }
        #endregion

        #region 重启、重启前弹窗
        private void StartKiller()
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000; //5秒启动 
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            KillMessageBox();
            ((System.Windows.Forms.Timer)sender).Stop();
        }
        private void KillMessageBox()
        {
            IntPtr ptr = FindWindow(null, "MessageBox");
            if (ptr != IntPtr.Zero)
            {
                //找到则关闭MessageBox窗口 
                PostMessage(ptr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        //重启
        private void ReLogin(string msg = "")
        {
            StartKiller();
            MessageBox.Show("5秒后重启客户端,原因:" + msg, "MessageBox");
            StringHelper.WriteLog("ReLogin", msg);

            Application.ExitThread();
            Thread thtmp = new Thread(new ParameterizedThreadStart(Run));
            object appName = Application.ExecutablePath;
            Thread.Sleep(1);
            thtmp.Start(appName);
        }
        private void Run(Object appName)
        {
            Process ps = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = appName.ToString()
                }
            };
            ps.Start();

            Environment.Exit(0);
        }
        #endregion

        #region 获取或设置配置文件中的值
        private void WriteConfig()
        {
            string settingFile = AppConfig();
            if (File.Exists(settingFile))
            {
                File.Delete(settingFile);
            }
            if (!File.Exists(settingFile))
            {
                var version = GetVersion();
                ClientSetting cSet = new ClientSetting
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    EdmClientType = 1,
                    Version = version
                };
                string json = JsonConvert.SerializeObject(cSet);
                File.WriteAllText(settingFile, json);
            }
        }

        private string AppConfig()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDomain.CurrentDomain.SetData("DataDirectory", local);
            string settingFile = $@"{AppDomain.CurrentDomain.GetData("DataDirectory")}\Email_163.json";
            return settingFile;
        }

        private string GetAppConfigValue(string key)
        {
            try
            {
                string jsonfile = AppConfig();

                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonfile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        JObject o = (JObject)JToken.ReadFrom(reader);
                        var value = o[key].ToString();
                        return value;
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void SetAppConfigValue(int type, string version)
        {
            try
            {
                string settingFile = AppConfig();

                ClientSetting cSet = new ClientSetting
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    EdmClientType = type,
                    Version = version
                };
                string json = JsonConvert.SerializeObject(cSet);
                File.WriteAllText(settingFile, json);
            }
            catch (Exception)
            {
            }
        }

        public class ClientSetting
        {
            public string Time { get; set; }
            public int EdmClientType { get; set; }
            public string Version { get; set; }
        }
        #endregion

        private void btnReset_Click(object sender, EventArgs e)
        {
            HardwareUtil.ResetNetwork();
            Task.Delay(7000);
            BrEmail.Navigate(loginUrl);
            Task.Delay(3000);
        }
    }
}
