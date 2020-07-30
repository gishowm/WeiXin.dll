using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace WeiXin
{
    /// <summary>
    /// 微信服务
    /// </summary>
    /// 
    [api]
    public static class Wechat
    {
        public static string dev_access_token, dev_jsapi_ticket;
        /// <summary>
        /// access_token
        /// </summary>
        public static string access_token
        {
            get
            {
                if (string.IsNullOrEmpty(dev_access_token))
                {
                    
                    return UAP.Cache.Get("access_token", 7200, delegate
                    {
                        string s = UAP.Function.Http.Get(string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", Config.AppID, Config.AppSecret), Encoding.UTF8);
                        try
                        {
                            return UAP.JSON.Decode(s).String("access_token");
                        }
                        catch (Exception)
                        {
                            throw new Exception("获取access_token出错:" + s);
                        }
                    });
                }
                return dev_access_token;
            }
        }

        /// <summary>
        /// jsapi_ticket
        /// </summary>
        public static string jsapi_ticket
        {
            get
            {
                if (string.IsNullOrEmpty(dev_jsapi_ticket))
                {
                    return UAP.Cache.Get("jsapi_ticket", 7200, delegate
                    {
                        string s = UAP.Function.Http.Get(string.Format("https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={0}&type=jsapi", access_token), Encoding.UTF8);
                        try
                        {
                            return UAP.JSON.Decode(s).String("ticket");
                        }
                        catch (Exception)
                        {
                            throw new Exception("获取jsapi_ticket出错:" + s);
                        }
                    });
                }
                return dev_jsapi_ticket;
            }
        }

        public static UAP.JSON getWXUserByOpenID(string OpenID)
        {
            string s = UAP.Function.Http.Get(string.Format("https://api.weixin.qq.com/cgi-bin/user/info?access_token={0}&openid={1}&lang=zh_CN", access_token, OpenID), Encoding.UTF8);
            return UAP.JSON.Decode(s);
        }

        public static string SendTemplate(string template_id, string OpenID, string url, UAP.JSON data)
        {
            UAP.JSON post = new UAP.JSON();
            post.Add("touser", OpenID);
            post.Add("template_id", template_id);
            post.Add("url", url);
            string[] keys = new string[data.Keys.Count];
            data.Keys.CopyTo(keys, 0);
            foreach (string key in keys)
            {
                object v = data[key];
                if (v is UAP.JSON) continue;
                UAP.JSON json = new UAP.JSON();
                json.Add("value", v).Add("color", "#173177");
                data[key] = json;
            }
            post.Add("data", data);
            string s = UAP.Function.Http.getPost("https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=" + access_token, post.ToString(true));
            return UAP.JSON.Decode(s).String("errmsg");
        }
        public static string SendTemplate(string template_id, string OpenID, string url, UAP.JSON data, string token)
        {
            UAP.JSON post = new UAP.JSON();
            post.Add("touser", OpenID);
            post.Add("template_id", template_id);
            post.Add("url", url);
            string[] keys = new string[data.Keys.Count];
            data.Keys.CopyTo(keys, 0);
            foreach (string key in keys)
            {
                object v = data[key];
                if (v is UAP.JSON) continue;
                UAP.JSON json = new UAP.JSON();
                json.Add("value", v).Add("color", "#173177");
                data[key] = json;
            }
            post.Add("data", data);
            string s = UAP.Function.Http.getPost("https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=" + token, post.ToString(true));
            return UAP.JSON.Decode(s).String("errmsg");
        }

        /// <summary> 
        /// return {ticket,url,expire_seconds}
        /// 地址:https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=ticket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="expireSeconds">默认7天有效</param>
        /// <returns></returns>
        public static UAP.JSON getQRCode(string id, int expireSeconds = 2592000)
        {
            string s;
            if (expireSeconds > 0)
            {
                //s = UAP.Cache.Get("scene_id_" + id, expireSeconds, delegate
                //{
                s = UAP.Function.Http.getPost("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + access_token, "{\"expire_seconds\":" + expireSeconds + ",\"action_name\":\"QR_SCENE\",\"action_info\":{\"scene\":{\"scene_id\":" + id + "}}}");
                //});
            }
            else
            {
                //s = UAP.Cache.Get("scene_str_" + id, expireSeconds, delegate
                //{
                s = UAP.Function.Http.getPost("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + access_token, "{\"action_name\": \"QR_LIMIT_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\":\"" + id + "\"}}}");
                //});
            }
            try
            {
                return UAP.JSON.Decode(s);
            }
            catch
            {
                throw new Exception(s);
            }
        }
    }
}
