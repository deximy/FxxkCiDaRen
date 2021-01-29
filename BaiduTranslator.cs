using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using RestSharp;
using System.Text.RegularExpressions;
using System.Net;

namespace FxxkCiDaRen
{
    class BaiduTranslator
    {
        private string token_;
        private string gtk_;
        private string cookies_;

        private static int a(int r, string o)
        {
            var o_char = o.ToCharArray();
            for (var t = 0; t < o.Length - 2; t += 3)
            {
                var a = (int)o_char[t + 2];
                if (a >= 'a')
                {
                    a = a - 87;
                }
                else
                {
                    a = int.Parse(((char)a).ToString());
                }
                if ('+' == o[t + 1])
                {
                    UInt32 r_ = (UInt32)r;
                    a = (Int32)(r_ >> a);
                }
                else
                {
                    a = r << a;
                }
                if ('+' == o[t])
                {
                    r = r + a;
                }
                else
                {
                    r = r ^ a;
                }
            }
            return r;
        }

        private string GetSign(string origional_text, string gtk)
        {
            string[] e = gtk.Split('.');
            int h = int.Parse(e[0]);
            var i = int.Parse(e[1]);
            ArrayList d = new ArrayList();
            var t = origional_text.ToCharArray();
            for (int g = 0; g < origional_text.Length; g++)
            {
                var m = (int)t[g];
                if (128 > m)
                {
                    d.Add(m);
                }
                else
                {

                    if (2048 > m)
                    {
                        d.Add(m >> 6 | 192);
                    }
                    else
                    {
                        if (55296 == (64512 & m) && g + 1 < origional_text.Length && 56320 == (64512 & (int)t[g + 1]))
                        {
                            m = 65536 + ((1023 & m) << 10) + (1023 & (int)t[++g]);
                            d.Add(m >> 18 | 240);
                            d.Add(m >> 12 & 63 | 128);
                        }
                        else
                        {
                            d.Add(m >> 12 | 224);
                            d.Add(m >> 6 & 63 | 128);
                        }
                        d.Add(63 & m | 128);
                    }
                }
            }
            int S = h;
            var u = "+-a^+6";
            var l = "+-3^+b+-f";
            for (int s = 0; s < d.Count; s++)
            {
                S += (int)d[s];
                S = a(S, u);
            }
            S = a(S, l);
            S ^= i;
            double S_ = 0;
            if (0 > S)
                S_ = (2147483647 & S) + 2147483648;

            S_ %= 1e6;
            return S_.ToString() + "." + ((int)S_ ^ h).ToString();
        }

        public BaiduTranslator(string token, string gtk, string cookie)
        {
            this.cookies_ = cookie;
            this.token_ = token;
            this.gtk_ = gtk;
        }

        public void Translate(string original_text)
        {
            var client = new RestClient("https://fanyi.baidu.com/");
            client.Timeout = -1;
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36";
            var request = new RestRequest("v2transapi?from=en&to=zh", Method.POST);
            request.AddCookie("BAIDUID", cookies_);
            request.AlwaysMultipartFormData = true;
            request.AddParameter("from", "en");
            request.AddParameter("to", "zh");
            request.AddParameter("query", original_text);
            request.AddParameter("simple_means_flag", "3");
            request.AddParameter("sign", GetSign(original_text, gtk_));
            request.AddParameter("token", token_);
            request.AddParameter("domain", "common");
            var response = client.Execute(request).Content;
            System.Web.HttpUtility.HtmlDecode("\u5b8b  \u79e6\u89c2 \u300a\u5343\u79cb\u5c81\u300b\u8bcd\uff1a\u201c");
            Console.WriteLine(response);
        }
    }
}