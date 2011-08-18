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


    /// <summary>
    /// Extension methods to make queries on package sets easier.
    /// </summary>
    /// <remarks></remarks>
    public static class PackageCollectionExtensions {

        /// <summary>
        /// Adds the feed location.
        /// </summary>
        /// <param name="feedCollection">The feed collection.</param>
        /// <param name="feedLocation">The feed location.</param>
        /// <returns></returns>
        /// <remarks>
        /// NOTE: This is probably gettin' refactored PFQ
        /// </remarks>
        internal static Task AddFeedLocation(this ObservableCollection<PackageFeed> feedCollection, string feedLocation) {
            
            if ((from feed in feedCollection
                 where feed.Location.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase)
                 select feed).Count() == 0) {
                return PackageFeed.GetPackageFeedFromLocation(feedLocation).ContinueWith(antecedent => {
                    if (antecedent.Result != null) {
                        if (
                            (from feed in feedCollection
                                where feed.Location.Equals(feedLocation, StringComparison.CurrentCultureIgnoreCase)
                                select feed).Count() == 0) {
                            feedCollection.Add(antecedent.Result);
                        }
                    }
                }, TaskContinuationOptions.AttachedToParent );
            }
            return Task.Factory.StartNew(() => { });
        }

        /// <summary>
        /// Gets the feed locations.
        /// </summary>
        /// <param name="feedCollection">The feed collection.</param>
        /// <returns></returns>
        /// <remarks>
        /// NOTE: This is probably gettin' refactored PFQ
        /// </remarks>
        internal static IEnumerable<string> GetFeedLocations(this ObservableCollection<PackageFeed> feedCollection) {
            return from feed in feedCollection select feed.Location;
        }

        /// <summary>
        /// finds packages that match the given wildcard-mask
        /// </summary>
        /// <param name="packageSet">The package set to search thru.</param>
        /// <param name="wildcardMatch">The cosmetic name to match against.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static IEnumerable<Package> Match(this IEnumerable<Package> packageSet, string wildcardMatch) {
            return from p in packageSet where p.CosmeticName.IsWildcardMatch(wildcardMatch) select p;
        }

        /// <summary>
        /// This gets the highest version of all the packages in the set.
        /// </summary>
        /// <param name="packageSet">The package set.</param>
        /// <returns>the filtered colleciton of packages</returns>
        /// <remarks></remarks>
        internal static IEnumerable<Package> HighestPackages(this IEnumerable<Package> packageSet ) {
            if (packageSet.Count() > 1) {
                var names = (from p in packageSet group p by new { p.Name, p.Architecture });
                return names.Select(each => (from package in packageSet
                                             from uniq in names
                                             where package.Name == each.Key.Name && package.Architecture == each.Key.Architecture
                                             select package).OrderByDescending(p => p.Version).FirstOrDefault());
            }
            return packageSet;
        }
    }
}
