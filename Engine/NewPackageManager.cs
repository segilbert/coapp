//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Crypto;
    using Exceptions;
    using Extensions;
    using Feeds;
    using PackageFormatHandlers;
    using Tasks;

    public class NewPackageManager {

        public static NewPackageManager Instance = new NewPackageManager();
        private static Regex _canonicalNameParser = new Regex(@"^(.*)-(\d{1,5}\.\d{1,5}\.\d{1,5}\.\d{1,5})-(any|x86|x64|arm)-([0-9a-f]{16})$",RegexOptions.IgnoreCase);

        private List<ManualResetEvent> manualResetEvents = new List<ManualResetEvent>();

        /// <summary>
        /// the collection of all known packages
        /// </summary>
        private static readonly ObservableCollection<Package> _packages = new ObservableCollection<Package>();

        private bool CancellationRequested {
            get { return PackageManagerSession.Invoke.CancellationRequested(); }
        }

        private NewPackageManager() {
            // always load the Installed Package Feed.
            PackageFeed.GetPackageFeedFromLocation(InstalledPackageFeed.CanonicalLocation);
        }

        /// <summary>
        /// feeds that we should try to load as system feeds
        /// </summary>
        private IEnumerable<string> SystemFeedLocations {
            get {
                if (PackageManagerSettings.CoAppSettings["#feedLocations"].HasValue) {
                    return PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue;
                }
                // defaults to the installed packages feed 
                // and the default coapp feed.
                return "http://coapp.org/feed".SingleItemAsEnumerable();
            }
        }

        private IEnumerable<string> SessionFeedLocations {
            get { 
                var result = SessionCache<IEnumerable<string>>.Value["session-feeds"];
                return result.IsNullOrEmpty() ? Enumerable.Empty<string>() : result;
            }
        }

        private void AddSessionFeed( string feedLocation ) {
            lock (this) {
                var sessionFeeds = SessionFeedLocations.Union(feedLocation.SingleItemAsEnumerable()).Distinct();
                SessionCache<IEnumerable<string>>.Value["session-feeds"] = sessionFeeds.ToArray();
            }
        }

        private void AddSystemFeed(string feedLocation) {
            lock (this) {
                var systemFeeds = SystemFeedLocations.Union(feedLocation.SingleItemAsEnumerable()).Distinct();
                PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue = systemFeeds.ToArray();
            }
        }

        private void RemoveSessionFeed(string feedLocation) {
            lock (this) {
                var sessionFeeds = from feed in SessionFeedLocations where !feed.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase) select feed;
                SessionCache<IEnumerable<string>>.Value["session-feeds"] = sessionFeeds.ToArray();
                
                // remove it from the cached feeds
                SessionCache<PackageFeed>.Value.Clear(feedLocation);
            }
        }

        private void RemoveSystemFeed(string feedLocation) {
            lock (this) {
                var systemFeeds = from feed in SystemFeedLocations where !feed.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase) select feed;
                PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue = systemFeeds.ToArray();

                // remove it from the cached feeds
                Cache<PackageFeed>.Value.Clear(feedLocation);
            }
        }
        
        internal IEnumerable<Task> LoadSystemFeeds() {
            // load system feeds

            var systemCacheLoaded = SessionCache<string>.Value["system-cache-loaded"];
            if (systemCacheLoaded.IsTrue()) {
                yield break;
            }

            SessionCache<string>.Value["system-cache-loaded"] = "true";
            
            foreach (var f in SystemFeedLocations) {
                var feedLocation = f;
                yield return PackageFeed.GetPackageFeedFromLocation(feedLocation).ContinueWith(antecedent => {
                    if (antecedent.Result != null) {
                        Cache<PackageFeed>.Value[feedLocation] = antecedent.Result;
                    }
                    else {
                        LogMessage("Feed {0} was unable to load.", feedLocation);
                    }
                }, TaskContinuationOptions.AttachedToParent);
            }
        }

        private void LogMessage(string message, params object[] objs) {
            string msg = message.format(objs);
            // do something with the message?
        }

        public Task FindPackages( string canonicalName, string name, string version, string arch, string publicKeyToken,
            bool? dependencies, bool? installed, bool? active, bool? required, bool? blocked, bool? latest, 
            int? index, int? maxResults, string location, bool? forceScan, PackageManagerMessages messages ) {

            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EnumeratePackages)) {
                    PackageManagerMessages.Invoke.PermissionRequired("EnumeratePackages");
                    return;
                }

                // get basic list of packages based on primary characteristics
                if (!string.IsNullOrEmpty(canonicalName)) {
                    // if canonical name is passed, override name,version,pkt,arch with the parsed canonicalname.
                    var match = _canonicalNameParser.Match(canonicalName.ToLower());
                    if (!match.Success) {
                        PackageManagerMessages.Invoke.Error("find-packages", "canonical-name",
                            "Canonical name '{0}' does not appear to be a valid canonical name".format(canonicalName));
                        return;
                    }

                    name = match.Groups[1].Captures[0].Value;
                    version = match.Groups[2].Captures[0].Value;
                    arch = match.Groups[3].Captures[0].Value;
                    publicKeyToken = match.Groups[4].Captures[0].Value;
                }

                var results = SearchForPackages(name, version, arch, publicKeyToken, location);
                // filter results of list based on secondary filters

                results = from package in results
                    where
                        (installed == null || package.IsInstalled == installed) && (active == null || package.IsActive == active) &&
                            (required == null || package.Required == required) && (blocked == null || package.IsBlocked == blocked)

                    select package;

                // only the latest?
                if (latest.HasValue && latest == true) {
                    results = results.HighestPackages();
                }

                // paginate the results
                if (index.HasValue) {
                    results = results.Skip(index.Value);
                }

                if (maxResults.HasValue) {
                    results = results.Take(maxResults.Value);
                }

                if (results.Any()) {
                    UpdateIsRequestedFlags();

                    foreach (var package in results) {
                        if (CancellationRequested) {
                            PackageManagerMessages.Invoke.OperationCancelled("find-packages");
                            return;
                        }

                        // otherwise, we're installing a dependency, and we need something compatable.
                        var supercedents = (from p in SearchForPackages(package.Name, null, package.Architecture, package.PublicKeyToken)
                            where p.InternalPackageData.PolicyMinimumVersion <= package.Version && p.InternalPackageData.PolicyMaximumVersion >= package.Version
                            select p).OrderByDescending(p => p.Version).ToArray();

                        PackageManagerMessages.Invoke.PackageInformation(package, supercedents);
                    }
                }
                else {
                    PackageManagerMessages.Invoke.NoPackagesFound();
                }

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task GetPackageDetails(string canonicalName, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                var package = GetSinglePackage(canonicalName, "get-package-details");
                if (package == null) {
                    return;
                }

                PackageManagerMessages.Invoke.PackageDetails(package);
            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task InstallPackage(string canonicalName, bool? autoUpgrade, bool? force, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                using (var manualResetEvent = new ManualResetEvent(true)) {
                    try {
                        lock (manualResetEvents) {
                            manualResetEvents.Add(manualResetEvent);
                        }

                        var package = GetSinglePackage(canonicalName, "install-package");

                        if (package == null) {
                            PackageManagerMessages.Invoke.UnknownPackage(canonicalName);
                            return;
                        }

                        // is the user authorized to install this?
                        var highestInstalledPackage =
                            SearchForInstalledPackages(package.Name, null, package.Architecture, package.PublicKeyToken).HighestPackages();
                        if (highestInstalledPackage.Any() && highestInstalledPackage.FirstOrDefault().Version < package.Version) {
                            if (!package.IsBlocked) {
                                PackageManagerMessages.Invoke.PackageBlocked(canonicalName);
                                return;
                            }

                            if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.UpdatePackage)) {
                                PackageManagerMessages.Invoke.PermissionRequired("UpdatePackage");
                                return;
                            }
                        }
                        else {
                            if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.InstallPackage)) {
                                PackageManagerMessages.Invoke.PermissionRequired("InstallPackage");
                                return;
                            }
                        }

                        // mark the package as the client requested.
                        package.PackageSessionData.DoNotSupercede = (false == autoUpgrade);
                        package.PackageSessionData.UpgradeAsNeeded = (true == autoUpgrade);
                        package.PackageSessionData.UserSpecified = true;

                        // the resolve-acquire-install-loop
                        do {
                            // if the world changes, this will get set somewhere between here and the 
                            // other end of the do-loop.
                            manualResetEvent.Reset();

                            if (CancellationRequested) {
                                PackageManagerMessages.Invoke.OperationCancelled("install-package");
                                return;
                            }

                            IEnumerable<Package> installGraph = null;
                            try {
                                installGraph = GenerateInstallGraph(package).ToArray();
                            }
                            catch (OperationCompletedBeforeResultException) {
                                // we encountered an unresolvable condition in the install graph.
                                // messages should have already been sent.
                                PackageManagerMessages.Invoke.FailedPackageInstall(canonicalName, package.InternalPackageData.LocalPackagePath,
                                    "One or more dependencies are unable to be resolved.");
                                return;
                            }

                            // seems like a good time to check if we're supposed to bail...
                            if (CancellationRequested) {
                                PackageManagerMessages.Invoke.OperationCancelled("install-package");
                                return;
                            }

                            // we've got an install graph.
                            // let's see if we've got all the files
                            var missingFiles = from p in installGraph where !p.InternalPackageData.HasLocalFile select p;
                            if (missingFiles.Any()) {
                                // we've got some that don't have files.
                                foreach (var p in missingFiles.Where(p => !p.PackageSessionData.RequestedDownload)) {
                                    PackageManagerMessages.Invoke.RequireRemoteFile(p.CanonicalName,
                                        p.InternalPackageData.RemoteLocation.Select(each => each.AbsoluteUri), PackageManagerSettings.CoAppCacheDirectory, false);

                                    p.PackageSessionData.RequestedDownload = true;
                                }
                            }
                            else {
                                var failed = false;
                                // no missing files? Check
                                // complete install graph? Check
                                foreach (var p in installGraph) {
                                    var pkg = p;
                                    // seems like a good time to check if we're supposed to bail...
                                    if (CancellationRequested) {
                                        PackageManagerMessages.Invoke.OperationCancelled("install-package");
                                        return;
                                    }
                                    try {
                                        if (!pkg.IsInstalled) {
                                            var isValid = false;
                                            foreach (
                                                var location in
                                                    pkg.InternalPackageData.LocalPackagePath.Value.SingleItemAsEnumerable().Union(
                                                        pkg.InternalPackageData.LocalPackagePath.OtherValues)) {
                                                isValid = Verifier.HasValidSignature(location);
                                                PackageManagerMessages.Invoke.SignatureValidation(location, isValid,
                                                    Verifier.GetPublisherInformation(location)["PublisherName"]);
                                                if (isValid) {
                                                    pkg.InternalPackageData.LocalPackagePath.Value = location;
                                                    break;
                                                }
                                            }

                                            if (!isValid) {
                                                PackageManagerMessages.Invoke.FailedPackageInstall(pkg.CanonicalName, pkg.InternalPackageData.LocalPackagePath,
                                                    "Package failed to install due to not being signed with a valid certificate.");
                                                pkg.PackageSessionData.PackageFailedInstall = true;
                                            }

                                            // GS01: We should put a softer lock here to keep the client aware that packages 
                                            // are being installed on other threads...
                                            lock (typeof (MSIBase)) {
                                                pkg.Install(percentage => PackageManagerMessages.Invoke.InstallingPackageProgress(pkg.CanonicalName, percentage));
                                            }
                                            PackageManagerMessages.Invoke.InstallingPackageProgress(pkg.CanonicalName, 100);
                                            PackageManagerMessages.Invoke.InstalledPackage(pkg.CanonicalName);
                                        }
                                    }
                                    catch (PackageInstallFailedException pife) {
                                        PackageManagerMessages.Invoke.FailedPackageInstall(pkg.CanonicalName, pkg.InternalPackageData.LocalPackagePath,
                                            "Package failed to install.");
                                        pkg.PackageSessionData.PackageFailedInstall = true;

                                        if (!pkg.PackageSessionData.AllowedToSupercede) {
                                            throw new OperationCompletedBeforeResultException(); // user specified packge as critical.
                                        }
                                        failed = true;
                                        break;
                                    }
                                }
                                if (!failed) {
                                    // W00T ... We did it!
                                    return;
                                }

                                // otherwise, let's run it thru again. maybe it'll come together.
                            }

                            //----------------------------------------------------------------------------
                            // wait until either the manualResetEvent is set, but check every second or so
                            // to see if the client has cancelled the operation.
                            while (!manualResetEvent.WaitOne(1000)) {
                                if (CancellationRequested) {
                                    PackageManagerMessages.Invoke.OperationCancelled("install-package");
                                    return;
                                }
                            }
                        } while (true);

                    }
                    catch (OperationCompletedBeforeResultException) {
                        // can't continue with options given.
                        return;
                    }
                    finally {
                        // remove manualResetEvent from the mre list
                        lock (manualResetEvents) {
                            manualResetEvents.Remove(manualResetEvent);
                        }
                    }
                }

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task ListFeeds(int? index, int? maxResults, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {

                if (messages != null) {
                    messages.Register();
                }

                var canFilterSession = PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSessionFeeds);
                var canFilterSystem = PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSystemFeeds);

                var activeSessionFeeds = SessionCache<PackageFeed>.Value.SessionValues;
                var activeSystemFeeds = Cache<PackageFeed>.Value.Values;

                var x = from feedLocation in SystemFeedLocations
                    let theFeed = activeSystemFeeds.Where(each => each.IsLocationMatch(feedLocation)).FirstOrDefault()
                    let validated = theFeed != null
                    select new {
                        feed = feedLocation,
                        LastScanned = validated ? theFeed.LastScanned : DateTime.FromFileTime(0),
                        session = false,
                        suppressed = canFilterSystem && BlockedScanLocations.Contains(feedLocation),
                        validated,
                    };

                var y = from feedLocation in SessionFeedLocations
                    let theFeed = activeSessionFeeds.Where(each => each.IsLocationMatch(feedLocation)).FirstOrDefault()
                    let validated = theFeed != null
                    select new {
                        feed = feedLocation,
                        LastScanned = validated ? theFeed.LastScanned : DateTime.FromFileTime(0),
                        session = true,
                        suppressed = canFilterSession && BlockedScanLocations.Contains(feedLocation),
                        validated,
                    };

                var results = x.Union(y);

                // paginate the results
                if (index.HasValue) {
                    results = results.Skip(index.Value);
                }

                if (maxResults.HasValue) {
                    results = results.Take(maxResults.Value);
                }



                if (results.Any()) {
                    foreach (var f in results) {
                        PackageManagerMessages.Invoke.FeedDetails(f.feed, f.LastScanned, f.session, f.suppressed, f.validated);
                    }
                }
                else {
                    PackageManagerMessages.Invoke.NoFeedsFound();
                }


            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task RemoveFeed(string location, bool? session, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                // Note: This may need better lookup/matching for the location
                // as location can be a fuzzy match.

                if (session ?? false) {
                    // session feed specfied
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSessionFeeds)) {
                        PackageManagerMessages.Invoke.PermissionRequired("EditSessionFeeds");
                        return;
                    }

                    RemoveSessionFeed(location);
                    PackageManagerMessages.Invoke.FeedRemoved(location);
                }
                else {
                    // system feed specified
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSystemFeeds)) {
                        PackageManagerMessages.Invoke.PermissionRequired("EditSystemFeeds");
                        return;
                    }

                    RemoveSystemFeed(location);
                    PackageManagerMessages.Invoke.FeedRemoved(location);
                }
            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task AddFeed(string location, bool? session, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                if (session ?? false) {
                    // new feed is a session feed
                    // session feed specfied
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSessionFeeds)) {
                        PackageManagerMessages.Invoke.PermissionRequired("EditSessionFeeds");
                        return;
                    }

                    // check if it is already a system feed
                    if (SystemFeedLocations.Contains(location)) {
                        PackageManagerMessages.Invoke.Warning("add-feed", "location", "location '{0}' is already a system feed".format(location));
                        return;
                    }

                    if (SessionFeedLocations.Contains(location)) {
                        PackageManagerMessages.Invoke.Warning("add-feed", "location", "location '{0}' is already a session feed".format(location));
                        return;
                    }

                    AddSessionFeed(location);
                    PackageManagerMessages.Invoke.FeedAdded(location);

                    // add feed to the session feeds.
                    PackageFeed.GetPackageFeedFromLocation(location).ContinueWith(antecedent => {
                        if (antecedent.Result != null) {
                            SessionCache<PackageFeed>.Value[location] = antecedent.Result;
                        }
                        else {
                            PackageManagerMessages.Invoke.Error("add-feed", "location",
                                "failed to recognize location '{0}' as a valid package feed".format(location));
                            LogMessage("Feed {0} was unable to load.", location);
                        }
                    }, TaskContinuationOptions.AttachedToParent);

                }
                else {
                    // new feed is a system feed
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSystemFeeds)) {
                        PackageManagerMessages.Invoke.PermissionRequired("EditSystemFeeds");
                        return;
                    }

                    if (SystemFeedLocations.Contains(location)) {
                        PackageManagerMessages.Invoke.Warning("add-feed", "location", "location '{0}' is already a system feed".format(location));
                        return;
                    }

                    AddSystemFeed(location);
                    PackageManagerMessages.Invoke.FeedAdded(location);

                    // add feed to the system feeds.
                    PackageFeed.GetPackageFeedFromLocation(location).ContinueWith(antecedent => {
                        if (antecedent.Result != null) {
                            Cache<PackageFeed>.Value[location] = antecedent.Result;
                        }
                        else {
                            PackageManagerMessages.Invoke.Error("add-feed", "location",
                                "failed to recognize location '{0}' as a valid package feed".format(location));
                            LogMessage("Feed {0} was unable to load.", location);
                        }
                    }, TaskContinuationOptions.AttachedToParent);
                }

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task VerifyFileSignature(string filename, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                if (string.IsNullOrEmpty(filename)) {
                    PackageManagerMessages.Invoke.Error("verify-signature", "filename", "parameter 'filename' is required to verify a file");
                    return;
                }

                var location = PackageManagerSession.Invoke.GetCanonicalizedPath(filename);

                if (!File.Exists(location)) {
                    PackageManagerMessages.Invoke.FileNotFound(location);
                    return;
                }

                var r = Verifier.HasValidSignature(location);
                if (r) {
                    PackageManagerMessages.Invoke.SignatureValidation(location, r, Verifier.GetPublisherInformation(location)["PublisherName"]);
                }
                else {
                    PackageManagerMessages.Invoke.SignatureValidation(location, false, null);
                }

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task SetPackage(string canonicalName, bool? active, bool? required, bool? blocked, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                var package = GetSinglePackage(canonicalName, "set-package");

                if (package == null) {
                    PackageManagerMessages.Invoke.UnknownPackage(canonicalName);
                    return;
                }

                if (!package.IsInstalled) {
                    PackageManagerMessages.Invoke.Error("set-package", "canonical-name", "package '{0}' is not installed.".format(canonicalName));
                    return;
                }

                // seems like a good time to check if we're supposed to bail...
                if (CancellationRequested) {
                    PackageManagerMessages.Invoke.OperationCancelled("remove-package");
                    return;
                }

                if (true == active) {
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.ChangeActivePackage)) {
                        PackageManagerMessages.Invoke.PermissionRequired("ChangeActivePackage");
                    }
                    else {
                        package.SetPackageCurrent();
                    }
                }

                if (false == active) {
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.ChangeActivePackage)) {
                        PackageManagerMessages.Invoke.PermissionRequired("ChangeActivePackage");
                    }
                    else {
                        SearchForInstalledPackages(package.Name, null, package.Architecture, package.PublicKeyToken).HighestPackages().FirstOrDefault().
                            SetPackageCurrent();
                    }
                }

                if (true == required) {
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.ChangeRequiredState)) {
                        PackageManagerMessages.Invoke.PermissionRequired("ChangeRequiredState");
                    }
                    else {
                        package.Required = true;
                    }
                }

                if (false == required) {
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.ChangeRequiredState)) {
                        PackageManagerMessages.Invoke.PermissionRequired("ChangeRequiredState");
                    }
                    else {
                        package.Required = false;
                    }
                }

                if (true == blocked) {
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.ChangeBlockedState)) {
                        PackageManagerMessages.Invoke.PermissionRequired("ChangeBlockedState");
                    }
                    else {
                        package.IsBlocked = true;
                    }
                }

                if (false == blocked) {
                    if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.ChangeBlockedState)) {
                        PackageManagerMessages.Invoke.PermissionRequired("ChangeBlockedState");
                    }
                    else {
                        package.IsBlocked = true;
                    }
                }

                PackageManagerMessages.Invoke.PackageInformation(package, Enumerable.Empty<Package>());

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task RemovePackage(string canonicalName, bool? force ,PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                if (!PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.RemovePackage)) {
                    PackageManagerMessages.Invoke.PermissionRequired("RemovePackage");
                    return;
                }

                var package = GetSinglePackage(canonicalName, "remove-package");
                if (package == null) {
                    PackageManagerMessages.Invoke.UnknownPackage(canonicalName);
                    return;
                }

                if (!package.IsInstalled) {
                    PackageManagerMessages.Invoke.Error("remove-package", "canonical-name", "package '{0}' is not installed.".format(canonicalName));
                    return;
                }

                if (!package.IsBlocked) {
                    PackageManagerMessages.Invoke.PackageBlocked(canonicalName);
                    return;
                }
                if (true != force) {
                    UpdateIsRequestedFlags();
                    if (package.PackageSessionData.IsDependency) {
                        PackageManagerMessages.Invoke.FailedPackageRemoval(canonicalName,
                            "Package '{0}' is a required dependency of another package.".format(canonicalName));
                        return;
                    }

                }
                // seems like a good time to check if we're supposed to bail...
                if (CancellationRequested) {
                    PackageManagerMessages.Invoke.OperationCancelled("remove-package");
                    return;
                }

                try {
                    package.Remove((percentage) => PackageManagerMessages.Invoke.RemovingPackageProgress(package.CanonicalName, percentage));

                    PackageManagerMessages.Invoke.RemovingPackageProgress(canonicalName, 100);
                    PackageManagerMessages.Invoke.RemovedPackage(canonicalName);
                }
                catch (OperationCompletedBeforeResultException e) {
                    PackageManagerMessages.Invoke.FailedPackageRemoval(canonicalName, e.Message);
                    return;
                }

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task UnableToAcquire(string canonicalName, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                if (canonicalName.IsNullOrEmpty()) {
                    PackageManagerMessages.Invoke.Error("unable-to-acquire", "canonical-name", "canonical-name is required.");
                    return;
                }

                // if there is a continuation task for the canonical name that goes along with this, 
                // we should continue with that task, and get the heck out of here.
                // 

                var package = GetSinglePackage(canonicalName, null);
                if (package != null) {
                    package.PackageSessionData.CouldNotDownload = true;
                }

                var continuationTask = SessionCache<Task<Recognizer.RecognitionInfo>>.Value[canonicalName];
                SessionCache<Task<Recognizer.RecognitionInfo>>.Value.Clear(canonicalName);
                Updated(); // notify threads that we're not going to be able to get that file.

                if (continuationTask != null) {
                    var state = continuationTask.AsyncState as Recognizer.RecognizerState;
                    if (state != null) {
                        state.LocalLocation = null;
                    }

                    // the task can run, 
                    continuationTask.Start();
                    return;
                }
            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task RecognizeFile(string canonicalName, string localLocation, string remoteLocation, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }
                if (string.IsNullOrEmpty(localLocation)) {
                    PackageManagerMessages.Invoke.Error("recognize-file", "local-location", "parameter 'local-location' is required to recognize a file");
                    return;
                }

                var location = PackageManagerSession.Invoke.GetCanonicalizedPath(localLocation);
                if (location.StartsWith(@"\\")) {
                    // a local unc path was passed. This isn't allowed--we need a file on a local volume that
                    // the user has access to.
                    PackageManagerMessages.Invoke.Error("recognize-file", "local-location",
                        "local-location '{0}' appears to be a file on a remote server('{1}') . Recognized files must be local".format(localLocation, location));
                    return;
                }

                if (!File.Exists(location)) {
                    PackageManagerMessages.Invoke.FileNotFound(location);
                    return;
                }

                // if there is a continuation task for the canonical name that goes along with this, 
                // we should continue with that task, and get the heck out of here.
                // 
                if (!canonicalName.IsNullOrEmpty()) {
                    var continuationTask = SessionCache<Task<Recognizer.RecognitionInfo>>.Value[canonicalName];
                    SessionCache<Task<Recognizer.RecognitionInfo>>.Value.Clear(canonicalName);
                    if (continuationTask != null) {
                        var state = continuationTask.AsyncState as Recognizer.RecognizerState;
                        if (state != null) {
                            state.LocalLocation = localLocation;
                        }
                        continuationTask.Start();
                        return;
                    }
                }

                // otherwise, we'll call the recognizer 
                Recognizer.Recognize(location).ContinueWith(antecedent => {
                    if (antecedent.IsFaulted) {
                        PackageManagerMessages.Invoke.FileNotRecognized(location, "Unexpected error recognizing file.");
                        return;
                    }

                    if (antecedent.Result.IsPackageFile) {
                        var package = GetPackageFromFilename(location);
                        SessionPackageFeed.Instance.Add(package);
                        PackageManagerMessages.Invoke.PackageInformation(package, Enumerable.Empty<Package>());
                        PackageManagerMessages.Invoke.Recognized(localLocation);
                        return;
                    }

                    if (antecedent.Result.IsPackageFeed) {
                        PackageManagerMessages.Invoke.FeedAdded(location);
                        PackageManagerMessages.Invoke.Recognized(location);
                    }

                    // if this isn't a package file, then there is something odd going on here.
                    // we don't accept non-package files willy-nilly. 
                    PackageManagerMessages.Invoke.FileNotRecognized(location, "File isn't a package, and doesn't appear to have been requested. ");
                    return;
                }, TaskContinuationOptions.AttachedToParent);

            }, TaskCreationOptions.AttachedToParent);
            return t;
        }

        public Task SuppressFeed(string location, PackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                if (messages != null) {
                    messages.Register();
                }

                var suppressedFeeds = SessionCache<List<string>>.Value["suppressed-feeds"] ?? new List<string>();

                lock (suppressedFeeds) {
                    if (!suppressedFeeds.Contains(location)) {
                        suppressedFeeds.Add(location);
                        SessionCache<List<string>>.Value["suppressed-feeds"] = suppressedFeeds;
                    }
                }

                PackageManagerMessages.Invoke.FeedSuppressed(location);
            }, TaskCreationOptions.AttachedToParent);
            return t;
        }


        internal void Updated() {
            foreach (var mre in manualResetEvents) {
                mre.Set();
            }
        }

        private Package GetSinglePackage(string canonicalName, string messageName) {
            // name != null?
            if( string.IsNullOrEmpty(canonicalName)) {
                if (messageName != null) {
                    PackageManagerMessages.Invoke.Error(messageName, "canonical-name",
                        "Canonical name '{0}' does not appear to be a valid canonical name".format(canonicalName));
                }
                return null;
            }

            // if canonical name is passed, override name,version,pkt,arch with the parsed canonicalname.
            var match = _canonicalNameParser.Match(canonicalName.ToLower());
            if( !match.Success ) {
                if (messageName != null) {
                    PackageManagerMessages.Invoke.Error(messageName, "canonical-name",
                        "Canonical name '{0}' does not appear to be a valid canonical name".format(canonicalName));
                }
                return null;
            }

            var pkg = SearchForPackages(match.Groups[1].Captures[0].Value, match.Groups[2].Captures[0].Value, match.Groups[3].Captures[0].Value,
                match.Groups[4].Captures[0].Value);

            if( !pkg.Any()) {
                if (messageName != null) {
                    PackageManagerMessages.Invoke.UnknownPackage(canonicalName);
                }
                return null;
            }

            if( pkg.Count() > 1 ) {
                if (messageName != null) {
                    PackageManagerMessages.Invoke.Error(messageName, "canonical-name",
                        "Canonical name '{0}' matches more than one package.".format(canonicalName));
                }
                return null; 
            }

            return pkg.FirstOrDefault();
        }

        internal List<string> BlockedScanLocations {
            get { return SessionCache<List<string>>.Value["suppressed-feeds"] ?? new List<string>(); }
        }

        internal Package GetPackageFromFilename(string filename ) {
            if (!File.Exists(filename)) {
                PackageManagerMessages.Invoke.FileNotFound(filename);
                return null;
            }

            Package pkg;

            filename = filename.GetFullPath();

            lock (_packages) {
                pkg = (_packages.Where(
                    package =>
                        package.InternalPackageData.HasLocalFile && package.InternalPackageData.LocalPackagePath.ContainsValue(filename))).
                    FirstOrDefault();
            }

            if (pkg != null) {
                return pkg;
            }

            var packageFileInformation = CoAppMSI.GetCoAppPackageFileInformation(filename);

            // try via just the package id
            if (!string.IsNullOrEmpty(packageFileInformation.packageId)) {
                lock (_packages) {
                    pkg = _packages.Where(package => package.ProductCode == packageFileInformation.packageId).FirstOrDefault();
                }
            }

            // try via the cosmetic name fields
            if (pkg == null) {
                lock (_packages) {
                    pkg = (_packages.Where(package =>
                        package.Architecture == packageFileInformation.Architecture &&
                        package.Version == packageFileInformation.Version &&
                        package.PublicKeyToken == packageFileInformation.PublicKeyToken &&
                        package.Name.Equals(packageFileInformation.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
                }
            }

            if (pkg == null) {
                pkg = new Package(packageFileInformation.Name, packageFileInformation.Architecture, packageFileInformation.Version, packageFileInformation.PublicKeyToken, packageFileInformation.packageId);

                lock( _packages ) {
                    _packages.Add(pkg);
                }
            }

            if (string.IsNullOrEmpty(pkg.ProductCode)) {
                pkg.ProductCode = packageFileInformation.packageId;
            }

            if (pkg.InternalPackageData.Dependencies.Count == 0) {
                pkg.InternalPackageData.Dependencies.AddRange((IEnumerable<Package>) packageFileInformation.dependencies);
            }

            pkg.InternalPackageData.LocalPackagePath.Value = filename;

            pkg.InternalPackageData.Assemblies.AddRange((IEnumerable<PackageAssemblyInfo>) packageFileInformation.assemblies.Values);
            pkg.InternalPackageData.Roles.AddRange((IEnumerable<Tuple<PackageRole, string>>) packageFileInformation.roles);

            pkg.InternalPackageData.PolicyMinimumVersion = packageFileInformation.policy_min_version;
            pkg.InternalPackageData.PolicyMaximumVersion = packageFileInformation.policy_max_version;

            pkg.PackageHandler = CoAppMSI.Instance;

            pkg.InternalPackageData.CanonicalFeedLocation = packageFileInformation.feedLocation;
            pkg.InternalPackageData.CanonicalPackageLocation = packageFileInformation.originalLocation;

            // set the delegate to get the package details if it is really needed.
            Cache<PackageDetails>.Value.Insert(pkg.CanonicalName, (unusedCanonicalFileName) => CoAppMSI.GetPackageDetails(pkg, filename));

            return pkg;
        }

        internal Package GetPackage(string packageName, ulong version, string architecture, string publicKeyToken, string packageId) {
            Package pkg;

            // try via just the package id
            if (!string.IsNullOrEmpty(packageId)) {
                lock (_packages) {
                    pkg = _packages.Where(package => package.ProductCode == packageId).FirstOrDefault();
                }

                if (pkg != null && string.IsNullOrEmpty(pkg.Name)) {
                    pkg.Name = packageName;
                    pkg.Architecture = architecture;
                    pkg.Version = version;
                    pkg.PublicKeyToken = publicKeyToken;
                }

                if (pkg != null)
                    return pkg;
            }

            lock (_packages) {
                pkg = (_packages.Where(package =>
                    package.Architecture == architecture &&
                    package.Version == version &&
                    package.PublicKeyToken == publicKeyToken &&
                    package.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
            }
            
            if (pkg == null) {
                pkg = new Package(packageName, architecture, version, publicKeyToken, packageId);
                lock(_packages) {
                    _packages.Add(pkg);
                }
            }

            if( !string.IsNullOrEmpty(packageId) && string.IsNullOrEmpty(pkg.ProductCode) ) {
                pkg.ProductCode = packageId;
            }

            return pkg;
        }
       
        internal IEnumerable<PackageFeed> Feeds { get {
            // ensure that the system feeds actually get loaded.
            Task.WaitAll(LoadSystemFeeds().ToArray());

            var canFilterSession = PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSessionFeeds);
            var canFilterSystem = PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditSystemFeeds);
            var feedFilters = BlockedScanLocations;

            return new PackageFeed[] { SessionPackageFeed.Instance,InstalledPackageFeed.Instance }.Union(
                from feed in Cache<PackageFeed>.Value.Values where !canFilterSystem || !feed.IsLocationMatch(feedFilters) select feed).Union(
                from feed in SessionCache<PackageFeed>.Value.SessionValues where !canFilterSession || !feed.IsLocationMatch(feedFilters) select feed);
        }}


#region package scanning
        /// <summary>
        /// Gets packages from all visible feeds based on criteria
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="arch"></param>
        /// <param name="publicKeyToken"></param>
        /// <returns></returns>
        internal IEnumerable<Package> SearchForPackages(string name, string version, string arch, string publicKeyToken, string location = null) {
            var feeds = string.IsNullOrEmpty(location) ? Feeds : Feeds.Where(each => each.IsLocationMatch(location));
            return feeds.SelectMany(each => each.FindPackages(name, version, arch, publicKeyToken)).Distinct().ToArray();
        }

        /// <summary>
        /// Gets just installed packages based on criteria
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="arch"></param>
        /// <param name="publicKeyToken"></param>
        /// <returns></returns>
        internal IEnumerable<Package> SearchForInstalledPackages(string name, string version, string arch, string publicKeyToken) {
            return InstalledPackageFeed.Instance.FindPackages(name, version, arch, publicKeyToken);
        }

        internal IEnumerable<Package> InstalledPackages {
            get { return InstalledPackageFeed.Instance.FindPackages(null, null, null, null); }
        }

        internal IEnumerable<Package> AllPackages {
            get { return SearchForPackages(null, null, null, null); }
        }

        #endregion

        /// <summary>
        /// This generates a list of files that need to be installed to sastisy a given package.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        private IEnumerable<Package> GenerateInstallGraph(Package package, bool hypothetical = false) {
            if (package.IsInstalled)
                yield break;

            var packageData = package.PackageSessionData;

            if( !package.PackageSessionData.PotentiallyInstallable)
                yield break;

            if (!packageData.DoNotSupercede) {

                var installedSupercedents = SearchForInstalledPackages(package.Name, null, package.Architecture, package.PublicKeyToken);

                if( package.PackageSessionData.UserSpecified || hypothetical )  {
                    // this means that we're talking about a requested package
                    // and not a dependent package and we can liberally construe supercedent 
                    // as anything with a highger version number
                    installedSupercedents =  (from p in installedSupercedents where p.Version > package.Version select p).OrderByDescending(p => p.Version).ToArray();

                } else {
                    // otherwise, we're installing a dependency, and we need something compatable.
                    installedSupercedents =  (from p in installedSupercedents 
                                where p.InternalPackageData.PolicyMinimumVersion <= package.Version &&
                                      p.InternalPackageData.PolicyMaximumVersion >= package.Version select p).OrderByDescending(p => p.Version).ToArray();
                }

                if (installedSupercedents.Any()) {
                    yield break; // a supercedent package is already installed.
                }

                // if told not to supercede, we won't even perform this check 
                packageData.Supercedent = null;
                
                var supercedents = SearchForPackages(package.Name, null, package.Architecture, package.PublicKeyToken).ToArray();

                if( package.PackageSessionData.UserSpecified || hypothetical )  {
                    // this means that we're talking about a requested package
                    // and not a dependent package and we can liberally construe supercedent 
                    // as anything with a highger version number
                    supercedents =  (from p in supercedents where p.Version > package.Version select p).OrderByDescending(p => p.Version).ToArray();

                } else {
                    // otherwise, we're installing a dependency, and we need something compatable.
                    supercedents =  (from p in supercedents 
                                where p.InternalPackageData.PolicyMinimumVersion <= package.Version &&
                                      p.InternalPackageData.PolicyMaximumVersion >= package.Version select p).OrderByDescending(p => p.Version).ToArray();
                }

                if (supercedents.Any()) {
                    if (packageData.AllowedToSupercede) {
                        foreach (var supercedent in supercedents) {
                            IEnumerable<Package> children;
                            try {
                                children = GenerateInstallGraph(supercedent, true);
                            }
                            catch {
                                // can't be satisfied with that supercedent.
                                // we can quietly move along here.
                                continue;
                            }

                            supercedent.PackageSessionData.UserSpecified = true;
                            // since we got to this spot, we can assume that we can 
                            // supercede this package with the results of the successful
                            // GIG call.
                            foreach (var child in children) {
                                yield return child;
                            }

                            // if we have a supercedent, then this package's dependents are moot.)
                            yield break;
                        }
                    }
                    else {
                        // the user hasn't specifically asked us to supercede, yet we know of 
                        // potential supercedents. Let's force the user to make a decision.
                        // throw new PackageHasPotentialUpgradesException(packageToSatisfy, supercedents);
                        PackageManagerMessages.Invoke.PackageHasPotentialUpgrades(package, supercedents);
                        throw new OperationCompletedBeforeResultException();
                    }
                }
            }

            if (packageData.CouldNotDownload) {
                if (!hypothetical) {
                    PackageManagerMessages.Invoke.UnableToDownloadPackage(package);
                }
                throw new OperationCompletedBeforeResultException();
            }

            if (packageData.PackageFailedInstall) {
                if (!hypothetical) {
                    PackageManagerMessages.Invoke.UnableToInstallPackage(package);
                }
                throw new OperationCompletedBeforeResultException();
            }

            var childrenFailed = false;
            foreach( var d in package.InternalPackageData.Dependencies ) {
                IEnumerable<Package> children;
                try {
                    children = GenerateInstallGraph(d);
                }
                catch {
                    childrenFailed = true;
                    continue;
                }

                foreach (var child in children)
                    yield return child;
            }

            if(childrenFailed) {
                throw new OperationCompletedBeforeResultException();
            }
            
            yield return package;
        }
 
        private void UpdateIsRequestedFlags() {
            lock (this) {
                var installedPackages = InstalledPackages.ToArray();

                foreach (var p in installedPackages) {
                    p.PackageSessionData.IsDependency = false;
                }

                foreach (var dep in installedPackages.SelectMany(package => package.InternalPackageData.Dependencies).Distinct()) {
                    // find each dependency that is the policy-preferred version, and mark it as currentlyrequested.
                    var sup = (from supercedent in SearchForInstalledPackages(dep.Name, null, dep.Architecture, dep.PublicKeyToken)
                        where dep.InternalPackageData.PolicyMinimumVersion <= dep.Version && dep.InternalPackageData.PolicyMaximumVersion >= dep.Version
                        select dep).OrderByDescending(p => p.Version).FirstOrDefault();

                    if( sup != null ) {
                        sup.PackageSessionData.IsDependency = true;
                    }
                }
            }
        }
    }
}
