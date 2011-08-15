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
        /// The transfer manager interface to files in the cache directory.
        /// 
        /// NOTE: this will be refactored shortly.
        /// </summary>
        private static readonly TransferManager _transferManager = TransferManager.GetTransferManager(PackageManagerSettings.CoAppCacheDirectory);
        
        /// <summary>
        /// the actual Atom/Rss feed object.
        /// </summary>
        private AtomFeed _feed;
        /// <summary>
        /// the remote location this feed is coming from. 
        /// 
        /// May be null if this file is local to start with
        /// </summary>
        private readonly Uri _remoteLocation;

        /// <summary>
        /// the collection of packages found in this feed.
        /// </summary>
        private readonly List<Package> _packageList = new List<Package>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageFeed"/> class.
        /// </summary>
        /// <param name="location">The local file location of the package feed.
        /// This must be a file. NOTE: Needs a check!
        /// </param>
        /// <remarks></remarks>
        internal AtomPackageFeed(string location) : base(location) {
            _feed = AtomFeed.Load(location);
        }
  
        /// <summary>
        /// Initializes a new instance of the <see cref="AtomPackageFeed"/> class.
        /// 
        /// From a remote URI location
        /// </summary>
        /// <param name="location">The URL of the remote package feed..</param>
        /// <remarks>
        /// Note: TransferManager is refactoring soon, I'm sure.
        /// </remarks>
        internal AtomPackageFeed(Uri location) : base(location.AbsoluteUri) {
            _remoteLocation = location;
            var tsk = _transferManager[_remoteLocation].Get();
            tsk.ContinueWith(antecedent => {
                if (_transferManager[_remoteLocation].LastStatus == HttpStatusCode.OK) {
                    _feed = AtomFeed.Load(_transferManager[_remoteLocation].LocalFullPath);
                }
            }, TaskContinuationOptions.AttachedToParent );
        }


        /// <summary>
        /// Iterates thru the list of atom feed items and creates package representations of each item.
        /// </summary>
        /// <remarks></remarks>
        protected void Scan() {
            if (!Scanned) {
                Scanned = true;
                foreach (var each in _feed.Items) {
                    var item = each as AtomItem;
                    var pkgElement = item.packageElement;

                    var package = Registrar.GetPackage(pkgElement.Name, pkgElement.Version, pkgElement.Architecture, pkgElement.PublicKeyToken, pkgElement.Id);


                    package.SummaryDescription = item.Summary.Text;
                    package.PublishDate = item.PublishDate.Date;

                    if (item.Authors.Count > 0) {
                        var author = item.Authors[0];
                        package.Publisher.Name = author.Name;
                        package.Publisher.Email = author.Email;
                        package.Publisher.Url = author.Uri;
                    }

                    package.Contributors = item.Contributors.Select(party => new Package.Party {
                        Name = party.Name,
                        Email = party.Email,
                        Url = party.Uri
                    }).ToList();

                    if (item.Copyright != null) {
                        package.CopyrightStatement = item.Copyright.Text;
                    }

                    package.Tags = item.Categories.Select(cat => cat.Name).ToList();
                    if (item.Content as TextSyndicationContent != null) {
                        package.FullDescription = (item.Content as TextSyndicationContent).Text;
                    }

                    package.PolicyMaximumVersion = pkgElement.BindingPolicyMaxVersion;
                    package.PolicyMinimumVersion = pkgElement.BindingPolicyMinVersion;
                    package.Base64IconData = pkgElement.Icon;

                    package.RemoteLocation.Add(
                        item.Links.Where(link => link.RelationshipType != null && link.RelationshipType.Equals("enclosure")).Select(y => y.Uri));

                    foreach (var dep in pkgElement.Dependencies) {
                        package.Dependencies.Add(Registrar.GetPackage(dep));
                    }

                    if (_remoteLocation == null) {
                        // relative links are local file links.

                        var localDir = Path.GetDirectoryName(Location);

                        if (!string.IsNullOrEmpty(pkgElement.RelativeLocation)) {
                            package.LocalPackagePath.Value = Path.Combine(localDir, pkgElement.RelativeLocation);
                        }

                        if (pkgElement.Locations != null) {
                            foreach (var loc in pkgElement.Locations) {
                                if (loc.Contains("://")) {
                                    // remote location
                                    package.RemoteLocation.Add(new Uri(loc));
                                }
                                else {
                                    // must be a local 
                                    package.LocalPackagePath.Add(Path.IsPathRooted(loc) ? loc : Path.Combine(localDir, loc));
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
                                    package.RemoteLocation.Add(new Uri(loc));
                                }
                                else {
                                    // must be a relative link 
                                    package.RemoteLocation.Add(new Uri(_remoteLocation, loc));
                                }
                            }
                        }
                        package.RemoteLocation.Value = new Uri(_remoteLocation, pkgElement.RelativeLocation);
                        package.FeedLocation.Add(_remoteLocation.AbsoluteUri);
                    }

                    _packageList.Add(package);
                }
            }
        }

        /// <summary>
        /// Finds packages matching the same publisher, name, and publickeytoken
        /// </summary>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns>Returns a collection of packages that match</returns>
        /// <remarks></remarks>
        internal override IEnumerable<Package> FindPackages(Package packageFilter) {
            Scan();
            // DebugMessage.Invoke.WriteLine(string.Format( "Scanning(pk) for package [{0}]-[{1}]-[{2}]", packageFilter.Name,packageFilter.Architecture, packageFilter.PublicKeyToken));
            return from package in _packageList
                where
                    package.Name == packageFilter.Name && package.Architecture == packageFilter.Architecture &&
                        package.PublicKeyToken == packageFilter.PublicKeyToken
                select package;
        }

        /// <summary>
        /// Finds packages based on the cosmetic name of the package.
        /// 
        /// Supports wildcard in pattern match.
        /// </summary>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal override IEnumerable<Package> FindPackages(string packageFilter) {
            Scan();

            return from package in _packageList where package.CosmeticName.IsWildcardMatch(packageFilter) select package;
        }
    }
}