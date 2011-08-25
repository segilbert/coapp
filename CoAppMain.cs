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
        private bool terse = false;

        private ulong? minVersion = null;
        private ulong? maxVersion = null;

        private bool? installed = null;
        private bool? active = null;
        private bool? required = null;
        private bool? blocked = null;
        private bool? latest = null;

        private PackageManagerMessages messages;
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
            // Process.Start("pskill.exe", "coapp.service");
            // Thread.Sleep(500);

            // Process.Start("coapp.service.exe", "--interactive");

            return new CoAppMain().Startup(args);
        }

        /// <summary>
        /// The (non-static) startup method
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>Process return code.</returns>
        /// <remarks></remarks>
        protected override int Main(IEnumerable<string> args) {
            messages = new PackageManagerMessages {
                UnexpectedFailure = UnexpectedFailure,
                NoPackagesFound = NoPackagesFound,
                PermissionRequired = OperationRequiresPermission,
                Error = MessageArgumentError,
                RequireRemoteFile = GetRemoteFile,
                OperationCancelled = CancellationRequested,
            };

            try {
                #region commane line parsing

                var options = args.Where( each => each.StartsWith("--")).Switches();
                var parameters = args.Where(each => !each.StartsWith("--")).Parameters();

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];
                    var last = argumentParameters.LastOrDefault();
                    var lastAsBool = (last ?? "true").IsTrue();

                    switch (arg) {
                        /* options  */
                        case "min-version":
                            minVersion =last.VersionStringToUInt64();
                            break;

                        case "max-version":
                            maxVersion = last.VersionStringToUInt64();
                            break;

                        case "installed":
                            installed = lastAsBool;
                            break;

                        case "active":
                            active = lastAsBool;
                            break;

                        case "required":
                            required = lastAsBool;
                            break;

                        case "blocked":
                            blocked = lastAsBool;
                            break;

                        case "latest":
                            latest = lastAsBool;
                            break;

                            /* global switches */
                        case "load-config":
                            // all ready done, but don't get too picky.
                            break;

                        case "nologo":
                            this.Assembly().SetLogo(string.Empty);
                            break;

                        case "terse":
                            terse = true;
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

                if( ConsoleExtensions.InputRedirected ) {
                    // grab the contents of the input stream and use that as parameters
                    var lines = Console.In.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(each => each.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)[0])
                        .Select(each => each.Trim());
                    
                    parameters = parameters.Union(lines.Where( each => !each.StartsWith("#"))).ToArray();
                }

                if( ConsoleExtensions.OutputRedirected ) {
                    terse = true;
                }

                if( command.IsNullOrEmpty() ) {
                    return Help();
                }

                Console.WriteLine("# Contacting Service...");
                PackageManager.Instance.Connect("command-line-client", "garrett");
                Console.WriteLine("# Waiting for service to respond...");
                if (!PackageManager.Instance.IsReady.WaitOne(5000)) {
                    Console.WriteLine("# not connected...");
                    throw new ConsoleException("# Unable to connect to CoApp Service.");
                }

                Console.WriteLine("# Connected to Service...");



                if (command.EndsWith(".msi") && File.Exists(command) && parameters.IsNullOrEmpty() ) {
                    // assume install if the only thing given is a filename.
                    task = PackageManager.Instance.GetPackages(command, minVersion, maxVersion, installed, active, required, blocked, latest, messages).ContinueWith(antecedent => Install(antecedent.Result));
                    return 0;
                }

                if( !command.StartsWith("-")) {
                    command = command.ToLower();
                }

/*
list-package	    list	    -l	lists packages
get-packageinfo	    info	    -g	shows extended package information
install-package	    install	    -i	installs a package
remove-package	    remove *    -r	removes a package
update-package	    update	    -u	updates a package
trim-packages	    trim	    -t	trims unneccessary packages
activate-package	activate    -a	makes a specific package the 'current'
block-package	    block	    -b	marks a package as 'blocked'
unblock-package	    unblock	    -B	unblocks a package
mark-package	    mark	    -m	marks a package as 'required'
unmark-package	    unmark	    -M	unmarks a package as 'required'
list-feed	        feeds	    -f	lists the feeds known to the system
add-feed	        add	        -A	adds a feed to the system
remove-feed	        remove *	-R	removes a feed from the system
*/

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
                            task = PackageManager.Instance.GetPackages(command, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => Install(antecedent.Result));
                            break;

                        case "-r":
                        case "remove":
                        case "remove-package":
                        case "remove-packages":
                            if (parameters.Count() < 1) {
                                throw new ConsoleException(Resources.RemoveRequiresPackageName);
                            }
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => Remove(antecedent.Result));

                            break;

                        case "-l":
                        case "list":
                        case "list-package":
                        case "list-packages":
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => ListPackages(antecedent.Result));
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
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => Upgrade(antecedent.Result));
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
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => Activate(antecedent.Result));

                            // activate(Parameters)
                            break;

                        case "-g":
                        case "get-packageinfo":
                        case "info":
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => GetPackageInfo(antecedent.Result));

                            break;

                        case "-b":
                        case "block-packages":
                        case "block-package":
                        case "block":
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => Block(antecedent.Result));

                            break;

                        case "-B":
                        case "unblock-packages":
                        case "unblock-package":
                        case "unblock":
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => UnBlock(antecedent.Result));

                            break;

                        case "-m":
                        case "mark-packages":
                        case "mark-package":
                        case "mark":
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => Mark(antecedent.Result));
                            break;

                        case "-M":
                        case "unmark-packages":
                        case "unmark-package":
                        case "unmark":
                            task = PackageManager.Instance.GetPackages(parameters, minVersion, maxVersion, installed, active, required, blocked, latest, messages)
                                .ContinueWith(antecedent => UnMark(antecedent.Result));
                            break;


                        default:
                            throw new ConsoleException(Resources.UnknownCommand, command);
                    }

                    if (task != null) {
                        task.ContinueWith(antecedent => WaitForPackageManagerToComplete()).Wait();
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
            if (terse) {
                foreach( var package in packages) {
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
            Console.WriteLine("Message Argument Error {0}, {1}, {2}.",arg1,arg2,arg3);
        }

        private void OperationRequiresPermission(string policyName) {
            Console.WriteLine("Operation requires permission Policy:{0}", policyName);
        }

        private void NoPackagesFound() {
            Console.WriteLine("Did not find any packages.");
        }

        private void UnexpectedFailure(Exception obj) {
            throw new ConsoleException("SERVER EXCEPTION: {0}\r\n{1}",obj.Message,obj.StackTrace);
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

       
    }
}