using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace WeiXin
{
    public class Notify_JSAPI
    {

        public static PaynotifyModel GetInputStream()
        {
            #region 从InputStream中获取参数
            System.IO.Stream s = Current.Request.InputStream;
            int count = 0;
            byte[] buffer = new byte[1024];
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            while ((count = s.Read(buffer, 0, 1024)) > 0)
            {
                builder.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, count));
            }
            s.Flush();
            s.Close();
            s.Dispose();
            string xmlString = builder.ToString();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlString);
            PaynotifyModel pm = new PaynotifyModel();
            Type ty = pm.GetType();
            foreach (var properties in ty.GetProperties())
            {
                properties.SetValue(pm, xml.SelectSingleNode("xml/" + properties.Name).InnerText, null);
            }
            if (pm.return_code != "SUCCESS" || pm.result_code != "SUCCESS")
            {
                throw new Exception("支付不成功");
            }
            return pm;
            #endregion
        }
        public static string makeXML(bool flag = false)
        {
            switch (flag)
            {
                case false:
                    return "<xml><return_code><![CDATA[SUCCESS]]></return_code><return_msg><![CDATA[OK]]></return_msg></xml>";
                case true:
                    return "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[FAIL]]></return_msg></xml>";
                default:
                    return "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[FAIL]]></return_msg></xml>";
            }
        }

        public static string MakeSign(PaynotifyModel model)
        {
            string str = "";
            Type ty = model.GetType();
            List<string> list = new List<string>();
            foreach (var item in ty.GetProperties())
            {
                if (item.Name != "sign")
                {
                    list.Add(item.Name + "=" + ty.GetProperty(item.Name).GetValue(model, null));
                }
            }
            str = string.Join("&", list.ToArray<string>());
            //在string后加入API KEY
            str += "&key=" + WeiXin.Config.Pay_key;
            //MD5加密

            //所有字符转为大写
            //return UAP.Function.String.MD5(str).ToUpper();
            return model.sign;
        }
    }
}
