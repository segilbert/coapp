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


        private List<Package> acquirePackageQueue = new List<Package>();
        private List<Package> installQueue = new List<Package>();

        public CancellationToken CancellationToken;

        public PackageManager() {
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

        public void InstallPackages(IEnumerable<string> packages, Action<string, int> status, Action complete) {
            if (CancellationToken.IsCancellationRequested) {
                return;
            }

            var packageFiles = packages.Select(Registrar.GetPackage).ToList();

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
           
            do {
                if (CancellationToken.IsCancellationRequested) {
                    return;
                }

                state = Registrar.StateCounter;
                acquirePackageQueue.Clear();
                installQueue.Clear();

                // identify any outstanding dependencies
                foreach(var pf in packageFiles) {
                    if (CanSatisfyPackage(pf)) 
                        continue;

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
                    status("Can't satisfy {0}".format(pf.CosmeticName), 100);
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

                if( acquirePackageQueue.Count > 0 ) {
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
                    if (acquirePackageQueue.Count > 0) {
                        foreach (var notAvailablePackage in acquirePackageQueue)
                            notAvailablePackage.CouldNotDownload = true;
                    }

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state) {
                        continue;
                    }
                }

                if( installQueue.Count > 0 ) {
                    foreach (var pkg in installQueue) {
                        try {
                            
                            if (CancellationToken.IsCancellationRequested) {
                                return;
                            }
                            status("Installing {0}".format(pkg.CosmeticName), 100);
                            if (!Pretend) {
                                pkg.Install();    
                            }
                            
                        }
                        catch (PackageInstallFailed pif) {
                            if (!pkg.AllowedToSupercede) {
                                throw pif; // user specified packge as critical.
                            }
                            pkg.PackageFailedInstall = true;
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

            /*
            var dependencies = new List<Package>();
            foreach( var pkg in packageFiles ) {
                dependencies.Intersect(AddRange(pkg.))
            }
            */


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

            complete();
            // request.Abort();
        }

        private bool CanSatisfyPackage(Package packageToSatisfy) {
            if (packageToSatisfy.IsInstalled)
                return true;
            
            if( packageToSatisfy.AllowedToSupercede ) {
                packageToSatisfy.Supercedent = null;
                var supercedents = Registrar.LocateSupercedentPackages(packageToSatisfy);
                // Console.WriteLine("PACKAGE :"+packageToSatisfy.CosmeticName);
                // Registrar.DumpPackages(supercedents);

                foreach( var supercedent in supercedents ) {
                    if (CanSatisfyPackage(supercedent)) {
                        packageToSatisfy.Supercedent = supercedent;
                        break;
                    }
                }
                // if we have a supercedent, then this package's dependents are moot.)
                if (packageToSatisfy.Supercedent != null )
                    return true;
            }

            if (packageToSatisfy.CouldNotDownload || packageToSatisfy.PackageFailedInstall ) {
                
                return false;
            }

            foreach(var dependentPackage in packageToSatisfy.Dependencies ) {
                if (!CanSatisfyPackage(dependentPackage))
                    return false;
            }

            if(string.IsNullOrEmpty(packageToSatisfy.LocalPackagePath)) {
                acquirePackageQueue.Add(packageToSatisfy);
            } else {
                installQueue.Add(packageToSatisfy);
            }

            return true;
        }

        public void RemovePackages(IEnumerable<string> packages, Action<string, int> status, Action complete) {
        }

        public IEnumerable<string> GetInstalledPackages(Action<string, int> status) {
            return null;
        }

        public void InstallPackages(string path) {
        }
    }
}