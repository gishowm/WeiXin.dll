using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UAP;

namespace WeiXin
{
    public class WebLogin : Attribute
    {
        //使用：<script src="http://res.wx.qq.com/connect/zh_CN/htmledition/js/wxLogin.js"></script>
        public WebLogin()
        {
            var code = Current.Request["code"];
            if (string.IsNullOrEmpty(code)) return;
            var token = GetAccess_Token(code);
            var url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}", token["access_token"], token["openid"]);
            string s = UAP.Function.Http.Get(url, Encoding.UTF8);
            OnUserInfo(JSON.Decode(s));

        }
        public JSON GetAccess_Token(dynamic code)
        {
            return UAP.Cache.Get("access_token", 7200, delegate
            {

                var url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", Config.OpenWebAppID, Config.OpenWebAppID, code);
                string s = UAP.Function.Http.Get(url, Encoding.UTF8);
                try
                {
                    return JSON.Decode(s);
                }
                catch (Exception)
                {
                    throw new Exception("获取access_token出错:" + s);
                }
            });
        }

        public virtual  void OnUserInfo(JSON json)
        {

        }
    }
}
