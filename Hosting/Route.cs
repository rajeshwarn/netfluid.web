using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    public class Route
    {
        MethodInfo methodInfo;
        string url;
        Regex regex;

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

                if (url == null) return;

                var urlRegex = url;
                var find = new Regex(":[^//]+");
                foreach (Match item in find.Matches(url))
                {
                    urlRegex = urlRegex.Replace(item.Value, "(?<" + item.Value.Substring(1) + ">[^//]+?)");
                }
                regex = new Regex("^" + urlRegex + "$");
            }
        }

        public string Method { get; set; }

        public Regex Regex
        {
            get
            {
                return regex;
            }
            set
            {
                regex = value;
                url = regex.ToString();
            }
        }

        public virtual MethodInfo MethodInfo
        {
            get
            {
                return methodInfo;
            }
            set
            {
                var type = value.DeclaringType;

                if (!type.Inherit(typeof(MethodExposer)))
                        throw new TypeLoadException(value.Name + " is declared by " + type.FullName +" wich is not a NetFluid.MethodExposer");

                if (!type.HasDefaultConstructor())
                    throw new TypeLoadException(type.FullName + " does not have a parameterless constructor");

                methodInfo = value;
                Type = type; 
            }
        }

        public int Index { get; set; }

        public Type Type { get; private set; }
        public ParameterInfo[] Parameters { get; private set; }
        public string[] GroupNames { get; private set; }

        public Host Host { get; internal set; }

        public virtual void Handle(Context cnt)
        {
            MethodExposer exposer = null;
            object[] args;
            IResponse resp;

            #region ARGS
            try
            {
                exposer = Type.CreateIstance() as MethodExposer;
                exposer.Context = cnt;
                exposer.Host = Host;

                args = null;

                if (Parameters != null && Parameters.Length > 0)
                {
                    args = new object[Parameters.Length];
                    for (int i = 0; i < Parameters.Length; i++)
                    {
                        if (cnt.Values.Contains(Parameters[i].Name))
                        {
                            args[i] = cnt.Values[Parameters[i].Name].Parse(Parameters[i].ParameterType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(cnt, ex);
                return;
            }
            #endregion

            #region RESPONSE
            try
            {
                resp = MethodInfo.Invoke(exposer, args) as IResponse;

                if (resp != null)
                    resp.SetHeaders(cnt);
            }
            catch (Exception ex)
            {
                ShowError(cnt, ex.InnerException);
                return;
            }
            #endregion

            try
            {
                if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                    resp.SendResponse(cnt);

                cnt.Close();
            }
            catch (Exception ex)
            {
                ShowError(cnt, ex);
            }
        }

        public void ShowError(Context cnt, Exception ex)
        {
            try
            {
                Engine.Logger.Log("exception serving " + cnt.Request.Url, ex);

                if (ex is TargetInvocationException)
                    ex = ex.InnerException;

                if (Engine.ShowException)
                    cnt.Writer.Write(ex);
            }
            catch
            {

            }
            cnt.Close();
        }
    }
}
