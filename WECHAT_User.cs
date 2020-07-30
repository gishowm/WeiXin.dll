using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WeiXin
{
    public class WECHAT_User : Attribute
    {
        public static UserInfo User = null;
        public WECHAT_User()
        {
            try
            {
                Console.Write("经过构造函数");
                try
                {
                    User = UAP.Session.Get("weichatUser");
                }
                catch (Exception)
                {
                    User = null;
                }
                if (User == null)
                {
                    string code = Current.Request["code"];
                    var redirect_uri = Current.Request.Url;
                    var ticket = Current.Request["guid"];

                    if (string.IsNullOrEmpty(code))
                    {
                        var url = string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_userinfo&state={2}#wechat_redirect", WeiXin.Config.AppID, redirect_uri, ticket);
                        Current.Response.Redirect(url, true);
                    }
                    else
                    {
                        ticket = Current.Request["state"];
                        string url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", WeiXin.Config.AppID, WeiXin.Config.AppSecret, code);

                        HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
                        webrequest.Method = "get";
                        HttpWebResponse webreponse = (HttpWebResponse)webrequest.GetResponse();
                        Stream stream = webreponse.GetResponseStream();
                        string resp = string.Empty;
                        Encoding encoding = Encoding.UTF8;
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            resp = reader.ReadToEnd();
                        }
                        AccessToken tokens = new AccessToken();
                        tokens = JSONHandler.JsonDeserializeBySingleData<AccessToken>(resp);
                        User = getUserInfo(tokens.access_token, tokens.openid);
                        UAP.Session.Set("weichatUser", User);
                        GetDone(User);
                    }
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }


        private UserInfo getUserInfo(string token, string openid)
        {
            string url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang=zh_CN ", token, openid);
            HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            webrequest.Method = "get";
            HttpWebResponse webreponse = (HttpWebResponse)webrequest.GetResponse();
            Stream stream = webreponse.GetResponseStream();
            string resp = string.Empty;
            Encoding encoding = Encoding.UTF8;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                resp = reader.ReadToEnd();
            }
            UserInfo userifno = new UserInfo();
            userifno = JSONHandler.JsonDeserializeBySingleData<UserInfo>(resp);
            return userifno;
        }


        public virtual void GetDone(UserInfo user)
        {

        }
    }
}
