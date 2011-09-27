using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Bootstrapper {
    using System.Runtime.InteropServices;

    internal class NativeMethods {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern uint SearchPath(string lpPath, string lpFileName, string lpExtension, int nBufferLength,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBuffer, out IntPtr lpFilePart);

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern WinVerifyTrustResult WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, WinTrustData pWVTData);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiOpenDatabase(string szDatabasePath, IntPtr uiOpenMode, out int hDatabase);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiDatabaseOpenView(int hDatabase, string szQuery, out int hView);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiViewExecute(int hView, int hRecord);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiViewFetch(int hView, out int hRecord);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiRecordDataSize(int hRecord, uint iField);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiRecordReadStream(int hRecord, uint iField, byte[] szDataBuf, ref uint cbDataBuf);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiCloseHandle(int hAny);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiInstallProduct(string szPackagePath, string szCommandLine);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiSetInternalUI(uint dwUILevel, IntPtr phWnd);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern MainWindow.NativeExternalUIHandler MsiSetExternalUI(
            [MarshalAs(UnmanagedType.FunctionPtr)] MainWindow.NativeExternalUIHandler puiHandler, uint dwMessageFilter, IntPtr pvContext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int LoadString(SafeModuleHandle hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeModuleHandle LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr FindResource(SafeModuleHandle moduleHandle, int resourceId, string resourceType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadResource(SafeModuleHandle moduleHandle, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SizeofResource(SafeModuleHandle moduleHandle, IntPtr hResInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        public const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    }
}
