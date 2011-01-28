//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exceptions;
    using Extensions;

    public static class PackageExtensions {
        public static IEnumerable<Package> Match(this IEnumerable<Package> packageSet, string wildcardMatch) {
            return from p in packageSet where p.CosmeticName.IsWildcardMatch(wildcardMatch) select p;
        }

        public static IEnumerable<Package> HighestPackages(this IEnumerable<Package> packageSet ) {
            if (packageSet.Count() > 1) {
                var names = (from p in packageSet group p by new { p.Name, p.Architecture });
                return names.Select(each => (from package in packageSet
                                             from uniq in names
                                             where package.Name == each.Key.Name && package.Architecture == each.Key.Architecture
                                             select package).OrderByDescending(p => p.Version).FirstOrDefault());
            }
            return packageSet;
        }

        public static IEnumerable<Package> SupercedentPackages(this IEnumerable<Package> packageSet, Package package) {
            // anything superceedent in the list of known packages?
            return packageSet.Where(p => p.Architecture == package.Architecture &&
                p.PublicKeyToken == package.PublicKeyToken &&
                p.Name.Equals(package.Name, StringComparison.CurrentCultureIgnoreCase) &&
                p.PolicyMinimumVersion <= package.Version &&
                p.PolicyMaximumVersion >= package.Version).OrderByDescending(p => p.Version);
        }


        public static IEnumerable<Package> GetPackagesByName(this IEnumerable<Package> packageSet, IEnumerable<string> packages, bool scanForPackagesIfNeeded, bool justHighestMatch) {
            var packageFiles = new List<Package>();
            var unknownPackages = new List<string>();

            foreach (var pf in packages) {
                try {
                    packageFiles.Add(Registrar.GetPackage(pf));
                }
                catch (PackageNotFoundException e) {
                    unknownPackages.Add(pf);
                }
            }

            if (unknownPackages.Count > 0) {
                if (scanForPackagesIfNeeded)
                    Registrar.ScanForPackages();

                foreach (var pf in unknownPackages) {
                    var possibleMatches = packageSet.Match(pf + (pf.Contains("*") || pf.Contains("-") ? "*" : "-*"));
                    if (justHighestMatch)
                        possibleMatches = possibleMatches.HighestPackages();

                    if (possibleMatches.Count() == 0)
                        throw new PackageNotFoundException(pf);
                    if (possibleMatches.Count() == 1) {
                        packageFiles.Add(possibleMatches.First());
                    }
                    else {
                        throw new MultiplePackagesMatchException(pf, possibleMatches);
                    }
                }
            }
            return packageFiles;
        }


        private static string trimto(string s, int sz) {
            return s.Length < sz ? s : s.Substring(s.Length - sz);
        }

        public static void Dump(this IEnumerable<Package> pkgs) {
            string fmt = "|{0,35}|{1,20}|{2,5}|{3,20}|{4,8}|{5,20}|";
            string line = "--------------------------------------------------------";
            Console.WriteLine(fmt, "Filename", "Name", "Arch", "Version", "Key", "GUID");
            Console.WriteLine(fmt, trimto(line, 35), trimto(line, 20), trimto(line, 5), trimto(line, 20), trimto(line, 8), trimto(line, 20));

            foreach (var p in pkgs) {
                
                Console.WriteLine(fmt, trimto(p.LocalPackagePath ?? "(unknown)", 35), trimto(p.Name, 20), p.Architecture,
                    p.Version.UInt64VersiontoString(), trimto(p.PublicKeyToken, 8), trimto(p.ProductCode, 20));
            }
            Console.WriteLine(fmt, trimto(line, 35), trimto(line, 20), trimto(line, 5), trimto(line, 20), trimto(line, 8), trimto(line, 20));
            Console.WriteLine("\r\n");
        }
    }
}
