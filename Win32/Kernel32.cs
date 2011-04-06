//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32.SafeHandles;

    public static class Kernel32 {
        [DllImport("kernel32.dll")]
        public static extern int GlobalAddAtom(string name);
        [DllImport("kernel32.dll")]
        public static extern int GlobalDeleteAtom(int atom);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr InBuffer, int nInBufferSize, IntPtr OutBuffer, int nOutBufferSize, out int pBytesReturned, IntPtr lpOverlapped);
        
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string name, NativeFileAccess access, FileShare share, IntPtr security, FileMode mode, NativeFileAttributesAndFlags flags, IntPtr template);


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr vaListArguments);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetFileAttributes(string fileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileSizeEx(SafeFileHandle handle, out long size);

        [DllImport("kernel32.dll")]
        public static extern int GetFileType(SafeFileHandle handle);

        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string name);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BackupRead(SafeFileHandle hFile, ref Win32StreamId pBuffer, int numberOfBytesToRead, out int numberOfBytesRead, [MarshalAs(UnmanagedType.Bool)] bool abort, [MarshalAs(UnmanagedType.Bool)] bool processSecurity, ref IntPtr context);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BackupRead(SafeFileHandle hFile, SafeHGlobalHandle pBuffer, int numberOfBytesToRead, out int numberOfBytesRead, [MarshalAs(UnmanagedType.Bool)] bool abort, [MarshalAs(UnmanagedType.Bool)] bool processSecurity, ref IntPtr context);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BackupSeek(SafeFileHandle hFile, int bytesToSeekLow, int bytesToSeekHigh, out int bytesSeekedLow, out int bytesSeekedHigh, ref IntPtr context);

        [DllImportAttribute("kernel32.dll", EntryPoint = "MoveFileEx")]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);

    }
}