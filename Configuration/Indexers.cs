//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using CoApp.Toolkit.Extensions;

namespace CoApp.Toolkit.Configuration {
    using System.Collections.Generic;
    using System.Linq;

    public class SettingsIndexer {
        protected Settings quickSettings;
    }

    public class SettingsStringIndexer : SettingsIndexer {
        public SettingsStringIndexer(Settings settings) {
            quickSettings = settings;
        }
        public string this[string settingName] {
            get { return quickSettings[settingName] as string ?? string.Empty; }
            set { quickSettings[settingName] = (value ?? string.Empty); }
        }
    }

    public class SettingsStringArrayIndexer : SettingsIndexer {
        public SettingsStringArrayIndexer(Settings settings) {
            quickSettings = settings;
        }
        public IEnumerable<string> this[string settingName] {
            get {
                object data = quickSettings[settingName];
                if( data == null ) {
                    return Enumerable.Empty<string>();
                }
                if( data is string ) {
                    return new[] {data as string};
                }
                if( data is string[] ) {
                    return data as string[];
                }
                return Enumerable.Empty<string>();;
            }
            set {
                quickSettings[settingName] = value.ToArray();
            }
        }
    }

    public class SettingsEncryptedStringIndexer : SettingsIndexer {
        public SettingsEncryptedStringIndexer(Settings settings) {
            quickSettings = settings;
        }
        public string this[string settingName] {
            get {
                var bytes = quickSettings[settingName] as byte[];
                return bytes.Unprotect();
            }
            set {
                quickSettings[settingName] = value.Protect();
            }
        }
    }

    public class SettingsBooleanIndexer : SettingsIndexer {
        public SettingsBooleanIndexer(Settings settings) {
            quickSettings = settings;
        }
        public bool this[string settingName] {
            get {
                object value = quickSettings[settingName] as string ?? string.Empty;
                return (value.ToString().IsTrue() || value.ToString().Equals("1"));
            }
            set { quickSettings[settingName] = (value ? "true" : "false"); }
        }
    }

    public class SettingsIntIndexer : SettingsIndexer {
        public SettingsIntIndexer(Settings settings) {
            quickSettings = settings;
        }
        public int this[string settingName] {
            get {
                int result = 0;
                object value = quickSettings[settingName];

                if (value is int) {
                    return (int)value;
                }

                if (value is string) {
                    Int32.TryParse(value as string, out result);
                }

                return result;
            }
            set { quickSettings[settingName] = value; }
        }
    }

    public class SettingsLongIndexer : SettingsIndexer {
        public SettingsLongIndexer(Settings settings) {
            quickSettings = settings;
        }
        public long this[string settingName] {
            get {
                long result = 0;
                object value = quickSettings[settingName];

                if (value is long || value is int) {
                    return (long)value;
                }

                if (value is string) {
                    Int64.TryParse(value as string, out result);
                }

                return result;
            }
            set { quickSettings[settingName] = value; }
        }
    }
}