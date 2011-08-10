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

    public class PackagesNotAvailableException : Exception {
        public IEnumerable<Package> Packages;

        public PackagesNotAvailableException(IEnumerable<Package> pkgs) {
            Packages = pkgs;
        }
    }
}