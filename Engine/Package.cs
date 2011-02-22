//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;
    
    using PackageFormatHandlers;

    public class Package {
        public class Party {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Email { get; set; }
        }

        public readonly string Architecture;
        public readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();
        /// <summary>
        /// the tuple is: (role name, flavor)
        /// </summary>
        public readonly List<Tuple<string, string>> Roles = new List<Tuple<string, string>>();
        public readonly List<PackageAssemblyInfo> Assemblies = new List<PackageAssemblyInfo>();
        public readonly string Name;
        public readonly string ProductCode;
        public readonly string PublicKeyToken;
        public readonly UInt64 Version;

        internal bool DoNotSupercede; // TODO: it's possible these could be contradictory
        internal bool UpgradeAsNeeded; // TODO: it's possible these could be contradictory
        internal bool UserSpecified;

        private string _cosmeticName;
        private bool _couldNotDownload;
        private bool? _isInstalled;
        private string _localPackagePath;
        private bool _packageFailedInstall;
        private Uri _remoteLocation;
        private Package _supercedent;

        // Other Package Metadata 
        public string SummaryDescription { get; set; }
        public DateTime PublishDate { get; set; }

        public Party Publisher { get; set; }
        public IEnumerable<Party> Contributors { get; set; }

        public string CopyrightStatement { get; set; }
        public string FeedLocation { get; set; }
        public string PackageLocation { get; set; }
        public string SourcePackageLocation { get; set; }

        public IEnumerable<string> Tags { get; set; }
        public string FullDescription { get; set; }
        public string Base64IconData { get; set; }

        internal IPackageFormatHandler packageHandler;

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken, string productCode) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
            ProductCode = productCode;
            Changed();
            Dependencies.CollectionChanged += (x, y) => Changed();

            Publisher = new Party() {
                Name = "publisher name",
                Url = "http://foo",
                Email = "foo@goo.com"

            };
        }

        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }
        
        // Causes notifications:
        public string LocalPackagePath {
            get { return _localPackagePath; }
            set {
                AddAlternatePath(value);
                if (value != _localPackagePath) {
                    _localPackagePath = value;
                    Changed();
                }
            }
        }

        public readonly List<string> LocalPackagePaths = new List<string>(); 

        public void AddAlternatePath( string path ) {
            path = path.ToLower();
            if( !LocalPackagePaths.Contains(path))
                LocalPackagePaths.Add(path);
        }

        public bool HasAlternatePath( string path  ) {
            path = path.ToLower();
            return LocalPackagePaths.Contains(path);
        }

        internal Uri RemoteLocation {
            get { return _remoteLocation; }
            set {
                if (value != _remoteLocation) {
                    _remoteLocation = value;
                    Changed();
                }
            }
        }

        public Package Supercedent {
            get { return _supercedent; }
            set {
                if (value != _supercedent) {
                    _supercedent = value;
                }
            }
        }

        public bool PackageFailedInstall {
            get { return _packageFailedInstall; }
            set {
                if (_packageFailedInstall != value) {
                    _packageFailedInstall = value;
                    Changed();
                }
            }
        }

        public string CosmeticName {
            get {
                return _cosmeticName ??
                    (_cosmeticName = "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant());
            }
        }

        public bool CanSatisfy { get; set; }

        public bool AllowedToSupercede {
            get { return UpgradeAsNeeded || (!UserSpecified && !DoNotSupercede); }
        }

        public bool PotentiallyInstallable {
            get {
                if (CouldNotDownload || _packageFailedInstall) {
                    return false;
                }
                return (!string.IsNullOrEmpty(LocalPackagePath) || RemoteLocation != null);
            }
        }

        public bool HasLocalFile {
            get { return string.IsNullOrEmpty(_localPackagePath) ? false : File.Exists(_localPackagePath) ? true : false; }
        }

        public bool HasRemoteLocation {
            get { return RemoteLocation == null ? false : RemoteLocation.IsAbsoluteUri ? true : false; }
        }

        public bool IsInstalled {
            get {
                return _isInstalled ?? (_isInstalled = ((Func<bool>) (() => {
                    try {
                        Changed();
                        return packageHandler.IsInstalled(ProductCode);
                    }
                    catch {
                    }
                    return false;
                }))()).Value;
            }
            set { _isInstalled = value; }
        }

        public bool CouldNotDownload {
            get { return _couldNotDownload; }
            set {
                if (value != _couldNotDownload) {
                    _couldNotDownload = value;
                    Changed();
                }
            }
        }

        public bool IsPackageSatisfied {
            get { return IsInstalled || !string.IsNullOrEmpty(LocalPackagePath) && RemoteLocation != null && Supercedent != null; }
        }

        private static void Changed() {
            Registrar.Updated();
        }

        public void Install(Action<int> progress = null) {
            try {
                packageHandler.Install(_localPackagePath, progress);
                _isInstalled = true;
            }
            catch {
                throw new PackageInstallFailedException(this);
            }
        }

        public void Remove(Action<int> progress = null) {
            try {
                packageHandler.Remove(_localPackagePath, progress);
                _isInstalled = false;
            }
            catch {
                throw new PackageRemoveFailedException(this);
            }
        }
    }
}