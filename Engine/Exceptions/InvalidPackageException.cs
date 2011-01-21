//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;

    public enum InvalidReason {
        NotValidMSI,
        NotCoAppMSI,
        MalformedCoAppMSI,
    }

    public class InvalidPackageException : Exception {
        public string PackagePath;

        public InvalidPackageException(InvalidReason reason, string packagePath) {
            PackagePath = packagePath;
        }
    }
}