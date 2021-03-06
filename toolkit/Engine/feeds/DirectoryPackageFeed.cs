﻿//-----------------------------------------------------------------------
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

        private string _path;
        /// <summary>
        /// the wildcard patter for matching files in this feed.
        /// </summary>
        private readonly string _filter;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryPackageFeed"/> class.
        /// </summary>
        /// <param name="location">The directory to scan.</param>
        /// <param name="patternMatch">The wildcard pattern match files agains.</param>
        /// <param name="recursive">if set to <c>true</c> if we should recursively scan folders..</param>
        /// <remarks></remarks>
        internal DirectoryPackageFeed(string location, string patternMatch ) : base(location) {
            _path = location;
            _filter = patternMatch ?? "*";
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
            if (!Scanned || Stale) {
                LastScanned = DateTime.Now;

                // GS01: BUG: recursive now should use ** in pattern match.
                var files = _path.DirectoryEnumerateFilesSmarter(_filter, false ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly /*, NewPackageManager.Instance.BlockedScanLocations*/);
                files = from file in files
                    where Recognizer.Recognize(file).Result.IsPackageFile // Since we know this to be local, it'm ok with blocking on the result.
                    select file;

                foreach (var pkg in files.Select(Package.GetPackageFromFilename).Where(pkg => pkg != null)) {
                    pkg.InternalPackageData.FeedLocation = Location;

                    if (!_packageList.Contains(pkg)) {
                        _packageList.Add(pkg);
                    }
                }
                Stale = false;
                Scanned = true;
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