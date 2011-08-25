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
    using System.Collections.Generic;
    using Tasks;

#if COAPP_ENGINE_CLIENT 
    using Client;
#endif 

    public class PackageManagerMessages : MessageHandlers<PackageManagerMessages> {
#if COAPP_ENGINE_CORE 
        public Action<Package, IEnumerable<Package>> PackageInformation;
#else
        public Action<Package> PackageInformation;
#endif
        public Action<Package> PackageDetails;
        public Action NoPackagesFound;
        public Action<string, DateTime, bool, bool, bool> FeedDetails;
        public Action<string, int> ScanningPackagesProgress;
        public Action<string, int> InstallingPackageProgress;
        public Action<string, int> RemovingPackageProgress;
        public Action<string> InstalledPackage;
        public Action<string> RemovedPackage;
        public Action<string,string,string> FailedPackageInstall;
        public Action<string,string> FailedPackageRemoval;
        public Action<string, IEnumerable<string>, string, bool> RequireRemoteFile;
        public Action<string, bool, string> SignatureValidation;
        public Action<string> PermissionRequired;
        public Action<string, string, string > Error;
        public Action<string, string, string> Warning;
        public Action<string> FeedAdded;
        public Action<string> FeedRemoved;
        public Action<string> FeedSuppressed;
        public Action NoFeedsFound;
        public Action<string> FileNotFound;
        public Action<string> UnknownPackage;
        public Action<string> PackageBlocked;
        public Action<string,string> FileNotRecognized;
        public Action<string> Recognized;
        public Action<string> OperationCancelled;
        public Action<Exception> UnexpectedFailure;
        public Action<Package, IEnumerable<Package>> PackageHasPotentialUpgrades;
        public Action<Package> UnableToDownloadPackage;
        public Action<Package> UnableToInstallPackage;
        public Action<Package, IEnumerable<Package>> UnableToResolveDependencies;

        public string RequestId;
    }
}