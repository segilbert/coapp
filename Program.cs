//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.cli {
    using System;
    using Toolkit.Engine;
    using Toolkit.Extensions;

    /// <summary>
    ///     Main Program for command line coapp tool
    /// </summary>
    internal class Program {
        /// <summary>
        /// Command line help information
        /// </summary>
        private const string HelpMessage = @"
Usage:
-------

CoApp-cli [options] <command> <parameters>

Options:
--------
    --help                      this help
    --nologo                    don't display the logo
    --load-config=<file>        loads configuration from <file>
    --verbose                   prints verbose messages

    --pretend                   doesn't actually alter the system

    --max-packages=<number>     overrides the maximum number of packages that
                                can be installed at once (default 25)
    
    --override-protect          ignores any protect flags on packages 
    --override-frozen           ignores any frozen flags on packages
    --override-block            ignores any blocak flags on packages
    
Package Commands
----------------
    list packages               lists the installed packages
    find <package*>             lists all the known packages that match 
    show 

    install <package*>...       installs the package <package>
    install <msi-url>           gets the msi at <msi-url> and installs it
    install <pkg-url>           gets the package feed at <pkg-url> and installs
                                everything in the feed

    uninstall <package*>...     removes the package <package>
    uninstall <pkg-url>         removes all the packages in the feed

    update                      updates all packages not frozen
    update <package*>...        updates [package] to the latest version
    update <pkg-url>            updates all packages from feed at <url>
  
    freeze <package*>...        places a freeze on the <package>
    protect <package*>...       protects package <package> from being removed
    block <package*>...         blocks <package> from being installed

    unfreeze <package*>...      removes a freeze on the <package>
    unprotect <package*>...     allows package <package> to be removed
    unblock <package*>...       allows <package> to be installed

    trim                        removes (non-app) packages that are not used 
                                or protected

Repository Commands
-------------------
    list repo                 lists all the repositories in the directory 
                              and added locals
                                
    add <url>                 adds a localally recognized repository 
    remove <url|name>         removes a repository <url> or by <name>
    block <url|name>          blocks a repository at <url> even if it is 
                              in the directory 
    unblock <url|name>        unblocks a repository at <url> 
                              

Repository Directory Commands
-----------------------------
    show-directory            returns the URL for the repository directory
    set-directory <url>       sets the URL repository directory 
    clear-directory <url>     clear the URL for the repository directory

Notes:
-------
<package*>      indicates a partial, wildcard or complete package name 

                A canonical package name is specified: 
                    
                    [repo:]name[-MM.NN][.RR][.BB]

                where 
                
                    [repo:] is the common name (optional)
                    name    is the package name (supports wildcards [*,?])
                    [-MM.NN] is the major/minor build number (optional)
                    [RR] is the revision number (optional)
                    [BB] is the build number (optional)

<package*>...   indicates one or more packages    
";

        /// <summary>
        ///     Main entrypoint for CLI.
        /// </summary>
        /// <param name="args">
        /// The command line arguments
        /// </param>
        /// <returns>
        ///     int value representing the ERRORLEVEL.
        /// </returns>
        private static int Main(string[] args) {
            return new Program().Startup(args);
        }

        /// <summary>
        ///     The (non-static) startup method 
        /// </summary>
        /// <param name="args">
        /// The command line arguments.
        /// </param>
        /// <returns>
        ///     Process return code.
        /// </returns>
        private int Startup(string[] args) {
            var pkgManager = new PackageManager();
            var options = args.Switches();
            var parameters = args.Parameters();

            foreach(var arg in options.Keys) {
                var argumentParameters = options[arg];

                switch(arg) {
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
                        return Fail("Unknown parameter [--{0}]", arg);
                }
            }

            Logo();

            if(parameters.Count < 1) {
                return Fail("Missing Command. \r\n\r\n    Use --help for command line help.");
            }

            var command = parameters[0].ToLower();
            parameters.RemoveAt(0);

            try {
                switch(command) {
                    case "install":
                        if(parameters.Count < 1) {
                            return Fail("Command 'install' requires at least one package. \r\n\r\n    Use --help for command line help.");
                        }

                        pkgManager.Install(parameters);
                        break;

                    case "uninstall":
                        if(parameters.Count < 1) {
                            return Fail("Command 'uninstall' requires at least one package. \r\n\r\n    Use --help for command line help.");
                        }

                        pkgManager.Remove(parameters);
                        break;

                    case "list":
                        // dual purpose
                        if(parameters.Count != 1) {
                            return Fail("Command 'list' requires a parameter: either 'packages' or 'repo'. \r\n\r\n    Use --help for command line help.");
                        }

                        /*
                        if(parameters[0].ToLower() == "packages")
                            pkgManager.ListPackages();


                        */

                        break;
                    
                    case "find":
                        if(parameters.Count < 1) {
                            return Fail("Command 'find' requires at least one partial package name. \r\n\r\n    Use --help for command line help.");
                        }

                        break;

                    case "update":
                        break;

                    case "freeze":
                        break;
                    case "unfreeze":
                        break;
                    case "protect":
                        break;
                    case "unprotect":
                        break;
                    case "block":
                            // dual purpose
                        break;
                    case "unblock":
                        break;

                    case "trim":
                        break;
                    case "add":
                        break;
                    case "remove":
                        break;

                    case "show-directory":
                        "Repository Directory URL: [{0}]".Print(pkgManager.RepostoryDirectoryUrl);
                        break;
                        
                    case "set-directory":
                        if(parameters.Count != 1) {
                            return Fail("Command 'set-directory' requires the URL for the repository directory . \r\n\r\n    Use --help for command line help.");
                        }

                        pkgManager.RepostoryDirectoryUrl = parameters[0];
                        "Repository Directory URL: [{0}]".Print(pkgManager.RepostoryDirectoryUrl);

                        break;
                    case "clear-directory":
                        break;

                    default:
                        return Fail("Unknown Command [{0}]. \r\n\r\n    Use --help for command line help.".format(command));
                }
            }
            catch(Exception failure) {
                return Fail(failure.Message);
            }

            return 0;
        }

        #region fail/help/logo

        /// <summary>
        ///     Displays a failure message.
        /// </summary>
        /// <param name="text">
        /// The text format string.
        /// </param>
        /// <param name="par">
        /// The parameters for the formatted string.
        /// </param>
        /// <returns>
        ///     returns 1 (usually passed out as the process end code)
        /// </returns>
        public int Fail(string text, params object[] par) {
            Logo();
            using(new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black)) {
                Console.WriteLine("Error: {0}", text.format(par));
            }

            return 1;
        }

        /// <summary>
        ///     Displays the program help.
        /// </summary>
        /// <returns>
        ///     returns 0.
        /// </returns>
        private int Help() {
            Logo();
            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                HelpMessage.Print();
            }

            return 0;
        }

        /// <summary>
        ///     Displays the program logo.
        /// </summary>
        private void Logo() {
            using (new ConsoleColors(ConsoleColor.Cyan, ConsoleColor.Black)) {
                this.Assembly().Logo().Print();
            }

            this.Assembly().SetLogo(string.Empty);
        }

        #endregion
    }
}