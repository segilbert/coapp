//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;
    using Toolkit.Exceptions;

    public class PackageNotFoundException : CoAppException {
        public string PackagePath;

        public PackageNotFoundException(string packagePath) {
            PackagePath = packagePath;
        }
    }
}