//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Exceptions;
    using Extensions;
    using Feeds;
    using Feeds.Atom;
    using Network;
    using PackageFormatHandlers;
    using Tasks;
    using OperationCompletedBeforeResultException = Tasks.OperationCompletedBeforeResultException;


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

    public class PackageManager {
        // private readonly List<Package> _acquirePackageQueue = new List<Package>();
        // private readonly List<Package> _installQueue = new List<Package>();
        private static readonly TransferManager _transferManager = TransferManager.GetTransferManager(PackageManagerSettings.CoAppCacheDirectory);

        private bool IsCancellationRequested { get { return Tasklet.CurrentCancellationToken.IsCancellationRequested; } }

        public IEnumerable<string> PackagesAreUpgradable = Enumerable.Empty<string>();
        public IEnumerable<string> PackagesAsSpecified = Enumerable.Empty<string>();

        public PackageManager() {
            MaximumPackagesToProcess = 10;
            Registrar.LoadCache();
        }

        public bool Pretend { get; set; }
        public int MaximumPackagesToProcess { get; set; }

        public IEnumerable<string> DoNotScanLocations {
            get { return Registrar.DoNotScanLocations; }
            set { Registrar.DoNotScanLocations.AddRange(value); }
        }

        public IEnumerable<string> SessionFeedLocations {
            get { return Registrar.SessionFeedLocations; }
            set { Registrar.AddSessionFeedLocations(value); }
        }

        public IEnumerable<string> SystemFeedLocations {
            get { return Registrar.SystemFeedLocations; }
        }

        public void FlushCache() {
            Registrar.FlushCache();
        }

        public Task AddSystemFeeds(IEnumerable<string> feedLocations, MessageHandlers messageHandlers = null) {
            return Registrar.AddSystemFeedLocations(feedLocations);
        }

        public Task DeleteSystemFeeds(IEnumerable<string> feedLocations, MessageHandlers messageHandlers = null) {
            return Registrar.DeleteSystemFeedLocations(feedLocations);
        }

        public Task InstallPackages(IEnumerable<string> packageMasks, MessageHandlers messageHandlers = null) {
            return Registrar.GetPackagesByName(packageMasks, messageHandlers).ContinueWithParent(antecedent => {
                foreach (var p in antecedent.Result) {
                    p.UserSpecified = true;
                }
                return antecedent.Result;
            }).ContinueWithParent(antecedent => { InstallPackages(antecedent.Result); });
        }

        public Task InstallPackages(IEnumerable<Package> packages, MessageHandlers messageHandlers = null) {
            return CoTask.Factory.StartNew(() => {
                foreach (var p in from p in packages from pas in PackagesAsSpecified where p.CosmeticName.IsWildcardMatch(pas) select p) {
                    p.DoNotSupercede = true;
                }

                foreach (
                    var p in from p in packages from pas in PackagesAreUpgradable where p.CosmeticName.IsWildcardMatch(pas) select p) {
                    p.UpgradeAsNeeded = true;
                }

                int state;
                GetInstalledPackages();
                var downloadQueue = new List<Package>();
                var installQueue = new List<Package>();

                do {
                    if (IsCancellationRequested) {
                        return;
                    }

                    state = Registrar.StateCounter;
                    downloadQueue.Clear();
                    installQueue.Clear();

                    Tasklet.WaitforCurrentChildTasks(); // HACK HACK HACK ???

                    Registrar.ScanForPackages(packages);

                    foreach (var eachPackage in packages) {
                        try {
                            if (CanSatisfyPackage(eachPackage, installQueue, downloadQueue)) {
                                break;
                            }
                        }
                        catch (PackageHasPotentialUpgradesException) {
                            // if this throws, this is a signal to gtfo. (get-the-function-out) ... 
                            // ... what did you think gtfo meant?
                            throw;
                        }

                        if (Registrar.StateCounter != state) {
                            // if stuff has changed, perhaps the registrar should scan again, 
                            // just in case it can find anything new
                            Registrar.ScanForPackages(packages);
                            break;
                        }
                        // so, no scanning has been done since it wasn't likely to find anything new
                        // and we can't satisfy this package. Bummer.
                        // throw new PackageNotSatisfiedException(eachPackageFile);
                        PackageManagerMessages.Invoke.PackageNotSatisfied(eachPackage);
                        throw new OperationCompletedBeforeResultException();
                    }

                    if (IsCancellationRequested) {
                        return;
                    }

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state) {
                        continue;
                    }

                    if(!DownloadPackages(downloadQueue))
                        continue;

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state) {
                        continue;
                    }

                    if( downloadQueue.Count > 0 ) {
                        // we've got packages that should have been downloaded, but seem to have not been
                        // BAD. GS01: TODO look into this if it occurs.
                        throw new Exception("SHOULD NOT HAPPEN: DownloadQueue is not empty");
                    }

                    if (installQueue.Count > 0) {
                        foreach (var pkg in installQueue) {
                            try {
                                if (IsCancellationRequested) {
                                    return;
                                }

                                if (!pkg.IsInstalled) {
                                    PackageManagerMessages.Invoke.InstallingPackage(pkg);

                                    if (!Pretend) {
                                        pkg.Install(percentage => {
                                            PackageManagerMessages.Invoke.InstallProgress(pkg, percentage);
                                        });
                                        PackageManagerMessages.Invoke.InstallProgress(pkg, 100);
                                    }
                                    pkg.IsInstalled = true;
                                }
                            }
                            catch (PackageInstallFailedException) {

                                if (!pkg.AllowedToSupercede) {
                                    throw; // user specified packge as critical.
                                }

                                PackageManagerMessages.Invoke.FailedDependentPackageInstall(pkg);
                                pkg.PackageFailedInstall = true;
                                GetInstalledPackages();
                                break; // let it try to find another package.
                            }
                        }
                        // ...

                        // if we're done here, make sure that any changes to the state are accounted for
                        if (Registrar.StateCounter != state) {
                            continue;
                        }
                    }
                    // try to find them
                } while (Registrar.StateCounter != state);
            }, messageHandlers);
        }

        private bool DownloadPackages(IEnumerable<Package> packages) {
            if (packages.Count() > 0) {
                // ensure packages are local

                // download what we don't have.
                var transferTasks = new List<Task>();
                foreach(var package in packages) {
                    if (IsCancellationRequested)
                        return false;

                    if( package.HasLocalFile )
                        continue;
                        
                    if( package.RemoteLocation.Value == null ) {
                        package.CouldNotDownload = true;
                        continue;
                    }

                    var thisPkg = package;
                    // try preferred location
                    var remoteFile = _transferManager[package.RemoteLocation.Value];
                    var tsk = remoteFile.Get();
                    tsk.ContinueWithParent(antecedent => {
                        if (remoteFile.LastStatus != HttpStatusCode.OK) {
                            // failed download; check for other possible download locations.
                            // GS01: TODO IMPLEMENT OTHER LOCATIONS.
                            thisPkg.CouldNotDownload = true;
                            return;
                        }
                        if( remoteFile.IsLocal ) {
                            Registrar.GetPackage(remoteFile.LocalFullPath); // this should return the same instance of the package we have (updated with all the info :D)
                            thisPkg.LocalPackagePath.Value = remoteFile.LocalFullPath;
                        }
                        // otherwise, it worked nicely.
                        if (!thisPkg.HasLocalFile)
                            throw new Exception("SHOULD NOT HAPPEN: File completed download, but doesn't have a local file [{0}]??".format(remoteFile.ActualRemoteLocation.AbsoluteUri));
                        
                    });

                    remoteFile.DownloadProgress.Notification += progress => PackageManagerMessages.Invoke.DownloadingFileProgress(remoteFile, progress);
                }

                Tasklet.WaitforCurrentChildTasks(); // HACK HACK HACK ???

                if (packages.Any(package => package.CouldNotDownload)) {
                    return false;
                }                
            }
            return true;
        }

        private bool CanSatisfyPackage(Package packageToSatisfy, List<Package> installQueue, List<Package> downloadQueue) {
            packageToSatisfy.CanSatisfy = false;

            if (packageToSatisfy.IsInstalled) {
                return packageToSatisfy.CanSatisfy = true;
            }

            if (!packageToSatisfy.DoNotSupercede) {
                var supercedents = Registrar.InstalledPackages.SupercedentPackages(packageToSatisfy);
                if (supercedents.Count() > 0) {
                    return true; // a supercedent package is already installed.
                }
            }

            if (!packageToSatisfy.DoNotSupercede) {
                // if told not to supercede, we won't even perform this check 
                packageToSatisfy.Supercedent = null;
                var supercedents = Registrar.Packages.SupercedentPackages(packageToSatisfy);

                if (supercedents.Count() > 0) {
                    if (packageToSatisfy.AllowedToSupercede) {
                        foreach (var supercedent in supercedents) {
                            if (CanSatisfyPackage(supercedent, installQueue, downloadQueue)) {
                                packageToSatisfy.Supercedent = supercedent;
                                break;
                            }
                        }
                        // if we have a supercedent, then this package's dependents are moot.)
                        if (packageToSatisfy.Supercedent != null) {
                            return packageToSatisfy.CanSatisfy = true;
                        }
                    }
                    else {
                        // the user hasn't specifically asked us to supercede, yet we know of 
                        // potential supercedents. Let's force the user to make a decision.
                        // throw new PackageHasPotentialUpgradesException(packageToSatisfy, supercedents);
                        PackageManagerMessages.Invoke.PackageHasPotentialUpgrades(packageToSatisfy, supercedents);
                        throw new OperationCompletedBeforeResultException();
                    }
                }
            }

            if (packageToSatisfy.CouldNotDownload || packageToSatisfy.PackageFailedInstall) {
                return false;
            }

            foreach (var dependentPackage in packageToSatisfy.Dependencies) {
                if (!CanSatisfyPackage(dependentPackage, installQueue, downloadQueue)) {
                    return false;
                }
            }

            if (!packageToSatisfy.HasLocalFile) {
                if (downloadQueue != null) {
                    downloadQueue.Add(packageToSatisfy);
                }
            }
            else {
                if (installQueue != null) {
                    installQueue.Add(packageToSatisfy);
                }
            }

            return packageToSatisfy.CanSatisfy = true;
        }

        public Task RemovePackages(IEnumerable<string> packages, MessageHandlers messageHandlers = null) {
            // scan 
            return CoTask.Factory.StartNew(() => {

                GetInstalledPackages().Wait();

                // this is going to be too aggressive I think...
                var packageFiles = Registrar.GetInstalledPackagesByName(packages);

                if (IsCancellationRequested) {
                    return;
                }

                foreach (var p in packageFiles) {
                    if (IsCancellationRequested) {
                        return;
                    }

                    if (!p.IsInstalled) {
                        throw new PackageIsNotInstalledException(p);
                    }
                }

                foreach (var p in packageFiles) {
                    if (IsCancellationRequested) {
                        return;
                    }

                    PackageManagerMessages.Invoke.RemovingPackage(p);
                    if (!Pretend) {
                        p.Remove((percentage) => {
                            PackageManagerMessages.Invoke.RemovingProgress(p, percentage);
                        });
                    }
                    PackageManagerMessages.Invoke.RemovingProgress(p, 100);
                }
            },messageHandlers);
        }

        public Task Upgrade(IEnumerable<string> packageList, MessageHandlers messageHandlers = null) {
            if( packageList == null || packageList.Count() == 0 ) {
                packageList = new[] {"*"};
            }
            return CoTask.Factory.StartNew(() => {
                GetInstalledPackages().ContinueWithParent((antecedent) => {

                    var installedPackages = antecedent.Result;
                    var newPackages = new List<Package>();

                    foreach (var pkg in installedPackages) {
                        foreach (var supercedents in from packageMask in packageList
                            where packageMask.Equals("all", StringComparison.CurrentCultureIgnoreCase) || pkg.CosmeticName.IsWildcardMatch(packageMask)
                            select Registrar.Packages.SupercedentPackages(pkg)) {
                            if (supercedents.Count() > 0) {
                                foreach (var supercedent in supercedents.Where(supercedent => CanSatisfyPackage(supercedent, null, null))) {
                                    if (!supercedent.IsInstalled) {
                                        newPackages.Add(supercedent);
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    PackageManagerMessages.Invoke.UpgradingPackage(newPackages);
                    InstallPackages(newPackages).Wait();
                });
            },messageHandlers);
        }

        public Task<IEnumerable<Package>> GetInstalledPackages(MessageHandlers messageHandlers = null) {
            return CoTask.Factory.StartNew<IEnumerable<Package>>(() => { 
                MSIBase.ScanInstalledMSIs();
                Registrar.SaveCache();
                return Registrar.InstalledPackages;
            },messageHandlers);
        }

        public Task<IEnumerable<Package>> GetPackagesInScanLocations(MessageHandlers messageHandlers = null)
        {
            return CoTask.Factory.StartNew<IEnumerable<Package>>(() =>
            {
                MSIBase.ScanInstalledMSIs();
                Registrar.SaveCache();
                return Registrar.Packages.Union(Registrar.ScanForPackages("*"));
            }, messageHandlers);
        }


        public void GenerateAtomFeed(string outputFilename, string packageSource, bool recursive,  string rootUrl, string packageUrl, string actualUrl = null, string title = null) {
            outputFilename = Path.GetFullPath(outputFilename);
            PackageFeed.GetPackageFeedFromLocation(packageSource, recursive).ContinueWithParent(antecedent => {
                var packageFeed = antecedent.Result;

                var generatedFeed = new AtomFeed(outputFilename, rootUrl, packageUrl, actualUrl, title);

                foreach (var pkg in packageFeed.FindPackages("*")) {
                    generatedFeed.AddPackage(pkg, packageFeed.Location.RelativePathTo(pkg.LocalPackagePath));
                }
                generatedFeed.Save(outputFilename);

            }).Wait();
        }
    }
}