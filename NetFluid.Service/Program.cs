using NetFluid;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace NetFluidService
{
    class Program : ServiceBase
    {
        public static string InstallServiceName = "NetFuidService";

        static void Main(string[] args)
        {
            bool debugMode = false;
            if (args.Length > 0)
            {
                for (int ii = 0; ii < args.Length; ii++)
                {
                    switch (args[ii].ToUpper())
                    {
                        case "/NAME":
                            if (args.Length > ii + 1)
                            {
                                InstallServiceName = args[++ii];
                            }
                            break;
                        case "/I":
                            InstallService();
                            return;
                        case "/U":
                            UninstallService();
                            return;
                        case "/D":
                            debugMode = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (debugMode)
            {
                Program service = new Program();
                service.OnStart(null);
                Console.WriteLine("Service Started...");
                Console.WriteLine("<press any key to exit...>");
                Console.Read();
            }
            else
            {
                System.ServiceProcess.ServiceBase.Run(new Program());
            }
        }

        protected override void OnStart(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            
            Engine.Interfaces.AddAllAddresses(80);
            Engine.AddPublicFolder("/", "./Public", true);
            Engine.Interfaces.AddInterface("127.0.0.1", 80);

            if (!Directory.Exists("./Trusted"))
                Directory.CreateDirectory("./Trusted");

            foreach (var webapp in Directory.GetDirectories("./Trusted"))
            {
                var host = webapp.Split(Path.DirectorySeparatorChar).Last();

                foreach (var dll in Directory.GetFiles(webapp,"*.dll",SearchOption.AllDirectories))
                {
                    Engine.LoadHost(host, Assembly.LoadFile(dll));
                }
            }

            Engine.Start();
        }

        /// <SUMMARY>
        /// Stop this service.
        /// </SUMMARY>
        protected override void OnStop()
        {
            //stop any threads here and wait for them to be stopped.
        }

        protected override void Dispose(bool disposing)
        {

            //clean your resources if you have to
            base.Dispose(disposing);
        }

        private static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == InstallServiceName);
        }

        private static void InstallService()
        {
            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        } 
    }
}
