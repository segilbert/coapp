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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct EXP_SPECIAL_FOLDER {
        public const UInt32 EXP_SPECIAL_FOLDER_SIG = 0xA0000005;

        public DATABLOCK_HEADER dbh;

        private UInt32 cbSize; // Size of this extra data block
        private UInt32 dwSignature; // signature of this extra data block
        private UInt32 idSpecialFolder; // special folder id this link points into
        private UInt32 cbOffset; // ofset into pidl from SLDF_HAS_ID_LIST for child
    }
}