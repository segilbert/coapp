//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.PackageFormatHandlers {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Engine;
    using Engine.Exceptions;
    using Engine.Model.Atom;
    using Extensions;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// A representation of an CoApp MSI file
    /// </summary>
    /// <remarks></remarks>
    internal class CoAppMSI : MSIBase {

        internal static CoAppMSI Instance  = new CoAppMSI();

        private CoAppMSI() {
        }

        /// <summary>
        /// Determines whether a given file is a CoApp MSI
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is co app package file] [the specified path]; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        internal static bool IsCoAppPackageFile(string path) {
            var packageProperties = GetMsiProperties(path);
            return (packageProperties.ContainsKey("CoAppCompositionRules") && packageProperties.ContainsKey("CoAppPackageFeed"));
        }

        /// <summary>
        /// Given a package filename, loads the metadata from the MSI
        /// 
        /// </summary>
        /// <param name="localPackagePath">The local package path.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static Package GetCoAppPackageFileInformation(string localPackagePath) {
            var packageProperties = GetMsiProperties(localPackagePath);

            if (!(packageProperties.ContainsKey("CoAppCompositionRules") && packageProperties.ContainsKey("CoAppPackageFeed"))) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, localPackagePath);
            }

            // pull out the rules & feed, send the info to the pm. 
            // var name = packageProperties["ProductName"];
            // var compositionRules = packageProperties["CoAppCompositionRules"];
            var atomFeedText = packageProperties["CoAppPackageFeed"];
            var productCode = new Guid( packageProperties["ProductCode"] );

            var feed = AtomFeed.Load(atomFeedText);
            var result = feed.Packages.Where(each => each.ProductCode == productCode).FirstOrDefault();
            
            if( result == null ) {
                throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, localPackagePath);
            }
            // set things that only we can do here...
            result.InternalPackageData.LocalLocation = localPackagePath;
            result.PackageHandler = Instance;

            return result;
        }

        /// <summary>
        /// Gets the package composition rules for the given package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        public override IEnumerable<CompositionRule> GetCompositionRules(Package package) {
            var packageProperties = GetMsiProperties(package.PackageSessionData.LocalValidatedLocation);

            if (!(packageProperties.ContainsKey("CoAppCompositionRules") && packageProperties.ContainsKey("CoAppPackageFeed"))) {
                throw new InvalidPackageException(InvalidReason.NotCoAppMSI, package.PackageSessionData.LocalValidatedLocation);
            }

            var compositionRulesText = packageProperties["CoAppCompositionRules"];
            if (string.IsNullOrEmpty(compositionRulesText)) {
                throw new InvalidPackageException(InvalidReason.MalformedCoAppMSI, package.PackageSessionData.LocalValidatedLocation);
            }

            return compositionRulesText.FromXml<List<CompositionRule>>("CompositionRules");
        }


        /// <summary>
        /// Installs the specified package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="progress">The progress.</param>
        /// <remarks></remarks>
        public override void Install(Package package, Action<int> progress = null) {
            lock (typeof(MSIBase)) {
                progress = progress ?? ((percent) => { });

                int currentTotalTicks = -1;
                int currentProgress = 0;
                int progressDirection = 1;
                int actualPercent = 0;

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
                                        currentProgress += msg[3]*progressDirection;
                                        break;
                                    case 3:
                                        //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                        break;
                                }
                            }

                            if (currentTotalTicks > 0) {
                                var newPercent = (currentProgress*100/currentTotalTicks);
                                if( actualPercent < newPercent) {
                                    actualPercent = newPercent;
                                    progress(actualPercent);    
                                }
                            }
                            break;
                    }
                    // capture installer messages to play back to status listener
                    return MessageResult.OK;
                }), InstallLogModes.Progress);

                try {
                    Installer.InstallProduct(package.PackageSessionData.LocalValidatedLocation,
                        @"TARGETDIR=""{0}"" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS {1}".format(PackageManagerSettings.CoAppInstalledDirectory,
                            package.PackageSessionData.IsClientSpecified ? "ADD_TO_ARP=1" : ""));
                }
                finally {
                    SetUIHandlersToSilent();
                }
            }
        }

        /// <summary>
        /// Removes the specified package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="progress">The progress.</param>
        /// <remarks></remarks>
        public override void Remove(Package package, Action<int> progress = null) {
            lock (typeof(MSIBase)) {
                progress = progress ?? ((percent) => { });
                int currentTotalTicks = -1;
                int currentProgress = 0;
                int progressDirection = 1;
                int actualPercent = 0;

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
                                    case 1: //Provides information related to progress messages to be sent by the current action.
                                        break;
                                    case 2: //Increments the progress bar.
                                        if (currentTotalTicks == -1) {
                                            break;
                                        }
                                        currentProgress += msg[3]*progressDirection;
                                        break;
                                    case 3:
                                        //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                        break;
                                }
                                if (currentTotalTicks > 0) {
                                    var newPercent = (currentProgress*100/currentTotalTicks);
                                    if (actualPercent < newPercent) {
                                        actualPercent = newPercent;
                                        progress(actualPercent);
                                    }
                                }
                            }
                            break;
                    }
                    // capture installer messages to play back to status listener
                    return MessageResult.OK;
                }), InstallLogModes.Progress);

                try {
                    Installer.InstallProduct(package.PackageSessionData.LocalValidatedLocation, @"REMOVE=ALL COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
                }
                finally {
                    SetUIHandlersToSilent();
                }
            }
        }
    }
}
