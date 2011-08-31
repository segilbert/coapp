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
    using System.IO;
    using System.Linq;
    using Crypto;
    using Exceptions;
    using Extensions;
    using Feeds;
    using PackageFormatHandlers;
    using Shell;
    using Tasks;
    using Win32;

    public class Package : NotifiesPackageManager {
        private string _canonicalName;
        private bool? _isInstalled;
        private PackageDetails _packageDetails;
        private InternalPackageData _internalPackageData;
        internal IPackageFormatHandler PackageHandler;

        public string CanonicalName { get {
            if( _canonicalName == null && Version > 0 ) {
                _canonicalName = "{0}-{1}-{2}-{3}".format(Name, Version.UInt64VersiontoString(), Architecture, PublicKeyToken).ToLowerInvariant();
            }
            return _canonicalName;
        } }
        public string Name { get; internal set; }
        public UInt64 Version { get; internal set; }
        public string Architecture { get; internal set; }
        public string PublicKeyToken { get; internal  set; }
        public string ProductCode { get; internal set; }
        

        /// <summary>
        /// Gets the package details object.
        /// 
        /// if _packageDetails is null, it tries to get the data from the system cache (probably by use of a delegate)
        /// </summary>
        internal PackageDetails PackageDetails { 
            get { return _packageDetails ?? (_packageDetails = Cache<PackageDetails>.Value[CanonicalName] ); }
        }

        internal InternalPackageData InternalPackageData {
            get { return _internalPackageData ?? (_internalPackageData = new InternalPackageData(this)); }
        }

        internal PackageSessionData PackageSessionData { get { return SessionCache<PackageSessionData>.Value[CanonicalName] ?? (SessionCache<PackageSessionData>.Value[CanonicalName] = new PackageSessionData(this)); } }
        internal PackageRequestData PackageRequestData { get { return RequestCache<PackageRequestData>.Value[CanonicalName] ?? (RequestCache<PackageRequestData>.Value[CanonicalName] = new PackageRequestData(this)); } }

        public bool IsInstalled {
            get {
                return _isInstalled ?? (_isInstalled = ((Func<bool>) (() => {
                    try {
                        Changed();
                        if (PackageHandler != null)
                            return PackageHandler.IsInstalled(ProductCode);

                        return false;
                    }
                    catch {

                    }
                    return false;
                }))()).Value;
            }
            set {
                if( _isInstalled != value ) {
                    if( value ) {
                        InstalledPackageFeed.Instance.PackageInstalled( this );
                    }
                    else {
                        InstalledPackageFeed.Instance.PackageRemoved(this);
                    }
                }
                _isInstalled = value;
            }
        }

        public bool IsActive {
            get { 
                return GetCurrentPackage(Name, PublicKeyToken) == Version;
            }
        }

        public string GeneralName {
            get { return "{0}-{1}".format(Name, PublicKeyToken).ToLowerInvariant(); }
        }

        public string CosmeticName {
            get { return "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant(); }
        }

        private Package() {
            Name = string.Empty;
            Version = 0;
            Architecture = string.Empty;
            PublicKeyToken = string.Empty;
        }

        internal Package(string productCode) : this() {
            ProductCode = productCode;
        }

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken, string productCode) : this(productCode) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
        }

        /// <summary>
        /// This drops any data from the object that isn't minimally neccessary for 
        /// the smooth running of the package manager.
        /// </summary>
        internal void DropDetails() {

            // drop the package details. If it's needed again, there should be a delegate to grab it 
            // from the MSI or Feed.
            _packageDetails = null;
            Cache<PackageDetails>.Value.Clear(CanonicalName);
        }

        #region Install/Remove
        public void Install(Action<int> progress = null) {
            try {
                var currentVersion = GetCurrentPackage(Name, PublicKeyToken);

                PackageHandler.Install(this, progress);
                IsInstalled = true;
                    
                if (Version > currentVersion) {
                    SetPackageCurrent();
                }
                else {
                    DoPackageComposition(false);
                }

                // GS01 : what was this call for again? 
                // SaveCachedInfo();
            }
            catch (Exception) {
                //we could get here and the MSI had installed but nothing else
                PackageHandler.Remove(this, null);
                IsInstalled = false;
                throw new PackageInstallFailedException(this);
            }
        }

        public void Remove(Action<int> progress = null) {
            try {
                UndoPackageComposition();
                PackageHandler.Remove(this, progress);
                IsInstalled = false;
            }
            catch (Exception) {
                PackageManagerMessages.Invoke.FailedPackageRemoval(CanonicalName, "GS01: I'm not sure of the reason... ");
                throw new OperationCompletedBeforeResultException();
            }
            finally {
                try {
                    // this will activate the next one in line
                    GetCurrentPackage(Name, PublicKeyToken);
                    // GS01: fix this to rerun package composition on prior version.
                }
                catch (Exception e) {
                    // boooo!
                    Console.WriteLine("failed setting active package for {0}-{1}", Name, PublicKeyToken);
                }
            }
        }
        #endregion

        #region Package Composition 

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
            // {$ALLPROGRAMS} The Programs directory for all users 
            //                  (usually C:\ProgramData\Microsoft\Windows\Start Menu\Programs)
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

            result = result.Replace(@"{$INCLUDE}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, "include"));
            result = result.Replace(@"{$LIB}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, "lib"));
            result = result.Replace(@"{$DOTNETASSEMBLIES}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, @".NET\Assemblies"));
            result = result.Replace(@"{$BIN}", Path.Combine(PackageManagerSettings.CoAppRootDirectory, "bin"));
            result = result.Replace(@"{$APPS}", PackageManagerSettings.CoAppRootDirectory);
            result = result.Replace(@"{$INSTALL}", PackageManagerSettings.CoAppInstalledDirectory);
            result = result.Replace(@"{$ALLPROGRAMS}", KnownFolders.GetFolderPath(KnownFolder.CommonPrograms));

            result = result.Replace(@"{$PUBLISHER}", PackageDetails.Publisher.Name);
            result = result.Replace(@"{$PRODUCTNAME}", Name);
            result = result.Replace(@"{$VERSION}", Version.UInt64VersiontoString());
            result = result.Replace(@"{$ARCH}", Architecture);
            result = result.Replace(@"{$COSMETICNAME}", "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant());

            return result;
        }


        public IEnumerable<CompositionRule> ImplicitRules {
            get {
                foreach (var role in InternalPackageData.Roles.Select(each => each.Item1)) {
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
                        case PackageRole.Source:
                            break;
                    }
                }
            }
        }

        public void DoPackageComposition(bool makeCurrent) {
            // GS01: if package composition fails, and we're in the middle of installing a package
            // we should roll back the package install.

            var rules = ImplicitRules.Union(PackageHandler.GetCompositionRules(this));

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.SymlinkFolder)) {
                var link = rule.Location.GetFullPath();
                var dir = rule.Target.GetFullPath();

                if (Directory.Exists(dir) && (makeCurrent || !Directory.Exists(link))) {
                    try {
                        Symlink.MakeDirectoryLink(link, dir);
                    }
                    catch (Exception) {
                        Console.WriteLine("Warning: Directory Symlink Link Failed. [{0}] => [{1}]", link, dir);
                        // Console.WriteLine(e.Message);
                        // Console.WriteLine(e.StackTrace);

                    }
                }
            }

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.SymlinkFile)) {
                var file = rule.Target.GetFullPath();
                var link = rule.Location.GetFullPath();
                if (File.Exists(file) && (makeCurrent || !File.Exists(link))) {
                    if (!Directory.Exists(Path.GetDirectoryName(link))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(link));
                    }

                    try {
                        Symlink.MakeFileLink(link, file);
                    }
                    catch (Exception) {
                        Console.WriteLine("Warning: File Symlink Link Failed. [{0}] => [{1}]", link, file);
                        // Console.WriteLine(e.Message);
                        // Console.WriteLine(e.StackTrace);
                    }
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
            var rules = ImplicitRules.Union(PackageHandler.GetCompositionRules(this));

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
                where File.Exists(target) && File.Exists(link) && Symlink.IsSymlink(link) && Symlink.GetActualPath(link).Equals(target)
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
            var installedVersionsOfPackage = from pkg in NewPackageManager.Instance.InstalledPackages
                where
                    pkg.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase) &&
                        pkg.PublicKeyToken.Equals(publicKeyToken, StringComparison.CurrentCultureIgnoreCase)
                orderby pkg.Version descending
                select pkg;

            var latestPackage = installedVersionsOfPackage.FirstOrDefault();

            // clean as we go...
            if (latestPackage == null) {
                PackageManagerSettings.PerPackageSettings["{0}-{1}".format(packageName, publicKeyToken), "CurrentVersion"].Value = null;
                return 0;
            }

            var ver = (ulong) PackageManagerSettings.PerPackageSettings[latestPackage.GeneralName, "CurrentVersion"].LongValue;

            if (ver == 0 || installedVersionsOfPackage.Where(p => p.Version == ver).FirstOrDefault() == null) {
                latestPackage.SetPackageCurrent();
                return latestPackage.Version;
            }

            return ver;
        }

        public bool Required { 
            get {
                return PackageManagerSettings.PerPackageSettings[CanonicalName, "Required"].BoolValue || PackageSessionData.IsDependency;
            } 
            set { PackageManagerSettings.PerPackageSettings[CanonicalName, "Required"].BoolValue = value; }
        }

        public bool IsBlocked { 
            get { return PackageManagerSettings.PerPackageSettings[CanonicalName, "Blocked"].BoolValue; } 
            set { PackageManagerSettings.PerPackageSettings[CanonicalName, "Blocked"].BoolValue = value; }
        }

        public void SetPackageCurrent() {
            if (!IsInstalled) {
                throw new PackageNotInstalledException(this);
            }
            var generalName = GeneralName;

            if (Version == (ulong) PackageManagerSettings.PerPackageSettings[generalName, "CurrentVersion"].LongValue) {
                return; // it's already set to the current version.
            }

            DoPackageComposition(true);

            if (0 != (ulong) PackageManagerSettings.PerPackageSettings[generalName, "CurrentVersion"].LongValue) {
                // if there isn't a forced current version, let's not force it
                PackageManagerSettings.PerPackageSettings[generalName, "CurrentVersion"].LongValue = (long) Version;
            }
        }
         #endregion
    }

    internal class InternalPackageData : NotifiesPackageManager {
        private Package _package;

        private string _canonicalPackageLocation;
        private string _canonicalFeedLocation;

        private string _primaryLocalLocation;
        private string _primaryRemoteLocation;
        private string _primaryFeedLocation;

        private readonly List<Uri> _remoteLocations = new List<Uri>();
        private readonly List<string> _feedLocations = new List<string>();
        private readonly List<string> _localLocations = new List<string>();

        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }

        /// <summary>
        /// the tuple is: (role name, flavor)
        /// </summary>
        public readonly List<Tuple<PackageRole, string>> Roles = new List<Tuple<PackageRole, string>>();
        public readonly List<PackageAssemblyInfo> Assemblies = new List<PackageAssemblyInfo>();
        public readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();

        public string CanonicalPackageLocation {
            get { return _canonicalPackageLocation; }
            set {
                try {
                    RemoteLocation = _canonicalPackageLocation = new Uri(value).AbsoluteUri;
                }
                catch {
                }
            }
        }

        public string CanonicalFeedLocation {
            get { return _canonicalFeedLocation; }
            set {
                FeedLocation = _canonicalFeedLocation = value;
            }
        }

        public string CanonicalSourcePackageLocation { get; set; }
        
        internal InternalPackageData(Package package) {
            _package = package;
             Dependencies.CollectionChanged += (x, y) => Changed();
        }

        public bool IsPackageSatisfied {
            get { return _package.IsInstalled || !string.IsNullOrEmpty(LocalLocation) && RemoteLocation != null && _package.PackageSessionData.Supercedent != null; }
        }

        public bool HasLocalLocation {
            get { return !string.IsNullOrEmpty(LocalLocation); }
        }

        public bool HasRemoteLocation {
            get { return !string.IsNullOrEmpty(RemoteLocation); }
        }

        public IEnumerable<string> LocalLocations { get { return _localLocations.ToArray(); } }
        public IEnumerable<string> RemoteLocations { get { return _remoteLocations.Select(each => each.AbsoluteUri).ToArray(); } }
        public IEnumerable<string> FeedLocations { get { return _feedLocations.ToArray(); } }

        public string LocalLocation { 
            get {
                if (_primaryLocalLocation.FileIsLocalAndExists()) {
                    return _primaryLocalLocation;
                }
                // use the setter to remove non-viable locations.
                LocalLocation = null;

                // whatever is primary after the set is good for me.
                return _primaryLocalLocation;
            }
            set {
                lock (_localLocations) {
                    try {
                        var location = value.CanonicalizePathIfLocalAndExists();

                        if (!string.IsNullOrEmpty(location)) {
                            // this location is acceptable.
                            _primaryLocalLocation = location;
                            if (!_localLocations.Contains(location)) {
                                _localLocations.Add(location);
                            }
                            return;
                        }
                    } catch {
                        // file couldn't canonicalize.
                    }

                    _primaryLocalLocation = null;

                    // try to find an acceptable local location from the list 
                    foreach (var path in _localLocations.Where(path => path.FileIsLocalAndExists())) {
                        _primaryLocalLocation = path;
                        break;
                    }
                }
            }
        }

        public string RemoteLocation {
            get {
                if (!string.IsNullOrEmpty(_canonicalPackageLocation)) {
                    return _canonicalPackageLocation;
                }

                if (!string.IsNullOrEmpty(_primaryRemoteLocation)) {
                    return _primaryRemoteLocation;
                }

                // use the setter to remove non-viable locations.
                RemoteLocation = null;

                // whatever is primary after the set is good for me.
                return _primaryRemoteLocation;
            }

            set {
                lock (_remoteLocations) {
                    if (!string.IsNullOrEmpty(value)) {
                        try {
                            var location = new Uri(value);

                            // this location is acceptable.
                            _primaryRemoteLocation = location.AbsoluteUri;

                            if (!_remoteLocations.Contains(location)) {
                                _remoteLocations.Add(location);
                            }

                            return;
                        }
                        catch {
                            // path couldn't be expressed as a URI?.
                        }
                    }
                    
                    // set it as the first viable remote location.
                    var uri = _remoteLocations.FirstOrDefault();
                    _primaryRemoteLocation = uri == null ? null : uri.AbsoluteUri;
                }
            }
        }

        public string FeedLocation {
            get {
                if (!string.IsNullOrEmpty(_canonicalFeedLocation)) {
                    return _canonicalFeedLocation;
                }

                if (!string.IsNullOrEmpty(_primaryFeedLocation)) {
                    return _primaryFeedLocation;
                }

                // use the setter to remove non-viable locations.
                FeedLocation = null;

                // whatever is primary after the set is good for me.
                return _primaryFeedLocation;
            }

            set {
                lock (_feedLocations) {
                    if (!string.IsNullOrEmpty(value)) {
                        _primaryFeedLocation= value;
                        if (!_feedLocations.Contains(value)) {
                            _feedLocations.Add(value);
                        }
                        return;
                    }

                    // set it as the first viable remote location.
                    var location = _feedLocations.FirstOrDefault();
                    _primaryFeedLocation = string.IsNullOrEmpty(location) ? null : location;
                }
            }
        }
    }

    internal class PackageDetails : NotifiesPackageManager {
        internal class Party {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Email { get; set; }
        }

        private Package _package;

        internal PackageDetails(Package package) {
            _package = package;
        }

        public string SummaryDescription { get; set; }
        public DateTime PublishDate { get; set; }

        public Party Publisher = new Party();
        public IEnumerable<Party> Contributors { get; set; }

        public string CopyrightStatement { get; set; }
        public string AuthorVersion { get; set; }

        public IEnumerable<string> Tags { get; set; }
        public string FullDescription { get; set; }
        public string Base64IconData { get; set; }

        public string License { get; set; }
        public string LicenseUrl { get; set; }

        public string DisplayName { get; set; }

    }

    /// <summary>
    /// This stores information that is really only relevant to the currently running 
    /// Session, not between sessions.
    /// 
    /// The instance of this is bound to the Session.
    /// </summary>
    internal class PackageSessionData : NotifiesPackageManager {

        internal bool DoNotSupercede; // TODO: it's possible these could be contradictory
        internal bool UpgradeAsNeeded; // TODO: it's possible these could be contradictory
        internal bool UserSpecified;
        internal bool RequestedDownload;

        internal bool IsDependency;

        private bool _couldNotDownload;
        private Package _supercedent;
        private bool _packageFailedInstall;
        private Package _package;
        private string _localValidatedLocation;

        internal PackageSessionData(Package package) {
            _package = package;
        }

        public Package Supercedent {
            get { return _supercedent; }
            set {
                if (value != _supercedent) {
                    _supercedent = value;
                    Changed();
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

        /*
        public bool Supercedes(Package p)
        {
            return Architecture == p.Architecture &&
                   PublicKeyToken == p.PublicKeyToken &&
                   Name.Equals(p.Name, StringComparison.CurrentCultureIgnoreCase) &&
                   p.Version <= PolicyMaximumVersion && p.Version >= PolicyMinimumVersion;
        }
        */
        
        public bool CouldNotDownload {
            get { return _couldNotDownload; }
            set {
                if (value != _couldNotDownload) {
                    _couldNotDownload = value;
                    Changed();
                }
            }
        }

        public bool AllowedToSupercede {
            get { return UpgradeAsNeeded || (!UserSpecified && !DoNotSupercede) && PotentiallyInstallable; }
        }

        public bool PotentiallyInstallable {
            get {
                return !PackageFailedInstall && (_package.InternalPackageData.HasLocalLocation || !CouldNotDownload && _package.InternalPackageData.HasRemoteLocation);
            }
        }

        public bool CanSatisfy { get; set; }

        
        public string LocalValidatedLocation {
            get {
                if (!string.IsNullOrEmpty(_localValidatedLocation) && _localValidatedLocation.FileIsLocalAndExists()) {
                    return _localValidatedLocation;
                }

                var location = _package.InternalPackageData.LocalLocation;
                if (string.IsNullOrEmpty(location)) {
                    // there are no local locations at all for this package?
                    return _localValidatedLocation = null;
                }

                if (Verifier.HasValidSignature(location)) {
                    PackageManagerMessages.Invoke.SignatureValidation(location, true, Verifier.GetPublisherName(location));
                    return _localValidatedLocation = location;
                }
                PackageManagerMessages.Invoke.SignatureValidation(location, false, null);

                var result = _package.InternalPackageData.LocalLocations.Any(Verifier.HasValidSignature) ? location : null;

                PackageManagerMessages.Invoke.SignatureValidation(result, !string.IsNullOrEmpty(result), string.IsNullOrEmpty(result) ? null : Verifier.GetPublisherName(result));
                return _localValidatedLocation = result;
            }
        }
    }

    /// <summary>
    /// This stores information that is really only relevant to the currently running 
    /// request, not between sessions.
    /// 
    /// The instance of this is bound to the Session.
    /// </summary>
    internal class PackageRequestData : NotifiesPackageManager {
        private Package _package;

        internal bool NotifiedClientThisSupercedes;

        internal PackageRequestData(Package package) {
            _package = package;
        }
    }

    public class NotifiesPackageManager {
        internal static void Changed() {
            // notify the Registrar that a change has occured in a package.
            NewPackageManager.Instance.Updated();
        }
    }
}