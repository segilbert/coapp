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
    internal interface ISymlink {
        void MakeFileLink(string linkPath, string actualFilePath);
        void MakeDirectoryLink(string linkPath, string actualFolderPath);
        void ChangeLinkTarget(string linkPath, string actualFolderPath);

        void DeleteSymlink(string linkPath);
        
        bool IsSymlink(string linkPath);
        string GetActualPath(string linkPath);
    }
}