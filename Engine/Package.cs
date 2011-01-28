//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Exceptions;
    using Extensions;
    using Microsoft.Deployment.WindowsInstaller;

    public class Package {
        // read only: 
        internal readonly string Name;
        internal readonly UInt64 Version;
        internal readonly string Architecture;
        internal readonly string PublicKeyToken;
        internal readonly string ProductCode;

        // Private containment
        private string cosmeticName;

        private bool? isInstalled;
        private bool couldNotDownload;
        private bool packageFailedInstall;
        private string localPackagePath;
        private Uri remoteLocation;
        private Package _supercedent;

        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }
        internal bool UserSpecified;
        internal bool DoNotSupercede; // TODO: it's possible these could be contradictory
        internal bool UpgradeAsNeeded; // TODO: it's possible these could be contradictory

        // Causes notifications:
        internal string LocalPackagePath {
            get { return localPackagePath; }
            set {
                if (value != localPackagePath) {
                    localPackagePath = value;
                    Changed();
                }
            }
        }

        internal Uri RemoteLocation {
            get { return remoteLocation; }
            set {
                if (value != remoteLocation) {
                    remoteLocation = value;
                    Changed();
                }
            }
        }

        internal readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();

        internal Package Supercedent {
            get { return _supercedent; }
            set {
                if (value != _supercedent) {
                    _supercedent = value;
                    // Changed();
                }
            }
        }

        internal bool ThisPackageIsNotInstallable;

        

        internal bool PackageFailedInstall {
            get { return packageFailedInstall; }
            set {
                if (packageFailedInstall != value) {
                    packageFailedInstall = value;
                    Changed();
                }
            }
        }

        public string CosmeticName {
            get {
                return cosmeticName ??
                    (cosmeticName = "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant());
            }
        }

        public bool AllowedToSupercede {
            get { return UpgradeAsNeeded || (!UserSpecified && !DoNotSupercede); }
        }

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken, string productCode) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
            this.ProductCode = productCode;
            Changed();
            Dependencies.CollectionChanged += (x, y) => Changed();
        }

        private static void Changed() {
            Registrar.Updated(); 
        }

        public bool IsInstalled {
            get {
                return isInstalled ?? (isInstalled = ((Func<bool>) (() => {
                    
                    try {
                        Installer.OpenProduct(ProductCode).Close();
                        Changed();
                        return true;
                    }
                    catch {
                    }
                    return false;
                }))()).Value;
            }
        }

        public bool CouldNotDownload {
            get { return couldNotDownload; }
            set {
                if (value != couldNotDownload) {
                    couldNotDownload = value;
                    Changed();
                }
            }
        }


        internal static dynamic GetCoAppPackageFileDetails(string localPackagePath) {
            Session installSession = null;
            try {
                installSession = Installer.OpenPackage(localPackagePath, true);
            }
            catch (InstallerException) {
                throw new InvalidPackageException(InvalidReason.NotValidMSI, localPackagePath);
            }

            try {
                var name = installSession.GetProductProperty("ProductName");
                var view =
                    installSession.Database.OpenView(installSession.Database.Tables["CO_PACKAGE"].SqlSelectString +
                        " WHERE `name` = '{0}'".format(name));
                view.Execute();
                var record = view.Fetch();
                if (record == null) {
                    throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                }

                var pkgid = record.GetString("package_id");
                var arch = record.GetString("arch");
                var version = record.GetString("version").VersionStringToUInt64();
                var pkt = record.GetString("public_key_token");
                view.Close();
                UInt64 minPolicy = 0;
                UInt64 maxPolicy = 0;

                if (installSession.Database.Tables.Contains("CO_BINDING_POLICY")) {
                    view = installSession.Database.OpenView(installSession.Database.Tables["CO_BINDING_POLICY"].SqlSelectString);
                    view.Execute();
                    record = view.Fetch();

                    minPolicy = record.GetString("minimum_version").VersionStringToUInt64();
                    maxPolicy = record.GetString("maximum_version").VersionStringToUInt64();
                    view.Close();
                }


                dynamic result =
                    new {
                        Name = name,
                        Version = version,
                        Architecture = arch,
                        PublicKeyToken = pkt,
                        packageId = pkgid,
                        policy_min_version = minPolicy,
                        policy_max_version = maxPolicy,
                        dependencies = new List<Package>()
                    };


                if (installSession.Database.Tables.Contains("CO_DEPENDENCY")) {
                    // dependencies
                    view =
                        installSession.Database.OpenView(
                            "SELECT CO_PACKAGE.package_id, CO_PACKAGE.name, CO_PACKAGE.arch, CO_PACKAGE.version, CO_PACKAGE.public_key_token, CO_DEPENDENCY.dependency_id FROM CO_PACKAGE, CO_DEPENDENCY WHERE CO_PACKAGE.package_id = CO_DEPENDENCY.dependency_id");
                    view.Execute();
                    record = view.Fetch();
                    while (record != null) {
                        pkgid = record.GetString("package_id");
                        name = record.GetString("name");
                        arch = record.GetString("arch");
                        version = record.GetString("version").VersionStringToUInt64();
                        pkt = record.GetString("public_key_token");

                        result.dependencies.Add(Registrar.GetPackage(name, arch, version, pkt, pkgid));

                        record = view.Fetch();
                    }
                }
                return result;
            }
            catch (InstallerException) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }
            finally {
                if (installSession != null) {
                    installSession.Close();
                }
            }
        }

        public void EnsureDependenciesAreUnderstood() {
            if (IsInstalled) {
                return;
            }

            if (DependenciesKnown) {
                foreach (var p in Dependencies) {
                    EnsureDependenciesAreUnderstood();
                }
            }

            // do we have a local package?
            if (IsPackageSatisfied) {
                ResolvePackage();
                if (IsPackageSatisfied) {
                    throw new PackageMissingException(Name, Architecture, Version, PublicKeyToken);
                }
            }
        }

        public IEnumerable<Package> DependenciesToInstall {
            get { return null; }
        }

        public void Install() {
            Installer.InstallProduct(localPackagePath, @"""TARGETDIR={0}"" COAPP_INSTALLED=1".format(PackageManagerSettings.CoAppRootDirectory));
            isInstalled = true;
        }

        public void ResolvePackage() {
        }

        public bool IsPackageSatisfied {
            get { return IsInstalled || !string.IsNullOrEmpty(LocalPackagePath) && RemoteLocation != null && Supercedent != null; }
        }


        public bool DependenciesKnown {
            get { return (Dependencies != null); }
        }

        public bool AllDependenciesKnown {
            get {
                if (!DependenciesKnown) {
                    return false;
                }

                return !Dependencies.Any(i => i.AllDependenciesKnown == false);
            }
        }

        public bool DependenciesSatisfied {
            get {
                if (IsInstalled) {
                    return true;
                }

                if (!DependenciesKnown) {
                    return false;
                }

                return !Dependencies.Any(p => !p.DependenciesSatisfied);
            }
        }


        /*
        public IEnumerable<Package> PackageDependencies() {
            Installer.OpenPackage(LocalPackagePath, true);
        }
         * */
    }
}