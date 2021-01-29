using System;
using RestSharp;
using Newtonsoft.Json;
using System.Collections;

namespace FxxkCiDaRen
{ 
    class BingDictionary
    {
        public static ArrayList Translate(string original_text)
        {
            ArrayList result = new ArrayList();
            var client = new RestClient("https://cn.bing.com/dict/search?q=" + original_text);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request).Content;
            int start_position = response.IndexOf("<span class=\"def b_regtxt\"><span>");
            while (start_position != -1)
            {
                start_position += "<span class=\"def b_regtxt\"><span>".Length;
                int end_position = response.IndexOf("</span>", start_position);
                result.Add(response.Substring(start_position, end_position - start_position));
                start_position = response.IndexOf("<span class=\"def b_regtxt\"><span>", end_position);
            }
            return result;
        }
    }
}
