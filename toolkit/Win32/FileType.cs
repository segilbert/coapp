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
    public enum FileType : uint {
        Char = 0x0002,
        Disk = 0x0001,
        Pipe = 0x0003,
        Remote = 0x8000,
        Unknown = 0x0000,
    }
}