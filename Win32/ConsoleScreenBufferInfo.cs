//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;

    public struct ConsoleScreenBufferInfo {
        public Coord dwSize;
        public Coord dwCursorPosition;
        public Int16 wAttributes;
        public SmallRect srWindow;
        public Int16 dwMaximumWindowSize;
    }
}