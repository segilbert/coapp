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
    using System.Runtime.InteropServices.ComTypes;

    public struct ByHandleFileInformation {
        public FILETIME CreationTime;
        public uint FileAttributes;
        public ulong FileIndex;
        public ulong FileSize;
        public FILETIME LastAccessTime;
        public FILETIME LastWriteTime;
        //         public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint VolumeSerialNumber;
        //public uint FileIndexLow;
    }
}