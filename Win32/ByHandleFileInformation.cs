//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System.Runtime.InteropServices.ComTypes;

    public struct ByHandleFileInformation {
        public uint FileAttributes;
        public FILETIME CreationTime;
        public FILETIME LastAccessTime;
        public FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public ulong FileSize;
//         public uint FileSizeLow;
        public uint NumberOfLinks;
        public ulong FileIndex;
        //public uint FileIndexLow;
    }
}