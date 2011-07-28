//-----------------------------------------------------------------------
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
        public static void ChangeLinkTarget(string linkPath, string newActualPath) {
            _symlink.Value.ChangeLinkTarget(linkPath,newActualPath);
        }
        public static void DeleteSymlink(string linkPath) {
            _symlink.Value.DeleteSymlink(linkPath);
        }
        public static bool IsSymlink(string linkPath) {
            return _symlink.Value.IsSymlink(linkPath);
        }
        public static string GetActualPath(string linkPath) {
            return _symlink.Value.GetActualPath(linkPath);
        }
    }
}
