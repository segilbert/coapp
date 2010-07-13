//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;

    public class ConsoleColors : IDisposable {
        private readonly ConsoleColor fg;
        private readonly ConsoleColor bg;

        public ConsoleColors(ConsoleColor fg, ConsoleColor bg) {
            this.fg = Console.ForegroundColor;
            this.bg = Console.BackgroundColor;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public void Dispose() {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }
    }

    public static class ConsoleExtensions {
    }
}