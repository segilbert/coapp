﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using System.Reflection;
using CoApp.Toolkit.Engine.Model;

namespace CoApp.Toolkit.Engine.Client {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Net;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Logging;
    using Pipes;
    using Tasks;
    using Toolkit.Exceptions;
    using Win32;

    public class PackageManager {
        internal class ManualEventQueue : Queue<UrlEncodedMessage>, IDisposable {
            internal static readonly Dictionary<int, ManualEventQueue> EventQueues = new Dictionary<int, ManualEventQueue>();
            private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true);

            public ManualEventQueue() {
                var tid = Task.CurrentId.GetValueOrDefault();
                if (tid == 0) {
                    throw new CoAppException("Cannot create a ManualEventQueue outside of a task.");
                }
                lock (EventQueues) {
                    EventQueues.Add(tid, this);
                }
            }

            public new void Enqueue(UrlEncodedMessage message) {
                base.Enqueue(message);
                _resetEvent.Set();
            }

            public void Dispose() {
                lock (EventQueues) {
                    EventQueues.Remove(Task.CurrentId.GetValueOrDefault());
                    if (EventQueues.Count == 0) {
                        Instance.OnCompleted();
                    }
                }
            }

            public static ManualEventQueue GetQueue(int taskId) {
                lock (EventQueues) {
                    return EventQueues[taskId];
                }
            }

            internal void DispatchResponses() {
                var continueHandlingMessages = true;

                while (continueHandlingMessages && _resetEvent.WaitOne()) {
                    _resetEvent.Reset();
                    while (Count > 0) {
                        if (!Dispatch(Dequeue())) {
                            continueHandlingMessages = false;
                        }
                    }
                }
            }
        }

        public event Action Completed;

        private void OnCompleted() {
            if( Completed != null ) {
                Completed();
            }
        }

        public static PackageManager Instance = new PackageManager();
        private NamedPipeClientStream _pipe;
        internal const int BufferSize = 1024*1024*2;

        public int ActiveCalls {
            get { return ManualEventQueue.EventQueues.Keys.Count; }
        }

        public bool IsServiceAvailable {
            get { return EngineServiceManager.Available; }
        }

        public bool IsConnected {
            get { lock(this) {return IsServiceAvailable && _pipe != null && _pipe.IsConnected;} }
        }

        private PackageManager() {
        }

        /// <summary>
        /// DEPRECATED Making this deprecated. Client library should be smart enough to connect without being told to.
        /// 
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="sessionId"></param>
        /// <param name="millisecondsTimeout"></param>
        public void ConnectAndWait(string clientName, string sessionId = null,int millisecondsTimeout = 5000 ) {
            Connect(clientName, sessionId).Wait(millisecondsTimeout);
        }

        public Task Connect() {
            return Connect(Process.GetCurrentProcess().Id.ToString());
        }

        private Task ConnectingTask;
        private int autoConnectionCount;

        public Task Connect(string clientName, string sessionId = null) {
            lock (this) {
                if (IsConnected) {
                    return "Completed".AsResultTask();
                }

                if (ConnectingTask == null) {
                    ConnectingTask = Task.Factory.StartNew(() => {
                        // ensure any old connection is removed.
                        //Disconnect();

                        EngineServiceManager.EnsureServiceIsResponding();

                        sessionId = sessionId ?? Process.GetCurrentProcess().Id.ToString() + "/" + autoConnectionCount++;

                        for (int count = 0; count < 60; count++) {
                            _pipe = new NamedPipeClientStream(".", "CoAppInstaller", PipeDirection.InOut,
                                                              PipeOptions.Asynchronous,
                                                              TokenImpersonationLevel.Impersonation);
                            try {
                                _pipe.Connect(500);
                                _pipe.ReadMode = PipeTransmissionMode.Message;
                                break;
                            }
                            catch {
                                _pipe = null;
                            }
                        }

                        if (_pipe == null) {
                            throw new CoAppException("Unable to connect to CoApp Service");
                        }

                        StartSession(clientName, sessionId);

                        Task.Factory.StartNew(ProcessMessages,TaskCreationOptions.None).AutoManage();
                    });

                }
            }
            return ConnectingTask;
        }

