//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Service {
    using System;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.ServiceProcess;
    using Toolkit.Engine;
    using Toolkit.Extensions;
    using Toolkit.Win32;

    public class CoAppService : ServiceBase {
        public static bool IsInstalled {
            get {
                return EngineServiceManager.IsServiceInstalled;
            }
        }

        public CoAppService() {
            ServiceName = EngineServiceManager.CoAppServiceName;
        }

        public static void StartService() {
            EngineServiceManager.TryToStartService();
        }

        public static void StopService() {
            if (IsInstalled) {
                EngineServiceManager.TryToStopService();
            }
        }

        public static bool IsRunning {
            get {
                return EngineServiceManager.IsServiceRunning;
            }
        }

        protected override void OnStart(string[] args) {
            EngineService.Start();
        }

        protected override void OnStop() {
            EngineService.Stop();
        }

        public static void RunThisProcessAsService() {
            Run(new CoAppService());
        }

        public static void Uninstall() {
            if (IsInstalled) {
                // fyi, this will fail in an interesting way if the MMC is running
                // it won't completely uninstall the service, it'll mark it as deleted.
                // we should encourage the closing of MMC here before uninstalling :D
                // http://projectdream.org/wordpress/2007/05/30/the-specified-service-has-been-marked-for-deletion/

                ManagedInstallerClass.InstallHelper(new[] {
                    "/u", Assembly.GetEntryAssembly().Location
                });
            }
        }

        public static void Install(string username = null, string password = null) {
            if (!IsInstalled) {
                //http://arcanecode.com/2007/05/23/windows-services-in-c-adding-the-installer-part-3/
                ManagedInstallerClass.InstallHelper(string.IsNullOrEmpty(username) ? new[] {
                    Assembly.GetEntryAssembly().Location
                } : new[] {
                    "/username=" + username, "/password=" + password, Assembly.GetEntryAssembly().Location
                });
            }
        }

        private static string coappBinDirectory;
        private static string coappInstalledDirectory;
        private static string canonicalServiceExePath;
        public static string CoAppRootDirectory { get { return PackageManagerSettings.CoAppRootDirectory; }}
        public static string CoAppBinDirectory { get { return coappBinDirectory ?? (coappBinDirectory = Path.Combine(CoAppRootDirectory, "bin")); } }
        public static string CanonicalServiceExePath { get { return canonicalServiceExePath ?? (canonicalServiceExePath = Path.Combine(CoAppBinDirectory, "coapp.service.exe")); } }
        public static string CoAppInstalledDirectory { get { return coappInstalledDirectory ?? (coappInstalledDirectory = Path.Combine(CoAppRootDirectory, ".installed")); } }

        public static int AutoInstall() {
            if (IsInstalled) {
                StartService();
                return 0;
            }
            
            if (!Directory.Exists(CoAppRootDirectory)) {
                // no coapp directory? 
                // no way.
                return 100;
            }

            Directory.CreateDirectory(CoAppBinDirectory);
            if (!Directory.Exists(CoAppBinDirectory)) {
                // we couldn't make the bin dir? 
                // I don't wann live in such a world...
                return 200;
            }

            // Let's find the right one
            if (!Directory.Exists(CoAppInstalledDirectory)) {
                var searchDirectory = Path.Combine(CoAppInstalledDirectory, "outercurve foundation");
                if (!Directory.Exists(searchDirectory)) {
                    // hmm. No outercurve directory.
                    // lets just use the .installed directory 
                    searchDirectory = CoAppInstalledDirectory;
                }

                // get all of the coapp.service.exes and pick the one with the highest version
                var serviceExes = searchDirectory.FindFilesSmarter(@"**\coapp.service.exe").OrderByDescending(Version).ToArray();

                // ah, so we found some did we?
                if (serviceExes.Where(each => !Symlink.IsSymlink(each)).Any(AutoInstallService)) {
                    return 0;
                }
            }

            // ok, so we tried installing every exe we could find.
            // What about the EXE we're currently running? 
            // if it's somewhere in the %coapp% directory, 
            // I vote we try creating a symlink for this, and install it.
            var thisEXE = Assembly.GetEntryAssembly().Location;
            if (thisEXE.StartsWith(CoAppRootDirectory, StringComparison.CurrentCultureIgnoreCase)) {
                // it's here somewhere.
                // do it.
                if (AutoInstallService(thisEXE)) {
                    // yay!
                    return 0;
                }
            }

            // fargle-bargle.
            // Ok, I'm thinkin' there is not really any coapp installed at all.
            // we're outta here.
            return 300;
        }

        private static bool AutoInstallService(string path) {
            if (!File.Exists(path)) {
                return false;
            }

            if (Symlink.IsSymlink(path)) {
                // we found a symlink,
                if( !File.Exists(Symlink.GetActualPath(path) )) {
                    // it is invalid anyway. trash it, try again.
                    Symlink.DeleteSymlink(path);
                }
                return false;
            }
            
            try {
                Symlink.MakeFileLink(CanonicalServiceExePath, path);

                // hey we found one where it's supposed to be!
                var processStartInfo = new ProcessStartInfo(CanonicalServiceExePath) {
                    Arguments = "--start",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = Process.Start(processStartInfo);
                process.WaitForExit();

                // after it exits, lets see if we've got an installed service
                if (IsInstalled) {
                    // YAY!. We're outta here!
                    StartService();
                    return true;
                }
            }
            catch {
                // hmm. not working...
            }
            return false;
        }

        private static ulong Version(string path) {
            try {
                var info = FileVersionInfo.GetVersionInfo(path);
                var fv = info.FileVersion;
                if (!String.IsNullOrEmpty(fv)) {
                    fv = fv.Substring(0, fv.PositionOfFirstCharacterNotIn("0123456789.".ToCharArray()));
                }

                if (String.IsNullOrEmpty(fv)) {
                    return 0;
                }

                var vers = fv.Split('.');
                var major = vers.Length > 0 ? vers[0].ToInt32() : 0;
                var minor = vers.Length > 1 ? vers[1].ToInt32() : 0;
                var build = vers.Length > 2 ? vers[2].ToInt32() : 0;
                var revision = vers.Length > 3 ? vers[3].ToInt32() : 0;

                return (((UInt64)major) << 48) + (((UInt64)minor) << 32) + (((UInt64)build) << 16) + (UInt64)revision;
            }
            catch {
                return 0;
            }
        }

    }
}