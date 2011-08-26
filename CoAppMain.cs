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
    using System.Diagnostics;
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
        private bool? _pause = null;

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
                RequireRemoteFile = GetRemoteFile,
                OperationCancelled = CancellationRequested,
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

                        case "pause":
                            _pause = lastAsBool;
                            break;

                        case "verbose":
                            _verbose = lastAsBool;
                            break;

                            /* global switches */
                        case "load-config":
                            // all ready done, but don't get too picky.
                            break;

                        case "nologo":
                            this.Assembly().SetLogo(string.Empty);
                            break;

                        case "terse":
                            _terse = true;
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
                    _terse = true;
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

                Verbose("# Connected to Service...");

                if (command.EndsWith(".msi") && File.Exists(command) && parameters.IsNullOrEmpty()) {
                    // assume install if the only thing given is a filename.
                    task =
                        PackageManager.Instance.GetPackages(command, _minVersion, _maxVersion, _installed, _active, _required, _blocked, _latest, _messages).
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
                    case "-i":
                    case "install":
                    case "install-package":
                    case "install-packages":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.InstallRequiresPackageName);
                        }

                        task =
                            PackageManager.Instance.GetPackages(command, _minVersion, _maxVersion, false, _active, _required, _blocked, _latest, _messages).
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
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
                                ContinueWith(antecedent => Remove(antecedent.Result));

                        break;

                    case "-l":
                    case "list":
                    case "list-package":
                    case "list-packages":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _installed, _active, _required, _blocked, _latest,
                                _messages).ContinueWith(antecedent => ListPackages(antecedent.Result));
                        break;

                    case "-L":
                    case "feed":
                    case "feeds":
                    case "list-feed":
                    case "list-feeds":
                        ListFeeds();
                        break;

                    case "-u":
                    case "upgrade":
                    case "upgrade-package":
                    case "upgrade-packages":
                        if (parameters.Count() != 1) {
                            throw new ConsoleException(Resources.MissingParameterForUpgrade);
                        }
                        // should get all packages that are installed (using criteria),
                        // and then see if each one of those can be upgraded.

                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
                                ContinueWith(antecedent => Upgrade(antecedent.Result));
                        break;

                    case "-A":
                    case "add-feed":
                    case "add-feeds":
                    case "add":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.AddFeedRequiresLocation);
                        }
                        // AddFeed(parameters);
                        break;

                    case "-R":
                    case "remove-feed":
                    case "remove-feeds":
                        if (parameters.Count() < 1) {
                            throw new ConsoleException(Resources.DeleteFeedRequiresLocation);
                        }
                        //DeleteFeed(parameters);
                        break;

                    case "-t":
                    case "trim-packages":
                    case "trim-package":
                    case "trim":
                        if (parameters.Count() != 0) {
                            throw new ConsoleException(Resources.TrimErrorMessage);
                        }


                        // Trim();
                        break;


                    case "-a":
                    case "activate":
                    case "activate-package":
                    case "activate-packages":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
                                ContinueWith(antecedent => Activate(antecedent.Result));

                        // activate(Parameters)
                        break;

                    case "-g":
                    case "get-packageinfo":
                    case "info":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, _installed, _active, _required, _blocked, _latest,
                                _messages).ContinueWith(antecedent => GetPackageInfo(antecedent.Result));

                        break;

                    case "-b":
                    case "block-packages":
                    case "block-package":
                    case "block":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
                                ContinueWith(antecedent => Block(antecedent.Result));

                        break;

                    case "-B":
                    case "unblock-packages":
                    case "unblock-package":
                    case "unblock":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
                                ContinueWith(antecedent => UnBlock(antecedent.Result));

                        break;

                    case "-m":
                    case "mark-packages":
                    case "mark-package":
                    case "mark":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
                                ContinueWith(antecedent => Mark(antecedent.Result));
                        break;

                    case "-M":
                    case "unmark-packages":
                    case "unmark-package":
                    case "unmark":
                        task =
                            PackageManager.Instance.GetPackages(parameters, _minVersion, _maxVersion, true, _active, _required, _blocked, _latest, _messages).
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

                PackageManager.Instance.Disconnect();
            }
            catch (ConsoleException failure) {
                CancellationTokenSource.Cancel();
                PackageManager.Instance.Disconnect();
                Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
            }

            // Process.Start("pskill.exe", "coapp.service");
            return 0;
        }

        private object UnMark(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private object Mark(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private object UnBlock(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private object Block(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private object GetPackageInfo(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private object Activate(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private object Upgrade(IEnumerable<Package> iEnumerable) {
            throw new NotImplementedException();
        }

        private void WaitForPackageManagerToComplete() {
            // wait for cancellation token, or service to disconnect
            WaitHandle.WaitAny(new[] {
                CancellationTokenSource.Token.WaitHandle, PackageManager.Instance.IsDisconnected, PackageManager.Instance.IsCompleted
            });
        }

        /*
        /// <summary>
        /// Dumps the packages.
        /// </summary>
        /// <param name="packages">The packages.</param>
        /// <remarks></remarks>
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
        */




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
                        Installed = pkg.IsInstalled,
                        Local_Path = pkg.IsInstalled ? "(installed)" : pkg.LocalPackagePath ?? "<not local>",
                        // Remote_Location = pkg.RemoteLocation.Value != null ? pkg.RemoteLocation.Value.AbsoluteUri : "<unknown>"
                    }).ToTable().ConsoleOut();
            }
            else {
                Console.WriteLine("No packages found.");
            }

            /*
            var packages = new List<Package>();
            var t = PackageManager.Instance.FindPackages(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, new PackageManagerMessages {
                UnexpectedFailure = UnexpectedFailure,
                PackageInformation = (package) => {
                    if( terse ) {
                        Console.WriteLine("{0} # Installed:{1}", package.CanonicalName, package.IsInstalled);
                    } else {
                        packages.Add(package);
                    }
                },
                NoPackagesFound = NoPackagesFound,
                PermissionRequired = OperationRequiresPermission,
                Error = MessageArgumentError,
                RequireRemoteFile = GetRemoteFile,
                OperationCancelled = CancellationRequested,
            });
            if (!terse) {
                t.ContinueWith(antecedent => {
                    if (packages.Count() > 0) {
                        (from pkg in packages
                            orderby pkg.Name
                            select new {
                                pkg.Name,
                                Version = pkg.Version,
                                Arch = pkg.Architecture,
                                Installed = pkg.IsInstalled,
                                Local_Path = pkg.IsInstalled ? "(installed)" : pkg.LocalPackagePath ?? "<not local>",
                                // Remote_Location = pkg.RemoteLocation.Value != null ? pkg.RemoteLocation.Value.AbsoluteUri : "<unknown>"
                            }).ToTable().ConsoleOut();
                    }
                }, TaskContinuationOptions.AttachedToParent);
            } */
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

        private void ListFeeds() {
            PackageManager.Instance.ListFeeds(null, null, new PackageManagerMessages {
                RequireRemoteFile = GetRemoteFile,
                NoFeedsFound = () => { Console.WriteLine("No Feeds Found."); },
                FeedDetails = (location, lastScanned, isSession, isSuppressed, isValidated) => {
                    Console.Write("HI!");
                    Console.WriteLine("FEED: {0}", location);
                }
            });
        }

        private void GetRemoteFile(string canonicalName, IEnumerable<string> arg2, string arg3, bool arg4) {
            PackageManager.Instance.UnableToAcquire(canonicalName, new PackageManagerMessages());
        }

        /// <summary>
        /// Removes the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <remarks></remarks>
        private void Remove(IEnumerable<Package> parameters) {



        }

        /// <summary>
        /// Installs the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <remarks></remarks>
        private void Install(IEnumerable<Package> parameters) {

        }

        private void Verbose(string text, params object[] objs) {
            if (true == _verbose) {
                Console.WriteLine(text.format(objs));
            }
        }
    }
}