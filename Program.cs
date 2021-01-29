using System;
using Fiddler;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;
using System.Collections;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using GoogleTranslateFreeApi;
using RestSharp;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace FxxkCiDaRen
{
    class Program
    {
        static X509Certificate2 root_certificate;
        private static readonly GoogleTranslator translator = new GoogleTranslator();
        // private static BaiduTranslator translator = null;
        static bool use_api = false;

        static void Main(string[] args)
        {
            string input;

            Console.WriteLine("作者: 闲月疏云");
            Console.WriteLine("免费软件, 免费软件, 免费软件, 重要的事情说三遍!");

            Console.WriteLine("免责声明:");
            Console.WriteLine("本程序仅限用于学习和研究目的，不得用于商业或者非法用途，否则，一切后果请用户自负。您必须在下载后的24个小时之内，从您的电脑中彻底删除本程序。");
            
            Console.WriteLine("请输入\"我同意上述声明\"以继续:");
            input= Console.ReadLine();
            if (input != "我同意上述声明")
                return;
           
            Console.WriteLine("您是否同意使用第三方题库? (y/N)");
            Console.WriteLine("第三方词库非本人开发 / 维护, 题库域名mcol.cc, 在此向题库作者大佬致以崇高的敬意, 如有侵权我会尽快删除!");
            if (Console.ReadLine() == "y")
                use_api = true;

            Console.WriteLine("挂科模式, 启动!");

            if (!InitFiddler())
                return;

            Console.WriteLine("Possible ip:");
            foreach (string ip in GetPossibleIp())
                Console.WriteLine(ip);
            Console.WriteLine("Port: 5654");

            do
            {
                input = Console.ReadLine();
            } while (input != "quit");

            Clean();

            Console.WriteLine("程序可以帮你拿一次高分, 但只有真正掌握了知识才能每次都拿高分, 后会有期!");
            Console.WriteLine("获得支持: QQ群: 1098697159");
        }

        static bool InitFiddler()
        {
            Console.WriteLine("Initializing...");

            if (Fiddler.CertMaker.GetRootCertificate() == null)
            {
                Console.WriteLine("Unable to find root certificate. Try to create a new one.");
                if (!Fiddler.CertMaker.createRootCert())
                {
                    Console.WriteLine("FATAL ERROR: Failed when creating root certificate!");
                    return false;
                }
                root_certificate = Fiddler.CertMaker.GetRootCertificate();
            }
            X509Store cert_store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            cert_store.Open(OpenFlags.ReadWrite);
            try
            {
                cert_store.Add(root_certificate);
            }
            catch
            {
                Console.WriteLine("FATAL ERROR: Failed when adding root certificate to system!");
                return false;
            }
            finally
            {
                cert_store.Close();
            }
            Fiddler.FiddlerApplication.oDefaultClientCertificate = root_certificate;
            Fiddler.FiddlerApplication.Startup(5654, FiddlerCoreStartupFlags.AllowRemoteClients | FiddlerCoreStartupFlags.DecryptSSL | FiddlerCoreStartupFlags.RegisterAsSystemProxy);
            Fiddler.FiddlerApplication.BeforeResponse += BeforeResponse;

            Console.WriteLine("Initialization finished.");
            return true;
        }

        static void Clean()
        {
            Console.WriteLine("Cleaning...");
            Fiddler.FiddlerApplication.Shutdown();
            X509Store cert_store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            cert_store.Open(OpenFlags.ReadWrite);
            cert_store.Remove(root_certificate);
            Console.WriteLine("Clean finished.");
        }

        static void BeforeResponse(Fiddler.Session session)
        {
            if (session.url.Contains("gateway.vocabgo.com"))
            {
                if (session.url.Contains("StartAnswer") || session.url.Contains("SubmitAnswerAndSave") || session.url.Contains("SkipAnswer") || session.url.Contains("SubmitAnswerAndReturn"))
                {
                    if (!session.utilDecodeResponse())
                    {
                        Console.WriteLine("Could not decode response! URL: " + session.url);
                        return;
                    }

                    Console.WriteLine("");
                    Console.WriteLine("===================================================================");
                    Console.WriteLine("===================================================================");

                    dynamic json = JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(session.responseBodyBytes));

                    if (json.code.Value != 1)
                    {
                        Console.WriteLine("返回代码: " + json.code.Value);
                        Console.WriteLine("返回信息: " + json.msg.Value);
                        return;
                    }

                    if (use_api)
                    {
                        var client = new RestClient("http://cidaren.mcol.cc/q.php");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter(
                            "application/json",
                            "{\"code\": true,\"content\": \"" + json.data.stem.content + "\",\"remark\": \"" + json.data.stem.remark + "\",\"mode\": " + json.data.topic_mode + "}",
                            ParameterType.RequestBody
                            );
                        var response = client.Execute(request).Content;
                        dynamic answer = JsonConvert.DeserializeObject(response);
                        if (answer.Property("answer") != null && answer.answer.Count > 0)
                        {
                            Console.WriteLine("已在题库中找到该题目答案!");
                            Console.WriteLine("题目:");
                            Console.WriteLine("    " + answer.content);
                            Console.WriteLine("答案:");
                            Console.WriteLine("    " + answer.answer[0][0]);
                            return;
                        }

                        Console.WriteLine("题目中没有该题目答案, 转为辅助模式");
                    }

                    SwitchQuestionType(json);
                }
            }
            
        }


        static ArrayList GetPossibleIp()
        {
            ArrayList ip_list = new ArrayList();
            foreach (NetworkInterface network_interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!network_interface.Supports(NetworkInterfaceComponent.IPv4) || network_interface.Name.Contains("VMware"))
                    continue;

                foreach (UnicastIPAddressInformation ip_infomation in network_interface.GetIPProperties().UnicastAddresses)
                {
                    if (ip_infomation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        ip_list.Add(ip_infomation.Address.ToString());
                }
            }
            return ip_list;
        }

        static void SwitchQuestionType(dynamic json)
        {
            if (json.data.options.Count == 0)
                FillInTheBlank(json);
            else if (json.data.stem.remark.GetType() == typeof(JArray))
                CollocateVocabulary(json);
            else if (json.data.stem.content.Value.Contains("_"))
                ChooseOrder(json);
            else if (json.data.stem.content.Value.Contains("{}"))
                ChooseToFillTheBlank(json);
            else if (json.data.stem.content.Value.Contains("{") && json.data.stem.content.Value.Contains("}"))
                ChooseWordMeanInSentence(json);
            else if (Regex.IsMatch(json.data.stem.content.Value, @"[\u4e00-\u9fa5]"))
                TranslateChineseToEnglish(json);
            else if (Regex.IsMatch(json.data.options[0].content.Value, @"[\u4e00-\u9fa5]")
                    && Regex.IsMatch(json.data.options[1].content.Value, @"[\u4e00-\u9fa5]")
                    && Regex.IsMatch(json.data.options[2].content.Value, @"[\u4e00-\u9fa5]")
                    && Regex.IsMatch(json.data.options[3].content.Value, @"[\u4e00-\u9fa5]")
                    )
                TranslateEnglishToChinese(json);
            else
                Console.WriteLine("未知题型, 请联系开发者! 题目数据: " + json.data);
        }

        static async void FillInTheBlank(dynamic json)
        {
            Console.WriteLine("该题为: 根据中文意思补全所缺单词");
            Console.WriteLine("题目:");
            Console.WriteLine("  " + json.data.stem.content.Value);
            Console.WriteLine("题目翻译:");
            Console.WriteLine("  " + json.data.stem.remark.Value);
            Console.WriteLine("提示(单词前缀): " + json.data.w_tip.Value);
            Console.WriteLine("单词总长度: " + json.data.w_len.Value.ToString());
            Console.WriteLine();

            string chinese = json.data.stem.remark.Value;
            string prefix = json.data.w_tip.Value;
            int full_length = (int)json.data.w_len.Value;
            int length = full_length - prefix.Length;
            ArrayList english_results = new ArrayList();
            
            var translate_result = await translator.TranslateLiteAsync(chinese, Language.Auto, GoogleTranslator.GetLanguageByName("English"));
            foreach (string translation in translate_result.FragmentedTranslation)
                foreach (Match match in Regex.Matches(translation, @"\b" + prefix + "[a-z]{" + length.ToString() + @"}\b"))
                    if (!english_results.Contains(match.Value))
                        english_results.Add(match.Value);
            
            if (english_results.Count > 0)
            {
                Console.WriteLine("根据翻译, 可能的结果如下:");
                foreach (string english_result in english_results)
                    ShowAllEnglishTranslation(english_result, 4);
                Console.WriteLine("是否有正确答案? [y/N]:");
                if (Console.ReadLine() == "y")
                    return;
            }

            var client = new RestClient("https://www.morewords.com/change-max-results");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Referer", @"https://www.morewords.com/search?w=" + prefix + @"*&length=" + full_length.ToString() + @"&sort=alpha-az");
            request.AlwaysMultipartFormData = true;
            request.AddParameter("max_results", "all");
            var response = client.Execute(request).Content;
            int pre_count = english_results.Count;
            foreach (Match match in Regex.Matches(response, prefix + "[a-z]{" + length + "}"))
            {
                string english_result = match.Value.Substring(0, full_length);
                if (!english_results.Contains(english_result))
                    english_results.Add(english_result);
            }
            english_results.RemoveRange(0, pre_count);

            if (english_results.Count > 0)
            {
                Console.WriteLine("所有可能的结果如下:");
                foreach (string english_result in english_results)
                    ShowAllEnglishTranslation(english_result, 4);
                return;
            }

            Console.WriteLine("没有找到合适的词, 应该是个bug, 告辞(逃");
        }

        static void CollocateVocabulary(dynamic json)
        {
            Console.WriteLine("该题为: 选出与所给单词常见的搭配词");
            Console.WriteLine("题目: " + json.data.stem.content.Value);
            Console.WriteLine();

            Console.WriteLine("答案如下(不用找翻译了, 保证正确): ");
            foreach (dynamic remark in json.data.stem.remark)
            {
                Console.WriteLine("    " + remark.relation.Value);
            }
        }

        static void ChooseOrder(dynamic json)
        {
            Console.WriteLine("该题为: 选择正确的单词顺序, 组成正确的表达");
            Console.WriteLine("题目: " + json.data.stem.content.Value);
            Console.WriteLine("题目翻译: " + json.data.stem.remark.Value);
            Console.WriteLine();

            Console.WriteLine("所有候选词及翻译如下:");
            foreach (dynamic option in json.data.options)
                ShowAllEnglishTranslation(option.content.Value, 4);
        }

        static void ChooseToFillTheBlank(dynamic json)
        {
            Console.WriteLine("该题为: 选出合适的单词, 补全句意");
            Console.WriteLine("题目:");
            Console.WriteLine("  " + json.data.stem.content.Value);
            Console.WriteLine("题目翻译:");
            Console.WriteLine("  " + json.data.stem.remark.Value);
            Console.WriteLine();

            Console.WriteLine("所有候选词及翻译如下:");
            foreach (dynamic option in json.data.options)
                ShowAllEnglishTranslation(option.content.Value, 4);
        }

        static void ChooseWordMeanInSentence(dynamic json)
        {
            Console.WriteLine("该题为: 选出被\"{}\"括起来的单词的词义");
            Console.WriteLine("题目:");
            Console.WriteLine("  " + json.data.stem.content.Value);
            Console.WriteLine("题目翻译:");
            Console.WriteLine("  " + json.data.stem.remark.Value);
            Console.WriteLine();

            Console.WriteLine("所有选项如下:");
            foreach (dynamic option in json.data.options)
                Console.WriteLine("    " + option.content.Value);
        }

        static void TranslateChineseToEnglish(dynamic json)
        {
            Console.WriteLine("该题为: 根据所给词义选出单词");
            Console.WriteLine("题目: " + json.data.stem.content.Value);
            Console.WriteLine();

            Console.WriteLine("所有候选词及翻译如下:");
            foreach (dynamic option in json.data.options)
                ShowAllEnglishTranslation(option.content.Value, 4);
        }

        static void TranslateEnglishToChinese(dynamic json)
        {
            Console.WriteLine("该题为: 根据所给单词选出词义");
            Console.WriteLine("题目: " + json.data.stem.content.Value);
            Console.WriteLine();

            Console.WriteLine("该单词翻译如下:");
            ShowAllEnglishTranslation(json.data.stem.content.Value, 4);
            Console.WriteLine();

            Console.WriteLine("所有选项如下:");
            foreach (dynamic option in json.data.options)
                Console.WriteLine("    " + option.content);
        }

        static void ShowAllEnglishTranslation(string english, int indent)
        {
            string prefix = indent > 0 ? RepeatString(" ", indent) : "";
            Console.WriteLine(prefix + english + ":");
            ArrayList translations = BingDictionary.Translate(english);
            if (translations != null)
            {
                foreach (string translation in translations)
                    Console.WriteLine(prefix + "    " + translation);
            }
            else
            {
                var translate_result = translator.TranslateAsync(english, Language.Auto, GoogleTranslator.GetLanguageByName("Chinese Simplified"));

                if (translate_result.Result.ExtraTranslations == null)
                {
                    foreach (string translation in translate_result.Result.FragmentedTranslation)
                        Console.WriteLine(prefix + "    " + translation);
                    return;
                }

                if (translate_result.Result.ExtraTranslations.Abbreviation != null)
                {
                    Console.WriteLine(prefix + "    Abbreviation:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Abbreviation)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Adjective != null)
                {
                    Console.WriteLine(prefix + "    Adjective:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Adjective)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Adverb != null)
                {
                    Console.WriteLine(prefix + "    Adverb:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Adverb)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.AuxiliaryVerb != null)
                {
                    Console.WriteLine(prefix + "    AuxiliaryVerb:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.AuxiliaryVerb)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Conjunction != null)
                {
                    Console.WriteLine(prefix + "    Conjunction:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Conjunction)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Interjection != null)
                {
                    Console.WriteLine(prefix + "    Interjection:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Interjection)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Noun != null)
                {
                    Console.WriteLine(prefix + "    Noun:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Noun)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Phrase != null)
                {
                    Console.WriteLine(prefix + "    Phrase:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Phrase)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Prefix != null)
                {
                    Console.WriteLine(prefix + "    Prefix:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Prefix)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Preposition != null)
                {
                    Console.WriteLine(prefix + "    Preposition:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Preposition)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Pronoun != null)
                {
                    Console.WriteLine(prefix + "    Pronoun:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Pronoun)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Suffix != null)
                {
                    Console.WriteLine(prefix + "    Suffix:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Suffix)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
                if (translate_result.Result.ExtraTranslations.Verb != null)
                {
                    Console.WriteLine(prefix + "    Verb:");
                    foreach (var translation in translate_result.Result.ExtraTranslations.Verb)
                        Console.WriteLine(prefix + "        " + translation.Phrase);
                }
            }

            
        }

        static string RepeatString(string str, int n)
        {
            char[] arr = str.ToCharArray();
            char[] arrDest = new char[arr.Length * n];
            for (int i = 0; i < n; i++)
                Buffer.BlockCopy(arr, 0, arrDest, i * arr.Length * 2, arr.Length * 2);
            return new string(arrDest);
        }
    }
}
