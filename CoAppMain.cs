//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.CLI {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Threading;
    using System.Threading.Tasks;
    using Properties;
    using Toolkit.Console;
    using Toolkit.Engine;
    using Toolkit.Engine.Client;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Logging;
    using Toolkit.Network;

    /// <summary>
    /// Main Program for command line coapp tool
    /// </summary>
    /// <remarks></remarks>
    public class CoAppMain : AsyncConsoleProgram {
        private bool _terse = false;
        private bool _verbose = false;

        private ulong? _minVersion = null;
        private ulong? _maxVersion = null;

        private bool? _installed = null;
        private bool? _active = null;
        private bool? _required = null;
        private bool? _blocked = null;
        private bool? _latest = null;
        private bool? _force = null;
        private bool? _forceScan = null;
        private string _location = null;
        private bool? _pause = null;
        private bool? _dependencies = null;
        private bool? _download = null;
        private bool? _pretend = null;
        private bool? _autoUpgrade = null;

        private PackageManagerMessages _messages;

        /// <summary>
        /// Gets the res.
        /// </summary>
        /// <remarks></remarks>
        protected override ResourceManager Res {
            get { return Resources.ResourceManager; }
        }

        /// <summary>
        /// Main entrypoint for CLI.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>int value representing the ERRORLEVEL.</returns>
        /// <remarks></remarks>coapp.service
        private static int Main(string[] args) {
            if( System.Diagnostics.Debugger.IsAttached) {
                // wait for service to start if we're attached to the debugger
                // (it could be starting still)
                Thread.Sleep(1000);
            }
            return new CoAppMain().Startup(args);
        }

        /// <summary>
        /// The (non-static) startup method
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>Process return code.</returns>
        /// <remarks></remarks>
        protected override int Main(IEnumerable<string> args) {
            _messages = new PackageManagerMessages {
                UnexpectedFailure = UnexpectedFailure,
                NoPackagesFound = NoPackagesFound,
                PermissionRequired = OperationRequiresPermission,
                Error = MessageArgumentError,
                RequireRemoteFile = (canonicalName, remoteLocations, localFolder, force ) => Downloader.GetRemoteFile(canonicalName, remoteLocations, localFolder, force, new RemoteFileMessages {
                    Progress = (itemUri, percent) => {
                        "Downloading {0}".format(itemUri.AbsoluteUri).PrintProgressBar(percent);
                    }, 
                } ,_messages),
                OperationCancelled = CancellationRequested,
                PackageSatisfiedBy = (original, satisfiedBy) => {
                    original.SatisfiedBy = satisfiedBy;
                },
                PackageBlocked = BlockedPackage,
                UnknownPackage = UnknownPackage,
            };

            try {
                #region command line parsing

                var options = args.Where(each => each.StartsWith("--")).Switches();
                var parameters = args.Where(each => !each.StartsWith("--")).Parameters();

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];
                    var last = argumentParameters.LastOrDefault();
                    var lastAsBool = string.IsNullOrEmpty(last) || last.IsTrue();

                    switch (arg) {
                            /* options  */
                        case "min-version":
                            _minVersion = last.VersionStringToUInt64();
                            break;

                        case "max-version":
                            _maxVersion = last.VersionStringToUInt64();
                            break;

                        case "installed":
                            _installed = lastAsBool;
                            break;

                        case "active":
                            _active = lastAsBool;
                            break;

                        case "required":
                            _required = lastAsBool;
                            break;

                        case "blocked":
                            _blocked = lastAsBool;
                            break;

                        case "latest":
                            _latest = lastAsBool;
                            break;

                        case "force":
                            _force = lastAsBool;
                            break;

                        case "force-scan":
                            _forceScan = lastAsBool;
                            break;

                        case "download":
                            _download = lastAsBool;
                            break;

                        case "pretend":
                            _pretend= lastAsBool;
                            break;

                        case "auto-upgrade":
                            _autoUpgrade = lastAsBool;
                            break;

                        case "use-feed":
                            _location = last;
                            break;

                        case "pause":
                            _pause = lastAsBool;
                            break;

                        case "verbose":
                            _verbose = lastAsBool;
                            Logger.Errors = true;
                            Logger.Messages = true;
                            Logger.Warnings = true;
                            break;

                        case "dependencies":
                        case "deps":
                            _dependencies = lastAsBool;
                            break;

                            /* global switches */
                        case "load-config":
                            // all ready done, but don't get too picky.
                            break;

                        case "nologo":
                            this.Assembly().SetLogo(string.Empty);
                            break;

                        case "terse":
                            this.Assembly().SetLogo(string.Empty);
                            _terse = true;
                            _verbose = false;
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

                Task task = null;
                var command = parameters.FirstOrDefault();
                parameters = parameters.Skip(1);

                if (ConsoleExtensions.InputRedirected) {
                    // grab the contents of the input stream and use that as parameters
                    var lines = Console.In.ReadToEnd().Split(new[] {
                        '\r', '\n'
                    }, StringSplitOptions.RemoveEmptyEntries).Select(each => each.Split(new[] {
                        '#'
                    }, StringSplitOptions.RemoveEmptyEntries)[0]).Select(each => each.Trim());

                    parameters = parameters.Union(lines.Where(each => !each.StartsWith("#"))).ToArray();
                }

                if (ConsoleExtensions.OutputRedirected) {
                    this.Assembly().SetLogo(string.Empty);
                    _terse = true;
                    _verbose = false;
                }

                if (command.IsNullOrEmpty()) {
                    return Help();
                }


                Verbose("# Contacting Service...");
                PackageManager.Instance.Connect("command-line-client", "garrett");

                Verbose("# Waiting for service to respond...");
                if (!PackageManager.Instance.IsReady.WaitOne(5000)) {
                    Verbose("# not connected...");
                    throw new ConsoleException("# Unable to connect to CoApp Service.");
                }

                if( _verbose) {
                    PackageManager.Instance.SetLogging(true, true, true);
                }

                Verbose("# Connected to Service...");

                if (command.EndsWith(".msi") && File.Exists(command) && parameters.IsNullOrEmpty()) {
                    // assume install if the only thing given is a filename.
                    task =
                        PackageManager.Instance.GetPackages(command, _minVersion, _maxVersion, _dependencies , _installed, _active, _required, _blocked, _latest, _location, _forceScan, messages: _messages).
                            ContinueWith(antecedent => Install(antecedent.Result));
                    return 0;
                }

                if (!command.StartsWith("-")) {
                    command = command.ToLower();
                }

                switch (command) {
#if FALSE
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
                            },TaskContinuationOptions.AttachedToParent);
                            break;

                        case "verify-package":
                            var r = Verifier.HasValidSignature(parameters.First());
                            Console.WriteLine("Has Valid Signature: {0}", r);
                            Console.WriteLine("Name: {0}", Verifier.GetPublisherInformation(parameters.First())["PublisherName"]);
                            break;
#endif

                    case "-l":
                    case "list":
                    case "list-package":
                    case "list-packages":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, _installed, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages).ContinueWith(antecedent => ListPackages(antecedent.Result));
                        break;

                    case "-i":
                    case "install":
                    case "install-package":
                    case "install-packages":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.InstallRequiresPackageName);
                        }

                        // if you haven't specified this, we're gonna assume that you want the latest version
                        // this is overridden if the user specifies a version tho'
                        if( _latest == null ) {
                            _latest = true;
                        }

                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, _installed, _active, _required, _blocked, _latest, _location, _forceScan, messages: _messages).
                                ContinueWith(antecedent => Install(antecedent.Result));
                        break;

                    case "-r":
                    case "remove":
                    case "uninstall":
                    case "remove-package":
                    case "remove-packages":
                    case "uninstall-package":
                    case "uninstall-packages":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.RemoveRequiresPackageName);
                        }
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion,_dependencies, true,  _active, _required, _blocked, _latest,_location,_forceScan,  messages: _messages).
                                ContinueWith(antecedent => Remove(antecedent.Result));

                        break;

                    case "-L":
                    case "feed":
                    case "feeds":
                    case "list-feed":
                    case "list-feeds":
                        task = ListFeeds();
                        break;

                    case "-u":
                    case "upgrade":
                    case "upgrade-package":
                    case "upgrade-packages":
                    case "update":
                    case "update-package":
                    case "update-packages":
                        if (parameters.Count() != 1) {
                            throw new ConsoleException(Resources.MissingParameterForUpgrade);
                        }

                        // if they didn't say to rescan (one way or the other), we're defaulting to yeah,
                        // because we really should force a rescan if we're updating 
                        if( _forceScan == null ) {
                            _forceScan = true;
                        }

                        // should get all packages that are installed (using criteria),
                        // and then see if each one of those can be upgraded.
                        
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, true, _active, _required, false, _latest ,_location,_forceScan,  messages: _messages).
                                ContinueWith(antecedent => Upgrade(antecedent.Result));
                        break;

                    case "-A":
                    case "add-feed":
                    case "add-feeds":
                    case "add":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.AddFeedRequiresLocation);
                        }
                        task = AddFeed(parameters);
                        break;

                    case "-R":
                    case "remove-feed":
                    case "remove-feeds":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.DeleteFeedRequiresLocation);
                        }
                        task = DeleteFeed(parameters);
                        break;

                    case "-t":
                    case "trim-packages":
                    case "trim-package":
                    case "trim":
                        if (parameters.Count() != 0) {
                            throw new ConsoleException(Resources.TrimErrorMessage);
                        }
                        task = PackageManager.Instance.GetPackages("*", _minVersion, _maxVersion, _dependencies, true, _active, false, _blocked, _latest, _location,
                            _forceScan, messages: _messages).ContinueWith(antecedent => Remove(antecedent.Result));
                        break;

                    case "-a":
                    case "activate":
                    case "activate-package":
                    case "activate-packages":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, true, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages).
                                ContinueWith(antecedent => Activate(antecedent.Result));

                        break;

                    case "-g":
                    case "get-packageinfo":
                    case "info":
                        task = PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, _installed, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages).ContinueWith(antecedent => GetPackageInfo(antecedent.Result));
                        break;

                    case "-b":
                    case "block-packages":
                    case "block-package":
                    case "block":
                        task =PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, true, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages). ContinueWith(antecedent => Block(antecedent.Result));

                        break;

                    case "-B":
                    case "unblock-packages":
                    case "unblock-package":
                    case "unblock":
                        task = PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, true, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages).ContinueWith(antecedent => UnBlock(antecedent.Result));

                        break;

                    case "-m":
                    case "mark-packages":
                    case "mark-package":
                    case "mark":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, true, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages).
                                ContinueWith(antecedent => Mark(antecedent.Result));
                        break;

                    case "-M":
                    case "unmark-packages":
                    case "unmark-package":
                    case "unmark":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _dependencies, true, _active, _required, _blocked, _latest, _location,_forceScan,  messages: _messages).
                                ContinueWith(antecedent => UnMark(antecedent.Result));
                        break;

                    default:
                        throw new ConsoleException(Resources.UnknownCommand, command);
                }

                if (task != null) {
                    task.ContinueWith(antecedent => {
                        if (!(antecedent.IsFaulted || antecedent.IsCanceled)) {
                            WaitForPackageManagerToComplete();
                        }
                    }).Wait();
                }

                if( _pause == true ) {
                    Console.ReadLine();
                }
                PackageManager.Instance.Disconnect();
                if (_pause == true) {
                    Console.ReadLine();
                }

            }
            catch (ConsoleException failure) {
                CancellationTokenSource.Cancel();
                PackageManager.Instance.Disconnect();
                Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
            }
            
            return 0;
        }

        private void UnknownPackage(string canonicalName) {
            Console.WriteLine("Unknown Package {0}", canonicalName);
        }

        private void BlockedPackage(string canonicalName) {
            Console.WriteLine("Package {0} is blocked", canonicalName);
        }

        private Task AddFeed(IEnumerable<string> feeds) {
            var tasks = feeds.Select(each => PackageManager.Instance.AddFeed(each, false, new PackageManagerMessages {
                FeedAdded = (f) => { Console.WriteLine("Adding Feed: {0}", f); }
            }.Extend(_messages))).ToArray();

            return Task.Factory.ContinueWhenAll(tasks, antecdent => {
                // 
            }, TaskContinuationOptions.AttachedToParent);
        }

        private Task DeleteFeed(IEnumerable<string> feeds) {
            var tasks = feeds.Select(each => PackageManager.Instance.RemoveFeed(each, false, new PackageManagerMessages {
                FeedRemoved = (f) => { Console.WriteLine("Feed Removed: {0}", f); }
            }.Extend(_messages))).ToArray();

            return Task.Factory.ContinueWhenAll(tasks, antecdent => {
                // 
            }, TaskContinuationOptions.AttachedToParent);
        }

        private object UnMark(IEnumerable<Package> packages) {
            if (packages.Any()) {
                var remoteTasks = packages.Select(package => PackageManager.Instance.SetPackage(package.CanonicalName,required: false)).ToArray();
                return Task.Factory.ContinueWhenAll(remoteTasks, antecedents => { });
            }
            return null;
        }

        private object Mark(IEnumerable<Package> packages) {
            if (packages.Any()) {
                var remoteTasks = packages.Select(package => PackageManager.Instance.SetPackage(package.CanonicalName,required: true)).ToArray();
                return Task.Factory.ContinueWhenAll(remoteTasks, antecedents => { });
            }
            return null;
        }

        private object UnBlock(IEnumerable<Package> packages) {
            if (packages.Any()) {
                var remoteTasks = packages.Select(package => PackageManager.Instance.SetPackage(package.CanonicalName, blocked: false)).ToArray();
                return Task.Factory.ContinueWhenAll(remoteTasks, antecedents => { });
            }
            return null;
        }

        private object Block(IEnumerable<Package> packages) {
            if (packages.Any()) {
                var remoteTasks = packages.Select(package => PackageManager.Instance.SetPackage(package.CanonicalName, blocked: true)).ToArray();
                return Task.Factory.ContinueWhenAll(remoteTasks, antecedents => { });
            }
            return null;
        }

        private Task Activate(IEnumerable<Package> packages) {
            if (packages.Any()) {
                var remoteTasks = packages.Select(package => PackageManager.Instance.SetPackage(package.CanonicalName, active: true)).ToArray();
                return Task.Factory.ContinueWhenAll(remoteTasks, antecedents => { });
            }
            return null;
        }

        private Task GetPackageInfo(IEnumerable<Package> packages) {
            if(packages.Any()) {
                var remoteTasks = packages.Select(package => PackageManager.Instance.GetPackageDetails(package.CanonicalName, _messages )).ToArray();
                return Task.Factory.ContinueWhenAll(remoteTasks, antecedents => {

                var length0 = packages.Max(each => Math.Max( Math.Max(each.Name.Length, each.Architecture.Length), each.PublisherName.Length)) + 1;
                var length1 = packages.Max(each =>  Math.Max( Math.Max( each.Version.Length,each.AuthorVersion.Length),each.PublisherUrl.Length) )+1;

                foreach (var package in packages) {
                    var date = DateTime.FromFileTime(long.Parse(package.PublishDate));
                    Console.WriteLine("-----------------------------------------------------------");
                    Console.WriteLine("Package: {0}", package.DisplayName);
                    Console.WriteLine("  Name: {{0,-{0}}}      Architecture:{{1,-{1}}} ".format(length0, length1), package.Name, package.Architecture);
                    Console.WriteLine("  Version: {{0,-{0}}}   Author Version:{{1,-{1}}} ".format(length0, length1), package.Version, package.AuthorVersion);
                    Console.WriteLine("  Published:{0}", date.ToShortDateString());
                    Console.WriteLine("  Local Path:{0}", package.LocalPackagePath);
                    Console.WriteLine("  Publisher: {{0,-{0}}} Location:{{1,-{1}}} ".format(length0, length1), package.PublisherName, package.PublisherUrl);
                    Console.WriteLine("  Installed: {0,-6} Blocked:{1,-6} Required:{2,-6} Active:{3,-6}", package.IsInstalled, package.IsBlocked,
                        package.IsRequired, package.IsActive);
                    Console.WriteLine("  Summary: {0}", package.Summary);
                    Console.WriteLine("  Description: {0}", package.Description);
                    Console.WriteLine("  Copyright: {0}", package.Copyright);
                    Console.WriteLine("  License: {0}", package.License);
                    Console.WriteLine("  License URL: {0}", package.LicenseUrl);
                    if (!package.Tags.IsNullOrEmpty()) {
                        Console.WriteLine("  Tags: {0}", package.Tags.Aggregate((current, each) => current + "," + each));
                    }

                    if (package.RemoteLocations.Any()) {
                        Console.WriteLine("  Remote Locations:");
                        foreach (var location in package.RemoteLocations) {
                            Console.WriteLine("    {0}", location);
                        }
                    }

                    if (package.Dependencies.Any()) {
                        Console.WriteLine("  Package Dependencies:");
                        foreach (var dep in package.Dependencies) {
                            Console.WriteLine("    {0}", dep);
                        }
                    }

                    /*
                        SupercedentPackages 
                        SatisfiedBy;
                     */

                }
                    Console.WriteLine("-----------------------------------------------------------");
                }, TaskContinuationOptions.AttachedToParent);
            }
            return null;
        }

        private Task Upgrade(IEnumerable<Package> packages) {
            if (packages.Any()) {
                var upgrades = new List<Package>();
                
                var anyUpgrades = packages.Where(each => each.IsClientRequired).Select(package => PackageManager.Instance.FindPackages(null, package.Name, null, package.Architecture, package.PublicKeyToken, installed: false, blocked: false, latest: true, messages: new PackageManagerMessages {
                    PackageInformation = (pkg) => {
                        if (pkg.Version.VersionStringToUInt64() > package.Version.VersionStringToUInt64()) {
                            upgrades.Add(pkg);
                        }
                    }
                }.Extend(_messages)));

                var policyUpgrades = packages.Where(each => each.IsDependency).Select(package => PackageManager.Instance.FindPackages(null, package.Name, null, package.Architecture, package.PublicKeyToken, installed: false, blocked: false, latest: true, messages: new PackageManagerMessages {
                    PackageInformation = (pkg) => {
                        if (!pkg.SupercedentPackages.IsNullOrEmpty()) {
                            var pkgToAdd = pkg.SupercedentPackages.Select(Package.GetPackage).MaxElement(each => each.Version.VersionStringToUInt64());
                            if (pkgToAdd.Version.VersionStringToUInt64() > package.Version.VersionStringToUInt64()) {
                                upgrades.Add(pkgToAdd);
                            }
                        }
                    }
                }.Extend(_messages)));

                return Task.Factory.ContinueWhenAll(anyUpgrades.Union(policyUpgrades).ToArray(), antecedent => {
                    _autoUpgrade = true;
                    Install(upgrades);
                }, TaskContinuationOptions.AttachedToParent);
            }
            return null;
        }

        private void WaitForPackageManagerToComplete() {
            // wait for cancellation token, or service to disconnect
            WaitHandle.WaitAny(new[] {
                CancellationTokenSource.Token.WaitHandle, PackageManager.Instance.IsDisconnected, PackageManager.Instance.IsCompleted
            });
        }

        /// <summary>
        /// Lists the packages.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <remarks></remarks>
        private void ListPackages(IEnumerable<Package> packages) {
            if (_terse) {
                foreach (var package in packages) {
                    Console.WriteLine("{0} # Installed:{1}", package.CanonicalName, package.IsInstalled);
                }
            }
            else if (packages.Any()) {
                (from pkg in packages
                    orderby pkg.Name
                    select new {
                        pkg.Name,
                        Version = pkg.Version,
                        Arch = pkg.Architecture,
                        Status = (pkg.IsInstalled ? "Installed " + (pkg.IsBlocked ? "Blocked " : "") + (pkg.IsClientRequired ? "Required ": pkg.IsRequired ? "Dependency " : "")+ (pkg.IsActive ? "Active " : "" ) : ""),
                        Location = pkg.IsInstalled ? "(installed)" : !string.IsNullOrEmpty(pkg.LocalPackagePath) ? pkg.LocalPackagePath : (pkg.RemoteLocations.IsNullOrEmpty() ? "<unknown>" :  pkg.RemoteLocations.FirstOrDefault()),
                    }).ToTable().ConsoleOut();
            }
            else {
                Console.WriteLine("No packages found.");
            }
        }

        private void CancellationRequested(string obj) {
            Console.WriteLine("Cancellation Requested.");
        }

        private void MessageArgumentError(string arg1, string arg2, string arg3) {
            Console.WriteLine("Message Argument Error {0}, {1}, {2}.", arg1, arg2, arg3);
        }

        private void OperationRequiresPermission(string policyName) {
            Console.WriteLine("Operation requires permission Policy:{0}", policyName);
        }

        private void NoPackagesFound() {
            Console.WriteLine("Did not find any packages.");
        }

        private void UnexpectedFailure(Exception obj) {
            throw new ConsoleException("SERVER EXCEPTION: {0}\r\n{1}", obj.Message, obj.StackTrace);
        }

        private Task ListFeeds() {
            return PackageManager.Instance.ListFeeds(null, null, new PackageManagerMessages {
                NoFeedsFound = () => { Console.WriteLine("No Feeds Found."); },
                FeedDetails = (location, lastScanned, isSession, isSuppressed, isValidated) => {
                    Console.WriteLine("Feed: {0}", location);
                }
            }.Extend(_messages));
        }

        /// <summary>
        /// Removes the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <remarks></remarks>
        private void Remove(IEnumerable<Package> parameters) {
            var removedList = new List<string>();
            var failedList = new List<string>();

            foreach( var package in parameters ) {
                PackageManager.Instance.RemovePackage(package.CanonicalName, _force, new PackageManagerMessages {
                    RemovingPackageProgress= (canonicalName, progress) => {
                        // installation progress
                        ConsoleExtensions.PrintProgressBar("Removing {0}".format(canonicalName), progress);
                    },
                    RemovedPackage = (canonicalName) => {
                        // completed install of package 
                        removedList.Add(canonicalName);
                         Console.WriteLine();
                    },
                    FailedPackageInstall = (canonicalName, filename, reason) => {
                        // failed install of package 
                        failedList.Add(canonicalName);
                         Console.WriteLine();
                    },
                    PackageBlocked= (canonicalName) => {
                        // failed install of package 
                        failedList.Add(canonicalName);
                    },
                }).Wait();
            }
        }

        /// <summary>
        /// Installs the packages specified.  
        /// 
        /// 
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <remarks></remarks>
        private void Install(IEnumerable<Package> packages) {
            
            var failed = false;
            foreach (var conflicts in packages.Select(package => packages.Where(each => each.Name == package.Name && each.PublicKeyToken == package.PublicKeyToken).ToArray()).Where(conflicts => conflicts.Count() > 1 && !conflicts.FirstOrDefault().IsConflicted)) {
                failed = true;
                // there are conflicting duplicates of a package in here. 
                // tell the user and bail.
                Console.WriteLine("A conflict exists between the following packages:");
                foreach (var conflict in conflicts) {
                    conflict.IsConflicted = true;
                    Console.WriteLine("   {0}", conflict.CanonicalName);
                }
            }

            if (!failed) {
                if (packages.Any()) {
                    Console.WriteLine("Packages to install:\r\n");
                    
                    var pretendList = new List<Package>();
                    foreach (var p in packages) {
                        var package = p;

                        PackageManager.Instance.InstallPackage(package.CanonicalName, _autoUpgrade, _force, _download, true, new PackageManagerMessages {
                            PackageInformation = (pkg) => {
                                // pretending to install package pkg
                                pretendList.Add(pkg);
                            },

                            PackageSatisfiedBy = (pkg, satisfiedBy) => {
                                pkg.SatisfiedBy = satisfiedBy;
                                pretendList.Add(pkg);
                            },
                        }.Extend(_messages)).Wait();
                    }

                    (from pkg in pretendList.Distinct().Where(each => !each.IsInstalled)
                        let getsSatisfied = !(pkg.SatisfiedBy == null || pkg.SatisfiedBy == pkg)
                        orderby pkg.Name
                        select new {
                            pkg.Name,
                            Version = pkg.Version,
                            Arch = pkg.Architecture,
                            Type = getsSatisfied ? "(superceded)" : packages.Contains(pkg) ? "Requested" : "Dependency",
                            Location = getsSatisfied ? "Satisfied by {0}".format(pkg.SatisfiedBy.CanonicalName) : !string.IsNullOrEmpty(pkg.LocalPackagePath) ?pkg.LocalPackagePath :  (pkg.RemoteLocations.IsNullOrEmpty() ? "<unknown>" :  pkg.RemoteLocations.FirstOrDefault())
                            // Satisfied_By = getsSatisfied ? "" : pkg.SatisfiedBy.CanonicalName ,
                            // Satisfied_By = pkg.SatisfiedBy == null ? pkg.CanonicalName : pkg.SatisfiedBy.CanonicalName ,
                            // Status = pkg.IsInstalled ? "Installed" : "will install",
                        }).OrderBy(each => each.Type ).ToTable().ConsoleOut();
                }
                Console.WriteLine();

                if( _pretend == true ) {
                    return;
                }

                // now, each package in the list can be installed
                var installedList= new List<string>();
                var failedList = new List<string>();
                
                foreach( var p in packages ) {
                    var package = p;

                    PackageManager.Instance.InstallPackage(package.CanonicalName, _autoUpgrade, _force, _download, _pretend, new PackageManagerMessages {
                        InstallingPackageProgress = (canonicalName, progress, overallProgress ) => {
                            // installation progress
                            ConsoleExtensions.PrintProgressBar("Installing: {0}".format(canonicalName), progress);
                        },

                        InstalledPackage = (canonicalName) => {
                            // completed install of package 
                            installedList.Add(canonicalName);
                            Console.WriteLine();
                        }, 

                        FailedPackageInstall = (canonicalName, filename , reason ) => {
                            // failed install of package 
                            failedList.Add(canonicalName);

                            if (packages.Contains(Package.GetPackage(canonicalName))) {
                                Console.WriteLine("\r\nNOTE: Requested package {0} failed to install [{1}]", canonicalName, reason);
                            }
                            else {
                                Console.WriteLine("\r\nNOTE: Dependent package {0} failed to install [{1}]", canonicalName, reason);
                                Console.WriteLine("    (attempting to find alternative)");
                            }
                        },
                    }.Extend(_messages)).Wait();
                    
                }
            }
        }

        private void Verbose(string text, params object[] objs) {
            if (true == _verbose) {
                Console.WriteLine(text.format(objs));
            }
        }
    }
}