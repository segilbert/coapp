using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class PackageNotSatisfiedException : Exception {
        public Package packageNotSatified;
        public PackageNotSatisfiedException(Package p) {
            packageNotSatified = p;
        }
    }
}
