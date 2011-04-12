//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;

    [Flags]
    public enum KeyModifiers {
        MOD_ALT=0x0001,
        MOD_CONTROL=0x0002,
        MOD_SHIFT=0x0004,
        MOD_WIN=0x0008
    }
}