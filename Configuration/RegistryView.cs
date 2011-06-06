using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace CoApp.Toolkit.Configuration {
    public class RegistryView {

        private static Lazy<RegistryView> _localMachine = new Lazy<RegistryView>(() => new RegistryView(Registry.LocalMachine));
        private static Lazy<RegistryView> _classesRoot = new Lazy<RegistryView>(() => new RegistryView(Registry.ClassesRoot));
        private static Lazy<RegistryView> _user = new Lazy<RegistryView>(() => new RegistryView(Registry.CurrentUser));
        public static RegistryView LocalMachine { get { return _localMachine.Value; }}
        public static RegistryView ClassesRoot { get { return _classesRoot.Value; } }
        public static RegistryView User { get { return _user.Value; } }

        private readonly RegistryKey _rootKey;
        private readonly string _subKey;
        private readonly string _valueName;

        protected RegistryView(RegistryKey rootKey) {
            _rootKey = rootKey;
            _subKey = null;
            _valueName = null;
        }

        protected RegistryView(RegistryKey rootKey, string subKey) {
            _rootKey = rootKey;

            var p = subKey.IndexOf("#");
            _subKey = (p > -1) ? subKey.Substring(0, p) : subKey;
            _valueName = (p > -1) ? subKey.Substring(p + 1): null;
        }

        protected RegistryView(RegistryKey rootKey, string subKey, string valuename) {
            _rootKey = rootKey;
            _subKey = subKey;
            _valueName = valuename;
        }

        private RegistryKey ReadableKey {
            get {
                try {
                    return string.IsNullOrEmpty(_subKey) ? _rootKey : _rootKey.OpenSubKey(_subKey);
                }
                catch {
                    return null;
                }
            }
        }

        private RegistryKey WriteableKey {
            get {
                try {
                    return string.IsNullOrEmpty(_subKey) ? _rootKey : _rootKey.CreateSubKey(_subKey);
                }
                catch {
                    return null;
                }
            }
        }

        public IEnumerable<string> Subkeys {
            get {
                using( var key = ReadableKey) {
                    return key != null ? key.GetSubKeyNames() : Enumerable.Empty<string>();
                }
            }
        }

        public object Value {
            get {
                using (var key = ReadableKey) {
                    return key != null ? key.GetValue(_valueName, null) : null;
                }
            }
            set {
                using (var key = WriteableKey) {
                    if (key != null) {

                        if (value == null) {
                            key.DeleteValue(_valueName);
                        }
                        else if (value is long) {
                            key.SetValue(_valueName, value, RegistryValueKind.QWord);
                        }
                        else if (value is string[]) {
                            key.SetValue(_valueName, value, RegistryValueKind.MultiString);
                        }
                        else {
                            key.SetValue(_valueName, value);
                        }
                    }
                }
            }
        }

        public void DeleteSubkey(string subkey) {
            using (var key = WriteableKey) {
                if(  key != null ) {
                    try {
                        key.DeleteSubKeyTree(subkey);
                    } catch {
                    }
                }
            }
        }

        public RegistryView this[string keyName, string valueName] {
            get {
                return new RegistryView(_rootKey, keyName, valueName);
            }
        }

        public RegistryView this[string keyName] {
            get {
                return new RegistryView(_rootKey, keyName);
            }
        }
    }

  

    public static class RegistryExtensions {
        static IEnumerable<string> Subkeys( this RegistryKey key ) {
            return null;
        }
    }
}
