//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class FocusInputRecord {
        public Int16 EventType;
        public bool bSetFocus;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public String wszTitle = new String(' ', 256);
    }
}