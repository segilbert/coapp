//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;

    internal class LegacySymlink : ISymlink {
        public void MakeFileLink(string linkPath, string actualFilePath) {
            throw new NotImplementedException();
        }

        public void DeleteFileLink(string linkPath) {
            throw new NotImplementedException();
        }


        public void MakeDirectoryLink(string linkPath, string actualFolderPath) {
            throw new NotImplementedException();
        }

        public void DeleteSymlink(string linkPath) {
            throw new NotImplementedException();
        }

        public bool IsSymlink(string linkPath) {
            throw new NotImplementedException();
        }

        public string GetActualPath(string linkPath) {
            throw new NotImplementedException();
        }
    }
}