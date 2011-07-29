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
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class FocusInputRecord {
        public Int16 EventType;
        public bool bSetFocus;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public String wszTitle = new String(' ', 256);
    }
}