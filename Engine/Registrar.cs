//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;

    public class Registrar {
        private static readonly ObservableCollection<Package> packages = new ObservableCollection<Package>();
        private static readonly ObservableCollection<string> DiscoveredScanLocations = new ObservableCollection<string>();
        private static bool _hasScannedAtLeastOnce;

        internal static IEnumerable<string> DoNotScanLocations = Enumerable.Empty<string>();
        internal static IEnumerable<string> AdditionalScanLocations = Enumerable.Empty<string>();
        internal static IEnumerable<string> AdditionalRecursiveScanLocations = Enumerable.Empty<string>();

        internal static bool HasScannedAtLeastOnce {
            get { return _hasScannedAtLeastOnce; }
            set {
                if (_hasScannedAtLeastOnce != value) {
                    _hasScannedAtLeastOnce = value;
                    Updated(); 
                }
            }
        }

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

            if (!File.Exists(localPackagePath)) {
                throw new PackageNotFoundException(localPackagePath);
            }

            var pkgDetails = Package.GetCoAppPackageFileDetails(packagePath);

            pkg = (packages.Where(package =>
                package.Architecture == pkgDetails.Architecture &&
                    package.Version == pkgDetails.Version &&
                        package.PublicKeyToken == pkgDetails.PublicKeyToken &&
                            package.Name.Equals(pkgDetails.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();

            if (pkg == null) {
                pkg = new Package(pkgDetails.Name, pkgDetails.Architecture, pkgDetails.Version, pkgDetails.PublicKeyToken,
                    pkgDetails.packageId);

                pkg.Dependencies.Clear();
                pkg.Dependencies.AddRange((IEnumerable<Package>)pkgDetails.dependencies);

                packages.Add(pkg);
            }
            if (pkg.LocalPackagePath == null) {
                pkg.LocalPackagePath = localPackagePath;
            }

            pkg.PolicyMinimumVersion = pkgDetails.policy_min_version;
            pkg.PolicyMaximumVersion = pkgDetails.policy_max_version;

            return pkg;
        }

        public static IEnumerable<Package> LocateSupercedentPackages(Package package) {
            // anything superceedent in the list of known packages?
            return packages.Where(p => p.Architecture == package.Architecture &&
                p.PublicKeyToken == package.PublicKeyToken &&
                    p.Name.Equals(package.Name, StringComparison.CurrentCultureIgnoreCase) &&
                        p.PolicyMinimumVersion <= package.Version &&
                            p.PolicyMaximumVersion >= package.Version).OrderByDescending(p => p.Version);
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

        private static string trimto(string s, int sz) {
            return s.Length < sz ? s : s.Substring(s.Length - sz);
        }

        public static void DumpPackages(IEnumerable<Package> pkgs) {
            string fmt = "|{0,35}|{1,20}|{2,5}|{3,20}|{4,8}|{5,20}|";
            string line = "--------------------------------------------------------";
            Console.WriteLine(fmt, "Filename", "Name", "Arch", "Version", "Key", "GUID");
            Console.WriteLine(fmt, trimto(line, 35), trimto(line, 20), trimto(line, 5), trimto(line, 20), trimto(line, 8), trimto(line, 20));

            foreach (var p in pkgs) {
                Console.WriteLine(fmt, trimto(p.LocalPackagePath ?? "(unknown)", 35), trimto(p.Name, 20), p.Architecture,
                    p.Version.UInt64VersiontoString(), trimto(p.PublicKeyToken, 8), trimto(p.ProductCode, 20));
            }
            Console.WriteLine(fmt, trimto(line, 35), trimto(line, 20), trimto(line, 5), trimto(line, 20), trimto(line, 8), trimto(line, 20));
            Console.WriteLine("\r\n");
        }
    }
}