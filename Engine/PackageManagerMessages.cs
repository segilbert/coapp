namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using Network;
    using Tasks;

#if COAPP_ENGINE_CLIENT 
    using Client;
#endif 

    public class PackageManagerMessages : MessageHandlers<PackageManagerMessages> {
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
}