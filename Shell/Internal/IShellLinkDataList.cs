//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original (c) Author: Richard G Russell (Foredecker) 
//     Changes Copyright (c) 2010  Garrett Serack, CoApp Contributors. All rights reserved.
// </copyright>
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
        Guid("45e2b4ae-b1c3-11d0-b92f-00a0c90312e1")
    ]
    internal interface IShellLinkDataList {
        [PreserveSig]
        Int32 AddDataBlock(IntPtr pDataBlock);

        [PreserveSig]
        Int32 CopyDataBlock(UInt32 dwSig, out IntPtr ppDataBlock);

        [PreserveSig]
        Int32 RemoveDataBlock(UInt32 dwSig);

        // The flags paramter values are defined in shlobj.h - see the SHELL_LINK_DATA_FLAGS enumeration
        void GetFlags(out UInt32 pdwFlags);
        void SetFlags(UInt32 dwFlags);
    }
}