//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.CLI {
    using System;
    using System.Collections;
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
    using Toolkit.Tasks;
    using Toolkit.Win32;

    /// <summary>
    ///   Main Program for command line coapp tool
    /// </summary>
    public class CoAppMain : AsyncConsoleProgram {
        private string feedOutputFile = null;
        private string feedRootUrl = null;
        private string feedActualUrl = null;
        private string feedPackageSource = null;
        private bool feedRecursive = false;
        private string feedPackageUrl = null;
        private string feedTitle = null;
        
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
                            feedOutputFile = argumentParameters.LastOrDefault();
                            break;
                        case "feed-root-url":
                            feedRootUrl = argumentParameters.LastOrDefault();
                            break;
                        case "feed-actual-url":
                            feedActualUrl = argumentParameters.LastOrDefault();
                            break;
                        case "feed-package-source":
                            feedPackageSource = argumentParameters.LastOrDefault();
                            break;
                        case "feed-recursive":
                            feedRecursive = true;
                            break;
                        case "feed-package-url":
                            feedPackageUrl = argumentParameters.LastOrDefault();
                            break;
                        case "feed-title":
                            feedTitle = argumentParameters.LastOrDefault();
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
                        getTask.ContinueWith(t => {
                            // this executes when the Get() operation completes.
                            Console.WriteLine("File {0} has finished downloading", remoteFile.LocalFullPath);
                        }, TaskContinuationOptions.AttachedToParent);

                        getTask.Wait();
                        break;

                    case "verify-package":
                        var r = Toolkit.Crypto.Verifier.HasValidSignature(parameters.First());
                        Console.WriteLine("Has Valid Signature: {0}",r);
                        Console.WriteLine("Name: {0}", Toolkit.Crypto.Verifier.GetPublisherInformation(parameters.First())["PublisherName"]);
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

                        CoTask.Factory.StartNew(() => Upgrade(parameters));
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
            Task<IEnumerable<Package>> x = _pkgManager.GetInstalledPackages(new PackageManagerMessages {
                PackageScanning = (progress) => {
                    "Scanning: ".PrintProgressBar(progress);
                }
            });

            var z = x.ContinueWith((antecedent) => {

                var pkgsInstalled = antecedent.Result;
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
            });

            z.Wait();
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

                PackageNotFound = (packageMask) => {
                    Console.WriteLine(Resources.PackageNotFound, packageMask);
                },

                PackageIsNotInstalled = (package) => {
                    Console.WriteLine("The package {0} is not currently installed", package.CosmeticName);
                },
            });

            try {
                task.Wait();
            }
            catch (AggregateException ae) {
                ae.Ignore(typeof (OperationCompletedBeforeResultException));
            }
        }

        private void Install(IEnumerable<string> parameters) {
            if (!AdminPrivilege.IsRunAsAdmin) {
                throw new ConsoleException(
                    "Admin privilege is required to install packages. \r\nPlease run as an elevated administrator.");
            }

            var maxPercent = 0L;
            var t = _pkgManager.InstallPackages(parameters, new PackageManagerMessages {
                InstallingPackage = (package) => {
                    Console.Write("\r\nInstalling: {0}\r", package.CosmeticName);
                    maxPercent = 0;
                },

                InstallProgress =  (package,progress) => {
                    if (progress > maxPercent) {
                        "Installing: {0}".format(package.CosmeticName).PrintProgressBar(progress);
                        maxPercent = progress;
                    }    
                },

                
                
                
                InstallerMessage = (packageInstallerMessage, payload, progress) => {
                    // status
                    var package = payload as Package;
                    switch (packageInstallerMessage) {

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

        private void GenerateFeed(IEnumerable<string> parameters) {
            try {
                Console.WriteLine("...");
                _pkgManager.GenerateAtomFeed(feedOutputFile, feedPackageSource, feedRecursive, feedRootUrl, feedPackageUrl, feedActualUrl,
                    feedTitle);
                Console.WriteLine("...");
            }
            catch(Exception e) { 
                Console.WriteLine("stacktrace: {0}", e.StackTrace);
                throw new ConsoleException("Failed to create Atom feed: {0}",e.Message);
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