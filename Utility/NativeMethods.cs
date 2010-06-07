//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------
namespace CoApp.Toolkit.Utility
{
    using System;
    using System.Runtime.InteropServices;

    class NativeMethods
    {
        [DllImport("version.dll")]
        public static extern int GetFileVersionInfoSize(string sFileName, out int handle);
        [DllImport("version.dll")]
        public static extern bool GetFileVersionInfo(string sFileName, int handle, int size, byte[] infoBuffer);
        [DllImport("version.dll")]
        unsafe public static extern bool VerQueryValue(byte[] pBlock, string pSubBlock, out string pValue, out uint len);
        [DllImport("version.dll")]
        unsafe public static extern bool VerQueryValue(byte[] pBlock, string pSubBlock, out short* pValue, out uint len);
    }

    public enum MachineType
    {
        Native = 0,
        I386 = 0x014c,
        Itanium = 0x0200,
        x64 = 0x8664
    }

    [Flags]
    public enum ExecutableInfo
    {
        x86 = 0x0001,
        x64 = 0x0002,

        native = 0x0010,
        managed = 0x0020
    }

    public struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        [FieldOffset(0)]
        public ushort Magic;
        [FieldOffset(208)]
        public IMAGE_DATA_DIRECTORY DataDirectory;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        [FieldOffset(0)]
        public ushort Magic;
        [FieldOffset(224)]
        public IMAGE_DATA_DIRECTORY DataDirectory;
    }


    public struct IMAGE_OPTIONAL_HEADER64_full
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public UInt64 ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public UInt64 SizeOfStackReserve;
        public UInt64 SizeOfStackCommit;
        public UInt64 SizeOfHeapReserve;
        public UInt64 SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY[] DataDirectory;
    }

    public struct IMAGE_OPTIONAL_HEADER32_full
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        public uint ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public uint SizeOfStackReserve;
        public uint SizeOfStackCommit;
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY[] DataDirectory;
    }


    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_NT_HEADERS32
    {
        [FieldOffset(0)]
        public uint Signature;
        [FieldOffset(4)]
        public IMAGE_FILE_HEADER FileHeader;
        [FieldOffset(24)]
        public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
    }
    public struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    public struct IMAGE_NT_HEADERS64
    {
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
    }

    public struct IMAGE_NT_HEADERS32_other
    {
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
    }
    public struct IMAGE_DOS_HEADER_unused
    {
        public ushort e_magic;
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        public ushort[] e_res;
        public ushort e_oemid;
        public ushort e_oeminfo;
        public ushort[] d_res2;
        public int e_lfanew;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_DOS_HEADER
    {
        [FieldOffset(60)]
        public int e_lfanew;
    }
}
