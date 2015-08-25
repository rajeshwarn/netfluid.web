using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    public class StatusCodeHandler:IRoute
    {
        string url;
        Regex regex;
        MethodInfo methodInfo;

        public string Name { get; set; }

        public string HttpMethod { get; set; }

        public int Index { get; set; }

        public StatusCode StatusCode { get; set; }

        public ParameterInfo[] Parameters { get; private set; }

        public string[] GroupNames { get; private set; }

        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                if (value == null) return;

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

                if (!methodInfo.ReturnType.Implements(typeof(IResponse)))
                    throw new Exception("Status code handlers must returns IResponse");
            }
        }

        public bool Handle(Context cnt)
        {
            if (cnt.Response.StatusCode == (int)StatusCode || StatusCode == StatusCode.AnyError || (cnt.Response.StatusCode >= 400 && cnt.Response.StatusCode <500 && StatusCode == StatusCode.AnyClientError) || (cnt.Response.StatusCode >= 500 && StatusCode == StatusCode.AnyServerError))
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
                        else if (param.ParameterType == typeof(Context))
                        {
                            args[i] = cnt;
                        }
                    }
                }

                var resp = MethodInfo.Invoke(Target,args) as IResponse;

                if (resp != null)
                {
                    resp.SetHeaders(cnt);

                    if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                        resp.SendResponse(cnt);

                    cnt.Close();
                }
                return true;
            }
            return false;
        }
    }
}
