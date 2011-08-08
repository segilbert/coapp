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

        public static IEnumerable<Package> AllVersionsOfPackage(this IEnumerable<Package> packageSet, Package package)
        {
            return from p in packageSet
                   where p.Architecture == package.Architecture &&
                         p.PublicKeyToken == package.PublicKeyToken &&
                         p.Name.Equals(package.Name, StringComparison.CurrentCultureIgnoreCase)
                   select p;
        }

        public static IEnumerable<IGrouping<Package, Package>> LatestSupercedentPackages(this IEnumerable<Package> packageSet, Package package)
        {
            var tempPkgs = packageSet.AllVersionsOfPackage(package).OrderByDescending((p) => p.Version);
            PrivateGroup currentGroup = null;
            foreach (var p in tempPkgs)
            {
                if (currentGroup == null)
                {
                    currentGroup = new PrivateGroup {Key = p};
                    continue;
                }
                if (currentGroup.Key.Supercedes(p))
                {
                    currentGroup.Add(p);
                }
                else
                {
                    yield return currentGroup;
                    currentGroup = new PrivateGroup {Key = p};
                }

            }

            yield break;
        }


        private class PrivateGroup : IGrouping<Package, Package>
        {
            private readonly IList<Package> _pkgs = new List<Package>();
            private Package _key;
            public Package Key { 
                get { return _key; }
                set { _key = value;
                Add(_key);} 
            }

            public IEnumerator<Package> GetEnumerator()
            {
                return _pkgs.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public void Add(Package pkg)
            {
                _pkgs.Add(pkg);
            }
        }
    }
}
