//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct Win32StreamId {
        public readonly int StreamId;
        public readonly int StreamAttributes;
        public long Size;
        public readonly int StreamNameSize;
    }
}