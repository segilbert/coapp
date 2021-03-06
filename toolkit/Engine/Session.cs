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
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Feeds;
    using Logging;
    using Pipes;
    using Shell;
    using Tasks;
    using Toolkit.Exceptions;
    using Win32;

    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class Session {
#if DEBUG
    // keep the reconnect window at 5 seconds for debugging
        private static readonly TimeSpan _maxDisconenctedWait = new TimeSpan(0, 0, 0, 5);
#else
        // twenty seconds in the real world
        private static TimeSpan _maxDisconenctedWait = new TimeSpan(0, 0, 00, 20);

#endif
        private static TimeSpan _synchronousClientHeartbeat = new TimeSpan(0, 0, 0, 0, 650);

        /// <summary>
        /// </summary>
        private static readonly List<Session> _activeSessions = new List<Session>();

        /// <summary>
        /// </summary>
        private readonly string _clientId;

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

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true);

        private bool _waitingForClientResponse;
        private readonly Task _task;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly bool _isAsychronous = true;

        private readonly Dictionary<string, PackageSessionData> _sessionData = new Dictionary<string, PackageSessionData>();
        private PackageManagerSession _packageManagerSession;
        private SessionCacheMessages _sessionCacheMessages;

        private readonly PackageManagerMessages _messages;

        private bool Connected {
            get { return _resetEvent.WaitOne(0); }
            set {
                if (value) {
                    _resetEvent.Set();
                } else {
                    _resetEvent.Reset();
                }
            }
        }

        public static void CancelAll() {
            while (HasActiveSessions) {
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

        public static void NotifyClientsOfRestart() {
            foreach (var s in _activeSessions.ToArray()) {
                s.SendRestarting();
                // cancel everyone.
                s._cancellationTokenSource.Cancel();
            }
        }

        public static bool HasActiveSessions {
            get { return _activeSessions.Any(); }
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
                } else {
                    var session = existingSessions.FirstOrDefault();
                    // found just one session.
                    session._serverPipe = serverPipe;
                    session._responsePipe = responsePipe;
                    Logger.Message("Rejoining existing session...");
                    session.SendSessionStarted(sessionId);
                    session.SendQueuedMessages();
                    session.Connected = true;
                    return;
                }
            } else {
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
            Logger.Message("Starting new session...");
        }

        public void End() {
            if (!_ended) {
                _ended = true;

                // remove this session.
                lock (_activeSessions) {
                    _activeSessions.Remove(this);
                }

                Logger.Message("Ending Client: [{0}]-[{1}]".format(_clientId, _sessionId));

                // end any outstanding tasks as gracefully as we can.
                _cancellationTokenSource.Cancel();

                // drop all our local session data.
                _sessionCache.Clear();
                _sessionCache = null;

                // close and clean up the pipes. 
                Disconnect();

                GC.Collect();
            }
        }

        private void Disconnect() {
            lock (this) {
                if (!Connected) {
                    return;
                }
                Connected = false;
            }

            Logger.Message("disposing of pipes: [{0}]-[{1}]".format(_clientId, _sessionId));
            try {
                if (_serverPipe != null) {
                    _serverPipe.Close();
                }
                _serverPipe = null;

                if (!_isAsychronous && _responsePipe != null) {
                    _responsePipe.Close();
                }
                _responsePipe = null;

            } catch (Exception e) {
                Logger.Error(e);
            }

            // clean up anything that can be cleaned up.
            FilesystemExtensions.RemoveTemporaryFiles();

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

            // default handlers work for all messages now.
            _messages = new PackageManagerMessages {
                Error = SendMessageArgumentError,
                FailedPackageInstall = SendFailedPackageInstall,
                FailedPackageRemoval = SendFailedRemovePackage,
                FeedAdded = SendFeedAdded,
                FeedDetails = SendFoundFeed,
                FeedRemoved = SendFeedRemoved,
                FeedSuppressed = SendFeedSuppressed,
                FileNotFound = SendFileNotFound,
                FileNotRecognized = SendUnableToRecognizeFile,
                InstalledPackage = SendInstalledPackage,
                InstallingPackageProgress = SendInstallingPackage,
                NoFeedsFound = SendNoFeedsFound,
                NoPackagesFound = SendNoPackagesFound,
                OperationCancelled = SendCancellationRequested,
                PackageBlocked = SendPackageIsBlocked,
                PackageDetails = SendPackageDetails,
                PackageHasPotentialUpgrades = SendPackageHasPotentialUpgrades,
                PackageInformation = SendFoundPackage,
                PackageSatisfiedBy = SendPackageSatifiedBy,
                PermissionRequired = SendOperationRequiresPermission,
                RemovedPackage = SendRemovedPackage,
                RemovingPackageProgress = SendRemovingPackage,
                RequireRemoteFile = SendRequireRemoteFile,
                SignatureValidation = SendSignatureValidation,
                UnexpectedFailure = SendUnexpectedFailure,
                UnknownPackage = SendUnknownPackage,
                Warning = SendMessageWarning,
            };
        

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
            if( IsCancelled ) {
                return;
            }

            Logger.Message("adding message to queue: {0}".format(response));
            Disconnect();

            lock (_outputQueue) {
                _outputQueue.Enqueue(response);
            }
        }

        private void SendQueuedMessages() {
            while (_outputQueue.Any() && _responsePipe != null) {
               
                if (IsCancelled) {
                    return;
                }

                try {
                    _responsePipe.WriteLineAsync(_outputQueue.Peek().ToString()).ContinueWith(antecedent => {
                        lock (_outputQueue) {
                            _outputQueue.Dequeue();
                        }
                    }, TaskContinuationOptions.NotOnFaulted).Wait();
                }
                catch /* (Exception e) */ {
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
            if (IsCancelled) {
                return;
            }

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
                catch /* (Exception e) */ {
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
                        }
                        catch {
                            // path didn't canonicalize. Pity.
                        }
                    });
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

            _sessionCacheMessages.Register(); // visible to this task and all properly behaved children

            Task<int> readTask = null;
            SendSessionStarted(_sessionId);

            while (EngineService.IsRunning) {
                if (!Connected) {
                    readTask = null;

                    if (IsCancelled) {
                        return;
                    }

                    Logger.Message("Waiting for client to reconnect.");
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

                try {
                    if (IsCancelled) {
                        return;
                    }

                    // if there is currently a task reading the from the stream, let's skip it this time.
                    if ((readTask == null || readTask.IsCompleted) && Connected) {
                        var serverInput = new byte[EngineService.BufferSize];
                        try {
                            // when the readasync command can finally complete, then we know that
                            // it's ok to ask it to read again.
                            readTask = _serverPipe.ReadAsync(serverInput, 0, serverInput.Length).AutoManage();
                            
                            readTask.ContinueWith(antecedent => {
                                if (antecedent.IsFaulted || antecedent.IsCanceled || !_serverPipe.IsConnected) {
                                    Disconnect();
                                    return;
                                }
                                if (antecedent.Result >= EngineService.BufferSize) {
                                    SendUnexpectedFailure(new CoAppException("Message size exceeds maximum size allowed."));
                                    return;
                                }

                                var rawMessage = Encoding.UTF8.GetString(serverInput, 0, antecedent.Result);

                                if (string.IsNullOrEmpty(rawMessage)) {
                                    return;
                                }
                                var requestMessage = new UrlEncodedMessage(rawMessage);
                                var rqid = requestMessage["rqid"].ToString();

                                // create a request cache.
                                new RequestCacheMessages().Register();

                                var dispatchTask = Dispatch(requestMessage);

                                if (!string.IsNullOrEmpty(rqid)) {
                                    if (dispatchTask == null) { // completed synchronously.
                                        WriteAsync(new UrlEncodedMessage("task-complete") {{ "rqid", rqid } });
                                    } else {
                                        dispatchTask.ContinueWith(dispatchAntecedent => {
                                            try {
                                                // had to force this to ensure that async writes are at least in the pipe 
                                                // before waiting on the pipe drain.
                                                // without this, it is possible that the async writes are still 'getting to the pipe' 
                                                // and not actually in the pipe, **even though the async write is complete**
                                                Thread.Sleep(50);
                                                if (_responsePipe != null) {
                                                    _responsePipe.WaitForPipeDrain();
                                                    WriteAsync(new UrlEncodedMessage("task-complete") {
                                                        {
                                                            "rqid", rqid
                                                            }
                                                    });
                                                }
                                            } catch (Exception e) {
                                                Logger.Error(e);
                                                // supress any exceptions.
                                            }
                                        });
                                    }
                                }

                                WriteErrorsOnException(dispatchTask);
                                // readTask = null;
                            }).AutoManage();

                            WriteErrorsOnException(readTask);
                        }
                        catch /* (Exception e) */ {
                            // if the pipe is broken, let's move to the disconnected state
                            Disconnect();
                        }
                    }
                    if (_isAsychronous) {
                        readTask.Wait(_cancellationTokenSource.Token);
                    }
                    else {
                        readTask.Wait((int) _synchronousClientHeartbeat.TotalMilliseconds, _cancellationTokenSource.Token);
                    }

                    if (IsCancelled) {
                        return;
                    }

                    if (!_isAsychronous) {
                        SendKeepAlive();
                    }
                }
                catch (AggregateException ae) {
                    if (_cancellationTokenSource.IsCancellationRequested) {
                        // ok, I'll assume you know what you're doing.
                        return;
                    }

                    foreach (var e in ae.Flatten().InnerExceptions) {
                        if (e.GetType() == typeof (IOException)) {
                            // pipe got disconnected.
                        }
                        Logger.Error(e);
                    }
                }

                catch (Exception e) {
                    // something broke. Could be a closed pipe.
                    Logger.Error(e);
                }
            }
        }

        private void WriteErrorsOnException(Task task) {
            if (IsCancelled) {
                return;
            }

            if (task != null) {
                task.ContinueWith(antecedent => {
                    if (antecedent.Exception != null) {
                        foreach (var failure in antecedent.Exception.Flatten().InnerExceptions.Where(failure => failure.GetType() != typeof(AggregateException))) {
                            if (IsCancelled) {
                                return;
                            }

                            Logger.Error(failure);
                            WriteAsync(new UrlEncodedMessage("unexpected-failure") {
                            {"type", failure.GetType().ToString()},
                            {"message", failure.Message},
                            {"stacktrace", failure.StackTrace},
                        });
                        }
                    }
                }, _cancellationTokenSource.Token, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);
            }
        }

        /// <summary>
        ///   Dispatches the specified request message.
        /// </summary>
        /// <param name = "requestMessage">The request message.</param>
        /// <remarks>
        /// </remarks>
        private Task Dispatch(UrlEncodedMessage requestMessage) {
            Logger.Message("Request:{0}".format(requestMessage.ToSmallerString()));
            if (IsCancelled) {
                SendCancellationRequested("Service is shutting down");
                return null;
            }

            switch (requestMessage.Command) {
                case "find-packages":
                    // get the package names collection and run the command
                    return NewPackageManager.Instance.FindPackages(requestMessage["canonical-name"], requestMessage["name"], requestMessage["version"],
                        requestMessage["arch"], requestMessage["public-key-token"], requestMessage["dependencies"], requestMessage["installed"],
                        requestMessage["active"], requestMessage["required"], requestMessage["blocked"], requestMessage["latest"], requestMessage["index"],
                        requestMessage["max-results"], requestMessage["location"], requestMessage["force-scan"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "get-package-details":
                    return NewPackageManager.Instance.GetPackageDetails(requestMessage["canonical-name"].ToString(), new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "install-package":
                    return NewPackageManager.Instance.InstallPackage(requestMessage["canonical-name"], requestMessage["auto-upgrade"], requestMessage["force"],
                        requestMessage["download"], requestMessage["pretend"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "download-progress":
                    return NewPackageManager.Instance.DownloadProgress(requestMessage["canonical-name"], requestMessage["progress"], new PackageManagerMessages {
                        RequestId = requestMessage["rqid"],
                    }.Extend(_messages));

                case "recognize-file":
                    return NewPackageManager.Instance.RecognizeFile(requestMessage["canonical-name"], requestMessage["local-location"],
                        requestMessage["remote-location"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "unable-to-acquire":
                    return NewPackageManager.Instance.UnableToAcquire(requestMessage["canonical-name"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "remove-package":
                    return NewPackageManager.Instance.RemovePackage(requestMessage["canonical-name"], requestMessage["force"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "set-package":
                    return NewPackageManager.Instance.SetPackage(requestMessage["canonical-name"], requestMessage["active"], requestMessage["required"],
                        requestMessage["blocked"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "verify-file-signature":
                    return NewPackageManager.Instance.VerifyFileSignature(requestMessage["filename"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "add-feed":
                    return NewPackageManager.Instance.AddFeed(requestMessage["location"], requestMessage["session"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "remove-feed":
                    return NewPackageManager.Instance.RemoveFeed(requestMessage["location"], requestMessage["session"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "find-feeds":
                    return NewPackageManager.Instance.ListFeeds(requestMessage["index"], requestMessage["max-results"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "suppress-feed":
                    return NewPackageManager.Instance.SuppressFeed(requestMessage["location"], new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages));

                case "get-policy":
                    return Task.Factory.StartNew(() => {
                        new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages).Register();

                        var policyName = requestMessage["name"].ToString();
                        var policy = PermissionPolicy.AllPolicies.Where(each => each.Name.Equals(policyName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                        if( policy == null ) {
                            SendMessageArgumentError("get-policy", "name", "policy '{0}' not found".format(policyName));
                            return;
                        }

                        var msg = new UrlEncodedMessage("policy") {
                            { "name" , policy.Name },
                            { "description" , policy.Description},
                        };
                        // msg.AddCollection("sids",policy.Sids);
                        msg.AddCollection("accounts", policy.Accounts);

                        WriteAsync(msg);
                    });

                case "add-to-policy":
                    return Task.Factory.StartNew(() => {
                        new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages).Register();

                        if (!_packageManagerSession.CheckForPermission(PermissionPolicy.ModifyPolicy)) {
                            PackageManagerMessages.Invoke.PermissionRequired("ModifyPolicy");
                            return;
                        }


                        var policyName = requestMessage["name"].ToString();
                        var policy = PermissionPolicy.AllPolicies.Where(each => each.Name.Equals(policyName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                        if (policy == null) {
                            SendMessageArgumentError("add-to-policy", "name", "policy '{0}' not found".format(policyName));
                            return;
                        }

                        try {
                            policy.Add(requestMessage["account"]);
                        } catch {
                            SendMessageArgumentError("add-to-policy", "account", "policy '{0}' could not add account '{1}'".format(policyName, requestMessage["account"]));
                        }

                    });

                case "remove-from-policy":
                    return Task.Factory.StartNew(() => {
                        new PackageManagerMessages {
                            RequestId = requestMessage["rqid"],
                        }.Extend(_messages).Register();

                        if (!_packageManagerSession.CheckForPermission(PermissionPolicy.ModifyPolicy)) {
                            PackageManagerMessages.Invoke.PermissionRequired("ModifyPolicy");
                            return;
                        }

                        var policyName = requestMessage["name"].ToString();
                        var policy = PermissionPolicy.AllPolicies.Where(each => each.Name.Equals(policyName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                        if (policy == null) {
                            SendMessageArgumentError("remove-from-policy", "name", "policy '{0}' not found".format(policyName));
                            return;
                        }

                        try {
                            policy.Remove(requestMessage["account"]);
                        } catch {
                            SendMessageArgumentError("remove-from-policy", "account", "policy '{0}' could not remove account '{1}'".format(policyName, requestMessage["account"]));
                        }
                    });

                case "symlink" :
                    if (!_packageManagerSession.CheckForPermission(PermissionPolicy.Symlink)) {
                        PackageManagerMessages.Invoke.PermissionRequired("Symlink");
                        return null;
                    }

                    var existingLocation = requestMessage["existing-location"].ToString();

                    if (string.IsNullOrEmpty(existingLocation)) {
                        PackageManagerMessages.Invoke.Error("symlink", "existing-location", "location is null/empty. ");
                        return null; 
                    }

                    var newLink = requestMessage["new-link"].ToString();

                    if (string.IsNullOrEmpty(newLink)) {
                        PackageManagerMessages.Invoke.Error("symlink", "new-link", "new-link is null/empty.");
                        return null;
                    }

                    
                    LinkType linkType;
                    if(! Enum.TryParse(requestMessage["link-type"].ToString(), true, out linkType) ) {
                        PackageManagerMessages.Invoke.Error("symlink", "link-type", "link-type is invalid.");
                        return null;
                    }
                    try {
                        if (existingLocation.FileIsLocalAndExists()) {
                            // source is a file
                            switch (linkType) {
                                case LinkType.Symlink:
                                    Symlink.MakeFileLink(newLink, existingLocation);
                                    break;

                                case LinkType.Hardlink:
                                    Kernel32.CreateHardLink(newLink, existingLocation, IntPtr.Zero);
                                    break;

                                case LinkType.Shortcut:
                                    ShellLink.CreateShortcut(newLink, existingLocation);
                                    break;
                            }
                        }

                        if (existingLocation.DirectoryExistsAndIsAccessible()) {
                            // source is a folder
                            switch (linkType) {
                                case LinkType.Symlink:
                                    Symlink.MakeDirectoryLink(newLink, existingLocation);
                                    break;

                                case LinkType.Hardlink:
                                    Kernel32.CreateHardLink(newLink, existingLocation, IntPtr.Zero);
                                    break;

                                case LinkType.Shortcut:
                                    ShellLink.CreateShortcut(newLink, existingLocation);
                                    break;
                            }
                        }

                        PackageManagerMessages.Invoke.Error("symlink", "existing-location", "can not make symlink for location '{0}'".format(existingLocation));
                    } catch (Exception exception) {
                            PackageManagerMessages.Invoke.Error("symlink", "", "Failed to create symlink -- error: {0}".format(exception.Message));    
                    }
                    return null;

                case "stop-service":
                    if (PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.StopService)) {
                        _cancellationTokenSource.Cancel();
                        EngineService.RequestStop();
                        return "Shutting down".AsResultTask();
                    }
                    return null; // "Unable to Stop Service".AsResultTask();

                case "set-logging" :
                    try {
                        var b = (bool?)requestMessage["messages"];
                        if (b.HasValue) {
                            SessionCache<string>.Value["LogMessages"] = b.ToString();
                        }
                        b = (bool?)requestMessage["errors"];
                        if (b.HasValue) {
                            SessionCache<string>.Value["LogErrors"] = b.ToString();
                        }
                        b = (bool?)requestMessage["warnings"];
                        if (b.HasValue) {
                            SessionCache<string>.Value["LogWarnings"] = b.ToString();
                        }
                        WriteAsync(new UrlEncodedMessage("done-set-logging") {
                        {"is-logging-errors", Logger.Errors },
                        {"is-logging-warnings", Logger.Warnings },
                        {"is-logging-messages", Logger.Messages },
                        {"rqid", requestMessage["rqid"].ToString() },
                    });
                    } catch {
                    }
                    return null; //"set-logging".AsResultTask();

                default:
                    // not recognized command, return error code.
                    WriteAsync(new UrlEncodedMessage("unknown-command") {
                        {"command", requestMessage.Command},
                        {"rqid", requestMessage["rqid"].ToString() },
                    });
                    return null; // "unknown-command".AsResultTask();
            }
        }

        #region Response Messages

        private void SendSessionStarted(string sessionId) {
            WriteAsync(new UrlEncodedMessage("session-started") {
                {"session-id", sessionId}
            });
        }

        private void SendNoPackagesFound() {
            WriteAsync(new UrlEncodedMessage("no-packages-found"));
        }

        private void SendFoundPackage(Package package, IEnumerable<Package> supercedentPackages) {
            var msg = new UrlEncodedMessage("found-package") {
                {"canonical-name", package.CanonicalName},
                {"local-location", package.InternalPackageData.LocalLocation},
                {"name", package.Name},
                {"version", package.Version.ToString()},
                {"arch", package.Architecture.ToString()},
                {"public-key-token", package.PublicKeyToken},
                {"product-code", package.ProductCode.ToString()},
                {"installed", package.IsInstalled.ToString()},
                {"blocked", package.IsBlocked.ToString()},
                {"required", package.IsRequired.ToString()},
                {"client-required", package.IsClientRequired.ToString()},
                {"active", package.IsActive.ToString()},
                {"dependent", package.PackageSessionData.IsDependency.ToString()},
            };

            msg.AddCollection("remote-locations", package.InternalPackageData.RemoteLocations);
            msg.AddCollection("dependencies", package.InternalPackageData.Dependencies.Select(each => each.CanonicalName));
            msg.AddCollection("supercedent-packages", supercedentPackages.Select(each => each.CanonicalName));

            WriteAsync(msg);
        }

        private void SendPackageDetails(Package package) {
            var msg = new UrlEncodedMessage("package-details") {
                {"canonical-name", package.CanonicalName},
                {"description", package.PackageDetails.Description},
                {"summary", package.PackageDetails.SummaryDescription},
                {"display-name", package.DisplayName},
                {"copyright", package.PackageDetails.CopyrightStatement},
                {"author-version", package.PackageDetails.AuthorVersion},
                {"icon", package.PackageDetails.IconLocations.FirstOrDefault()},
                // {"license", package.PackageDetails.License},
                // {"license-url", package.PackageDetails.LicenseUrl},
                {"license", "Comming soon: License Data Is changing to support multiple licenses."},
                {"license-url", "http://Comming_soon_License_Data_Is_changing_to_support_multiple_licenses."},

                {"publish-date", package.PackageDetails.PublishDate.ToFileTime().ToString()},
                {"publisher-name", package.PackageDetails.Publisher.Name},
                {"publisher-url", package.PackageDetails.Publisher.Location == null ? string.Empty : package.PackageDetails.Publisher.Location.AbsoluteUri},
                {"publisher-email", package.PackageDetails.Publisher.Email},
                {"package-item-text", package.PackageDetails.GetAtomItemText(package) },
            };

            package.InternalPackageData.Roles.ForEach(each => msg.AddKeyValuePair("role", each.Name, each.PackageRole.ToString()));
            //msg.AddKeyValueCollection("roles", package.InternalPackageData.Roles.Select( each => new KeyValuePair<string, string>(each.Name, each.PackageRole.ToString())));

            msg.AddCollection("tags", package.PackageDetails.Tags);

            if (!package.PackageDetails.Contributors.IsNullOrEmpty()) {
                msg.AddCollection("contributor-name", package.PackageDetails.Contributors.Select(each => each.Name));
                msg.AddCollection("contributor-url", package.PackageDetails.Contributors.Select(each => each.Location.AbsoluteUri));
                msg.AddCollection("contributor-email", package.PackageDetails.Contributors.Select(each => each.Email));
            }
            WriteAsync(msg);
        }

        private void SendFoundFeed(string location, DateTime lastScanned, bool session, bool suppressed, bool validated) {
            WriteAsync(new UrlEncodedMessage("found-feed") {
                {"location", location},
                {"last-scanned", lastScanned.ToFileTime().ToString()},
                {"session", session},
                {"suppressed", suppressed},
                {"validated", validated},
            });
        }

        private void SendInstallingPackage(string canonicalName, int percentComplete, int overallProgress) {
            WriteAsync(new UrlEncodedMessage("installing-package") {
                {"canonical-name", canonicalName},
                {"percent-complete", percentComplete.ToString()},
                {"overall-percent-complete", overallProgress.ToString()},
            });
        }

        private void SendRemovingPackage(string canonicalName, int percentComplete) {
            WriteAsync(new UrlEncodedMessage("removing-package") {
                {"canonical-name", canonicalName},
                {"percent-complete", percentComplete.ToString()},
            });
        }

        private void SendInstalledPackage(string canonicalName) {
            WriteAsync(new UrlEncodedMessage("installed-package") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendRemovedPackage(string canonicalName) {
            WriteAsync(new UrlEncodedMessage("removed-package") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendFailedPackageInstall(string canonicalName, string filename, string reason) {
            WriteAsync(new UrlEncodedMessage("failed-package-install") {
                {"canonical-name", canonicalName},
                {"filename", filename},
                {"reason", reason},
            });
        }

        private void SendFailedRemovePackage(string canonicalName, string reason) {
            WriteAsync(new UrlEncodedMessage("failed-package-remove") {
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
            WriteAsync(new UrlEncodedMessage("signature-validation") {
                {"filename", filename},
                {"is-valid", isValid.ToString()},
                {"certificate-subject-name", certificateSubjectName ?? string.Empty},
            });
        }

        private void SendOperationRequiresPermission(string policyRequired) {
            WriteAsync(new UrlEncodedMessage("operation-requires-permission") {
                {"current-user-name", _userId},
                {"policy-required", policyRequired},
            });
        }

        private void SendMessageArgumentError(string messageName, string argumentName, string problem) {
            WriteAsync(new UrlEncodedMessage("message-argument-error") {
                {"message", messageName},
                {"parameter", argumentName},
                {"reason", problem},
            });
        }

        private void SendMessageWarning(string messageName, string argumentName, string problem) {
            WriteAsync(new UrlEncodedMessage("message-warning") {
                {"message", messageName},
                {"parameter", argumentName},
                {"reason", problem},
            });
        }

        private void SendPackageSatifiedBy( Package requested, Package satisfiedBy  ) {
            WriteAsync(new UrlEncodedMessage("package-satisfied-by") {
                {"canonical-name", requested.CanonicalName},
                {"satisfied-by", satisfiedBy.CanonicalName},
            });
        }

        private void SendFeedAdded(string location) {
            WriteAsync(new UrlEncodedMessage("feed-added") {
                {"location", location},
            });
        }

        private void SendFeedRemoved(string location) {
            WriteAsync(new UrlEncodedMessage("feed-removed") {
                {"location", location},
            });
        }

        private void SendFileNotFound(string filename) {
            WriteAsync(new UrlEncodedMessage("file-not-found") {
                {"filename", filename},
            });
        }

        private void SendUnknownPackage(string canonicalName) {
            WriteAsync(new UrlEncodedMessage("unknown-package") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendPackageIsBlocked(string canonicalName) {
            WriteAsync(new UrlEncodedMessage("package-is-blocked") {
                {"canonical-name", canonicalName},
            });
        }

        private void SendUnableToRecognizeFile(string filename, string reason) {
            WriteAsync(new UrlEncodedMessage("unable-to-recognize-file") {
                {"filename", filename},
                {"reason", reason},
            });
        }

        private void SendUnexpectedFailure(Exception failure) {
            if (failure != null) {
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
            WriteAsync(new UrlEncodedMessage("keep-alive"));
        }

        private void SendCancellationRequested(string message) {
            WriteAsync(new UrlEncodedMessage("operation-cancelled") {
                {"message", message}
            });
        }

        private void SendPackageHasPotentialUpgrades(Package package, IEnumerable<Package> supercedents) {
            var msg = new UrlEncodedMessage("package-has-potential-upgrades") {
                {"canonical-name", package.CanonicalName},
            };
            msg.AddCollection("supercedent-packages", supercedents.Select(each => each.CanonicalName));

            WriteAsync(msg);
        }

        private void SendNoFeedsFound() {
            WriteAsync(new UrlEncodedMessage("no-feeds-found"));
        }

        private void SendRestarting() {
            WriteAsync(new UrlEncodedMessage("restarting"));
        }

        private void SendShuttingDown() {
            WriteAsync(new UrlEncodedMessage("shutting-down"));
        }

        #endregion
    }
}