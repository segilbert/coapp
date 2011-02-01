//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
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
    using Microsoft.Deployment.WindowsInstaller;

    public class Package {
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

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken, string productCode) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
            ProductCode = productCode;
            Changed();
            Dependencies.CollectionChanged += (x, y) => Changed();
        }

        
        

        // set once only:
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }
        

        // Causes notifications:
        public string LocalPackagePath {
            get { return _localPackagePath; }
            set {
                if (value != _localPackagePath) {
                    _localPackagePath = value;
                    Changed();
                }
            }
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
                        Installer.OpenProduct(ProductCode).Close();
                        Changed();
                        return true;
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

        private static void Changed() {
            Registrar.Updated();
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
                if(!installSession.Database.Tables.Contains("CO_PACKAGE")) {
                    throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
                }

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
                    new
                        {
                            Name = name,
                            Version = version,
                            Architecture = arch,
                            PublicKeyToken = pkt,
                            packageId = pkgid,
                            policy_min_version = minPolicy,
                            policy_max_version = maxPolicy,
                            dependencies = new List<Package>(),
                            // type and flavor
                            roles = new List<Tuple<string, string>>(),
                            assemblies = new Dictionary<string, PackageAssemblyInfo>()
                                            
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

                if (installSession.Database.Tables.Contains("CO_ROLES"))
                {
                    view = installSession.Database.OpenView("SELECT * FROM CO_ROLES");
                    view.Execute();
                    record = view.Fetch();

                    if (record == null)
                        // you need at least ONE role
                        throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);

                    //we need to know how many shared libs we have so we'll count them here
                    var numOfSharedLibs = 0;
                    for (; record != null; record = view.Fetch() )

                    {
                        var type = record.GetString("type");
                        if (type == "sharedlib")
                            numOfSharedLibs++;
                        var role = new Tuple<string, string>(type, record.GetString("flavor"));
                                      
                        result.roles.Add(role);
                    }

                 
                    
                    if (numOfSharedLibs > 0)
                    {

                        if (installSession.Database.Tables.Contains("MsiAssembly") && installSession.Database.Tables.Contains("MsiAssemblyName"))
                        {
                          

                            view =
                                installSession.Database.OpenView(
                                    "Select * FROM MsiAssemblyName");
                            view.Execute();
                            var assms = result.assemblies;
                            var numberOfNonPolicyAssms = 0;
                            for (record = view.Fetch(); record != null; record = view.Fetch())
                            {
                                var componentId = record.GetString("Component_");

                                if (!assms.ContainsKey(componentId))
                                    assms[componentId] = new PackageAssemblyInfo();
                                
                                switch (record.GetString("Name"))
                                {
                                    case "name": assms[componentId].Name = record.GetString("Value");
                                        break;
                                    case "processorArchitecture": assms[componentId].Arch = record.GetString("Value");
                                        break;
                                    case "type":
                                        var type = record.GetString("Value");
                                        if (!type.Contains("policy"))
                                            numberOfNonPolicyAssms++;
                                        assms[componentId].Type = type;

                                        break;
                                    case "version":
                                        assms[componentId].Version = record.GetString("Value");
                                        break;
                                    case "publicKeyToken":
                                        assms[componentId].PublicKeyToken = record.GetString("Value");
                                        break;
                                }

                            }

                            if (numberOfNonPolicyAssms != numOfSharedLibs)
                            {
                                
                                // you need to have one MSI per sharedlib);
                                throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                            }   
                            
                        }
                        else
                        {
                            // you have shared libs but no MsiAssembly and/or no MsiAssembly Name. That's what shared libs are.
                            throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                        }
                    }
                }
                else
                {
                    // you need to have a ROLE TABLE!
                    throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                    
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

        public void Install() {
            try {
                Installer.InstallProduct(_localPackagePath,
                    @"TARGETDIR=""{0}"" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS".format(PackageManagerSettings.CoAppInstalledDirectory));
                _isInstalled = true;
                // if( Installer.RebootInitiated || Installer.RebootRequired )

            }
            catch {
                throw new PackageInstallFailedException(this);
            }
        }

        public void Remove() {
            try {
                Installer.InstallProduct(_localPackagePath, @"REMOVE=ALL COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
                _isInstalled = false;
            }
            catch {
                throw new PackageRemoveFailedException(this);
            }
        }

    }


}