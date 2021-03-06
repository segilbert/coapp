//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using Tasks;

    internal class PackageManagerSession : MessageHandlers<PackageManagerSession> {
        public Func<PermissionPolicy, bool> CheckForPermission;
        public Func<bool> CancellationRequested;
        public Func<string, string> GetCanonicalizedPath;
    }
}