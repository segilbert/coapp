//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (C) 2000-2002 Lutz Roeder. All rights reserved.
//     Changes Copyright (c) 2011  Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// Copyright (C) 2000-2002 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Collections;
    using Exceptions;
    using Extensions;
    using Logging;
    using Scripting.Languages.CSV;
    using Utility;

    public class PEInfo {
        private static readonly Dictionary<string, PEInfo> _cache = new Dictionary<string, PEInfo>();

        private static readonly Lazy<ProgramFinder> _programFinder =
            new Lazy<ProgramFinder>(
                () =>
                    new ProgramFinder("",
                        @"{0}\optional;%SystemDrive%\WinDDK;%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%".format(
                            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))));

        private static readonly Lazy<ProcessUtility> _dependsX86 =
            new Lazy<ProcessUtility>(() => new ProcessUtility(_programFinder.Value.ScanForFile("depends.exe", ExecutableInfo.x86, "2.2")));

        private static readonly Lazy<ProcessUtility> _dependsX64 =
            new Lazy<ProcessUtility>(() => new ProcessUtility(_programFinder.Value.ScanForFile("depends.exe", ExecutableInfo.x64, "2.2")));

        private readonly Lazy<ExecutableInfo> _executableInfo;

        private readonly Lazy<FileVersionInfo> _fileVersionInfo;
        private readonly Lazy<ulong> _fileVersionLong;
        private readonly Lazy<string> _fileVersionString;
        private readonly string _filename;
        public ImageCoffHeader CoffHeader;
        public ImageCor20Header CorHeader;
        public ImageOptionalHeaderNt NtHeader;
        public string MD5;
        public string Filename { get {return _filename;} }

        public ImageSectionHeader[] SectionHeaders;
        private ImageDataDirectory _baseRelocationTable;
        private ImageDataDirectory _boundImport;
        private ImageDataDirectory _certificateTable;
        private ImageDataDirectory _copyright;
        private ImageDataDirectory _debug;
        private ImageDataDirectory _delayImportDescriptor;
        private ImageDataDirectory _exceptionTable;
        private ImageDataDirectory _exportTable;
        private ImageDataDirectory _globalPtr;
        private ImageDataDirectory _iat;
        private ImageDataDirectory _importTable;
        private ImageDataDirectory _loadConfigTable;
        private ImageDataDirectory _reserved;
        private ImageDataDirectory _resourceTable;
        private ImageDataDirectory _runtimeHeader;
        private ImageDataDirectory _tlsTable;

        private PEInfo(string filename) {
            _filename = filename;
            try {
                IsPEBinary = true;
                _executableInfo = new Lazy<ExecutableInfo>(() => {
                    var result = IsManaged ? ExecutableInfo.managed : ExecutableInfo.native;
                    if (IsAny) {
                        result |= ExecutableInfo.any;
                    }
                    else {
                        switch (CoffHeader.Machine) {
                            case 0x01c0:
                                result |= ExecutableInfo.arm;
                                break;
                            case 0x014c:
                                result |= ExecutableInfo.x86;
                                break;
                            case 0x0200:
                                result |= ExecutableInfo.ia64;
                                break;
                            case 0x8664:
                                result |= ExecutableInfo.x64;
                                break;
                            default:
                                throw new CoAppException("Unrecognized Executable Machine Type.");
                        }
                    }

                    return result;
                });

                _fileVersionInfo = new Lazy<FileVersionInfo>(() => FileVersionInfo.GetVersionInfo(_filename));
                _fileVersionString =
                    new Lazy<string>(
                        () =>
                            string.Format("{0}.{1}.{2}.{3}", _fileVersionInfo.Value.FileMajorPart, _fileVersionInfo.Value.FileMinorPart,
                                _fileVersionInfo.Value.FileBuildPart, _fileVersionInfo.Value.FilePrivatePart));
                _fileVersionLong =
                    new Lazy<ulong>(
                        () =>
                            (((ulong) _fileVersionInfo.Value.FileMajorPart) << 48) + (((ulong) _fileVersionInfo.Value.FileMinorPart) << 32) +
                                (((ulong) _fileVersionInfo.Value.FileBuildPart) << 16) + (ulong) _fileVersionInfo.Value.FilePrivatePart);

                DependencyInformation = new LazyEnumerable<DependencyInformation>(DependencyInformationImpl);

                using (var reader = new BinaryReader(File.OpenRead(_filename))) {
                    // Skip DOS Header and seek to PE signature
                    if (reader.ReadUInt16() != 0x5A4D) {
                        Logger.Warning("File '{0}' does not have a valid PE Header", _filename);
                        throw new CoAppException("Invalid DOS header.", true);
                    }

                    reader.ReadBytes(58);
                    reader.BaseStream.Position = reader.ReadUInt32();

                    // Read "PE\0\0" signature
                    if (reader.ReadUInt32() != 0x00004550) {
                        throw new CoAppException("File is not a portable executable.");
                    }

                    // Read COFF header
                    CoffHeader = new ImageCoffHeader {
                        Machine = reader.ReadUInt16(),
                        NumberOfSections = reader.ReadUInt16(),
                        TimeDateStamp = reader.ReadUInt32(),
                        SymbolTablePointer = reader.ReadUInt32(),
                        NumberOfSymbols = reader.ReadUInt32(),
                        OptionalHeaderSize = reader.ReadUInt16(),
                        Characteristics = reader.ReadUInt16()
                    };

                    // Compute data sections offset
                    var dataSectionsOffset = reader.BaseStream.Position + CoffHeader.OptionalHeaderSize;

                    // Read NT-specific fields
                    NtHeader = new ImageOptionalHeaderNt();

                    NtHeader.Magic = reader.ReadUInt16();
                    NtHeader.MajorLinkerVersion = reader.ReadByte();
                    NtHeader.MinorLinkerVersion = reader.ReadByte();
                    NtHeader.SizeOfCode = reader.ReadUInt32();
                    NtHeader.SizeOfInitializedData = reader.ReadUInt32();
                    NtHeader.SizeOfUninitializedData = reader.ReadUInt32();
                    NtHeader.AddressOfEntryPoint = reader.ReadUInt32();
                    NtHeader.BaseOfCode = reader.ReadUInt32();

                    if (Is32BitPE) {
                        NtHeader.BaseOfData_32bit = reader.ReadUInt32();
                        NtHeader.ImageBase_32bit = reader.ReadUInt32();
                    }

                    if (Is64BitPE) {
                        NtHeader.ImageBase_64bit = reader.ReadUInt64();
                    }

                    NtHeader.SectionAlignment = reader.ReadUInt32();
                    NtHeader.FileAlignment = reader.ReadUInt32();
                    NtHeader.OsMajor = reader.ReadUInt16();
                    NtHeader.OsMinor = reader.ReadUInt16();
                    NtHeader.UserMajor = reader.ReadUInt16();
                    NtHeader.UserMinor = reader.ReadUInt16();
                    NtHeader.SubSysMajor = reader.ReadUInt16();
                    NtHeader.SubSysMinor = reader.ReadUInt16();
                    NtHeader.Reserved = reader.ReadUInt32();
                    NtHeader.ImageSize = reader.ReadUInt32();
                    NtHeader.HeaderSize = reader.ReadUInt32();
                    NtHeader.FileChecksum = reader.ReadUInt32();
                    NtHeader.SubSystem = reader.ReadUInt16();
                    NtHeader.DllFlags = reader.ReadUInt16();

                    if (Is32BitPE) {
                        NtHeader.StackReserveSize_32bit = reader.ReadUInt32();
                        NtHeader.StackCommitSize_32bit = reader.ReadUInt32();
                        NtHeader.HeapReserveSize_32bit = reader.ReadUInt32();
                        NtHeader.HeapCommitSize_32bit = reader.ReadUInt32();
                    }
                    if (Is64BitPE) {
                        NtHeader.StackReserveSize_64bit = reader.ReadUInt64();
                        NtHeader.StackCommitSize_64bit = reader.ReadUInt64();
                        NtHeader.HeapReserveSize_64bit = reader.ReadUInt64();
                        NtHeader.HeapCommitSize_64bit = reader.ReadUInt64();
                    }
                    NtHeader.LoaderFlags = reader.ReadUInt32();
                    NtHeader.NumberOfDataDirectories = reader.ReadUInt32();
                    if (NtHeader.NumberOfDataDirectories < 16) {
                        return;
                    }

                    // Read data directories
                    _exportTable = ReadDataDirectory(reader);
                    _importTable = ReadDataDirectory(reader);
                    _resourceTable = ReadDataDirectory(reader);
                    _exceptionTable = ReadDataDirectory(reader);
                    _certificateTable = ReadDataDirectory(reader);
                    _baseRelocationTable = ReadDataDirectory(reader);
                    _debug = ReadDataDirectory(reader);
                    _copyright = ReadDataDirectory(reader);
                    _globalPtr = ReadDataDirectory(reader);
                    _tlsTable = ReadDataDirectory(reader);
                    _loadConfigTable = ReadDataDirectory(reader);
                    _boundImport = ReadDataDirectory(reader);
                    _iat = ReadDataDirectory(reader);
                    _delayImportDescriptor = ReadDataDirectory(reader);
                    _runtimeHeader = ReadDataDirectory(reader);
                    _reserved = ReadDataDirectory(reader);

                    if (_runtimeHeader.Size == 0) {
                        return;
                    }

                    // Read data sections
                    reader.BaseStream.Position = dataSectionsOffset;
                    SectionHeaders = new ImageSectionHeader[CoffHeader.NumberOfSections];
                    for (var i = 0; i < SectionHeaders.Length; i++) {
                        reader.ReadBytes(12);
                        SectionHeaders[i].VirtualAddress = reader.ReadUInt32();
                        SectionHeaders[i].SizeOfRawData = reader.ReadUInt32();
                        SectionHeaders[i].PointerToRawData = reader.ReadUInt32();
                        reader.ReadBytes(16);
                    }

                    // Read COR20 Header
                    reader.BaseStream.Position = RvaToVa(_runtimeHeader.Rva);
                    CorHeader = new ImageCor20Header {
                        Size = reader.ReadUInt32(),
                        MajorRuntimeVersion = reader.ReadUInt16(),
                        MinorRuntimeVersion = reader.ReadUInt16(),
                        MetaData = ReadDataDirectory(reader),
                        Flags = reader.ReadUInt32(),
                        EntryPointToken = reader.ReadUInt32(),
                        Resources = ReadDataDirectory(reader),
                        StrongNameSignature = ReadDataDirectory(reader),
                        CodeManagerTable = ReadDataDirectory(reader),
                        VTableFixups = ReadDataDirectory(reader),
                        ExportAddressTableJumps = ReadDataDirectory(reader)
                    };
                }
            }
            catch {
                IsPEBinary = false;
            }
        }

        public ExecutableInfo ExecutableInfo {
            get { return _executableInfo.Value; }
        }

        public ulong FileVersionLong {
            get { return IsPEBinary ? _fileVersionLong.Value : 0L; }
        }

        public string FileVersion {
            get { return IsPEBinary ? _fileVersionString.Value : "0.0.0.0"; }
        }

        public FileVersionInfo VersionInfo {
            get { return IsPEBinary ? _fileVersionInfo.Value : null; }
        }

        public IEnumerable<DependencyInformation> DependencyInformation { get; private set; }

        private IEnumerable<DependencyInformation> DependencyInformationImpl {
            get {
                var depends = Environment.Is64BitOperatingSystem && (ExecutableInfo & ExecutableInfo.x64) == ExecutableInfo.x64
                    ? _dependsX64.Value : _dependsX86.Value;
                var tmpFile = "dependencyInfo".GenerateTemporaryFilename();

                depends.Exec(@"/c /a:1 ""/oc:{0}"" /u:1 /f:1 ""{1}""", tmpFile, _filename);

                using (var csv = new CsvReader(new StreamReader(tmpFile), true)) {
                    while (csv.ReadNextRecord()) {
                        yield return new DependencyInformation {
                            Status = csv["Status"],
                            Module = csv["Module"].ToLower(),
                            Filename = Path.GetFileName(csv["Module"]).ToLower(),
                            FileTimeStamp = csv["File Time Stamp"],
                            LinkTimeStamp = csv["Link Time Stamp"],
                            FileSize = csv["File Size"],
                            Attr = csv["Attr."],
                            LinkChecksum = csv["Link Checksum"],
                            RealChecksum = csv["Real Checksum"],
                            CPU = csv["CPU"],
                            Subsystem = csv["Subsystem"],
                            Symbols = csv["Symbols"],
                            PreferredBase = csv["Preferred Base"],
                            ActualBase = csv["Actual Base"],
                            VirtualSize = csv["Virtual Size"],
                            LoadOrder = csv["Load Order"],
                            FileVer = csv["File Ver"],
                            ProductVer = csv["Product Ver"],
                            ImageVer = csv["Image Ver"],
                            LinkerVer = csv["Linker Ver"],
                            OSVer = csv["OS Ver"],
                            SubsystemVer = csv["Subsystem Ver"]
                        };
                    }
                }
                tmpFile.TryHardToDelete();
            }
        }

        public bool Is64BitPE {
            get { return IsPEBinary && (NtHeader.Magic == 0x20b); }
        }

        public bool Is32BitPE {
            get { return IsPEBinary && (NtHeader.Magic == 0x10b); }
        }

        public bool IsManaged {
            get { return IsPEBinary && CorHeader != null; }
        }

        public bool IsNative {
            get { return IsPEBinary && CorHeader == null; }
        }

        public bool Is32Bit {
            get {
                if (!IsPEBinary) {
                    return false;
                }

                if (!Is32BitPE) {
                    return false;
                }

                if (!IsManaged) {
                    return true;
                }

                return (CorHeader.Flags & 0x0002) != 0;
            }
        }

        public bool Is64Bit {
            get { return IsPEBinary && Is64BitPE; }
        }

        public bool IsAny {
            get { return IsPEBinary && (Is32BitPE && IsManaged && ((CorHeader.Flags & 0x0002) == 0)); }
        }

        public bool IsConsole {
            get { return IsPEBinary && (NtHeader.SubSystem & 1) == 1; }
        }

        public bool IsPEBinary { get; private set; }

        public long MetaDataRoot {
            get { return RvaToVa(CorHeader.MetaData.Rva); }
        }

        public static PEInfo Scan(string filename) {
            filename = filename.GetFullPath();

            if (!File.Exists(filename)) {
                throw new FileNotFoundException("Unable to find file", filename);
            }

            lock (_cache) {
                var MD5 = filename.GetFileMD5();

                if (!_cache.ContainsKey(filename)) {
                    var result = new PEInfo(filename) {MD5 = MD5};
                    _cache.Add(filename, result);
                    return result;
                }

                var cachedResult = _cache[filename];
                if( cachedResult.MD5 != MD5 ) {
                    // MD5 doesn't match. 
                    // replace the old one with the current one
                    _cache.Remove(filename);
                    
                    var result = new PEInfo(filename) {MD5 = MD5};
                    _cache.Add(filename, result);
                    return result;

                }
                return cachedResult;
            }
        }

        private static ImageDataDirectory ReadDataDirectory(BinaryReader reader) {
            return new ImageDataDirectory {
                Rva = reader.ReadUInt32(),
                Size = reader.ReadUInt32()
            };
        }

        public long RvaToVa(long rva) {
            for (var i = 0; i < SectionHeaders.Length; i++) {
                if ((SectionHeaders[i].VirtualAddress <= rva) && (SectionHeaders[i].VirtualAddress + SectionHeaders[i].SizeOfRawData > rva)) {
                    return (SectionHeaders[i].PointerToRawData + (rva - SectionHeaders[i].VirtualAddress));
                }
            }
            throw new CoAppException("Invalid RVA address.");
        }
    }
}