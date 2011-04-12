//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    public enum IoReparseTag : uint {
        MountPoint = 0xA0000003,    //   Reparse point tag used to identify mount points and junction points.
        Symlink = 0xA000000C        //   Reparse point tag used to identify symlinks
    }
}