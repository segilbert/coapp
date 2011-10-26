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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Configuration;
    using Crypto;
    using Exceptions;
    using Extensions;
    using Feeds;
    using Logging;
    using Model;
    using PackageFormatHandlers;
    using Shell;
    using Tasks;
    using Toolkit.Exceptions;
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
        public Guid? ProductCode { get; internal set; }

        internal string DisplayName { get; set; }
        internal string PublisherDirectory { get; set; }

        /// <summary>
        /// Gets the package details object.
        /// 
        /// if _packageDetails is null, it tries to get the data from the cache (probably by use of a delegate)
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
                        if (PackageHandler != null && ProductCode != null ) {
                            return PackageHandler.IsInstalled(ProductCode.Value);
                        }

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
                return GetCurrentPackageVersion(Name, PublicKeyToken) == Version;
            }
        }

        public string GeneralName {
            get { return "{0}-{1}".format(Name, PublicKeyToken).ToLowerInvariant(); }
        }

        public string CosmeticName {
            get { return "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant(); }
        }

        /// <summary>
        /// the collection of all known packages
        /// </summary>
        private static readonly ObservableCollection<Package> _packages = new ObservableCollection<Package>();

        internal static Package GetPackageFromProductCode(Guid? productCode) {
            if (productCode != null ) {
                lock (_packages) {
                    var pkg = _packages.Where(package => package.ProductCode == productCode).FirstOrDefault();
                    if (pkg == null) {
                        // where the only thing we know is product code.
                        pkg = new Package(productCode);
                        _packages.Add(pkg);
                    }
                    return pkg;
                }
            }
            return null; // only happens if the productCode isn't a guid.
        }

#if FALSE
        

        internal static  Package GetPackageFromCanonicalName(string canonicalName) {
            lock (_packages) {
                var packageName = PackageName.Parse(canonicalName);
                if (packageName.IsFullMatch) {
                    var pkg = _packages.Where(package => package.CanonicalName == canonicalName).FirstOrDefault();
                    if (pkg == null) {
                        // where the only thing we know is canonical Name.
                        pkg = new Package(packageName.Name, packageName.Arch, packageName.Version.VersionStringToUInt64(), packageName.PublicKeyToken, null);
                        _packages.Add(pkg);
                    }
                    return pkg;
                }
            }
            return null; // only happens if the canonicalName isn't a canonicalName.
        }
#endif 

        internal static Package GetPackageFromFilename(string filename ) {
            filename = filename.CanonicalizePathIfLocalAndExists();

            if (!File.Exists(filename)) {
                PackageManagerMessages.Invoke.FileNotFound(filename);
                return null;
            }

            Package pkg;

            lock (_packages) {
                pkg = (_packages.Where(
                    package => package.InternalPackageData.HasLocalLocation && package.InternalPackageData.LocalLocations.Contains(filename))).FirstOrDefault();
            }

            return pkg ?? CoAppMSI.GetCoAppPackageFileInformation(filename);
        }

        internal static Package GetPackage(string packageName, ulong version, Architecture architecture, string publicKeyToken, Guid? productCode) {
            return GetPackage(packageName, version, architecture.ToString(), publicKeyToken, productCode);
        }

        internal static Package GetPackage(string packageName, ulong version, string architecture, string publicKeyToken, Guid? productCode) {
            Package pkg;

            // try via just the package product code
            if (productCode != null) {
                lock (_packages) {
                    pkg = _packages.Where(package => package.ProductCode == productCode).FirstOrDefault();
                }

                if (pkg != null) {
                    // if we *have* this package somewhere, but don't know its name, 
                    // we can now fill *that* in.
                    if (string.IsNullOrEmpty(pkg.Name)) {
                        pkg.Name = packageName;
                        pkg.Architecture = architecture;
                        pkg.Version = version;
                        pkg.PublicKeyToken = publicKeyToken;
                    }
                       return pkg;
                }
            }

            lock (_packages) {
                pkg = (_packages.Where(package =>
                            package.Architecture == architecture && package.Version == version && package.PublicKeyToken == publicKeyToken &&
                                package.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();

                // we've tried finding it a couple of ways, and got back nothing for our trouble.
                // we'll create an package with the details we have, and pass that back.
                if (pkg == null) {
                    pkg = new Package(packageName, architecture, version, publicKeyToken, productCode);
                    _packages.Add(pkg);
                }
            }

            // if we did find a package and its product code was empty, we can fill that in now if we have it.
            if (productCode !=null && pkg.ProductCode == null) {
                pkg.ProductCode = productCode;
            }

            return pkg;
        }

        private Package() {
            Name = string.Empty;
            Version = 0;
            Architecture = string.Empty;
            PublicKeyToken = string.Empty;
        }

        private Package(Guid? productCode) : this() {
            ProductCode = productCode;
        }

        private Package(string name, string architecture, UInt64 version, string publicKeyToken, Guid? productCode) : this(productCode) {
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
                EnsureCanonicalFoldersArePresent();

                var currentVersion = GetCurrentPackageVersion(Name, PublicKeyToken);

                PackageHandler.Install(this, progress);
                IsInstalled = true;
                
                Logger.Error("MSI Install of package [{0}] SUCCEEDED.", CanonicalName );

                if (Version > currentVersion) {
                    SetPackageCurrent();
                    DoPackageComposition(true);
                    Logger.Error("Set Current Version [{0}] SUCCEEDED.", CanonicalName);
                }
                else {
                    DoPackageComposition(false);
                    Logger.Error("Package Composition [{0}] SUCCEEDED.", CanonicalName);
                }
                if( PackageSessionData.IsClientSpecified ) {
                    IsRequired = true;
                }
            }
            catch (Exception e) {
                Logger.Error("Package Install Failure [{0}] => [{1}].\r\n{2}", CanonicalName, e.Message, e.StackTrace);
                
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
                PackageManagerSettings.PerPackageSettings.DeleteSubkey(CanonicalName);
            }
            catch (Exception) {
                PackageManagerMessages.Invoke.FailedPackageRemoval(CanonicalName, "GS01: I'm not sure of the reason... ");
                throw new OperationCompletedBeforeResultException();
            }
            finally {
                try {
                    // this will activate the next one in line
                    GetCurrentPackageVersion(Name, PublicKeyToken);
                    // GS01: fix this to rerun package composition on prior version.
                }
                catch /* (Exception e) */ {
                    // boooo!
                    Logger.Error("failed setting active package for {0}-{1}", Name, PublicKeyToken);
                    PackageManagerSettings.PerPackageSettings.DeleteSubkey(GeneralName);
                }
            }
        }
        #endregion

        static private readonly string[] CanonicalFolders = new string[] { ".installed", ".cache", "assemblies", "x86", "x64", "bin", "powershell", "lib", "include", "etc" };
        

        internal static void EnsureCanonicalFoldersArePresent() {
            var root = PackageManagerSettings.CoAppRootDirectory;
            foreach (var path in CanonicalFolders.Select(folder => Path.Combine(root, folder)).Where(path => !Directory.Exists(path))) {
                Directory.CreateDirectory(path);
            }
            // make sure system paths are updated.
            var binPath = Path.Combine(root, "bin");
            var psPath = Path.Combine(root, "powershell");
            var changed = false;
            if (!EnvironmentUtility.SystemPath.Contains(binPath)) {
                EnvironmentUtility.SystemPath = EnvironmentUtility.SystemPath.Prepend(binPath);
                changed = true;
            }
            if (!EnvironmentUtility.PowershellModulePath.Contains(psPath)) {
                EnvironmentUtility.PowershellModulePath = EnvironmentUtility.PowershellModulePath.Prepend(psPath);
                changed = true;
            }
            if( changed ) {
                EnvironmentUtility.BroadcastChange();
            }
        }

        #region Package Composition 


        private static Lazy<Dictionary<string, string>> DefaultMacros    = new Lazy<Dictionary<string, string>>(() => {
            var root = PackageManagerSettings.CoAppRootDirectory;
            return new Dictionary<string, string>() {
                { "apps" , root },
                { "installed" , Path.Combine(root, ".installed" )},
                { "cache" , Path.Combine(root, ".cache" )},
                { "assemblies" , Path.Combine(root, "assemblies" )},
                { "x86", Path.Combine(root, "x86" )},
                { "x64", Path.Combine(root, "x64" )},
                { "bin", Path.Combine(root, "bin" )},
                { "powershell", Path.Combine(root, "powershell" )},
                { "lib", Path.Combine(root, "lib" )},
                { "include", Path.Combine(root, "include" )},
                { "etc", Path.Combine(root, "etc" )},
                { "allprograms", KnownFolders.GetFolderPath(KnownFolder.CommonPrograms)},
                
            };
        });

            /// <summary>
        /// V1 of the Variable Resolver.
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal string ResolveVariables(string text) {
            if (string.IsNullOrEmpty(text)) {
                return string.Empty;
            }

            return text.FormatWithMacros((macro) => {
                if(DefaultMacros.Value.ContainsKey(macro) ) {
                    return DefaultMacros.Value[macro];
                }

                switch( macro ) {
                    case "packagedir":
                    case "packagedirectory":
                    case "packagefolder":
                        return @"${installed}\${publishername}\${productname}-${version}-${arch}\";

                    case "publishedpackagedir":
                    case "publishedpackagedirectory":
                    case "publishedpackagefolder":
                        return @"${apps}\${productname}";

                    case "publishername":
                        return PublisherDirectory;

                    case "productname":
                        return Name;

                    case "version" :
                        return Version.UInt64VersiontoString();

                    case "arch" :
                        return Architecture;

                    case "cosmeticname" :
                        return "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant();

                }
                return null;
            });
        }

        internal void UpdateDependencyFlags() {
            foreach (var dependentPackage in InternalPackageData.Dependencies.Where(each=>!each.IsRequired)) {
                // find each dependency that is the policy-preferred version, and mark it as currentlyrequested.
                var supercedentPackage = (from supercedent in NewPackageManager.Instance.SearchForInstalledPackages(dependentPackage.Name, null, dependentPackage.Architecture, dependentPackage.PublicKeyToken)
                    where supercedent.InternalPackageData.PolicyMinimumVersion <= dependentPackage.Version && supercedent.InternalPackageData.PolicyMaximumVersion >= dependentPackage.Version
                    select supercedent).OrderByDescending(p => p.Version).FirstOrDefault();

                (supercedentPackage??dependentPackage).UpdateDependencyFlags();
            }
            // if this isn't already set, do it.
            if( !IsRequired ) {
                PackageSessionData.IsDependency = true;
            }
        }

        public IEnumerable<CompositionRule> ImplicitRules {
            get {
                foreach (var role in InternalPackageData.Roles.Select(each => each.PackageRole)) {
                    switch (role) {
                        case PackageRole.Application:
                            yield return new CompositionRule() {
                                Action = CompositionAction.SymlinkFolder,
                                Link = "${CANONICALPACKAGEDIR}",
                                Target = "${PACKAGEDIR}",
                                Category = null,
                            };
                            break;
                        case PackageRole.DeveloperLibrary:
                            break;
                        case PackageRole.Assembly:
                            break;
                        case PackageRole.SourceCode:
                            break;
                        case PackageRole.Driver:
                            break;
                        case PackageRole.Service:
                            break;
                        case PackageRole.WebApplication:
                            break;
                    }
                }
            }
        }

        public void DoPackageComposition(bool makeCurrent) {
            // GS01: if package composition fails, and we're in the middle of installing a package
            // we should roll back the package install.

            var rules = ImplicitRules.Union(PackageHandler.GetCompositionRules(this)).ToArray();

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.SymlinkFolder)) {
                var link = ResolveVariables(rule.Link).GetFullPath();
                var dir = ResolveVariables(rule.Target).GetFullPath();

                if (Directory.Exists(dir) && (makeCurrent || !Directory.Exists(link))) {
                    try {
                        Symlink.MakeDirectoryLink(link, dir);
                    }
                    catch (Exception) {
                        Logger.Error("Warning: Directory Symlink Link Failed. [{0}] => [{1}]", link, dir);
                    }
                }
            }

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.SymlinkFile)) {
                var file = ResolveVariables(rule.Target).GetFullPath();
                var link = ResolveVariables(rule.Link).GetFullPath();
                if (File.Exists(file) && (makeCurrent || !File.Exists(link))) {
                    if (!Directory.Exists(Path.GetDirectoryName(link))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(link));
                    }

                    try {
                        Symlink.MakeFileLink(link, file);
                    }
                    catch (Exception) {
                        Logger.Error("Warning: File Symlink Link Failed. [{0}] => [{1}]", link, file);
                    }
                }
            }

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.Shortcut)) {
                var target = ResolveVariables(rule.Target).GetFullPath();
                var link = ResolveVariables(rule.Link).GetFullPath();

                if (File.Exists(target) && (makeCurrent || !File.Exists(link))) {
                    if (!Directory.Exists(Path.GetDirectoryName(link))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(link));
                    }

                    ShellLink.CreateShortcut(link, target);
                }
            }

            foreach (var rule in rules.Where(r => r.Action == CompositionAction.EnvironmentVariable)) {
                var environmentVariable = ResolveVariables(rule.Link);
                var environmentValue = ResolveVariables(rule.Target);

                switch( environmentVariable.ToLower() ) {
                    case "path":
                    case "pathext":
                    case "psmodulepath":
                    case "comspec":
                    case "temp":
                    case "tmp":
                    case "username":
                    case "windir":
                    case "allusersprofile":
                    case "appdata":
                    case "commonprogramfiles":
                    case "commonprogramfiles(x86)":
                    case "commonprogramw6432":
                    case "computername":
                    case "current_cpu":
                    case "FrameworkVersion":
                    case "homedrive":
                    case "homepath":
                    case "logonserver":
                    case "number_of_processors":
                    case "os":
                    case "processor_architecture":
                    case "processor_identifier":
                    case "processor_level":
                    case "processor_revision":
                    case "programdata":
                    case "programfiles":
                    case "programfiles(x86)":
                    case "programw6432":
                    case "prompt":
                    case "public":
                    case "systemdrive":
                    case "systemroot":
                    case "userdomain":
                    case "userprofile":
                        break;

                    default:
                        EnvironmentUtility.SetSystemEnvironmentVariable(environmentVariable, environmentValue);
                        break;
                }
            }

            var view = RegistryView.System["SOFTWARE"];
            foreach (var rule in rules.Where(r => r.Action == CompositionAction.Registry)) {
                var regKey = ResolveVariables(rule.Link);
                var regValue= ResolveVariables(rule.Target);

                view[regKey].StringValue = regValue;
            }

        }

        public void UndoPackageComposition() {
            var rules = ImplicitRules.Union(PackageHandler.GetCompositionRules(this));

            foreach (var link in from rule in rules.Where(r => r.Action == CompositionAction.Shortcut)
                let target = this.ResolveVariables(rule.Target).GetFullPath()
                let link = this.ResolveVariables(rule.Link).GetFullPath()
                where ShellLink.PointsTo(link, target)
                select link) {
                link.TryHardToDeleteFile();
            }

            foreach (var link in from rule in rules.Where(r => r.Action == CompositionAction.SymlinkFile)
                let target = this.ResolveVariables(rule.Target).GetFullPath()
                let link = this.ResolveVariables(rule.Link).GetFullPath()
                where File.Exists(target) && File.Exists(link) && Symlink.IsSymlink(link) && Symlink.GetActualPath(link).Equals(target)
                select link) {
                Symlink.DeleteSymlink(link);
            }

            foreach (var link in from rule in rules.Where(r => r.Action == CompositionAction.SymlinkFolder)
                let target = this.ResolveVariables(rule.Target).GetFullPath()
                let link = this.ResolveVariables(rule.Link).GetFullPath()
                where File.Exists(target) && Symlink.IsSymlink(link) && Symlink.GetActualPath(link).Equals(target)
                select link) {
                Symlink.DeleteSymlink(link);
            }
        }


        internal static ulong GetCurrentPackageVersion(string packageName, string publicKeyToken) {
            var installedVersionsOfPackage = from pkg in NewPackageManager.Instance.InstalledPackages
                where pkg.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase) &&
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

        /// <summary>
        ///  Indicates that the client specifically requested the package, or is the dependency of a requested package
        /// </summary>
        public bool IsRequired { 
            get { return IsClientRequired || PackageSessionData.IsDependency; } 
            set { IsClientRequired = value; }
        }

        /// <summary>
        ///  Indicates that the client specifically requested the package
        /// </summary>
        public bool IsClientRequired {
            get { return PackageSessionData.PackageSettings["#Required"].BoolValue; }
            set { PackageSessionData.PackageSettings["#Required"].BoolValue = value;}
        }

        public bool IsBlocked { 
            get { return PackageSessionData.PackageSettings["#Blocked"].BoolValue; } 
            set { PackageSessionData.PackageSettings["#Blocked"].BoolValue = value; }
        }

        public void SetPackageCurrent() {
            if (!IsInstalled) {
                throw new PackageNotInstalledException(this);
            }
            var generalName = GeneralName;

            if (Version == (ulong) PackageSessionData.GeneralPackageSettings["#CurrentVersion"].LongValue) {
                return; // it's already set to the current version.
            }

            DoPackageComposition(true);

            if (0 != (ulong) PackageSessionData.GeneralPackageSettings["#CurrentVersion"].LongValue) {
                // if there isn't a forced current version, let's not force it
                PackageSessionData.GeneralPackageSettings["#CurrentVersion"].LongValue = (long) Version;
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

        public readonly List<Role> Roles = new List<Role>();
        public List<Feature> Features { get; set; }
        public List<Feature> RequiredFeatures { get; set; } 

        // public readonly List<PackageAssemblyInfo> Assemblies = new List<PackageAssemblyInfo>();
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

    /// <summary>
    /// This stores information that is really only relevant to the currently running 
    /// Session, not between sessions.
    /// 
    /// The instance of this is bound to the Session.
    /// </summary>  
    internal class PackageSessionData : NotifiesPackageManager {

        internal bool DoNotSupercede; // TODO: it's possible these could be contradictory
        internal bool UpgradeAsNeeded; // TODO: it's possible these could be contradictory
        internal bool IsClientSpecified;
        internal bool HasRequestedDownload;

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
            get { return UpgradeAsNeeded || (!IsClientSpecified && !DoNotSupercede) && IsPotentiallyInstallable; }
        }

        public bool IsPotentiallyInstallable {
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

        private RegistryView _generalPackageSettings;
        internal RegistryView GeneralPackageSettings { get {
            return
                _generalPackageSettings ?? (_generalPackageSettings = PackageManagerSettings.PerPackageSettings["{0}-{1}".format(_package.Name, _package.PublicKeyToken)]);
        }}

        private RegistryView _packageSettings;
        internal RegistryView PackageSettings { get {
            return _packageSettings ?? (_packageSettings = PackageManagerSettings.PerPackageSettings[_package.CanonicalName]);
        }}

        private int _lastProgress;
        public int DownloadProgress { get; set; }
        public int DownloadProgressDelta { get {
            var p = DownloadProgress;
            var result = p - _lastProgress;
            if (result < 0)
                return 0;

            _lastProgress = p;
            return result;
        }}
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