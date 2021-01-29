using System;
using RestSharp;
using Newtonsoft.Json;
using System.Collections;

namespace FxxkCiDaRen
{
    class GoogleDictonary
    {
        public static ArrayList Translate(string original_text)
        {
            ArrayList result = new ArrayList();
            var client = new RestClient("http://gdictchinese.freecollocation.com/ajax_search/?q=" + original_text);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request).Content;
            Console.WriteLine(response);
            dynamic retn = JsonConvert.DeserializeObject(response);
            string info = retn.info;
            if (info == "")
                return null;
            info = info.Replace("\\", string.Empty);
            Console.WriteLine(info);
            dynamic translations = JsonConvert.DeserializeObject(info);
            foreach (dynamic translation in translations.primaries[0].entries[0].entries)
            {
                if (translation.terms.Count > 1)
                    result.Add(translation.terms[1].text.Value);
            }
                
            return result;
        }
    }
}
