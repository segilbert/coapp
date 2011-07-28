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

    public enum InvalidReason {
        NotValidMSI,
        NotCoAppMSI,
        MalformedCoAppMSI,
    }

    public class InvalidPackageException : Exception {
        public string PackagePath;
        public InvalidReason Reason;

        public InvalidPackageException(InvalidReason reason, string packagePath) {
            PackagePath = packagePath;
            Reason = reason;
        }
    }
}