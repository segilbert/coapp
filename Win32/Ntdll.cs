//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;

    public static class Ntdll {
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 RtlAcquirePrivilege(ref UInt32 Privilege, UInt32 NumPriv, UInt32 Flags, ref IntPtr ReturnedState);

        [DllImport("ntdll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 RtlReleasePrivilege(IntPtr ReturnedState);
    }
}