//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;
    using System.Collections.Generic;

    public class MultiplePackagesMatchException : Exception {
        public string PackageMask;
        public IEnumerable<Package> PackageMatches;

        public MultiplePackagesMatchException(string packageMask, IEnumerable<Package> packageMatch) {
            PackageMask = packageMask;
            PackageMatches = packageMatch;
        }
    }
}