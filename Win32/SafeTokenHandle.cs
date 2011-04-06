//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Represents a wrapper class for a token handle.
    /// </summary>
    public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeTokenHandle()
            : base(true) {
        }

        public SafeTokenHandle(IntPtr handle)
            : base(true) {
            base.SetHandle(handle);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        protected override bool ReleaseHandle() {
            return CloseHandle(base.handle);
        }
    }
}