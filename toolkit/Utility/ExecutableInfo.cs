//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
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
        ia64 = 0x0004,
        arm = 0x0008,
        any = 0x0010,

        native = 0x0100,
        managed = 0x0200
    }
}
