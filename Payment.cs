using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Linq;
using System.Xml;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using UAP;
using System.Drawing;
using ThoughtWorks.QRCode.Codec;

namespace WeiXin
{
    /// <summary>
    /// 微信支付处理类
    /// </summary>
    /// 
    [api]
    public class Payment
    {
        /// <summary>
        /// 统一下单
        /// </summary>
        /// <param name="body">商品描述</param>
        /// <param name="detail">商品详情</param>
        /// <param name="orderNO">订单号</param>
        /// <param name="amt">金额(分)</param>
        /// <param name="openid">openid</param>
        /// <param name="limit_pay">是否使用信用卡支付</param>
        /// <param name="notify_url">支付成功回调地址</param>
        /// <param name="trade_type">NATIVE/JSAPI/APP</param>
        /// <returns>下单结果</returns>
        public static UAP.JSON CreateOrder(string body, string detail, string orderNO, int amt, string openid = null, bool limit_pay = true, string notify_url = null, string trade_type = null)
        {
            body = body.Replace(" ", "").Replace("\n", "").Replace("\t", "");
            detail = detail.Replace(" ", "").Replace("\n", "").Replace("\t", "");
            string[] parameters ={
                                    "appid="+Config.Pay_AppID,
                                    "mch_id="+Config.Pay_Memberid,
                                    "nonce_str="+ Guid.NewGuid().ToString("N"),
                                    "body="+body,
                                    "detail="+detail,
                                    "out_trade_no="+orderNO, 
                                    //"notify_url="+Config.Pay_NotifyURL, 
                                    "total_fee="+amt,
                                    "spbill_create_ip=220.115.186.146",//+Current.IP, 
                                 };
            List<string> paraList = parameters.ToList<string>();
            if (!string.IsNullOrEmpty(trade_type))
            {
                paraList.Add("trade_type=NATIVE");

            }
            else
            {
                paraList.Add("trade_type=JSAPI");
                paraList.Add("openid=" + openid);
            }

            if (limit_pay == false)
            {
                paraList.Add("limit_pay=no_credit");
            }
            paraList.Add("notify_url=" + (notify_url == null ? Config.Pay_NotifyURL : notify_url));
            string xmlstring = MakeXML(paraList.ToArray());

            string result = UAP.Function.Http.getPost("https://api.mch.weixin.qq.com/pay/unifiedorder", xmlstring.Replace(" ", "").Replace("\n", "").Replace("\t", ""), System.Text.Encoding.UTF8);

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(result);
            if (xml.SelectSingleNode("/xml/return_code").InnerText.Equals("SUCCESS"))
            {
                if (xml.SelectSingleNode("/xml/result_code").InnerText.Equals("SUCCESS"))
                {
                    string prepay_id = xml.SelectSingleNode("/xml/prepay_id").InnerText;

                    List<string> list = new List<string>();
                    list.Add("appId=" + Config.Pay_AppID);
                    list.Add("timeStamp=" + UAP.Function.Datetime.GetStamp());
                    list.Add("nonceStr=" + Guid.NewGuid().ToString("N"));
                    list.Add("package=prepay_id=" + prepay_id);
                    list.Add("signType=MD5");
                    list.Add("paySign=" + Sign(list));

                    UAP.JSON ret = new UAP.JSON() {
                        {"orderNo",orderNO}
                    };

                    UAP.JSON dict = new UAP.JSON();

                    list.All(s =>
                    {
                        int pos = s.IndexOf('=');
                        dict.Add(s.Substring(0, pos), s.Substring(pos + 1));
                        return true;
                    });

                    XmlNode qr = xml.SelectSingleNode("/xml/code_url");
                    if (qr != null)
                    {
                        ret.Add("qrcode", WeiXin.Common.QRcode(qr.InnerText));
                    }

                    ret.Add("data", dict);
                    return ret;
                }
            }
            throw new Exception(xml.SelectSingleNode("/xml/return_msg").InnerText);
        }

