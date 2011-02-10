//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;

    public class PackageNotInstalledException : Exception {
        public Package NotInstalledPackage;

        public PackageNotInstalledException(Package package) {
            NotInstalledPackage = package;
        }
    }
}