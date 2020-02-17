using EdmApiClientLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWY.CommonHelper
{
    public class DataHelper
    {
        //随机取N条密送人
        public static List<string> GetReceiveList(int count = 5)
        {
            List<string> list = new List<string>();

            //list.Add("14753752336@163.com");
            //list.Add("zhangfeng1122@163.com");
            //list.Add("zhou753hh@163.com");
            //list.Add("wudimaoge@163.com");
            //list.Add("15589862447@163.com");
            //list.Add("zangchao9222@163.com");
            //list.Add("15852000981@163.com");
            //list.Add("927799257qq@163.com");
            list.Add("xzq0822lhy@163.com");
            list.Add("luffytest@outlook.com");
            list.Add("andyliu@gmail.com");
            list.Add("tw29CJMPSWZc@163.com");
            list.Add("gongsunsh8@163.com");

            list.Add("ku841484774704@163.com");
            list.Add("shuhui551884744@163.com");
            list.Add("shao470470770@163.com");
            list.Add("qian55114114044@163.com");
            list.Add("rbry8ELRYb@163.com");

            return list.OrderBy(o => Guid.NewGuid()).Take(count).ToList();
        }

        //随机取一个用户登录
        public static Dictionary<string, string> GetLoginUser()
        {
            Dictionary<string, string> bList = new Dictionary<string, string>();
            bList.Add("gongsunsh8@163.com", "lingm636");
            bList.Add("tw29CJMPSWZc@163.com", "zhang70370");
            bList.Add("ku841484774704@163.com", "bingy0363");
            bList.Add("shuhui551884744@163.com", "miaow22881");
            bList.Add("shao470470770@163.com", "zhou625925");
            bList.Add("qian55114114044@163.com", "tanta8417");
            bList.Add("rbry8ELRYb@163.com", "cang8811");

            return bList.OrderBy(o => Guid.NewGuid()).Take(1).ToDictionary(p => p.Key, o => o.Value);
        }

        private List<Order> _orders;
        public List<Order> Orders
        {
            get
            {
                if (_orders is null)
                {
                    var loginInfo = GetLoginUser().First();
                    _orders = new List<Order>()
                    {
                        new Order(){
                            SenderEmailAddress = loginInfo.Key,
                            SenderPassword = loginInfo.Value,
                            //ReceiverEmailAddress = GetReceiveList(1).First(),
                            BccReceiverEmailList = GetReceiveList(),
                            //MailSubject = "测试-标题-"+DateTime.Now.ToString("yyyyMMddHHmmss"),
                            MailBody = "测试-正文"+DateTime.Now.ToString("yyyyMMddHHmmss"),
                            AttachedFileUrl = ""
                            }
                   };
                }

                //_orders = _orders.OrderBy(o => Guid.NewGuid()).ToList();
                _orders = _orders.ToList();
                return _orders;
            }
            set { _orders = value; }
        }

    }
}
