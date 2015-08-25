using System;
using System.Reflection;

namespace Netfluid
{
    public class StatusCodeHandler
    {
        MethodInfo methodInfo;

        public string Name { get; set; }

        public string HttpMethod { get; set; }

        public int Index { get; set; }

        public StatusCode StatusCode { get; set; }

        public ParameterInfo[] Parameters { get; private set; }

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

                var resp = MethodInfo.DynamicInvoke(args) as IResponse;

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
