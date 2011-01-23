using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class PackagesNotAvailableException : Exception {
        public IEnumerable<Package> Packages;

        public PackagesNotAvailableException(IEnumerable<Package> pkgs) {
            Packages = pkgs;
        }
    }
}
