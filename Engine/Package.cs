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
    using Exceptions;
    using Extensions;

    using PackageFormatHandlers;
    using Shell;
    using Tasks;
    using Win32;

    public class Package : NotifiesPackageManager {
        private readonly Lazy<string> _canonicalName;
        private bool? _isInstalled;
        private PackageDetails _packageDetails;
        private InternalPackageData _internalPackageData;
        internal IPackageFormatHandler PackageHandler;

        public string CanonicalName { get { return _canonicalName.Value; } }
        public string Name { get; private set; }
        public UInt64 Version { get; private set; }
        public string Architecture { get; private set; }
        public string PublicKeyToken { get; private set; }
        public string ProductCode { get; internal set; }
        
        internal PackageDetails PackageDetails { 
            get { return _packageDetails ?? (_packageDetails = SessionCache<PackageDetails>.Value[CanonicalName]); }
        }

        internal InternalPackageData InternalPackageData {
            get { return _internalPackageData ?? (_internalPackageData = new InternalPackageData(this)); }
        }

        internal PackageSessionData PackageSessionData { get { return SessionCache<PackageSessionData>.Value[CanonicalName] ?? (SessionCache<PackageSessionData>.Value[CanonicalName] = new PackageSessionData(this)); } }

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
            set { _isInstalled = value; }
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
            _canonicalName =
                new Lazy<string>(() => "{0}-{1}-{2}-{3}".format(Name, Version.UInt64VersiontoString(), Architecture, PublicKeyToken).ToLowerInvariant());
            DropDetails();
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
            _packageDetails = null;
            SessionCache<PackageDetails>.Value.Clear(CanonicalName);
            // PackageManagerSession.Invoke.DropPackageSessionData(this);

        }

        #region Install/Remove
        public void Install(Action<int> progress = null) {
            try {
                var currentVersion = GetCurrentPackage(Name, PublicKeyToken);

                PackageHandler.Install(this, progress);
                _isInstalled = true;


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
                _isInstalled = false;
                throw new PackageInstallFailedException(this);
            }
        }

        public void Remove(Action<int> progress = null) {
            try {
                UndoPackageComposition();
                PackageHandler.Remove(this, progress);
                _isInstalled = false;

                // this will activate the next one in line
                GetCurrentPackage(Name, PublicKeyToken);
            }
            catch (Exception) {
                NewPackageManagerMessages.Invoke.FailedPackageRemoval(CanonicalName, "GS01: I'm not sure of the reason... ");
                throw new OperationCompletedBeforeResultException();
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
        private string _canonicalPackageLocation;
        private string _canonicalFeedLocation;
        private Package _package;
         
        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }

        /// <summary>
        /// the tuple is: (role name, flavor)
        /// </summary>
        public readonly List<Tuple<PackageRole, string>> Roles = new List<Tuple<PackageRole, string>>();
        public readonly List<PackageAssemblyInfo> Assemblies = new List<PackageAssemblyInfo>();

        public readonly MultiplexedProperty<string> FeedLocation = new MultiplexedProperty<string>((x, y) => Changed());
        public readonly MultiplexedProperty<Uri> RemoteLocation = new MultiplexedProperty<Uri>((x, y) => Changed());
        public readonly MultiplexedProperty<string> LocalPackagePath = new MultiplexedProperty<string>((x, y) => Changed(), false);
        public readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();


        public string CanonicalPackageLocation {
            get { return _canonicalPackageLocation; }
            set {
                _canonicalPackageLocation = value;
                try {
                    RemoteLocation.Add(new Uri(value));
                }
                catch {
                }
            }
        }

        public string CanonicalFeedLocation {
            get { return _canonicalFeedLocation; }
            set {
                _canonicalFeedLocation = value;
                FeedLocation.Add(value);
            }
        }

        public string CanonicalSourcePackageLocation { get; set; }
        
        internal InternalPackageData(Package package) {
            _package = package;
             Dependencies.CollectionChanged += (x, y) => Changed();
        }

        public bool IsPackageSatisfied {
            get { return _package.IsInstalled || !string.IsNullOrEmpty(LocalPackagePath) && RemoteLocation != null && _package.PackageSessionData.Supercedent != null; }
        }

        
        public bool CanSatisfy { get; set; }

        public bool HasLocalFile {
            get {
                if (string.IsNullOrEmpty(LocalPackagePath) && File.Exists(LocalPackagePath))
                    return true;

                return LocalPackagePath.Any(location => File.Exists(location));
            }
        }

        public bool HasRemoteLocation {
            get { return RemoteLocation.Value != null; }
        }


        internal List<Func<Package, bool>> RetrievePackageDetails = new List<Func<Package, bool>>();
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

        public Party Publisher { get; set; }
        public IEnumerable<Party> Contributors { get; set; }

        public string CopyrightStatement { get; set; }
        public string AuthorVersion { get; set; }

        public IEnumerable<string> Tags { get; set; }
        public string FullDescription { get; set; }
        public string Base64IconData { get; set; }

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

        private bool _couldNotDownload;
        private Package _supercedent;
        private bool _packageFailedInstall;
        private Package _package;

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
            get { return UpgradeAsNeeded || (!UserSpecified && !DoNotSupercede); }
        }

        public bool PotentiallyInstallable {
            get {
                if (CouldNotDownload || PackageFailedInstall) {
                    return false;
                }
                return (!string.IsNullOrEmpty(_package.InternalPackageData.LocalPackagePath) || _package.InternalPackageData.RemoteLocation != null);
            }
        }
    }

    public class NotifiesPackageManager {
        internal static void Changed() {
            // notify the Registrar that a change has occured in a package.
            NewPackageManager.Instance.Updated();
        }
    }
}