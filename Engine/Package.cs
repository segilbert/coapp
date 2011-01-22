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

        // Private containment
        private string cosmeticName;
        private readonly string productCode;
        private bool? isInstalled;
        private string localPackagePath;
        private Uri remoteLocation;
        private Package superceedent;

        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }
        internal bool DoNotSuperceed;

        // Causes notifications:
        internal string LocalPackagePath { 
            get {
                return localPackagePath;
            } 
            set { 
                if( value != localPackagePath ) {
                    localPackagePath = value;
                    Changed();
                }
            }
        }

        internal Uri RemoteLocation  { 
            get {
                return remoteLocation;
            } 
            set { 
                if( value != remoteLocation ) {
                    remoteLocation = value;
                    Changed();
                }
            }
        }

        internal readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();

        internal Package Superceedent {
            get {
                return superceedent;
            }
            set {
                if (value != superceedent) {
                    superceedent = value;
                    Changed();
                }
            }
        }

        internal bool ThisPackageIsNotInstallable;

        public string CosmeticName {
            get {
                return cosmeticName ??
                    (cosmeticName = "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture).ToLowerInvariant());
            }
        }

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken, string productCode) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
            this.productCode = productCode;
            Changed();
            Dependencies.CollectionChanged += (x,y) => Changed();
        }

        private static void Changed() {
            Registrar.StateCounter++;
        }

        public bool IsInstalled {
            get {
                return isInstalled ?? (isInstalled = ((Func<bool>)(() => {
                    Changed();
                    try {
                        Installer.OpenProduct(productCode).Close();
                        return true;
                    }
                    catch {
                    }
                    return false;
                }))()).Value;
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

                var dependencies = new List<Package>();

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

                    dependencies.Add(Registrar.GetPackage(name, arch, version, pkt, pkgid));

                    record = view.Fetch();
                }
                return new {Name = name, Version = version, Architecture = arch, PublicKeyToken = pkt, dependencies, packageId = pkgid};
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

        public IEnumerable<Package> DependenciesToInstall { get { return null; }}

        public void Install() {

        }

        public void ResolvePackage() {
        }

        public bool IsPackageSatisfied {
            get { return IsInstalled || !string.IsNullOrEmpty(LocalPackagePath) && RemoteLocation != null && Superceedent != null; }
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