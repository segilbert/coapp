//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using CoApp.Toolkit.Engine.Feeds;
using CoApp.Toolkit.Utility;
using CoApp.Toolkit.Win32;

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Diagnostics;
    using System.IO.Pipes;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Logging;
    using Microsoft.Win32.SafeHandles;
    using Pipes;
    using Tasks;

    internal static class Signals {
        private static readonly SafeWaitHandle _availableEvent = Kernel32.CreateEvent(IntPtr.Zero, true, false, "Global\\CoAppAvailable");
        private static readonly SafeWaitHandle _startingupEvent = Kernel32.CreateEvent(IntPtr.Zero, true, false, "Global\\CoAppStartingUp");
        private static readonly SafeWaitHandle _shuttingdownEvent = Kernel32.CreateEvent(IntPtr.Zero, true, false, "Global\\CoAppShuttingDown");
        private static readonly SafeWaitHandle _shuttingdownRequestedEvent  = Kernel32.CreateEvent(IntPtr.Zero, true, false, "Global\\CoAppShutdownRequested");
        private static readonly SafeWaitHandle _installedEvent = Kernel32.CreateEvent(IntPtr.Zero, true, false, "Global\\CoAppInstalledPackage");
        private static readonly SafeWaitHandle _removedEvent = Kernel32.CreateEvent(IntPtr.Zero, true, false, "Global\\CoAppRemovedPackage");

        private static bool _available;
        public static bool Available {

            get { return _available; }
            set {
                _available = value;
                Kernel32.ResetEvent(_availableEvent);

                if (value) {
                    StartingUp = false;
                    ShuttingDown = false;
                    Kernel32.SetEvent(_availableEvent);
                }
                
            }
        }

        private static bool _startingUp;
        public static bool StartingUp {
            get { return _startingUp; }
            set {
                _startingUp = value;
                Kernel32.ResetEvent(_startingupEvent);

                if (value) {
                    Available = false;
                    ShuttingDown = false;
                    Kernel32.SetEvent(_startingupEvent);
                }
            }
        }

        private static bool _shuttingDown;
        public static bool ShuttingDown {
            get { return _shuttingDown; }
            set {
                _shuttingDown = value;
                Kernel32.ResetEvent(_shuttingdownEvent);
                if (value) {
                    StartingUp = false;
                    Available = false;
                    Kernel32.SetEvent(_shuttingdownEvent);
                }
            }
        }

        private static bool _shutdownRequested;
        public static bool ShutdownRequested {
            get { return _shutdownRequested; }
            set {
                _shutdownRequested = value;
                Kernel32.ResetEvent(_shuttingdownRequestedEvent);
                if (value) {
                    Kernel32.SetEvent(_shuttingdownRequestedEvent);
                }
            }
        }

        public static void InstalledPackage(string canonicalPackageName) {
            Task.Factory.StartNew(() => {
                PackageManagerSettings.CoAppInformation["InstalledPackages"].StringsValue =
                    PackageManagerSettings.CoAppInformation["InstalledPackages"].StringsValue.UnionSingleItem(canonicalPackageName);
                Kernel32.ResetEvent(_installedEvent);
                Kernel32.SetEvent(_installedEvent);
                Thread.Sleep(100); // give everyone a chance to wake up and do their job
                Kernel32.ResetEvent(_installedEvent);
            });
        }

        public static void RemovedPackage(string canonicalPackageName) {
            Task.Factory.StartNew(() => {
                PackageManagerSettings.CoAppInformation["RemovedPackages"].StringsValue =
                    PackageManagerSettings.CoAppInformation["RemovedPackages"].StringsValue.UnionSingleItem(canonicalPackageName);
                Kernel32.ResetEvent(_removedEvent);
                Kernel32.SetEvent(_removedEvent);
                Thread.Sleep(100); // give everyone a chance to wake up and do their job
                Kernel32.ResetEvent(_removedEvent);
            });
        }

        public static int EngineStartupStatus { 
            get {
                return PackageManagerSettings.CoAppInformation["StartupPercentComplete"].IntValue;
            } 
            set {
                PackageManagerSettings.CoAppInformation["StartupPercentComplete"].IntValue = value;
                if (value > 0 && value < 100) {
                    StartingUp = true;
                }
            }
        }
    }

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

        public static bool IsInteractive { get; set; }

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
        public static void RequestStop() {
            // this should stop the coapp engine.
            _instance.Value._cancellationTokenSource.Cancel();
            _instance.Value._isRunning = false;
            Signals.ShutdownRequested = true;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks></remarks>
        public static Task Start(bool interactive) {
            // this should spin up a task and start listening for commands
            IsInteractive = interactive;
            Logger.Warning("Starting up Engine in mode: {0}." , interactive ? "[Interactive]":"[Service]");
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
            if (IsRunning) {
                return _engineService;
            }
            Signals.EngineStartupStatus = 0;
            var npmi = NewPackageManager.Instance;

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;
            
            Signals.StartingUp = true;
            // make sure coapp is properly set up.
            Task.Factory.StartNew(() => {
                try {
                    var stage = new ProgressFactor(ProgressWeight.Tiny);
                    var packageScan = new ProgressFactor(ProgressWeight.Medium);
                    var progress = new MultifactorProgressTracker {stage, packageScan};
                    progress.ProgressChanged += (p) => { Signals.EngineStartupStatus = p; };

                    // this ensures that composition rules are run for toolkit.
                    stage.Progress = 5;
                    Package.EnsureCanonicalFoldersArePresent();
                    stage.Progress = 25;
                    var v = Package.GetCurrentPackageVersion("coapp.toolkit", "1e373a58e25250cb");
                    stage.Progress = 50;
                    Logger.Warning("CoApp Version : " + v);
                    stage.Progress = 100;
                    do {
                        Thread.Sleep(1);
                        packageScan.Progress = InstalledPackageFeed.Instance.Progress;
                    } while (InstalledPackageFeed.Instance.Progress < 100);

                    // Completes startup. 
                    packageScan.Progress = 100;
                    Signals.Available = true;
                } catch (Exception e ) {
                    Logger.Error(e);
                    RequestStop();
                }
            });

            _engineService = Task.Factory.StartNew(() => {
                _pipeSecurity = new PipeSecurity();
                _pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid,null ), PipeAccessRights.ReadWrite, AccessControlType.Allow));
                _pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

                // start a few listeners by default--each listener will also spawn a new empty one.
                StartListener();
                StartListener();
            }, _cancellationTokenSource.Token).AutoManage();
            
            _engineService = _engineService.ContinueWith(antecedent => {
                RequestStop();
                // ensure the sessions are all getting closed.
                Session.CancelAll();
                _engineService = null;
            }, TaskContinuationOptions.AttachedToParent).AutoManage();
            return _engineService;
        }


        private int listenerCount;
        /// <summary>
        /// Starts the listener.
        /// </summary>
        /// <remarks></remarks>
        private void StartListener() {
            if (_cancellationTokenSource.Token.IsCancellationRequested) {
                return;
            }

            try {
                if (IsRunning) {
                    Logger.Message("Starting New Listener {0}", listenerCount++);
                    var serverPipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, Instances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                        BufferSize, BufferSize, _pipeSecurity);
                    var listenTask = Task.Factory.FromAsync(serverPipe.BeginWaitForConnection, serverPipe.EndWaitForConnection, serverPipe);

                    listenTask.ContinueWith(t => {
                        if (t.IsCanceled || _cancellationTokenSource.Token.IsCancellationRequested) {
                            return;
                        }

                        StartListener(); // spawn next one!

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
                RequestStop();
            }
        }

        public static bool DoesTheServiceNeedARestart {
            get {
                // is this the coapp win32 service process, or is this interactive
                if( IsInteractive ) {
                    Logger.Warning("Service doens't need a restart, since it's interactive");
                    return false;
                }

                Logger.Warning("Checking to see if service needs a restart");
                // what is the version of the process running?
                FourPartVersion currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                // what is the version of the installed toolkit
                var installedVersion = Package.GetCurrentPackageVersion("coapp.toolkit", "1e373a58e25250cb");
                Logger.Warning("Running Version [{0}] == InstalledVersion [{1}]",currentVersion , installedVersion );

                return installedVersion > currentVersion;
            }
        }

        public static void RestartService() {
            Task.Factory.StartNew(() => {
                try {
                    Logger.Message("Service Restart Order Issued.");
                    // make sure nobody else can connect.
                    RequestStop();

                    // tell the clients to go away.
                    Logger.Message("Telling clients to go away.");
                    Session.NotifyClientsOfRestart();

                    Logger.Message("Waiting up to 10 seconds for clients to disconnect.");
                    // I'll give you 10 seconds to get lost.
                    for (var i = 0; i < 100 && Session.HasActiveSessions; i++) {
                        Thread.Sleep(100);
                    }

                    if (Session.HasActiveSessions) {
                        Logger.Message("Forcing Disconnection of clients.");
                        Session.CancelAll();
                    }
                } catch(Exception e) {
                    Logger.Error(e);
                }
                Logger.Message("Clients should be disconnected; forcing restart");
                Process.Start(new ProcessStartInfo {
                    FileName =EngineServiceManager.CoAppServiceExecutablePath,
                    Arguments = "--restart",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            });
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
                Logger.Error(e);
            }
        }
    }
}