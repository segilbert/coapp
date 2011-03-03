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
    using System.Threading;
    using System.Threading.Tasks;
    using Exceptions;
    using Extensions;
    using Feeds;
    using Feeds.Atom;
    using Network;
    using PackageFormatHandlers;
    using Tasks;


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

    }


    public class PackageManager {
        private readonly List<Package> _acquirePackageQueue = new List<Package>();
        private readonly List<Package> _installQueue = new List<Package>();

        public CancellationToken CancellationToken {
            get { return CoTask.CurrentCancellationToken; }
        }

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

        public void AddSystemFeeds(IEnumerable<string> feedLocations) {
            Registrar.AddSystemFeedLocations(feedLocations);
        }

        public void DeleteSystemFeeds(IEnumerable<string> feedLocations) {
            Registrar.DeleteSystemFeedLocations(feedLocations);
        }

        public CoTask InstallPackages(IEnumerable<string> packages, MessageHandlers messageHandlers) {
            return CoTask.Factory.StartNew(() => InstallPackagesImpl(packages), CancellationToken, messageHandlers);
        }

        private void InstallPackagesImpl(IEnumerable<string> packages) {
            if (CancellationToken.IsCancellationRequested) {
                return;
            }

            var packageFiles = Registrar.GetPackagesByName(packages);

            foreach (var p in packageFiles) {
                p.UserSpecified = true;
            }

            foreach (var p in from p in packageFiles from pas in PackagesAsSpecified where p.CosmeticName.IsWildcardMatch(pas) select p) {
                p.DoNotSupercede = true;
            }

            foreach (var p in from p in packageFiles from pas in PackagesAreUpgradable where p.CosmeticName.IsWildcardMatch(pas) select p) {
                p.UpgradeAsNeeded = true;
            }
            // must wait for child tasks to complete any scanning work
            // TODO: fix HACK: using task.wait() instead of continuewith() 
            // HACK HACK HACK HACK HACK HACK HACK 
            foreach (var t in CoTask.CurrentTask.ChildTasks.ToList()) {
                if (!t.IsCompleted) {
                    t.Wait();
                    Thread.Sleep(100); // this should give any continuations time to start
                }
            }
            // HACK HACK HACK HACK HACK HACK HACK 

            int state;
            GetInstalledPackages();

            do {
                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                state = Registrar.StateCounter;
                _acquirePackageQueue.Clear();
                _installQueue.Clear();

                // must wait for child tasks to complete any scanning work
                // TODO: fix HACK: using task.wait() instead of continuewith() 
                // HACK HACK HACK HACK HACK HACK HACK 
                foreach (var t in CoTask.CurrentTask.ChildTasks.ToList()) {
                    if (!t.IsCompleted) {
                        t.Wait();
                        Thread.Sleep(100); // this should give any continuations time to finish
                    }
                }
                // HACK HACK HACK HACK HACK HACK HACK 

                Registrar.ScanForPackages(packageFiles);

                foreach (var eachPackageFile in packageFiles) {
                    try {
                        if (CanSatisfyPackage(eachPackageFile)) {
                            continue;
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
                        Registrar.ScanForPackages(packageFiles);
                        break;
                    }
                    // so, no scanning has been done since it wasn't likely to find anything new
                    // and we can't satisfy this package. Bummer.
                    // throw new PackageNotSatisfiedException(eachPackageFile);
                    PackageManagerMessages.Invoke.PackageNotSatisfied(eachPackageFile);
                }

                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                // if we're done here, make sure that any changes to the state are accounted for
                if (Registrar.StateCounter != state) {
                    continue;
                }

                if (_acquirePackageQueue.Count > 0) {
                    // ensure packages are local

                    // download what we don't have.
                    // pretendToDownloadTheFiles(); // uh, we don't have any download mechanism right now.

                    // after downloading is finished, check to see if anything changed
                    if (Registrar.StateCounter != state) {
                        continue;
                    }

                    // if we still have packages that we can't get
                    if (_acquirePackageQueue.Count > 0) {
                        foreach (var notAvailablePackage in _acquirePackageQueue) {
                            notAvailablePackage.CouldNotDownload = true;
                        }
                    }

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state) {
                        continue;
                    }
                }

                if (_installQueue.Count > 0) {
                    foreach (var pkg in _installQueue) {
                        try {
                            if (CancellationToken.IsCancellationRequested) {
                                return;
                            }

                            if (!pkg.IsInstalled) {
                                PackageManagerMessages.Invoke.InstallingPackage(pkg);

                                if (!Pretend) {
                                    pkg.Install((percentage) => {
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
        }

        private bool CanSatisfyPackage(Package packageToSatisfy) {
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
                            if (CanSatisfyPackage(supercedent)) {
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
                if (!CanSatisfyPackage(dependentPackage)) {
                    return false;
                }
            }

            if (!packageToSatisfy.HasLocalFile) {
                _acquirePackageQueue.Add(packageToSatisfy);
            }
            else {
                _installQueue.Add(packageToSatisfy);
            }

            return packageToSatisfy.CanSatisfy = true;
        }

        public CoTask RemovePackages(IEnumerable<string> packages, MessageHandlers messageHandlers = null) {
            // scan 

            return CoTask.Factory.StartNew(() => {

                GetInstalledPackages().Wait();

                // this is going to be too aggressive I think...
                var packageFiles = Registrar.GetInstalledPackagesByName(packages);

                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                foreach (var p in packageFiles) {
                    if (CancellationToken.IsCancellationRequested) {
                        return;
                    }

                    if (!p.IsInstalled) {
                        throw new PackageIsNotInstalledException(p);
                    }
                }

                foreach (var p in packageFiles) {
                    if (CancellationToken.IsCancellationRequested) {
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

        public CoTask<IEnumerable<Package>> GetInstalledPackages(MessageHandlers messageHandlers = null) {
            return CoTask.Factory.StartNew<IEnumerable<Package>>(() => { 
                MSIBase.ScanInstalledMSIs();
                Registrar.SaveCache();
                return Registrar.InstalledPackages;
            },messageHandlers);
        }

        public void GenerateAtomFeed(string outputFilename, string packageSource, bool recursive,  string rootUrl, string packageUrl, string actualUrl = null, string title = null) {
            outputFilename = Path.GetFullPath(outputFilename);
            PackageFeed.GetPackageFeedFromLocation(packageSource,recursive).ContinueWith(antecedent => {
                var packageFeed = antecedent.Result;

                AtomFeed generatedFeed = new AtomFeed(outputFilename, rootUrl, packageUrl, actualUrl, title);

                foreach (var pkg in packageFeed.FindPackages("*")) {
                    generatedFeed.AddPackage(pkg, packageFeed.Location.RelativePathTo(pkg.LocalPackagePath));
                }
                generatedFeed.Save(outputFilename);

            }).Wait();
        }
    }
}