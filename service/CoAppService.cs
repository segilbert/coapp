//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Service {
    using System.ServiceProcess;
    using Toolkit.Engine;

    public class CoAppService : ServiceBase {
        public static bool IsInstalled {
            get {
                return EngineServiceManager.IsServiceInstalled;
            }
        }

        public CoAppService() {
            ServiceName = EngineServiceManager.CoAppServiceName;
        }

        public static void StartService() {
            EngineServiceManager.TryToStartService();
        }

        public static void StopService() {
            EngineServiceManager.TryToStopService();
        }

        public static bool IsRunning {
            get {
                return EngineServiceManager.IsServiceRunning;
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