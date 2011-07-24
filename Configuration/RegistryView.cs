namespace CoApp.Toolkit.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extensions;
    using Microsoft.Win32;

    public class RegistryView {
        private static readonly Lazy<RegistryView> _classesRoot = new Lazy<RegistryView>(() => new RegistryView(RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, Microsoft.Win32.RegistryView.Registry64)));
        private static readonly Lazy<RegistryView> _system = new Lazy<RegistryView>(() => new RegistryView(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64)));
        private static readonly Lazy<RegistryView> _user = new Lazy<RegistryView>(() => new RegistryView(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64)));
        private static readonly Lazy<RegistryView> _coappSystem= new Lazy<RegistryView>(() => System[@"Software\CoApp\"]);
        private static readonly Lazy<RegistryView> _coappUser = new Lazy<RegistryView>(() => User[@"Software\CoApp\"]);
        private static readonly Lazy<RegistryView> _applicationSystem = new Lazy<RegistryView>(() => CoAppSystem[Assembly.GetEntryAssembly().Title()]);
        private static readonly Lazy<RegistryView> _applicationUser = new Lazy<RegistryView>(() => CoAppUser[Assembly.GetEntryAssembly().Title()]);

        public static RegistryView System {
            get { return _system.Value; }
        }
        public static RegistryView User {
            get { return _user.Value; }
        }

        public static RegistryView CoAppSystem {
            get { return _coappSystem.Value; }
        }

        public static RegistryView CoAppUser {
            get { return _coappUser.Value; }
        }

        public static RegistryView ApplicationSystem {
            get { return _applicationSystem.Value; }
        }
        public static RegistryView ApplicationUser{
            get { return _applicationUser.Value; }
        }

        public static RegistryView ClassesRoot {
            get { return _classesRoot.Value; }
        }

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
            _subKey = ( (p > -1) ? subKey.Substring(0, p) : subKey).Trim('\\');
            _valueName = (p > -1) ? subKey.Substring(p + 1).Trim('#'): null;
        }

        protected RegistryView(RegistryKey rootKey, string subKey, string valuename) {
            _rootKey = rootKey;
            _subKey = subKey.Trim('\\');
            _valueName = valuename.Trim('#');
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
                using (var key = ReadableKey) {
                    return key != null ? key.GetSubKeyNames() : Enumerable.Empty<string>();
                }
            }
        }


        public string StringValue {
            get { return Value as string ?? string.Empty; }
            set { Value = value; }
        }

        public int IntValue {
            get {
                var result = 0;
                var value = Value;

                if (value is long || value is int) {
                    return (int) value;
                }

                if (value is string) {
                    Int32.TryParse(value as string, out result);
                }

                return result;
            }
            set { Value = value; }
        }

        public long LongValue {
            get {
                long result = 0;
                var value = Value;

                if (value is long || value is int) {
                    return (long) value;
                }

                if (value is string) {
                    Int64.TryParse(value as string, out result);
                }

                return result;
            }
            set { Value = value; }
        }

        private static T ParseEnum<T>(string value, T defaultValue = default(T)) {
            if (Enum.IsDefined(typeof(T), value)) {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            return defaultValue;
        }

        private static T CastToEnum<T>(string value) {
            if (value.Contains("+")) {
                var values = value.Split('+');
                Type numberType = Enum.GetUnderlyingType(typeof(T));
                if (numberType.Equals(typeof(int))) {
                    var newResult = values.Aggregate(0, (current, val) => current | (int) (Object) ParseEnum<T>(val));
                    return (T)(Object)newResult;
                }
            }
            return ParseEnum<T>(value);
        }

        private static string CastToString<T>(T value) {
            return Enum.Format(typeof(T), value, "G").Replace(", ", "+");
        }

        public T GetEnumValue<T>() {
            return CastToEnum<T>(StringValue);
        }

        public void SetEnumValue<T>(T value ) {
            StringValue = CastToString(value);
        }

        public string EncryptedStringValue {
            get {
                return _rootKey == Registry.CurrentUser
                    ? (Value as IEnumerable<byte>).UnprotectForUser()
                    : (Value as IEnumerable<byte>).UnprotectForMachine();
            }
            set { Value = _rootKey == Registry.CurrentUser ? value.ProtectForUser() : value.ProtectForMachine(); }
        }

        public IEnumerable<byte> EncryptedBinaryValue {
            get {
                return _rootKey == Registry.CurrentUser
                    ? (Value as IEnumerable<byte>).UnprotectBinaryForUser()
                    : (Value as IEnumerable<byte>).UnprotectBinaryForMachine();
            }
            set { Value = _rootKey == Registry.CurrentUser ? value.ProtectBinaryForUser() : value.ProtectBinaryForMachine(); }
        }

        public bool BoolValue {
            get {
                object value = Value as string ?? string.Empty;
                return (value.ToString().IsTrue() || value.ToString().Equals("1"));
            }
            set { Value = (value ? "true" : "false"); }
        }

        public IEnumerable<string> StringsValue {
            get {
                var data = Value;
                if (data == null) {
                    return Enumerable.Empty<string>();
                }
                if (data is string) {
                    return new[] {data as string};
                }
                if (data is string[]) {
                    return data as string[];
                }
                return Enumerable.Empty<string>();
            }
            set { Value = value.ToArray(); }
        }

        public IEnumerable<byte> BinaryValue {
            get { return Value as byte[] ?? new byte[0]; }
            set { Value = value.ToArray(); }
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
                            if (key.GetValue(_valueName, null) != null)
                                key.DeleteValue(_valueName);
                        }
                        else if (value is long) {
                            key.SetValue(_valueName, value, RegistryValueKind.QWord);
                        }
                        else if (value is int) {
                            key.SetValue(_valueName, value, RegistryValueKind.DWord);
                        }
                        else if (value is string[]) {
                            key.SetValue(_valueName, value, RegistryValueKind.MultiString);
                        }
                        else if (value is byte[]) {
                            key.SetValue(_valueName, value, RegistryValueKind.Binary);
                        }
                        else {
                            key.SetValue(_valueName, value);
                        }
                        // key.SetValue(_valueName,value,RegistryValueKind.ExpandString);
                    }
                }
            }
        }

        public void DeleteSubkey(string subkey) {
            using (var key = WriteableKey) {
                if (key != null) {
                    try {
                        key.DeleteSubKeyTree(subkey);
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch {
                    // ReSharper restore EmptyGeneralCatchClause
                    }
                }
            }
        }

        public RegistryView this[string keyName, string valueName] {
            get { return new RegistryView(_rootKey, _subKey +@"\"+ keyName, valueName); }
        }

        public RegistryView this[string keyName] {
            get { return new RegistryView(_rootKey, _subKey +@"\"+ keyName ); }
        }
    }
}