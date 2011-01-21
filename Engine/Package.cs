//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Exceptions;
    using Extensions;
    using Microsoft.Deployment.WindowsInstaller;
    using Properties;

    public class Package {
        internal readonly string Name;
        internal readonly UInt64 Version;
        internal readonly string Architecture;
        internal readonly string PublicKeyToken;

        internal string LocalPackagePath { get; set; }

        internal Uri RemoteLocation { get; set; }
        internal List<Package> Dependencies;
        internal Package Superceedent;
        internal UInt64 PolicyMinimumVersion { get; set; }
        internal UInt64 PolicyMaximumVersion { get; set; }

        internal Package(string name, string architecture, UInt64 version, string publicKeyToken ) {
            Name = name;
            Version = version;
            Architecture = architecture;
            PublicKeyToken = publicKeyToken;
        }

        internal static dynamic GetCoAppPackageFileDetails(string localPackagePath) {
            Session installSession = null;
            try {
                installSession = Installer.OpenPackage(localPackagePath, true);
            }
            catch (InstallerException) {
                throw new InvalidPackageException(InvalidReason.NotValidMSI, localPackagePath );
            }

            try {
                var name = installSession.GetProductProperty("ProductName");
                var view = installSession.Database.OpenView(installSession.Database.Tables["CO_PACKAGE"].SqlSelectString +  " WHERE `name` = '{0}'".format(name));
                view.Execute();
                var record = view.Fetch();
                if( record == null )
                    throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath );

                var pkgid = record.GetString("package_id");
                var arch = record.GetString("arch");
                var version = record.GetString("version").VersionStringToUInt64();
                var pkt = record.GetString("public_key_token");
                view.Close();

                var dependencies = new List<Package>();

                // dependencies
                view = installSession.Database.OpenView("SELECT CO_PACKAGE.package_id, CO_PACKAGE.name, CO_PACKAGE.arch, CO_PACKAGE.version, CO_PACKAGE.public_key_token, CO_DEPENDENCY.dependency_id FROM CO_PACKAGE, CO_DEPENDENCY WHERE CO_PACKAGE.package_id = CO_DEPENDENCY.dependency_id");
                view.Execute();
                record = view.Fetch();
                while( record != null ) {
                    pkgid = record.GetString("package_id");
                    name = record.GetString("name");
                    arch = record.GetString("arch");
                    version = record.GetString("version").VersionStringToUInt64();
                    pkt = record.GetString("public_key_token");

                    dependencies.Add(Registrar.GetPackage(name, arch, version, pkt));

                    record = view.Fetch();
                }
                return new { Name = name, Version = version, Architecture = arch, PublicKeyToken = pkt,dependencies = dependencies};
            
            }
            catch (InstallerException) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath );
            }
            finally {
                if(installSession != null)
                    installSession.Close();
            }
            
        }
        
        

        public void EnsureDependenciesAreUnderstood() {
            if (IsInstalled)
                return; 

            if(DependenciesKnown) {
                foreach(var p in Dependencies)
                    EnsureDependenciesAreUnderstood();
            }
            
            // do we have a local package?
            if (IsPackageSatisfied) {
                ResolvePackage();
                if (IsPackageSatisfied) {
                    throw new PackageMissingException(Name,Architecture, Version, PublicKeyToken);
                }
            }
        }

        public void ResolvePackage() {
            
        }

        public bool IsPackageSatisfied { 
            get {
                return IsInstalled || !string.IsNullOrEmpty(LocalPackagePath) && RemoteLocation != null && Superceedent != null;
            } 
        }

        public bool IsInstalled { 
            get { return false; }  
        }

        public bool DependenciesKnown {
            get {
                return (Dependencies != null);
            }
        }

        public bool AllDependenciesKnown {
            get {
                if (!DependenciesKnown)
                    return false;

                return !Dependencies.Any(i => i.AllDependenciesKnown == false);
            }
        }

        public bool DependenciesSatisfied {
            get {
                if (IsInstalled)
                    return true;

                if (!DependenciesKnown)
                    return false;

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