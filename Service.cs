using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace WeiXin
{
    public class Service : IHttpHandler
    {
        string xmlText(string s)
        {
            return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
        string makeResponse(string FromUserName, string ToUserName, string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            StringBuilder sb = new StringBuilder("<xml>");
            sb.Append("<ToUserName>");
            sb.Append(ToUserName);
            sb.Append("</ToUserName>");

            sb.Append("<FromUserName>");
            sb.Append(FromUserName);
            sb.Append("</FromUserName>");

            sb.Append("<CreateTime>");
            sb.Append(DateTime.Now.Ticks.ToString());
            sb.Append("</CreateTime>");

            sb.Append("<MsgType>text</MsgType>");
            sb.Append("<Content>" + xmlText(content) + "</Content>");
            sb.Append("</xml>");
            return sb.ToString();
        }


        /// <summary>
        /// 收到威信服务器XML
        /// </summary>
        /// <param name="xmlData"></param>
        /// <returns></returns>
        public virtual string makeXML(string FromUserName, string ToUserName)
        {
            try
            {
                
            }
            catch { }

            string sb = @"<xml>
     <ToUserName><![CDATA[" + ToUserName + @"]]></ToUserName>
     <FromUserName><![CDATA[" + FromUserName + @"]]></FromUserName>
     <CreateTime>" + UAP.Function.Datetime.GetStamp() + @"</CreateTime>
     <MsgType><![CDATA[transfer_customer_service]]></MsgType>
 </xml>";
            return sb.ToString();
        }

        public string Handle(HttpServerUtility Server, HttpRequest Request, HttpResponse Response)
        {
            string xml = Request.QueryString.Get("echostr");
            if (!string.IsNullOrEmpty(xml))
            {
                return xml;
            }
            using (System.IO.Stream s = Request.InputStream)
            {
                byte[] buffer = new byte[s.Length];
                StringBuilder builder = new StringBuilder();
                s.Read(buffer, 0, (int)s.Length);
                xml = Encoding.UTF8.GetString(buffer);
            }
            if (string.IsNullOrEmpty(xml))
            {
                return "";
            }
            this.onData(xml);
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            string user = document.SelectSingleNode("/xml/FromUserName").InnerText;
            string server = document.SelectSingleNode("/xml/ToUserName").InnerText;
            string EventType = document.SelectSingleNode("/xml/MsgType").InnerText;
            string MediaId, ThumbMediaId;
            string ret;
            try
            {
                switch (EventType)
                {
                    case "text":
                        return makeXML(server, user);
                    case "image":
                        return makeXML(server, user);
                    case "voice":
                        return makeXML(server, user);
                    case "video":
                        return makeXML(server, user);
                    case "shortvideo":
                        return makeXML(server, user);
                    case "location":
                        return makeXML(server, user);
                    case "link":
                        return makeXML(server, user);
                    default:
                        string Event = document.SelectSingleNode("/xml/Event").InnerText;
                        string EventKey;
                        switch (Event)
                        {
                            case "subscribe":
                                EventKey = document.SelectSingleNode("/xml/EventKey").InnerText;
                                ret = this.onSubscribe(server, user, string.IsNullOrEmpty(EventKey) ? null : EventKey.Substring(8));
                                break;
                            case "unsubscribe":
                                this.unSubscribe(server, user);
                                return "";
                            case "SCAN":
                                EventKey = document.SelectSingleNode("/xml/EventKey").InnerText;
                                ret = this.onScan(server, user, EventKey);
                                break;
                            case "CLICK":
                                EventKey = document.SelectSingleNode("/xml/EventKey").InnerText;
                                ret = this.onClick(server, user, EventKey);
                                break;
                            case "VIEW":
                                EventKey = document.SelectSingleNode("/xml/EventKey").InnerText;
                                ret = this.onView(server, user, EventKey);
                                break;
                            case "LOCATION":
                                string Latitude = document.SelectSingleNode("/xml/Latitude").InnerText;
                                string Longitude = document.SelectSingleNode("/xml/Longitude").InnerText;
                                string Precision = document.SelectSingleNode("/xml/Precision").InnerText;
                                ret = this.onAutoLocation(server, user, Latitude, Longitude, Precision);
                                break;
                            default:
                                ret = this.onUnhandle(server, user, EventType, document);
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ret = onError(ex);
            }
            return makeResponse(server, user, ret);
        }
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Write(Handle(context.Server, context.Request, context.Response));
        }

        /// <summary>
        /// 收到威信服务器XML
        /// </summary>
        /// <param name="xmlData"></param>
        /// <returns></returns>
        public virtual string onData(string xmlData)
        {
            return xmlData;
        }

        /// <summary>
        /// 未处理事件
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="eventType"></param>
        /// <param name="xml"></param>
        /// <returns></returns>
        public virtual string onUnhandle(string server, string user, string eventType, XmlDocument xml)
        {
            return string.Format("你好啊！");
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public virtual string onError(Exception ex)
        {
            return ex.Message + ex.StackTrace;
        }

        /// <summary>
        /// 系统自动物理地址
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="Latitude"></param>
        /// <param name="Longitude"></param>
        /// <param name="Precision"></param>
        /// <returns></returns>
        protected virtual string onAutoLocation(string server, string user, string Latitude, string Longitude, string Precision)
        {
            return string.Format("您在:{0},{1}-{2}", Latitude, Longitude, Precision);
        }

        /// <summary>
        /// 用户点击网页按钮
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual string onView(string server, string user, string url)
        {
            return "";
        }

        /// <summary>
        /// 用户点击事件按钮
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="EventKey"></param>
        /// <returns></returns>
        protected virtual string onClick(string server, string user, string EventKey)
        {
            return "";
        }

        /// <summary>
        /// 老用户扫描二维码
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected virtual string onScan(string server, string user, string param)
        {
            return param;
        }

        /// <summary>
        /// 用户取消关注
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        protected virtual void unSubscribe(string server, string user) { }


        /// <summary>
        /// 用户关注
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected virtual string onSubscribe(string server, string user, string param)
        {
            return param;
        }

        /// <summary>
        /// 收到用户信息
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected virtual string onText(string server, string user, string content)
        {
            return string.Format("收到文字信息：{0}", content);
        }

        /// <summary>
        /// 收到用户图片
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="PicUrl"></param>
        /// <param name="MediaId"></param>
        /// <returns></returns>
        public virtual string onImage(string server, string user, string PicUrl, string MediaId)
        {
            return string.Format("<a href=\"{0}\">藏好在这了</a>，是黄图么？等管理员妈妈来了再看。", PicUrl);
        }

        /// <summary>
        /// 收到用户语音
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="MediaId"></param>
        /// <param name="Format"></param>
        /// <returns></returns>
        public virtual string onVoice(string server, string user, string MediaId, string Format)
        {
            return string.Format("听不懂哟！");
        }

        /// <summary>
        /// 收到用户视频
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="MediaId"></param>
        /// <param name="ThumbMediaId"></param>
        /// <returns></returns>
        public virtual string onVideo(string server, string user, string MediaId, string ThumbMediaId)
        {
            return string.Format("看不懂哟！");
        }

        /// <summary>
        /// 收到用户小视频
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="MediaId"></param>
        /// <param name="ThumbMediaId"></param>
        /// <returns></returns>
        public virtual string onShortvideo(string server, string user, string MediaId, string ThumbMediaId)
        {
            return string.Format("小视频？");
        }

        /// <summary>
        /// 用户主动发送地理位置
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="Location_X"></param>
        /// <param name="Location_Y"></param>
        /// <param name="Scale"></param>
        /// <param name="Label"></param>
        /// <returns></returns>
        public virtual string onLocation(string server, string user, string Location_X, string Location_Y, string Scale, string Label)
        {
            return string.Format("你在这里哟{0},{1}", Location_X, Location_Y);
        }

        /// <summary>
        /// 用户推介链接
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="Title"></param>
        /// <param name="Description"></param>
        /// <param name="Url"></param>
        /// <returns></returns>
        public virtual string onLink(string server, string user, string Title, string Description, string Url)
        {
            return string.Format("一会再看哟。");
        }
    }
}