//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using Exceptions;
    using Extensions;
    using Microsoft.Deployment.WindowsInstaller;

    public class PackageManager {
        public bool Pretend { get; set; }
        public IEnumerable<string> PackagesAsSpecified  = Enumerable.Empty<string>();
        public IEnumerable<string> PackagesAreUpgradable = Enumerable.Empty<string>();

        public IEnumerable<string> DoNotScanLocations { get { return Registrar.DoNotScanLocations; } set { Registrar.DoNotScanLocations = value; } }
        public IEnumerable<string> AdditionalScanLocations { get { return Registrar.AdditionalScanLocations; } set { Registrar.AdditionalScanLocations = value; } }
        public IEnumerable<string> AdditionalRecursiveScanLocations { get { return Registrar.AdditionalRecursiveScanLocations; } set { Registrar.AdditionalRecursiveScanLocations = value; } }

        private readonly List<Package> _acquirePackageQueue = new List<Package>();
        private readonly List<Package> _installQueue = new List<Package>();
        private IEnumerable<Package> _installedPackages;

        public CancellationToken CancellationToken;

        public PackageManager() {
            SetUIHandlersToSilent();
        }

        private void SetUIHandlersToSilent() {
            Installer.SetInternalUI(InstallUIOptions.Silent);
            Installer.SetExternalUI(ExternalUI, InstallLogModes.Verbose);
        }

        public MessageResult ExternalUI(InstallMessage messageType, string message, MessageBoxButtons buttons, MessageBoxIcon icon,
            MessageBoxDefaultButton defaultButton) {
            return MessageResult.OK;
        }

        public PackageManager(CancellationToken token) : this() {
            CancellationToken = token;
        }

        public void FlushCache() {
            Registrar.FlushCache();
        }

        public void InstallPackages(IEnumerable<string> packages, Action<PackageInstallerMessage, Package, int> status = null) {
            status = status ?? ((pim, pkg, num) => { });

            if (CancellationToken.IsCancellationRequested) {
                return;
            }

            var packageFiles = Registrar.Packages.GetPackagesByName(packages,true, true);

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
                foreach(var pf in packageFiles) {
                    try {
                        if (CanSatisfyPackage(pf, status))
                            continue;
                    } catch(PackageHasPotentialUpgradesException) {
                        // if this throws, this is a signal to gtfo. (get-the-function-out) ... 
                        // ... what did you think gtfo meant?
                        throw;
                    }

                    if( !Registrar.HasScannedAtLeastOnce ) {
                        // if the registrar has never scanned, try it now.
                        Registrar.ScanForPackages();
                        break; 
                    }

                    if (Registrar.StateCounter != state) {
                        // if stuff has changed, perhaps the registrar should scan again, 
                        // just in case it can find anything new
                        Registrar.ScanForPackages();
                        break;
                    }
                    // so, no scanning has been done since it wasn't likely to find anything new
                    // and we can't satisfy this package. Bummer.
                    throw new PackageNotSatisfiedException(pf);
                    
                }
                
                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                // if we're done here, make sure that any changes to the state are accounted for
                if (Registrar.StateCounter != state) {
                    continue;
                }

                if( _acquirePackageQueue.Count > 0 ) {
                    status(PackageInstallerMessage.NoticeAcquiringPackages, null, 0);
                    // ensure packages are local
                    if (!Registrar.HasScannedAtLeastOnce) {
                        // if the registrar has never scanned, try it now.
                        Registrar.ScanForPackages();
                        continue;
                    }

                    // download what we don't have.
                    // pretendToDownloadTheFiles(); // uh, we don't have any download mechanism right now.

                    
                    // after downloading is finished, check to see if anything changed
                    if (Registrar.StateCounter != state) {
                        continue;
                    }

                    // if we still have packages that we can't get
                    if (_acquirePackageQueue.Count > 0) {
                        foreach (var notAvailablePackage in _acquirePackageQueue)
                            notAvailablePackage.CouldNotDownload = true;
                    }

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state) {
                        continue;
                    }
                }

                status(PackageInstallerMessage.NoticeInstallingPackages, null, 0);
                if( _installQueue.Count > 0 ) {
                    foreach (var pkg in _installQueue) {
                        try {
                            if (CancellationToken.IsCancellationRequested) {
                                return;
                            }

                            if (!pkg.IsInstalled) {
                                status(PackageInstallerMessage.Installing, pkg, 0);
                                int currentTotalTicks = -1;
                                int currentProgress = 0;
                                int progressDirection = 1;

                                if (!Pretend) {
                                    Installer.SetExternalUI(((InstallMessage messageType, string message, 
                                        MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => {
                                        switch(messageType ) {
                                            case InstallMessage.Progress:
                                                var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).ToArray();

                                                switch (msg[1]) { // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                                    case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                                        currentTotalTicks = msg[3];
                                                        currentProgress = 0;
                                                        if (msg.Length >= 6)
                                                            progressDirection = msg[5] == 0 ? 1 : -1;
                                                        break;
                                                    case 1: //Provides information related to progress messages to be sent by the current action.
                                                        break;
                                                    case 2: //Increments the progress bar.
                                                        if (currentTotalTicks == -1)
                                                            break;
                                                        currentProgress += msg[3] * progressDirection ;
                                                        break;
                                                    case 3: //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                                        break;

                                                }
                                                if (currentTotalTicks > 0)
                                                    status(PackageInstallerMessage.InstallProgress, pkg, currentProgress*100/currentTotalTicks );
                                                break;
                                        }
                                        // capture installer messages to play back to status listener
                                        return MessageResult.OK;
                                    }), InstallLogModes.Progress);
                                    pkg.Install();
                                    SetUIHandlersToSilent();
                                    status(PackageInstallerMessage.InstallProgress, pkg, 100);
                                }
                                else {
                                    pkg.IsInstalled = true;
                                }
                            }
                        }
                        catch (PackageInstallFailedException pif) {
                            SetUIHandlersToSilent();
                            if (!pkg.AllowedToSupercede) {
                                throw; // user specified packge as critical.
                            }
                            status(PackageInstallerMessage.FailedDependentPackageInstall,pkg, 0);
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

            // verify the packages are local 
            // ret

            /*
            var request = (HttpWebRequest) WebRequest.Create("http://foo");
            request.BeginGetResponse(x => {
                // 
            }, null);

            
            while (!CancellationToken.IsCancellationRequested) {
                Thread.Sleep(1000);
                status("message", 100);
            }
            */

            // request.Abort();
        }

        private bool CanSatisfyPackage(Package packageToSatisfy, Action<PackageInstallerMessage, Package, int> status) {
            status(PackageInstallerMessage.NoticeCanSatisfyPackage, packageToSatisfy, 0);
            packageToSatisfy.CanSatisfy = false;

            if (packageToSatisfy.IsInstalled)
                return packageToSatisfy.CanSatisfy = true;

            if(!packageToSatisfy.DoNotSupercede) {
                var supercedents = _installedPackages.SupercedentPackages(packageToSatisfy);
                if (supercedents.Count() > 0)
                    return true; // a supercedent package is already installed.
            }

            if(!packageToSatisfy.DoNotSupercede ) {
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
                        if (packageToSatisfy.Supercedent != null)
                            return packageToSatisfy.CanSatisfy = true;
                    }
                    else {
                        // the user hasn't specifically asked us to supercede, yet we know of 
                        // potential supercedents. Let's force the user to make a decision.
                        throw new PackageHasPotentialUpgradesException(packageToSatisfy, supercedents);
                    }
                }
            }

            if (packageToSatisfy.CouldNotDownload || packageToSatisfy.PackageFailedInstall ) {
                
                return false;
            }

            foreach(var dependentPackage in packageToSatisfy.Dependencies ) {
                if (!CanSatisfyPackage(dependentPackage, status))
                    return false;
            }

            if(string.IsNullOrEmpty(packageToSatisfy.LocalPackagePath)) {
                _acquirePackageQueue.Add(packageToSatisfy);
            } else {
                _installQueue.Add(packageToSatisfy);
            }

            return packageToSatisfy.CanSatisfy = true;
        }

        public void RemovePackages(IEnumerable<string> packages, Action<PackageInstallerMessage, Package, int> status) {
            var installedPackages = GetInstalledPackages(status);
            var packageFiles = installedPackages.GetPackagesByName(packages, false, false);

            if (CancellationToken.IsCancellationRequested) {
                return;
            }

            foreach( var p in packageFiles) {
                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                if (!p.IsInstalled)
                    throw new PackageIsNotInstalledException(p);
            }

            foreach (var p in packageFiles) {
                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                int currentTotalTicks = -1;
                int currentProgress = 0;
                int progressDirection = 1;
                status(PackageInstallerMessage.Removing, p, 0);
                if (!Pretend) {
                    Installer.SetExternalUI(((InstallMessage messageType, string message,
                        MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => {
                        switch (messageType) {
                            case InstallMessage.Progress:
                                var msg =
                                    message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).
                                        ToArray();

                                switch (msg[1]) {
                                        // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                    case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                        currentTotalTicks = msg[3];
                                        currentProgress = 0;
                                        if (msg.Length >= 6)
                                            progressDirection = msg[5] == 0 ? 1 : -1;
                                        break;
                                    case 1: //Provides information related to progress messages to be sent by the current action.
                                        break;
                                    case 2: //Increments the progress bar.
                                        if (currentTotalTicks == -1)
                                            break;
                                        currentProgress += msg[3]*progressDirection;
                                        break;
                                    case 3:
                                        //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                        break;

                                }
                                if (currentTotalTicks > 0)
                                    status(PackageInstallerMessage.RemoveProgress, p, currentProgress * 100 / currentTotalTicks);
                                break;
                        }
                        // capture installer messages to play back to status listener
                        return MessageResult.OK;
                    }), InstallLogModes.Progress);
                    p.Remove();
                    SetUIHandlersToSilent();
                }
                status(PackageInstallerMessage.RemoveProgress, p, 100);
            }
        }

        public IEnumerable<Package> GetInstalledPackages(Action<PackageInstallerMessage, Package, int> status) {
            Registrar.LoadCache();
            var products = ProductInstallation.AllProducts;
            var n = 0;
            var total = products.Count();

            foreach( var product in products ) {
                try {
                    if (CancellationToken.IsCancellationRequested) {
                        return null;
                    }
                    int percent = ((n++)*100)/total;
                    status(PackageInstallerMessage.Scanning, null, percent );
                    Registrar.GetPackage(product.LocalPackage);
                }
                catch {
                }
            }
            Registrar.SaveCache();
            return _installedPackages = from package in Registrar.Packages where package.IsInstalled select package;
        }
    }
}