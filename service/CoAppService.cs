//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Service {
    using System;
    using System.Linq;
    using System.ServiceProcess;
    using System.Threading.Tasks;
    using Toolkit.Engine;

    public class CoAppService : ServiceBase {
        public const string CoAppServiceName = "CoApp Package Installer Service";

        public static bool IsInstalled {
            get {
                return ServiceController.GetServices().Any(service => service.ServiceName == CoAppServiceName);
            }
        }

        protected static Lazy<ServiceController> Controller = new Lazy<ServiceController>(() => new ServiceController(CoAppServiceName)); 

        public CoAppService() {
            ServiceName = CoAppServiceName;
        }

        public static void StartService() {
            if (!IsInstalled)
                throw new Exception("Service is not installed");

            if (Controller.Value.Status == ServiceControllerStatus.Stopped) {
                Controller.Value.Start();
            }
        }

        public static void StopService() {
            if(!IsInstalled) 
                throw new Exception("Service is not installed");

            if (Controller.Value.Status == ServiceControllerStatus.Running && Controller.Value.CanStop) {
                Controller.Value.Stop();
            }
        }

        public static bool IsRunning {
            get {
                return IsInstalled ? Controller.Value.Status == ServiceControllerStatus.Running : false;
            }
        }

        protected override void OnStart(string[] args) {
            EngineService.Start();
        }

        protected override void OnStop() {
            EngineService.Stop();
        }
    }
}