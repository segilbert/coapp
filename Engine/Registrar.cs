//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;

    public class Registrar {
        private static HashSet<long> nonCoAppMSIFiles = new HashSet<long>();
        private static bool _readCache;

        private static readonly ObservableCollection<Package> packages = new ObservableCollection<Package>();
        private static readonly ObservableCollection<string> DiscoveredScanLocations = new ObservableCollection<string>();
        private static bool _hasScannedAtLeastOnce;

        internal static IEnumerable<string> DoNotScanLocations = Enumerable.Empty<string>();
        internal static IEnumerable<string> AdditionalScanLocations = Enumerable.Empty<string>();
        internal static IEnumerable<string> AdditionalRecursiveScanLocations = Enumerable.Empty<string>();

        internal static bool HasScannedAtLeastOnce {
            get { return _hasScannedAtLeastOnce; }
            set {
                if (_hasScannedAtLeastOnce == value) return;
                _hasScannedAtLeastOnce = value;
                Updated();
            }
        }

        public static void FlushCache() {
            PackageManagerSettings.systemSettings["nonCoAppPackageMap"] = null;
            nonCoAppMSIFiles.Clear();
        }

        public static void SaveCache() {
            using (var ms = new MemoryStream()) {

                var binaryWriter = new BinaryWriter(ms);

                // order of the following is very important.
                binaryWriter.Write(nonCoAppMSIFiles.Count);
                foreach (var val in nonCoAppMSIFiles)
                    binaryWriter.Write(val);

                PackageManagerSettings.systemSettings["nonCoAppPackageMap"] = ms.GetBuffer();
            }
        }

        public static void LoadCache() {
            if (!_readCache) {
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
                _readCache = true;
            }
        }

        public static IEnumerable<Package> Packages { get { return packages;  } }

        public static int StateCounter;
        public static void Updated() {
            StateCounter++;
        }
        static Registrar() {
            packages.CollectionChanged += (x, y) => Updated();
            DiscoveredScanLocations.CollectionChanged += (x, y) => Updated();
        }

        public static Package GetPackage(string packageName, string architecture, UInt64 version, string publicKeyToken, string packageId) {
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

        public static Package GetPackage(string packagePath) {
            if( packagePath.Contains("*") ) {
                throw new PackageNotFoundException(packagePath);
            }

            var localPackagePath = Path.GetFullPath(packagePath);

            var localFolder = Path.GetDirectoryName(localPackagePath).ToLower();
            if (!DiscoveredScanLocations.Contains(localFolder)) {
                DiscoveredScanLocations.Add(localFolder);
            }

            var pkg =
                (packages.Where(
                    package =>
                        !string.IsNullOrEmpty(package.LocalPackagePath) &&
                            package.LocalPackagePath.Equals(localPackagePath, StringComparison.CurrentCultureIgnoreCase))).
                    FirstOrDefault();

            if (pkg != null) {
                return pkg;
            }

            if (!File.Exists(localPackagePath) ) {
                // could this be another representation of a package?
                if( !localPackagePath.EndsWith(".msi") ) {
                    return GetPackage(localPackagePath + ".msi");
                }

                throw new PackageNotFoundException(localPackagePath);
            }

            dynamic pkgDetails;
            var lookup = File.GetCreationTime(localPackagePath).Ticks+localPackagePath.GetHashCode();
            if( nonCoAppMSIFiles.Contains(lookup)) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }
            

            try {
                pkgDetails = Package.GetCoAppPackageFileDetails(localPackagePath);
            } catch (InvalidPackageException ipe) {
                if( ipe.Reason == InvalidReason.NotCoAppMSI) {
                    nonCoAppMSIFiles.Add(lookup);
                }
                throw;
            }
            
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
            pkg.Dependencies.Clear();
            pkg.Dependencies.AddRange((IEnumerable<Package>)pkgDetails.dependencies);

            if (pkg.LocalPackagePath == null) {
                pkg.LocalPackagePath = localPackagePath;
            }

            pkg.Assemblies.AddRange((IEnumerable<PackageAssemblyInfo>)pkgDetails.assemblies.Values);

            pkg.Roles.AddRange((IEnumerable<Tuple<string,string>>)pkgDetails.roles);

            pkg.PolicyMinimumVersion = pkgDetails.policy_min_version;
            pkg.PolicyMaximumVersion = pkgDetails.policy_max_version;

            return pkg;
        }

        public static void ScanForPackages() {
            // only handles file locations right now.

            var msiFiles = AdditionalScanLocations.Union(DiscoveredScanLocations).AsParallel().SelectMany(
                directory => directory.DirectoryEnumerateFilesSmarter("*.msi", SearchOption.TopDirectoryOnly, DoNotScanLocations))
                .Union(
                    AdditionalRecursiveScanLocations.AsParallel().SelectMany(
                        directory => directory.DirectoryEnumerateFilesSmarter("*.msi", SearchOption.AllDirectories, DoNotScanLocations)));

            foreach (var p in msiFiles) {
                try {
                    GetPackage(p);
                }
                catch (InvalidPackageException ipe) {
                    // that's ok, it's been skipped.
                    // Console.WriteLine("IPE:{0}",p);
                }
                catch (PackageNotFoundException pnf) {
                    // that's a bit odd, but it's been skipped.
                    // Console.WriteLine("PNF:{0}", p);
                }
            }
            HasScannedAtLeastOnce = true;
        }

       
    }
}