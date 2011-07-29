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
    using Exceptions;
    using Extensions;
    using Feeds;
    using PackageFormatHandlers;
    using Tasks;
    using Win32;

    internal class Registrar {
        private static readonly HashSet<long> _nonCoAppMSIFiles = new HashSet<long>();
        private static Regex _canonicalFilter = new Regex(@"^(\S*)-(\d*\.\d*\.\d*\.\d*)-(\S*)-([\dabcdef]{16})$");
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

        public static Task AddSystemFeedLocations(IEnumerable<string> feedLocations) {
            return CoTask.Factory.StartNew(() => {
                foreach (var location in feedLocations) {
                    AddSystemFeedLocation(location);
                }
            });
        }

        public static Task DeleteSystemFeedLocations(IEnumerable<string> feedLocations) {
            return CoTask.Factory.StartNew(() => {
                foreach (var location in feedLocations) {
                    DeleteSystemFeedLocation(location);
                }
            });
        }

        public static Task AddSystemFeedLocation(string feedLocation) {
            return _systemFeedLocations.AddFeedLocation(feedLocation);
        }

        public static Task DeleteSystemFeedLocation(string feedLocation) {
            return Recognizer.Recognize(feedLocation).ContinueWithParent(antecedent => {
                var info = antecedent.Result;

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
            });
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
            PackageManagerSettings.CacheSettings["#nonCoAppPackageMap"].Value = null;

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

                PackageManagerSettings.CacheSettings["#nonCoAppPackageMap"].BinaryValue = ms.GetBuffer();
            }

            PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue = SystemFeedLocations;
        }

        public static void LoadCache() {
            if (!_readCache) {
                _readCache = true;
                var cache = PackageManagerSettings.CacheSettings["#nonCoAppPackageMap"].BinaryValue;
                if (cache.IsNullOrEmpty()) {
                    return;
                }

                using (var ms = new MemoryStream(cache.ToArray())) {
                    var binaryReader = new BinaryReader(ms);
                    var count = binaryReader.ReadInt32();
                    for (var i = 0; i < count; i++) {
                        var value = binaryReader.ReadInt64();
                        if (!_nonCoAppMSIFiles.Contains(value)) {
                            _nonCoAppMSIFiles.Add(value);
                        }
                    }
                }
                AddSystemFeedLocations(PackageManagerSettings.CoAppSettings["#feedLocations"].StringsValue);
            }
        }

        public static IEnumerable<string> GetCachedStrings(string cachename, params object[] args ) {
            return PackageManagerSettings.CacheSettings["#"+cachename.format(args)].StringsValue;
        }

        public static void SetCachedStrings(IEnumerable<string> values, string cachename, params object[] args) {
            PackageManagerSettings.CacheSettings["#"+cachename.format(args)].StringsValue = values;
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
        }

        internal static Package GetPackage(string packageName, UInt64 version,string architecture, string publicKeyToken, string packageId) {
            Package pkg;

            // try via just the package id
            if (!string.IsNullOrEmpty(packageId)) {
                lock (_packages) {
                    pkg = _packages.Where(package => package.ProductCode == packageId).FirstOrDefault();
                }

                if (pkg != null && string.IsNullOrEmpty(pkg.Name)) {
                    pkg.SetPackageProperties(packageName, architecture, version, publicKeyToken);
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
                AddPackage(pkg);
            }

            if( !string.IsNullOrEmpty(packageId) && string.IsNullOrEmpty(pkg.ProductCode) ) {
                pkg.ProductCode = packageId;
            }

            return pkg;
        }


        internal static Package GetPackage(string packagePath) {
            Package pkg;
            Guid pkgGuid;

            if (packagePath.Contains("*")) {
                throw new PackageNotFoundException(packagePath);
            }

            // supports looking up by productcode/packageID.
            if( packagePath[0] == '{' && Guid.TryParse(packagePath, out pkgGuid)) {
                pkg = _packages.Where(package => package.ProductCode == packagePath).FirstOrDefault();
                if( pkg == null ) {
                    // where the only thing we know is packageID.
                    pkg = new Package(packagePath);
                    AddPackage(pkg);
                }
                return pkg;
            }

            // lookup by canonical name.
            var match = _canonicalFilter.Match(packagePath);
            if( match.Success ) {
                return GetPackage(match.Groups[1].Value, match.Groups[2].Value.VersionStringToUInt64(), match.Groups[3].Value, match.Groups[4].Value,
                    string.Empty);
            }
            
            // assuming at this point, it should be a filename?
            var localPackagePath = Path.GetFullPath(packagePath);

            if (!File.Exists(localPackagePath)) {
                // could this be another representation of a package?
                // not sure we ever ever get here anymore.
                
                if (!localPackagePath.EndsWith(".msi")) {
                    Console.WriteLine("Trying package name with .msi appendend. Is this used?");
                    return GetPackage(localPackagePath + ".msi");
                }

                throw new PackageNotFoundException(localPackagePath);
            }

            var lookup = File.GetCreationTime(localPackagePath).Ticks + localPackagePath.GetHashCode();

            if (_nonCoAppMSIFiles.Contains(lookup)) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }

            
            lock (_packages) {
                pkg = (_packages.Where(
                    package =>
                        package.HasLocalFile && package.LocalPackagePath.ContainsValue(localPackagePath))).
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

                // try via just the package id
                if (!string.IsNullOrEmpty(pkgDetails.packageId)) {
                    lock (_packages) {
                        pkg = _packages.Where(package => package.ProductCode == pkgDetails.packageId).FirstOrDefault();
                    }
                }

                // try via the cosmetic name fields
                if (pkg == null) {
                    lock (_packages) {
                        pkg = (_packages.Where(package =>
                            package.Architecture == pkgDetails.Architecture &&
                                package.Version == pkgDetails.Version &&
                                    package.PublicKeyToken == pkgDetails.PublicKeyToken &&
                                        package.Name.Equals(pkgDetails.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
                    }
                }

                if (pkg == null) {
                    pkg = new Package(pkgDetails.Name, pkgDetails.Architecture, pkgDetails.Version, pkgDetails.PublicKeyToken,
                        pkgDetails.packageId);

                    AddPackage(pkg);
                }

                if (string.IsNullOrEmpty(pkg.ProductCode)) {
                    pkg.ProductCode = pkgDetails.packageId;
                }

                if (pkg.Dependencies.Count == 0) {
                    pkg.Dependencies.AddRange((IEnumerable<Package>) pkgDetails.dependencies);
                }

                pkg.LocalPackagePath.Value = localPackagePath;

                pkg.Assemblies.AddRange((IEnumerable<PackageAssemblyInfo>) pkgDetails.assemblies.Values);
                pkg.Roles.AddRange((IEnumerable<Tuple<PackageRole, string>>) pkgDetails.roles);

                pkg.PolicyMinimumVersion = pkgDetails.policy_min_version;
                pkg.PolicyMaximumVersion = pkgDetails.policy_max_version;
                
                // pkgDetails.displayName
                pkg.FullDescription = pkgDetails.description;
                long publishDateTicks;
                if (Int64.TryParse(pkgDetails.publishDate, out publishDateTicks)) {
                    pkg.PublishDate = new DateTime(publishDateTicks);
                }
                pkg.AuthorVersion = pkgDetails.authorVersion;
                pkg.CanonicalPackageLocation = pkgDetails.originalLocation;
                pkg.CanonicalFeedLocation = pkgDetails.feedLocation;
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
        public static Task<IEnumerable<Package>> GetPackagesByName(IEnumerable<string> packageNames, MessageHandlers messageHandlers = null) {
            return CoTask<IEnumerable<Package>>.Factory.StartNew(() => {
                var packageFiles = new List<Package>();
                var unknownPackages = new List<string>();

                foreach (var item in packageNames) {
                    var currentItem = item;
                    Recognizer.Recognize(currentItem, ensureLocal: true).ContinueWithParent(antecedent0 => {
                        try {
                            var info = antecedent0.Result;

                            if (info.RemoteFile != null) {
                                PackageManagerMessages.Invoke.DownloadingFile(info.RemoteFile);

                                info.RemoteFile.DownloadProgress.Notification +=
                                    (progress) => PackageManagerMessages.Invoke.DownloadingFileProgress(info.RemoteFile, progress);

                                info.Recognized.Notification += v => {
                                    if (info.IsPackageFeed) {
                                        // we have been given a package feed, and asked to return all the packages from it
                                        PackageFeed.GetPackageFeedFromLocation(currentItem).ContinueWithParent(antecedent => {
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
                    });
                }

                Tasklet.WaitforCurrentChildTasks(); // HACK HACK HACK ???

                if (unknownPackages.Count > 0) {
                    ScanForPackages(unknownPackages);

                    foreach (var item in unknownPackages) {
                        IEnumerable<Package> possibleMatches;
                        lock (_packages) {
                            possibleMatches =
                                _packages.Match(item + (item.Contains("*") || item.Contains("-") ? "*" : "-*")).HighestPackages();
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
            }, messageHandlers);
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
                    var info = Recognizer.Recognize(item).Result; // this should be a local file...
                    if (info.IsPackageFeed) {
                        // we have been given a package feed, and asked to return all the packages from it
                        // this lets you ask to uninstall a whole feed (and the only way to get multiple matches 
                        // from a single parameter.)

                        PackageFeed.GetPackageFeedFromLocation(item).ContinueWithParent(antecedent => {
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

        public static void DumpPackages(IEnumerable<Package> packages = null) {
            if (packages == null)
                packages = _packages;

            if (packages.Count() > 0) {
                Console.WriteLine("\rPackages:");
                (from pkg in packages
                 orderby pkg.IsInstalled descending, pkg.Name 
                        select new {
                            Name = pkg.Name,
                            Version = pkg.Version.UInt64VersiontoString(),
                            Arch = pkg.Architecture,
                            Publisher = pkg.Publisher.Name,
                            Local_Path = pkg.LocalPackagePath.Value ?? "(not local)",
                            Remote_Location = pkg.RemoteLocation.Value != null ? pkg.RemoteLocation.Value.AbsoluteUri : "<unknown>",
                            Installed = pkg.IsInstalled
                     } ) .ToTable(Console.BufferWidth).ConsoleOut();
            }
            else {
                Console.WriteLine("\rNo packages.");
            }
        }

       

    }
}