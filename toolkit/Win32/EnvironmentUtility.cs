﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Configuration;
    using Extensions;

    public static class EnvironmentUtility {
        private static readonly RegistryView SystemEnvironment = RegistryView.System[@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment"];
        private static readonly RegistryView UserEnvironment = RegistryView.User[@"Environment"];

#if !COAPP_ENGINE_CORE
        private const Int32 HWND_BROADCAST = 0xffff;
        private const Int32 WM_SETTINGCHANGE = 0x001A;
        private const Int32 SMTO_ABORTIFHUNG = 0x0002;
#endif

        public static void BroadcastChange() {
#if COAPP_ENGINE_CORE
            Rehash.ForceProcessToReloadEnvironment("explorer");
            Rehash.ForceProcessToReloadEnvironment("services");
#else
            Task.Factory.StartNew(() => { User32.SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_ABORTIFHUNG, 1000, IntPtr.Zero); });
#endif
        }

        public static string GetSystemEnvironmentVariable(string name) {
            return SystemEnvironment["#" + name].StringValue;
        }

        public static void SetSystemEnvironmentVariable(string name, string value) {
            SystemEnvironment["#" + name].StringValue = value;
        }

        public static IEnumerable<string> PowershellModulePath {
            get {
                var path = SystemEnvironment["#PSModulePath"].StringValue;
                return string.IsNullOrEmpty(path) ? Enumerable.Empty<string>() : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set {
                var newValue = value.Any() ? value.Aggregate((current, each) => current + ";" + each) : null;
                if (newValue != SystemEnvironment["#PSModulePath"].StringValue) {
                    SystemEnvironment["#PSModulePath"].StringValue = newValue;
                }
            }
        }

        public static IEnumerable<string> SystemPath {
            get {
                var path = SystemEnvironment["#Path"].StringValue;
                return string.IsNullOrEmpty(path) ? Enumerable.Empty<string>() : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set {
                var newValue = value.Any() ? value.Aggregate((current, each) => current + ";" + each) : null;
                if (newValue != SystemEnvironment["#Path"].StringValue) {
                    SystemEnvironment["#Path"].StringValue = newValue;
                }
            }
        }

        public static IEnumerable<string> UserPath {
            get {
                var path = UserEnvironment["#Path"].StringValue;
                return string.IsNullOrEmpty(path) ? Enumerable.Empty<string>() : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set {
                var newValue = value.Any() ? value.Aggregate((current, each) => current + ";" + each) : null;
                if (newValue != UserEnvironment["#Path"].StringValue) {
                    UserEnvironment["#Path"].StringValue = newValue;
                }
            }
        }

        public static IEnumerable<string> EnvironmentPath {
            get {
                var path = Environment.GetEnvironmentVariable("path");
                return string.IsNullOrEmpty(path) ? Enumerable.Empty<string>() : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set { Environment.SetEnvironmentVariable("path", value.Any() ? value.Aggregate((current, each) => current + ";" + each) : ""); }
        }

        public static IEnumerable<string> Append(this IEnumerable<string> searchPath, string pathToAdd) {
            if (searchPath.Any(s => s.Equals(pathToAdd, StringComparison.CurrentCultureIgnoreCase))) {
                return searchPath;
            }
            return searchPath.UnionSingleItem(pathToAdd);
        }

        public static IEnumerable<string> Prepend(this IEnumerable<string> searchPath, string pathToAdd) {
            if (searchPath.Any(s => s.Equals(pathToAdd, StringComparison.CurrentCultureIgnoreCase))) {
                return searchPath;
            }
            return pathToAdd.SingleItemAsEnumerable().Union(searchPath);
        }

        public static IEnumerable<string> Remove(this IEnumerable<string> searchPath, string pathToRemove) {
            return searchPath.Where(s => !s.Equals(pathToRemove, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}