using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Feeds {
    using System.IO;
    using Extensions;
    using PackageFormatHandlers;

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

        protected void Scan() {
            if (!Scanned) {
                LastScanned = DateTime.Now;

                var installedFiles = MSIBase.InstalledMSIFilenames.ToArray();

                for (var i = 0; i < installedFiles.Length;i++ ) {
                    var packageFilename = installedFiles[i];

                    PackageManagerMessages.Invoke.ScanningPackagesProgress(packageFilename,  ((i) * 100) / installedFiles.Length);

                    var lookup = File.GetCreationTime(packageFilename).Ticks + packageFilename.GetHashCode();

                    if( _nonCoAppMSIFiles.Contains(lookup) ) {
                        // already identified as a not-coapp-package.
                        continue;
                    }

                    try {
                        _packageList.Add( NewPackageManager.Instance.GetPackageFromFilename(packageFilename) );
                    } catch(Exception e) {
                        // Console.WriteLine(e.Message);
                        // files that fail aren't coapp packages. 
                        // remember that one for next time.
                        _nonCoAppMSIFiles.Add(lookup);
                    }
                }
                
                SaveCache();

                Scanned = true;
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
        /// <param name="packageFilter">The package filter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal override IEnumerable<Package> FindPackages(string name, string version, string arch, string publicKeyToken) { 
            Scan();
            return from p in _packageList where 
                ( string.IsNullOrEmpty(name) || name.IsWildcardMatch(p.Name) )  && 
                ( string.IsNullOrEmpty(version) || version.IsWildcardMatch(p.Version.UInt64VersiontoString()) )  && 
                ( string.IsNullOrEmpty(arch) || arch.IsWildcardMatch(p.Architecture) )  && 
                ( string.IsNullOrEmpty(publicKeyToken) || publicKeyToken.IsWildcardMatch(p.PublicKeyToken) )  select p;
        }
    }
}
