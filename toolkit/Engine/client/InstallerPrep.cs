using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using Logging;

    public class InstallerPrep : MarshalByRefObject, IComparable {
        public InstallerPrep() {
            try {
                // worst case scenario 
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
