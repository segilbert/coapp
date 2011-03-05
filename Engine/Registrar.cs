//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Exceptions;
    using Extensions;
    using Feeds;
    using Network;
    using PackageFormatHandlers;
    using Tasks;

    internal class Registrar {
        private static readonly HashSet<long> _nonCoAppMSIFiles = new HashSet<long>();
        private static bool _readCache;

        private static readonly ObservableCollection<Package> _packages = new ObservableCollection<Package>();
        private static readonly ObservableCollection<PackageFeed> _sessionFeedLocations = new SortedObservableCollection<PackageFeed>();
        private static readonly ObservableCollection<PackageFeed> _autoFeedLocations = new SortedObservableCollection<PackageFeed>();
        private static readonly ObservableCollection<PackageFeed> _systemFeedLocations = new SortedObservableCollection<PackageFeed>();

        internal static int StateCounter;

        internal static List<string> DoNotScanLocations = new List<string> {"c:\\windows\\*", PackageManagerSettings.CoAppPackageCache + "\\*"};

        #region FeedLocationAccessors

        public static IEnumerable<string> SystemFeedLocations {
            get { return _systemFeedLocations.GetFeedLocations(); }
        }

        public static IEnumerable<string> SessionFeedLocations {
            get { return _sessionFeedLocations.GetFeedLocations(); }
        }

        public static IEnumerable<string> AutoFeedLocations {
            get { return _autoFeedLocations.GetFeedLocations(); }
        }

        public static void AddSystemFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations) {
                AddSystemFeedLocation(location);
            }
        }

        public static void DeleteSystemFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations) {
                DeleteSystemFeedLocation(location);
            }
        }

        public static void AddSystemFeedLocation(string feedLocation) {
            _systemFeedLocations.AddFeedLocation(feedLocation);
        }

        public static void DeleteSystemFeedLocation(string feedLocation) {
            var info = Recognizer.Recognize(feedLocation);

            if (info.IsWildcard) {
                var feedsToDelete =
                    (from feed in _systemFeedLocations where feed.Location.IsWildcardMatch(feedLocation) select feed).ToList();
                foreach (var feed in feedsToDelete) {
                    _systemFeedLocations.Remove(feed);
                }
            }
            else if (info.IsFolder || info.IsFile) {
                var feedsToDelete =
                    (from feed in _systemFeedLocations
                     where feed.Location.Equals(info.FullPath, StringComparison.CurrentCultureIgnoreCase)
                     select feed).ToList();
                foreach (var feed in feedsToDelete) {
                    _systemFeedLocations.Remove(feed);
                }
            }
            else if (info.IsUnknown) {
                var feedsToDelete =
                    (from feed in _systemFeedLocations
                     where feed.Location.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase)
                     select feed).ToList();
                foreach (var feed in feedsToDelete) {
                    _systemFeedLocations.Remove(feed);
                }
            }
        }

        public static void AddSessionFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations) {
                AddSessionFeedLocation(location);
            }
        }

        public static void AddSessionFeedLocation(string feedLocation) {
            _sessionFeedLocations.AddFeedLocation(feedLocation);
        }

        public static void AddAutoFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations) {
                AddAutoFeedLocation(location);
            }
        }

        public static void AddAutoFeedLocation(string feedLocation) {
            _autoFeedLocations.AddFeedLocation(feedLocation);
        }

        #endregion

        #region Cache Management

        public static void FlushCache() {
            PackageManagerSettings.systemSettings["nonCoAppPackageMap"] = null;
            _nonCoAppMSIFiles.Clear();
            try {
                Parallel.ForEach(Directory.GetDirectories(PackageManagerSettings.CoAppCacheDirectory), dir => Directory.Delete(dir, true));
                Parallel.ForEach(Directory.GetFiles(PackageManagerSettings.CoAppCacheDirectory), File.Delete);
            }
            catch {
            }
        }

        public static void SaveCache() {
            if (!_readCache) {
                LoadCache();
            }

            using (var ms = new MemoryStream()) {
                var binaryWriter = new BinaryWriter(ms);

                // order of the following is very important.
                binaryWriter.Write(_nonCoAppMSIFiles.Count);
                foreach (var val in _nonCoAppMSIFiles) {
                    binaryWriter.Write(val);
                }

                PackageManagerSettings.systemSettings["nonCoAppPackageMap"] = ms.GetBuffer();
            }

            PackageManagerSettings.SystemStringArraySetting["feedLocations"] = SystemFeedLocations;
        }

        public static void LoadCache() {
            if (!_readCache) {
                _readCache = true;
                var cache = PackageManagerSettings.systemSettings["nonCoAppPackageMap"] as byte[];
                if (cache == null) {
                    return;
                }

                using (var ms = new MemoryStream(cache)) {
                    var binaryReader = new BinaryReader(ms);
                    var count = binaryReader.ReadInt32();
                    for (var i = 0; i < count; i++) {
                        var value = binaryReader.ReadInt64();
                        if (!_nonCoAppMSIFiles.Contains(value)) {
                            _nonCoAppMSIFiles.Add(value);
                        }
                    }
                }

                AddSystemFeedLocations(PackageManagerSettings.SystemStringArraySetting["feedLocations"]);
            }
        }

        #endregion

        internal static IEnumerable<Package> Packages {
            get { return _packages; }
        }

        private static void AddPackage( Package package) {
            lock( _packages ) {
                _packages.Add(package);
            }
        }

        internal static IEnumerable<Package> InstalledPackages {
            get { lock (_packages) return _packages.Where(package => package.IsInstalled).ToList(); }
        }

        static Registrar() {
            LoadCache();
            _packages.CollectionChanged += (x, y) => Updated();
            _sessionFeedLocations.CollectionChanged += (x, y) => Updated();
            _autoFeedLocations.CollectionChanged += (x, y) => Updated();
            _systemFeedLocations.CollectionChanged += (x, y) => Updated();
            _systemFeedLocations.CollectionChanged += (x, y) => SaveCache();
        }

        internal static void Updated() {
            StateCounter++;
#if false
            StackTrace stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            string txt = "";
            foreach( var f in frames) {
                if (f != null) {
                    var method = f.GetMethod();
                    var fnName = method.Name;
                    var cls = method.DeclaringType;
                    if (cls == null)
                        cls = stackTrace.GetType();

                    var clsName = cls.Name;

                    var filters = new[] { "*Thread*", "*Enumerable*", "*__*", "*trace*", "*updated*", "*Task*" }; //"*`*",
                    var print = true;
                    foreach( var flt in filters ) {
                        if (fnName.IsWildcardMatch(flt) || clsName.IsWildcardMatch(flt))
                            print = false;
                    }
                    if( print )
                        txt += string.Format("<=[{1}.{0}]", fnName, clsName);
                }
                
            }
            Debug.WriteLine("Counter[{0}] {1}",StateCounter, txt);
            Console.WriteLine("  Counter {0}",StateCounter);
#endif
        }

        internal static Package GetPackage(string packageName, string architecture, UInt64 version, string publicKeyToken, string packageId) {
            Package pkg;

            lock (_packages) {
                pkg = (_packages.Where(package =>
                    package.Architecture == architecture &&
                        package.Version == version &&
                            package.PublicKeyToken == publicKeyToken &&
                                package.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
            }

            if (pkg == null) {
                pkg = new Package(packageName, architecture, version, publicKeyToken, packageId);
                AddPackage(pkg);
            }

            return pkg;
        }

        internal static Package GetPackage(string packagePath) {
            if (packagePath.Contains("*")) {
                throw new PackageNotFoundException(packagePath);
            }

            var localPackagePath = Path.GetFullPath(packagePath);

            if (!File.Exists(localPackagePath)) {
                // could this be another representation of a package?
                if (!localPackagePath.EndsWith(".msi")) {
                    return GetPackage(localPackagePath + ".msi");
                }

                throw new PackageNotFoundException(localPackagePath);
            }

            var lookup = File.GetCreationTime(localPackagePath).Ticks + localPackagePath.GetHashCode();

            if (_nonCoAppMSIFiles.Contains(lookup)) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }

            Package pkg;
            lock (_packages) {
                pkg = (_packages.Where(
                    package =>
                        package.HasLocalFile && package.HasAlternatePath(localPackagePath))).
                    FirstOrDefault();
            }
            if (pkg != null) {
                return pkg;
            }

            var localFolder = Path.GetDirectoryName(localPackagePath).ToLower();
            // remember this place for finding packages
            AddAutoFeedLocation(localFolder);

            try {
                var pkgDetails = CoAppMSI.GetCoAppPackageFileDetails(localPackagePath);
                lock (_packages) {
                    pkg = (_packages.Where(package =>
                        package.Architecture == pkgDetails.Architecture &&
                            package.Version == pkgDetails.Version &&
                                package.PublicKeyToken == pkgDetails.PublicKeyToken &&
                                    package.Name.Equals(pkgDetails.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
                }
                if (pkg == null) {
                    pkg = new Package(pkgDetails.Name, pkgDetails.Architecture, pkgDetails.Version, pkgDetails.PublicKeyToken,
                        pkgDetails.packageId);

                    AddPackage(pkg);
                }

                if (pkg.Dependencies.Count == 0) {
                    pkg.Dependencies.AddRange((IEnumerable<Package>) pkgDetails.dependencies);
                }

                if (pkg.LocalPackagePath == null) {
                    pkg.LocalPackagePath = localPackagePath;
                }
                else {
                    pkg.AddAlternatePath(localPackagePath);
                }

                pkg.Assemblies.AddRange((IEnumerable<PackageAssemblyInfo>) pkgDetails.assemblies.Values);
                pkg.Roles.AddRange((IEnumerable<Tuple<string, string>>) pkgDetails.roles);

                pkg.PolicyMinimumVersion = pkgDetails.policy_min_version;
                pkg.PolicyMaximumVersion = pkgDetails.policy_max_version;
                
                // pkgDetails.displayName
                pkg.FullDescription = pkgDetails.description;
                pkg.PublishDate = DateTime.Parse(pkgDetails.publishDate);
                pkg.AuthorVersion = pkgDetails.authorVersion;
                pkg.PackageLocation = pkgDetails.originalLocation;
                pkg.FeedLocation = pkgDetails.feedLocation;
                pkg.Base64IconData = pkgDetails.icon;
                pkg.SummaryDescription = pkgDetails.summary;
                pkg.Publisher.Name = pkgDetails.publisherName;
                pkg.Publisher.Url = pkgDetails.publisherUrl;
                pkg.Publisher.Email = pkgDetails.publisherEmail;

                pkg.packageHandler = new CoAppMSI();

                return pkg;
            }
            catch (InvalidPackageException ipe) {
                if (ipe.Reason == InvalidReason.NotCoAppMSI) {
                    _nonCoAppMSIFiles.Add(lookup);
                }
                throw;
            }
        }

        /// <summary>
        ///   Returns a list of packages matching a given list of desired packages.
        /// 
        ///   This method should be aggressive in locating packages
        /// </summary>
        /// <param name = "packageNames"></param>
        /// <returns></returns>
        public static IEnumerable<Package> GetPackagesByName(IEnumerable<string> packageNames) {
            var packageFiles = new List<Package>();
            var unknownPackages = new List<string>();

            foreach (var item in packageNames) {
                try {
                    var currentItem = item;
                    var info = Recognizer.Recognize(currentItem, ensureLocal: true);

                    if (info.RemoteFile != null) {
                        PackageManagerMessages.Invoke.DownloadingFile(info.RemoteFile);

                        info.RemoteFile.DownloadProgress.Notification += (progress) => PackageManagerMessages.Invoke.DownloadingFileProgress(info.RemoteFile,progress);

                        info.Recognized.Notification += v => {
                            if (info.IsPackageFeed) {
                                // we have been given a package feed, and asked to return all the packages from it
                                PackageFeed.GetPackageFeedFromLocation(currentItem).ContinueWith(antecedent => {
                                    packageFiles.AddRange(antecedent.Result.FindPackages("*"));
                                }).Wait();
                            }
                            else if (info.IsPackageFile) {
                                packageFiles.Add(GetPackage(info.FullPath));
                            }
                            else {
                                unknownPackages.Add(currentItem);
                            }
                        };
                    }
                    else if (info.IsPackageFile) {
                        packageFiles.Add(GetPackage(info.FullPath));
                    }
                    else {
                        unknownPackages.Add(currentItem);
                    }
                }
                catch (PackageNotFoundException) {
                    unknownPackages.Add(item);
                }
            }


            if (unknownPackages.Count > 0) {
                ScanForPackages(unknownPackages);

                foreach (var item in unknownPackages) {
                    IEnumerable<Package> possibleMatches;
                    lock (_packages) {
                        possibleMatches = _packages.Match(item + (item.Contains("*") || item.Contains("-") ? "*" : "-*")).HighestPackages();
                    }

                    if (possibleMatches.Count() == 0) {
                        throw new PackageNotFoundException(item);
                    }

                    if (possibleMatches.Count() == 1) {
                        packageFiles.Add(possibleMatches.First());
                    }
                    else {
                        // for package matching, we only support 1 match for a given unknown.
                        PackageManagerMessages.Invoke.MultiplePackagesMatch(item, possibleMatches);
                        throw new OperationCompletedBeforeResultException();
                    }
                }
            }
            return packageFiles;
        }

        /// <summary>
        ///   Returns a list of installed packages matching a given list of desired packages.
        /// </summary>
        /// <param name = "packageNames"></param>
        /// <returns></returns>
        public static IEnumerable<Package> GetInstalledPackagesByName(IEnumerable<string> packageNames) {
            var packageFiles = new List<Package>();
            var unknownPackages = new List<string>();

            foreach (var item in packageNames) {
                try {
                    var info = Recognizer.Recognize(item);
                    if (info.IsPackageFeed) {
                        // we have been given a package feed, and asked to return all the packages from it
                        // this lets you ask to uninstall a whole feed (and the only way to get multiple matches 
                        // from a single parameter.)

                        PackageFeed.GetPackageFeedFromLocation(item).ContinueWith(antecedent => {
                            var feed = antecedent.Result;
                            packageFiles.AddRange(feed.FindPackages("*").Where(each => each.IsInstalled));
                        }).Wait();
                    }
                    else if (info.IsPackageFile) {
                        var pkg = GetPackage(item);
                        if (pkg.IsInstalled) {
                            packageFiles.Add(GetPackage(item));
                        }
                        else {
                            throw new PackageNotInstalledException(pkg);
                        }
                    }
                    else {
                        unknownPackages.Add(item);
                    }
                }
                catch (PackageNotFoundException) {
                    unknownPackages.Add(item);
                }
            }

            if (unknownPackages.Count > 0) {
                ScanForPackages(unknownPackages);

                foreach (var item in unknownPackages) {
                    var possibleMatches = InstalledPackages.Match(item + (item.Contains("*") || item.Contains("-") ? "*" : "-*"));

                    if (possibleMatches.Count() == 0) {
                        PackageManagerMessages.Invoke.PackageNotFound(item);
                        throw new OperationCompletedBeforeResultException();
                    }

                    if (possibleMatches.Count() == 1) {
                        packageFiles.Add(possibleMatches.First());
                    }
                    else {
                        // for package matching, we only support 1 match for a given unknown.
                        PackageManagerMessages.Invoke.MultiplePackagesMatch(item, possibleMatches);
                        throw new OperationCompletedBeforeResultException();
                    }
                }
            }
            return packageFiles;
        }

        // Scan Methods find what they can, empty results are OK.

        internal static IEnumerable<Package> ScanForPackages(IEnumerable<Package> packagesToScanFor) {
            return packagesToScanFor.Aggregate(Enumerable.Empty<Package>(),
                (current, package) => current.Union(ScanForPackages(package)).Union(ScanForPackages(package.Dependencies)));
        }

        internal static IEnumerable<Package> ScanForPackages(IEnumerable<string> packageFilters) {
            return packageFilters.Aggregate(Enumerable.Empty<Package>(), (current, filter) => current.Union(ScanForPackages(filter)));
        }

        internal static IEnumerable<Package> ScanForPackages(Package package) {
            var feeds = _autoFeedLocations.Union(_sessionFeedLocations).Union(_systemFeedLocations);
            feeds = DoNotScanLocations.Aggregate(feeds,
                (current, loc) => (from feed in current where !feed.Location.IsWildcardMatch(loc) select feed));
            return feeds.Aggregate(Enumerable.Empty<Package>(), (current, feed) => current.Union(feed.FindPackages(package)));
        }

        internal static IEnumerable<Package> ScanForPackages(string packageFilter) {
            if (!packageFilter.Contains("*")) {
                packageFilter += "*";
            }

            var feeds = _autoFeedLocations.Union(_sessionFeedLocations).Union(_systemFeedLocations);
            feeds = DoNotScanLocations.Aggregate(feeds,
                (current, loc) => (from feed in current where !feed.Location.IsWildcardMatch(loc) select feed));
            return feeds.Aggregate(Enumerable.Empty<Package>(), (current, feed) => current.Union(feed.FindPackages(packageFilter)));
        }
    }
}