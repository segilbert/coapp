//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;

    internal class DirectoryPackageFeed : PackageFeed {
        private readonly List<Package> _packageList = new List<Package>();
        private readonly string _patternMatch;
        private readonly bool _recursive;


        internal DirectoryPackageFeed(string location, string patternMatch, bool recursive = false) : base(location) {
            _patternMatch = patternMatch ?? "*";
            _recursive = recursive;
        }

        protected void Scan() {
            if (!Scanned) {
                var files = Location.DirectoryEnumerateFilesSmarter(_patternMatch, _recursive ? SearchOption.AllDirectories: SearchOption.TopDirectoryOnly, Registrar.DoNotScanLocations);
                files = from file in files
                    where Recognizer.Recognize(file).IsPackageFile
                    select file;

                foreach (var p in files) {
                    try {
                        var pkg = Registrar.GetPackage(p);
                        if( !_packageList.Contains(pkg))
                            _packageList.Add(pkg);
                    }
                    catch (InvalidPackageException) {
                        // that's ok, it's been skipped.
                        // Console.WriteLine("IPE:{0}",p);
                    }
                    catch (PackageNotFoundException) {
                        // that's a bit odd, but it's been skipped.
                        // Console.WriteLine("PNF:{0}", p);
                    }
                }
                Scanned = true;
            }
        }

        internal override bool DownloadPackage(Package package) {
            return package.HasLocalFile;
        }

        internal override IEnumerable<Package> FindPackages(string packageFilter) {
            Scan();
            return from package in _packageList where package.CosmeticName.IsWildcardMatch(packageFilter) select package;
        }

        internal override IEnumerable<Package> FindPackages(Package packageFilter) {
            Scan();
            return from package in _packageList
                   where
                       package.Name == packageFilter.Name &&
                       package.Architecture == packageFilter.Architecture &&
                       package.PublicKeyToken == packageFilter.PublicKeyToken
                   select package;
        }
    }
}