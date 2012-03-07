//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.IO;

namespace CoApp.Service {
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.ServiceProcess;
    using Toolkit.Engine;

    [RunInstaller(true)]
    public class CoAppServiceInstaller : Installer {
        
        private readonly ServiceProcessInstaller _serviceProcessInstaller = new ServiceProcessInstaller();
        private readonly ServiceInstaller _serviceInstaller = new ServiceInstaller();

        public CoAppServiceInstaller() : this(false) {
            System.Environment.CurrentDirectory = System.Environment.GetEnvironmentVariable("tmp") ?? Path.Combine(System.Environment.GetEnvironmentVariable("systemroot"),"temp");
        }

        public CoAppServiceInstaller(bool useUserAccount) {
            _serviceProcessInstaller.Account = useUserAccount ? ServiceAccount.User : ServiceAccount.LocalSystem;
            _serviceProcessInstaller.Password = null;
            _serviceProcessInstaller.Username = null;

            _serviceInstaller.ServiceName = EngineServiceManager.CoAppServiceName;
            _serviceInstaller.DisplayName = EngineServiceManager.CoAppDisplayName;

            _serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.AddRange(new Installer[] {_serviceProcessInstaller,_serviceInstaller});
        }
    }
}