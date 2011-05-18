//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Configuration {
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Win32;

    public class Settings {
        internal string applicationName;
        internal RegistryKey hive;

        public Settings(string name) {
            applicationName = name;
            hive = Registry.CurrentUser;
        }

        public Settings(string name, RegistryKey hive) {
            applicationName = name;
            this.hive = hive;
        }
        
        public void DeleteSubkey(string subkey) {
            RegistryKey regkey = null;
            try {
                regkey = hive.CreateSubKey(@"Software\CoApp\" + applicationName);

                if (null == regkey) {
                    return;
                }
                regkey.DeleteSubKey(subkey);
            }
            catch {
            }
            finally {
                if (null != regkey) {
                    regkey.Close();
                }
            }
        }

        public IEnumerable<string> Subkeys {
            get {
                RegistryKey regkey = null;
                try {
                    regkey = hive.OpenSubKey(@"Software\CoApp\" + applicationName);

                    if (null == regkey) {
                        return Enumerable.Empty<string>();
                    }
                    return regkey.GetSubKeyNames();
                }
                catch {
                }
                finally {
                    if (null != regkey) {
                        regkey.Close();
                    }
                }
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> SettingNames {
            get {
                return null;
            }
        }


        public object this[string keyName, string settingName] {
            get {
                RegistryKey regkey = null;
                try {
                    regkey = hive.OpenSubKey(@"Software\CoApp\" + applicationName + @"\" + keyName);

                    if (null == regkey) {
                        return null;
                    }

                    return regkey.GetValue(settingName, null);
                }
                catch {
                }
                finally {
                    if (null != regkey) {
                        regkey.Close();
                    }
                }
                return null;
            }
            set {
                RegistryKey regkey = null;
                try {
                    regkey = hive.CreateSubKey(@"Software\CoApp\" + applicationName + @"\" + keyName);

                    if (null == regkey) {
                        return;
                    }

                    if (value == null) {
                        regkey.DeleteValue(settingName);
                    }
                    else if (value is long) {
                        regkey.SetValue(settingName, value, RegistryValueKind.QWord);
                    }
                    else if (value is string[]) {
                        regkey.SetValue(settingName, value, RegistryValueKind.MultiString);
                    }
                    else {
                        regkey.SetValue(settingName, value);
                    }
                }
                catch {
                }
                finally {
                    if (null != regkey) {
                        regkey.Close();
                    }
                }
            }
        }

        public object this[string settingName] {
            get {
                RegistryKey regkey = null;
                try {
                    regkey = hive.OpenSubKey(@"Software\CoApp\" + applicationName);

                    if (null == regkey) {
                        return null;
                    }

                    return regkey.GetValue(settingName, null);
                }
                catch {
                }
                finally {
                    if (null != regkey) {
                        regkey.Close();
                    }
                }
                return null;
            }
            set {
                RegistryKey regkey = null;
                try {
                    regkey = hive.CreateSubKey(@"Software\CoApp\" + applicationName);

                    if (null == regkey) {
                        return;
                    }

                    if (value == null) {
                        regkey.DeleteValue(settingName);
                    }
                    else if (value is long) {
                        regkey.SetValue(settingName, value, RegistryValueKind.QWord);
                    }
                    else if (value is string[]) {
                        regkey.SetValue(settingName, value, RegistryValueKind.MultiString);
                    }
                    else {
                        regkey.SetValue(settingName, value);
                    }
                }
                catch {
                }
                finally {
                    if (null != regkey) {
                        regkey.Close();
                    }
                }
            }
        }
    }
}