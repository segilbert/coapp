using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CoApp.Toolkit.Engine {

    public enum ApiResponseCode {
        Ok						= 0x0000,
        UrlNotWellFormed		= 0x0100
    }

    public class NativeMethods {
        [DllImport("coapp-engine.dll", CharSet = CharSet.Unicode)]
        public static extern ApiResponseCode coapp_SetRepositoryDirectoryURL(Int32 sizeOfInputString, string URL);

        [DllImport("coapp-engine.dll", CharSet = CharSet.Unicode)]
        public static extern ApiResponseCode coapp_ClearRepositoryDirectoryURL();

        [DllImport("coapp-engine.dll", CharSet = CharSet.Unicode)]
        public static extern ApiResponseCode coapp_GetRepostitoryDirectoryURL(Int32 sizeOfResponseBuffer, StringBuilder responseBuffer);
    }
}
// [MarshalAs(UnmanagedType.LPWStr)]