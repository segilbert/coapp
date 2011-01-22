//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;
    using CoApp.Toolkit.Extensions;

    public class PackageNotFoundException : Exception {
        public string PackagePath;
        public PackageNotFoundException(string packagePath) {
            PackagePath = packagePath;
        }
    }
}
