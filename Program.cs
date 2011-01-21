//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.CLI {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Resources;
    using Properties;
    using Toolkit.Console;
    using Toolkit.Engine;
    using Toolkit.Engine.Exceptions;
    using Toolkit.Extensions;

    /// <summary>
    ///   Main Program for command line coapp tool
    /// </summary>
    public class Program : AsyncConsoleProgram {
        protected override ResourceManager Res {
            get { return Resources.ResourceManager; }
        }

        /// <summary>
        ///   Main entrypoint for CLI.
        /// </summary>
        /// <param name = "args">
        ///   The command line arguments
        /// </param>
        /// <returns>
        ///   int value representing the ERRORLEVEL.
        /// </returns>
        private static int Main(string[] args) {
            return new Program().Startup(args);
        }

        protected PackageManager pkgManager;

        /// <summary>
        ///   The (non-static) startup method
        /// </summary>
        /// <param name = "args">
        ///   The command line arguments.
        /// </param>
        /// <returns>
        ///   Process return code.
        /// </returns>
        protected override int Main(IEnumerable<string> args) {
            try {
                pkgManager = new PackageManager(CancellationTokenSource.Token);

                #region commane line parsing

                var options = args.Switches();
                var parameters = args.Parameters();

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];

                    switch (arg) {
                            /* options  */
                        case "pretend":
                            pkgManager.Pretend = true;
                            break;

                            /* global switches */
                        case "load-config":
                            // all ready done, but don't get too picky.
                            break;

                        case "nologo":
                            this.Assembly().SetLogo(string.Empty);
                            break;

                        case "help":
                            return Help();

                        default:
                            throw new ConsoleException(Resources.UnknownParameter, arg);
                    }
                }

                Logo();

                if (parameters.Count() < 1) {
                    throw new ConsoleException(Resources.MissingCommand);
                }
                #endregion

                var command = parameters.FirstOrDefault().ToLower();
                parameters = parameters.Skip(1);

                switch (command) {
                    case "install":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.InstallRequiresPackageName);
                        }

                        TaskAdd(() => Install(parameters));
                        break;

                    case "uninstall":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.UninstallRequiresPackageName);
                        }
                        TaskAdd(() => Uninstall(parameters));
                        break;

                    case "list":
                        if (parameters.Count() != 1) {
                            throw new ConsoleException(Resources.MissingParameterForList);
                        }

                        List(parameters);
                        break;

                    default:
                        throw new ConsoleException(Resources.UnknownCommand,command);
                }
            }
            catch (ConsoleException failure) {
                CancellationTokenSource.Cancel();
                Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
            }
            return 0;
        }

        private void List(IEnumerable<string> parameters) {
            // dual purpose
            if (parameters.FirstOrDefault().ToLower() == "packages") {
                var pkgsInstalled = pkgManager.GetInstalledPackages((message, percentage) => {
                    // status
                    Console.WriteLine("Status:{0}", message);
                });
            }
        }

        private void Uninstall(IEnumerable<string> parameters) {
            pkgManager.RemovePackages(parameters, (message, percentage) => {
                // status
                Console.WriteLine("Status:{0}", message);
            }, () => {
                // complete
                Console.WriteLine("Done");
            });
        }

        private void Install(IEnumerable<string> parameters) {
            try {
                pkgManager.InstallPackages(parameters, (message, percentage) => {
                    // status
                    Console.WriteLine("Status:{0}", message);
                }, () => {
                    // complete
                    Console.WriteLine("Done");
                });
            }
            catch (PackageNotFoundException pnf) {
                throw new ConsoleException(Resources.PackageNotFound, pnf.PackagePath);
            }
            catch (InvalidPackageException ip) {
                throw new ConsoleException(Resources.InvalidPackage, ip.PackagePath);
            }
        }
    }
}