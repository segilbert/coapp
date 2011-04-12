//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class KeyInputRecord {
        public Int16 EventType = ConsoleEventTypes.KeyEvent; //this is only one of several possible cases of the INPUT_RECORD structure

        //the rest of fields are from KEY_EVENT_RECORD :
        public bool bKeyDown;
        public Int16 wRepeatCount;
        public Int16 wVirtualKeyCode;
        public Int16 wVirtualScanCode;
        public char UnicodeChar;
        public Int32 dwControlKeyState;
    }
}