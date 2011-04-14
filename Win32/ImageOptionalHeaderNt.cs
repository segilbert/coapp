//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (C) 2000-2002 Lutz Roeder. All rights reserved.
//     Changes Copyright (c) 2011  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// Copyright (C) 2000-2002 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
namespace CoApp.Toolkit.Win32 {
    using System;

    internal class ImageOptionalHeaderNt {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData_32bit; // 32bit only 
        public uint ImageBase_32bit;
        public UInt64 ImageBase_64bit;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort OsMajor;
        public ushort OsMinor;
        public ushort UserMajor;
        public ushort UserMinor;
        public ushort SubSysMajor;
        public ushort SubSysMinor;
        public uint Reserved;
        public uint ImageSize;
        public uint HeaderSize;
        public uint FileChecksum;
        public ushort SubSystem;
        public ushort DllFlags;
        public uint StackReserveSize_32bit;
        public uint StackCommitSize_32bit;
        public uint HeapReserveSize_32bit;
        public uint HeapCommitSize_32bit;
        public UInt64 StackReserveSize_64bit;
        public UInt64 StackCommitSize_64bit;
        public UInt64 HeapReserveSize_64bit;
        public UInt64 HeapCommitSize_64bit;
        public uint LoaderFlags;
        public uint NumberOfDataDirectories;
    }
}