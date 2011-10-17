//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Client {
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
    using Pipes;
    using Tasks;

    public class PackageManager {
        internal class ManualEventQueue : Queue<UrlEncodedMessage>, IDisposable {
            internal static readonly Dictionary<int, ManualEventQueue> EventQueues = new Dictionary<int, ManualEventQueue>();
            internal static readonly ManualResetEvent IsCompleted = new ManualResetEvent(true);

            public readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(true);

            public ManualEventQueue() {
                IsCompleted.Reset();
                var tid = Task.CurrentId.GetValueOrDefault();
                if (tid == 0) {
                    throw new Exception("Cannot create a ManualEventQueue outside of a task.");
                }
                lock (EventQueues) {
                    EventQueues.Add(tid, this);
                }
            }

            public void Dispose() {
                lock (EventQueues) {
                    EventQueues.Remove(Task.CurrentId.GetValueOrDefault());
                    if (EventQueues.Count == 0) {
                        // Console.WriteLine("Completed: True");
                        IsCompleted.Set();
                    }
                }
            }

            public static ManualEventQueue GetQueueForTaskId(int taskId) {
                return EventQueues[taskId];
            }

            internal void DispatchResponses() {
                var continueHandlingMessages = true;

                while (continueHandlingMessages && ManualResetEvent.WaitOne()) {
                    ManualResetEvent.Reset();
                    while (Count > 0) {
                        if (!Dispatch(Dequeue())) {
                            continueHandlingMessages = false;
                        }
                    }
                }
            }
        }

        public static PackageManager Instance = new PackageManager();

        private Task _serviceTask;
        private NamedPipeClientStream _pipe;
        internal const int BufferSize = 8192;
        public ManualResetEvent IsReady = new ManualResetEvent(false);
        public ManualResetEvent IsDisconnected = new ManualResetEvent(true);

        public int ActiveCalls {
            get { return ManualEventQueue.EventQueues.Keys.Count; }
        }

        public ManualResetEvent IsCompleted {
            get { return ManualEventQueue.IsCompleted; }
        }

        private bool IsConnected {
            get { return _pipe != null && _pipe.IsConnected; }
        }

        private PackageManager() {
        }

        public Task Connect(string clientName, string sessionId = null) {
            if (IsConnected) {
                return _serviceTask;
            }

#if DEBUG
            EngineServiceManager.EnsureServiceIsResponding(true);
#else
                  EngineServiceManager.EnsureServiceIsResponding();
#endif

            sessionId = sessionId ?? DateTime.Now.Ticks.ToString();

            return _serviceTask = Task.Factory.StartNew(() => {
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

                while (IsConnected) {
                    var incomingMessage = new byte[BufferSize];

                    try {

                        var readTask = _pipe.ReadAsync(incomingMessage, 0, BufferSize);

                        readTask.ContinueWith(antecedent => {
                            if (antecedent.IsCanceled || antecedent.IsFaulted || !IsConnected) {
                                Disconnect();
                                return;
                            }

                            var rawMessage = Encoding.UTF8.GetString(incomingMessage, 0, antecedent.Result);

                            if (string.IsNullOrEmpty(rawMessage)) {
                                return;
                            }

                            var responseMessage = new UrlEncodedMessage(rawMessage);
                            int? rqid = responseMessage["rqid"];
                            Debug.WriteLine("Response:{0}".format(responseMessage.ToString()));

                            try {
                                var mreq = ManualEventQueue.GetQueueForTaskId(rqid.GetValueOrDefault());
                                mreq.Enqueue(responseMessage);
                                mreq.ManualResetEvent.Set();
                            }
                            catch {
                                if (responseMessage.Command.Equals("session-started")) {
                                    IsReady.Set();
                                    return;
                                }
                                //  Console.WriteLine("Unable to queue the response to the right request event queue!");
                                // Console.WriteLine("    Response:{0}", responseMessage.Command);
                                // not able to queue up the response to the right task?
                            }
                        }).AutoManage();
                        readTask.Wait();
                    }
                    catch {
                        Disconnect();
                    }
                }
                Disconnect();
            });
        }

        public void Disconnect() {
            lock (this) {
                if (_pipe != null) {
                    var pipe = _pipe;
                    _pipe = null;
                    pipe.Close();
                    pipe.Dispose();
                    IsDisconnected.Set();
                }
            }
        }

        public Task<IEnumerable<Package>> GetPackages(IEnumerable<string> parameters, ulong? minVersion = null, ulong? maxVersion = null,
            bool? dependencies = null, bool? installed = null, bool? active = null, bool? required = null, bool? blocked = null, bool? latest = null,
            string location = null, bool? forceScan = null, PackageManagerMessages messages = null) {
            if (parameters.IsNullOrEmpty()) {
                return GetPackages(string.Empty, minVersion, maxVersion, dependencies, installed, active, required, blocked, latest, location, forceScan,
                    messages);
            }

            // spawn the tasks off in parallel
            var tasks =
                parameters.Select(
                    each => GetPackages(each, minVersion, maxVersion, dependencies, installed, active, required, blocked, latest, location, forceScan, messages))
                    .ToArray();

            // return a task that is the sum of all the tasks.
            return Task<IEnumerable<Package>>.Factory.ContinueWhenAll(tasks, antecedents => {

                var faulted = tasks.Where(each => each.IsFaulted);
                if( faulted.Any()) {
                    throw faulted.FirstOrDefault().Exception.Flatten().InnerExceptions.FirstOrDefault();
                }
               return tasks.SelectMany(each => each.Result).Distinct();
            },
                TaskContinuationOptions.AttachedToParent);
        }

        public Task<IEnumerable<Package>> GetPackages(string parameter, ulong? minVersion = null, ulong? maxVersion = null, bool? dependencies = null,
            bool? installed = null, bool? active = null, bool? required = null, bool? blocked = null, bool? latest = null, string location = null,
            bool? forceScan = null, PackageManagerMessages messages = null) {
            var packages = new List<Package>();

            if (parameter.IsNullOrEmpty()) {
                return FindPackages( /* canonicalName:*/
                    null, /* name */null, /* version */null, /* arch */ null, /* pkt */null, dependencies, installed, active, required, blocked, latest,
                    /* index */null, /* max-results */null, location, forceScan, new PackageManagerMessages {
                        PackageInformation = package => packages.Add(package),
                    }.Extend(messages)).ContinueWith(antecedent => {
                        if( antecedent.IsFaulted || antecedent.IsCanceled ) {
                            throw antecedent.Exception.Flatten().InnerExceptions.FirstOrDefault();
                        }
                        return packages as IEnumerable<Package>;
                    }, TaskContinuationOptions.AttachedToParent);
            }

            Package singleResult = null;
            var feedAdded = string.Empty;

            if (File.Exists(parameter)) {
                var localPath = parameter.EnsureFileIsLocal();
                var originalDirectory = Path.GetDirectoryName(parameter.GetFullPath());
                // add the directory it came from as a session package feed

                if (!string.IsNullOrEmpty(localPath)) {
                    return RecognizeFile(null, localPath, null, new PackageManagerMessages {
                        PackageInformation = package => { singleResult = package; },
                        FeedAdded = feedLocation => { feedAdded = feedLocation; }
                    }.Extend(messages)).ContinueWith(antecedent => {
                        if (singleResult != null) {
                            return AddFeed(originalDirectory, true, new PackageManagerMessages {
                                // don't have to handle any messages here...
                            }.Extend(messages)).ContinueWith(antecedent2 => singleResult.SingleItemAsEnumerable(), TaskContinuationOptions.AttachedToParent).
                                Result;
                        }

                        // if it was a feed, then continue with the big query
                        if (feedAdded != null) {
                            return
                                InternalGetPackages(null, minVersion, maxVersion, dependencies, installed, active, required, blocked, latest, feedAdded,
                                    forceScan, messages).Result;
                        }

                        // if we get here, that means that we didn't recognize the file. 
                        // we're gonna return an empty collection at this point.
                        return singleResult.SingleItemAsEnumerable();
                    }, TaskContinuationOptions.AttachedToParent);
                }
                // if we don't get back a local path for the file... this is pretty odd. DUnno what we should really do here yet.
                return Enumerable.Empty<Package>().AsResultTask();
            }

            if (Directory.Exists(parameter) || parameter.IndexOf('\\') > -1 || parameter.IndexOf('/') > -1 ||
                (parameter.IndexOf('*') > -1 && parameter.ToLower().EndsWith(".msi"))) {
                // specified a folder, or some kind of path that looks like a feed.
                // add it as a feed, and then get the contents of that feed.
                return AddFeed(parameter, true, new PackageManagerMessages {
                    FeedAdded = feedLocation => { feedAdded = feedLocation; }
                }.Extend(messages)).ContinueWith(antecedent => {
                    // if it was a feed, then continue with the big query
                    if (feedAdded != null) {
                        // this overrides any passed in locations with just the feed added.
                        return
                            InternalGetPackages(null, minVersion, maxVersion, dependencies, installed, active, required, blocked, latest, feedAdded, forceScan,
                                messages).Result;
                    }

                    // if we get here, that means that we didn't recognize the file. 
                    // we're gonna return an empty collection at this point.
                    return singleResult.SingleItemAsEnumerable();
                }, TaskContinuationOptions.AttachedToParent);
            }
            // can only be a canonical name match, proceed with that.            
            return InternalGetPackages(PackageName.Parse(parameter), minVersion, maxVersion, dependencies, installed, active, required, blocked, latest,
                location, forceScan, messages);
        }

        private Task<IEnumerable<Package>> InternalGetPackages(PackageName packageName, ulong? minVersion = null, ulong? maxVersion = null,
            bool? dependencies = null, bool? installed = null, bool? active = null, bool? required = null, bool? blocked = null, bool? latest = null,
            string location = null, bool? forceScan = null, PackageManagerMessages messages = null) {
            var packages = new List<Package>();

            return FindPackages(packageName != null && packageName.IsFullMatch ? packageName.CanonicalName : null, packageName == null ? null : packageName.Name,
                packageName == null ? null : packageName.Version, packageName == null ? null : packageName.Arch,
                packageName == null ? null : packageName.PublicKeyToken, dependencies, installed, active, required, blocked, latest, null, null, location,
                forceScan, new PackageManagerMessages {
                    PackageInformation = package => {
                        if ((!minVersion.HasValue || package.Version.VersionStringToUInt64() >= minVersion) &&
                            (!maxVersion.HasValue || package.Version.VersionStringToUInt64() <= maxVersion)) {
                            packages.Add(package);
                        }
                    },
                }.Extend(messages)).ContinueWith(antecedent => { 
                        if( antecedent.IsFaulted || antecedent.IsCanceled ) {
                            throw antecedent.Exception.Flatten().InnerExceptions.FirstOrDefault();
                        }
                    return packages as IEnumerable<Package>;
                }, TaskContinuationOptions.AttachedToParent);
        }

        public Task FindPackages(string canonicalName = null, string name = null, string version = null, string arch = null, string publicKeyToken = null,
            bool? dependencies = null, bool? installed = null, bool? active = null, bool? required = null, bool? blocked = null, bool? latest = null,
            int? index = null, int? maxResults = null, string location = null, bool? forceScan = null, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("find-packages") {
                        {"canonical-name", canonicalName},
                        {"name", name},
                        {"version", version},
                        {"arch", arch},
                        {"public-key-token", publicKeyToken},
                        {"dependencies", dependencies},
                        {"installed", installed},
                        {"active", active},
                        {"required", required},
                        {"blocked", blocked},
                        {"latest", latest},
                        {"index", index},
                        {"max-results", maxResults},
                        {"location", location},
                        {"force-scan", forceScan},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task GetPackageDetails(string canonicalName, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("get-package-details") {
                        {"canonical-name", canonicalName},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task InstallPackage(string canonicalName, bool? autoUpgrade = null, bool? force = null, bool? download = null, bool? pretend = null,
            PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("install-package") {
                        {"canonical-name", canonicalName},
                        {"auto-upgrade", autoUpgrade},
                        {"force", force},
                        {"download", download},
                        {"pretend", pretend},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task ListFeeds(int? index = null, int? maxResults = null, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("find-feeds") {
                        {"index", index},
                        {"max-results", maxResults},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task RemoveFeed(string location, bool? session = null, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("remove-feed") {
                        {"location", location},
                        {"session", session},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task AddFeed(string location, bool? session = null, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("add-feed") {
                        {"location", location},
                        {"session", session},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task VerifyFileSignature(string filename, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("verify-file-signature") {
                        {"filename", filename},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task SetPackage(string canonicalName, bool? active = null, bool? required = null, bool? blocked = null, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("set-package") {
                        {"canonical-name", canonicalName},
                        {"active", active},
                        {"required", required},
                        {"blocked", blocked},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task RemovePackage(string canonicalName, bool? force = null, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("remove-package") {
                        {"canonical-name", canonicalName},
                        {"force", force},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task UnableToAcquire(string canonicalName, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("unable-to-acquire") {
                        {"canonical-name", canonicalName},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task DownloadProgress(string canonicalName, int progress, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("download-progress") {
                        {"canonical-name", canonicalName},
                        {"progress", progress.ToString()},

                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task RecognizeFile(string canonicalName, string localLocation, string remoteLocation, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("recognize-file") {
                        {"canonical-name", canonicalName},
                        {"local-location", localLocation},
                        {"remote-location", remoteLocation},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        public Task SuppressFeed(string location, PackageManagerMessages messages = null) {
            IsCompleted.Reset();
            return Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("suppress-feed") {
                        {"location", location},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }).AutoManage();
        }

        internal static bool Dispatch(UrlEncodedMessage responseMessage = null) {
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
                    PackageManagerMessages.Invoke.FeedDetails(responseMessage["location"], DateTime.FromFileTime((long?) responseMessage["last-scanned"] ?? 0),
                        (bool?) responseMessage["session"] ?? false, (bool?) responseMessage["suppressed"] ?? false,
                        (bool?) responseMessage["validated"] ?? false);
                    break;

                case "found-package":
                    var result = Package.GetPackage(responseMessage["canonical-name"]);

                    result.LocalPackagePath = responseMessage["local-location"];
                    result.Name = responseMessage["name"];
                    result.Version = responseMessage["version"];
                    result.Architecture = responseMessage["arch"];
                    result.PublicKeyToken = responseMessage["public-key-token"];
                    result.IsInstalled = (bool?) responseMessage["installed"] ?? false;
                    result.IsBlocked = (bool?) responseMessage["blocked"] ?? false;
                    result.IsRequired = (bool?) responseMessage["required"] ?? false;
                    result.IsClientRequired = (bool?) responseMessage["client-required"] ?? false;
                    result.IsActive = (bool?) responseMessage["active"] ?? false;
                    result.IsDependency = (bool?) responseMessage["dependent"] ?? false;
                    result.RemoteLocations = responseMessage.GetCollection("remote-locations");
                    result.Dependencies = responseMessage.GetCollection("dependencies");
                    result.SupercedentPackages = responseMessage.GetCollection("supercedent-packages");


                    PackageManagerMessages.Invoke.PackageInformation(result);
                    break;

                case "installed-package":
                    PackageManagerMessages.Invoke.InstalledPackage(responseMessage["canonical-name"]);
                    break;

                case "installing-package":
                    PackageManagerMessages.Invoke.InstallingPackageProgress(responseMessage["canonical-name"], (int?) responseMessage["percent-complete"] ?? 0,(int?) responseMessage["overall-percent-complete"] ?? 0);
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

                case "package-satisfied-by":
                    PackageManagerMessages.Invoke.PackageSatisfiedBy(Package.GetPackage(responseMessage["canonical-name"]), Package.GetPackage(responseMessage["satisfied-by"]));
                    break;

                case "package-details":
                    var details = Package.GetPackage(responseMessage["canonical-name"]);
                    details.Description = responseMessage["description"];

                    details.Summary = responseMessage["summary"];
                    details.DisplayName = responseMessage["display-name"];
                    details.Copyright = responseMessage["copyright"];
                    details.AuthorVersion = responseMessage["author-version"];
                    details.Icon = responseMessage["icon"];
                    details.License = responseMessage["license"];
                    details.LicenseUrl = responseMessage["license-url"];
                    details.PublishDate = responseMessage["publish-date"];
                    details.PublisherName = responseMessage["publisher-name"];
                    details.PublisherUrl = responseMessage["publisher-url"];
                    details.PublisherEmail = responseMessage["publisher-email"];
                    details.Tags = responseMessage.GetCollection("tags");

                    /*
                    if (!package.PackageDetails.Contributors.IsNullOrEmpty()) {
                        msg.AddCollection("contributor-name", package.PackageDetails.Contributors.Select(each => each.Name));
                        msg.AddCollection("contributor-url", package.PackageDetails.Contributors.Select(each => each.Url));
                        msg.AddCollection("contributor-email", package.PackageDetails.Contributors.Select(each => each.Email));
                    }
                     * */
                    PackageManagerMessages.Invoke.PackageDetails(details);
                    break;

                case "package-has-potential-upgrades":
                    // PackageManagerMessages.Invoke.PackageHasPotentialUpgrades(new Package(), Enumerable.Empty<Package>());
                    break;

                case "package-is-blocked":
                    PackageManagerMessages.Invoke.PackageBlocked(responseMessage["canonical-name"]);
                    break;

                case "removed-package":
                    PackageManagerMessages.Invoke.RemovedPackage(responseMessage["canonical-name"]);
                    break;

                case "removing-package":
                    PackageManagerMessages.Invoke.RemovingPackageProgress(responseMessage["canonical-name"], (int?) responseMessage["percent-complete"] ?? 0);
                    break;

                case "require-remote-file":
                    PackageManagerMessages.Invoke.RequireRemoteFile(responseMessage["canonical-name"], responseMessage.GetCollection("remote-locations"),
                        responseMessage["destination"], (bool?) responseMessage["force"] ?? false);
                    break;

                case "signature-validation":
                    PackageManagerMessages.Invoke.SignatureValidation(responseMessage["filename"], (bool?) responseMessage["is-valid"] ?? false,
                        responseMessage["certificate-subject-name"]);
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
                    _pipe.WriteLineAsync(message.ToString()).ContinueWith(antecedent => { Console.WriteLine("Async Write Fail!? (1)"); },
                        TaskContinuationOptions.OnlyOnFaulted);
                }
                catch /* (Exception e) */ {
                    Console.WriteLine("Async Write Fail!? (2)");
                }
            }
        }

        private void StartSession(string clientId, string sessionId) {
            WriteAsync(new UrlEncodedMessage("start-session") {
                {"client", clientId},
                {"id", sessionId},
                {"rqid", sessionId},
            });
        }
    }
}