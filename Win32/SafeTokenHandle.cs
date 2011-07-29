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