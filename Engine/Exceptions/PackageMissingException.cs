//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
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