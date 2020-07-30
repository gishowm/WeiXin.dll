using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UAP;

namespace WeiXin
{
    /// <summary>
    /// 公众号机器人
    /// </summary>
    public class Robot
    {
        UAP.Net.WebClient wc;
        string token;

        /// <summary>
        /// 公众号机器人
        /// </summary>
        public Robot(string user = null, string pass = null)
        {
            wc = new UAP.Net.WebClient
            {
                webSite = "https://mp.weixin.qq.com"
            };
            wc.setLogin(delegate
            {
                wc.GET("/");

                UAP.JSON json = wc.POST("/cgi-bin/login", new
                {
                    username = user ?? Config.Acc_User,
                    pwd = UAP.Function.String.MD5(pass ?? Config.Acc_Pass).ToLower(),
                    imgcode = "",
                    f = "json"

                }).ToJSON();


                if (json.Json("base_resp").Int("ret") != 0)
                {
                    throw new Exception(json.Json("base_resp").String("err_msg"));
                }


                string login_url = json.String("redirect_url");

                if (login_url.Contains("validate_wx_tmpl"))
                {
                    string admin = Regex.Match(login_url, "bindalias\\=([^\\&]+)").Result("$1");
                    throw new Exception("此公众号需要管理员 " + admin + " 授权验证通过。");
                }

                token = Regex.Match(login_url, "token\\=(\\d+)").Result("$1");

                Console.WriteLine("login token:" + token);
            });
            wc.doLogin();
        }
        /// <summary>
        /// 主动发送信息
        /// </summary>
        /// <param name="openid">openid</param>
        /// <param name="content">内容</param>
        public void SendMessage(string openid, string content)
        {
            try
            {
                UAP.JSON json = wc.POST("/cgi-bin/singlesend?t=ajax-response&f=json&token=" + token + "&lang=zh_CN", new
                   {
                       token = token,
                       lang = "zh_CN",
                       f = "json",
                       ajax = 1,
                       random = new Random().Next(),
                       type = 1,
                       content = content,
                       tofakeid = openid,
                       imgcode = ""
                   }).ToJSON();

                if (json.Json("base_resp").Int("ret") != 0)
                {
                    Console.WriteLine(json.Json("base_resp").String("err_msg"));
                    return;
                }
            }
            catch
            {

            }
            Console.WriteLine("发送成功");
        }

        public void SendResource(string openid, string templateid)
        {
            JSON json=wc.POST("/cgi-bin/singlesend?t=ajax-response&f=json&token=" + token + "&lang=zh_CN", new
            {
                token = token,
                lang = "zh_CN",
                f = "json",
                ajax = 1,
                random = new Random().Next(),
                type = 10,
                app_id = templateid,
                tofakeid = openid,
                appmsgid = templateid,
                imgcode = ""
            }).ToJSON();

            if (json.Json("base_resp").Int("ret") != 0)
            {
                Console.WriteLine(json.Json("base_resp").String("err_msg"));
                throw new Exception(json.Json("base_resp").String("err_msg"));
            }
            Console.WriteLine("发送成功");
        }
    }
}
