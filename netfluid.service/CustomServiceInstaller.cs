using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;


namespace NetFluidService
{
    [RunInstaller(true)]
    public class CustomServiceInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public CustomServiceInstaller()
        {
            processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalService
            };
            serviceInstaller = new ServiceInstaller
            {
                ServiceName = "NetFluidService",
                StartType = ServiceStartMode.Automatic
            };
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        } 
    }
}
