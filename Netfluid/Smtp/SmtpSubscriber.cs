using System;
using System.Collections.Generic;

namespace Netfluid.Smtp
{
    public static class SmtpSubscriber
    {
        static Dictionary<string, Action<SmtpSessionInfo>> subscribers;

        static SmtpSubscriber()
        {
            subscribers = new Dictionary<string, Action<SmtpSessionInfo>>();
        }

        public static void Add(string host, Action<SmtpSessionInfo> action)
        {
            subscribers[host] = action;
        }

        public static void NewSession(SmtpSessionInfo s)
        {
            foreach (var r in s.Recipients)
            {
                if (subscribers.ContainsKey(r.Host))
                    subscribers[r.Host](s);
            }
        }
    }
}
