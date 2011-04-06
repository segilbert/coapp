//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;

    public static class WindowsVersionInfo {
        public static bool IsVistaOrBeyond {
            get { return Environment.OSVersion.Version.Major >= 5; }
        }

        public static bool IsCurrentProcess32Bit {
            get {
                return !Environment.Is64BitProcess;
            }
        }

        public static bool IsCurrentProcess64Bit {
            get {
                return Environment.Is64BitProcess;
            }
        }

        public static bool IsOS32Bit {
            get {
                return !Environment.Is64BitOperatingSystem;
            }
        }

        public static bool IsOS64Bit {
            get {
                return Environment.Is64BitOperatingSystem;
            }
        }
    }
}