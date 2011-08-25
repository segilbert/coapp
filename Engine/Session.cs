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
    using Pipes;
    using Tasks;
    using Win32;

    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class Session {
#if DEBUG
        // keep the reconnect window at 20 seconds for debugging
        private static TimeSpan _maxDisconenctedWait = new TimeSpan(0, 0, 0, 10);
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

        private bool _ended;

        private ManualResetEvent _resetEvent = new ManualResetEvent(true );

        private bool _waitingForClientResponse;
        private Task _task;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isAsychronous = true;

        private readonly Dictionary<string, PackageSessionData> _sessionData = new Dictionary<string, PackageSessionData>();
        private PackageManagerSession _packageManagerSession;
        private SessionCacheMessages _sessionCacheMessages;

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
                    session.SendSessionStarted(sessionId);
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

                // _task should wait here I think...
                /*
                while(!_task.IsCompleted && !_task.Wait(1000) ) {
                    Console.WriteLine("Waiting for task to complete... You may want to expand the message here to see what its waiting on.");
                }
                */

                // drop all our local session data.
                _sessionCache.Clear();
                _sessionCache = null;

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

            // this session task
            _task = Task.Factory.StartNew(ProcessMesages, _cancellationTokenSource.Token);

            // this task is not attached to a parent anywhere.
            _task.AutoManage();

            // when the task is done, call end.
            _task.ContinueWith((antecedent) => End());
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
                    try {
                        // if there is a RequestId in this session, let's grab it.
                        message.Add("rqid", PackageManagerMessages.Invoke.RequestId);    
                    }
                    catch {
                        // no worries if we can't get that.
                    }
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

        private Dictionary<Type, object> _sessionCache = new Dictionary<Type, object>();

        /// <summary>
        ///   Processes the mesages.
        /// </summary>
        /// <remarks>
        /// </remarks>
        private void ProcessMesages() {
            // instantiate the Asynchronous Package Session object (ie, like thread-local-storage, but really, 
            // it's session-local-storage. So for this task and all its children, this will serve up data.

            _packageManagerSession = new PackageManagerSession {
/*
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
*/
                CheckForPermission = (policy) => {
                    var result = false;
                    _serverPipe.RunAsClient(() => { result = policy.HasPermission; });
                    return result;
                },
                CancellationRequested = () => _cancellationTokenSource.Token.IsCancellationRequested,
                GetCanonicalizedPath = (path) => {
                    var result = path;
                    _serverPipe.RunAsClient(() => {
                        try {
                            result = path.CanonicalizePath();
                        } catch {
                            // path didn't canonicalize. Pity.
                        } });
                    return result;
                },

            };

            _packageManagerSession.Register(); // visible to this task and all properly behaved children

            _sessionCacheMessages = new SessionCacheMessages {
                GetInstance = (type, constructor) => {
                    lock (_sessionCache) {
                        if (!_sessionCache.ContainsKey(type)) {
                            _sessionCache.Add(type, constructor());
                        }
                        return _sessionCache[type];
                    }
                }
            };

            _sessionCacheMessages.Register();   // visible to this task and all properly behaved children

            Task readTask = null;
            SendSessionStarted(_sessionId);
            
            while (EngineService.IsRunning) {
                if (!Connected) {
                    readTask = null;

                    if (IsCancelled) {
                        return;
                    }

                    Console.WriteLine("Waiting for client to reconnect.");
                    _resetEvent.WaitOne(_maxDisconenctedWait);
                    _waitingForClientResponse = true; // debug, always drop session on timeout.

                    if (IsCancelled || (_waitingForClientResponse && !Connected)) {
                        // we're disconnected, we've waited for the duration, 
                        // we're assuming the client isn't coming back.
                        // End(); // get out of the function ... 
                        return;
                    }
                    continue;
                }

                Console.WriteLine("In Loop");

                try {
                    if (IsCancelled) {
                        return;
                    }

                    // if there is currently a task reading the from the stream, let's skip it this time.
                    if ((readTask == null || readTask.IsCompleted) && Connected) {
                        var serverInput = new byte[EngineService.BufferSize];
                        try {
                            readTask = _serverPipe.ReadAsync(serverInput, 0, serverInput.Length).AutoManage().ContinueWith(antecedent => {
                                if (antecedent.IsFaulted || antecedent.IsCanceled || !_serverPipe.IsConnected) {
                                    Disconnect();
                                    return;
                                }
                                if( antecedent.Result >= EngineService.BufferSize ) {
                                    SendUnexpectedFailure(new Exception("Message size exceeds maximum size allowed."));
                                    return;
                                }

                                var rawMessage = Encoding.UTF8.GetString(serverInput, 0, antecedent.Result);

                                if (string.IsNullOrEmpty(rawMessage)) {
                                    return;
                                }
                                var requestMessage = new UrlEncodedMessage(rawMessage);
                                var rqid = requestMessage["rqid"].ToString();
                                var dispatchTask = Dispatch(requestMessage);

                                if (!string.IsNullOrEmpty(rqid)) {
                                    dispatchTask.ContinueWith(dispatchAntecedent => {
                                        // had to force this to ensure that async writes are at least in the pipe 
                                        // before waiting on the pipe drain.
                                        // without this, it is possible that the async writes are still 'getting to the pipe' 
                                        // and not actually in the pipe, **even though the async write is complete**
                                        Thread.Sleep(50); 
                                        
                                        _responsePipe.WaitForPipeDrain();
                                        WriteAsync(new UrlEncodedMessage("task-complete") {
                                            { "rqid", rqid }
                                        });
                                    });
                                }

                                WriteErrorsOnException(dispatchTask);
                                // readTask = null;
                            }).AutoManage();

                            WriteErrorsOnException(readTask);
                        }
                        catch (Exception e) {
                            // if the pipe is broken, let's move to the disconnected state
                            Disconnect();
                        }
                    }
                    if( _isAsychronous) {
                        readTask.Wait(_cancellationTokenSource.Token);
                    } else {
                        readTask.Wait((int)_synchronousClientHeartbeat.TotalMilliseconds, _cancellationTokenSource.Token);
                    }
                    // readTask.Wait(_isAsychronous ? 6000 : (int) _synchronousClientHeartbeat.TotalMilliseconds, _cancellationTokenSource.Token);

                    if (IsCancelled) {
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

        private void WriteErrorsOnException(Task task) {
            task.ContinueWith(antecedent => {
                if (antecedent.Exception != null) {
                    foreach (var failure in antecedent.Exception.Flatten().InnerExceptions.Where(failure => failure.GetType() != typeof(AggregateException))) {
                        Console.Write(failure.GetType());
                        Console.WriteLine(failure.Message);
                        Console.WriteLine(failure.StackTrace);
                        WriteAsync(new UrlEncodedMessage("unexpected-failure") {
                            {"type", failure.GetType().ToString()},
                            {"message", failure.Message},
                            {"stacktrace", failure.StackTrace},
                        });
                    }
                }
            }, _cancellationTokenSource.Token, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);
            
        }

        /// <summary>
        ///   Dispatches the specified request message.
        /// </summary>
        /// <param name = "requestMessage">The request message.</param>
        /// <remarks>
        /// </remarks>
        private Task Dispatch(UrlEncodedMessage requestMessage) {
            Console.WriteLine("Req: {0}", requestMessage.Command);
            switch (requestMessage.Command) {
                case "find-packages":
                    // get the package names collection and run the command
                    return NewPackageManager.Instance.FindPackages( requestMessage["canonical-name"],requestMessage["name"],requestMessage["version"],requestMessage["arch"],requestMessage["public-key-token"],
                        requestMessage["dependencies"],requestMessage["installed"],requestMessage["active"],requestMessage["required"],requestMessage["blocked"],requestMessage["latest"],
                        requestMessage["index"],requestMessage["max-results"],requestMessage["location"],requestMessage["force-scan"], new PackageManagerMessages {
                            UnexpectedFailure = SendUnexpectedFailure,
                            PackageInformation = (package, supercedents) => SendFoundPackage(package,supercedents),
                            NoPackagesFound = SendNoPackagesFound,
                            PermissionRequired = SendOperationRequiresPermission,
                            Error = SendMessageArgumentError,
                            RequireRemoteFile = SendRequireRemoteFile,
                            OperationCancelled = SendCancellationRequested,
                            RequestId = requestMessage["rqid"],
                        });

                case "get-package-details":
                    return NewPackageManager.Instance.GetPackageDetails(requestMessage["canonical-name"].ToString(), new PackageManagerMessages() {
                        UnexpectedFailure = SendUnexpectedFailure,
                        PackageDetails = SendPackageDetails,
                        UnknownPackage = SendUnknownPackage,
                        Error = SendMessageArgumentError,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "install-package":
                    return NewPackageManager.Instance.InstallPackage(requestMessage["canonical-name"], requestMessage["auto-upgrade"], requestMessage["force"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        UnknownPackage = SendUnknownPackage,
                        PermissionRequired = SendOperationRequiresPermission,
                        Error = SendMessageArgumentError,
                        InstallingPackageProgress = SendInstallingPackage,
                        InstalledPackage = SendInstalledPackage,
                        FailedPackageInstall = SendFailedPackageInstall,
                        PackageBlocked = SendPackageIsBlocked,
                        RequireRemoteFile = SendRequireRemoteFile,
                        SignatureValidation = SendSignatureValidation,
                        OperationCancelled = SendCancellationRequested,
                        PackageHasPotentialUpgrades = SendPackageHasPotentialUpgrades,
                        RequestId = requestMessage["rqid"],
                    });

                case "recognize-file":
                    return NewPackageManager.Instance.RecognizeFile( requestMessage["canonical-name"], requestMessage["local-location"], requestMessage["remote-location"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        FileNotRecognized = SendUnableToRecognizeFile,
                        OperationCancelled = SendCancellationRequested,
                        FileNotFound = SendFileNotFound,
                        PackageInformation = SendFoundPackage,
                        RequestId = requestMessage["rqid"],
                    });

                case "unable-to-acquire":
                    return NewPackageManager.Instance.UnableToAcquire(requestMessage["canonical-name"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "remove-package":
                    return NewPackageManager.Instance.RemovePackage(requestMessage["canonical-name"],requestMessage["force"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        UnknownPackage = SendUnknownPackage,
                        PermissionRequired = SendOperationRequiresPermission,
                        FailedPackageRemoval = SendFailedRemovePackage,
                        RemovingPackageProgress = SendRemovingPackage,
                        RemovedPackage = SendRemovedPackage,
                        Error = SendMessageArgumentError,
                        PackageBlocked = SendPackageIsBlocked,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "set-package":
                    return NewPackageManager.Instance.SetPackage(requestMessage["canonical-name"], requestMessage["active"], requestMessage["required"], requestMessage["blocked"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        PermissionRequired = SendOperationRequiresPermission,
                        UnknownPackage = SendUnknownPackage,
                        OperationCancelled = SendCancellationRequested,
                        PackageInformation = SendFoundPackage,
                        RequestId = requestMessage["rqid"],
                    });

                case "verify-file-signature":
                    return NewPackageManager.Instance.VerifyFileSignature(requestMessage["filename"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        FileNotFound= SendFileNotFound,
                        SignatureValidation= SendSignatureValidation,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "add-feed":
                    return NewPackageManager.Instance.AddFeed(requestMessage["location"], requestMessage["session"] , new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        Warning = SendMessageWarning,
                        FeedAdded = SendFeedAdded,
                        PermissionRequired = SendOperationRequiresPermission,
                        RequireRemoteFile = SendRequireRemoteFile,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "remove-feed":
                    return NewPackageManager.Instance.RemoveFeed(requestMessage["location"], requestMessage["session"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        Warning = SendMessageWarning,
                        FeedRemoved = SendFeedRemoved,
                        PermissionRequired = SendOperationRequiresPermission,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "find-feeds":
                    return NewPackageManager.Instance.ListFeeds(requestMessage["index"], requestMessage["max-results"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        FeedDetails = SendFoundFeed,
                        NoFeedsFound = SendNoFeedsFound,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });

                case "suppress-feed":
                    return NewPackageManager.Instance.SuppressFeed(requestMessage["location"], new PackageManagerMessages {
                        UnexpectedFailure = SendUnexpectedFailure,
                        Error = SendMessageArgumentError,
                        FeedSuppressed = SendFeedSuppressed,
                        OperationCancelled = SendCancellationRequested,
                        RequestId = requestMessage["rqid"],
                    });


                default:
                    // not recognized command, return error code.
                    WriteAsync(new UrlEncodedMessage("unknown-command") {
                        { "command", requestMessage.Command }
                    });
                    return "unknown-command".AsResultTask();
            }
        }

        #region Response Messages
              
        private void SendSessionStarted(string sessionId) {
            WriteAsync(new UrlEncodedMessage("session-started") {{
                "session-id", sessionId
            }});
        }

        private void SendNoPackagesFound() {
            WriteAsync(new UrlEncodedMessage("no-packages-found"));
        }

        private void SendFoundPackage(Package package,IEnumerable<Package> supercedentPackages) {
            var msg = new UrlEncodedMessage("found-package") {
                { "canonical-name", package.CanonicalName },
                { "local-location", package.InternalPackageData.LocalPackagePath },
                { "name", package.Name },
                { "version", package.Version.UInt64VersiontoString() },
                { "arch",  package.Architecture },
                { "public-key-token", package.PublicKeyToken },
                { "installed", package.IsInstalled.ToString() },
                { "blocked", package.IsBlocked.ToString() },
                { "required", package.Required.ToString() },
                { "active", package.IsActive.ToString() },
                { "dependent", package.PackageSessionData.IsDependency.ToString() },
            };

            msg.AddCollection("remote-locations", package.InternalPackageData.RemoteLocation.Select( each => each.AbsoluteUri));
            msg.AddCollection("dependencies",  package.InternalPackageData.Dependencies.Select( each => each.CanonicalName ));
            msg.AddCollection("supercedent-packages", supercedentPackages.Select( each => each.CanonicalName ));
            
            WriteAsync(msg);
        }

        private void SendPackageDetails(Package package) {
            var msg = new UrlEncodedMessage("package-details") {
                { "canonical-name", package.CanonicalName },
                { "description", package.PackageDetails.FullDescription },
                { "summary", package.PackageDetails.SummaryDescription},
                { "display-name", package.PackageDetails.DisplayName},
                { "copyright", package.PackageDetails.CopyrightStatement},
                { "author-version", package.PackageDetails.AuthorVersion},
                { "icon", package.PackageDetails.Base64IconData},
                { "license", package.PackageDetails.License},
                { "license-url", package.PackageDetails.LicenseUrl},
                { "publish-date", package.PackageDetails.PublishDate.ToFileTime().ToString()},
                { "publisher-name", package.PackageDetails.Publisher.Name},
                { "publisher-url", package.PackageDetails.Publisher.Url},
                { "publisher-email", package.PackageDetails.Publisher.Email},
            };

            msg.AddCollection("tags",package.PackageDetails.Tags);
            if (!package.PackageDetails.Contributors.IsNullOrEmpty()) {
                msg.AddCollection("contributor-name", package.PackageDetails.Contributors.Select(each => each.Name));
                msg.AddCollection("contributor-url", package.PackageDetails.Contributors.Select(each => each.Url));
                msg.AddCollection("contributor-email", package.PackageDetails.Contributors.Select(each => each.Email));
            }
            WriteAsync(msg);
        }

        private void SendFoundFeed(string location, DateTime lastScanned, bool session, bool suppressed, bool validated) {
            WriteAsync( new UrlEncodedMessage("found-feed") {
                {"location", location},
                {"last-scanned", lastScanned.ToFileTime().ToString()},
                {"session", session},
                {"suppressed", suppressed},
                {"validated", validated},
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
            WriteAsync( new UrlEncodedMessage("failed-package-remove") {
                {"canonical-name", canonicalName},
                {"reason", reason},
            });
        }

        private void SendRequireRemoteFile(string canonicalName, IEnumerable<string> remoteLocations, string destination, bool force) {
            var msg = new UrlEncodedMessage("require-remote-file") {
                {"canonical-name", canonicalName},
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

        private void SendOperationRequiresPermission(string policyRequired) {
            WriteAsync( new UrlEncodedMessage("operation-requires-permission") {
                {"current-user-name", _userId},
                {"policy-required", policyRequired},
            });
        }

        private void SendMessageArgumentError(string messageName, string argumentName, string problem) {
            WriteAsync(new UrlEncodedMessage("message-argument-error") {
                {"message-name", messageName},
                {"argument-name", argumentName},
                {"error", problem},
            });
        }

        private void SendMessageWarning(string messageName, string argumentName, string problem) {
            WriteAsync(new UrlEncodedMessage("message-warning") {
                {"message-name", messageName},
                {"argument-name", argumentName},
                {"warning", problem},
            });
        }

        private void SendFeedAdded( string location ) {
            WriteAsync(new UrlEncodedMessage("feed-added") {
                {"location", location },
            });
        }

        private void SendFeedRemoved( string location ) {
            WriteAsync(new UrlEncodedMessage("feed-removed") {
                {"location", location },
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
            if( failure != null ) {
                WriteAsync(new UrlEncodedMessage("unexpected-failure") {
                    {"type", failure.GetType().ToString()},
                    {"message", failure.Message},
                    {"stacktrace", failure.StackTrace},
                });
            }
        }

        private void SendFeedSuppressed(string location) {
            WriteAsync(new UrlEncodedMessage("feed-suppressed") {
                {"location", location},
            });
        }

        private void SendKeepAlive() {
            WriteAsync( new UrlEncodedMessage("keep-alive"));
        }

        private void SendCancellationRequested(string message) {
            WriteAsync( new UrlEncodedMessage("operation-cancelled") {
                { "message",message }});
        }

        private void SendPackageHasPotentialUpgrades(Package package, IEnumerable<Package> supercedents ) {
            var msg = new UrlEncodedMessage("package-has-potential-upgrades") {
                {"canonical-name", package.CanonicalName},
            };
            msg.AddCollection("supercedent-packages",supercedents.Select(each => each.CanonicalName));
            
            WriteAsync(msg);
        }

        private void SendNoFeedsFound() {
            WriteAsync( new UrlEncodedMessage("no-feeds-found"));
        }
        #endregion
    }

}
