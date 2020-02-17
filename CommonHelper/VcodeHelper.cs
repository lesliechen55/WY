using FastVerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWY.CommonHelper
{
   public class VcodeHelper
    {
        public static string GetLianZongVcode(string pathImg)
        {
            string vcode = string.Empty;
            string result = VerCode.RecYZM_A(pathImg, "lovee68", "qaz67890", "");
            if (result.Contains("|!|"))
                vcode = result.Split('|')[0];
            return vcode;
        }

        public static string GetLianZongVcode2(byte[] vcode)
        {
            string vcode2 = string.Empty;
            string result2 = VerCode.RecByte_A(vcode, vcode.Length, "lovee68", "qaz67890", "");
            if (result2.Contains("|!|"))
                vcode2 = result2.Split('|')[0];
            return vcode2;
        }

        public static string GetYunVcode(string pathImg)
        {
            int nCaptchaId = 0;
            StringBuilder pCodeResult = new StringBuilder(new string(' ', 30)); // 分配30个字节存放识别结果
            nCaptchaId = YmzApi.YDM_EasyDecodeByPath("lovee68", "qaz67890", 1, "22cc5376925e9387a23cf797cb9ba745", pathImg, 1005, 60, pCodeResult);
            if (nCaptchaId > 0)
                return pCodeResult.ToString();
            else
                return "";
        }

        public static string GetYunVcode2(byte[] vcode)
        {
            int nCaptchaId = 0;

            StringBuilder pCodeResult = new StringBuilder(new string(' ', 30)); // 分配30个字节存放识别结果
            nCaptchaId = YmzApi.YDM_EasyDecodeByBytes("lovee68", "qaz67890", 1, "22cc5376925e9387a23cf797cb9ba745", vcode, vcode.Length, 1005, 60, pCodeResult);
            if (nCaptchaId > 0)
                return pCodeResult.ToString();
            else
                return "";
        }
    }
}
