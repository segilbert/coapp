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
    using System;

    [Flags]
    public enum CreateRemoteThreadFlags {
        None = 0,
        Suspended = 0x00000004,
        StackSizeIsaReservation = 0x00010000,
    }
}