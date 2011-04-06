//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;

    public class Symlink {
        private static Lazy<ISymlink> _symlink = new Lazy<ISymlink>(() => {
            if (WindowsVersionInfo.IsVistaOrBeyond) {
                return new ModernSymlink();
            }
            return new LegacySymlink();
        });

        public static void MakeFileLink(string linkPath, string actualFilePath) {
            _symlink.Value.MakeFileLink(linkPath, actualFilePath);
        }
        public static void MakeDirectoryLink(string linkPath, string actualFolderPath) {
            _symlink.Value.MakeDirectoryLink(linkPath, actualFolderPath);
        }

        public static void DeleteSymlink(string linkPath) {
            _symlink.Value.DeleteSymlink(linkPath);
        }

        public static void CreateShortcut(string shortcutPath, string actualFilePath) {
        }

        public static bool IsSymlink(string linkPath) {
            return _symlink.Value.IsSymlink(linkPath);
        }

        public static string GetActualPath(string linkPath) {
            return _symlink.Value.GetActualPath(linkPath);
        }
    }
}