        /// <summary>
        /// 查询订单
        /// </summary>
        /// <param name="orderNO">orderNO</param>
        public static UAP.JSON Query(string orderNO)
        {
            string[] parameters ={
                "appid="+Config.Pay_AppID,
                "mch_id="+Config.Pay_Memberid,
                "out_trade_no="+orderNO,
                "nonce_str="+Guid.NewGuid().ToString("N")
            };
            string xmlstring = MakeXML(parameters);

            string result = UAP.Function.Http.getPost("https://api.mch.weixin.qq.com/pay/orderquery", xmlstring.ToString(), System.Text.Encoding.UTF8);

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(result);
            if (xml.SelectSingleNode("/xml/return_code").InnerText.Equals("SUCCESS"))
            {
                if (xml.SelectSingleNode("/xml/result_code").InnerText.Equals("SUCCESS"))
                {
                    switch (xml.SelectSingleNode("/xml/trade_state").InnerText)
                    {
                        case "SUCCESS":
                            return new UAP.JSON {
                                {"openid",xml.SelectSingleNode("/xml/openid").InnerText},
                                {"total_fee",xml.SelectSingleNode("/xml/total_fee").InnerText},
                                {"out_trade_no",xml.SelectSingleNode("/xml/out_trade_no").InnerText},
                                {"transaction_id",xml.SelectSingleNode("/xml/transaction_id").InnerText}
                            };
                        case "REFUND": throw new Exception("转入退款");
                        case "NOTPAY": throw new Exception("未支付");
                        case "CLOSED": throw new Exception("已关闭");
                        case "REVOKED": throw new Exception("已撤销（刷卡支付）");
                        case "USERPAYING": throw new Exception("用户支付中");
                        case "PAYERROR": throw new Exception("支付失败(其他原因，如银行返回失败)");
                    }
                }
            }
            throw new Exception("获取支付状态异常");
        }

        /// <summary>
        /// 生成Post参数
        /// </summary>
        /// <param name="parameters">parameters</param>
        /// <returns>订单参数</returns>
        private static string MakeXML(string[] parameters)
        {
            #region 生成签名
            List<string> ls = new List<string>(parameters);
            string sign = Sign(ls);
            #endregion

            #region 组成Xml参数
            ls.Add("sign=" + sign);
            //ls.Sort();
            string[] signSort = ls.ToArray();
            StringBuilder xmlstring = new StringBuilder();
            xmlstring.Append("<?xml version='1.0' encoding='UTF-8' standalone='yes' ?><xml>");
            foreach (string item in signSort)
            {
                xmlstring.Append("<" + item.Substring(0, item.IndexOf('=')) + ">");
                xmlstring.Append(item.Substring(item.IndexOf('=') + 1, item.Length - item.IndexOf('=') - 1));
                xmlstring.Append("</" + item.Substring(0, item.IndexOf('=')) + ">");
            }
            xmlstring.Append("</xml>");
            return xmlstring.ToString();
            #endregion
        }

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
        /// <summary>
        /// 签名
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static string Sign(List<string> list)
        {
            list.Sort();
            string[] arrsort = list.ToArray();
            return UAP.Function.String.MD5(string.Join("&", arrsort) + "&key=" + Config.Pay_key).ToUpper(); ;
        }




        /*CheckValidationResult的定义*/
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            return false;
        }


        //企业付款
        public static JSON EnterprisePayment(string partner_trade_no, string openid, int amount, string re_user_name, string desc)
        {
            ///请求的URL
            string url = "https://api.mch.weixin.qq.com/mmpaymkttransfers/promotion/transfers";
            //获取证书，证书位置放在根目录不安全。
            string cert = HttpContext.Current.Server.MapPath("/!/apiclient_cert.p12");
            string password = WeiXin.Config.Pay_Memberid;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //获取证书
            //X509Certificate cer = new X509Certificate(cert, password);
            X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);//线上发布需要添加
            HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //添加证书
            webrequest.ClientCertificates.Add(cer);
            //创建web请求。
            webrequest.Method = "post";
            string[] parameters ={
             "mch_appid="+WeiXin.Config.Pay_AppID,
             "mchid="+WeiXin.Config.Pay_Memberid,
             "nonce_str="+Guid.NewGuid().ToString("N"),
             "partner_trade_no="+partner_trade_no,
             "openid="+openid,
             "check_name=NO_CHECK",
             "re_user_name="+re_user_name,
             "amount="+amount,
             "desc="+desc,
             "spbill_create_ip="+(Current.HOST=="localhost"?"220.115.186.146":Current.IP)};
            string xmlstring = MakeXML(parameters);

