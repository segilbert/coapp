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
    public static class DebugExtensions {
        public static void Debug(this string buf, params object[] args) {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(buf.format(args));
#endif
        }

        public static void Debug(this byte[] buf) {
#if DEBUG
            const int lnWidth = 16;

            System.Diagnostics.Debug.WriteLine(" Buffer Length: {0} [0x{0:x4}]".format(buf.Length));

            for(var x = 0; x < buf.Length; x += lnWidth) {
                for(var y = 0; y < lnWidth; y++) {
                    if(x + y >= buf.Length) {
                        System.Diagnostics.Debug.Write("   ");
                    }
                    else {
                        System.Diagnostics.Debug.Write(" {0:x2}".format(buf[x + y]));
                    }
                }
                System.Diagnostics.Debug.Write("    ");

                for(var y = 0; y < lnWidth; y++) {
                    if(x + y >= buf.Length) {
                        System.Diagnostics.Debug.Write(" ");
                    }
                    else {
                        var c = buf[x + y] < 32 || buf[x + y] > 127 ? '.' : (char) buf[x + y];
                        System.Diagnostics.Debug.Write("{0}".format(c));
                    }
                }
                System.Diagnostics.Debug.WriteLine("");
            }
            System.Diagnostics.Debug.WriteLine("");
#endif
        }

    }
}