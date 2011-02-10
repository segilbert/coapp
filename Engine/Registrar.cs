//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Threading;
    using System.Xml;
    using Exceptions;
    using Extensions;
    using Feeds;
    using Feeds.Atom;
    using Network;
    using PackageFormatHandlers;

    internal class Registrar {
        private static readonly HashSet<long> nonCoAppMSIFiles = new HashSet<long>();
        private static bool _readCache;

        private static readonly ObservableCollection<Package> packages = new ObservableCollection<Package>();
        private static readonly ObservableCollection<PackageFeed> sessionFeedLocations = new SortedObservableCollection<PackageFeed>();
        private static readonly ObservableCollection<PackageFeed> autoFeedLocations = new SortedObservableCollection<PackageFeed>();
        private static readonly ObservableCollection<PackageFeed> systemFeedLocations = new SortedObservableCollection<PackageFeed>();
        
        internal static int StateCounter;
        internal static List<string> DoNotScanLocations = new List<string>() {"c:\\windows\\*", PackageManagerSettings.CoAppPackageCache +"\\*"};
        internal static CachingHttpClient HttpClient = new CachingHttpClient(PackageManagerSettings.CoAppCacheDirectory);

        #region FeedLocationAccessors

        public static void AddSystemFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations)
                AddSystemFeedLocation(location);
        }

        public static void DeleteSystemFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations)
                DeleteSystemFeedLocation(location);
        }

        public static void AddSystemFeedLocation(string feedLocation) {
            systemFeedLocations.AddFeedLocation(feedLocation);
        }

        public static void DeleteSystemFeedLocation( string feedLocation ) {
            var info = Recognizer.Recognize(feedLocation);
            
            if( info.IsWildcard  ) {
                var feedsToDelete = (from feed in systemFeedLocations where feed.Location.IsWildcardMatch(feedLocation) select feed).ToList();
                foreach (var feed in feedsToDelete) {
                    systemFeedLocations.Remove(feed);
                }
            }
            else if( info.IsFolder || info.IsFile ) {
                var feedsToDelete = (from feed in systemFeedLocations where feed.Location.Equals(info.fullPath,StringComparison.CurrentCultureIgnoreCase) select feed).ToList();
                foreach (var feed in feedsToDelete) {
                    systemFeedLocations.Remove(feed);
                }
            }
            else if (info.IsUnknown) {
                var feedsToDelete = (from feed in systemFeedLocations where feed.Location.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase) select feed).ToList();
                foreach (var feed in feedsToDelete) {
                    systemFeedLocations.Remove(feed);
                }
            }
        }


        public static IEnumerable<string> SystemFeedLocations {
            get { return systemFeedLocations.GetFeedLocations(); }
        }

        public static void AddSessionFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations)
                AddSessionFeedLocation(location);
        }

        public static void AddSessionFeedLocation(string feedLocation) {
            sessionFeedLocations.AddFeedLocation(feedLocation);
        }

        public static IEnumerable<string> SessionFeedLocations {
            get { return sessionFeedLocations.GetFeedLocations(); }
        }

        public static void AddAutoFeedLocations(IEnumerable<string> feedLocations) {
            foreach (var location in feedLocations)
                AddAutoFeedLocation(location);
        }

        public static void AddAutoFeedLocation(string feedLocation) {
            autoFeedLocations.AddFeedLocation(feedLocation);
        }

        public static IEnumerable<string> AutoFeedLocations {
            get { return autoFeedLocations.GetFeedLocations(); }
        }

        #endregion

        #region Cache Management
        public static void FlushCache() {
            PackageManagerSettings.systemSettings["nonCoAppPackageMap"] = null;
            nonCoAppMSIFiles.Clear();
            try {
                foreach (var file in Directory.GetFiles(PackageManagerSettings.CoAppCacheDirectory)) {
                    File.Delete(file);
                }
            } catch {
                
            }
        }

        public static void SaveCache() {
            if (!_readCache)
                LoadCache();
            
            using (var ms = new MemoryStream()) {

                var binaryWriter = new BinaryWriter(ms);

                // order of the following is very important.
                binaryWriter.Write(nonCoAppMSIFiles.Count);
                foreach (var val in nonCoAppMSIFiles)
                    binaryWriter.Write(val);

                PackageManagerSettings.systemSettings["nonCoAppPackageMap"] = ms.GetBuffer();
            }

            PackageManagerSettings.SystemStringArraySetting["feedLocations"] = SystemFeedLocations;
        }

        public static void LoadCache() {
            if (!_readCache) {
                _readCache = true;
                var cache = PackageManagerSettings.systemSettings["nonCoAppPackageMap"] as byte[];
                if (cache == null)
                    return;

                using (var ms = new MemoryStream(cache)) {
                    var binaryReader = new BinaryReader(ms);
                    var count = binaryReader.ReadInt32();
                    for (var i = 0; i < count; i++) {
                        var value = binaryReader.ReadInt64();
                        if (!nonCoAppMSIFiles.Contains(value))
                            nonCoAppMSIFiles.Add(value);
                    }
                }

                AddSystemFeedLocations(PackageManagerSettings.SystemStringArraySetting["feedLocations"]);
                
            }
        }
        #endregion

        internal static IEnumerable<Package> Packages { get { return packages;  } }
        internal static IEnumerable<Package> InstalledPackages { get { return packages.Where(package => package.IsInstalled); } }
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

        static Registrar() {
            LoadCache();
            packages.CollectionChanged += (x, y) => Updated();
            sessionFeedLocations.CollectionChanged += (x, y) => Updated();
            autoFeedLocations.CollectionChanged += (x, y) => Updated();
            systemFeedLocations.CollectionChanged += (x, y) => Updated();
            systemFeedLocations.CollectionChanged += (x, y) => SaveCache();
        }

        internal static Package GetPackage(string packageName, string architecture, UInt64 version, string publicKeyToken, string packageId) {
            var pkg = (packages.Where(package =>
                package.Architecture == architecture &&
                    package.Version == version &&
                        package.PublicKeyToken == publicKeyToken &&
                            package.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();

            if (pkg == null) {
                pkg = new Package(packageName, architecture, version, publicKeyToken, packageId);
                packages.Add(pkg);
            }

            return pkg;
        }

        internal static Package GetPackage(string packagePath) {
            if( packagePath.Contains("*") ) {
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

            if (nonCoAppMSIFiles.Contains(lookup)) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }

            var pkg =
                (packages.Where(
                    package =>
                        package.HasLocalFile && package.HasAlternatePath(localPackagePath))).
                    FirstOrDefault();

            if (pkg != null) {
                return pkg;
            }

            var localFolder = Path.GetDirectoryName(localPackagePath).ToLower();
            // remember this place for finding packages
            AddAutoFeedLocation(localFolder);

            try {
                var pkgDetails = CoAppMSI.GetCoAppPackageFileDetails(localPackagePath);

                pkg = (packages.Where(package =>
                package.Architecture == pkgDetails.Architecture &&
                    package.Version == pkgDetails.Version &&
                        package.PublicKeyToken == pkgDetails.PublicKeyToken &&
                            package.Name.Equals(pkgDetails.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();

                if (pkg == null) {
                    pkg = new Package(pkgDetails.Name, pkgDetails.Architecture, pkgDetails.Version, pkgDetails.PublicKeyToken,
                        pkgDetails.packageId);

                    packages.Add(pkg);
                }
                if( pkg.Dependencies.Count == 0)
                    pkg.Dependencies.AddRange((IEnumerable<Package>)pkgDetails.dependencies);

                if (pkg.LocalPackagePath == null) {
                    pkg.LocalPackagePath = localPackagePath;
                }
                else {
                    pkg.AddAlternatePath(localPackagePath);
                }

                pkg.Assemblies.AddRange((IEnumerable<PackageAssemblyInfo>)pkgDetails.assemblies.Values);
                pkg.Roles.AddRange((IEnumerable<Tuple<string, string>>)pkgDetails.roles);

                pkg.PolicyMinimumVersion = pkgDetails.policy_min_version;
                pkg.PolicyMaximumVersion = pkgDetails.policy_max_version;
                pkg.packageHandler = new CoAppMSI();

                return pkg;

            } catch (InvalidPackageException ipe) {
                if( ipe.Reason == InvalidReason.NotCoAppMSI) {
                    nonCoAppMSIFiles.Add(lookup);
                }
                throw;
            }
        }

        /// <summary>
        /// Returns a list of packages matching a given list of desired packages.
        /// 
        /// This method should be aggressive in locating packages
        /// </summary>
        /// <param name="packageNames"></param>
        /// <returns></returns>
        public static IEnumerable<Package> GetPackagesByName(IEnumerable<string> packageNames) {
            var packageFiles = new List<Package>();
            var unknownPackages = new List<string>();

            foreach (var item in packageNames) {
                try {
                    var info = Recognizer.Recognize(item);
                    if (info.IsPackageFeed) {
                        // we have been given a package feed, and asked to return all the packages from it
                        var feed = PackageFeed.GetPackageFeedFromLocation(item);
                        packageFiles.AddRange(feed.FindPackages("*"));
                    }
                    else if (info.IsPackageFile) {
                        packageFiles.Add(GetPackage(item));
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
                    var possibleMatches = Packages.Match(item + (item.Contains("*") || item.Contains("-") ? "*" : "-*")).HighestPackages();

                    if (possibleMatches.Count() == 0)
                        throw new PackageNotFoundException(item);

                    if (possibleMatches.Count() == 1) {
                        packageFiles.Add(possibleMatches.First());
                    }
                    else {
                        throw new MultiplePackagesMatchException(item, possibleMatches);
                    }
                }
            }
            return packageFiles;
        }

        /// <summary>
        /// Returns a list of installed packages matching a given list of desired packages.
        /// </summary>
        /// <param name="packageNames"></param>
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

                        var feed = PackageFeed.GetPackageFeedFromLocation(item);
                        packageFiles.AddRange(feed.FindPackages("*").Where(each => each.IsInstalled));
                    }
                    else if (info.IsPackageFile) {
                        var pkg = GetPackage(item);
                        if( pkg.IsInstalled ) {
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

                    if (possibleMatches.Count() == 0)
                        throw new PackageNotFoundException(item);

                    if (possibleMatches.Count() == 1) {
                        packageFiles.Add(possibleMatches.First());
                    }
                    else {
                        // for package matching, we only support 1 match for a given unknown.
                        throw new MultiplePackagesMatchException(item, possibleMatches);
                    }
                }
            }
            return packageFiles;
        }

        // Scan Methods find what they can, empty results are OK.

        internal static IEnumerable<Package> ScanForPackages(IEnumerable<Package> packagesToScanFor) {
            return packagesToScanFor.Aggregate(Enumerable.Empty<Package>(), (current, package) => current.Union(ScanForPackages(package)).Union(ScanForPackages(package.Dependencies)));
        }
        internal static IEnumerable<Package> ScanForPackages(IEnumerable<string> packageFilters) {
            return packageFilters.Aggregate(Enumerable.Empty<Package>(), (current, filter) => current.Union(ScanForPackages(filter)));
        }
        internal static IEnumerable<Package> ScanForPackages(Package package) {
            var feeds = autoFeedLocations.Union(sessionFeedLocations).Union(systemFeedLocations);
            feeds = DoNotScanLocations.Aggregate(feeds, (current, loc) => (from feed in current where !feed.Location.IsWildcardMatch(loc) select feed));
            return feeds.Aggregate(Enumerable.Empty<Package>(), (current, feed) => current.Union(feed.FindPackages(package)));
        }
        internal static IEnumerable<Package> ScanForPackages(string packageFilter) {
            if (!packageFilter.Contains("*"))
                packageFilter += "*";

            var feeds = autoFeedLocations.Union(sessionFeedLocations).Union(systemFeedLocations);
            feeds = DoNotScanLocations.Aggregate(feeds, (current, loc) => (from feed in current where !feed.Location.IsWildcardMatch(loc) select feed));
            return feeds.Aggregate(Enumerable.Empty<Package>(), (current, feed) => current.Union(feed.FindPackages(packageFilter)));
        }

    }
}