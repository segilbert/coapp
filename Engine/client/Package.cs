//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Client {
    using System.Collections.Generic;


    public class Package {
        private static readonly Dictionary<string, Package> _allPackages = new Dictionary<string, Package>();

        public static Package GetPackage(string canonicalName) {
            lock (_allPackages) {
                if (_allPackages.ContainsKey(canonicalName)) {
                    return _allPackages[canonicalName];
                }

                var result = new Package {
                    CanonicalName = canonicalName
                };

                _allPackages.Add(canonicalName, result);
                return result;
            }
        }

        protected Package() {
        }

        public string CanonicalName;
        public string LocalPackagePath;
        public string Name;
        public string Version;
        public string Architecture;
        public string PublicKeyToken;
        public bool IsInstalled;
        public bool IsBlocked;
        public bool Required;
        public bool IsActive;
        public bool IsDependency;
        public string Description;
        public string Summary;
        public string DisplayName;
        public string Copyright;
        public string AuthorVersion;
        public string Icon;
        public string License;
        public string LicenseUrl;
        public string PublishDate;
        public string PublisherName;
        public string PublisherUrl;
        public string PublisherEmail;

        public IEnumerable<string> Tags;
        public IEnumerable<string> RemoteLocations;
        public IEnumerable<string> Dependencies;
        public IEnumerable<string> SupercedentPackages;
    };
}