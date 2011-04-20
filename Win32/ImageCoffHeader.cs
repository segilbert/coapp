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
    public class ImageCoffHeader {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint SymbolTablePointer;
        public uint NumberOfSymbols;
        public ushort OptionalHeaderSize;
        public ushort Characteristics;
    }
}