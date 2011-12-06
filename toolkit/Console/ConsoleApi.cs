//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Console {
    using System;
    using Microsoft.Win32.SafeHandles;
    using Win32;

    /// <summary>
    ///   Declarations of some Console API functions and structures.
    /// </summary>
    public static class ConsoleApi {
        /*
            public const Int32 CONSOLE_TEXTMODE_BUFFER = 1;
            public const Int32 SW_SHOW = 5;
            public const Int32 WM_KEYDOWN = 0x100;
            public const Int32 WM_COMMAND = 0x112;
            public const Int32 WM_CLOSE = 0x0010;
            public const int CREATE_NEW_CONSOLE = 0x00000010;
            */

        public static void SendStringToStdIn(string text) {
            var stdIn = Kernel32.GetStdHandle(StandardHandle.INPUT);
            foreach (var c in text) {
                SendCharacterToStream(stdIn, c);
            }
        }

        private static void SendCharacterToStream(SafeFileHandle hIn, char c) {
            var count = 0;
            var keyInputRecord = new KeyInputRecord {
                bKeyDown = true,
                wRepeatCount = 1,
                wVirtualKeyCode = 0,
                wVirtualScanCode = 0,
                UnicodeChar = c,
                dwControlKeyState = 0
            };
            Kernel32.WriteConsoleInput(hIn, keyInputRecord, 1, out count);
            keyInputRecord.bKeyDown = false;
            Kernel32.WriteConsoleInput(hIn, keyInputRecord, 1, out count);
        }
    }
}