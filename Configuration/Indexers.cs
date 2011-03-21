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
        protected Settings _settings;
    }

    public class SettingsStringIndexer : SettingsIndexer {
        public SettingsStringIndexer(Settings settings) {
            _settings = settings;
        }
        public string this[string settingName] {
            get { return _settings[settingName] as string ?? string.Empty; }
            set { _settings[settingName] = (value ?? string.Empty); }
        }

        public string this[string keyName, string settingName] {
            get { return _settings[keyName, settingName] as string ?? string.Empty; }
            set { _settings[keyName, settingName] = (value ?? string.Empty); }
        }
    }

    public class SettingsStringArrayIndexer : SettingsIndexer {
        public SettingsStringArrayIndexer(Settings settings) {
            _settings = settings;
        }
        public IEnumerable<string> this[string settingName] {
            get {
                object data = _settings[settingName];
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
                _settings[settingName] = value.ToArray();
            }
        }

        public IEnumerable<string> this[string keyName, string settingName] {
            get {
                object data = _settings[keyName, settingName];
                if (data == null) {
                    return Enumerable.Empty<string>();
                }
                if (data is string) {
                    return new[] { data as string };
                }
                if (data is string[]) {
                    return data as string[];
                }
                return Enumerable.Empty<string>();
                ;
            }
            set {
                _settings[keyName, settingName] = value.ToArray();
            }
        }
    }

    public class SettingsEncryptedStringIndexer : SettingsIndexer {
        public SettingsEncryptedStringIndexer(Settings settings) {
            _settings = settings;
        }
        public string this[string settingName] {
            get {
                var bytes = _settings[settingName] as byte[];
                return bytes.Unprotect();
            }
            set {
                _settings[settingName] = value.Protect();
            }
        }
        public string this[string keyName, string settingName] {
            get {
                var bytes = _settings[keyName, settingName] as byte[];
                return bytes.Unprotect();
            }
            set {
                _settings[keyName, settingName] = value.Protect();
            }
        }
    }

    public class SettingsBooleanIndexer : SettingsIndexer {
        public SettingsBooleanIndexer(Settings settings) {
            _settings = settings;
        }
        public bool this[string settingName] {
            get {
                object value = _settings[settingName] as string ?? string.Empty;
                return (value.ToString().IsTrue() || value.ToString().Equals("1"));
            }
            set { _settings[settingName] = (value ? "true" : "false"); }
        }

        public bool this[string keyName, string settingName] {
            get {
                object value = _settings[keyName , settingName] as string ?? string.Empty;
                return (value.ToString().IsTrue() || value.ToString().Equals("1"));
            }
            set { _settings[keyName, settingName] = (value ? "true" : "false"); }
        }
    }

    public class SettingsIntIndexer : SettingsIndexer {
        public SettingsIntIndexer(Settings settings) {
            _settings = settings;
        }
        public int this[string settingName] {
            get {
                int result = 0;
                object value = _settings[settingName];

                if (value is int) {
                    return (int)value;
                }

                if (value is string) {
                    Int32.TryParse(value as string, out result);
                }

                return result;
            }
            set { _settings[settingName] = value; }
        }
        public int this[string keyName, string settingName] {
            get {
                int result = 0;
                object value = _settings[keyName, settingName];

                if (value is int) {
                    return (int)value;
                }

                if (value is string) {
                    Int32.TryParse(value as string, out result);
                }

                return result;
            }
            set { _settings[keyName, settingName] = value; }
        }
    }

    public class SettingsLongIndexer : SettingsIndexer {
        public SettingsLongIndexer(Settings settings) {
            _settings = settings;
        }
        public long this[string settingName] {
            get {
                long result = 0;
                object value = _settings[settingName];

                if (value is long || value is int) {
                    return (long)value;
                }

                if (value is string) {
                    Int64.TryParse(value as string, out result);
                }

                return result;
            }
            set { _settings[settingName] = value; }
        }
        public long this[string keyName, string settingName] {
            get {
                long result = 0;
                object value = _settings[keyName, settingName];

                if (value is long || value is int) {
                    return (long)value;
                }

                if (value is string) {
                    Int64.TryParse(value as string, out result);
                }

                return result;
            }
            set { _settings[keyName, settingName] = value; }
        }
    }
}