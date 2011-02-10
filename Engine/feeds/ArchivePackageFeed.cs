//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds {
    internal class ArchivePackageFeed : PackageFeed {
        internal ArchivePackageFeed(string location)
            : base(location) {
        }

        internal override bool DownloadPackage(Package package) {
            return false;
        }

    }
}