//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Exceptions;
    using Extensions;
    using Feeds;
    using Tasks;

    public static class PackageExtensions {

        internal static Task AddFeedLocation(this ObservableCollection<PackageFeed> feedCollection, string feedLocation) {
            
            if ((from feed in feedCollection
                 where feed.Location.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase)
                 select feed).Count() == 0) {
                return PackageFeed.GetPackageFeedFromLocation(feedLocation).ContinueWithParent(antecedent => {
                    if (antecedent.Result != null) {
                        if (
                            (from feed in feedCollection
                                where feed.Location.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase)
                                select feed).Count() == 0) {
                            feedCollection.Add(antecedent.Result);
                        }
                    }
                });
            }
            return CoTaskFactory.CompletedTask;
        }

        internal static IEnumerable<string> GetFeedLocations(this ObservableCollection<PackageFeed> feedCollection) {
            return from feed in feedCollection select feed.Location;
        }

        public static IEnumerable<Package> Match(this IEnumerable<Package> packageSet, string wildcardMatch) {
            return from p in packageSet where p.CosmeticName.IsWildcardMatch(wildcardMatch) select p;
        }

        public static IEnumerable<Package> HighestPackages(this IEnumerable<Package> packageSet ) {
            if (packageSet.Count() > 1) {
                var names = (from p in packageSet group p by new { p.Name, p.Architecture });
                return names.Select(each => (from package in packageSet
                                             from uniq in names
                                             where package.Name == each.Key.Name && package.Architecture == each.Key.Architecture
                                             select package).OrderByDescending(p => p.Version).FirstOrDefault());
            }
            return packageSet;
        }

        public static IEnumerable<Package> SupercedentPackages(this IEnumerable<Package> packageSet, Package package) {
            // DebugMessage.Invoke.WriteLine(string.Format( "Scanning for package [{0}]", package.Name.SingleItemAsEnumerable()));
            // anything superceedent in the list of known packages?
            Registrar.ScanForPackages(package);

            return packageSet.Where(p => p.Architecture == package.Architecture &&
                p.PublicKeyToken == package.PublicKeyToken &&
                p.Name.Equals(package.Name, StringComparison.CurrentCultureIgnoreCase) &&
                p.PolicyMinimumVersion <= package.Version &&
                p.PolicyMaximumVersion >= package.Version).OrderByDescending(p => p.Version);
        }
    }
}
