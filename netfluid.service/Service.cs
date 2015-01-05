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
                        StartNetFluid();
                        Console.ReadLine();
                        return;
                    case "install":
                        InstallService();
                        return;
                    case "uninstall":
                        UninstallService();
                        return;
                    case "start":
                        var service = new ServiceController("NetFluidService");
                        service.Start();
                        return;
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

        static void StartNetFluid()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(location);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            foreach (var dir in Directory.GetDirectories("./applications"))
            {
                var name = dir.Split(Path.DirectorySeparatorChar).Last();
                var path = Path.Combine(dir, name + ".dll");
                path = File.Exists(path) ? path : Path.Combine(dir, name + ".exe");

                if(File.Exists(path))
                {
                    Engine.Load(Assembly.LoadFile(Path.GetFullPath(path)));
                }
            }

            Engine.ShowException = true;

            Engine.Load(typeof(DefaultExposer).Assembly);
            Engine.Interfaces.AddAllAddresses();
            Engine.DefaultHost.PublicFolders = new DefaultPublicFolderManager()
            {
                new PublicFolder{ RealPath= "./public", VirtualPath="/public"}
            };

            Engine.Interfaces.AddInterface("127.0.0.1", 80);
            Engine.Start();
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dir = Path.GetDirectoryName(args.RequestingAssembly.Location);
            Engine.Logger.Log("Loading "+args.Name);
            foreach(var dll in Directory.GetFiles(dir, "*.dll"))
            {
                try
                {
                   var loaded= Assembly.LoadFile(dll);
                   if (loaded.FullName == args.Name)
                       return loaded;
                }
                catch (Exception)
                {
                }
            }

            Engine.Logger.Log("Failed to found " + args.Name);
            return null;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                StartNetFluid();
            }
            catch (Exception ex)
            {
                Engine.Logger.Log("Error starting up the service",ex);
            }

        }

        protected override void OnStop()
        {
            Engine.Logger.Log("NetFluid Service is stopping");
        }
    }
}
