using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class PackageInstallFailedException : Exception {
        public Package FailedPackage;

        public PackageInstallFailedException(Package failedPackage) {
            FailedPackage = failedPackage;
        }
    }
}
