using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class MultiplePackagesMatchException : Exception {
        public string PackageMask;
        public IEnumerable<Package> PackageMatches;

        public MultiplePackagesMatchException(string packageMask, IEnumerable<Package> packageMatch) {
            PackageMask = packageMask; 
            PackageMatches = packageMatch;
        }
    }
}
