namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Network;
    using Tasks;

#if COAPP_ENGINE_CLIENT 
    using Client;
#endif 


#if COAPP_ENGINE_CORE

    public class NewPackageManagerMessages : MessageHandlers<NewPackageManagerMessages> {
        public Action<Package, IEnumerable<Package>> PackageInformation;
        public Action<Package> PackageDetails;
        public Action NoPackagesFound;
        public Action<string, DateTime, bool> FeedDetails;
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
    }

    internal class PackageManagerSession : MessageHandlers<PackageManagerSession> {
        // public Func<Package, PackageSessionData> GetPackageSessionData;
        // public Func<Package, PackageManagerSessionData> GetPackageManagerSessionData;
        // public Action<Package> DropPackageSessionData;
        public Func<PermissionPolicy, bool> CheckForPermission;
        public Func<bool> CancellationRequested;
        public Func<string, string> GetCanonicalizedPath;
    }
#endif
}