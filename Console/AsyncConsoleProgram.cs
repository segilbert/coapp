//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------


using System.ComponentModel;

namespace CoApp.Toolkit.Console {
    using System;
    using System.Collections.Generic;
    using System.Resources;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using Exceptions;
    using Extensions;
    using Tasks;

    public abstract class AsyncConsoleProgram {
        protected abstract ResourceManager Res { get; }
        protected int Counter = 0;

        protected abstract int Main(IEnumerable<string> args);
        protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        protected virtual int Startup(IEnumerable<string> args) {
            var task = Task.Factory.StartNew(() => {
#if DEBUG
                new DebugMessage {
                    WriteLine = (text) => {
                        Console.WriteLine("[DEBUG][{0}] {1}", ++Counter,text);
                    }
                }.Register();
#endif                
                Main(args);
            }, CancellationTokenSource.Token);

            try {
                Console.CancelKeyPress += (x, y) => {
                    if (!CancellationTokenSource.IsCancellationRequested) {
                        Console.WriteLine("Operation Cancelled...");
                        CancellationTokenSource.Cancel();
                        if (y.SpecialKey == ConsoleSpecialKey.ControlBreak) {
                            // can't cancel so we just block on the task.
                            return;
                            // task.Wait();
                        }
                    }
                    y.Cancel = true;
                };
                task.Wait();
            }
            catch( AggregateException ae ) {
                ae = ae.Flatten();
                ae.Handle(HandleException);
                return 1;
            }
            return 0;
        }

        bool HandleException(Exception ex ) {
            if (ex is ConsoleException) {
                Fail(ex.Message);
                return true;
            }
            if( ex is OperationCompletedBeforeResultException) {
                // assumably, this has been actually handled elsewhere.. right?
                return true;
            }

            if (ex is TaskCanceledException) {
                // assumably, this has been actually handled elsewhere.. right?
                return true;
            }

            Console.WriteLine("Unhandled Exception:  {0}", ex.Message);
            return false;
        }

        protected Object AskUser(string question, Func<string, bool> decision)
        {
            Console.Write(question + "  ");
            string response = null;
            while (true)
            {
                response = Console.ReadLine();
                if (decision.Invoke(response))
                    break;

                Console.Write("Invalid response.");

            }

            return response;

        }

        protected Object AskUser(string question, params object[] choices)
        {
            var dict = choices.
                Aggregate(new List<KeyValuePair<object, string>>(),
                          (temp, i) =>
                              {
                                  temp.Add(new KeyValuePair<object, string>(i, i.ToString()));
                                  return temp;
                              });

            return AskUser(question, dict);
        }

        protected Object AskUser(string question, IEnumerable<KeyValuePair<object, string>> choices)
        {
            int response = -1;
            Console.WriteLine(question);
            while (true)
            {
                for (int i = 0; i < choices.Count(); i++) {
                    Console.WriteLine("{0}. {1}", i+1, choices.ElementAt(i).Value);
                }

                var temp = Console.ReadLine();
                if (int.TryParse(temp, out response) && response >= 1 || response <= choices.Count())
                {
                    break;
                }
                Console.WriteLine("Invalid Entry.");
            }

            return choices.ElementAt(response).Key;
        }

        #region fail/help/logo

        /// <summary>
        ///   Displays a failure message.
        /// </summary>
        /// <param name = "text">
        ///   The text format string.
        /// </param>
        /// <param name = "par">
        ///   The parameters for the formatted string.
        /// </param>
        /// <returns>
        ///   returns 1 (usually passed out as the process end code)
        /// </returns>
        protected int Fail(string text, params object[] par) {
            Logo();
            using (new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black)) {
                Console.WriteLine("Error: {0}", text.format(par));
            }
            
            int x = 6;
            while( x-- > 0 || !CancellationTokenSource.Token.WaitHandle.WaitOne( 500 ) ) {
                // Console.Write("");
            }

            return 1;
        }

        /// <summary>
        ///   Displays the program help.
        /// </summary>
        /// <returns>
        ///   returns 0.
        /// </returns>
        protected int Help() {
            Logo();
            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                Res.GetString("HelpText").Print();
            }

            return 0;
        }

        /// <summary>
        ///   Displays the program logo.
        /// </summary>
        protected void Logo() {
            using (new ConsoleColors(ConsoleColor.Cyan, ConsoleColor.Black)) {
                this.Assembly().Logo().Print();
            }

            this.Assembly().SetLogo(string.Empty);
        }

        #endregion
    }
}