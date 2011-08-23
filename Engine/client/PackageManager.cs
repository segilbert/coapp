//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using System.IO.Pipes;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipes;
    using Tasks;
    using Console = System.Console;

    public class Package {

    };

    

    public class PackageManager {
        internal class ManualEventQueue : Queue<UrlEncodedMessage>, IDisposable {
            internal static readonly Dictionary<int, ManualEventQueue> _eventQueues = new Dictionary<int, ManualEventQueue>();
            internal static readonly ManualResetEvent IsCompleted = new ManualResetEvent(true);

            public readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(true);

            public ManualEventQueue() {
                IsCompleted.Reset();
                var tid = Task.CurrentId.GetValueOrDefault();
                if (tid == 0) {
                    throw new Exception("Cannot create a ManualEventQueue outside of a task.");
                }
                lock (_eventQueues) {
                    _eventQueues.Add(tid, this);
                }
            }

            public void Dispose() {
                lock (_eventQueues) {
                    _eventQueues.Remove(Task.CurrentId.GetValueOrDefault());
                    if( _eventQueues.Count ==  0) {
                        Console.WriteLine("Completed: True");
                        IsCompleted.Set();
                    }
                }
            }

            public static ManualEventQueue GetQueueForTaskId(int taskId) {
                return _eventQueues[taskId];
            }

            internal void DispatchResponses() {
                var continueHandlingMessages = true;

                while (continueHandlingMessages && ManualResetEvent.WaitOne()) {
                    ManualResetEvent.Reset();
                    while (Count > 0 && continueHandlingMessages) {
                        continueHandlingMessages = Dispatch(Dequeue());
                    }
                }
            }
        }

        public static PackageManager Instance = new PackageManager();

        private NamedPipeClientStream _pipe;
        internal const int BufferSize = 8192;
        public ManualResetEvent IsReady = new ManualResetEvent(false);
        public ManualResetEvent IsDisconnected = new ManualResetEvent(true);
        public int ActiveCalls { get { return ManualEventQueue._eventQueues.Keys.Count; } }
        public ManualResetEvent IsCompleted { get { return ManualEventQueue.IsCompleted; } }

        private bool IsConnected {
            get {
                return _pipe != null && _pipe.IsConnected;
            }
        }

        private PackageManager() {
               
        }

        public Task Connect(string clientName, string sessionId = null ) {
            if (IsConnected)
                return null;

            sessionId = sessionId ?? DateTime.Now.Ticks.ToString();
            
            return Task.Factory.StartNew(() => {
                _pipe = new NamedPipeClientStream(".", "CoAppInstaller", PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

                try {
                    _pipe.Connect();
                    _pipe.ReadMode = PipeTransmissionMode.Message;
                }
                catch {
                    _pipe = null;
                    throw new Exception("Unable to connect to CoApp Service");
                }

                IsDisconnected.Reset();

                StartSession(clientName, sessionId);

                while( IsConnected  ) {
                    var incomingMessage =
                        new byte[BufferSize];

                    _pipe.ReadAsync(incomingMessage, 0, BufferSize).ContinueWith(antecedent => {
                        if (antecedent.IsCanceled || antecedent.IsFaulted || !IsConnected ) {
                            Disconnect();
                            return;
                        }

                        var rawMessage = Encoding.UTF8.GetString(incomingMessage, 0, antecedent.Result);

                        if (string.IsNullOrEmpty(rawMessage)) {
                            return;
                        }

                        var responseMessage = new UrlEncodedMessage(rawMessage);
                        int? rqid = responseMessage["rqid"];
                        Console.WriteLine("    Response:{0}", responseMessage.Command);

                        try {
                            var mreq = ManualEventQueue.GetQueueForTaskId(rqid.GetValueOrDefault());
                            mreq.Enqueue(responseMessage);
                            mreq.ManualResetEvent.Set();
                        } catch {
                            if( responseMessage.Command.Equals("session-started") ) {
                                IsReady.Set();
                                return;
                            }
                            Console.WriteLine("Unable to queue the response to the right request event queue!");
                            Console.WriteLine("    Response:{0}", responseMessage.Command);
                            // not able to queue up the response to the right task?
                        }
                    });
                }

                Disconnect();
            });
        }

        public void Disconnect() {
            var pipe = _pipe;
            _pipe = null; 
            pipe.Close();
            pipe.Dispose();
            IsDisconnected.Set();
        }

        public Task FindPackages(string canonicalName, string name, string version, string arch, string publicKeyToken,
            bool? dependencies, bool? installed, bool? active, bool? required, bool? blocked, bool? latest,
            int? index, int? maxResults, string location, bool? forceScan, PackageManagerMessages messages) {
            IsCompleted.Reset();
                return Task.Factory.StartNew(() => {
                    messages.Register();
                    using( var eventQueue = new ManualEventQueue() ) { 
                        WriteAsync(new UrlEncodedMessage("find-packages") {
                            {"canonical-name" , canonicalName },
                            {"name", name},
                            {"version", version },
                            {"arch", arch },
                            {"public-key-token", publicKeyToken },
                            {"dependencies", dependencies  },
                            {"installed", installed },
                            {"active", active },
                            {"required", required },
                            {"blocked", blocked },
                            {"latest", latest },
                            {"index", index },
                            {"max-results", maxResults },
                            {"location", location },
                            {"force-scan", forceScan },

                            {"rqid",  Task.CurrentId},
                        });

                        // will return when the final message comes thru.
                        eventQueue.DispatchResponses();
                    }
                }).AutoManage();
        }

        public Task GetPackageDetails(string canonicalName, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("get-package-details") {
                            {"canonical-name" , canonicalName },
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task InstallPackage(string canonicalName, bool? autoUpgrade, bool? force, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("install-package") {
                            {"canonical-name" , canonicalName },
                            {"auto-upgrade" , autoUpgrade},
                            {"force" , force},
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task ListFeeds(int? index, int? maxResults, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("find-feeds") {
                            {"index" , index },
                            {"max-results" , maxResults },
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task RemoveFeed(string location, bool? session, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("remove-feed") {
                            {"location" , location},
                            {"session" , session},
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task AddFeed(string location, bool? session, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("add-feed") {
                            {"location" , location },
                            {"session" , session},
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task VerifyFileSignature(string filename, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("verify-file-signature") {
                            {"filename" , filename },
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task SetPackage(string canonicalName, bool? active, bool? required, bool? blocked, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("set-package") {
                            {"canonical-name" , canonicalName },
                            {"active" , active},
                            {"required" , required},
                            {"blocked" , blocked},

                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task RemovePackage(string canonicalName, bool? force, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("remove-package") {
                            {"canonical-name" , canonicalName },
                            {"force" , force},
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task UnableToAcquire(string canonicalName, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("unable-to-acquire") {
                            {"canonical-name" , canonicalName },
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task RecognizeFile(string canonicalName, string localLocation, string remoteLocation, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("recongnize-file") {
                            {"canonical-name" , canonicalName },
                            {"local-location" , localLocation},
                            {"remote-location" , remoteLocation},
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task SuppressFeed(string location, PackageManagerMessages messages) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                messages.Register();
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("suppress-feed") {
                            {"location" , location },
                            
                            {"rqid",  Task.CurrentId},
                        });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        internal static bool Dispatch(UrlEncodedMessage responseMessage) {
            switch (responseMessage.Command) {
                case "failed-package-install":
                    PackageManagerMessages.Invoke.FailedPackageInstall(responseMessage["canonical-name"], responseMessage["filename"], responseMessage["reason"]);
                    break;

                case "failed-package-remove":
                    PackageManagerMessages.Invoke.FailedPackageRemoval(responseMessage["canonical-name"], responseMessage["reason"]);
                    break;

                case "feed-added":
                    PackageManagerMessages.Invoke.FeedAdded(responseMessage["location"]);
                    break;

                case "feed-removed":
                    PackageManagerMessages.Invoke.FeedRemoved(responseMessage["location"]);
                    break;

                case "feed-suppressed":
                    PackageManagerMessages.Invoke.FeedSuppressed(responseMessage["location"]);
                    break;

                case "file-not-found":
                    PackageManagerMessages.Invoke.FileNotFound(responseMessage["filename"]);
                    break;

                case "found-feed":
                    PackageManagerMessages.Invoke.FeedDetails(responseMessage["location"], DateTime.FromFileTime((long?)responseMessage["last-scanned"] ?? 0), (bool?)responseMessage["session"] ?? false, (bool?)responseMessage["suppressed"] ?? false, (bool?)responseMessage["validated"] ?? false);
                    break;

                case "found-package":
                    PackageManagerMessages.Invoke.PackageInformation(new Package(), Enumerable.Empty<Package>());
                    break;

                case "installed-package":
                    PackageManagerMessages.Invoke.InstalledPackage(responseMessage["canonical-name"]);
                    break;

                case "installing-package":
                    PackageManagerMessages.Invoke.InstallingPackageProgress(responseMessage["canonical-name"], (int?)responseMessage["percent-complete"] ?? 0);
                    break;

                case "message-argument-error":
                    PackageManagerMessages.Invoke.Error(responseMessage["message"], responseMessage["parameter"], responseMessage["reason"]);
                    break;

                case "message-warning":
                    PackageManagerMessages.Invoke.Warning(responseMessage["message"], responseMessage["parameter"], responseMessage["reason"]);
                    break;

                case "no-feeds-found":
                    PackageManagerMessages.Invoke.NoFeedsFound();
                    break;

                case "no-packages-found":
                    PackageManagerMessages.Invoke.NoPackagesFound();
                    break;

                case "operation-cancelled":
                    PackageManagerMessages.Invoke.OperationCancelled(responseMessage["message"]);
                    break;

                case "operation-requires-permission":
                    PackageManagerMessages.Invoke.PermissionRequired(responseMessage["policy-required"]);
                    break;

                case "package-details":
                    PackageManagerMessages.Invoke.PackageDetails(new Package());
                    break;

                case "package-has-potential-upgrades":
                    PackageManagerMessages.Invoke.PackageHasPotentialUpgrades(new Package(), Enumerable.Empty<Package>());
                    break;

                case "package-is-blocked":
                    PackageManagerMessages.Invoke.PackageBlocked(responseMessage["canonical-name"]);
                    break;

                case "removed-package":
                    PackageManagerMessages.Invoke.RemovedPackage(responseMessage["canonical-name"]);
                    break;

                case "removing-package":
                    PackageManagerMessages.Invoke.RemovingPackageProgress(responseMessage["canonical-name"], (int?)responseMessage["percent-complete"] ?? 0);
                    break;

                case "require-remote-file":
                    PackageManagerMessages.Invoke.RequireRemoteFile(responseMessage["canonical-name"], responseMessage.GetCollection("remote-locations"), responseMessage["destination"],
                        (bool?) responseMessage["force"] ?? false);
                    break;

                case "signature-validation":
                    PackageManagerMessages.Invoke.SignatureValidation(responseMessage["filename"], (bool?)responseMessage["is-valid"] ?? false, responseMessage["certificate-subject-name"]);
                    break;

                case "unable-to-recognize-file":
                    PackageManagerMessages.Invoke.FileNotRecognized(responseMessage["filename"], responseMessage["reason"]);
                    break;

                case "unexpected-failure":
                    // PackageManagerMessages.Invoke.UnexpectedFailure( responseMessage["type"], responseMessage["message"], responseMessage["stacktrace"]);
                    break;

                case "unknown-package":
                    PackageManagerMessages.Invoke.UnknownPackage(responseMessage["canonical-name"]);
                    break;

                case "unknown-command":
                    Console.WriteLine("Unknown command!");
                    break;

                case "task-complete":
                    return false;
            }

            return true;
        }

        /// <summary>
        ///   Writes the message to the stream asyncly.
        /// </summary>
        /// <param name = "message">The request.</param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        private void WriteAsync(UrlEncodedMessage message) {
            if (IsConnected) {
                try {
                    _pipe.WriteLineAsync(message.ToString()).ContinueWith(antecedent => { System.Console.WriteLine("Async Write Fail!? (1)"); }, TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (Exception e) {
                     System.Console.WriteLine("Async Write Fail!? (2)");
                }
            }
        }

        private void StartSession(string clientId, string sessionId ) {
            WriteAsync(new UrlEncodedMessage("start-session") {
                {"client" , clientId },
                {"id"  , sessionId },
                {"rqid"  , sessionId },
            });
        }
    }
}
