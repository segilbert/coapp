//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Win32 {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Exceptions;
    using Extensions;
    using Microsoft.Win32.SafeHandles;

    internal class ModernSymlink : ISymlink {
        public void MakeFileLink(string linkPath, string actualFilePath) {
            linkPath = Path.GetFullPath(linkPath);
            actualFilePath= Path.GetFullPath(actualFilePath);

            if( !File.Exists(actualFilePath))
                throw new FileNotFoundException("Cannot link to non-existent file", actualFilePath);

            if( File.Exists(linkPath) || Directory.Exists(linkPath)) {
                if (IsSymlink(linkPath)) {
                    deleteSymlink(linkPath);
                }

                if( !File.Exists(linkPath) || Directory.Exists(linkPath))
                    throw new ConflictingFileOrFolderException(linkPath);
            }

            Kernel32.CreateSymbolicLink(linkPath, actualFilePath, 0);
        }

        public void MakeDirectoryLink(string linkPath, string actualFolderPath) {
            linkPath = Path.GetFullPath(linkPath);
            actualFolderPath = Path.GetFullPath(actualFolderPath);

            if (!Directory.Exists(actualFolderPath))
                throw new FileNotFoundException("Cannot link to non-existent directory", actualFolderPath);

            if (File.Exists(linkPath) || Directory.Exists(linkPath)) {
                if (IsSymlink(linkPath)) {
                    deleteSymlink(linkPath);
                }

                if (!File.Exists(linkPath) || Directory.Exists(linkPath))
                    throw new ConflictingFileOrFolderException(linkPath);
            }

            Kernel32.CreateSymbolicLink(linkPath, actualFolderPath, 1);
        }

        public void DeleteSymlink(string linkPath) {
            if (!File.Exists(linkPath) && !Directory.Exists(linkPath)) {
                throw new FileNotFoundException(linkPath);
            }

            if (IsSymlink(linkPath)) {
                deleteSymlink(linkPath);
            }
            else {
                throw new PathIsNotSymlinkException(linkPath);
            }
        }

        private void deleteSymlink(string linkPath) {
            if (File.Exists(linkPath)) {
                linkPath.TryHardToDeleteFile();
            }
            else if (Directory.Exists(linkPath)) {
                linkPath.TryHardToDeleteDirectory();
            }
        }

        public bool IsSymlink(string linkPath) {
            if(!ReparsePoint.IsReparsePoint(linkPath)){
                return false;
            }

            var reparsePoint = ReparsePoint.Open(linkPath);
            return reparsePoint.IsSymlinkOrJunction;
        }

        public string GetActualPath(string linkPath) {
            return ReparsePoint.GetActualPath(linkPath);
        }
    }
}