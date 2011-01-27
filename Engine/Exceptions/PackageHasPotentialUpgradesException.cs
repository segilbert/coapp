using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class PackageHasPotentialUpgradesException : Exception {
        public Package UnsatisfiedPackage;
        public IEnumerable<Package> SatifactionOptions;

        public PackageHasPotentialUpgradesException(Package unsatisfiedPackage, IEnumerable<Package> satisfactionOptions) {
            UnsatisfiedPackage = unsatisfiedPackage;
            SatifactionOptions = satisfactionOptions;
        }
    }
}
