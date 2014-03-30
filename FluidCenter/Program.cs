using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows.Forms;

namespace FluidCenter
{
    class Program
    {
        static ServiceController service;

        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        public static void Start()
        {
            if (service==null)
            {
                service = new ServiceController("NetFluidService");
            }
            service.Start();
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

            sec.AddAccessRule(new FileSystemAccessRule(networkService,FileSystemRights.FullControl, AccessControlType.Allow));
            sec.AddAccessRule(new FileSystemAccessRule(localService, FileSystemRights.FullControl, AccessControlType.Allow));

            dir.SetAccessControl(sec);

            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new [] { Path.GetFullPath("NetFluid.Service.exe") });
        }

        public static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/u", Path.GetFullPath("NetFluid.Service.exe") });
        } 
    }
}
