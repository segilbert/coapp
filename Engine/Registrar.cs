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

    public class Registrar {
        private static readonly ObservableCollection<Package> packages = new ObservableCollection<Package>();
        public static int StateCounter;
        static Registrar() {
            packages.CollectionChanged += (x, y) => { StateCounter++; };
        }

        public static Package GetPackage(string packageName, string architecture, UInt64 version, string publicKeyToken, string packageId) {
            var pkg = (packages.Where(package =>
                package.Architecture == architecture &&
                package.Version == version &&
                package.PublicKeyToken == publicKeyToken &&
                package.Name.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();

            if( pkg == null ) {
                pkg = new Package(packageName, architecture, version, publicKeyToken, packageId);
                packages.Add(pkg);
            }

            return pkg;
        }

        public static Package GetPackage(string packagePath) {
            var localPackagePath = Path.GetFullPath(packagePath);

            var pkg =
                (packages.Where(package => package.LocalPackagePath.Equals(localPackagePath, StringComparison.CurrentCultureIgnoreCase))).
                    FirstOrDefault();

            if (pkg != null)
                return pkg;

            if (!File.Exists(localPackagePath)) {
                throw new PackageNotFoundException(localPackagePath);
            }

            var pkgDetails = Package.GetCoAppPackageFileDetails(packagePath);

            pkg = (packages.Where(package =>
                package.Architecture == pkgDetails.Architecture &&
                    package.Version == pkgDetails.Version &&
                        package.PublicKeyToken == pkgDetails.PublicKeyToken &&
                            package.Name.Equals(pkgDetails.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
            
            if (pkg == null) {
                pkg = new Package(pkgDetails.Name, pkgDetails.Architecture, pkgDetails.Version, pkgDetails.PublicKeyToken,
                    pkgDetails.packageId);
                
                pkg.Dependencies.Clear();
                pkg.Dependencies.AddRange(pkg.Dependencies);

                packages.Add(pkg);
            }
            pkg.LocalPackagePath = localPackagePath;
            return pkg;
        }

        public static void LocateSuitablePackage(Package package) {
            // anything superceedent in the list of known packages?
            var pkgs = (packages.Where(p =>
               p.Architecture == package.Architecture &&
               p.PublicKeyToken == package.PublicKeyToken &&
               p.Name.Equals(package.Name, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
        }

    }


}