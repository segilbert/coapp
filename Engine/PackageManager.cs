//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using Exceptions;
    using CoApp.Toolkit.Extensions;
    using Microsoft.Deployment.WindowsInstaller;
    using Properties;

    public class PackageManager {
        public bool Pretend { get; set; }
        public IEnumerable<string> PackagesAsSpecified;
        public CancellationToken CancellationToken;

        public PackageManager() {
            Installer.SetInternalUI(InstallUIOptions.Silent);
            Installer.SetExternalUI(ExternalUI, InstallLogModes.Verbose);
        }

        public MessageResult ExternalUI( InstallMessage messageType, string message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) {

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
            
            foreach (var p in from p in packageFiles from pas in PackagesAsSpecified where p.CosmeticName.IsWildcardMatch(pas) select p) {
                p.DoNotSuperceed = true;
            }
            
            int state;

            do {
                state = Registrar.StateCounter;

                {
                    // identify any outstanding dependencies

                    //...

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state)
                        continue;
                }

                {
                    // ensure packages are local

                    //...

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state)
                        continue;
                }

                {
                    // try to install packages selected for install
                    // full set of packages to install
                    var pkgSet = packageFiles.Aggregate((IEnumerable<Package>)packageFiles, (current, p) => current.Union(p.DependenciesToInstall));

                    foreach( var pkg in pkgSet ) {
                        try {
                            pkg.Install();
                        } catch( PackageInstallFailed pif ) {
                            //if( pkg.)    
                        }
                    }
                    // ...

                    // if we're done here, make sure that any changes to the state are accounted for
                    if (Registrar.StateCounter != state)
                        continue;
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

        public void RemovePackages(IEnumerable<string> packages, Action<string, int> status, Action complete) {
        }

        public IEnumerable<string> GetInstalledPackages(Action<string, int> status) {
            return null;
        }

        public void InstallPackages(string path) {
        }
    }
}