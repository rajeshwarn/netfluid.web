using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using NetFluid.Collections;


namespace NetFluid.Service
{
    public class Service : ServiceBase
    {
        static void Main(string[] args)
        {
            if (args!=null && args.Length>=1 && args[0]=="debug")
            {
               (new Service()).OnStart(null);
               Engine.DevMode = true;
                Console.ReadLine();
                return;
            }
            Run(new Service());
        }


        protected override void OnStart(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                if (!Engine.LoadAppConfiguration())
                {
                    Engine.Interfaces.AddAllAddresses();
                    Engine.AddPublicFolder("/", "./Public", true);
                    Engine.Interfaces.AddInterface("127.0.0.1", 80);
                    Engine.Interfaces.AddInterface("127.0.0.1", 8000);
                }

                Engine.Start();
                ExternalHostManager.Start();
                PluggedHostManager.Start();

                Engine.DevMode = true;
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception,"Error starting up the service",ex);
            }

        }


        protected override void OnStop()
        {
            Engine.Logger.Log(LogLevel.Warning,"NetFluid Service is stopping");
            ExternalHostManager.Stop();
        }
    }
}
