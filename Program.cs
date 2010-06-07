//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.cli {
    using System;
    using Toolkit.Engine;
    using Toolkit.Extensions;

    internal class Program {
        private const string help = @"
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

    

    Available Commands:
    -------------------

    install                     installs a package

    remove                      removes a package

    
";

        private static int Main(string[] args) {
            return new Program().main(args);
        }

        private int main(string[] args) {
            var pkgManager = new PackageManager();
            var options = args.Switches();
            var parameters = args.Parameters();

            #region Parse Options 

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
                        this.Assembly().SetLogo("");
                        break;

                    case "help":
                        return Help();

                    default:
                        return Fail("Unknown parameter [--{0}]", arg);
                }
            }
            Logo();

            #endregion

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

                    case "remove":
                        if(parameters.Count < 1) {
                            return Fail("Command 'remove' requires at least one package. \r\n\r\n    Use --help for command line help.");
                        }

                        pkgManager.Remove(parameters);
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

        public int Fail(string text, params object[] par) {
            Logo();
            using(new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black))
                Console.WriteLine("Error:{0}", text.format(par));
            return 1;
        }

        private int Help() {
            Logo();
            using(new ConsoleColors(ConsoleColor.White, ConsoleColor.Black))
                help.Print();
            return 0;
        }

        private void Logo() {
            using(new ConsoleColors(ConsoleColor.Cyan, ConsoleColor.Black))
                this.Assembly().Logo().Print();
            this.Assembly().SetLogo("");
        }

        #endregion
    }
}