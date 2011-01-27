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
    using System.Threading;
    using Properties;
    using Toolkit.Console;
    using Toolkit.Engine;
    using Toolkit.Engine.Exceptions;
    using Toolkit.Extensions;

    /// <summary>
    ///   Main Program for command line coapp tool
    /// </summary>
    public class CoAppMain : AsyncConsoleProgram {
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
            return new CoAppMain().Startup(args);
        }

        protected PackageManager _pkgManager;

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
                _pkgManager = new PackageManager(CancellationTokenSource.Token);

                #region commane line parsing

                // default:
                _pkgManager.AdditionalScanLocations = new[] {Environment.CurrentDirectory};

                var options = args.Switches();
                var parameters = args.Parameters();

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];

                    switch (arg) {
                            /* options  */
                        case "pretend":
                            _pkgManager.Pretend = true;
                            break;

                        case "as-specified":
                            _pkgManager.PackagesAsSpecified = string.IsNullOrEmpty(argumentParameters.FirstOrDefault()) ? new[] {"*"} : argumentParameters;
                            break;

                        case "upgrade":
                            _pkgManager.PackagesAreUpgradable =string.IsNullOrEmpty(argumentParameters.FirstOrDefault())  ? new[] {"*"} : argumentParameters;
                            break;

                        case "no-scan":
                            _pkgManager.DoNotScanLocations = string.IsNullOrEmpty(argumentParameters.FirstOrDefault())  ? new[] {"*"} : argumentParameters;
                            break;

                        case "scan":
                            if (string.IsNullOrEmpty(argumentParameters.FirstOrDefault())) {
                                throw new ConsoleException(Resources.OptionRequiresLocation.format("--scan"));
                            }

                            _pkgManager.AdditionalScanLocations = argumentParameters;
                            break;

                        case "recursive-scan":
                            if (argumentParameters.Count() == 0) {
                                throw new ConsoleException(Resources.OptionRequiresLocation.format("--recursive-scan"));
                            }

                            _pkgManager.AdditionalRecursiveScanLocations = argumentParameters;
                            break;

                        case "flush-cache":
                            _pkgManager.FlushCache();
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

                    case "remove":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.RemoveRequiresPackageName);
                        }
                        TaskAdd(() => Remove(parameters));
                        break;

                    case "list":
                        if (parameters.Count() != 1) {
                            throw new ConsoleException(Resources.MissingParameterForList);
                        }

                        List(parameters);
                        break;

                    default:
                        throw new ConsoleException(Resources.UnknownCommand, command);
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
                var pkgsInstalled = _pkgManager.GetInstalledPackages((packageInstallerMessage, package, percentage) => {
                    // status
                    switch (packageInstallerMessage) {
                        case PackageInstallerMessage.Scanning:
                            "Scanning: ".PrintProgressBar(percentage);
                            break;
                    }
                });
                " ".PrintProgressBar(-1);
                Console.WriteLine("\r");
                if (pkgsInstalled.Count() > 0) {
                    Console.WriteLine("\rPackages currently installed:");
                    pkgsInstalled.ToTable(new[] {"CosmeticName", "LocalPackagePath", "PublicKeyToken"}).Dump(new[]
                    {"Name", "Installer", "Public Key Token"});
                } else {
                    Console.WriteLine("\rThere are no packages currently installed.");
                }
            }
        }

        private void Remove(IEnumerable<string> parameters) {
            try {
                int maxPercent = 0;
                _pkgManager.RemovePackages(parameters, (packageInstallerMessage, package, percentage) => {
                    // status
                    switch (packageInstallerMessage) {
                        case PackageInstallerMessage.Removing:
                            Console.Write("\r\nRemoving: {0}", package.CosmeticName);
                            maxPercent = 0;
                            break;

                        case PackageInstallerMessage.RemoveProgress:
                            if (percentage > maxPercent) {
                                "Removing: {0}".format(package.CosmeticName).PrintProgressBar(percentage);
                                maxPercent = percentage;
                            }
                            break;
                    }
                });
            } catch( PackageRemoveFailedException puif ) {
                Console.WriteLine("Remove of package {0} failed:", puif.FailedPackage.CosmeticName );
                Console.WriteLine("    File: {0} :", puif.FailedPackage.LocalPackagePath);
            }
            catch (PackageNotFoundException pnf) {
                Console.WriteLine(Resources.PackageNotFound, pnf.PackagePath);
            }
            catch (PackageIsNotInstalledException pini) {
                Console.WriteLine("The package {0} is not currently installed", pini.Package.CosmeticName);
            }
            catch (MultiplePackagesMatchException mpm) {
                Console.WriteLine(Resources.PackageHasMultipleMatches, mpm.PackageMask);
                foreach (var pkg in mpm.PackageMatches) {
                    Console.WriteLine(@"   {0}", pkg.CosmeticName);
                }
            }
        }

        private void Install(IEnumerable<string> parameters) {
            try {
                int maxPercent = 0;
                _pkgManager.InstallPackages(parameters, (packageInstallerMessage, package, percentage) => {
                    // status
                    switch (packageInstallerMessage) {
                        case PackageInstallerMessage.Installing:
                            Console.Write("\r\nInstalling: {0}\r", package.CosmeticName);
                            maxPercent = 0;
                            break;

                        case PackageInstallerMessage.InstallProgress:
                            if (percentage > maxPercent) {
                                "Installing: {0}".format(package.CosmeticName).PrintProgressBar(percentage);
                                maxPercent = percentage;
                            }
                            break;
                    }
                });

                // success means that this doesn't throw an exception.
            }
            catch (PackageNotFoundException pnf) {
                Console.WriteLine(Resources.PackageNotFound, pnf.PackagePath);
            }
            catch (InvalidPackageException ip) {
                Console.WriteLine(Resources.InvalidPackage, ip.PackagePath);
            }
            catch (PackageInstallFailedException pif) {
                Console.WriteLine(Resources.PackageFailedInstall,pif.FailedPackage);
            }
            catch (PackageNotSatisfiedException pns) {
                Console.WriteLine(Resources.PackageDependenciesCantInstall,
                    pns.packageNotSatified.CosmeticName);

                var depth = 0;
                Action<Package> printDepMap = null;
                printDepMap = (p => {
                    depth++;
                    if (!p.CanSatisfy) {
                        var count = p.Dependencies.Count();
                        Console.WriteLine(@"{0}{1} [{2}]", "".PadLeft(3*depth), p.CosmeticName,
                            count > 0
                            ? Resources.MissingPkgText.format(count, count > 1 ? "dependencies" : "dependency")
                                : p.CouldNotDownload
                                    ? Resources.CouldNotDownload : p.PackageFailedInstall ? Resources.FailedToInstall : !p.HasLocalFile ? "No local package file" : "Unknown");
                        foreach (var dp in p.Dependencies.Where(each => !each.CanSatisfy)) {
                            printDepMap(dp);
                        }
                    }
                    depth--;
                });
                printDepMap(pns.packageNotSatified);
            }
            catch (PackageHasPotentialUpgradesException phphu) {
                // the user hasn't specifically asked us to supercede, yet we know of 
                // potential supercedents. Let's force the user to make a decision.
                Console.WriteLine(Resources.PackageHasPossibleNewerVersion, phphu.UnsatisfiedPackage);
                Console.WriteLine(Resources.TheFollowingPackageSupercede);
                Console.WriteLine(@"   [Latest] {0}", phphu.SatifactionOptions.First().CosmeticName);

                foreach (var pkg in phphu.SatifactionOptions.Skip(1)) {
                    Console.WriteLine(@"            {0}", pkg.CosmeticName);
                }

                Console.WriteLine();
                Console.WriteLine(Resources.AutoAcceptHint,
                    phphu.UnsatisfiedPackage.CosmeticName);
                Console.WriteLine(Resources.AsSpecifiedHint,
                    phphu.UnsatisfiedPackage.CosmeticName);
            }
            catch (MultiplePackagesMatchException mpm) {
                Console.WriteLine(Resources.PackageHasMultipleMatches, mpm.PackageMask );
                foreach (var pkg in mpm.PackageMatches) {
                    Console.WriteLine(@"   {0}", pkg.CosmeticName);
                }
            }
            
        }
    }
}