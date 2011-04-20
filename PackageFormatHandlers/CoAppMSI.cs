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
    using Microsoft.Deployment.WindowsInstaller;

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
            
            if (packageData.CO_BINDING_POLICY != null && 
                ((IEnumerable<dynamic>)packageData.CO_BINDING_POLICY).Count() == 1) {
                var policy = packageData.CO_BINDING_POLICY[0];

                minPolicy = ((string)policy.minimum_version).VersionStringToUInt64();
                maxPolicy = ((string)policy.maximum_version).VersionStringToUInt64();
            }

            string licenseText = null;
            string licenseUrl = null;

            if (packageData.CO_LICENSE != null &&
                ((IEnumerable<dynamic>)packageData.CO_BINDING_POLICY).Count() == 1)
            {
                var license = packageData.CO_LICENSE[0];
                licenseText = license.license_text;
                licenseUrl = license.license_url;
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
                    roles = new List<Tuple<PackageRole, string>>(),
                    assemblies = new Dictionary<string, PackageAssemblyInfo>(),

                    // new cosmetic metadata fields
                    displayName = properties.display_name,
                    description = StringExtensions.GunzipFromBase64(properties.description),
                    publishDate = properties.publish_date,
                    authorVersion = properties.author_version,
                    originalLocation = GetURL(packageData.CO_URLS, properties.original_location ),
                    feedLocation = GetURL(packageData.CO_URLS, properties.feed_location),
                    icon = properties.icon,
                    summary = properties.short_description,
                    publisherName = publisher.name,
                    publisherUrl = GetURL(packageData.CO_URLS,publisher.location ),
                    publisherEmail = publisher.email,
                    license = StringExtensions.GunzipFromBase64(licenseText)
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
                    result.dependencies.Add(Registrar.GetPackage(name, version, arch, pkt, pkgid));
                }
            }

            if (packageData.CO_ROLES != null) {
                var numOfSharedLibs = 0;

                foreach (var record in packageData.CO_ROLES as IEnumerable<dynamic>) {
                    PackageRole type = Enum.Parse(typeof(PackageRole), record.type.ToString(), true);
                    
                    if (type == PackageRole.SharedLib )
                        numOfSharedLibs++;

                    var role = new Tuple<PackageRole, string>(type, record.flavor);

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

        public override void Install(Package package, Action<int> progress = null) {
            progress = progress ?? ((percent) => { });

            int currentTotalTicks = -1;
            int currentProgress = 0;
            int progressDirection = 1;

            Installer.SetExternalUI(((messageType, message, buttons, icon, defaultButton) => {
                switch (messageType) {
                    case InstallMessage.Progress:
                        if (message.Length >= 2) {
                            var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).ToArray();

                            switch (msg[1]) {
                                // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                    currentTotalTicks = msg[3];
                                    currentProgress = 0;
                                    if (msg.Length >= 6) {
                                        progressDirection = msg[5] == 0 ? 1 : -1;
                                    }
                                    break;
                                case 1:
                                    //Provides information related to progress messages to be sent by the current action.
                                    break;
                                case 2: //Increments the progress bar.
                                    if (currentTotalTicks == -1) {
                                        break;
                                    }
                                    currentProgress += msg[3] * progressDirection;
                                    break;
                                case 3:
                                    //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                    break;
                            }
                        }

                        if (currentTotalTicks > 0) {
                            progress(currentProgress * 100 / currentTotalTicks);
                        }
                        break;
                }
                // capture installer messages to play back to status listener
                return MessageResult.OK;
            }), InstallLogModes.Progress);

            try {
                Installer.InstallProduct(package.LocalPackagePath,
                    @"TARGETDIR=""{0}"" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS {1}".format(PackageManagerSettings.CoAppInstalledDirectory, package.UserSpecified ? "ADD_TO_ARP=1" : ""));
            }
            finally {
                SetUIHandlersToSilent();
            }
        }

        public override void Remove(Package package, Action<int> progress = null) {
            progress = progress ?? ((percent) => { });
            int currentTotalTicks = -1;
            int currentProgress = 0;
            int progressDirection = 1;

            Installer.SetExternalUI(((messageType, message, buttons, icon, defaultButton) => {
                switch (messageType) {
                    case InstallMessage.Progress:
                        if (message.Length >= 2) {
                            var msg =
                                message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).
                                    ToArray();

                            switch (msg[1]) {
                                // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                    currentTotalTicks = msg[3];
                                    currentProgress = 0;
                                    if (msg.Length >= 6) {
                                        progressDirection = msg[5] == 0 ? 1 : -1;
                                    }
                                    break;
                                case 1: //Provides information related to progress messages to be sent by the current action.
                                    break;
                                case 2: //Increments the progress bar.
                                    if (currentTotalTicks == -1) {
                                        break;
                                    }
                                    currentProgress += msg[3] * progressDirection;
                                    break;
                                case 3:
                                    //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                    break;
                            }
                            if (currentTotalTicks > 0) {
                                progress(currentProgress * 100 / currentTotalTicks);
                            }
                        }
                        break;
                }
                // capture installer messages to play back to status listener
                return MessageResult.OK;
            }), InstallLogModes.Progress);

            try {
                Installer.InstallProduct(package.LocalPackagePath, @"REMOVE=ALL COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
            }
            finally {
                SetUIHandlersToSilent();
            }
        }

        public override IEnumerable<CompositionRule> GetCompositionRules(Package package) {
            dynamic packageData = GetDynamicMSIData(package.LocalPackagePath);

            if (packageData.CO_INSTALL_PROPERTIES == null) {
                return Enumerable.Empty<CompositionRule>();
            }
            
            return from rec in packageData.CO_INSTALL_PROPERTIES as IEnumerable<dynamic>
                   where CompositionRule.IsCompositionAction(rec.type) /* ensures the parse below always succeeds */
                        select new CompositionRule(package) {
                            Location = rec.link,
                            Target = rec.target,
                            Action = Enum.Parse(typeof (CompositionAction), rec.type, true),
                            Parameters = string.Empty
                        };
        }

    }
}
