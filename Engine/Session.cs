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
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Win32;

    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class Session {
#if DEBUG
        // keep the reconnect window at 20 seconds for debugging
        private static TimeSpan _maxDisconenctedWait = new TimeSpan(0, 0, 0, 20);
#else 
    // fifteen minutes is good for the real world.
        private static TimeSpan _maxDisconenctedWait = new TimeSpan(0,0,15,00);

#endif
        private static TimeSpan _synchronousClientHeartbeat = new TimeSpan(0, 0, 0, 0, 650);

        /// <summary>
        /// </summary>
        private static List<Session> _activeSessions = new List<Session>();

        /// <summary>
        /// </summary>
        private string _clientId;

        /// <summary>
        /// </summary>
        private readonly string _sessionId;

        /// <summary>
        /// </summary>
        private readonly string _userId;

        /// <summary>
        /// </summary>
        private readonly bool _isElevated;

        /// <summary>
        /// </summary>
        private NamedPipeServerStream _serverPipe;

        /// <summary>
        /// </summary>
        private NamedPipeServerStream _responsePipe;

        /// <summary>
        /// </summary>
        private readonly NewPackageManager _packageManager = new NewPackageManager();

        private bool _ended;

        private ManualResetEvent _resetEvent = new ManualResetEvent(true );

        private bool _waitingForClientResponse;
        private Task _task;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isAsychronous = true;

        private readonly Dictionary<string, PackageSessionData> _sessionData = new Dictionary<string, PackageSessionData>();
        private PackageManagerSession _packageManagerSession;

        private bool Connected {
            get { return _resetEvent.WaitOne(0); }
            set {
                if (value) {
                    _resetEvent.Set();
                }
                else {
                    _resetEvent.Reset();
                }
            }
        }

        public static void CancelAll() {
            while (_activeSessions.Any()) {
                var session = _activeSessions.FirstOrDefault();
                if (session != null) {
                    session.End();
                }
            }
        }



        private static void Add(Session session) {
            lock (_activeSessions) {
                _activeSessions.Add(session);
            }
        }


        /// <summary>
        ///   Starts the session.
        /// </summary>
        /// <param name = "clientId">The client id.</param>
        /// <param name = "sessionId">The session id.</param>
        /// <param name = "serverPipe">The server pipe.</param>
        /// <param name = "responsePipe">The response pipe.</param>
        /// <remarks>
        /// </remarks>
        public static void Start(string clientId, string sessionId, NamedPipeServerStream serverPipe, NamedPipeServerStream responsePipe) {
            var isElevated = false;
            var userId = string.Empty;

            serverPipe.RunAsClient(() => {
                userId = WindowsIdentity.GetCurrent().Name;
                isElevated = AdminPrivilege.IsProcessElevated();
            });

            var existingSessions = (from session in _activeSessions
                where session._clientId == clientId && session._sessionId == sessionId && isElevated == session._isElevated && session._userId == userId
                select session).ToList();

            if (existingSessions.Any()) {
                if (existingSessions.Count() > 1) {
                    // multiple matching sessions? This isn't good. Shut em all down to be safe 
                    foreach (var each in existingSessions) {
                        each.End();
                    }
                }
                else {
                    var session = existingSessions.FirstOrDefault();
                    // found just one session.
                    session._serverPipe = serverPipe;
                    session._responsePipe = responsePipe;
                    Console.WriteLine("Rejoining existing session...");
                    session.SendQueuedMessages();
                    session.Connected = true;
                    return;
                }
            }
            else {
                // if the exact session isn't there, find any that are partial matches, and shut em down.
                foreach (
                    var each in (from session in _activeSessions where session._clientId == clientId && session._sessionId == sessionId select session).ToList()
                    ) {
                    each.End();
                }
            }
            // no viable matching session.
            // Let's start a new one.
            Add(new Session(clientId, sessionId, serverPipe, responsePipe, userId, isElevated));
            Console.WriteLine("Starting new session...");
        }

        public void End() {
            if (!_ended) {
                _ended = true;

                // remove this session.
                lock (_activeSessions) {
                    _activeSessions.Remove(this);
                }

                Console.WriteLine("Ending Client: [{0}]-[{1}]", _clientId, _sessionId);

                // end any outstanding tasks as gracefully as we can.
                _cancellationTokenSource.Cancel();

                // close and clean up the pipes. 
                Disconnect();

                GC.Collect();
            }
        }

        private void Disconnect() {
            if (Connected) {
                Connected = false;
                Console.WriteLine("disposing of pipes: [{0}]-[{1}]", _clientId, _sessionId);
                try {
                    if (_serverPipe != null) {
                        _serverPipe.Close();
                        _serverPipe.Dispose();
                        _serverPipe = null;
                    }

                    if (_isAsychronous && _responsePipe != null) {
                        _responsePipe.Close();
                        _responsePipe.Dispose();
                        _responsePipe = null;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Errors when disposing pipes.");
                }
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Session" /> class.
        /// </summary>
        /// <param name = "clientId">The client id.</param>
        /// <param name = "sessionId">The session id.</param>
        /// <param name = "serverPipe">The server pipe.</param>
        /// <param name = "responsePipe">The response pipe.</param>
        /// <remarks>
        /// </remarks>
        protected Session(string clientId, string sessionId, NamedPipeServerStream serverPipe, NamedPipeServerStream responsePipe, string userId,
            bool isElevated) {
            _clientId = clientId;
            _sessionId = sessionId;
            _serverPipe = serverPipe;
            _responsePipe = responsePipe;
            _userId = userId;
            _isElevated = isElevated;
            _isAsychronous = serverPipe == responsePipe;
            Connected = true;

            _task = Task.Factory.StartNew(ProcessMesages, _cancellationTokenSource.Token).ContinueWith((antecedent) => End(),
                TaskContinuationOptions.AttachedToParent);
        }

        protected void WriteAsync() {
        }

        private bool IsCancelled {
            get { return _cancellationTokenSource.Token.IsCancellationRequested; }
        }

        private readonly Queue<UrlEncodedMessage> _outputQueue = new Queue<UrlEncodedMessage>();

        private void QueueResponseMessage(UrlEncodedMessage response) {
            Console.WriteLine("adding message to queue: {0}", response);
            Disconnect();

            lock (_outputQueue) {
                _outputQueue.Enqueue(response);
            }
        }

        private void SendQueuedMessages() {
            while (_outputQueue.Any() && _responsePipe != null) {
                try {
                    _responsePipe.WriteLineAsync(_outputQueue.Peek().ToString()).ContinueWith(antecedent => {
                        lock (_outputQueue) {
                            _outputQueue.Dequeue();
                        }
                    }, TaskContinuationOptions.NotOnFaulted).Wait();
                }
                catch (Exception e) {
                    // hmm. disconnected again.
                    Disconnect();
                    return;
                }
            }
        }

        /// <summary>
        ///   Writes the message to the stream asyncly.
        /// </summary>
        /// <param name = "message">The request.</param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        public void WriteAsync(UrlEncodedMessage message) {
            if (Connected) {
                try {
                    _responsePipe.WriteLineAsync(message.ToString()).ContinueWith(antecedent => QueueResponseMessage(message),
                        TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (Exception e) {
                    // queue the message
                    QueueResponseMessage(message);
                }
            }
            else {
                QueueResponseMessage(message);
            }
        }

        /// <summary>
        ///   Processes the mesages.
        /// </summary>
        /// <remarks>
        /// </remarks>
        private void ProcessMesages() {
            // instantiate the Asynchronous Package Session object (ie, like thread-local-storage, but really, 
            // it's session-local-storage. So for this task and all its children, this will serve up data.
            _packageManagerSession = new PackageManagerSession {
                GetPackageSessionData = (package) => {
                    lock (_sessionData) {
                        if (!_sessionData.ContainsKey(package.CanonicalName)) {
                            _sessionData.Add(package.CanonicalName, new PackageSessionData(package));
                        }
                    }
                    return _sessionData[package.CanonicalName];
                },

                DropPackageSessionData = (package) => {
                    lock (_sessionData) {
                        if (_sessionData.ContainsKey(package.CanonicalName)) {
                            _sessionData.Remove(package.CanonicalName);
                        }
                    }
                },

                CheckForPermission = (policy) => {
                    var result = false;
                    _serverPipe.RunAsClient(() => { result = policy.HasPermission; });
                    return result;
                }
            };

            Task readTask = null;
            WriteAsync(new UrlEncodedMessage("session-started") {
                {
                    "id", _sessionId
                    }
            });

            while (EngineService.IsRunning) {
                if (!Connected) {
                    readTask = null;

                    if (IsCancelled) {
                        End();
                        return;
                    }

                    Console.WriteLine("Waiting for client to reconnect.");
                    _resetEvent.WaitOne(_maxDisconenctedWait);
                    _waitingForClientResponse = true;

                    if (IsCancelled || (_waitingForClientResponse && !Connected)) {
                        // we're disconnected, we've waited for the duration, 
                        // we're assuming the client isn't coming back.
                        End();
                        return;
                    }
                    continue;
                }

                Console.WriteLine("In Loop");

                try {
                    if (IsCancelled) {
                        End();
                        return;
                    }

                    // if there is currently a task reading the from the stream, let's skip it this time.
                    if ((readTask == null || readTask.IsCompleted) && Connected) {
                        var serverInput = new byte[EngineService.BufferSize];
                        try {
                            readTask = _serverPipe.ReadAsync(serverInput, 0, serverInput.Length).ContinueWith(antecedent => {
                                var rawMessage = Encoding.UTF8.GetString(serverInput, 0, antecedent.Result);

                                if (string.IsNullOrEmpty(rawMessage)) {
                                    return;
                                }

                                Dispatch(new UrlEncodedMessage(rawMessage));
                            });
                        }
                        catch (Exception e) {
                            // if the pipe is broken, let's move to the disconnected state
                            Disconnect();
                        }
                    }

                    readTask.Wait(_isAsychronous ? -1 : (int) _synchronousClientHeartbeat.TotalMilliseconds, _cancellationTokenSource.Token);

                    if (IsCancelled) {
                        End();
                        return;
                    }

                    if (!_isAsychronous) {
                        SendKeepAlive();
                    }
                }
                catch (AggregateException ae) {
                    foreach (var e in ae.Flatten().InnerExceptions) {
                        if (e.GetType() == typeof (IOException)) {
                            // pipe got disconnected.
                        }
                        Console.WriteLine("\r\n----------------\r\nAggregate:");
                        Console.WriteLine("   {0} -> {1}\r\n{2}", e.GetType(), e.Message, e.StackTrace);
                    }
                }

                catch (Exception e) {
                    // something broke. Could be a closed pipe.
                    Console.Write(e.GetType());
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        /// <summary>
        ///   Dispatches the specified request message.
        /// </summary>
        /// <param name = "requestMessage">The request message.</param>
        /// <remarks>
        /// </remarks>
        private void Dispatch(UrlEncodedMessage requestMessage) {
            Console.WriteLine("Req: {0}", requestMessage.Command);
            switch (requestMessage.Command) {
                case "find-packages":
                    // get the package names collection and run the command
                    _packageManager.FindPackages(new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        PackageInformation = (package) => SendFoundPackage(package.CanonicalName, package.InternalPackageData.LocalPackagePath, package.Name, package.Version.UInt64VersiontoString(), package.Architecture, package.PublicKeyToken, package.IsInstalled, package.InternalPackageData.RemoteLocation.Select( each => each.AbsoluteUri), Enumerable.Empty<string>(), Enumerable.Empty<string>() ),
                        NoPackagesFound = SendNoPackagesFound,
                        PermissionRequired = SendOperationRequiresPermission,
                        ArgumentError = SendMessageArgumentError,
                        RequireRemoteFile = SendRequireRemoteFile
                    });
                    break;


                case "get-package-details":
                    _packageManager.GetPackageDetails(requestMessage["canonical-name"].ToString(), new NewPackageManagerMessages() {
                        UnexpectedFailure = SendUnexpectedFailure,
                        PackageDetails = (package) => SendPackageDetails(package.CanonicalName, package.PackageDetails.FullDescription),
                        UnknownPackage = SendUnknownPackage,
                        ArgumentError = SendMessageArgumentError
                    });
                    break;

                case "install-package":
                    _packageManager.InstallPackage(requestMessage["canonical-name"], requestMessage["auto-upgrade"], requestMessage["force"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        UnknownPackage = SendUnknownPackage,
                        PermissionRequired = SendOperationRequiresPermission,
                        ArgumentError = SendMessageArgumentError,
                        InstallingPackageProgress = SendInstallingPackage,
                        InstalledPackage = SendInstalledPackage,
                        FailedPackageInstall = SendFailedPackageInstall,
                        PackageBlocked = SendPackageIsBlocked,
                        RequireRemoteFile = SendRequireRemoteFile,
                        SignatureValidation = SendSignatureValidation,
                    });
                break;

                case "recognize-file":
                    _packageManager.RecognizeFile(requestMessage["reference-id"], requestMessage["local-location"], requestMessage["remote-location"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                        FileNotRecognized = SendUnableToRecognizeFile
                    });
                break;

                case "unable-to-acquire":
                    _packageManager.UnableToAcquire(requestMessage["reference-id"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                    });
                break;

                case "remove-package":
                    _packageManager.RemovePackage(requestMessage["canonical-name"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        UnknownPackage = SendUnknownPackage,
                        PermissionRequired = SendOperationRequiresPermission,
                        FailedPackageRemoval = SendFailedRemovePackage,
                        ArgumentError = SendMessageArgumentError,
                        PackageBlocked = SendPackageIsBlocked
                    });
              break;

                case "set-package":
                    _packageManager.SetPackage(requestMessage["canonical-name"], requestMessage["active"], requestMessage["required"], requestMessage["blocked"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                        PermissionRequired = SendOperationRequiresPermission,
                        UnknownPackage = SendUnknownPackage,
                    });
                break;

                case "verify-file-signature":
                    _packageManager.VerifyFileSignature(requestMessage["filename"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                        FileNotFound= SendFileNotFound,
                        SignatureValidation= SendSignatureValidation,
                    });
                break;

                case "add-feed":
                    _packageManager.AddFeed(requestMessage["location"], requestMessage["session"] , new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                        PermissionRequired = SendOperationRequiresPermission,
                        RequireRemoteFile = SendRequireRemoteFile,
                    });
                break;

                case "remove-feed":
                    _packageManager.RemoveFeed(requestMessage["location"], requestMessage["session"], new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                        PermissionRequired = SendOperationRequiresPermission,
                    });
                break;

                case "find-feeds":
                    _packageManager.ListFeeds( new NewPackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        ArgumentError = SendMessageArgumentError,
                        FeedDetails = SendFoundFeed
                    });
                break;

                default:
                    // not recognized command, return error code.
                    WriteAsync(new UrlEncodedMessage("unknowncommand") {
                        { "command", requestMessage.Command }
                    });
                    break;
            }
        }

        #region Response Messages
              
        private void SendSessionStarted(string sessionId) {
            WriteAsync(new UrlEncodedMessage("start-session") {{
                "session-id", sessionId
            }});
        }

        private void SendNoPackagesFound() {
            WriteAsync(new UrlEncodedMessage("no-packages-found"));
        }

        private void SendFoundPackage(string canonicalName, string localLocation, string name, string version, string arch, string publicKeyToken,
            bool installed, IEnumerable<string> remoteLocations, IEnumerable<string> dependencies, IEnumerable<string> supercedentPackages) {
            var msg = new UrlEncodedMessage("found-package") {
                { "canonical-name", canonicalName },
                { "local-location", localLocation },
                { "name", name },
                { "version", version },
                { "arch", arch },
                { "public-key-token", publicKeyToken },
                { "installed", installed.ToString() },
            };

            msg.AddCollection("remote-locations", remoteLocations);
            msg.AddCollection("dependencies", dependencies);
            msg.AddCollection("supercedent-packages", supercedentPackages);

            WriteAsync(msg);
        }

        private void SendPackageDetails(string canonicalName, string description /*...*/) {
            var msg = new UrlEncodedMessage("package-details") {
                { "canonical-name", canonicalName },
                { "description", description },
            };

            WriteAsync(msg);
        }

        private void SendFoundFeed(string location, DateTime lastScanned, bool session) {
            WriteAsync( new UrlEncodedMessage("found-feed") {
                {"location", location},
                {"last-scanned", lastScanned.ToFileTime().ToString()},
                {"session", session.ToString()},
            });
        }

        private void SendScanningPackages(string currentItem, int percentComplete) {
            WriteAsync( new UrlEncodedMessage("scanning-packages") {
                {"current-item", currentItem},
                {"percent-complete", percentComplete.ToString()},
            });
        }

        private void SendInstallingPackage(string canonicalName, int percentComplete) {
            WriteAsync( new UrlEncodedMessage("installing-package") {
                {"canonical-name", canonicalName},
                {"percent-complete", percentComplete.ToString()},
            });
        }

        private void SendRemovingPackage(string canonicalName, int percentComplete) {
            WriteAsync( new UrlEncodedMessage("removing-package") {
                {"canonical-name", canonicalName},
                {"percent-complete", percentComplete.ToString()},
            });
        }

        private void SendInstalledPackage(string canonicalName) {
            WriteAsync( new UrlEncodedMessage("installed-package") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendRemovedPackage(string canonicalName) {
            WriteAsync( new UrlEncodedMessage("removed-package") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendFailedPackageInstall(string canonicalName, string filename, string reason) {
            WriteAsync( new UrlEncodedMessage("failed-package-install") {
                {"canonical-name", canonicalName},
                {"filename", filename},
                {"reason", reason},
            });
        }

        private void SendFailedRemovePackage(string canonicalName, string reason) {
            WriteAsync( new UrlEncodedMessage("failed-remove-package") {
                {"canonical-name", canonicalName},
                {"reason", reason},
            });
        }

        private void SendRequireRemoteFile(IEnumerable<string> remoteLocations, string destination, bool force) {
            var msg = new UrlEncodedMessage("require-remote-file") {
                {"destination", destination},
                {"force", force.ToString()},
            };

            msg.AddCollection("remote-locations", remoteLocations);

            WriteAsync(msg);
        }

        private void SendSignatureValidation(string filename, bool isValid, string certificateSubjectName) {
             WriteAsync( new UrlEncodedMessage("signature-validation") {
                {"filename", filename},
                {"is-valid", isValid.ToString()},
                {"certificate-subject-name", certificateSubjectName ?? string.Empty},
            });
        }

        private void SendOperationRequiresPermission(string currentUserName, string policyRequired) {
            WriteAsync( new UrlEncodedMessage("operation-requires-permission") {
                {"current-user-name", currentUserName},
                {"policyRequired", policyRequired},
            });
        }

        private void SendMessageArgumentError(string messageName, string argumentName, string problem) {
            WriteAsync( new UrlEncodedMessage("message-argument-error") {
                {"message-name", messageName},
                {"argument-name", argumentName},
                {"problem", problem},
            });
        }

        private void SendFileNotFound(string filename) {
            WriteAsync( new UrlEncodedMessage("file-not-found") {
                {"filename", filename},
            });
        }

        private void SendUnknownPackage(string canonicalName) {
            WriteAsync( new UrlEncodedMessage("unknown-package") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendPackageIsBlocked(string canonicalName) {
            WriteAsync( new UrlEncodedMessage("package-is-blocked") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendUnableToRecognizeFile(string filename, string reason) {
            WriteAsync( new UrlEncodedMessage("unable-to-recognize-file") {
                {"filename", filename},
                {"reason", reason},
            });
        }

        private void SendUnexpectedFailure(Exception failure) {
            WriteAsync(new UrlEncodedMessage("unexpected-failure") {
                {"type", failure.GetType().ToString()},
                {"message", failure.Message},
                {"stacktrace", failure.StackTrace},
            });
        }


        private void SendKeepAlive() {
            WriteAsync( new UrlEncodedMessage("keep-alive"));
        }

        #endregion

    }

}
