//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Service {
    using System;
    using System.Collections.Generic;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.ServiceProcess;
    using System.Threading;
    using CoApp.Toolkit.Extensions;
    using Toolkit.Engine;
    using Toolkit.Exceptions;
    using Toolkit.Win32;

    internal class CoAppServiceMain {
        public static bool UseUserAccount;
        private bool start;
        private bool stop;
        private bool install;
        private bool status;
        private bool interactive;
        private bool uninstall;
        private string username;
        private string password;
        

        private const string help =
           @"
Usage:
-------

CoApp.Service [options] 
    
    Options:
    --------
    --help                      this help
    --load-config=<file>        loads configuration from <file>

    --start                     starts the service if not running 
                                (implies --install)

    --restart                   stops and starts the service 
                                (implies --install, --stop, --start)

    --stop                      stops the service if running

    --install                   installs the service 

    --status                    prints the status of the service

    --interactive               runs the CoApp Service as an interactive process.
                                (use ctrl-c to stop)

    --user=<userid>             sets the userid when installing the service
                                (defaults to localsystem)

    --password=<password>       sets the password of the account when 
                                installing the service

    --uninstall                 uninstalls the service
";


        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        private static int Main(string[] args) {
            try {
                return new CoAppServiceMain().main(args);
            }
             catch (ConsoleException failure) {
                return Fail("\r\n{0}\r\n", failure.Message);
            }
        }

        private static void RequiresAdmin(string operation) {
            if(!AdminPrivilege.IsRunAsAdmin ) {
                throw new ConsoleException("The operation '{0}' requires administrator priviliges.",operation);
            }
        }

        private int main(IEnumerable<string> args) {
            var options = args.Switches();
            var parameters = args.Parameters();

            Console.CancelKeyPress += (x, y) => {
                Console.WriteLine("Stopping CoAppService.");
                EngineService.Stop();
            };

            #region Parse Options
            foreach (var arg in from arg in options.Keys  select arg) {
                var argumentParameters = options[arg];
                switch (arg) {
                    case "load-config":
                        break;

                    case "auto-install":
                        RequiresAdmin("--auto-install");
                        Environment.Exit(AutoInstall());
                        break;

                    case "start":
                        start = true;
                        install = true;
                        break;

                    case "restart":
                        stop = true;
                        start = true;
                        install = true;
                        break;

                    case "stop":
                        stop = true;
                        break;

                    case "install":
                        install = true;
                        break;

                    case "uninstall":
                        stop = true;
                        uninstall = true;
                        break;

                    case "username":
                        UseUserAccount = true;
                        username = argumentParameters.LastOrDefault();
                        break;

                    case "password":
                        password = argumentParameters.LastOrDefault();
                        break;

                    case "status":
                        status = true;
                        break;

                    case "interactive":
                        interactive = true;
                        break;

                    case "help":
                        return Help();

                    default:
                        Fail("Unrecognized switch [--{0}]", arg);
                        return Help();
                }
            }
            #endregion

            Logo();

            if(interactive) {
                RequiresAdmin("--interactive");

                if (CoAppService.IsRunning) {
                    throw new ConsoleException("The CoApp Service can not be running.\r\nYou must stop it with --stop before using the service interactively.");
                }
                Console.WriteLine("Launching CoApp Service interactively.\r\nUse ctrl-c to stop.");

                var task = EngineService.Start();

                Console.WriteLine("[CoApp Interactive -- Press escape to stop.]");
                
                // wait for user to cancel task, or when it's actually closed
                while(!task.Wait( 1000 ) ) {
                    Console.Write(".");
                    while (Console.KeyAvailable) {
                        if (Console.ReadKey(true).Key == ConsoleKey.Escape) {
                            EngineService.Stop();
                        }
                    }
                }
                return 0;
            }

            if(stop) {
                RequiresAdmin("--stop");
                if (CoAppService.IsInstalled) {
                    CoAppService.StopService();
                } else {
                    throw new ConsoleException("CoApp.Service is not installed.");
                }
            }

            if(uninstall) {
                RequiresAdmin("--uninstall");
                // fyi, this will fail in an interesting way if the MMC is running
                // it won't completely uninstall the service, it'll mark it as deleted.
                // we should encourage the closing of MMC here before uninstalling :D
                // http://projectdream.org/wordpress/2007/05/30/the-specified-service-has-been-marked-for-deletion/
                if (CoAppService.IsInstalled) {
                    ManagedInstallerClass.InstallHelper(new[] {"/u", Assembly.GetEntryAssembly().Location});
                }
                else {
                    throw new ConsoleException("CoApp.Service is not installed.");
                }
                return 0;
            }
            
            if(install) {
                RequiresAdmin("--install");
                if (!CoAppService.IsInstalled) {
                    //http://arcanecode.com/2007/05/23/windows-services-in-c-adding-the-installer-part-3/
                    ManagedInstallerClass.InstallHelper(string.IsNullOrEmpty(username)
                        ? new[] {Assembly.GetEntryAssembly().Location}
                        : new[] {"/username=" + username, "/password=" + password, Assembly.GetEntryAssembly().Location});
                } 
            }

            if (start) {
                RequiresAdmin("--start");
                if (CoAppService.IsInstalled) {
                    CoAppService.StartService();
                }
                else {
                    throw new ConsoleException("CoApp.Service is not installed.");
                }
            }

            if( !options.Any() && CoAppService.IsInstalled && parameters.FirstOrDefault() == null ) {
                // this lets us run the service 
                ServiceBase.Run(new CoAppService());
                return 0;
            }

            if(status) {
                Console.WriteLine("Service installed: {0}", CoAppService.IsInstalled);
                Console.WriteLine("Service running: {0}", CoAppService.IsRunning);
                return 0;
            }

            if (!options.Any()) {
                throw new ConsoleException("Missing CoApp.Service command. Use --help for information");
            }

            return 0;
        }

        private int AutoInstall() {
            if (CoAppService.IsInstalled) {
                CoAppService.StartService();
                return 0;
            }

            var coAppRootDirectory = PackageManagerSettings.CoAppRootDirectory;
            if( !Directory.Exists(coAppRootDirectory )) {
                // no coapp directory? 
                // no way.
                return 100;
            }

            var binDir = Path.Combine(coAppRootDirectory, "bin");
            var canonicalExePath = Path.Combine(binDir, "coapp.service.exe");

            if( Directory.Exists(binDir)) {
                if (AutoInstallService(canonicalExePath)) {
                    // yay!
                    return 0;
                }
            }

            Directory.CreateDirectory(binDir);
            if( !Directory.Exists(binDir)) {
                // we couldn't make the bin dir? 
                // I don't wann live in such a world...
                return 200;
            }

            // it's either not in the bin directory, or it was, and it didn't install.
            // time for plan B.
            var installedDir = Path.Combine(coAppRootDirectory, ".installed");
            if( !Directory.Exists(installedDir)) {
                var searchDirectory = Path.Combine(installedDir, "outercurve foundation");
                if (!Directory.Exists(searchDirectory)) {
                    // hmm. No outercurve directory.
                    // lets just use the .installed directory 
                    searchDirectory = installedDir;
                }

                // get all of the coapp.service.exes and pick the one with the highest version
                var serviceExes = searchDirectory.FindFilesSmarter(@"**\coapp.service.exe");
                if (!serviceExes.IsNullOrEmpty()) {
                    // ah, so we found some did we?
                    serviceExes = serviceExes.OrderByDescending(Version);

                    foreach (var path in serviceExes) {
                        // try to create the canonical symlink.
                        Symlink.MakeFileLink(canonicalExePath, path);

                        if (AutoInstallService(canonicalExePath)) {
                            // yay!
                            return 0;
                        }
                        // :(
                    }
                }
            }

            // ok, so we tried installing every exe we could find.
            // What about the EXE we're currently running? 
            // if it's somewhere in the %coapp% directory, 
            // I vote we try creating a symlink for this, and install it.
            var thisEXE = Assembly.GetExecutingAssembly().Location;
            if( thisEXE.StartsWith(coAppRootDirectory, StringComparison.CurrentCultureIgnoreCase)) {
                // it's here somewhere.
                // do it.
                Symlink.MakeFileLink(canonicalExePath, thisEXE);

                if( AutoInstallService(canonicalExePath)) {
                    // yay!
                    return 0;
                }
            }

            // fargle-bargle.
            // Ok, I'm thinkin' there is not really any coapp installed at all.
            // we're outta here.
            return 300;
        }

        private bool AutoInstallService(string path) {
            if( File.Exists(path)) {
                try {
                    // hey we found one where it's supposed to be!
                    var processStartInfo = new ProcessStartInfo(path) {
                        Arguments = "--start",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var process = Process.Start(processStartInfo);
                    process.WaitForExit();

                    // after it exits, lets see if we've got an installed service
                    if (CoAppService.IsInstalled) {
                        // YAY!. We're outta here!
                        CoAppService.StartService();
                        return true;
                    }

                } catch {
                    // hmm. not working...
                }
            }
            return false;
        }

        private static int PositionOfFirstCharacterNotIn(string str, char[] characters) {
            var p = 0;
            while (p < str.Length) {
                if (!characters.Contains(str[p])) {
                    return p;
                }
                p++;
            }
            return p;
        }

        private static ulong Version(string path) {
            try {
                var info = FileVersionInfo.GetVersionInfo(path);
                var fv = info.FileVersion;
                if (!String.IsNullOrEmpty(fv)) {
                    fv = fv.Substring(0, PositionOfFirstCharacterNotIn(fv, "0123456789.".ToCharArray()));
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
            } catch {
                return 0;
            }
        }

        #region fail/help/logo

        public static int Fail(string text, params object[] par) {
            Logo();
                Console.WriteLine("Error:{0}", text.format(par));
            return 1;
        }

        private static int Help() {
            Logo();
                help.Print();
            return 0;
        }

        private static void Logo() {
        }

        #endregion
    }
}