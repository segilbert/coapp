//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Configuration {
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

        internal object this[string settingName] {
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

                    if (value is long) {
                        regkey.SetValue(settingName, value, RegistryValueKind.QWord);
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