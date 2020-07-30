using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAP;

namespace WeiXin
{
    public abstract class User : UAP.User
    {
        string getWXJumpUrl(string redirect_uri, string state)
        {
            string url;
            url = "https://open.weixin.qq.com/connect/oauth2/authorize";
            url += "?appid=" + Config.AppID;
            url += "&redirect_uri=" + UAP.Function.String.UrlEncode(redirect_uri);
            url += "&response_type=code";
            url += "&scope=snsapi_userinfo";
            if (!string.IsNullOrEmpty(state))
            {
                url += "&state=" + state;
            }
            url += "#wechat_redirect";
            return url;
        }

        UAP.JSON getWXUser(string code)
        {
            UAP.JSON tmp;
            string data;
            using (System.Net.WebClient w = new System.Net.WebClient())
            {
                w.Encoding = System.Text.Encoding.UTF8;
                data = w.DownloadString(string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", WeiXin.Config.AppID, WeiXin.Config.AppSecret, code));
                tmp = UAP.JSON.Decode(data);
                try
                {
                    data = w.DownloadString(string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang=zh_CN", tmp.String("access_token"), tmp.String("openid")));
                    return UAP.JSON.Decode(data);
                }
                catch (Exception ew)
                {
                    throw new Exception(ew.Message + data);
                }
            }
        }

        public override void onForbidden(string loginUrl, string homeUrl)
        {
            Current.Response.Clear();
            Current.Response.StatusCode = 200;
            UAP.Session.Set("WeiXin.User.RawUrl", Current.Request.RawUrl);
            Current.Response.Redirect(getWXJumpUrl(Current.Request.Url.AbsoluteUri, "login"));
            //Current.Response.Write(string.Format("<meta name="viewport" content="user-scalable=no,width=device-width,initial-scale=1.0,maximum-scale=1.0" /><script>location='{0}'</script>请稍后...", getWXJumpUrl(Current.Request.Url.AbsoluteUri, "login")));
        }

        protected virtual JSON onRequiredLogin()
        {
            return null;
        }

        protected virtual bool onLogin(JSON wxUser)
        {
            LoginByData(wxUser);
            return true;
        }

        protected bool Login(JSON wxUser)
        {
            UAP.Session.Set("WeiXin.User.Data", wxUser);
            return onLogin(new JSON(wxUser));
        }

        public new static JSON Data
        {
            get
            {
                return UAP.Session.Get("WeiXin.User.Data");
            }
        }

        public override bool onCheckLegal()
        {
            bool v = base.onCheckLegal();
            if (v) return true;

            string code = Current.Request.QueryString.Get("code") ?? "", state = Current.Request.QueryString.Get("state") ?? "";
            JSON wxUser = new JSON();
            if (code.Length + state.Length == 0)
            {
                wxUser = onRequiredLogin();
                if (wxUser != null)
                {
                    return Login(wxUser);
                }
                return false;
            }
            try
            {

                wxUser = getWXUser(code);
                wxUser.Remove("privilege");
                bool log = Login(wxUser);
                if (log)
                {
                    string url = UAP.Session.Get("WeiXin.User.RawUrl");
                    if (!string.IsNullOrEmpty(url))
                    {
                        Current.Response.Redirect(url,false);
                    }
                }
                return log;
            }
            catch (Exception we)
            {
                try
                {
                    System.IO.File.AppendAllLines("D:\\wx_auth.txt", new string[] { "error:" + we.Message });
                }
                catch { }

                Current.Response.Write(we.Message);
                Current.Response.End();
                return true;
            }
        }
    }
}
