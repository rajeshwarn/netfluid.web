using System;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;


namespace NetFluid.Service
{
    public class Service : ServiceBase
    {
        static void Main(string[] args)
        {
            if (args!=null && args.Length>=1)
            {
                switch (args[0])
                {
                    case "debug":
                        (new Service()).OnStart(null);
                        Engine.DevMode = true;
                        Console.ReadLine();
                        break;
                    case "install":
                        InstallService();
                    break;
                    case "uninstall":
                        UninstallService();
                    break;
                    case "start":
                        var service = new ServiceController("NetFluidService");
                        service.Start();
                    break;
                }
            }
            Run(new Service());
        }

        public static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "NetFluidService");
        }

        public static void InstallService()
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
            var sec = dir.GetAccessControl();

            var networkService = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
            var localService = new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null);

            sec.AddAccessRule(new FileSystemAccessRule(networkService, FileSystemRights.FullControl, AccessControlType.Allow));
            sec.AddAccessRule(new FileSystemAccessRule(localService, FileSystemRights.FullControl, AccessControlType.Allow));

            dir.SetAccessControl(sec);

            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new[] { Path.GetFullPath("NetFluid.Service.exe") });
        }

        public static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/u", Path.GetFullPath("NetFluid.Service.exe") });
        } 

        protected override void OnStart(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                Engine.Interfaces.AddAllAddresses();
                Engine.DefaultHost.PublicFolders = new DefaultPublicFolderManager("./public");
                Engine.Interfaces.AddInterface("127.0.0.1", 80);
                Engine.Start();
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception,"Error starting up the service",ex);
            }

        }

        protected override void OnStop()
        {
            Engine.Logger.Log(LogLevel.Warning,"NetFluid Service is stopping");
        }
    }
}
