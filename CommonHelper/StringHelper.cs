using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmailWY.CommonHelper
{
    public class StringHelper
    {
        public static string GetSid(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return url;
                int strA = url.IndexOf("=");
                int strB = url.IndexOf("&");
                string strc = url.Substring(strA + 1, strB - strA - 1);
                return strc;
            }
            catch (Exception)
            {
                return url;
            }
        }

        public static string NoHTML(string htmlStr)
        {
            //删除脚本   
            string stroutput = htmlStr;
            Regex regex = new Regex(@"<[^>]+>|</[^>]+>");
            stroutput = regex.Replace(stroutput, "");
            stroutput = new Regex(@"(&nbsp;)+").Replace(stroutput, " ");
            return stroutput;
        }

        //写日志
        public static void WriteLog(string fileName,string content)
        {
            //using (StreamWriter writer = new StreamWriter(fileName, true))
            //{
            //    writer.AutoFlush = true;
            //    writer.WriteLine("时间:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//换行
            //    writer.WriteLine(content);
            //    writer.WriteLine(Environment.NewLine);//换行
            //}

            try
            {
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "Log";
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                string logPath = filePath + "/" +"_"+ fileName+"_"+DateTime.Now.ToString("yyyyMMdd") +".txt";
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    sw.WriteLine("时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine(content);
                    sw.WriteLine(Environment.NewLine);
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (Exception)
            {
                
            }

        }

        //删除3天以前的日志
        public static void DeleteLog()
        {
            try
            {
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "Log";
                if (Directory.Exists(filePath))
                {
                    DirectoryInfo folder = new DirectoryInfo(filePath);
                    FileInfo[] fileList = folder.GetFiles();
                    if (fileList.Length <= 0) return;

                    foreach (FileInfo file in fileList)
                    {
                        string fName = file.Name.Substring(file.Name.LastIndexOf("_") + 1, 8);
                        DateTime dtime = DateTime.ParseExact(fName, "yyyyMMdd", null);
                        TimeSpan ts = DateTime.Now - dtime;
                        if (ts.Days > 3)
                        {
                            file.Delete();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
