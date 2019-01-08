using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Globalization;
using System.Collections.Generic;

namespace ParseLunchHtmlFunction
{
    public static class Function1
    {
        [FunctionName("ParseLunchHtml")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string lunchContent = null;
            var cultureInfo = CultureInfo.GetCultureInfo("fi-FI");
            string dateMatchStr = "tab-" + DateTime.UtcNow.ToString("dddd-d-M", cultureInfo).ToLower();

            // Get request body
            string data = await req.Content.ReadAsStringAsync();

            if (data != null)
            {
                var idx = data.IndexOf(dateMatchStr);
                if (idx == -1)
                {                    
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Todays date not matched in lunch data.");
                }

                lunchContent = parseLunchContent(data, idx, log);
            }

            return lunchContent == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Body must contain HTML with lunch data")
                : req.CreateResponse(HttpStatusCode.OK, lunchContent);
        }

        private static string parseLunchContent(string data, int idx, TraceWriter log)
        {
            int pos = idx;
            bool insideTag = false;
            bool isEndingTag = false;
            string content = "<div id=\"";
            string currentTag = "";
            Stack<bool> depth = new Stack<bool>();
            depth.Push(true);

            try
            {
                while (depth.ToArray().Length > 0)
                {
                    char c = data[idx];
                    idx++;
                    content += c;

                    switch (c)
                    {
                        case '<':
                            insideTag = true;
                            break;
                        case '/':
                            if (insideTag)
                            {
                                isEndingTag = true;
                            }
                            break;
                        case '>':
                            if (currentTag.StartsWith("div"))
                            {
                                if (!isEndingTag)
                                {
                                    depth.Push(true);
                                }
                                else
                                {
                                    depth.Pop();
                                }
                            }

                            currentTag = "";
                            insideTag = false;
                            isEndingTag = false;
                            break;
                        default:
                            if (insideTag)
                            {
                                currentTag += c;
                            }
                            break;
                    }
                } 
            }
            catch (Exception ex)
            {
                log.Error("Error parsing lunch HTML", ex);
            }

            return content;
        }
    }
}
