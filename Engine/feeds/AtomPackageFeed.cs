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
    using System.Net;
    using System.ServiceModel.Syndication;
    using System.Threading.Tasks;
    using Atom;
    using Extensions;
    using Network;
    using Tasks;

    /// <summary>
    /// A package feed represented by a atom XML file
    /// 
    /// This may be a remote URL or a local xml file.
    /// </summary>
    /// <remarks></remarks>
    internal class AtomPackageFeed : PackageFeed {
        /// <summary>
        /// the remote location this feed is coming from. 
        /// 
        /// May be null if this file is local to start with
        /// </summary>
        private readonly Uri _remoteLocation;

        /// <summary>
        /// the local file on disk that contains the xml feed.
        /// </summary>
        private string _localLocation;

        /// <summary>
        /// the collection of packages found in this feed.
        /// </summary>
        private readonly List<Package> _packageList = new List<Package>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageFeed"/> class.
        /// </summary>
        /// <param name="location">The local file location of the package feed.
        /// </param>
        /// <remarks></remarks>
        internal AtomPackageFeed(string location) : base(location) {
             _localLocation = location.CanonicalizePathIfLocalAndExists();
            if( string.IsNullOrEmpty(_localLocation) ) {
                // perhaps this is a remote file
                try {
                    var uri = new Uri(location);
                    _remoteLocation = uri;
                } catch {
                    throw new Exception("Invalid Atom Feed Location");
                }
            } 
        }
  
        /// <summary>
        /// Initializes a new instance of the <see cref="AtomPackageFeed"/> class.
        /// 
        /// From a remote URI location
        /// </summary>
        /// <param name="location">The URL of the remote package feed..</param>
        internal AtomPackageFeed(Uri location) : this(location.AbsoluteUri) {
        }

        internal static PackageDetails ReadPackageDetails(Uri remoteLocation, string localLocation, Package package) {
            return null;
        }

        private Task<bool> EnsureFileIsLocal() {
            if (_localLocation.FileIsLocalAndExists())
                return true.AsResultTask();

            if( _remoteLocation != null ) {
                // do the remote thing (force the recognizer to acquire and load the file)
                return Recognizer.Recognize(_remoteLocation.AbsoluteUri).ContinueWith(antecedent => {
                    if( antecedent.IsFaulted || antecedent.IsCanceled ) {
                        return false;
                    }
                    if( antecedent.Result != null && antecedent.Result.IsAtom ) {
                        _localLocation = antecedent.Result.FullPath;
                        return true;
                    }
                    return false;
                }, TaskContinuationOptions.AttachedToParent);
            }

            return false.AsResultTask();
        }

        /// <summary>
        /// Iterates thru the list of atom feed items and creates package representations of each item.
        /// </summary>
        /// <remarks></remarks>
        protected void Scan() {
            if (!Scanned) {
                // bring the file local first
                EnsureFileIsLocal().ContinueWith(antecedent => {
                    if (antecedent.IsFaulted || antecedent.IsCanceled || !antecedent.Result) {              
                        Scanned = false;
                        LastScanned = DateTime.MinValue;
                        return false;
                    }

                    // we're good to load the file from the _localLocation
                    var feed = AtomFeed.Load(_localLocation);

                    var all = from item in feed.Items
                        let entry = item as AtomItem
                        let pkgElement = entry != null ? entry.packageElement : null
                        where entry != null
                        select new {
                            entry,
                            package = Package.GetPackage(pkgElement.Name, pkgElement.Version, pkgElement.Architecture, pkgElement.PublicKeyToken, pkgElement.Id),
                        };

                    foreach (var each in all) {
                        var item = each.entry;
                        var pkgElement = each.entry.packageElement;
                        var package = each.package;
                        // store the place to get the cosmetic package details later 
                        Cache<PackageDetails>.Value.Insert(package.CanonicalName, (unusedCanonicalFileName) => GetPackageDetails(package, _localLocation));

                        package.InternalPackageData.PolicyMaximumVersion = pkgElement.BindingPolicyMaxVersion;
                        package.InternalPackageData.PolicyMinimumVersion = pkgElement.BindingPolicyMinVersion;
                        foreach (var location in
                            item.Links.Where(link => link.RelationshipType != null && link.RelationshipType.Equals("enclosure")).Select(y => y.Uri)) {
                            package.InternalPackageData.RemoteLocation = location.AbsoluteUri;
                        }


                        foreach (var dependency in pkgElement.Dependencies) {
                            package.InternalPackageData.Dependencies.Add(Package.GetPackageFromCanonicalName(dependency));
                        }

                        if (_remoteLocation == null) {
                            // relative links are local file links.
                            var localDir = Path.GetDirectoryName(Location) ?? string.Empty;

                            if (!string.IsNullOrEmpty(pkgElement.RelativeLocation)) {
                                package.InternalPackageData.LocalLocation = Path.Combine(localDir, pkgElement.RelativeLocation);
                            }

                            if (pkgElement.Locations != null) {
                                foreach (var loc in pkgElement.Locations) {
                                    if (loc.Contains("://")) {
                                        // remote location
                                        package.InternalPackageData.RemoteLocation = new Uri(loc).AbsoluteUri;
                                    }
                                    else {
                                        // must be a local 
                                        package.InternalPackageData.LocalLocation = Path.IsPathRooted(loc) ? loc : Path.Combine(localDir, loc);
                                    }
                                }
                            }
                        }
                        else {
                            // relative links are remote links.
                            if (pkgElement.Locations != null) {
                                foreach (var loc in pkgElement.Locations) {
                                    if (loc.Contains("://")) {
                                        // remote location
                                        package.InternalPackageData.RemoteLocation = new Uri(loc).AbsoluteUri;
                                    }
                                    else {
                                        // must be a relative link 
                                        package.InternalPackageData.RemoteLocation = new Uri(_remoteLocation, loc).AbsoluteUri;
                                    }
                                }
                            }
                            package.InternalPackageData.RemoteLocation = new Uri(_remoteLocation, pkgElement.RelativeLocation).AbsoluteUri;
                            package.InternalPackageData.FeedLocation = _remoteLocation.AbsoluteUri;
                        }

                        _packageList.Add(package);

                    }

                    // finally, make sure that we're 
                    Scanned = true;
                    LastScanned = DateTime.Now;
                    return true;
                }).Wait(); // block on this actually finishing for now.
            }
        }

        internal override IEnumerable<Package> FindPackages(string name, string version, string arch, string publicKeyToken) { 
            Scan();
            return from p in _packageList where
                (string.IsNullOrEmpty(name) || p.Name.IsWildcardMatch(name)) &&
                (string.IsNullOrEmpty(version) || p.Version.UInt64VersiontoString().IsWildcardMatch(version)) &&
                (string.IsNullOrEmpty(arch) || p.Architecture.IsWildcardMatch(arch)) &&
                (string.IsNullOrEmpty(publicKeyToken) || p.PublicKeyToken.IsWildcardMatch(publicKeyToken)) select p;
        }

        internal static PackageDetails GetPackageDetails(Package pkg, string filename) {
            var feed = AtomFeed.Load(filename);

            var item = (from each in feed.Items
                let entry = each as AtomItem
                let pkgElement = entry != null ? entry.packageElement : null
                where entry != null && pkgElement.Name == pkg.Name && pkgElement.Version == pkg.Version && pkgElement.Architecture == pkg.Architecture && pkgElement.PublicKeyToken == pkg.PublicKeyToken
                select entry).FirstOrDefault();

            if( item != null ) {
                return new PackageDetails(pkg) {
                    SummaryDescription = item.Summary.Text,
                    PublishDate = item.PublishDate.Date,

                    Publisher = item.Authors.Select(party => new PackageDetails.Party {
                        Name = party.Name,
                        Email = party.Email,
                        Url = party.Uri
                    }).FirstOrDefault(),

                    Contributors = item.Contributors.Select(party => new PackageDetails.Party {
                        Name = party.Name,
                        Email = party.Email,
                        Url = party.Uri
                    }),
                    CopyrightStatement = item.Copyright != null ? item.Copyright.Text : string.Empty,
                    Tags = item.Categories.Select(cat => cat.Name),
                    FullDescription = (item.Content as TextSyndicationContent != null) ? (item.Content as TextSyndicationContent).Text : string.Empty,
                    Base64IconData = item.packageElement.Icon,
                };
            }
            return null;

            /* dynamic packageData = GetDynamicMSIData(filename);
            var properties = packageData.CO_PACKAGE_PROPERTIES[pkg.ProductCode];
            var publisher = packageData.CO_PUBLISHER[pkg.PublicKeyToken];

            long publishDateTicks;
            Int64.TryParse(properties.publish_date, out publishDateTicks);

            string licenseText = null;
            string licenseUrl = null;

            if (packageData.CO_LICENSE != null)
            {
                var license = packageData.CO_LICENSE[0];
                licenseText = license.license_text;
                licenseUrl = license.license_url;
            }

            return new PackageDetails(pkg) {
                DisplayName = properties.display_name,
                FullDescription = StringExtensions.GunzipFromBase64(properties.description),
                
                AuthorVersion = properties.author_version,
                Base64IconData = properties.icon,
                SummaryDescription = properties.short_description,
                Publisher = new PackageDetails.Party() {
                    Name = publisher.Name,
                    Url = GetURL(packageData.CO_URLS, publisher.location),
                    Email = publisher.email
                },
                License = licenseText.GunzipFromBase64(),
                LicenseUrl = GetURL(packageData.CO_URLS, licenseUrl),
            };
            */

        }
    }
}