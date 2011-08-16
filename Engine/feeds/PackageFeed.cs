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
    using System.Threading.Tasks;
    using Tasks;

    /// <summary>
    /// The common implementation of the features for a package feed.
    /// </summary>
    /// <remarks></remarks>
    internal class PackageFeed : IComparable {
        /// <summary>
        /// The collection of all the feeds known to the system at the current time.
        /// 
        /// This indexes feeds based on the location string used to create the feed object.
        /// </summary>
        private static readonly Dictionary<string, PackageFeed> _allFeeds = new Dictionary<string, PackageFeed>();

        /// <summary>
        /// indicates if the current feed has already scanned the contents
        /// 
        /// How this is used is up to the child class.
        /// </summary>
        private bool _scanned;

        /// <summary>
        /// What is known about the feed's location (url, file, type of file, etc)
        /// </summary>
        internal Recognizer.RecognitionInfo RecognitionInfo;

        /// <summary>
        /// 
        /// Gets or sets the location.
        /// 
        /// This can be a file, a directory or a URL.
        /// </summary>
        /// <value>The location of the feed.</value>
        /// <remarks></remarks>
        internal readonly string Location;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PackageFeed"/> is scanned.
        /// </summary>
        /// <value><c>true</c> if scanned; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        internal bool Scanned { 
            get { return _scanned; }
            set {
                if (_scanned != value) {
                    _scanned = value;
                    NewPackageManager.Instance.Updated();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageFeed"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <remarks></remarks>
        protected PackageFeed(string location) {
            Location = location;
        }

        #region IComparable Members

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj"/>. Zero This instance is equal to <paramref name="obj"/>. Greater than zero This instance is greater than <paramref name="obj"/>.</returns>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="obj"/> is not the same type as this instance. </exception>
        /// <remarks></remarks>
        public int CompareTo(object obj) {
            if (RecognitionInfo.IsURL == ((PackageFeed) obj).RecognitionInfo.IsURL) {
                return 0;
            }

            if (RecognitionInfo.IsURL) {
                return 1;
            }

            return -1;
        }

        #endregion

        /// <summary>
        /// Gets the package feed from the location. 
        /// 
        /// This will first attempt to look up a matching instance in the AllFeeds collection (so that multiple requests for the same feed return a single object)
        /// 
        /// It asks the recognizer to identify the location, and creates a specific subclass instance based on the results.
        /// 
        /// If it cannot identify or read the target, the task will return null.
        /// </summary>
        /// <param name="location">The feed location (url, file, directory).</param>
        /// <param name="recursive">if set to <c>true</c> the subclass can be told to recursively scan (ie, in a directory feed).</param>
        /// <returns>A Task with a return value of the PackageFeed. May be null if invalid.</returns>
        /// <remarks></remarks>
        internal static Task<PackageFeed> GetPackageFeedFromLocation(string location, bool recursive = false) {
            return Recognizer.Recognize(location, ensureLocal: true).ContinueWith(antecedent => {
                var info = antecedent.Result;
                PackageFeed result = null;

                string locationKey = null;

                if (info.IsPackageFeed) {
                    if (info.IsFolder) {
                        locationKey = Path.Combine(info.FullPath, info.Wildcard ?? "*");

                        if (_allFeeds.ContainsKey(locationKey)) {
                            return _allFeeds[locationKey];
                        }

                        result = new DirectoryPackageFeed(info.FullPath, info.Wildcard, recursive);
                    }
                    else if (info.IsFile) {
                        if (_allFeeds.ContainsKey(info.FullPath)) {
                            return _allFeeds[info.FullPath];
                        }
                        /*
                        if (info.IsAtom) {
                            result = new AtomPackageFeed(info.FullPath);
                        }
                        if (info.IsArchive) {
                            result = new ArchivePackageFeed(info.FullPath);
                        }
                         * */
                    }
                        // TODO: URL based feeds
                    else if (info.IsURL) {
                        if (_allFeeds.ContainsKey(info.FullUrl.AbsoluteUri)) {
                            return _allFeeds[info.FullUrl.AbsoluteUri];
                        }

                        /*
                        if (info.IsAtom) {
                            result = new AtomPackageFeed(info.FullUrl);
                        }
                         */
                    }
                }

                if (result != null) {
                    result.RecognitionInfo = info;
                    lock (_allFeeds) {
                        if (!_allFeeds.ContainsKey(locationKey ?? result.Location)) {
                            _allFeeds.Add(locationKey ?? result.Location, result);
                        }
                        else {
                            result = _allFeeds[locationKey ?? result.Location];
                        }
                        // GS01: TODO: This is a crappy way of avoiding a deadlock when the same feed has been requested twice by two different threads.
                    }
                }

                return result;
            }, TaskContinuationOptions.AttachedToParent ).AutoManage();
        }

        /// <summary>
        /// Finds the packages.
        /// </summary>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal virtual IEnumerable<Package> FindPackages(string packageFilter) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Finds packages matching the same publisher, name, and publickeytoken
        /// </summary>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns>Returns a collection of packages that match </returns>
        /// <remarks>
        /// This is a tad lazy. Really should do a better job in the subclass.
        /// </remarks>
        internal virtual IEnumerable<Package> FindPackages(Package packageFilter) {
            return from package in FindPackages(packageFilter.Name + "*")
                where
                    package.Name == packageFilter.Name && package.Architecture == packageFilter.Architecture &&
                        package.PublicKeyToken == packageFilter.PublicKeyToken
                select package;
        }

        internal DateTime LastScanned = DateTime.FromFileTime(0);
    }
}