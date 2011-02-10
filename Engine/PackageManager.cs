//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Exceptions;
    using Extensions;
    using PackageFormatHandlers;

    public class PackageManager {
        private readonly List<Package> _acquirePackageQueue = new List<Package>();
        private readonly List<Package> _installQueue = new List<Package>();
        public CancellationToken CancellationToken;
        public IEnumerable<string> PackagesAreUpgradable = Enumerable.Empty<string>();
        public IEnumerable<string> PackagesAsSpecified = Enumerable.Empty<string>();

        public PackageManager() {
            Maximum = 10;
            Registrar.LoadCache();
        }

        public PackageManager(CancellationToken token) : this() {
            CancellationToken = token;
        }

        public bool Pretend { get; set; }
        public int Maximum { get; set; }

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

        public void InstallPackages(IEnumerable<string> packages, Action<PackageInstallerMessage, Package, int> status = null) {
            status = status ?? ((pim, pkg, num) => { });

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

            int state;
            GetInstalledPackages(status);

            do {
                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                state = Registrar.StateCounter;
                _acquirePackageQueue.Clear();
                _installQueue.Clear();

                // identify any outstanding dependencies
                status(PackageInstallerMessage.NoticeCanSatisfyPackage, null, 0);

                Registrar.ScanForPackages(packageFiles);

                foreach (var eachPackageFile in packageFiles) {
                    try {
                        if (CanSatisfyPackage(eachPackageFile, status)) {
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
                    throw new PackageNotSatisfiedException(eachPackageFile);
                }

                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                // if we're done here, make sure that any changes to the state are accounted for
                if (Registrar.StateCounter != state) {
                    continue;
                }

                if (_acquirePackageQueue.Count > 0) {
                    status(PackageInstallerMessage.NoticeAcquiringPackages, null, 0);
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

                status(PackageInstallerMessage.NoticeInstallingPackages, null, 0);
                if (_installQueue.Count > 0) {
                    foreach (var pkg in _installQueue) {
                        try {
                            if (CancellationToken.IsCancellationRequested) {
                                return;
                            }

                            if (!pkg.IsInstalled) {
                                status(PackageInstallerMessage.Installing, pkg, 0);
                                if (!Pretend) {
                                    pkg.Install((percentage) => {
                                        status(PackageInstallerMessage.InstallProgress, pkg, percentage);
                                    });

                                    status(PackageInstallerMessage.InstallProgress, pkg, 100);
                                }
                                pkg.IsInstalled = true;
                            }
                        }
                        catch (PackageInstallFailedException) {
                            
                            if (!pkg.AllowedToSupercede) {
                                throw; // user specified packge as critical.
                            }
                            status(PackageInstallerMessage.FailedDependentPackageInstall, pkg, 0);
                            pkg.PackageFailedInstall = true;
                            GetInstalledPackages(status);
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

        private bool CanSatisfyPackage(Package packageToSatisfy, Action<PackageInstallerMessage, Package, int> status) {
            status(PackageInstallerMessage.NoticeCanSatisfyPackage, packageToSatisfy, 0);
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
                            if (CanSatisfyPackage(supercedent, status)) {
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
                        throw new PackageHasPotentialUpgradesException(packageToSatisfy, supercedents);
                    }
                }
            }

            if (packageToSatisfy.CouldNotDownload || packageToSatisfy.PackageFailedInstall) {
                return false;
            }

            foreach (var dependentPackage in packageToSatisfy.Dependencies) {
                if (!CanSatisfyPackage(dependentPackage, status)) {
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

        public void RemovePackages(IEnumerable<string> packages, Action<PackageInstallerMessage, Package, int> status) {
            // scan 
            GetInstalledPackages(status);

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

                status(PackageInstallerMessage.Removing, p, 0);
                if (!Pretend) {
                    p.Remove((percentage) => {
                        status(PackageInstallerMessage.RemoveProgress, p, percentage);
                    });
                }
                status(PackageInstallerMessage.RemoveProgress, p, 100);
            }
        }

        public IEnumerable<Package> GetInstalledPackages(Action<PackageInstallerMessage, Package, int> status) {
            MSIBase.ScanInstalledMSIs(status,CancellationToken);
            Registrar.SaveCache();
            return Registrar.InstalledPackages;
        }
    }
}