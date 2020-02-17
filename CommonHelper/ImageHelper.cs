using mshtml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmailWY.CommonHelper
{
    public static class ImageHelper
    {
        #region 图片验证码
        public static Image GetRegCodePic(WebBrowser webBrowser, String imgID)
        {
            HTMLDocument doc = (HTMLDocument)webBrowser.Document.DomDocument;
            HTMLBody body = (HTMLBody)doc.body;
            IHTMLControlRange rang = (IHTMLControlRange)body.createControlRange();
            IHTMLControlElement img;
            img = (IHTMLControlElement)webBrowser.Document.All[imgID].DomElement;//这里有错
            rang.add(img);
            rang.execCommand("Copy", false, null);
            Image regImg = Clipboard.GetImage();
            Clipboard.Clear();
            return regImg;
        }
        public static byte[] ImageToBytes(Image image)
        {
            MemoryStream ms = new MemoryStream();
            byte[] imagedata = null;
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            imagedata = ms.GetBuffer();
            return imagedata;
        }
        #endregion
    }
}
