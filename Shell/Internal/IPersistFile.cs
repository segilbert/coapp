//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original (c) Author: Richard G Russell (Foredecker) 
//     Changes Copyright (c) 2010  Garrett Serack, CoApp Contributors. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) Author: Richard G Russell (Foredecker) 
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Shell.Internal {
    using System;
    using System.Runtime.InteropServices;

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid("0000010B-0000-0000-C000-000000000046")
    ]
    internal interface IPersistFile {
        #region Methods inherited from IPersist

        void GetClassID(out Guid pClassID);

        #endregion

        [PreserveSig]
        int IsDirty();

        void Load(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            int dwMode);

        void Save(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        void SaveCompleted(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void GetCurFile(
            out IntPtr ppszFileName);
    }
}