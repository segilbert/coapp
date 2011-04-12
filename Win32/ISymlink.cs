//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Win32 {
    internal interface ISymlink {
        void MakeFileLink(string linkPath, string actualFilePath);
        void MakeDirectoryLink(string linkPath, string actualFolderPath);
        void ChangeLinkTarget(string linkPath, string actualFolderPath);

        void DeleteSymlink(string linkPath);
        
        bool IsSymlink(string linkPath);
        string GetActualPath(string linkPath);
    }
}