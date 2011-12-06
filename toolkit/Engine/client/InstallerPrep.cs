using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using System.Threading.Tasks;
    using Logging;

    public class InstallerPrep : MarshalByRefObject, IComparable {
        public InstallerPrep() {
            try {
                // worst case scenario 
                PackageManager.Instance.ConnectAndWait("PackageInstaller", null, 25000);

                PackageManager.Instance.GetEngineStatus(new PackageManagerMessages {
                    UnexpectedFailure = (ex) => { _currentPercent = -2; },
                    Error = (s1, s2, s3) => { _currentPercent = -3; },
                    OperationCancelled = (s1) => { _currentPercent = -4; },
                    EngineStatus = (percentComplete) => { _currentPercent = percentComplete; }
                }).ContinueWith((antecedent) => { _currentPercent = 100; }, TaskContinuationOptions.AttachedToParent);
            } catch(Exception e) {
                Logger.Error(e);
                _currentPercent = -1;
            }
        }

        private int _currentPercent;
        public int CompareTo(object obj) {
            return _currentPercent;
        }
    }
}
