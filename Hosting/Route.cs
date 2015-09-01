using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    public class Route
    {
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
                url = value;

                if (string.IsNullOrEmpty(value))
                {
                    if (this.GetType() == typeof(Route))
                        throw new ArgumentNullException("Routes url can not be null");

                    return;
                }

                if (value == null) return;

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

        public object Target { get; set; }

        public MethodInfo MethodInfo
        {
            get
            {
                return methodInfo;
            }
            set
            {
                methodInfo = value;
                Parameters = methodInfo.GetParameters();
            }
        }

        internal virtual dynamic Handle(Context cnt)
        {
            if (regex != null)
            {
                var m = regex.Match(cnt.Request.Url.LocalPath);

                if (!m.Success) return false;

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
                    else if(param.ParameterType==typeof(Context))
                    {
                        args[i] = cnt;
                    }
                    else if(param.ParameterType.IsValueType)
                    {
                        args[i] = param.ParameterType.DefaultValue();
                    }
                }
            }
            return methodInfo.Invoke(Target, args);
        }
    }
}