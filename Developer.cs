using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeiXin
{
    /// <summary>
    /// 开发者工具
    /// </summary>
    [api]
    public static class Developer
    {
        [UAP.Attr.HttpGet]
        public static string access_token()
        {
            return Wechat.access_token;
        }

        [UAP.Attr.HttpGet]
        public static string jsapi_ticket()
        {
            return Wechat.jsapi_ticket;
        }
        [UAP.Attr.HttpGet]
        public static void refesh_AccessToken()
        {
            UAP.Cache.Remove("access_token");
        }
        [UAP.Attr.HttpGet]
        public static string getMenu()
        {
            return UAP.JSON.Decode(UAP.Function.Http.Get("https://api.weixin.qq.com/cgi-bin/menu/get?access_token=" + Wechat.access_token, Encoding.UTF8)).Json("menu").ToString(true);
        }

        /// <summary>
        /// 请参考getMenu
        /// </summary>
        /// <param name="MenuJSON"></param>
        /// <returns></returns>
        public static string setMenu(UAP.JSON MenuJSON)
        {
            string s = UAP.Function.Http.getPost("https://api.weixin.qq.com/cgi-bin/menu/create?access_token=" + Wechat.access_token, MenuJSON.ToString(true));
            return UAP.JSON.Decode(s).String("errmsg");
        }
    }
}
