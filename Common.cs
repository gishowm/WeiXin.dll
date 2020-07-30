using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ThoughtWorks.QRCode.Codec;
using UAP;

namespace WeiXin
{

    public static class Common
    {
        public static string QRcode(string url)
        {

            Bitmap bt;
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            string strbaser64;
            bt = qrCodeEncoder.Encode(url, Encoding.UTF8);

            using (MemoryStream ms = new MemoryStream())
            {
                bt.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                strbaser64 = Convert.ToBase64String(ms.GetBuffer());
            }
            return "data:image/jpg;base64," + strbaser64;
        }
    }
}
