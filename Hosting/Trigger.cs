using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Netfluid
{
    public class Trigger
    {
        Delegate myDelegate;
        string url;
        Regex regex;
        MethodInfo methodInfo;

        public string Name { get; set; }

        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                if (url == null) return;

                url = value;

                var urlRegex = url;
                var find = new Regex(":[^//]+");
                foreach (Match item in find.Matches(url))
                {
                    urlRegex = urlRegex.Replace(item.Value, "(?<" + item.Value.Substring(1) + ">[^//]+?)");
                }
                regex = new Regex("^" + urlRegex + "$");
                GroupNames = regex.GetGroupNames();
            }
        }

        public string HttpMethod { get; set; }

        public int Index { get; set; }

        public ParameterInfo[] Parameters { get; private set; }

        public string[] GroupNames { get; private set; }

        public Delegate Delegate
        {
            get
            {
                return myDelegate;
            }
            set
            {
                myDelegate = value;
                methodInfo = value.Method;
                Parameters = methodInfo.GetParameters();
            }
        }

        public void Handle(Context cnt)
        {
            if(regex != null)
            {
                var m = regex.Match(cnt.Request.Url.LocalPath);

                if (!m.Success) return;

                for (int i = 0; i < GroupNames.Length; i++)
                {
                    var q = new QueryValue(GroupNames[i], m.Groups[GroupNames[i]].Value);
                    q.Origin = QueryValue.QueryValueOrigin.URL;
                    cnt.Values.Add(q.Name, q);
                }
            }

            object[] args = null;
            if (Parameters.Length > 0)
            {
                args = new object[Parameters.Length];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    var param = Parameters[i];

                    if (cnt.Values.Contains(param.Name))
                    {
                        args[i] = cnt.Values[param.Name].Parse(param.ParameterType);
                    }
                    else if (param.ParameterType == typeof(Context))
                    {
                        args[i] = cnt;
                    }
                }
            }

            Task.Factory.StartNew(() => Delegate.DynamicInvoke(args));
        }
    }
}
