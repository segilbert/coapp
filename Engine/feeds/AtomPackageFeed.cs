//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds {
    using System.Linq;

    internal class AtomPackageFeed : PackageFeed {
        internal AtomPackageFeed(string location)
            : base(location) {
            
        }

        internal override bool DownloadPackage(Package package) {
            return false;
        }

        internal override System.Collections.Generic.IEnumerable<Package> FindPackages(Package packageFilter) {
            return Enumerable.Empty<Package>();
        }

        internal override System.Collections.Generic.IEnumerable<Package> FindPackages(string packageFilter) {
            return Enumerable.Empty<Package>();
        }

    }
}