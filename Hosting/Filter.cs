using System;
using System.Reflection;

namespace Netfluid
{
    public class Filter : Route
    {
        public override MethodInfo MethodInfo
        {
            get
            {
                return base.MethodInfo;
            }
            set
            {
                if (!value.ReturnType.Implements(typeof(bool)))
                    throw new TypeLoadException("Filter methods must returns a bool");

                var args = value.GetParameters();
                if (args.Length != 1 || args[0].ParameterType.FullName != "NetFluid.IResponse&")
                    throw new TypeLoadException("Filters must have one parameter (ref IResponse)");

                base.MethodInfo = value;
            }
        }

        public override void Handle(Context cnt)
        {
            try
            {
                var exposer = Type.CreateIstance() as MethodExposer;
                exposer.Context = cnt;
                exposer.Host = Host;

                var args = new object[] { null };

                if ((bool)MethodInfo.Invoke(exposer, args) && args[0] != null)
                {
                    var resp = args[0] as IResponse;

                    if (resp != null)
                        resp.SetHeaders(cnt);

                    try
                    {
                        if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                            resp.SendResponse(cnt);
                    }
                    catch (Exception ex)
                    {
                        if (Engine.ShowException)
                        {
                            cnt.Writer.Write(ex.ToString());
                        }
                    }
                    cnt.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError(cnt, ex.InnerException);
            }
        }
    }
}
