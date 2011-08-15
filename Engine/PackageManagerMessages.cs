namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using Network;
    using Tasks;

#if COAPP_ENGINE_CLIENT 
    using Client;
#endif 
    /// <summary>
    /// interface for message delegates
    /// 
    /// Note: Probably being refactored.
    /// </summary>
    internal class PackageManagerMessages : MessageHandlers<PackageManagerMessages> {
        public Action<string, IEnumerable<Package>> MultiplePackagesMatch;
        public Action<Package> PackageRemoveFailed;
        public Action<string> PackageNotFound;
        public Action<Package> PackageIsNotInstalled;

        public Action<Package> RemovingPackage;
        public Action<Package, int> RemovingProgress;

        public Action<Package> InstallingPackage;
        public Action<Package, int> InstallProgress;

        public Action<int> PackageScanning;
        public Action<Package> FailedDependentPackageInstall;
        public Action<RemoteFile> DownloadingFile;
        public Action<RemoteFile, long> DownloadingFileProgress;
        public Action<Package> PackageNotSatisfied;
        public Action<Package, IEnumerable<Package>> PackageHasPotentialUpgrades;
        public Action<IEnumerable<Package>> UpgradingPackage;
    }

#if COAPP_ENGINE_CORE

    internal class NewPackageManagerMessages : MessageHandlers<NewPackageManagerMessages> {
        public Action<Package> PackageInformation;
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
        
        public Action<IEnumerable<string>, string, bool> RequireRemoteFile;
        public Action<string, bool, string> SignatureValidation;
        public Action<string, string> PermissionRequired;
        public Action<string, string,string > ArgumentError;

        public Action<string> FileNotFound;
        public Action<string> UnknownPackage;
        public Action<string> PackageBlocked;
        
        public Action<string,string> FileNotRecognized;

        public Action<Exception> UnexpectedFailure;
    }

    internal class PackageManagerSession : MessageHandlers<PackageManagerSession> {
        public Func<Package, PackageSessionData> GetPackageSessionData;
        public Func<Package, PackageManagerSessionData> GetPackageManagerSessionData;
        public Action<Package> DropPackageSessionData;
        public Func<PermissionPolicy, bool> CheckForPermission;
    }
#endif
}