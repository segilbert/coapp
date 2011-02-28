//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.PackageFormatHandlers {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Data;
    using Engine;
    using Engine.Exceptions;
    using Extensions;

    internal class CoAppMSI : MSIBase {
        internal static bool IsCoAppPackageFile(string path) {
            var packageData = GetMSIData(path);
            return packageData.Tables.Contains("CO_PACKAGE");
        }

        internal static dynamic GetCoAppPackageFileDetails(string localPackagePath) {
            var packageData = GetMSIData(localPackagePath);
            if (!packageData.Tables.Contains("CO_PACKAGE")) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }

            var name = packageData.GetProperty("ProductName");

            var newrecord =
                (from rec in packageData.GetTable("CO_PACKAGE") where rec.Field<string>("Name") == name select rec).FirstOrDefault();

            if (newrecord == null) {
                throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
            }

            var pkgid = newrecord.Field<string>("package_id");
            var arch = newrecord.Field<string>("arch");
            var version = newrecord.Field<string>("version").VersionStringToUInt64();
            var pkt = newrecord.Field<string>("public_key_token");

            UInt64 minPolicy = 0;
            UInt64 maxPolicy = 0;

            if (packageData.Tables.Contains("CO_BINDING_POLICY")) {
                var policy = packageData.GetTable("CO_BINDING_POLICY").FirstOrDefault();

                minPolicy = policy.Field<string>("minimum_version").VersionStringToUInt64();
                maxPolicy = policy.Field<string>("maximum_version").VersionStringToUInt64();
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
                    dependencies = new List<Package>(),
                    // type and flavor
                    roles = new List<Tuple<string, string>>(),
                    assemblies = new Dictionary<string, PackageAssemblyInfo>()

                };

            if (packageData.Tables.Contains("CO_DEPENDENCY")) {
                var dependencyPackageIds = from depPkg in packageData.GetTable("CO_DEPENDENCY") select depPkg.Field<string>("dependency_id");
                foreach (var pak in
                    dependencyPackageIds.Select(
                        eachPackageId =>
                            (from pkg in packageData.GetTable("CO_PACKAGE")
                             where eachPackageId == pkg.Field<string>("package_id")
                             select pkg).FirstOrDefault())) {
                    pkgid = pak.Field<string>("package_id");
                    name = pak.Field<string>("name");
                    arch = pak.Field<string>("arch");
                    version = pak.Field<string>("version").VersionStringToUInt64();
                    pkt = pak.Field<string>("public_key_token");
                    result.dependencies.Add(Registrar.GetPackage(name, arch, version, pkt, pkgid));
                }
            }

            if (packageData.Tables.Contains("CO_ROLES")) {
                var numOfSharedLibs = 0;

                foreach (var record in packageData.GetTable("CO_ROLES")) {
                    var type = record.Field<string>("type");
                    if (type == "sharedlib")
                        numOfSharedLibs++;
                    var role = new Tuple<string, string>(type, record.Field<string>("flavor"));

                    result.roles.Add(role);
                }

                if (numOfSharedLibs > 0) {

                    if (packageData.Tables.Contains("MsiAssembly") &&
                        packageData.Tables.Contains("MsiAssemblyName")) {

                        var assms = result.assemblies;
                        var numberOfNonPolicyAssms = 0;
                        foreach (var record in packageData.GetTable("MsiAssemblyName")) {

                            var componentId = record.Field<string>("Component_");

                            if (!assms.ContainsKey(componentId))
                                assms[componentId] = new PackageAssemblyInfo();

                            switch (record.Field<string>("Name")) {
                                case "name":
                                    assms[componentId].Name = record.Field<string>("Value");
                                    break;
                                case "processorArchitecture":
                                    assms[componentId].Arch = record.Field<string>("Value");
                                    break;
                                case "type":
                                    var type = record.Field<string>("Value");
                                   if (!type.Contains("policy"))
                                        numberOfNonPolicyAssms++;
                                    assms[componentId].Type = type;

                                    break;
                                case "version":
                                    assms[componentId].Version = record.Field<string>("Value");
                                    break;
                                case "publicKeyToken":
                                    assms[componentId].PublicKeyToken = record.Field<string>("Value");
                                    break;
                            }
                        }

                        if (numberOfNonPolicyAssms < numOfSharedLibs) {
                            // you need to have at least one assembly per sharedlib);
                            throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                        }
                    }
                    else {
                        // you have shared libs but no MsiAssembly and/or no MsiAssembly Name. That's what shared libs are.
                        throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                    }
                }
            }
            else {
                // you need to have a ROLE TABLE!
                throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
            }

            return result;
        }
    }
}
