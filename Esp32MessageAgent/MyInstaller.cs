using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Esp32MessageAgent
{
    [RunInstaller(true)]
    public partial class MyInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public MyInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Set the account type
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Set the service properties
            serviceInstaller.ServiceName = "Esp32MessageAgent";
            serviceInstaller.DisplayName = "ESP32 Health Agent";
            serviceInstaller.Description = "A service that sends messages to ESP32 over serial USB to let ESP32 PC is on.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Add to Installers collection
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
