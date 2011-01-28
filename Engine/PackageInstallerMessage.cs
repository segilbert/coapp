//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    public enum PackageInstallerMessage {
        NoticeCanSatisfyPackage,
        NoticeAcquiringPackages,
        NoticeInstallingPackages,

        FailedDependentPackageInstall,
        FoundUnconnfirmedUpgradePackage,
        IdentifyingDependents,
        Installing,
        InstallProgress,
        Scanning,
        Removing,
        RemoveProgress
    };
}
