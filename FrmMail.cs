using EdmApiClientLibrary;
using EdmApiClientLibrary.Enums;
using EdmApiClientLibrary.Models;
using EmailWY.CommonHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmailWY
{
    public partial class FrmMail : Form
    {
        #region 全局变量
        private string loginUrl = "https://mail.163.com/index_alternate.htm";
        private string appBaseTitle = "网易163邮箱客户端";

        private bool _isLogin = false;//登陆是否成功
        private bool _isWriter = false;
        private bool _isRunning = false;//是否进入发邮件流程

        private string TimeoutUrl = "https://mail.163.com/index.htm?errorType=Login_Timeout";
        private string logoutUrl = "https://mail.163.com/logout.htm";
        //private string email2Url = "email2.163.com";
        //private string regUrl = "reg.163.com";

        //private string SenderEmailAddress = "rbry8ELRYb@163.com";
        //private string SenderPassword = "cang8811";

        private static string Sid = string.Empty;
        private string title = "163客户端—";
        private string privateEmail = string.Empty;//密送

        private Order order = null;
        private static DeploymentTarget dTarget = DeploymentTarget.Qy;
        private int target = (int)dTarget;//如果为1,就用假数据
        private IEdmClient EdmClient;
        #endregion


        #region 重启用
        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        public const int WM_CLOSE = 0x10;
        #endregion

        public FrmMail()
        {
            InitializeComponent();

            BrEmail.ScriptErrorsSuppressed = true;
            //BrEmail.Url = new Uri(loginUrl);
            BrEmail.Navigate(loginUrl);

            GetClient();//实例化客户端
        }

        //登陆方法
        private async void Login()
        {
            try
            {
                await Task.Delay(1000);


                while (order == null)
                {
                    #region 取单
                    order = await GetOrderCommon();//取单
                    if (order == null)
                    {
                        AddRichTextBoxLog("60秒后重新取单登陆");
                        await WhileTime(60, "重新取单登陆");
                        continue;
                    }
                    if (string.IsNullOrEmpty(order.SenderEmailAddress) || string.IsNullOrEmpty(order.SenderPassword) || order.SenderEmailAddress == "undefined" || order.SenderPassword == "undefined")
                    {
                        AddRichTextBoxLog("账号密码不能为空");
                        await WhileTime(10, "账号密码不能为空");
                        continue;
                    }
                    #endregion
                }


                #region 获取登录参数,及点击登陆按钮
                HtmlDocument paydoc = BrEmail.Document;

                while (paydoc.GetElementById("idInput") == null)
                {
                    await Task.Delay(1000);
                }
                paydoc.GetElementById("idInput").SetAttribute("value", order.SenderEmailAddress);

                while (paydoc.GetElementById("idInput") == null)
                {
                    await Task.Delay(1000);
                }
                paydoc.GetElementById("pwdInput").SetAttribute("value", order.SenderPassword);

                while (paydoc.GetElementById("loginBtn") == null)
                {
                    await Task.Delay(1000);
                }
                paydoc.GetElementById("loginBtn").InvokeMember("Click");//登陆
                await Task.Delay(5000);
                #endregion

                #region 特殊情况
                if (BrEmail.Document.Body.InnerText.Contains("登陆过程有点慢哦"))
                {
                    AddRichTextBoxLog("登陆过程有点慢哦，可能是由于网络问题造成的");
                    if (BrEmail.Document.GetElementById("idLoginBtn") != null)
                    {
                        //await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "登陆过程有点慢哦，可能是由于网络问题造成的");
                        ReLogin("登陆过程缓慢");//重启
                    }
                }

                var errDiv = paydoc.GetElementById("bubbleLayerWrap");
                if (errDiv != null && !string.IsNullOrEmpty(errDiv.InnerText))
                {
                    //await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, errDiv.InnerHtml, null, "帐号或密码错误");
                    if (errDiv.InnerText.Contains("帐号或密码错误") || errDiv.InnerText.Contains("冻结"))
                    {
                        //AddRichTextBoxLog(order.SenderEmailAddress + ",帐号或密码错误...");

                        paydoc.GetElementById("idInput").SetAttribute("value", "");
                        paydoc.GetElementById("pwdInput").SetAttribute("value", "");
                        paydoc.GetElementById("idInput").Focus();

                        //密码输错，冻结
                        //await EdmClient.UpdateSenderStatus(order.SenderEmailAddress, SenderStatus.Frozen);
                        //continue;
                        ReLogin("帐号或密码错误");
                    }
                }
                else
                {
                    AddRichTextBoxLog("登陆成功...");
                    //_isLogin = true;
                }
                #endregion
            }
            catch (Exception ex)
            {
                AddRichTextBoxLog("登陆出错:" + ex.Message);
                StringHelper.WriteLog("Err", "登陆出错:" + ex.Message);
            }
        }

        //主流程
        private async void MainWorkflow()
        {
            await Task.Delay(1000);
            Text = "网易163邮箱客户端 - 开始发邮件";
            //StringHelper.DeleteLog();//删除log
            AddRichTextBoxLog(Text);
            try
            {
                while (true)
                {

                    #region 判断参数
                    if (order == null) //这里要不要写道while外面呢?
                    {
                        order = await GetOrderCommon();//取单
                    }
                    if (order == null)
                    {
                        await WhileTime(60, "重新取单进入发邮件流程");
                        Text = $@"{appBaseTitle} - 等待60秒-重新取单进入发邮件流程"; AddRichTextBoxLog(Text);
                        //StringHelper.WriteLog("WhileTime2", Text); await Task.Delay(1000 * 60);
                        continue;
                    }
                    if (string.IsNullOrEmpty(order.MailSubject) || string.IsNullOrEmpty(order.MailBody)
                        || (string.IsNullOrEmpty(order.ReceiverEmailAddress) && order.BccReceiverEmailList == null))
                    {
                        AddRichTextBoxLog("标题、内容数据为空,无法开始发邮件流程...");
                        break;
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
                        try
                        {
                            var divs = GetElement("获取密送按钮异常");//获取密送按钮
                            if (divs != null)
                            {
                                var divTw = GetHtmlElement(divs.GetElementsByTagName("div")[12]);
                                if (divTw != null)
                                {
                                    if (!divTw.FirstChild.InnerText.Contains("删除密送"))
                                    {
                                        //点击密送
                                        divTw.FirstChild.InvokeMember("Click"); AddRichTextBoxLog("点击密送");
                                        await Task.Delay(2000);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            AddRichTextBoxLog("获取密送按钮异常:" + ex.Message);
                            StringHelper.WriteLog("GetBccErr", ex.ToString());
                            privateEmail = string.Empty;
                        }
                    }
                    #endregion

                    var divModule = BrEmail.Document.GetElementById("dvContainer").GetElementsByTagName("div")[0];

                    #region 给收件人、密送人、主题赋值
                    try
                    {
                        var divSection = divModule.GetElementsByTagName("section")[0];
                        var divHeader = divSection.GetElementsByTagName("header")[0];

                        foreach (HtmlElement ele in divHeader.GetElementsByTagName("input"))
                        {
                            //收件人和密送人
                            if (ele.GetAttribute("className").Contains("nui-editableAddr-ipt"))
                            {
                                var title = ele.Parent.Parent.GetAttribute("title");
                                if (title == "发给多人时地址请以分号隔开")
                                {
                                    ele.Focus();
                                    //设置收件人的值(可能为空)
                                    ele.SetAttribute("value", order.ReceiverEmailAddress == null ? "" : order.ReceiverEmailAddress);
                                    await Task.Delay(2000);
                                    ele.RemoveFocus(); AddRichTextBoxLog("设置收件人的值");
                                    clickEmail++;
                                }

                                if (title == "该地址对于其他收件人是不可见的")
                                {
                                    ele.Focus();
                                    ele.SetAttribute("value", privateEmail); AddRichTextBoxLog("设置密送的值");
                                    await Task.Delay(6000);
                                    ele.RemoveFocus();
                                    clickEmail++;
                                }
                            }

                            if (ele.GetAttribute("id").Contains("subjectInput"))
                            {
                                ele.SetAttribute("value", order.MailSubject);//标题
                                await Task.Delay(4000);
                                clickEmail++; AddRichTextBoxLog("设置标题的值");
                            }
                        }

                        await Task.Delay(2000);

                        //更多发送选项
                        var divFooter = divSection.GetElementsByTagName("footer")[0];
                        var divFooterA = divFooter.GetElementsByTagName("a")[1];

                        if (divFooterA.InnerText.Trim().Contains("更多发送选项"))
                        {
                            divFooterA.InvokeMember("Click"); AddRichTextBoxLog("更多发送选项");
                            await Task.Delay(2000);
                        }

                        //点击纯文本
                        var section2 = divModule.GetElementsByTagName("section")[1].NextSibling.GetElementsByTagName("b")[3];
                        if (section2 != null)
                        {
                            section2.InvokeMember("Click"); AddRichTextBoxLog("点击纯文本");
                            await Task.Delay(2000);
                        }

                        //将此邮件转换为纯文本将会遗失某些格式
                        while (BrEmail.Document.Body.InnerText.Contains("将此邮件转换为纯文本将会遗失某些格式"))
                        {
                            StringHelper.WriteLog("Test", "将此邮件转换为纯文本将会遗失某些格式");
                            AddRichTextBoxLog("将此邮件转换为纯文本将会遗失某些格式");
                            SendKeys.SendWait("{Enter}");
                            await Task.Delay(4000);
                        }

                        //邮件内容
                        var textarea = divModule.GetElementsByTagName("textarea")[0];
                        if (textarea != null)
                        {
                            //textarea.SetAttribute("value", StringHelper.NoHTML("测试内容啊"));
                            textarea.SetAttribute("value", StringHelper.NoHTML(order.MailBody));
                            await Task.Delay(6000);
                            AddRichTextBoxLog("设置邮件内容的值");
                            await Task.Delay(3000);
                        }

                        //再次点击"纯文本"
                        //divModule.GetElementsByTagName("section")[1].NextSibling.GetElementsByTagName("b")[3].InvokeMember("Click");
                        //await Task.Delay(3000);

                        //while (BrEmail.Document.Body.InnerText.Contains("将此邮件转换为纯文本将会遗失某些格式"))
                        //{
                        //    StringHelper.WriteLog("Test", BrEmail.Document.Body.InnerHtml);
                        //    SendKeys.SendWait("{Enter}");
                        //    await Task.Delay(2000);
                        //}

                    }
                    catch (Exception ex)
                    {
                        AddRichTextBoxLog(ex.Message);
                        StringHelper.WriteLog("Err", ex.ToString());
                        break;
                    }
                    #endregion

                    #region 点击发送
                    try
                    {
                        var divs = GetElement("获取发送按钮异常");//获取发送按钮
                        if (divs != null)
                        {
                            var divSpan = GetHtmlElement(divs.GetElementsByTagName("span")[1]);
                            if (divs != null)
                            {
                                divSpan.InvokeMember("Click");//点击发送
                                AddRichTextBoxLog("点击发送");
                                await Task.Delay(6000);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddRichTextBoxLog("点击发送按钮异常:" + ex.Message);
                        StringHelper.WriteLog("ClickSendErr", ex.ToString());
                        break;
                    }
                    #endregion

                    #region 邮箱地址无效处理
                    if (BrEmail.Document.Body.InnerText.Contains("以下邮箱地址无效"))
                    {
                        try
                        {
                            //await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "邮箱无效测试"); //句柄错误
                            //StringHelper.WriteLog("GetErrorEmailHtml", "邮箱地址无效页面" + order.OrderNo + "------" + BrEmail.Document.Body.InnerHtml);
                            AddRichTextBoxLog("邮箱地址无效页面");
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
                                //await EdmClient.ReportWrongReceiver(item.InnerText.Trim());//会报错误收件人
                            }
                            StringHelper.WriteLog("GetErrorEmailList", errorEmail);

                            //if (order.BccReceiverEmailList != null)
                            //{
                            //    //如果密送人全错(比较少见,所以重启吧)
                            //    if (count == order.BccReceiverEmailList.Count)
                            //    {
                            //        ReLogin("密送人错误"); break;
                            //    }
                            //}
                            //if (!string.IsNullOrEmpty(order.ReceiverEmailAddress) && count == 1)
                            //{
                            //    ReLogin("收件人错误"); break;
                            //}

                            SendKeys.SendWait("{Enter}");
                            await Task.Delay(3000);
                            SpecialHandle(BrEmail.Document);//特殊情况处理
                            await Task.Delay(2000);

                            //因为错误邮件一次只显示三条,所以必须循环处理
                            //if (count >= 3)
                            //{
                            //int i = 0;
                            while (BrEmail.Document.Body.InnerText.Contains("以下邮箱地址无效"))
                            {
                                StringHelper.WriteLog("EnterErrorEmail", "无效邮箱");
                                SendKeys.SendWait("{Enter}"); AddRichTextBoxLog("以下邮箱地址无效");
                                await Task.Delay(4000);
                                SpecialHandle(BrEmail.Document);//特殊情况处理
                                await Task.Delay(2000);
                                //i++;
                            }
                            //}
                        }
                        catch (Exception ex)
                        {
                            StringHelper.WriteLog("GetErrorEmailErr", ex.ToString());
                            AddRichTextBoxLog("GetErrorEmailErr" + ex.ToString());
                            break;
                        }
                    }

                    if (BrEmail.Document.Body.InnerText.Contains("发送对象过多"))
                    {
                        AddRichTextBoxLog("发送对象过多");
                        //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "短时间发送对象过多");
                        //SendKeys.SendWait("{Enter}");
                        await Task.Delay(4000);
                        ReLogin("发送对象过多");//重启
                        return;
                    }
                    #endregion

                    #region 获取发送成功结果
                    await Task.Delay(5000);
                    try
                    {
                        if (BrEmail.Url.ToString().Contains("df=mail163_letter"))
                        {
                            AddRichTextBoxLog("邮件已成功发送,订单号");
                            StringHelper.WriteLog("SendSuccess", ",邮件已成功发送");
                            //await EdmClient.ReportOrderHasBeenSent(order.OrderNo);//修改单状态

                            //刷新,继续写信 BrEmail.Refresh();
                            //BrEmail.Document.ExecCommand("Refresh", false, null);

                            var divModule2 = BrEmail.Document.GetElementById("dvContainer");
                            var divSection = divModule2.GetElementsByTagName("section")[2];//sQ1

                            foreach (HtmlElement ele in divModule2.GetElementsByTagName("a"))
                            {
                                var text = ele.InnerText;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    if (text.Trim().Contains("继续写信"))
                                    {
                                        ele.InvokeMember("Click"); AddRichTextBoxLog("继续写信");
                                        await Task.Delay(1000);
                                        break;
                                    }
                                }
                            }
                            continue;//加一下
                        }
                        else
                        {
                            StringHelper.WriteLog("SendFail", "邮件发送不成功:" + BrEmail.Url.ToString());
                            AddRichTextBoxLog("邮件发送不成功");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        StringHelper.WriteLog("SendErr", ex.ToString());
                        AddRichTextBoxLog("SendErr" + ex.Message);
                        break;
                    }

                    #endregion
                }

            }
            catch (Exception ex)
            {
                AddRichTextBoxLog(ex.Message);
                StringHelper.WriteLog("MainWorkErr", ex.ToString());
                //await EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "主流程异常:" + ex.Message);
                ReLogin("主流程异常");//重启
            }

            int nextTime = 10;
            while (nextTime-- > 1)
            {
                Text = $@"{appBaseTitle} - 等待{nextTime}秒-{"重新开始发邮件流程"}";
                //StringHelper.WriteLog("MailCount2", Text);
                AddRichTextBoxLog("SendErr" + Text);
                //CloseWrite();//关闭写信页面
                await Task.Delay(1000);
            }

            //去到写信页面(首页加载东西太多容易卡死)
            _isRunning = false;
            //_isLogin = true;
            _isWriter = false;
            ReturnHome();

        }


        //特殊情况处理
        private void SpecialHandle(HtmlDocument hDoc)
        {
            try
            {
                var brEmailBody = hDoc.Body;

                #region 网络情况检测
                if (brEmailBody.InnerText.Contains("网络连接可能不正常") || brEmailBody.InnerText.Contains("网络连接已断开"))
                {
                    AddRichTextBoxLog("网络连接可能不正常");
                    //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "网络连接可能不正常");
                    //SendKeys.SendWait("{Enter}");
                    Task.Delay(3000);
                    ReLogin("网络连接可能不正常");//重启
                    return;
                }
                #endregion

                #region 设置姓名
                if (brEmailBody.InnerText.Contains("您还没设置姓名"))
                {
                    AddRichTextBoxLog("账号异常:您还没设置姓名");
                    //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "设置姓名页面");
                    var table = BrEmail.Document.GetElementsByTagName("table")[0];
                    if (table != null)
                    {
                        var iptName = table.GetElementsByTagName("input")[0];
                        if (iptName != null)
                        {
                            iptName.Focus();
                            iptName.SetAttribute("value", "Name");
                            Task.Delay(2000);
                            SendKeys.SendWait("{Enter}");
                        }
                    }
                    Task.Delay(2000);
                    return;
                }
                #endregion

                #region 验证码识别
                IdentifyCode();
                #endregion

                #region 禁言
                if (brEmailBody.InnerText.Contains("您的帐号目前被禁止发信"))
                {
                    AddRichTextBoxLog("您的帐号目前被禁止发信");
                    //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "您的帐号目前被禁止发信");
                    //SendKeys.SendWait("{Enter}");
                    Task.Delay(3000);
                    ReLogin("您的帐号目前被禁止发信");//重启
                    return;
                }
                #endregion

                #region 短时间发送对象过多
                if (brEmailBody.InnerText.Contains("发送对象过多"))
                {
                    AddRichTextBoxLog("发送对象过多");
                    //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, brEmailBody.InnerHtml, null, "短时间发送对象过多");
                    //SendKeys.SendWait("{Enter}");
                    Task.Delay(3000);
                    ReLogin("发送对象过多");//重启
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
        private void IdentifyCode()
        {
            #region 验证码
            if (BrEmail.Document.Body.InnerText.Contains("系统认为您需要输入验证码才能继续完成发信"))
            {
                Text = $@"{appBaseTitle}系统认为您需要输入验证码才能继续完成发信，验证码辨识中....";
                AddRichTextBoxLog("系统认为您需要输入验证码才能继续完成发信，验证码辨识中....");
                //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "系统认为您需要输入验证码才能继续完成发信");
                Task.Delay(2000);

                try
                {
                    Image vCode = ImageHelper.GetRegCodePic(BrEmail, "imgMsgBoxVerify");
                    var arr1 = ImageHelper.ImageToBytes(vCode);
                    var nCaptchaId = VcodeHelper.GetYunVcode2(arr1);

                    AddRichTextBoxLog(@"验证码辨识完成:" + nCaptchaId.ToUpper());
                    Text = $@"{appBaseTitle}验证码辨识完成";
                    Task.Delay(2000);

                    var iptCode = BrEmail.Document.GetElementsByTagName("table")[0].GetElementsByTagName("input")[0];
                    if (iptCode != null)
                    {
                        iptCode.SetAttribute("value", nCaptchaId.ToUpper()); Task.Delay(1000);//设置验证码的值
                    }

                    SendKeys.SendWait("{Enter}");
                    Task.Delay(4000);
                }
                catch (Exception ex)
                {
                    //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, null, ex, "验证码识别错误");
                    SendKeys.SendWait("{Enter}");
                    Task.Delay(2000);
                }
            }
            #endregion
        }


        private async Task<Order> GetOrderCommon()
        {
            try
            {
                if (order == null)
                {
                    AddRichTextBoxLog("取单中,请稍后....");
                    Text = $@"{appBaseTitle}取单中,请稍后....";
                    if (target == 1) //测试环境
                    {
                        return new DataHelper().Orders.First();
                    }
                    else
                    {
                        await Task.Delay(1000);
                        GetClient();//实例化客户端

                        var emailList = await EdmClient.GetOrder(SenderType.Mail163);
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
            //AddRichTextBoxLog("取单完成....");
            Text = $@"{appBaseTitle}取单完成....";
            return null;
        }


        //获取元素
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

        //关闭写信窗口
        private void CloseWrite()
        {
            #region 关闭多余的写信窗口
            try
            {
                var tabDiv = BrEmail.Document.GetElementById("dvMultiTab"); if (tabDiv == null) return;
                var tabUl = tabDiv.GetElementsByTagName("ul")[0]; if (tabUl == null) return;
                var tabCount = tabUl.GetElementsByTagName("li");
                if (tabCount.Count > 8)
                {
                    tabUl.GetElementsByTagName("li")[6].GetElementsByTagName("a")[0].InvokeMember("Click");//关闭写信窗口
                    Task.Delay(3000);
                    StringHelper.WriteLog("Mail", "关闭窗口");
                    //这里也写一次
                    if (BrEmail.Document.Body.InnerText.Contains("您正在写信中"))
                    {
                        //EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "您正在写信中");
                        HtmlDocument paydoc = BrEmail.Document;
                        foreach (HtmlElement ele in paydoc.GetElementsByTagName("span"))
                        {
                            if (ele.GetAttribute("className") == "nui-btn-text" && ele.InnerText == "离开并存草稿")
                            {
                                ele.InvokeMember("click"); StringHelper.WriteLog("Mail", "离开并存草稿");
                                break;
                            }
                        }
                        Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("CloseWriteErr", "关闭写信窗口异常:" + ex.ToString());
            }
            #endregion
        }

        //回到写信页面
        private void ReturnHome()
        {
            //CloseWrite();//关闭多余的写信窗口

            if (BrEmail.Document.Body.InnerText.Contains("您正在写信中"))
            {
                // EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "您正在写信中");
                HtmlDocument paydoc = BrEmail.Document;
                foreach (HtmlElement ele in paydoc.GetElementsByTagName("span"))
                {
                    if (ele.GetAttribute("className") == "nui-btn-text" && ele.InnerText == "离开并存草稿")
                    {
                        ele.InvokeMember("click");
                        break;
                    }
                }
                Task.Delay(2000);
                StringHelper.WriteLog("Mail", "您正在写信中");
            }

            if (!string.IsNullOrEmpty(Sid))
            {
                AddRichTextBoxLog("回首页");
                string faUrl = "https://hwwebmail.mail.163.com/js6/main.jsp?sid=" + Sid + "&df=mail163_letter#module=welcome.WelcomeModule|{}";//首页

                //重新回到写信页面
                //string faUrl = "https://hwwebmail.mail.163.com/js6/main.jsp?sid=" + Sid + "&df=webmail#module=compose.ComposeModule|{\"type\":\"compose\",\"fullScreen\":true}";
                //BrEmail.Url = new Uri(faUrl);
                BrEmail.Navigate(faUrl);
                Task.Delay(3000);
            }
        }

        private HtmlElement GetElement(string msg)
        {
            try
            {
                Task.Delay(2000);

                //var divModule = BrEmail.Document.GetElementById("_dvModuleContainer_compose.ComposeModule_0")
                //    .GetElementsByTagName("header")[0].GetElementsByTagName("div")[0];

                var divModule = BrEmail.Document.GetElementById("dvContainer").GetElementsByTagName("div")[0]
                    .GetElementsByTagName("header")[0].GetElementsByTagName("div")[0];

                return divModule;
            }
            catch (Exception ex)
            {
                StringHelper.WriteLog("GetElementErr", msg + "," + ex.ToString());
                StringHelper.WriteLog("GetElementErrHtml", BrEmail.Document.GetElementById("dvContainer").InnerHtml);
                return null;
            }
        }

        private async Task WhileTime(int nextTime, string msg)
        {
            while (nextTime-- > 1)
            {
                Text = $@"{appBaseTitle} - 等待{nextTime}秒-{msg}";
                await Task.Delay(1000);
            }
        }


        //写log到页面上
        private void AddRichTextBoxLog(string log)
        {
            RtxtLogger.SelectionStart = BrEmail.Text.Length;
            RtxtLogger.AppendText($@"[{DateTime.Now.ToString("MM-dd HH:mm:ss")}] {log}" + Environment.NewLine);
            RtxtLogger.SelectionStart = BrEmail.Text.Length;
            RtxtLogger.ScrollToCaret();
        }

        private void BrEmail_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                //if (BrEmail.ReadyState < WebBrowserReadyState.Complete) return;
                Text = $@"{title}页面加载中,状态为:{ BrEmail.ReadyState},请稍后.....";
                if (BrEmail.ReadyState != WebBrowserReadyState.Complete || BrEmail.IsBusy) return;

                #region 处理无效邮箱
                //int i = 0;
                if (BrEmail.Document.Body.InnerText.Contains("以下邮箱地址无效"))
                {
                    StringHelper.WriteLog("EmailErr", "无效邮箱"); AddRichTextBoxLog("无效邮箱");
                    SendKeys.SendWait("{Enter}");
                    Task.Delay(3000);
                    EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "邮箱地址无效!");
                    SpecialHandle(BrEmail.Document);//特殊情况处理
                    Task.Delay(2000);
                    //i++;
                }
                #endregion

                if (BrEmail.Url.Equals(loginUrl))
                {
                    Text = $@"{title}登陆页面加载完成";

                    if (!_isLogin)
                    {
                        Login();
                        _isLogin = true;
                    }
                }
                else if (BrEmail.Url.ToString().Contains("module=welcome.WelcomeModule"))//首页
                {
                    Text = $@"{title}首页加载完成";
                    AddRichTextBoxLog(Text + "_isWriter---" + _isWriter);

                    #region 去除引导页
                    if (BrEmail.Document.GetElementById("dvGuideMask") != null)
                    {
                        if (BrEmail.Document.GetElementById("dvGuideLayer") != null)
                        {
                            EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.GetElementById("dvGuideMask").InnerHtml, null, "获取引导页面");
                            BrEmail.Document.GetElementById("dvGuideLayer").GetElementsByTagName("a")[0].InvokeMember("Click");
                        }
                        Task.Delay(2000);
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
                                iptName.SetAttribute("value", "Name");
                                Task.Delay(2000);
                                SendKeys.SendWait("{Enter}");
                            }
                        }
                        Task.Delay(2000);
                    }
                    #endregion

                    if (!_isWriter)
                    {
                        try
                        {
                            var writeEmail = BrEmail.Document.GetElementById("dvNavTop").GetElementsByTagName("ul")[0]
                                .GetElementsByTagName("li")[1].GetElementsByTagName("span")[1];

                            writeEmail.InvokeMember("Click");//点击写信
                            Task.Delay(3000);

                            AddRichTextBoxLog("点击写信按钮");
                            _isWriter = true;
                        }
                        catch (Exception ex)
                        {
                            AddRichTextBoxLog("点击写信按钮异常" + ex.ToString());
                        }
                    }
                }
                else if (BrEmail.Url.ToString().Contains("module=compose.ComposeModule"))//写信页面
                {
                    Text = $@"{title}写信页面加载完成";
                    AddRichTextBoxLog(Text + "_isRunning---" + _isRunning);

                    if (!_isRunning)
                    {
                        MainWorkflow();
                        _isRunning = true;
                    }
                }
                #region 其他情况
                else if (BrEmail.Url.ToString().Contains(logoutUrl))
                {
                    AddRichTextBoxLog("用户已注销");
                    Text = $@"{appBaseTitle}用户已注销,即将重启";
                    EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "用户已注销,即将重启");
                    ReLogin("用户已注销");
                }
                else if (BrEmail.Url.ToString().Contains(TimeoutUrl))
                {
                    AddRichTextBoxLog("登陆超时,即将重启");
                    Text = $@"{appBaseTitle}登陆超时,即将重启";
                    EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, "登陆超时,即将重启");
                    ReLogin("登陆超时");//重启
                }
                else if (BrEmail.Url.ToString().Contains("email2.163.com"))
                {
                    BrEmail.Navigate(loginUrl);
                    _isLogin = false;
                }
                else if (BrEmail.Url.ToString().Contains("reg.163.com"))
                {
                    if (BrEmail.Document.Body.InnerText.Contains("无法显示此页"))
                    {
                        EdmClient.AddClientErrorLog(SenderType.Mail163, order == null ? "" : order.OrderNo, BrEmail.Document.Body.InnerHtml, null, order.SenderEmailAddress + ",无法登陆");
                        StringHelper.WriteLog("GetReg163", BrEmail.Url.ToString());
                        ReLogin("登陆异常");
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                AddRichTextBoxLog("服务器正忙:" + ex.Message);
                StringHelper.WriteLog("DocumentCompletedErr", ex.ToString());
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


        //根据配置文件中的EdmClientType实例化EdmClient
        private void GetClient()
        {
            try
            {
                var type = "1";// GetAppConfigValue("EdmClientType");
                //if (string.IsNullOrEmpty(type)) type = "1";

                //lbClientType.Text = "客户端类型：" + (type == "1" ? "一般" : "产品");
                //SlbRemark.Text = "客户端类型(可点击单选按钮切换)";

                switch (type)
                {
                    case "1":
                        //rbTypeOne.Checked = true;
                        EdmClient = new EdmClient(dTarget);
                        break;
                    case "2":
                        //rbTypeTwo.Checked = true;
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

        private void button1_Click(object sender, EventArgs e)
        {
            string myPath = System.AppDomain.CurrentDomain.BaseDirectory;
            Process.Start("explorer.exe", myPath);
        }
    }
}
