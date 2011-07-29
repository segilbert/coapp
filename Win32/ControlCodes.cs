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
    public enum ControlCodes : uint {
        SetReparsePoint = 0x000900A4,       // Command to set the reparse point data block.
        GetReparsePoint = 0x000900A8,       // Command to get the reparse point data block.
        DeleteReparsePoint = 0x000900AC     // Command to delete the reparse point data base.
    }
}