using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    public class Route
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public Regex Regex { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public int Index { get; set; }
        public Type Type { get; private set; }
        public ParameterInfo[] Parameters { get; private set; }
        public string[] GroupNames { get; private set; }

        public Host Host { get; private set; }

        public Route(Host host)
        {
            Host = host;
        }

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
