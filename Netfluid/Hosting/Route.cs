using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    public partial class Route
    {
        string url;
        Regex regex;
        protected dynamic method;

        protected Route() { }

        public Route(dynamic funcOrAction)
        {
            method = funcOrAction;
        }

        public Route(MethodInfo mi, object instance)
        {
            method = new MethodInfoWrapper
            {
                MethodInfo = mi,
                Target = instance
            };
        }

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

        internal string[] GroupNames { get; private set; }

        internal ParameterInfo[] Parameters { get; private set; }

        internal virtual dynamic Handle(Context cnt)
        {
            if (regex != null)
            {
                var m = regex.Match(cnt.Request.Url.LocalPath);

                if (!m.Success) return false;

                for (int i = 0; i < GroupNames.Length; i++)
                {
                    cnt.Values.Add(new QueryValue() { Name = GroupNames[i], Value = m.Groups[GroupNames[i]].Value, Origin = QueryValue.QueryValueOrigin.URL });
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

            return method.Invoke(args);
        }
    }
}