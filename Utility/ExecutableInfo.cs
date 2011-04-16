//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------
namespace CoApp.Toolkit.Utility {
    using System;

    [Flags]
    public enum ExecutableInfo {
        none = 0x0000,

        x86 = 0x0001,
        x64 = 0x0002,
        any = 0x0004,

        native = 0x0010,
        managed = 0x0020
    }
}
