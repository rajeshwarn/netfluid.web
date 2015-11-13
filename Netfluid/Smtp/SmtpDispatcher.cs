using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Threading;

namespace Netfluid.Smtp
{
    public class SmtpDispatcher
    {
        Dictionary<string, string> hostToUrl;
        public SmtpServer Server { get; private set; }

        public SmtpDispatcher()
        {
            hostToUrl = new Dictionary<string, string>();
            Server = new SmtpServer();
            Server.OnMessageArrived = (x) => 
            {
                var json = JSON.Serialize(x);
                var wc = new WebClient();

                foreach (var to in x.To)
                {
                    string url;

                    if (hostToUrl.TryGetValue(to.Host,out url))
                    {
                        wc.UploadString(url, json);
                    }
                }
                return DateTime.Now.Ticks.ToString();
            };
        }

        public static void Register(string registrant, string url, string host)
        {
            var wc = new WebClient();

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    wc.UploadValues(registrant, new NameValueCollection
                    {
                        { "url", url },
                        { "host", host }
                    });
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
