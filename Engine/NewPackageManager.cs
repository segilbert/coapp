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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Feeds;
    using Tasks;

    public class NewPackageManager {

        public static NewPackageManager Instance = new NewPackageManager();

        private NewPackageManager() {
            // load system feeds
            var systemFeeds = PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue;
            foreach( var f in systemFeeds ) {
                var feedLocation = f;
                PackageFeed.GetPackageFeedFromLocation(feedLocation, false).ContinueWith(antecedent => {
                    if (antecedent.Result != null) {
                        Cache<PackageFeed>.Value[feedLocation] = antecedent.Result;
                    }
                    else {
                        LogMessage("Feed {0} was unable to load.", feedLocation);
                    }
                });
            }
        }


        private void LogMessage(string message, params object[] objs) {
            string msg = message.format(objs);
            // do something with the message?
        }

        public Task FindPackages( NewPackageManagerMessages messages ) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task GetPackageDetails(string canonicalName, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task InstallPackage(string canonicalName, bool? autoUpgrade, bool? force, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task ListFeeds(NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                if (Cache<PackageFeed>.Value.Values.Any() || SessionCache<PackageFeed>.Value.Values.Any()) {
                    foreach (var feed in SessionCache<PackageFeed>.Value.Values) {
                        NewPackageManagerMessages.Invoke.FeedDetails(feed.Location, feed.LastScanned, true);
                    }

                    foreach (var feed in Cache<PackageFeed>.Value.Values) {
                        NewPackageManagerMessages.Invoke.FeedDetails(feed.Location, feed.LastScanned, false);
                    }
                } else {
                    NewPackageManagerMessages.Invoke.NoFeedsFound();
                }

            }).AutoManage();
            return t;
        }

        public Task RemoveFeed(string location, bool? session, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                if( !PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditFeeds) ) {
                    NewPackageManagerMessages.Invoke.PermissionRequired("EditFeeds");
                    return;
                }

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task AddFeed(string location, bool? session, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                if( !PackageManagerSession.Invoke.CheckForPermission(PermissionPolicy.EditFeeds) ) {
                    NewPackageManagerMessages.Invoke.PermissionRequired("EditFeeds");
                    return;
                }

                /*
                if( !location.IsPathOrUrl()  ) {
                    NewPackageManagerMessages.Invoke.Error("add-feed", "location", "location '{0}' does not appear to be path or URL".format(location));
                    return;
                }
                */


                if( session ?? false ) {
                    // new feed is a session feed

                    // check if it is already a system feed
                    if (PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue.Contains(location)) {
                        NewPackageManagerMessages.Invoke.Warning("add-feed", "location", "location '{0}' is already a system feed".format(location));
                        return;
                    }

                    if( (from feed in SessionCache<PackageFeed>.Value.Values where feed.Location == location select feed).Any() ) {
                        NewPackageManagerMessages.Invoke.Warning("add-feed", "location", "location '{0}' is already a session feed".format(location));
                        return;
                    }

                    // add feed to the session feeds.
                    PackageFeed.GetPackageFeedFromLocation(location).ContinueWith(antecedent => {
                        if (antecedent.Result != null) {
                            SessionCache<PackageFeed>.Value[location] = antecedent.Result;
                            NewPackageManagerMessages.Invoke.FeedAdded(location);
                        }
                        else {
                            NewPackageManagerMessages.Invoke.Error("add-feed", "location", "failed to recognize location '{0}' as a valid package feed".format(location));
                            LogMessage("Feed {0} was unable to load.", location);
                        }
                    }, TaskContinuationOptions.AttachedToParent);

                }else {
                    // new feed is a system feed
                    if( PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue.Contains(location) ) {
                        NewPackageManagerMessages.Invoke.Warning("add-feed", "location", "location '{0}' is already a system feed".format(location));
                        return;
                    }
                    // add feed to the system feeds.
                    PackageFeed.GetPackageFeedFromLocation(location).ContinueWith(antecedent => {
                        if (antecedent.Result != null) {
                            lock(this) {
                                var systemFeeds = PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue.UnionSingleItem(location);
                                PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue = systemFeeds;
                            }
                            Cache<PackageFeed>.Value[location] = antecedent.Result;
                            NewPackageManagerMessages.Invoke.FeedAdded(location);
                        }
                        else {
                            NewPackageManagerMessages.Invoke.Error("add-feed", "location", "failed to recognize location '{0}' as a valid package feed".format(location));
                            LogMessage("Feed {0} was unable to load.", location);
                        }
                    }, TaskContinuationOptions.AttachedToParent);
                }

            }).AutoManage();
            return t;
        }

        public Task VerifyFileSignature(string filename, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task SetPackage(string canonicalName, bool? active, bool? required, bool? blocked, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task RemovePackage(string canonicalName, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task UnableToAcquire(string referenceId, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task RecognizeFile(string referenceId, string localLocation, string remoteLocation, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        internal void Updated() {
            
        }

        internal IEnumerable<string> BlockedScanLocations {
            get { return Enumerable.Empty<string>(); }
        }

        internal Package GetPackageFromFilename(string filename ) {
            throw new NotImplementedException();
        }

        internal Package GetPackageByDetails(string name, ulong version, string architecture, string publicKeyToken, string id) {
            throw new NotImplementedException();
        }

        internal IEnumerable<Package> InstalledPackages {
            get {
                throw new NotImplementedException();
            }
        }
    }

    public class PackageManagerSessionData {
        
    }
    
}
