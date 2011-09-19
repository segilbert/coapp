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
    using Pipes;
    using Tasks;

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
        private static readonly Lazy<EngineService> _instance = new Lazy<EngineService>(() => new EngineService());

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

        private Task _engineService;
        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <remarks></remarks>
        public static void Stop() {
            // this should stop the task
            _instance.Value._cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks></remarks>
        public static Task Start() {
            // this should spin up a task and start listening for commands
            return _instance.Value.Main();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <remarks></remarks>
        public static bool IsRunning {
            get { return _instance.Value._isRunning; }
        }

        /// <summary>
        /// Mains this instance.
        /// </summary>
        /// <remarks></remarks>
        private Task Main() {
            if (_isRunning) {
                return _engineService;
            }
            var npmi = NewPackageManager.Instance;

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            _engineService = Task.Factory.StartNew(() => {
                _pipeSecurity = new PipeSecurity();
//                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid,null );


                _pipeSecurity.AddAccessRule(new PipeAccessRule( new SecurityIdentifier(WellKnownSidType.WorldSid,null ), PipeAccessRights.ReadWrite, AccessControlType.Allow));
                _pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

                // start two listeners by default--each listener will also spawn a new empty one.
                StartListener();
                StartListener();
                   
            }, _cancellationTokenSource.Token).AutoManage();

            _engineService = _engineService.ContinueWith(antecedent => {
                _isRunning = false;
                // ensure the sessions are all getting closed.
                Session.CancelAll();
                _engineService = null;
            }, TaskContinuationOptions.AttachedToParent).AutoManage();
            return _engineService;
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
                    var listenTask = Task.Factory.FromAsync(serverPipe.BeginWaitForConnection, serverPipe.EndWaitForConnection, serverPipe);

                    listenTask.ContinueWith(t => {
                        if (t.IsCanceled || _cancellationTokenSource.Token.IsCancellationRequested) {
                            return;
                        }
                        StartListener(); // start next one!

                        if (serverPipe.IsConnected) {
                            var serverInput = new byte[BufferSize];

                            serverPipe.ReadAsync(serverInput, 0, serverInput.Length).AutoManage().ContinueWith(antecedent => {
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
                                    serverPipe.RunAsClient(() => { hasAccess = PermissionPolicy.Connect.HasPermission; });
                                    if (!hasAccess)
                                        return;
                                }
                                catch {
                                    return;
                                }

                                // check for the required parameters. 
                                // close the session if they are not here.
                                if (string.IsNullOrEmpty(requestMessage["id"]) || string.IsNullOrEmpty(requestMessage.Data["client"])) {
                                    return;
                                }
                                var isAsync = (bool?) requestMessage["async"];

                                if (isAsync.HasValue && isAsync.Value == false) {
                                    StartResponsePipeAndProcessMesages(requestMessage.Data["client"], requestMessage["id"], serverPipe);
                                }
                                else {
                                    Session.Start(requestMessage.Data["client"], requestMessage["id"], serverPipe, serverPipe);
                                }
                            }).Wait();
                        }


                    }, _cancellationTokenSource.Token, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);

                }
            }
            catch /* (Exception e) */ {
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
                Task.Factory.FromAsync(responsePipe.BeginWaitForConnection, responsePipe.EndWaitForConnection, responsePipe,
                    TaskCreationOptions.AttachedToParent).ContinueWith(t => {
                        if (responsePipe.IsConnected) {
                            Session.Start(clientId, sessionId, serverPipe, responsePipe);
                        }
                    }, TaskContinuationOptions.AttachedToParent);
            }
            catch (Exception e) {
                Console.Write(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}