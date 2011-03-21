//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------l

namespace CoApp.Toolkit.Engine.Feeds {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Tasks;

    internal class PackageFeed : IComparable {
        internal static Dictionary<string, PackageFeed> AllFeeds = new Dictionary<string, PackageFeed>();

        private bool _scanned;
        internal Recognizer.RecognitionInfo RecognitionInfo;
        internal string Location { get; set; }

        protected bool Scanned {
            get { return _scanned; }
            set {
                if (_scanned != value) {
                    _scanned = value;
                    Registrar.Updated();
                }
            }
        }

        protected PackageFeed(string location) {
            Location = location;
        }

        #region IComparable Members

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

        internal static Task<PackageFeed> GetPackageFeedFromLocation(string location, bool recursive = false) {
            return Recognizer.Recognize(location, ensureLocal: true).ContinueWithParent(antecedent => {
                var info = antecedent.Result;
                PackageFeed result = null;

                string locationKey = null;

                if (info.IsPackageFeed) {
                    if (info.IsFolder) {
                        locationKey = Path.Combine(info.FullPath, info.Wildcard ?? "*");

                        if (AllFeeds.ContainsKey(locationKey)) {
                            return AllFeeds[locationKey];
                        }

                        result = new DirectoryPackageFeed(info.FullPath, info.Wildcard, recursive);
                    }
                    else if (info.IsFile) {
                        if (AllFeeds.ContainsKey(info.FullPath)) {
                            return AllFeeds[info.FullPath];
                        }

                        if (info.IsAtom) {
                            result = new AtomPackageFeed(info.FullPath);
                        }
                        if (info.IsArchive) {
                            result = new ArchivePackageFeed(info.FullPath);
                        }
                    }
                        // TODO: URL based feeds
                    else if (info.IsURL) {
                        if (AllFeeds.ContainsKey(info.FullUrl.AbsoluteUri)) {
                            return AllFeeds[info.FullUrl.AbsoluteUri];
                        }

                        if (info.IsAtom) {
                            result = new AtomPackageFeed(info.FullUrl);
                        }
                    }
                }

                if (result != null) {
                    result.RecognitionInfo = info;
                    lock (AllFeeds) {
                        if (!AllFeeds.ContainsKey(locationKey ?? result.Location)) {
                            AllFeeds.Add(locationKey ?? result.Location, result);
                        }
                        else
                            result = AllFeeds[locationKey ?? result.Location];
                        // GS01: TODO: This is a crappy way of avoiding a deadlock when the same feed has been requested twice by two different threads.
                    }
                }

                return result;
            });
        }

        internal virtual IEnumerable<Package> FindPackages(string packageFilter) {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Finds packages matching the same publisher, name, and publickeytoken
        /// </summary>
        /// <param name = "packageFilter"></param>
        /// <returns></returns>
        internal virtual IEnumerable<Package> FindPackages(Package packageFilter) {
            // this is a tad lazy. Really should do a better job in the subclass.
            return from package in FindPackages(packageFilter.Name + "*")
                where
                    package.Name == packageFilter.Name &&
                        package.Architecture == packageFilter.Architecture &&
                            package.PublicKeyToken == packageFilter.PublicKeyToken
                select package;
        }

        internal virtual bool DownloadPackage(Package package) {
            return false;
        }
    }
}