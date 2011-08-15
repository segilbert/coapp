//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.IO.Pipes;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public class EngineService {
        /// <summary>
        /// 
        /// </summary>
        private const string PipeName = @"CoAppInstaller";
        /// <summary>
        /// 
        /// </summary>
        private const string OutputPipeName = @"CoAppInstaller-";

        /// <summary>
        /// 
        /// </summary>
        private const int Instances = -1;
        /// <summary>
        /// 
        /// </summary>
        internal const int BufferSize = 8192;
        /// <summary>
        /// 
        /// </summary>
        private static readonly Lazy<EngineService> Instance = new Lazy<EngineService>(() => new EngineService());

        /// <summary>
        /// 
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// 
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// 
        /// </summary>
        private PipeSecurity _pipeSecurity;


        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <remarks></remarks>
        public static void Stop() {
            // this should stop the task
            Instance.Value._cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks></remarks>
        public static void Start() {
            // this should spin up a task and start listening for commands
            Instance.Value.Main();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <remarks></remarks>
        public static bool IsRunning {
            get { return Instance.Value._isRunning; }
        }

        /// <summary>
        /// Mains this instance.
        /// </summary>
        /// <remarks></remarks>
        private void Main() {
            if (_isRunning) {
                return;
            }

             _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            try {
                Task.Factory.StartNew(() => {
                    try {
                        _pipeSecurity = new PipeSecurity();
                        _pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));
                        _pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

                        // start two listeners by default--each listener will also spawn a new empty one.
                        StartListener();
                        StartListener();
                    }
                    finally {
                        Console.WriteLine("[Done.========================]");
                        _isRunning = false;
                    }
                }, _cancellationTokenSource.Token).Wait(_cancellationTokenSource.Token);

                // ensure the sessions are all getting closed.
                Session.CancelAll();
            }
            catch (Exception ex) {
                Console.Write(ex.GetType());
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        /// <summary>
        /// Starts the listener.
        /// </summary>
        /// <remarks></remarks>
        private void StartListener() {
            try {
                if (_isRunning) {
                    var serverPipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, Instances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                        BufferSize, BufferSize, _pipeSecurity);
                    Task.Factory.FromAsync(serverPipe.BeginWaitForConnection, serverPipe.EndWaitForConnection, serverPipe).ContinueWith(t => {
                        StartListener(); // start next one!

                        if (serverPipe.IsConnected) {
                            var serverInput = new byte[BufferSize];

                            serverPipe.ReadAsync(serverInput, 0, serverInput.Length).ContinueWith(antecedent => {
                                var rawMessage = Encoding.UTF8.GetString(serverInput, 0, antecedent.Result);
                                if (string.IsNullOrEmpty(rawMessage)) {
                                    return;
                                }

                                var requestMessage = new UrlEncodedMessage(rawMessage);

                                // first command must be "startsession"
                                if (!requestMessage.Command.Equals("start-session", StringComparison.CurrentCultureIgnoreCase)) {
                                    return;
                                }

                                // verify that user is allowed to connect.
                                try {
                                    var hasAccess = false;
                                    serverPipe.RunAsClient(()=> { hasAccess = PermissionPolicy.Connect.HasPermission; });
                                    if( !hasAccess )
                                        return;
                                } catch {
                                    return;
                                }

                                // check for the required parameters. 
                                // close the session if they are not here.
                                if (string.IsNullOrEmpty(requestMessage["id"]) || string.IsNullOrEmpty(requestMessage.Data["client"])) {
                                    return;
                                }

                                if (requestMessage["async"].Equals("false", StringComparison.CurrentCultureIgnoreCase)) {
                                    Console.WriteLine("Using Two-Pipe Client");
                                    StartResponsePipeAndProcessMesages(requestMessage.Data["client"], requestMessage["id"], serverPipe);
                                }
                                else {
                                    Console.WriteLine("Using Async Client");
                                    Session.Start(requestMessage.Data["client"], requestMessage["id"], serverPipe, serverPipe);
                                }
                            }).Wait();
                        }

                       
                    }, TaskContinuationOptions.AttachedToParent );
                }
            }
            catch (Exception e) {
                Stop();
            }
        }


        /// <summary>
        /// Starts the response pipe and process mesages.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="serverPipe">The server pipe.</param>
        /// <remarks></remarks>
        private void StartResponsePipeAndProcessMesages(string clientId, string sessionId, NamedPipeServerStream serverPipe) {
            try {
                var channelname = OutputPipeName + sessionId;
                var responsePipe = new NamedPipeServerStream(channelname, PipeDirection.Out, Instances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                    BufferSize, BufferSize, _pipeSecurity);
                Task.Factory.FromAsync(responsePipe.BeginWaitForConnection, responsePipe.EndWaitForConnection, responsePipe).ContinueWith(t => {
                    if (responsePipe.IsConnected) {
                        Session.Start(clientId, sessionId, serverPipe, responsePipe);
                    }
                }, TaskContinuationOptions.AttachedToParent );
            }
            catch (Exception e) {
                Console.Write(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}