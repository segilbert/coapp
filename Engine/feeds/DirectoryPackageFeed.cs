//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;

    /// <summary>
    /// Creates a package feed from a local filesystem directory.
    /// </summary>
    /// <remarks></remarks>
    internal class DirectoryPackageFeed : PackageFeed {
        /// <summary>
        /// contains the list of packages in the direcory. (may be recursive)
        /// </summary>
        private readonly List<Package> _packageList = new List<Package>();
        
        /// <summary>
        /// the wildcard patter for matching files in this feed.
        /// </summary>
        private readonly string _patternMatch;
        
        /// <summary>
        /// flag to see if this feed should recursively scan child folders.
        /// </summary>
        private readonly bool _recursive;


        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryPackageFeed"/> class.
        /// </summary>
        /// <param name="location">The directory to scan.</param>
        /// <param name="patternMatch">The wildcard pattern match files agains.</param>
        /// <param name="recursive">if set to <c>true</c> if we should recursively scan folders..</param>
        /// <remarks></remarks>
        internal DirectoryPackageFeed(string location, string patternMatch, bool recursive = false) : base(location) {
            _patternMatch = patternMatch ?? "*";
            _recursive = recursive;
        }


        /// <summary>
        /// Scans the directory for all packages that match the wildcard.
        /// 
        /// For each file found, it will ask the recognizer to identify if the file is a package (any kind of package)
        /// 
        /// This will only scan the directory if the Scanned property is false.
        /// </summary>
        /// <remarks>
        /// NOTE: Some of this may get refactored to change behavior before the end of the beta2.
        /// </remarks>
        protected void Scan() {
            if (!Scanned) {
                LastScanned = DateTime.Now;
                var files = Location.DirectoryEnumerateFilesSmarter(_patternMatch, _recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, NewPackageManager.Instance.BlockedScanLocations);
                files = from file in files
                    where Recognizer.Recognize(file).Result.IsPackageFile // Since we know this to be local, it'm ok with blocking on the result.
                    select file;

                foreach (var p in files) {
                    try {
                        var pkg = NewPackageManager.Instance.GetPackageFromFilename(p);
                        pkg.InternalPackageData.FeedLocation.Add(Location);
                        
                        if( !_packageList.Contains(pkg))
                            _packageList.Add(pkg);
                    }
                    catch (InvalidPackageException) {
                        // that's ok, it's been skipped.
                        // Console.WriteLine("IPE:{0}",p);
                    }
                    catch (PackageNotFoundException) {
                        // this might not happen anymore.
                        // that's a bit odd, but it's been skipped.
                        // Console.WriteLine("PNF:{0}", p);
                    }
                }
                Scanned = true;
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
        internal override IEnumerable<Package> FindPackages(string packageFilter) {
            Scan();
            return from package in _packageList where package.CosmeticName.IsWildcardMatch(packageFilter) select package;
        }

        /// <summary>
        /// Finds packages matching the same publisher, name, and publickeytoken
        /// </summary>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns>Returns a collection of packages that match</returns>
        /// <remarks></remarks>
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