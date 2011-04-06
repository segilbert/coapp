//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// The structure indicates whether a token has elevated privileges.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenElevation {
        public Int32 TokenIsElevated;
    }
}