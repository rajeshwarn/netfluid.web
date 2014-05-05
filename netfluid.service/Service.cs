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
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            #region LOAD CONFIGURATION
            if (!Engine.LoadAppConfiguration())
            {
                Engine.Interfaces.AddAllAddresses();
                Engine.AddPublicFolder("/", "./Public", true);
                Engine.Interfaces.AddInterface("127.0.0.1", 80);
                Engine.Interfaces.AddInterface("127.0.0.1", 8000);
            }
            #endregion

            if (!Directory.Exists("./Hosting"))
                Directory.CreateDirectory("./Hosting");

            Engine.Start();

            //Engine.SetController(x => new FluidTemplate("./UI/index.html"),"Update in progress");
        }


        protected override void OnStop()
        {
            Engine.Logger.Log(LogLevel.Warning,"NetFluid Service is stopping");
            HostRepository.Stop();
        }
    }
}
