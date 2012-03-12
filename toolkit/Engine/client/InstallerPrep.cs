namespace CoApp.Toolkit.Engine.Client {
    using System;
    using Logging;

    public class InstallerPrep : MarshalByRefObject, IComparable {
        public InstallerPrep() {
            try {
                PackageManager.Instance.Connect("Bootstrapper");
            } catch(Exception e) {
                Logger.Error(e);
            }
        }

        public int CompareTo(object obj) {
            if (EngineServiceManager.Available) {
                return 100;
            }
            if( EngineServiceManager.ShuttingDown) {
                return -1;
            }
            return EngineServiceManager.EngineStartupStatus;
        }
    }
}
