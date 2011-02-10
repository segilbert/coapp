//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    internal class PackageFeed : IComparable  {
        internal string Location { get; set; }
        internal static Dictionary<string, PackageFeed> allFeeds = new Dictionary<string, PackageFeed>();

        internal Recognizer.RecognitionInfo recognitionInfo;
        private bool _scanned;

        protected bool Scanned {
            get { return _scanned; }
            set {
                if (_scanned != value) {
                    _scanned = value;
                    Registrar.Updated();
                }
            }
        }

        internal static PackageFeed GetPackageFeedFromLocation(string location) {
            PackageFeed result = null;

            var info = Recognizer.Recognize(location);
            
            if (info.IsPackageFeed) {
                if (info.IsFolder) {
                    if (allFeeds.ContainsKey(info.fullPath))
                        return allFeeds[info.fullPath];

                    result = new DirectoryPackageFeed(info.fullPath);
                }
                else if (info.IsFile) {
                    if (allFeeds.ContainsKey(info.fullPath))
                        return allFeeds[info.fullPath];

                    if (info.IsAtom) {
                        result = new AtomPackageFeed(info.fullPath);
                    }
                    if (info.IsArchive) {
                        result = new ArchivePackageFeed(info.fullPath);
                    }
                }
                // TODO: URL based feeds
            }

            if (result != null) {
                result.recognitionInfo = info;
                allFeeds.Add(result.Location, result);
            }

            return result;
        }

        protected PackageFeed(string location) {
            Location = location;
        }

        internal virtual IEnumerable<Package> FindPackages(string packageFilter) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Finds packages matching the same publisher, name, and publickeytoken
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        internal virtual IEnumerable<Package> FindPackages(Package packageFilter) {
            // this is a tad lazy. Really should do a better job in the subclass.
            return from package in FindPackages(packageFilter.Name+"*")
                   where
                       package.Name == packageFilter.Name &&
                       package.Architecture == packageFilter.Architecture &&
                       package.PublicKeyToken == packageFilter.PublicKeyToken
                   select package;
        }

        internal virtual bool DownloadPackage(Package package) {
            return false;
        }

        public int CompareTo(object obj) {
            if (recognitionInfo.IsURL == (obj as PackageFeed).recognitionInfo.IsURL)
                return 0;

            if (recognitionInfo.IsURL)
                return 1;
          
            return -1;
        }
    }
}