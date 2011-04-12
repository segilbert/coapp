//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public class SecurityAttributes {
        public readonly Int32 nLength = Marshal.SizeOf(typeof(SecurityAttributes));
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
}