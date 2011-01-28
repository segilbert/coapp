using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class PackageIsNotInstalledException : Exception {
        public Package Package;

        public PackageIsNotInstalledException(Package p ) {
            Package = p;
        }
    }
}
