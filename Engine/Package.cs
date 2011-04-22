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
    using Shell;
    using Tasks;
    using Win32;

    public class Package {
        public class Party {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Email { get; set; }
        }

        public readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();
        /// <summary>
        /// the tuple is: (role name, flavor)
        /// </summary>
        public readonly List<Tuple<PackageRole, string>> Roles = new List<Tuple<PackageRole, string>>();
        public readonly List<PackageAssemblyInfo> Assemblies = new List<PackageAssemblyInfo>();
        public string ProductCode { get; internal set; }

        public string Architecture { get; private set; }
        public string Name { get; private set; }
        public string PublicKeyToken { get; private set; }
        public UInt64 Version { get; private set; }

        internal bool DoNotSupercede; // TODO: it's possible these could be contradictory
        internal bool UpgradeAsNeeded; // TODO: it's possible these could be contradictory
        internal bool UserSpecified;

        private readonly Lazy<string> _generalName;// foo-1234567890ABCDEF 
        private readonly Lazy<string> _cosmeticName; // foo-1.2.3.4-x86 
        private readonly Lazy<string> _canonicalName; // foo-1.2.3.4-x86-1234567890ABCDEF 

        private bool _couldNotDownload;
        private bool? _isInstalled;
        // private string _localPackagePath;
        private bool _packageFailedInstall;
        // private Uri _remoteLocation;
        private Package _supercedent;

        // Other Package Metadata 
        public string SummaryDescription { get; set; }
        public DateTime PublishDate { get; set; }

        public Party Publisher { get; set; }
        public IEnumerable<Party> Contributors { get; set; }

        public string CopyrightStatement { get; set; }
        public string AuthorVersion { get; set; }

        public readonly MultiplexedProperty<string> FeedLocation = new MultiplexedProperty<string>((x, y) => Changed());
        public readonly MultiplexedProperty<Uri> RemoteLocation = new MultiplexedProperty<Uri>((x, y) => Changed());
        public readonly MultiplexedProperty<string> LocalPackagePath = new MultiplexedProperty<string>((x, y) => Changed(), false);

        private string _canonicalPackageLocation;
        public string CanonicalPackageLocation {
            get { return _canonicalPackageLocation; } 
            set { _canonicalPackageLocation = value; try { RemoteLocation.Add( new Uri(value));}catch{}}
        }

        private string _canonicalFeedLocation;
        public string CanonicalFeedLocation {
            get { return _canonicalFeedLocation; }
            set { _canonicalFeedLocation = value; FeedLocation.Add(value); }
        }

        public string CanonicalSourcePackageLocation { get; set; }

        public IEnumerable<string> Tags { get; set; }
        public string FullDescription { get; set; }
        public string Base64IconData { get; set; }

        internal IPackageFormatHandler packageHandler;

        internal Package(string productCode) {
            Name = string.Empty;
            Version = 0;
            Architecture = string.Empty;
            PublicKeyToken = string.Empty;
            Dependencies.CollectionChanged += (x, y) => Changed();

            ProductCode = productCode;

            Publisher = new Party() {
                Name = string.Empty,
                Url = string.Empty,
                Email = string.Empty
            };

            _canonicalName = new Lazy<string>(() => "{0}-{1}-{2}-{3}".format(Name, Version.UInt64VersiontoString(), Architecture, PublicKeyToken).ToLowerInvariant());
            _cosmeticName = new Lazy<string>(() => "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant());
            _generalName = new Lazy<string>(() => "{0}-{1}".format(Name, PublicKeyToken).ToLowerInvariant());
        }

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken, string productCode) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
            ProductCode = productCode;
            Dependencies.CollectionChanged += (x, y) => Changed();

            _canonicalName = new Lazy<string>(() => "{0}-{1}-{2}-{3}".format(Name, Version.UInt64VersiontoString(), Architecture, PublicKeyToken).ToLowerInvariant());
            _cosmeticName = new Lazy<string>(() => "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant());
            _generalName = new Lazy<string>(() => "{0}-{1}".format(Name, PublicKeyToken).ToLowerInvariant());

            Publisher = new Party() {
                Name = string.Empty,
                Url = string.Empty,
                Email = string.Empty
            };

            LoadCachedInfo();
        }

        internal void SetPackageProperties(string name, string architecture, UInt64 version, string publicKeyToken ) {
            // only to support the construction of a package object where only the product code is known
            // at instanciation time.
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
            LoadCachedInfo(); 
            Changed();
        }

        private void LoadCachedInfo() {
            RemoteLocation.Add(PackageManagerSettings.CacheStringArraySetting[CanonicalName, "RemoteLocation"].Select(item => new Uri(item)));
            FeedLocation.Add(PackageManagerSettings.CacheStringArraySetting[CanonicalName, "Feed"]);
        }

        private void SaveCachedInfo() {
            PackageManagerSettings.CacheStringArraySetting[CanonicalName, "RemoteLocation"] = RemoteLocation.Select(item => item.AbsoluteUri);
            PackageManagerSettings.CacheStringArraySetting[CanonicalName, "Feed"] = FeedLocation;
        }

        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }

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

        public string GeneralName {
            get { return _generalName.Value; }
        }

        public string CosmeticName {
            get { return _cosmeticName.Value; }
        }

        public string CanonicalName {
            get { 
                return _canonicalName.Value;
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
            get {
                if( string.IsNullOrEmpty(LocalPackagePath) && File.Exists(LocalPackagePath)  )
                    return true;

                return LocalPackagePath.Any(location => File.Exists(location));
            }
        }

        public bool HasRemoteLocation {
            get { return RemoteLocation.Value != null; }
        }

        public bool IsInstalled {
            get {
                return _isInstalled ?? (_isInstalled = ((Func<bool>) (() => {
                    try {
                        Changed();
                        if( packageHandler != null )
                            return packageHandler.IsInstalled(ProductCode);
                        
                        return false;
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
                var currentVersion = GetCurrentPackage(Name, PublicKeyToken);

                packageHandler.Install(this , progress);
                _isInstalled = true;

                
                if( Version > currentVersion ) {
                    SetPackageCurrent();
                } else {
                    DoPackageComposition(false);  
                }
                
                SaveCachedInfo();
            }
            catch {
                throw new PackageInstallFailedException(this);
            }
        }

        public void Remove(Action<int> progress = null) {
            try {
                UndoPackageComposition(); 
                packageHandler.Remove(this, progress);
                _isInstalled = false;

                // this will activate the next one in line
                GetCurrentPackage(Name, PublicKeyToken);
            }
            catch {
                PackageManagerMessages.Invoke.PackageRemoveFailed(this);
                throw new OperationCompletedBeforeResultException();
            }
        }


        /// <summary>
        /// V1 of the Variable Resolver.
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal string ResolveVariables(string text) {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            //System Constants:
            // {$APPS} CoApp Application directory (c:\apps)
            // {$BIN} CoApp bin directory (in PATH) ({$APPS}\bin)
            // {$LIB} CoApp lib directory ({$APPS}\lib)
            // {$DOTNETASSEMBLIES} CoApp .NET Reference Assembly directory ({$APPS}\.NET\Assemblies)
            // {$INCLUDE} CoApp include directory ({$APPS}\include)
            // {$INSTALL} CoApp .installed directory ({$APPS}\.installed)
            
            // Package Variables:
            // {$PUBLISHER}         Publisher name (CN of the certificate used to sign the package)
            // {$PRODUCTNAME}       Name of product being installed
            // {$VERSION}           Version of package being installed. (##.##.##.##)
            // {$ARCH}              Platform of package being installed -- one of [x86, x64, any]
            // {$COSMETICNAME}      Complete name ({$PRODUCTNAME}-{$VERSION}-{$PLATFORM})

            // {$PACKAGEDIR}        Where the product is getting installed into
            // {$CANONICALPACKAGEDIR} The "publicly visible location" of the "current" version of the package.

            var result = text;

            result = result.Replace(@"{$PKGDIR}", @"{$PACKAGEDIR}");
            result = result.Replace(@"{$PACKAGEDIR}", @"{$INSTALL}\{$PUBLISHER}\{$PRODUCTNAME}-{$VERSION}-{$ARCH}\");
            result = result.Replace(@"{$CANONICALPACKAGEDIR}", @"{$APPS}\{$PRODUCTNAME}\");

            result = result.Replace(@"{$INCLUDE}", Path.Combine( PackageManagerSettings.CoAppRootDirectory, "include"));
            result = result.Replace(@"{$LIB}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, "lib"));
            result = result.Replace(@"{$DOTNETASSEMBLIES}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, @".NET\Assemblies"));
            result = result.Replace(@"{$BIN}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, "bin"));
            result = result.Replace(@"{$APPS}", PackageManagerSettings.CoAppRootDirectory);
            result = result.Replace(@"{$INSTALL}", PackageManagerSettings.CoAppInstalledDirectory);

            result = result.Replace(@"{$PUBLISHER}", Publisher.Name);
            result = result.Replace(@"{$PRODUCTNAME}", Name);
            result = result.Replace(@"{$VERSION}", Version.UInt64VersiontoString());
            result = result.Replace(@"{$ARCH}", Architecture);
            result = result.Replace(@"{$COSMETICNAME}", CosmeticName);

            return result;
        }

        public IEnumerable<CompositionRule> ImplicitRules {
            get {
                foreach (var role in Roles.Select(each => each.Item1)) {
                    switch (role) {
                        case PackageRole.Application:
                            yield return new CompositionRule(this) {
                                Action = CompositionAction.SymlinkFolder,
                                Location = "{$CANONICALPACKAGEDIR}",
                                Target = "{$PACKAGEDIR}",
                            };
                            break;
                        case PackageRole.DeveloperLib:
                            break;
                        case PackageRole.SharedLib:
                            break;
                        case PackageRole.SourceCode:
                            break;
                    }
                }
            }
        }

        public void DoPackageComposition(bool makeCurrent) {
            var rules = ImplicitRules.Union(packageHandler.GetCompositionRules(this));
            
            foreach( var rule in rules.Where(r => r.Action == CompositionAction.SymlinkFolder)) {
                var link = rule.Location.GetFullPath();
                var dir = rule.Target.GetFullPath();

                if (Directory.Exists(dir) && ( makeCurrent || !Directory.Exists(link) ) ) {
                    if (!Directory.Exists(Path.GetDirectoryName(link))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(link));
                    }

                    Symlink.MakeDirectoryLink(link, dir);
                }
            }

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.SymlinkFile)) {
                var file = rule.Target.GetFullPath();
                var link = rule.Location.GetFullPath();
                if (File.Exists(file) && (makeCurrent || !File.Exists(link))) {
                    if( !Directory.Exists( Path.GetDirectoryName(link)) ) {
                        Directory.CreateDirectory(Path.GetDirectoryName(link));
                    }

                    Symlink.MakeFileLink(link, file);
                }
            }

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.Shortcut)) {
                var target = rule.Target.GetFullPath();
                var link = rule.Location.GetFullPath();

                if (File.Exists(target) && (makeCurrent || !File.Exists(link))) {
                    if (!Directory.Exists(Path.GetDirectoryName(link))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(link));
                    }

                    ShellLink.CreateShortcut(link, target);
                }
            }
        }

        public void UndoPackageComposition() {
            var rules = ImplicitRules.Union(packageHandler.GetCompositionRules(this));

            foreach (var link in from rule in rules.Where(r => r.Action == CompositionAction.Shortcut)
                let target = rule.Target.GetFullPath()
                let link = rule.Location.GetFullPath()
                where ShellLink.PointsTo(link, target)
                select link) {
                    link.TryHardToDeleteFile();
            }

            foreach (var link in from rule in rules.Where(r => r.Action == CompositionAction.SymlinkFile)
                let target = rule.Target.GetFullPath()
                let link = rule.Location.GetFullPath()
                where File.Exists(target) && Symlink.IsSymlink(link) && Symlink.GetActualPath(link).Equals(target)
                select link) {
                    Symlink.DeleteSymlink(link);
            }

            foreach (var link in from rule in rules.Where(r => r.Action == CompositionAction.SymlinkFolder)
                let target = rule.Target.GetFullPath()
                let link = rule.Location.GetFullPath()
                where File.Exists(target) && Symlink.IsSymlink(link) && Symlink.GetActualPath(link).Equals(target)
                select link) {
                    Symlink.DeleteSymlink(link);
            }
        }

        internal static ulong GetCurrentPackage(string packageName, string publicKeyToken) {
            var installedVersionsOfPackage = from pkg in Registrar.InstalledPackages
                where pkg.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase) && pkg.PublicKeyToken.Equals( publicKeyToken, StringComparison.CurrentCultureIgnoreCase)
                orderby pkg.Version descending
                select pkg;

            var latestPackage = installedVersionsOfPackage.FirstOrDefault();

            if (latestPackage == null) {
                PackageManagerSettings.PerPackageStringSetting["{0}-{1}".format(packageName, publicKeyToken), "CurrentVersion"] = null;
                return 0;
            }

            var ver = (ulong)PackageManagerSettings.PerPackageLongSetting[latestPackage.GeneralName, "CurrentVersion"];

            if (ver == 0 || installedVersionsOfPackage.Where(p => p.Version == ver).FirstOrDefault() == null) {
                // hmm. Nothing is marked as current, or the 'current' version isn't installed.
                // either way, we're gonna fix that up while we're here, if we can.
                if (AdminPrivilege.IsRunAsAdmin) {
                    latestPackage.SetPackageCurrent();
                }
                return latestPackage.Version;
            }

            return ver;
        }

        public void SetPackageCurrent() {
            if(!IsInstalled) {
                throw new PackageNotInstalledException(this);
            }

            if (Version == (ulong)PackageManagerSettings.PerPackageLongSetting[GeneralName, "CurrentVersion"]) {
                return; // it's already set to the current version.
            }

            DoPackageComposition(true);
            PackageManagerSettings.PerPackageLongSetting[GeneralName, "CurrentVersion"] = (long)Version;
        }
    }
}