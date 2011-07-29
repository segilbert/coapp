//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;

    internal class PackageMissingException : Exception {
        public string Arch { get; set; }
        public string PublicKeyToken { get; set; }
        public string Name { get; set; }
        public UInt64 Version { get; set; }

        public PackageMissingException(string name, string arch, UInt64 version, string publicKeyToken) {
            Name = name;
            Arch = arch;
            Version = version;
            PublicKeyToken = publicKeyToken;
        }
    }
}