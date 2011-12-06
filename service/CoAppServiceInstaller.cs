//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Service {
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.ServiceProcess;
    using Toolkit.Engine;

    [RunInstaller(true)]
    public class CoAppServiceInstaller : Installer {
        
        public ServiceProcessInstaller ServiceProcessInstaller = new ServiceProcessInstaller();
        public ServiceInstaller ServiceInstaller = new ServiceInstaller();

        public CoAppServiceInstaller() : this(false) {
        }

        public CoAppServiceInstaller(bool useUserAccount) {
            ServiceProcessInstaller.Account = useUserAccount ? ServiceAccount.User : ServiceAccount.LocalSystem;
            ServiceProcessInstaller.Password = null;
            ServiceProcessInstaller.Username = null;

            ServiceInstaller.ServiceName = EngineServiceManager.CoAppServiceName;
            ServiceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.AddRange(new Installer[] {ServiceProcessInstaller,ServiceInstaller});
        }
    }
}