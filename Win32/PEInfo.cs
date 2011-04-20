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
    using System.IO;

    public class PEInfo {
        public ImageSectionHeader[] sectionHeaders;
        public ImageOptionalHeaderNt ntHeader;
        public ImageCor20Header corHeader;
        public ImageCoffHeader coffHeader;

        private ImageDataDirectory exportTable;
        private ImageDataDirectory importTable;
        private ImageDataDirectory resourceTable;
        private ImageDataDirectory exceptionTable;
        private ImageDataDirectory certificateTable;
        private ImageDataDirectory baseRelocationTable;
        private ImageDataDirectory debug;
        private ImageDataDirectory copyright;
        private ImageDataDirectory globalPtr;
        private ImageDataDirectory tlsTable;
        private ImageDataDirectory loadConfigTable;
        private ImageDataDirectory boundImport;
        private ImageDataDirectory iat;
        private ImageDataDirectory delayImportDescriptor;
        private ImageDataDirectory runtimeHeader;
        private ImageDataDirectory reserved;

        private readonly BinaryReader reader;

        public bool Is64BitPE {
            get { return (ntHeader.Magic == 0x20b); }
        }

        public bool Is32BitPE {
            get { return (ntHeader.Magic == 0x10b); }
        }

        public bool IsManaged {
            get { return corHeader != null; }
        }

        public bool IsNative {
            get { return corHeader == null; }
        }

        public bool Is32Bit {
            get {
                if (!Is32BitPE)
                    return false;

                if (!IsManaged)
                    return true;

                return (corHeader.Flags & 0x0002) != 0;
            }
        }

        public bool Is64Bit {
            get { return Is64BitPE; }
        }

        public bool IsAny {
            get { return (Is32BitPE && IsManaged && ((corHeader.Flags & 0x0002) == 0)); }
        }

        public PEInfo(string filename) {
            if( !File.Exists(filename)) 
                throw new FileNotFoundException("Unable to find file",filename);

            reader = new BinaryReader(File.OpenRead(filename));

            // Skip DOS Header and seek to PE signature
            if (reader.ReadUInt16() != 0x5A4D) {
                throw new Exception("Invalid DOS header.");
            }
            reader.ReadBytes(58);
            reader.BaseStream.Position = reader.ReadUInt32();

            // Read "PE\0\0" signature
            if (reader.ReadUInt32() != 0x00004550) {
                throw new Exception("File is not a portable executable.");
            }

            // Read COFF header
            coffHeader = new ImageCoffHeader();
            coffHeader.Machine = reader.ReadUInt16();
            coffHeader.NumberOfSections = reader.ReadUInt16();
            coffHeader.TimeDateStamp = reader.ReadUInt32();
            coffHeader.SymbolTablePointer = reader.ReadUInt32();
            coffHeader.NumberOfSymbols = reader.ReadUInt32();
            coffHeader.OptionalHeaderSize = reader.ReadUInt16();
            coffHeader.Characteristics = reader.ReadUInt16();

            // Compute data sections offset
            long dataSectionsOffset = reader.BaseStream.Position + coffHeader.OptionalHeaderSize;


            // Read NT-specific fields
            ntHeader = new ImageOptionalHeaderNt();

            ntHeader.Magic = reader.ReadUInt16();
            ntHeader.MajorLinkerVersion = reader.ReadByte();
            ntHeader.MinorLinkerVersion = reader.ReadByte();
            ntHeader.SizeOfCode = reader.ReadUInt32();
            ntHeader.SizeOfInitializedData = reader.ReadUInt32();
            ntHeader.SizeOfUninitializedData = reader.ReadUInt32();
            ntHeader.AddressOfEntryPoint = reader.ReadUInt32();
            ntHeader.BaseOfCode = reader.ReadUInt32();

            if (Is32BitPE) {
                ntHeader.BaseOfData_32bit = reader.ReadUInt32();
                ntHeader.ImageBase_32bit = reader.ReadUInt32();
            }

            if (Is64BitPE) {
                ntHeader.ImageBase_64bit = reader.ReadUInt64();
            }

            ntHeader.SectionAlignment = reader.ReadUInt32();
            ntHeader.FileAlignment = reader.ReadUInt32();
            ntHeader.OsMajor = reader.ReadUInt16();
            ntHeader.OsMinor = reader.ReadUInt16();
            ntHeader.UserMajor = reader.ReadUInt16();
            ntHeader.UserMinor = reader.ReadUInt16();
            ntHeader.SubSysMajor = reader.ReadUInt16();
            ntHeader.SubSysMinor = reader.ReadUInt16();
            ntHeader.Reserved = reader.ReadUInt32();
            ntHeader.ImageSize = reader.ReadUInt32();
            ntHeader.HeaderSize = reader.ReadUInt32();
            ntHeader.FileChecksum = reader.ReadUInt32();
            ntHeader.SubSystem = reader.ReadUInt16();
            ntHeader.DllFlags = reader.ReadUInt16();

            if (Is32BitPE) {
                ntHeader.StackReserveSize_32bit = reader.ReadUInt32();
                ntHeader.StackCommitSize_32bit = reader.ReadUInt32();
                ntHeader.HeapReserveSize_32bit = reader.ReadUInt32();
                ntHeader.HeapCommitSize_32bit = reader.ReadUInt32();
            }
            if (Is64BitPE) {
                ntHeader.StackReserveSize_64bit = reader.ReadUInt64();
                ntHeader.StackCommitSize_64bit = reader.ReadUInt64();
                ntHeader.HeapReserveSize_64bit = reader.ReadUInt64();
                ntHeader.HeapCommitSize_64bit = reader.ReadUInt64();
            }
            ntHeader.LoaderFlags = reader.ReadUInt32();
            ntHeader.NumberOfDataDirectories = reader.ReadUInt32();
            if (ntHeader.NumberOfDataDirectories < 16) {
                return;
            }

            // Read data directories
            exportTable = ReadDataDirectory();
            importTable = ReadDataDirectory();
            resourceTable = ReadDataDirectory();
            exceptionTable = ReadDataDirectory();
            certificateTable = ReadDataDirectory();
            baseRelocationTable = ReadDataDirectory();
            debug = ReadDataDirectory();
            copyright = ReadDataDirectory();
            globalPtr = ReadDataDirectory();
            tlsTable = ReadDataDirectory();
            loadConfigTable = ReadDataDirectory();
            boundImport = ReadDataDirectory();
            iat = ReadDataDirectory();
            delayImportDescriptor = ReadDataDirectory();
            runtimeHeader = ReadDataDirectory();
            reserved = ReadDataDirectory();

            if (runtimeHeader.Size == 0) {
                return;
            }

            // Read data sections
            reader.BaseStream.Position = dataSectionsOffset;
            sectionHeaders = new ImageSectionHeader[coffHeader.NumberOfSections];
            for (int i = 0; i < sectionHeaders.Length; i++) {
                reader.ReadBytes(12);
                sectionHeaders[i].VirtualAddress = reader.ReadUInt32();
                sectionHeaders[i].SizeOfRawData = reader.ReadUInt32();
                sectionHeaders[i].PointerToRawData = reader.ReadUInt32();
                reader.ReadBytes(16);
            }

            // Read COR20 Header
            reader.BaseStream.Position = RvaToVa(runtimeHeader.Rva);
            corHeader = new ImageCor20Header {
                Size = reader.ReadUInt32(),
                MajorRuntimeVersion = reader.ReadUInt16(),
                MinorRuntimeVersion = reader.ReadUInt16(),
                MetaData = ReadDataDirectory(),
                Flags = reader.ReadUInt32(),
                EntryPointToken = reader.ReadUInt32(),
                Resources = ReadDataDirectory(),
                StrongNameSignature = ReadDataDirectory(),
                CodeManagerTable = ReadDataDirectory(),
                VTableFixups = ReadDataDirectory(),
                ExportAddressTableJumps = ReadDataDirectory()
            };

            reader.Close();
        }

        private ImageDataDirectory ReadDataDirectory() {
            var directory = new ImageDataDirectory();
            directory.Rva = reader.ReadUInt32();
            directory.Size = reader.ReadUInt32();
            return directory;
        }

        public long RvaToVa(long rva) {
            for (int i = 0; i < sectionHeaders.Length; i++) {
                if ((sectionHeaders[i].VirtualAddress <= rva) && (sectionHeaders[i].VirtualAddress + sectionHeaders[i].SizeOfRawData > rva)) {
                    return (sectionHeaders[i].PointerToRawData + (rva - sectionHeaders[i].VirtualAddress));
                }
            }

            throw new Exception("Invalid RVA address.");
        }


        public long MetaDataRoot {
            get { return RvaToVa(corHeader.MetaData.Rva); }
        }
    }
}