//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.CLI {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Resources;
    using System.Threading;
    using System.Threading.Tasks;
    using Properties;
    using Toolkit.Console;
    using Toolkit.Engine;
    using Toolkit.Engine.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Network;
    using Toolkit.Win32;

    /// <summary>
    ///   Main Program for command line coapp tool
    /// </summary>
    public class CoAppMain : AsyncConsoleProgram {
        protected PackageManager _pkgManager;

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

                bool waitforbreak = false;

                #region commane line parsing

                // default:
                _pkgManager.SessionFeedLocations = new[] {Environment.CurrentDirectory};

                var options = args.Switches();
                var parameters = args.Parameters();

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];

                    switch (arg) {
                            /* options  */
                        case "pretend":
                            _pkgManager.Pretend = true;
                            break;

                        case "wait-for-break":
                            waitforbreak = true;
                            break;

                        case "maximum":
                            _pkgManager.MaximumPackagesToProcess = argumentParameters.Last().ToInt32(10);
                            break;

                        case "as-specified":
                            _pkgManager.PackagesAsSpecified = string.IsNullOrEmpty(argumentParameters.FirstOrDefault())
                                ? new[] {"*"}
                                : argumentParameters;
                            break;

                        case "upgrade":
                            _pkgManager.PackagesAreUpgradable = string.IsNullOrEmpty(argumentParameters.FirstOrDefault())
                                ? new[] {"*"}
                                : argumentParameters;
                            break;

                        case "no-scan":
                            _pkgManager.DoNotScanLocations = string.IsNullOrEmpty(argumentParameters.FirstOrDefault())
                                ? new[] {"*"}
                                : argumentParameters;
                            break;

                        case "no-network":
                            _pkgManager.DoNotScanLocations = new[] {"*://*"};
                            break;

                        case "scan":
                            if (string.IsNullOrEmpty(argumentParameters.FirstOrDefault())) {
                                throw new ConsoleException(Resources.OptionRequiresLocation.format("--scan"));
                            }
                            _pkgManager.SessionFeedLocations = argumentParameters;
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
                    case "nothing":
                        var remoteFileUri = new Uri(parameters.First());

                        // create the remote file reference 
                        var remoteFile = new RemoteFile(remoteFileUri, CancellationTokenSource.Token);

                        // Tell it to download it 
                        var getTask = remoteFile.Get();

                        // monitor the progress.
                        remoteFile.DownloadProgress.Notification += progress => {
                            // this executes when the download progress value changes. 
                            Console.Write(progress <= 100 ? "..{0}% " : "bytes: [{0}]", progress);
                        };

                        // when it's done, do this:
                        getTask.ContinueWith(t => {
                            // this executes when the Get() operation completes.
                            Console.WriteLine("File {0} has finished downloading", remoteFile.LocalFullPath);
                        });

                        /*
                        getTask.ProgressChanged = (value) => {
                            Console.Write(value <= 100 ? "..{0}% " : "bytes: [{0}]", value);
                        };

                        long p = f.Progress;
                        
                        while( !getTask.IsCompleted ) {
                          /*
                            OnValueChanged(f.Progress, (value) => {
                                Console.Write(value <= 100 ? "..{0}% " : "bytes: [{0}]", value);
                            });
                            


                            

                            if( p != f.Progress) {
                                p = f.Progress;
                                Console.Write(p  <= 100 ? "..{0}% " : "bytes: [{0}]", p);
                            }
                            Thread.Sleep(100);
                        }
                        */

                        getTask.Wait();

                        /*
                        var chc = new CachingHttpClient(Path.GetFullPath("."));

                        var t = chc.PreviewFile(new Uri(parameters.First())).ContinueWith(result => {
                            Console.WriteLine("1. Preview: Status Code[{0}] State[{1}] LocalPath[{2}]", result.Result.StatusCode,
                                result.Result.State, result.Result.LocalFullPath);
                            
                        }, TaskContinuationOptions.AttachedToParent);

                        chc.DownloadFile(new Uri(parameters.First()), progress => {
                            if (progress <= 100) {
                                Console.Write("..{0}% ", progress);
                            }
                            else {
                                Console.Write("bytes: [{0}]", progress);
                            }
                        }).ContinueWith(
                            result => {
                                Console.WriteLine("2. Download: Status Code[{0}] State[{1}] LocalPath[{2}]", result.Result.StatusCode,
                                    result.Result.State, result.Result.LocalFullPath);
                                waitforbreak = false;
                            }, TaskContinuationOptions.AttachedToParent);

                        Thread.Sleep(2000);
                         * )
                        */
                        ;
                        break;

                    case "install":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.InstallRequiresPackageName);
                        }

                        Install(parameters);
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
                        switch (parameters.FirstOrDefault().ToLower()) {
                            case "packages":
                            case "package":
                                ListPackages(parameters);
                                break;

                            case "feed":
                            case "feeds":
                            case "repositories":
                            case "repository":
                                ListFeeds(parameters);
                                break;
                        }
                        break;

                    case "upgrade":
                        if (parameters.Count() != 1) {
                            throw new ConsoleException(Resources.MissingParameterForUpgrade);
                        }

                        TaskAdd(() => Upgrade(parameters));
                        break;

                    case "add":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.AddFeedRequiresLocation);
                        }
                        TaskAdd(() => AddFeed(parameters));
                        break;

                    case "delete":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.DeleteFeedRequiresLocation);
                        }
                        TaskAdd(() => DeleteFeed(parameters));
                        break;

                    case "trim":
                        if (parameters.Count() != 0) {
                            throw new ConsoleException(Resources.TrimErrorMessage);
                        }

                        TaskAdd(() => Trim(parameters));
                        break;

                    default:
                        throw new ConsoleException(Resources.UnknownCommand, command);
                }

                while (waitforbreak && !CancellationTokenSource.IsCancellationRequested) {
                    Thread.Sleep(100);
                }
            }
            catch (ConsoleException failure) {
                CancellationTokenSource.Cancel();
                Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
            }
            return 0;
        }

        private void ListPackages(IEnumerable<string> parameters) {
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
            }
            else {
                Console.WriteLine("\rThere are no packages currently installed.");
            }
        }

        private void Remove(IEnumerable<string> parameters) {
            try {
                if (!AdminPrivilege.IsRunAsAdmin) {
                    throw new ConsoleException(
                        "Admin privilege is required to remove packages. \r\nPlease run as an elevated administrator.");
                }

                var maxPercent = 0L;
                _pkgManager.RemovePackages(parameters, (packageInstallerMessage, payload, progress) => {
                    // status
                    var package = payload as Package;
                    switch (packageInstallerMessage) {
                        case PackageInstallerMessage.Removing:
                            Console.Write("\r\nRemoving: {0}", package.CosmeticName);
                            maxPercent = 0;
                            break;

                        case PackageInstallerMessage.RemoveProgress:
                            if (progress > maxPercent) {
                                "Removing: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                                maxPercent = progress;
                            }
                            break;
                    }
                });
            }
            catch (PackageRemoveFailedException puif) {
                Console.WriteLine("Remove of package {0} failed:", puif.FailedPackage.CosmeticName);
                Console.WriteLine("    File: {0} :", puif.FailedPackage.LocalPackagePath);
            }
            catch (PackageNotFoundException pnf) {
                Console.WriteLine(Resources.PackageNotFound, pnf.PackagePath);
            }
            catch (PackageNotInstalledException pni) {
                Console.WriteLine(Resources.PackageNotFound, pni.NotInstalledPackage);
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
            if (!AdminPrivilege.IsRunAsAdmin) {
                throw new ConsoleException(
                    "Admin privilege is required to install packages. \r\nPlease run as an elevated administrator.");
            }

            var maxPercent = 0L;
            var t = _pkgManager.InstallPackages(parameters, (packageInstallerMessage, payload, progress) => {
                // status
                var package = payload as Package;
                switch (packageInstallerMessage) {
                    case PackageInstallerMessage.Installing:
                        Console.Write("\r\nInstalling: {0}\r", package.CosmeticName);
                        maxPercent = 0;
                        break;

                    case PackageInstallerMessage.InstallProgress:
                        if (progress > maxPercent) {
                            "Installing: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                            maxPercent = progress;
                        }
                        break;

                    case PackageInstallerMessage.DownloadingUrl:
                        maxPercent = 0L;
                        break;

                    case PackageInstallerMessage.DownloadUrlProgress:
                        if (progress > maxPercent) {
                            "Downloading: {0}".format(payload.ToString()).PrintProgressBar(progress);
                            maxPercent = progress;
                            // TODO: this probably looks like hell when downloading multiple packages concurrently.
                        }
                        break;

                    case PackageInstallerMessage.DownloadingPackage:
                        maxPercent = 0L;
                        break;

                    case PackageInstallerMessage.DownloadPackageProgress:
                        if (progress > maxPercent) {
                            "Downloading: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                            maxPercent = progress;
                            // TODO: this probably looks like hell when downloading multiple packages concurrently.
                        }
                        break;
                }
            });

            try {
                t.Wait(); // waiting for the task to finish.
                // success means that this doesn't throw an exception.
                
            }
            catch (AggregateException ae) {
                Console.WriteLine();
                Func<Exception,bool> handleException = null;

                handleException = exception => {
                    if (exception is AggregateException) {
                        (exception as AggregateException).Handle(handleException);
                        return true;
                    }

                    try {
                        Console.WriteLine(exception.StackTrace);
                        throw exception;
                    }
                    catch (PackageNotFoundException pnf) {
                        Console.WriteLine(Resources.PackageNotFound, pnf.PackagePath);
                    }
                    catch (InvalidPackageException ip) {
                        Console.WriteLine(Resources.InvalidPackage, ip.PackagePath);
                    }
                    catch (PackageInstallFailedException pif) {
                        Console.WriteLine(Resources.PackageFailedInstall, pif.FailedPackage);
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
                                Console.WriteLine(@"{0}{1} [{2}]", "".PadLeft(3 * depth), p.CosmeticName,
                                    count > 0
                                        ? Resources.MissingPkgText.format(count, count > 1 ? "dependencies" : "dependency")
                                        : p.CouldNotDownload
                                            ? Resources.CouldNotDownload
                                            : p.PackageFailedInstall
                                                ? Resources.FailedToInstall
                                                : !p.HasLocalFile ? "No local package file" : "Unknown");
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
                        Console.WriteLine(Resources.PackageHasMultipleMatches, mpm.PackageMask);
                        foreach (var pkg in mpm.PackageMatches) {
                            Console.WriteLine(@"   {0}", pkg.CosmeticName);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.StackTrace);
                        Console.WriteLine(ex.Message);
                        throw new ConsoleException("Something unexpected.\r\n{0}",ex.Message);
                    }

                    return true;
                };
                ae.Handle(handleException);
            }
        }

        private void Upgrade(IEnumerable<string> parameters) {
            try {
            }
            catch {
            }
        }

        private void Trim(IEnumerable<string> parameters) {
            try {
            }
            catch {
            }
        }

        private void ListFeeds(IEnumerable<string> parameters) {
            try {
                var feeds = _pkgManager.SystemFeedLocations;
                if (feeds.Count() > 0) {
                    Console.WriteLine("Current System Package Repositories");
                    Console.WriteLine("-----------------------------------");

                    foreach (var feed in feeds) {
                        Console.Write("   {0}", feed);
                    }
                }
                else {
                    Console.WriteLine("There are no package repositories currently configured.");
                }
            }
            catch (Exception) {
                throw new ConsoleException(
                    "LOL WHAT?.");
            }
        }

        private void AddFeed(IEnumerable<string> parameters) {
            try {
                if (!AdminPrivilege.IsRunAsAdmin) {
                    throw new ConsoleException(
                        "Admin privilege is required to modify the system feeds. \r\nPlease run as an elevated administrator.");
                }
                _pkgManager.AddSystemFeeds(parameters);
                ListFeeds(parameters);
            }
            catch (Exception) {
                throw new ConsoleException(
                    "LOL WHAT?.");
            }
        }

        private void DeleteFeed(IEnumerable<string> parameters) {
            try {
                if (!AdminPrivilege.IsRunAsAdmin) {
                    throw new ConsoleException(
                        "Admin privilege is required to modify the system feeds. \r\nPlease run as an elevated administrator.");
                }
                _pkgManager.DeleteSystemFeeds(parameters);
                ListFeeds(parameters);
            }
            catch (Exception) {
                throw new ConsoleException(
                    "LOL WHAT?.");
            }
        }
    }
}