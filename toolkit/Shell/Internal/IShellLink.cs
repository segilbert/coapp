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
    using System.Text;

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid("000214EE-0000-0000-C000-000000000046")
    ]
    internal interface IShellLink {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile,
            int cchMaxPath,
            out WIN32_FIND_DATAW pfd,
            SLGP_FLAGS fFlags);

        void GetIDList(
            out IntPtr ppidl);

        void SetIDList(
            IntPtr pidl);

        void GetDescription(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszName,
            int cchMaxName);

        void SetDescription(
            [MarshalAs(UnmanagedType.LPStr)] string pszName);

        void GetWorkingDirectory(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszDir,
            int cchMaxPath);

        void SetWorkingDirectory(
            [MarshalAs(UnmanagedType.LPStr)] string pszDir);

        void GetArguments(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszArgs,
            int cchMaxPath);

        void SetArguments(
            [MarshalAs(UnmanagedType.LPStr)] string pszArgs);

        void GetHotkey(
            out short pwHotkey);

        void SetHotkey(
            short wHotkey);

        [PreserveSig]
        Int32 GetShowCmd(
            out int piShowCmd);

        void SetShowCmd(
            int iShowCmd);

        [PreserveSig]
        Int32 GetIconLocation(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszIconPath,
            int cchIconPath,
            out int piIcon);

        void SetIconLocation(
            [MarshalAs(UnmanagedType.LPStr)] string pszIconPath,
            int iIcon);

        void SetRelativePath(
            [MarshalAs(UnmanagedType.LPStr)] string pszPathRel,
            int dwReserved);

        void Resolve(
            IntPtr hwnd,
            SLR_FLAGS fFlags);

        void SetPath(
            [MarshalAs(UnmanagedType.LPStr)] string pszFile);
    }
}