        private void ProcessMessages() {
            try {
                while (IsConnected) {
                    ConnectingTask = null;

                    var incomingMessage = new byte[BufferSize];

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

                        Logger.Message("Response:{0}".format(responseMessage.ToSmallerString()));

                        try {
                            ManualEventQueue.GetQueue(rqid.GetValueOrDefault()).Enqueue(responseMessage);
                        }
                        catch {
                            //  Console.WriteLine("Unable to queue the response to the right request event queue!");
                            // Console.WriteLine("    Response:{0}", responseMessage.Command);
                            // not able to queue up the response to the right task?
                        }
                    }).AutoManage();
                    readTask.Wait();
                }
            }
            catch (Exception e) {
                Logger.Message("Connection Terminating with Exception {0}/{1}", e.GetType(), e.Message);
            }
            finally {
                Disconnect();
            }
        }

        public void Disconnect() {
            lock (this) {
                try {
                    if (ManualEventQueue.EventQueues.Any()) {
                        Logger.Error("Manually clearing out event queues in client library. This is a symptom of something unsavory.");
                        ManualEventQueue.EventQueues.Clear();
                    }

                    if (_pipe != null) {
                        var pipe = _pipe;
                        _pipe = null;
                        pipe.Close();
                        pipe.Dispose();
                    }
                } catch {
                    // just close it!
                }
            }
        }

        public Task<IEnumerable<Package>> GetPackages(IEnumerable<string> parameters, ulong? minVersion = null, ulong? maxVersion = null,
            bool? dependencies = null, bool? installed = null, bool? active = null, bool? required = null, bool? blocked = null, bool? latest = null,
            string location = null, bool? forceScan = null, PackageManagerMessages messages = null) {
            Connect().Wait();

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
            return Task<IEnumerable<Package>>.Factory.ContinueWhenAll((Task[])tasks, antecedents => {

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
            Connect().Wait();
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
                        return InternalGetPackages(null, minVersion, maxVersion, dependencies, installed, active, required, blocked, latest, feedAdded, forceScan,
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

        private Task<IEnumerable<Package>> InternalGetPackages(PackageName packageName, FourPartVersion? minVersion = null, FourPartVersion? maxVersion = null,
            bool? dependencies = null, bool? installed = null, bool? active = null, bool? required = null, bool? blocked = null, bool? latest = null,
            string location = null, bool? forceScan = null, PackageManagerMessages messages = null) {
            Connect().Wait();

            var packages = new List<Package>();

            return FindPackages(packageName != null && packageName.IsFullMatch ? packageName.CanonicalName : null, packageName == null ? null : packageName.Name,
                packageName == null ? null : packageName.Version, packageName == null ? null : packageName.Arch,
                packageName == null ? null : packageName.PublicKeyToken, dependencies, installed, active, required, blocked, latest, null, null, location,
                forceScan, new PackageManagerMessages {
                    PackageInformation = package => {
                        if ((!minVersion.HasValue || package.Version >= minVersion) &&
                            (!maxVersion.HasValue || package.Version <= maxVersion)) {
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

            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task GetPackageDetails(string canonicalName, PackageManagerMessages messages = null) {
            
            return  Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task InstallPackage(string canonicalName, bool? autoUpgrade = null, bool? force = null, bool? download = null, bool? pretend = null,
            PackageManagerMessages messages = null) {

            return Connect().ContinueWith((antecedent) => {
                var msgs = new PackageManagerMessages {
                    InstalledPackage = (pkgCanonicalName) => {
                        if( !PackageManagerSettings.CoAppSettings["#Telemetry"].StringValue.IsFalse() ) {
                            // ping the coapp server to tell it that a package installed
                            try {
                                var uniqId = PackageManagerSettings.CoAppSettings["#AnonymousId"].StringValue; 
                                if( string.IsNullOrEmpty(uniqId) || uniqId.Length != 32 ) {
                                    uniqId = Guid.NewGuid().ToString("N");
                                    PackageManagerSettings.CoAppSettings["#AnonymousId"].StringValue = uniqId;
                                }
                                
                                Logger.Message("Pinging `http://coapp.org/telemetry/?anonid={0}&pkg={1}` ".format(uniqId, pkgCanonicalName));
                                var req =
                                    HttpWebRequest.Create("http://coapp.org/telemetry/?anonid={0}&pkg={1}".format(uniqId, pkgCanonicalName));
                                req.BetterGetResponse().Close();
                            } catch {
                                // who cares...
                            }
                        }

                        if (messages != null && messages.InstalledPackage != null) {
                            messages.InstalledPackage(pkgCanonicalName);
                        }
                    }
                }.Extend(messages);

                msgs.Register();
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task ListFeeds(int? index = null, int? maxResults = null, PackageManagerMessages messages = null) {
            return  Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task RemoveFeed(string location, bool? session = null, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task AddFeed(string location, bool? session = null, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task VerifyFileSignature(string filename, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task SetPackage(string canonicalName, bool? active = null, bool? required = null, bool? blocked = null, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task RemovePackage(string canonicalName, bool? force = null, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task UnableToAcquire(string canonicalName, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task DownloadProgress(string canonicalName, int progress, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
                if (messages != null) {
                    messages.Register();
                }
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("download-progress") {
                        {"canonical-name", canonicalName},
                        {"progress", progress.ToString()},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task RecognizeFile(string canonicalName, string localLocation, string remoteLocation, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task SuppressFeed(string location, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
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
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task SetLogging( bool? Messages, bool? Warnings, bool? Errors ) {
            return Connect().ContinueWith((antecedent) => {
                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("set-logging") {
                        {"messages", Messages},
                        {"warnings", Warnings},
                        {"errors", Errors},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task GetPolicy(string policyName, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
                if (messages != null) {
                    messages.Register();
                }

                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("get-policy") {
                        {"name", policyName},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task AddToPolicy(string policyName, string account ,PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
                if (messages != null) {
                    messages.Register();
                }

                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("add-to-policy") {
                        {"name", policyName},
                        {"account", account},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task RemoveFromPolicy(string policyName, string account, PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
                if (messages != null) {
                    messages.Register();
                }

                using (var eventQueue = new ManualEventQueue()) {
                    WriteAsync(new UrlEncodedMessage("remove-from-policy") {
                        {"name", policyName},
                        {"account", account},
                        {"rqid", Task.CurrentId},
                    });

                    // will return when the final message comes thru.
                    eventQueue.DispatchResponses();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
        }

        public Task CreateSymlink(string existingLocation, string newLink, LinkType linkType,  PackageManagerMessages messages = null) {
            return Connect().ContinueWith((antecedent) => {
                  if (messages != null) {
                      messages.Register();
                  }
                  using (var eventQueue = new ManualEventQueue()) {
                      WriteAsync(new UrlEncodedMessage("symlink") {
                        {"existing-location", existingLocation},
                        {"new-link", newLink},
                        {"link-type", linkType.ToString()},
                        {"rqid", Task.CurrentId},
                    });

                      // will return when the final message comes thru.
                      eventQueue.DispatchResponses();
                  }
            }, TaskContinuationOptions.OnlyOnRanToCompletion).AutoManage();
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
                    result.Version = (FourPartVersion)(string)responseMessage["version"];
                    result.Architecture = ((string)responseMessage["arch"]);
                    result.PublicKeyToken = responseMessage["public-key-token"];
                    result.ProductCode = responseMessage["product-code"];
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
                    EnvironmentUtility.BroadcastChange();
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
                    return false;

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
                    details.PackageItemText = responseMessage["package-item-text"];
                    details.Roles =
                        responseMessage.GetKeyValuePairs("role").Select(
                            each => new Role { Name = each.Key, PackageRole = (PackageRole)Enum.Parse(typeof(PackageRole), each.Value, true) });
                    

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

                case "policy":
                    PackageManagerMessages.Invoke.PolicyInformation(responseMessage["name"], responseMessage["description"], responseMessage.GetCollection("accounts"));
                    break;

                case "restarting":
                    PackageManagerMessages.Invoke.Restarting();
                    // disconnect from the engine, and let the client reconnect on the next call.
                    Instance.Disconnect();
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