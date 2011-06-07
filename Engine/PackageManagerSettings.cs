//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.IO;
    using System.Linq;
    using Configuration;
    using Exceptions;
    using Extensions;

    public class PackageManagerSettings {
        public static RegistryView CoAppSettings = RegistryView.CoAppSystem[@"CoAppPackageManager"];
        public static RegistryView CacheSettings = CoAppSettings[@".cache"];
        public static RegistryView PerPackageSettings = CoAppSettings[@".packageInformation"];

        private static string DEFAULT_COAPP_ROOT {
            get { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%SystemDrive%\apps")); }
        }

        public static string CoAppRootDirectory {
            get {
                // string result = SystemStringSetting["RootDirectory"];
                var result = CoAppSettings["#RootDirectory"].StringValue;

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

                var rootDirectory = CoAppSettings["#RootDirectory"].StringValue;

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

        public static string CoAppFeedCache {
            get {
                var result = Path.Combine(CoAppCacheDirectory, "feeds");
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