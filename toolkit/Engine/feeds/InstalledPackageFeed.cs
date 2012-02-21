using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Feeds {
    using System.IO;
    using Extensions;
    using PackageFormatHandlers;
    using Win32;

    internal class InstalledPackageFeed : PackageFeed {
        internal static string CanonicalLocation = "CoApp://InstalledPackages";
        internal static InstalledPackageFeed Instance = new InstalledPackageFeed();

        private readonly HashSet<long> _nonCoAppMSIFiles = new HashSet<long>();

        /// <summary>
        /// contains the list of packages in the direcory. (may be recursive)
        /// </summary>
        private readonly List<Package> _packageList = new List<Package>();

        private InstalledPackageFeed() : base(CanonicalLocation) {
            LoadCache();
        }

        internal void PackageRemoved( Package package ) {
            lock (this) {
                if (_packageList.Contains(package)) {
                    _packageList.Remove(package);
                }
            }
        }

        internal void PackageInstalled(Package package) {
            lock (this) {
                if (!_packageList.Contains(package)) {
                    _packageList.Add(package);
                }
            }
        }

        public int Progress { get; set; }

        protected void Scan() {
            if (!Scanned || Stale) {
                LastScanned = DateTime.Now;

                // add the cached package directory, 'cause on backlevel platform, they taint the MSI in the installed files folder.
                var installedFiles = MSIBase.InstalledMSIFilenames.Union(PackageManagerSettings.CoAppPackageCache.FindFilesSmarter("*.msi")).ToArray();
                
                for (var i = 0; i < installedFiles.Length;i++ ) {
                    var packageFilename = installedFiles[i];

                    Progress = (i*100)/installedFiles.Length;
                    PackageManagerMessages.Invoke.ScanningPackagesProgress(packageFilename,  Progress);

                    var lookup = File.GetCreationTime(packageFilename).Ticks + packageFilename.GetHashCode();

                    if( _nonCoAppMSIFiles.Contains(lookup) ) {
                        // already identified as a not-coapp-package.
                        continue;
                    }

                    var pkg = Package.GetPackageFromFilename(packageFilename);

                    if (pkg != null) { 
                        if (pkg.IsInstalled) {
                            _packageList.Add(pkg);
                        }
                    } else {
                        _nonCoAppMSIFiles.Add(lookup);
                    }
                }
                
                SaveCache();
                Progress = 100;
                Scanned = true;
                Stale = false;
            }
        }

        private void LoadCache() {
            _nonCoAppMSIFiles.Clear();

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
        }

        private void SaveCache() {
            using (var ms = new MemoryStream()) {
                var binaryWriter = new BinaryWriter(ms);

                // order of the following is very important.
                binaryWriter.Write(_nonCoAppMSIFiles.Count);
                foreach (var val in _nonCoAppMSIFiles) {
                    binaryWriter.Write(val);
                }

                PackageManagerSettings.CacheSettings["#nonCoAppPackageMap"].BinaryValue = ms.GetBuffer();
            }
        }

        /// <summary>
        /// Finds packages based on the cosmetic name of the package.
        /// 
        /// Supports wildcard in pattern match.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="arch"></param>
        /// <param name="publicKeyToken"></param>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal override IEnumerable<Package> FindPackages(string name, string version, string arch, string publicKeyToken) { 
            Scan();
            return from p in _packageList where
                (string.IsNullOrEmpty(name) || p.Name.IsWildcardMatch(name)) &&
                (string.IsNullOrEmpty(version) || p.Version.ToString().IsWildcardMatch(version)) &&
                (string.IsNullOrEmpty(arch) || p.Architecture.ToString().IsWildcardMatch(arch)) &&
                (string.IsNullOrEmpty(publicKeyToken) || p.PublicKeyToken.IsWildcardMatch(publicKeyToken)) select p;
        }
    }
}
