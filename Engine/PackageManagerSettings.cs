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
    using Microsoft.Win32;

    public class PackageManagerSettings {
        public static Settings userSettings = new Settings("CoAppPackageManager");
        public static Settings systemSettings = new Settings("CoAppPackageManager", Registry.LocalMachine);

        public static SettingsStringIndexer StringSetting = new SettingsStringIndexer(userSettings);
        public static SettingsStringArrayIndexer StringArraySetting = new SettingsStringArrayIndexer(userSettings);
        public static SettingsBooleanIndexer BoolSetting = new SettingsBooleanIndexer(userSettings);
        public static SettingsIntIndexer IntSetting = new SettingsIntIndexer(userSettings);
        public static SettingsLongIndexer LongSetting = new SettingsLongIndexer(userSettings);

        public static SettingsEncryptedStringIndexer EncryptedStringSetting =
            new SettingsEncryptedStringIndexer(userSettings);

        public static SettingsStringIndexer SystemStringSetting = new SettingsStringIndexer(systemSettings);
        public static SettingsStringArrayIndexer SystemStringArraySetting = new SettingsStringArrayIndexer(systemSettings);
        public static SettingsBooleanIndexer SystemBoolSetting = new SettingsBooleanIndexer(systemSettings);
        public static SettingsIntIndexer SystemIntSetting = new SettingsIntIndexer(systemSettings);
        public static SettingsLongIndexer SystemLongSetting = new SettingsLongIndexer(systemSettings);

        public static SettingsEncryptedStringIndexer SystemEncryptedStringSetting =
            new SettingsEncryptedStringIndexer(systemSettings);


        private static string DEFAULT_COAPP_ROOT {
            get { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%SystemDrive%\apps")); }
        }

        public static string CoAppRootDirectory {
            get {
                string result = SystemStringSetting["RootDirectory"];

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
                string newRootDirectory = Path.GetFullPath(value);

                string rootDirectory = SystemStringSetting["RootDirectory"];

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
                SystemStringSetting["RootDirectory"] = newRootDirectory;
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