            byte[] bs = Encoding.UTF8.GetBytes(xmlstring);
            //参数
            using (Stream reqStream = webrequest.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Close();
            }
            HttpWebResponse webreponse = (HttpWebResponse)webrequest.GetResponse();
            Stream stream = webreponse.GetResponseStream();
            string resp = string.Empty;
            Encoding encoding = Encoding.UTF8;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                resp = reader.ReadToEnd();
            }
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(resp);
            if (xml.SelectSingleNode("/xml/return_code").InnerText.Equals("SUCCESS"))
            {
                if (xml.SelectSingleNode("/xml/result_code").InnerText.Equals("SUCCESS"))
                {
                    JSON retMsg = new JSON();
                    try
                    {
                        retMsg.Add("partner_trade_no", xml.SelectSingleNode("/xml/partner_trade_no").InnerText);
                        retMsg.Add("payment_no", xml.SelectSingleNode("/xml/payment_no").InnerText);
                        retMsg.Add("payment_time", xml.SelectSingleNode("/xml/payment_time").InnerText);
                        return retMsg;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                else
                {
                    switch (xml.SelectSingleNode("/xml/err_code").InnerText)
                    {
                        case "NOAUTH": throw new Exception("没有权限");
                        case "AMOUNT_LIMIT": throw new Exception("付款金额不能小于最低限额");
                        case "PARAM_ERROR": throw new Exception("参数错误");
                        case "OPENID_ERROR": throw new Exception("Openid错误");
                        case "NOTENOUGH": throw new Exception("系统余额不足");
                        case "SYSTEMERROR": throw new Exception("系统繁忙，请稍后再试");
                        case "NAME_MISMATCH": throw new Exception("姓名校验出错");
                        case "SIGN_ERROR": throw new Exception("签名错误");
                        case "XML_ERROR": throw new Exception("Post内容出错");
                        case "FATAL_ERROR": throw new Exception("两次请求参数不一致");
                        case "CA_ERROR": throw new Exception("证书出错");
                        case "V2_ACCOUNT_SIMPLE_BAN": throw new Exception("非实名用户账号不可发放");
                    }
                }
            }
            throw new Exception(xml.SelectSingleNode("/xml/return_msg").InnerText);
            //throw new Exception(resp);
        }

        //发红包
        public static JSON SendRedPack(string partner_trade_no, string openid, int amount, string desc, string act_name)
        {
            ///请求的URL
            string url = "https://api.mch.weixin.qq.com/mmpaymkttransfers/sendredpack";
            //获取证书，证书位置放在根目录不安全。
            string cert = HttpContext.Current.Server.MapPath("/!/apiclient_cert.p12");
            string password = WeiXin.Config.Pay_Memberid;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //获取证书
            //X509Certificate cer = new X509Certificate(cert, password);
            X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);//线上发布需要添加
            HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //添加证书
            webrequest.ClientCertificates.Add(cer);
            //创建web请求。
            webrequest.Method = "post";
            string[] parameters ={
             "wxappid="+WeiXin.Config.Pay_AppID,
             "mch_id="+WeiXin.Config.Pay_Memberid,
             "send_name="+WeiXin.Config.SendName,
             "nonce_str="+Guid.NewGuid().ToString("N"),
             "mch_billno="+partner_trade_no,
             "re_openid="+openid,
             "total_amount="+amount,
             "total_num="+1,
             "act_name="+act_name??"恭喜发财",
             "wishing="+"恭喜发财",
             "client_ip="+(Current.HOST=="localhost"?"220.115.186.146":Current.IP)};
            string xmlstring = MakeXML(parameters);

            byte[] bs = Encoding.UTF8.GetBytes(xmlstring);
            //参数
            using (Stream reqStream = webrequest.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Close();
            }
            HttpWebResponse webreponse = (HttpWebResponse)webrequest.GetResponse();
            Stream stream = webreponse.GetResponseStream();
            string resp = string.Empty;
            Encoding encoding = Encoding.UTF8;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                resp = reader.ReadToEnd();
            }
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(resp);
            if (xml.SelectSingleNode("/xml/return_code").InnerText.Equals("SUCCESS"))
            {
                if (xml.SelectSingleNode("/xml/result_code").InnerText.Equals("SUCCESS"))
                {
                    switch (xml.SelectSingleNode("/xml/result_code").InnerText)
                    {
                        case "SUCCESS":
                            JSON retMsg = new JSON();
                            try
                            {
                                retMsg.Add("partner_trade_no", xml.SelectSingleNode("/xml/partner_trade_no").InnerText);
                                retMsg.Add("payment_no", xml.SelectSingleNode("/xml/payment_no").InnerText);
                                retMsg.Add("payment_time", xml.SelectSingleNode("/xml/payment_time").InnerText);
                                return retMsg;
                            }
                            catch (Exception)
                            {
                                return null;
                            }
                        case "NOAUTH": throw new Exception("没有权限");
                        case "SENDNUM_LIMIT": throw new Exception("该用户今日领取红包个数超过限制");
                        case "ILLEGAL_APPID": throw new Exception("错误传入了app的appid");
                        case "MONEY_LIMIT": throw new Exception("Openid错误");
                        case "SEND_FAILED": throw new Exception("系统余额不足");
                        case "FATAL_ERROR": throw new Exception("系统繁忙，请稍后再试");
                        case "NAME_MISMATCH": throw new Exception("姓名校验出错");
                        case "SIGN_ERROR": throw new Exception("签名错误");
                        case "XML_ERROR": throw new Exception("Post内容出错");
                        case "CA_ERROR": throw new Exception("证书出错");
                        case "NOTENOUGH": throw new Exception("账户余额不足");
                        case "OPENID_ERROR": throw new Exception("openid和appid不匹配");
                        case "MSGAPPID_ERROR": throw new Exception("msgappid与主、子商户号的绑定关系校验失败");
                        case "PROCESSING": throw new Exception("发红包流程正在处理");
                    }
                }
            }
            throw new Exception(xml.SelectSingleNode("/xml/return_msg").InnerText);
            //throw new Exception(resp);
        }


        /// <summary>
        /// 查询企业支付API
        /// </summary>
        /// <param name="partner_trade_no">商户订单号</param>
        public static UAP.JSON QueryPay(string partner_trade_no)
        {
            ///请求的URL
            string url = "https://api.mch.weixin.qq.com/mmpaymkttransfers/gettransferinfo";
            //获取证书，证书位置放在根目录不安全。 
            string cert;
            if (HttpContext.Current != null)
            {
                cert = HttpContext.Current.Server.MapPath("/!/apiclient_cert.p12");
            }
            else
            {
                cert = @"E:\MEI_V2\Mei_Ver2\app.chnmei.com\!\apiclient_cert.p12";
            }
            string password = WeiXin.Config.Pay_Memberid;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //获取证书
            //X509Certificate cer = new X509Certificate(cert, password);
            X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);//线上发布需要添加
            HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //添加证书
            webrequest.ClientCertificates.Add(cer);
            //创建web请求。
            webrequest.Method = "post";
            string[] parameters ={
                "appid="+Config.Pay_AppID,
                "mch_id="+Config.Pay_Memberid,
                "partner_trade_no="+partner_trade_no,
                "nonce_str="+Guid.NewGuid().ToString("N")
            };
            string xmlstring = MakeXML(parameters);
            byte[] bs = Encoding.UTF8.GetBytes(xmlstring);
            //参数
            using (Stream reqStream = webrequest.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Close();
            }
            HttpWebResponse webreponse = (HttpWebResponse)webrequest.GetResponse();
            Stream stream = webreponse.GetResponseStream();
            string resp = string.Empty;
            Encoding encoding = Encoding.UTF8;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                resp = reader.ReadToEnd();
            }
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(resp);
            if (xml.SelectSingleNode("/xml/return_code").InnerText.Equals("SUCCESS"))
            {
                if (xml.SelectSingleNode("/xml/result_code").InnerText.Equals("SUCCESS"))
                {
                    switch (xml.SelectSingleNode("/xml/status").InnerText) //SUCCESS:转账成功 FAILED:转账失败 PROCESSING:处理中
                    {
                        case "SUCCESS":
                            return new UAP.JSON {
                                {"openid",xml.SelectSingleNode("/xml/openid").InnerText},
                                {"transfer_name",xml.SelectSingleNode("/xml/transfer_name").InnerText},
                                {"payment_amount",xml.SelectSingleNode("/xml/payment_amount").InnerText},
                                {"detail_id",xml.SelectSingleNode("/xml/detail_id").InnerText}
                            };
                        case "FAILED": throw new Exception("转账失败" + xml.SelectSingleNode("/xml/reason").InnerText);
                        case "PROCESSING": throw new Exception("处理中:" + xml.SelectSingleNode("/xml/reason").InnerText);
                    }
                }
            }
            throw new Exception("获取支付状态异常:" + xml.SelectSingleNode("/xml/return_msg").InnerText);
        }


        //获取NotifyXML
        public static XmlDocument GetPayNotifyXml(HttpRequest request)
        {


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
            return xml;
        }

        //notify返回XML
        public static string makeXML(bool flag = false)
        {
            switch (flag)
            {
                case true:
                    return "<xml><return_code><![CDATA[SUCCESS]]></return_code><return_msg><![CDATA[OK]]></return_msg></xml>";
                case false:
                    return "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[FAIL]]></return_msg></xml>";
                default:
                    return "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[FAIL]]></return_msg></xml>";
            }
        }
    }
}