using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Service
{
    class DefaultExposer:MethodExposer
    {
        static DefaultExposer()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Engine.Logger.Log(LogLevel.SystemException, "unhandled exception", e.ExceptionObject as Exception);
        }

        [CallOn(StatusCode.AnyError)]
        public IResponse NotFound()
        {
            return new NetFluid.Templates.MustacheResponse("./ui/404.html");
        }
    }
}
