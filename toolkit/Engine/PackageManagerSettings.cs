//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.IO;
    using System.Linq;
    using Configuration;
    using Exceptions;
    using Extensions;

    /// <summary>
    /// Provides access to settings of the package manager.
    /// </summary>
    /// <remarks></remarks>
    public class PackageManagerSettings {
        /// <summary>
        /// Registry view for the package manager settings
        /// </summary>
        public static RegistryView CoAppSettings = RegistryView.CoAppSystem[@"PackageManager"];
        
        /// <summary>
        /// registry view for the cached items (contents subject to being dropped at a whim)
        /// </summary>
        public static RegistryView CacheSettings = CoAppSettings[@".cache"];
        
        /// <summary>
        /// registry view for package-specific information.
        /// 
        /// This data is currently the only registry data in coapp that can't be rebuilt--this stores the "current" version of a given package.
        /// This is also where we will store flags like "blocked" or "required" 
        /// </summary>
        public static RegistryView PerPackageSettings = CoAppSettings[@".packageInformation"];

        /// <summary>
        /// Gets the default for the CoApp root folder.
        /// </summary>
        /// <remarks></remarks>
        private static string DEFAULT_COAPP_ROOT {
            get { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%SystemDrive%\apps")); }
        }

        /// <summary>
        /// Gets or sets the coapp root directory.
        /// 
        /// May only change the value if the existing directory is empty.
        /// 
        /// If the directory can not be set, this will default to the DEFAULT_COAPP_ROOT location every time.
        /// </summary>
        /// <value>The coapp root directory.</value>
        /// <remarks></remarks>
        public static string CoAppRootDirectory {
            get {
                // string result = SystemStringSetting["RootDirectory"];
                var result = CoAppSettings["#Root"].StringValue;

                if (string.IsNullOrEmpty(result)) {
                    CoAppRootDirectory = result = DEFAULT_COAPP_ROOT;
                }

                if (!Directory.Exists(result)) {
                    throw new ConfigurationException("CoApp Root Directory does not exist", "RootDirectory",
                        "The Directory [{0}] did not get created.".format(result));
                }

                return result;
            }
            set {
                var newRootDirectory = value.GetFullPath();

                var rootDirectory = CoAppSettings["#Root"].StringValue;

                if (string.IsNullOrEmpty(rootDirectory)) {
                    rootDirectory = DEFAULT_COAPP_ROOT;
                }

                if (rootDirectory.Equals(newRootDirectory, StringComparison.CurrentCultureIgnoreCase)) {
                    if (!Directory.Exists(rootDirectory)) {
                        Directory.CreateDirectory(rootDirectory);
                    }
                    return;
                }

                if (Directory.Exists(rootDirectory)) {
                    if (Directory.EnumerateFileSystemEntries(rootDirectory).Count() > 0) {
                        throw new ConfigurationException(
                            "The CoApp RootDirectory can not be changed with contents in it.", "RootDirectory",
                            "Remove contents of the existing CoApp Root Directory before changing it [{0}]".format(
                                rootDirectory));
                    }

                    Directory.Delete(rootDirectory);
                }

                if (!Directory.Exists(newRootDirectory)) {
                    Directory.CreateDirectory(newRootDirectory);
                }

                // Warning: the user might not have permissions to set this value
                CoAppSettings["#RootDirectory"].StringValue = newRootDirectory;
            }
        }

        /// <summary>
        /// Gets the CoApp .installed directory (where the packages install to)
        /// </summary>
        /// <remarks></remarks>
        public static string CoAppInstalledDirectory {
            get {
                var result = Path.Combine(CoAppRootDirectory, ".installed");
                if (!Directory.Exists(result)) {
                    Directory.CreateDirectory(result);
                    var di = new DirectoryInfo(result);
                    di.Attributes = FileAttributes.Hidden;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the co app cache directory (where transient files are located).
        /// </summary>
        /// <remarks></remarks>
        public static string CoAppCacheDirectory {
            get {
                var result = Path.Combine(CoAppRootDirectory, ".cache");
                if (!Directory.Exists(result)) {
                    Directory.CreateDirectory(result);
                    var di = new DirectoryInfo(result);
                    di.Attributes = FileAttributes.Hidden;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the coapp package cache.
        ///  
        /// Not currently used--this is where we could copy MSIs that we've installed 
        /// This may be necessary on XP, where the OS doesn't store the complete MSI.
        /// </summary>
        /// <remarks></remarks>
        public static string CoAppPackageCache {
            get {
                var result = Path.Combine(CoAppCacheDirectory, "packages");
                if (!Directory.Exists(result)) {
                    Directory.CreateDirectory(result);
                    var di = new DirectoryInfo(result);
                    di.Attributes = FileAttributes.Hidden;
                }
                return result;
            }
        }
    }
}