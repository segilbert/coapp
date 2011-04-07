//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    ///   Represents a wrapper class for a token handle.
    /// </summary>
    public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeTokenHandle()
            : base(true) {
        }

        public SafeTokenHandle(IntPtr handle)
            : base(true) {
            base.SetHandle(handle);
        }

        protected override bool ReleaseHandle() {
            return Kernel32.CloseHandle(base.handle);
        }
    }
}