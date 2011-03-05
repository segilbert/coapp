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
    using DynamicXml;
    using Engine;
    using Engine.Exceptions;
    using Extensions;

    internal class CoAppMSI : MSIBase {
        internal static bool IsCoAppPackageFile(string path) {
            var packageData = GetMSIData(path);
            return packageData.Tables.Contains("CO_PACKAGE");
        }

        private static string GetURL(dynamic CO_URLS, string id ) {
            if (CO_URLS == null || string.IsNullOrEmpty(id))
                return null;
            var rec = CO_URLS[id];
            return rec == null ? null : rec.url;
        }

        internal static dynamic GetCoAppPackageFileDetails(string localPackagePath) {
            dynamic packageData = GetDynamicMSIData(localPackagePath);

            if (packageData.CO_PACKAGE == null) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }
            
            string name = packageData["ProductName"];

            var newrecord = (from rec in packageData.CO_PACKAGE as IEnumerable<dynamic> where rec.Name == name select rec).FirstOrDefault();

            if (newrecord == null) {
                throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
            }

            string pkgid = newrecord.package_id;
            string arch = newrecord.arch;
            UInt64 version = ((string)newrecord.version).VersionStringToUInt64();
            string pkt = newrecord.public_key_token;

            UInt64 minPolicy = 0;
            UInt64 maxPolicy = 0;
            
            if (packageData.CO_BINDING_POLICY != null) {
                var policy = packageData.CO_BINDING_POLICY[0];

                minPolicy = ((string)policy.minimum_version).VersionStringToUInt64();
                maxPolicy = ((string)policy.maximum_version).VersionStringToUInt64();
            }

            var properties = packageData.CO_PACKAGE_PROPERTIES[pkgid];
            var publisher = packageData.CO_PUBLISHER[pkt];

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
                    assemblies = new Dictionary<string, PackageAssemblyInfo>(),

                    // new cosmetic metadata fields
                    displayName = properties.display_name,
                    description = properties.description,
                    publishDate = properties.publish_date,
                    authorVersion = properties.author_version,
                    originalLocation = GetURL(packageData.CO_URLS, properties.original_location ),
                    feedLocation = GetURL(packageData.CO_URLS, properties.feed_location),
                    icon = properties.icon,
                    summary = properties.short_description,
                    publisherName = publisher.name,
                    publisherUrl = GetURL(packageData.CO_URLS,publisher.location ),
                    publisherEmail = publisher.email,
                };

            if (packageData.CO_DEPENDENCY != null) {
                var dependencyPackageIds = from depPkg in (packageData.CO_DEPENDENCY as IEnumerable<dynamic>) select depPkg.dependency_id;

                foreach (var pak in
                    dependencyPackageIds.Select(
                        eachPackageId =>
                            (from pkg in (packageData.CO_PACKAGE as IEnumerable<dynamic>)
                             where eachPackageId == pkg.package_id
                             select pkg).FirstOrDefault())) {

                    pkgid = pak.package_id;
                    name = pak.name;
                    arch = pak.arch;
                    version = ((string)pak.version).VersionStringToUInt64();
                    pkt = pak.public_key_token;
                    result.dependencies.Add(Registrar.GetPackage(name, arch, version, pkt, pkgid));
                }
            }

            if (packageData.CO_ROLES != null) {
                var numOfSharedLibs = 0;

                foreach (var record in packageData.CO_ROLES as IEnumerable<dynamic>) {
                    var type = record.type;
                    if (type == "sharedlib")
                        numOfSharedLibs++;
                    var role = new Tuple<string, string>(type, record.flavor);

                    result.roles.Add(role);
                }

                if (numOfSharedLibs > 0) {

                    if (packageData.MsiAssembly != null && packageData.MsiAssemblyName  != null) {

                        var assms = result.assemblies;
                        var numberOfNonPolicyAssms = 0;
                        foreach (var record in packageData.MsiAssemblyName as IEnumerable<dynamic>) {

                            var componentId = record.Component_;

                            if (!assms.ContainsKey(componentId))
                                assms[componentId] = new PackageAssemblyInfo();

                            switch ((string)record.Name) {
                                case "name":
                                    assms[componentId].Name = record.Value;
                                    break;
                                case "processorArchitecture":
                                    assms[componentId].Arch = record.Value;
                                    break;
                                case "type":
                                    var type = record.Value;
                                   if (!type.Contains("policy"))
                                        numberOfNonPolicyAssms++;
                                    assms[componentId].Type = type;

                                    break;
                                case "version":
                                    assms[componentId].Version = record.Value;
                                    break;
                                case "publicKeyToken":
                                    assms[componentId].PublicKeyToken = record.Value;
                                    break;
                            }
                        }
                        /*
                        if (numberOfNonPolicyAssms < numOfSharedLibs) {
                            // you need to have at least one assembly per sharedlib);
                            throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
                        }*/
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
