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
    using Toolkit.Crypto;
//     using Toolkit.Engine;
//    using Toolkit.Engine.Client;
    using Toolkit.Engine;
    using Toolkit.Engine.Client;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Network;
    using Toolkit.Tasks;
    using Toolkit.Win32;

    /// <summary>
    /// Main Program for command line coapp tool
    /// </summary>
    /// <remarks></remarks>
    public class CoAppMain : AsyncConsoleProgram {

        private bool terse = false;

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
        /// <remarks></remarks>
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
            try {
                #region commane line parsing

                var options = args.Where( each => each.StartsWith("--")).Switches();
                var parameters = args.Where(each => !each.StartsWith("--")).Parameters();


                var name = string.Empty;
                var minVersion=string.Empty;
                var maxVersion=string.Empty;
                var arch=string.Empty;
                var publicKeyToken=string.Empty;
                bool? installed=null;
                bool? active=null;
                bool? required=null;
                bool? blocked=null;
                bool? latest=null;

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];

                    switch (arg) {
                        /* options  */
                        case "name":
                            name = argumentParameters.LastOrDefault();
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

                var command = parameters.FirstOrDefault();
                parameters = parameters.Skip(1);

                if( ConsoleExtensions.InputRedirected ) {
                    // grab the contents of the input stream and use that as parameters
                    var lines = Console.In.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    parameters = parameters.Union(lines);
                }

                if( ConsoleExtensions.OutputRedirected ) {
                    terse = true;
                }

                if( command.IsNullOrEmpty() ) {
                    return Help();
                }

                Console.WriteLine("Contacting Service...");
                PackageManager.Instance.Connect("command-line-client", "garrett");
                Console.WriteLine("Waiting for service to respond...");
                if (!PackageManager.Instance.IsReady.WaitOne(5000)) {
                    Console.WriteLine("not connected...");
                    throw new ConsoleException("Unable to connect to CoApp Service.");
                }

                Console.WriteLine("Connected to Service...");

                if (command.EndsWith(".msi") && File.Exists(command) && parameters.IsNullOrEmpty() ) {
                    // assume install if the only thing given is a filename.
                    Install(command.SingleItemAsEnumerable());
                    return 0;
                }

                if( !command.StartsWith("-")) {
                    command = command.ToLower();
                }

                
                
                    

/*
list-package	list	-l	lists packages
get-packageinfo	info	-g	shows extended package information
install-package	install	-i	installs a package
remove-package	remove *	-r	removes a package
update-package	update	-u	updates a package
trim-packages	trim	-t	trims unneccessary packages
activate-package	activate	-a	makes a specific package the 'current'
block-package	block	-b	marks a package as 'blocked'
unblock-package	unblock	-B	unblocks a package
mark-package	mark	-m	marks a package as 'required'
unmark-package	unmark	-M	unmarks a package as 'required'
list-feed	feeds	-f	lists the feeds known to the system
add-feed	add	-A	adds a feed to the system
remove-feed	remove *	-R	removes a feed from the system
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
                            Install(parameters);
                            break;

                        case "-r":
                        case "remove":
                        case "remove-package":
                        case "remove-packages":
                            if (parameters.Count() < 1) {
                                throw new ConsoleException(Resources.RemoveRequiresPackageName);
                            }
                            Remove(parameters);
                            break;

                        case "-l":
                        case "list":
                        case "list-package":
                        case "list-packages":
                            ListPackages(parameters);
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

                            // Upgrade(parameters);
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
                            // activate(Parameters)
                            break;

                        case "-g":
                        case "get-packageinfo":
                        case "info":
                            // getPackageInfo(parameters)
                            break;

                        case "-b":
                        case "block-packages":
                        case "block-package":
                        case "block":
                            // block(parameters)
                            break;

                        case "-B":
                        case "unblock-packages":
                        case "unblock-package":
                        case "unblock":
                            // unblock(parameters)
                            break;

                        case "-m":
                        case "mark-packages":
                        case "mark-package":
                        case "mark":
                            // mark(parameters)
                            break;

                        case "-M":
                        case "unmark-packages":
                        case "unmark-package":
                        case "unmark":
                            // unmark(parameters)
                            break;


                        default:
                            throw new ConsoleException(Resources.UnknownCommand, command);
                    }

                // wait for cancellation token, or service to disconnect
                WaitHandle.WaitAny(new [] {
                    CancellationTokenSource.Token.WaitHandle, PackageManager.Instance.IsDisconnected, PackageManager.Instance.IsCompleted
                });

                PackageManager.Instance.Disconnect();
            }
            catch (ConsoleException failure) {
                CancellationTokenSource.Cancel();
                PackageManager.Instance.Disconnect();
                Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
            }
            return 0;
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
        private void ListPackages(IEnumerable<string> parameters) {

            PackageManager.Instance.FindPackages(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, new PackageManagerMessages {
                UnexpectedFailure = UnexpectedFailure,
                PackageInformation = (package) => {
                  Console.WriteLine("Package: {0}, IsInstalled:[{1}]",package.CanonicalName,package.IsInstalled);
                },
                NoPackagesFound = NoPackagesFound,
                PermissionRequired = OperationRequiresPermission,
                Error = MessageArgumentError,
                RequireRemoteFile = GetRemoteFile,
                OperationCancelled = CancellationRequested,


            });
           
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
        private void Remove(IEnumerable<string> parameters) {
            
           

        }

        /// <summary>
        /// Installs the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <remarks></remarks>
        private void Install(IEnumerable<string> parameters) {
            
        }

       
    }
}