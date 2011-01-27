using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class PackageRemoveFailedException : Exception {
        public Package FailedPackage;

        public PackageRemoveFailedException(Package failedPackage) {
            FailedPackage = failedPackage;
        }
    }
}
