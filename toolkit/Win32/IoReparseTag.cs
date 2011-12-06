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
    public enum IoReparseTag : uint {
        MountPoint = 0xA0000003, //   Reparse point tag used to identify mount points and junction points.
        Symlink = 0xA000000C //   Reparse point tag used to identify symlinks
    }
}