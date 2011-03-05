//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Configuration {
    using System;
    using System.Reflection;
    using Microsoft.Win32;

    public class QuickSettings : Settings {
        private static Settings userSettings = new Settings(Assembly.GetEntryAssembly().GetName().ToString().Substring(0,Assembly.GetEntryAssembly().GetName().ToString().IndexOf(",")));
        private static Settings systemSettings = new Settings(Assembly.GetEntryAssembly().GetName().ToString().Substring(0, Assembly.GetEntryAssembly().GetName().ToString().IndexOf(",")), Registry.LocalMachine);

        public QuickSettings(string name) : base(name) {
        }


        public static SettingsStringIndexer StringSetting = new SettingsStringIndexer(userSettings);
        public static SettingsStringArrayIndexer StringArraySetting = new SettingsStringArrayIndexer(userSettings);
        public static SettingsBooleanIndexer BoolSetting = new SettingsBooleanIndexer(userSettings);
        public static SettingsIntIndexer IntSetting = new SettingsIntIndexer(userSettings);
        public static SettingsLongIndexer LongSetting = new SettingsLongIndexer(userSettings);
        public static SettingsEncryptedStringIndexer EncryptedStringSetting = new SettingsEncryptedStringIndexer(userSettings);

        public static SettingsStringIndexer SystemStringSetting = new SettingsStringIndexer(systemSettings);
        public static SettingsStringArrayIndexer SystemStringArraySetting = new SettingsStringArrayIndexer(systemSettings);
        public static SettingsBooleanIndexer SystemBoolSetting = new SettingsBooleanIndexer(systemSettings);
        public static SettingsIntIndexer SystemIntSetting = new SettingsIntIndexer(systemSettings);
        public static SettingsLongIndexer SystemLongSetting = new SettingsLongIndexer(systemSettings);
        public static SettingsEncryptedStringIndexer SystemEncryptedStringSetting = new SettingsEncryptedStringIndexer(systemSettings);
    }

    public class QuickSettingsEnum<T> where T : struct, IComparable, IFormattable, IConvertible {
        public static QuickSettingsEnum<T> Setting = new QuickSettingsEnum<T>();

        public static T ParseEnum(string value, T defaultValue = default(T)) {
            if (Enum.IsDefined(typeof (T), value)) {
                return (T) Enum.Parse(typeof (T), value, true);
            }
            return defaultValue;
        }

        public static T CastToEnum(string value) {
            if (value.Contains("+")) {
                var values = value.Split('+');
                Type numberType = Enum.GetUnderlyingType(typeof (T));
                if (numberType.Equals(typeof (int))) {
                    int newResult = 0;
                    foreach (var val in values) {
                        newResult |= (int) (Object) ParseEnum(val);
                    }
                    return (T) (Object) newResult;
                }
            }
            return ParseEnum(value);
        }

        public static string CastToString(T value) {
            return Enum.Format(typeof (T), value, "G").Replace(", ", "+");
        }

        public T this[string settingName] {
            get { return CastToEnum(QuickSettings.StringSetting[settingName]); }
            set { QuickSettings.StringSetting[settingName] = CastToString(value); }
        }
    }
}