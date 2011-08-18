//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Service {
    using System;
    using System.Collections.Generic;
    using System.Configuration.Install;
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

//                    case "nologo":
//                        this.Assembly().SetLogo("");
//                        break;

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

                Console.WriteLine("[CoApp Interactive -- Press X to stop.]");
                
                // wait for user to cancel task, or when it's actually closed
                while(!task.Wait( 1000 ) ) {
                    Console.Write(".");
                    while (Console.KeyAvailable) {
                        if (Console.ReadKey(true).Key == ConsoleKey.X) {
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