//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.CLI {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Threading;
    using Properties;
    using Toolkit.Console;
    using Toolkit.Crypto;
    using Toolkit.Engine;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Network;
    using Toolkit.Tasks;
    using Toolkit.Win32;

    /// <summary>
    ///   Main Program for command line coapp tool
    /// </summary>
    public class CoAppMain : AsyncConsoleProgram {
        private string _feedOutputFile;
        private string _feedRootUrl;
        private string _feedActualUrl;
        private string _feedPackageSource;
        private bool _feedRecursive;
        private string _feedPackageUrl;
        private string _feedTitle;

        private PackageManager _pkgManager;

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
                _pkgManager = new PackageManager();

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

                        case "feed-output-file":
                            _feedOutputFile = argumentParameters.LastOrDefault();
                            break;
                        case "feed-root-url":
                            _feedRootUrl = argumentParameters.LastOrDefault();
                            break;
                        case "feed-actual-url":
                            _feedActualUrl = argumentParameters.LastOrDefault();
                            break;
                        case "feed-package-source":
                            _feedPackageSource = argumentParameters.LastOrDefault();
                            break;
                        case "feed-recursive":
                            _feedRecursive = true;
                            break;
                        case "feed-package-url":
                            _feedPackageUrl = argumentParameters.LastOrDefault();
                            break;
                        case "feed-title":
                            _feedTitle = argumentParameters.LastOrDefault();
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

                // GS01: I'm putting this in here so that feed resoltion happens before we actually get around to doing something. 
                // Look into the necessity later.
                Tasklet.WaitforCurrentChildTasks();

                var command = parameters.FirstOrDefault().ToLower();
                parameters = parameters.Skip(1);

                if (File.Exists(command)) {
                    // assume install if the only thing given is a filename.
                    Install(command.SingleItemAsEnumerable());
                }
                else {
                    switch (command) {
                        case "download":
                            var remoteFileUri = new Uri(parameters.First());
                            // create the remote file reference 
                            var remoteFile = new RemoteFile(remoteFileUri, CancellationTokenSource.Token);
                            var previewTask = remoteFile.Preview();
                            previewTask.Wait();
                            // Tell it to download it 
                            var getTask = remoteFile.Get();

                            // monitor the progress.
                            remoteFile.DownloadProgress.Notification += progress => {
                                // this executes when the download progress value changes. 
                                Console.Write(progress <= 100 ? "..{0}% " : "bytes: [{0}]", progress);
                            };

                            // when it's done, do this:
                            getTask.ContinueWithParent(t => {
                                // this executes when the Get() operation completes.
                                Console.WriteLine("File {0} has finished downloading", remoteFile.LocalFullPath);
                            });
                            break;

                        case "verify-package":
                            var r = Verifier.HasValidSignature(parameters.First());
                            Console.WriteLine("Has Valid Signature: {0}", r);
                            Console.WriteLine("Name: {0}", Verifier.GetPublisherInformation(parameters.First())["PublisherName"]);
                            break;

                        case "install":
                            if (parameters.Count() < 1) {
                                throw new ConsoleException(Resources.InstallRequiresPackageName);
                            }

                            Tasklet.WaitforCurrentChildTasks(); // HACK HACK HACK ???

                            Install(parameters);
                            break;

                        case "remove":
                            if (parameters.Count() < 1) {
                                throw new ConsoleException(Resources.RemoveRequiresPackageName);
                            }
                            Remove(parameters);
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
                                case "repo":
                                case "repos":
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

                            Upgrade(parameters);
                            break;

                        case "add":
                            if (parameters.Count() < 1) {
                                throw new ConsoleException(Resources.AddFeedRequiresLocation);
                            }
                            CoTask.Factory.StartNew(() => AddFeed(parameters));
                            break;

                        case "delete":
                            if (parameters.Count() < 1) {
                                throw new ConsoleException(Resources.DeleteFeedRequiresLocation);
                            }
                            CoTask.Factory.StartNew(() => DeleteFeed(parameters));
                            break;

                        case "trim":
                            if (parameters.Count() != 0) {
                                throw new ConsoleException(Resources.TrimErrorMessage);
                            }

                            CoTask.Factory.StartNew(() => Trim(parameters));
                            break;

                        case "generate-feed":
                            CoTask.Factory.StartNew(() => GenerateFeed(parameters));
                            break;

                        default:
                            throw new ConsoleException(Resources.UnknownCommand, command);
                    }
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

        private void DumpPackages(IEnumerable<Package> packages) {
            if (packages.Count() > 0) {
                    (from pkg in packages orderby pkg.Name
                        select new {
                            pkg.Name,
                            Version = pkg.Version.UInt64VersiontoString(),
                            Arch = pkg.Architecture,
                            Publisher = pkg.Publisher.Name,
                            // Local_Path = pkg.LocalPackagePath.Value ?? "<not local>",
                            // Remote_Location = pkg.RemoteLocation.Value != null ? pkg.RemoteLocation.Value.AbsoluteUri : "<unknown>"
                        } ).ToTable().ConsoleOut();
            }
            else {
                Console.WriteLine("\rNo packages.");
            }
        }

        private void ListPackages(IEnumerable<string> parameters) {
            var tsk = _pkgManager.GetInstalledPackages(new PackageManagerMessages {
                PackageScanning = (progress) => { "Scanning: ".PrintProgressBar(progress); }
            }).ContinueWithParent((antecedent) => {
                var pkgsInstalled = antecedent.Result;

                " ".PrintProgressBar(-1);
                Console.WriteLine("\r");

                if (pkgsInstalled.Count() > 0) {
                    Console.WriteLine("\rPackages currently installed:");
                    DumpPackages(pkgsInstalled);
                }
                else {
                    Console.WriteLine("\rThere are no packages currently installed.");
                }
            });

            tsk.Wait();
        }

        private void Remove(IEnumerable<string> parameters) {
            if (!AdminPrivilege.IsRunAsAdmin) {
                throw new ConsoleException(
                    "Admin privilege is required to remove packages. \r\nPlease run as an elevated administrator.");
            }

            var maxPercent = 0L;
            var task = _pkgManager.RemovePackages(parameters, new PackageManagerMessages {
                RemovingPackage = (package) => {
                    Console.Write("\r\nRemoving: {0}", package.CosmeticName);
                    maxPercent = 0;
                },
                RemovingProgress = (package, progress) => {
                    if (progress > maxPercent) {
                        "Removing: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                        maxPercent = progress;
                    }
                },
                MultiplePackagesMatch = (packageMask, packages) => {
                    Console.WriteLine(Resources.PackageHasMultipleMatches, packageMask);
                    foreach (var pkg in packages) {
                        Console.WriteLine(@"   {0}", pkg.CosmeticName);
                    }
                },
                PackageRemoveFailed = (package) => {
                    Console.WriteLine("Remove of package {0} failed:", package.CosmeticName);
                    Console.WriteLine("    File: {0} :", package.LocalPackagePath);
                },
                PackageNotFound = (packageMask) => { Console.WriteLine(Resources.PackageNotFound, packageMask); },
                PackageIsNotInstalled =
                    (package) => { Console.WriteLine("The package {0} is not currently installed", package.CosmeticName); },
            });

            try {
                task.Wait();
            }
            catch (AggregateException ae) {
                ae.Ignore(typeof (OperationCompletedBeforeResultException), () => { Console.WriteLine("operation not complete!"); });
            }
        }

        private void Install(IEnumerable<string> parameters) {
            if (!AdminPrivilege.IsRunAsAdmin) {
                throw new ConsoleException(
                    "Admin privilege is required to install packages. \r\nPlease run as an elevated administrator.");
            }

            var maxPercent = 0L;

            var installPackagesTask = _pkgManager.InstallPackages(parameters, new PackageManagerMessages {
                FailedDependentPackageInstall = OnFailedDependentPackageInstall,
                PackageNotFound = OnPackageNotFound,
                PackageHasPotentialUpgrades = OnPackageHasPotentialUpgrades,
                PackageNotSatisfied = OnPackageNotSatisfied,
                MultiplePackagesMatch = OnMultiplePackagesMatch,
                InstallingPackage = (package) => {
                    Console.Write("\r\nInstalling: {0}\r", package.CosmeticName);
                    maxPercent = 0;
                },
                InstallProgress = (package, progress) => {
                    if (progress > maxPercent) {
                        "Installing: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                        maxPercent = progress;
                    }
                },
                DownloadingFile = (remoteFile) => {
                    Console.WriteLine("Downloading:  {0}", remoteFile.ActualRemoteLocation.AbsoluteUri);
                    maxPercent = 0L;
                },
                DownloadingFileProgress = (remoteFile, progress) => {
                    if (progress > maxPercent) {
                        "Downloading: {0}".format(remoteFile.ActualRemoteLocation.AbsoluteUri).PrintProgressBar(progress);
                        maxPercent = progress;
                        // TODO: this probably looks like hell when downloading multiple packages concurrently.
                    }
                }
            });

            try {
                installPackagesTask.Wait();
            }
            catch (AggregateException ae) {
                ae.Ignore(typeof (OperationCompletedBeforeResultException), () => { Console.WriteLine("(Operation not complete)."); });
            }
        }

        private void OnMultiplePackagesMatch(string packageMask, IEnumerable<Package> packages) {
            Console.WriteLine(Resources.PackageHasMultipleMatches, packageMask);
            foreach (var pkg in packages) {
                Console.WriteLine(@"   {0} [{1}]", pkg.CosmeticName,
                    pkg.HasLocalFile
                        ? pkg.LocalPackagePath.Value
                        : pkg.HasRemoteLocation ? pkg.RemoteLocation.Value.AbsoluteUri : "Package Not Found");
            }
        }

        private void OnPackageNotSatisfied(Package package) {
            Console.WriteLine(Resources.PackageDependenciesCantInstall, package.CosmeticName);

            var depth = 0;
            Action<Package> printDepMap = null;
            printDepMap = (p => {
                depth++;
                if (!p.CanSatisfy) {
                    var unsatisfiedDependencies = p.Dependencies.Where(each => !each.CanSatisfy);
                    var count = unsatisfiedDependencies.Count();

                    Console.WriteLine(@"{0}{1} [{2}] [{3}]", "".PadLeft(3*depth), p.CosmeticName,
                        p.HasLocalFile
                            ? p.LocalPackagePath.Value
                            : p.HasRemoteLocation ? p.RemoteLocation.Value.AbsoluteUri : "Package Not Found",
                        count > 0
                            ? Resources.MissingPkgText.format(count, count > 1 ? "dependencies" : "dependency")
                            : p.CouldNotDownload
                                ? Resources.CouldNotDownload
                                : p.PackageFailedInstall ? Resources.FailedToInstall : !p.HasLocalFile ? "No local package file" : "Unknown");
                    foreach (var dp in unsatisfiedDependencies) {
                        printDepMap(dp);
                    }
                }
                depth--;
            });
            printDepMap(package);
        }

        private void OnPackageHasPotentialUpgrades(Package package, IEnumerable<Package> supercedents) {
            // the user hasn't specifically asked us to supercede, yet we know of 
            // potential supercedents. Let's force the user to make a decision.
            Console.WriteLine(Resources.PackageHasPossibleNewerVersion, package);
            Console.WriteLine(Resources.TheFollowingPackageSupercede);
            Console.WriteLine(@"   [Latest] {0}", supercedents.First().CosmeticName);

            foreach (var pkg in supercedents.Skip(1)) {
                Console.WriteLine(@"            {0} ", pkg.CosmeticName);
            }

            Console.WriteLine();
            Console.WriteLine(Resources.AutoAcceptHint, package.CosmeticName);
            Console.WriteLine(Resources.AsSpecifiedHint, package.CosmeticName);
        }

        private void OnPackageNotFound(string packageMask) {
            Console.WriteLine(Resources.PackageNotFound, packageMask);
        }

        private void OnFailedDependentPackageInstall(Package package) {
            // dependent package failed installation.
            Console.WriteLine(Resources.PackageFailedInstall, package.CosmeticName);
        }

        private void Upgrade(IEnumerable<string> parameters) {
            try {
                var maxPercent = 0L;
                _pkgManager.Upgrade(parameters, new PackageManagerMessages {
                    FailedDependentPackageInstall = OnFailedDependentPackageInstall,
                    PackageNotFound = OnPackageNotFound,
                    PackageHasPotentialUpgrades = OnPackageHasPotentialUpgrades,
                    PackageNotSatisfied = OnPackageNotSatisfied,
                    MultiplePackagesMatch = OnMultiplePackagesMatch,
                    InstallingPackage = (package) => {
                        Console.Write("\r\nInstalling: {0}\r", package.CosmeticName);
                        maxPercent = 0;
                    },
                    InstallProgress = (package, progress) => {
                        if (progress > maxPercent) {
                            "Installing: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                            maxPercent = progress;
                        }
                    },
                    DownloadingFile = (remoteFile) => {
                        Console.WriteLine("Downloading:  {0}", remoteFile.ActualRemoteLocation.AbsoluteUri);
                        maxPercent = 0L;
                    },
                    DownloadingFileProgress = (remoteFile, progress) => {
                        if (progress > maxPercent) {
                            "Downloading: {0}".format(remoteFile.ActualRemoteLocation.AbsoluteUri).PrintProgressBar(progress);
                            maxPercent = progress;
                            // TODO: this probably looks like hell when downloading multiple packages concurrently.
                        }
                    },
                    UpgradingPackage = (packageList) => {
                        Console.WriteLine("Packages that can be upgraded:");
                        DumpPackages(packageList);
                    }
                });
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

        private void GenerateFeed(IEnumerable<string> parameters) {
            try {
                Console.WriteLine("...");
                _pkgManager.GenerateAtomFeed(_feedOutputFile, _feedPackageSource, _feedRecursive, _feedRootUrl, _feedPackageUrl,
                    _feedActualUrl,
                    _feedTitle);
                Console.WriteLine("...");
            }
            catch (Exception e) {
                Console.WriteLine("stacktrace: {0}", e.StackTrace);
                throw new ConsoleException("Failed to create Atom feed: {0}", e.Message);
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
            catch (ConsoleException) {
                throw;
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
            catch (ConsoleException) {
                throw;
            }
            catch (Exception) {
                throw new ConsoleException(
                    "LOL WHAT?.");
            }
        }
    }
}