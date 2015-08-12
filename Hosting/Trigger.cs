using System;

namespace Netfluid
{
    public class Trigger : Route
    {
        public override void Handle(Context cnt)
        {
            var exposer = Type.CreateIstance() as MethodExposer;
            exposer.Context = cnt;
            exposer.Host = Host;

            object[] args = null;

            if (Parameters != null && Parameters.Length > 0)
            {
                args = new object[Parameters.Length];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    var q = cnt.Values[Parameters[i].Name];
                    if (q != null)
                        args[i] = q.Parse(Parameters[i].ParameterType);
                }
            }

            try
            {
                MethodInfo.Invoke(exposer, args);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log("exception serving " + cnt.Request.Url, ex.InnerException);
            }
        }
    }
}
