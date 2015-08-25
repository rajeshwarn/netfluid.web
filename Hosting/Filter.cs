using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    public class Filter
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

        public MethodInfo Delegate
        {
            get
            {
                return methodInfo;
            }
            set
            {
                methodInfo = value;
                Parameters = methodInfo.GetParameters();

                if (methodInfo.ReturnType != (typeof(bool)))
                    throw new Exception("Filters must returns bool");

                var r = Parameters[0].ParameterType;

                if (!r.Implements(typeof(IResponse)) || !r.IsByRef)
                    throw new Exception("Filters first parameter must be ref IResponse");
            }
        }

        public bool Handle(Context cnt)
        {
            if(regex != null)
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

            IResponse resp = null;
            var args = new object[] { resp };

            if (Parameters.Length > 1)
            {
                args = new object[Parameters.Length];
                for (int i = 1; i < Parameters.Length; i++)
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

            var res = (bool)Delegate.DynamicInvoke(args);

            if (res && resp!=null)
            {
                resp.SetHeaders(cnt);

                if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                    resp.SendResponse(cnt);

                cnt.Close();
                return true;
            }
            return false;
        }
    }
}
