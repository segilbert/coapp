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
        public delegate bool ConsoleHandlerRoutine(ConsoleEvents eventId);

        [DllImport("kernel32.dll")]
        public static extern int GlobalAddAtom(string name);
        [DllImport("kernel32.dll")]
        public static extern int GlobalDeleteAtom(int atom);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DeviceIoControl(IntPtr hDevice, ControlCodes dwIoControlCode, IntPtr InBuffer, int nInBufferSize, IntPtr OutBuffer, int nOutBufferSize, out int pBytesReturned, IntPtr lpOverlapped);
        
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string name, NativeFileAccess access, FileShare share, IntPtr security, FileMode mode, NativeFileAttributesAndFlags flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr vaListArguments);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetFileAttributes(string fileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileSizeEx(SafeFileHandle handle, out long size);

        [DllImport("kernel32.dll")]
        public static extern FileType GetFileType(SafeFileHandle handle);

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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileInformationByHandle(IntPtr hFile, out ByHandleFileInformation lpFileInformation);

        [DllImport("kernel32.dll")]
        public static extern Int32 GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(ConsoleHandlerRoutine routine, bool add);

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        public static extern SafeFileHandle GetStdHandle(StandardHandle nStandardHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll")]
        public static extern Coord GetLargestConsoleWindowSize();

        [DllImport("kernel32.dll")]
        public static extern Coord GetConsoleFontSize(IntPtr hOut, Int32 index);

        [DllImport("kernel32.dll")]
        public static extern bool GetCurrentConsoleFont(IntPtr hOut, bool bMaximumWnd, out ConsoleFontInfo ConsoleCurrentFont);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleActiveScreenBuffer(IntPtr hBuf);

        [DllImport("kernel32.dll")]
        public static extern bool GetConsoleScreenBufferInfo(IntPtr hOut, out ConsoleScreenBufferInfo csbi);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool WriteConsoleInput(SafeFileHandle hIn, [MarshalAs(UnmanagedType.LPStruct)] KeyInputRecord r, Int32 count, out Int32 countOut);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool PeekConsoleInput(IntPtr hConsoleInput, [Out] [MarshalAs(UnmanagedType.LPStruct)] FocusInputRecord lpBuffer, Int32 nLength, out Int32 lpNumberOfEventsRead);

        [DllImport("kernel32.dll")]
        public static extern bool GetNumberOfConsoleInputEvents(IntPtr hIn, out Int32 num);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateConsoleScreenBuffer(NativeFileAccess dwDesiredAccess, FileShare dwShareMode, [MarshalAs(UnmanagedType.LPStruct)] SecurityAttributes lpSecurityAttributes, Int32 dwFlags, IntPtr lpScreenBufferData);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCursorPosition(IntPtr hOut, Coord newPos);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferSize(IntPtr hOut, Coord newSize);

        [DllImport("kernel32.dll")]
        public static extern bool WriteConsole(IntPtr hConsoleOutput, String lpBuffer, Int32 nNumberOfCharsToWrite,
            out Int32 lpNumberOfCharsWritten, IntPtr lpReserved);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(
            IntPtr lpApplicationName,
            IntPtr lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            Int32 bInheritHandles,
            Int32 dwCreationFlags,
            IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory,
            [MarshalAs(UnmanagedType.LPStruct)] [In] Startupinfo lpStartupInfo,
            IntPtr lpProcessInformation
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(String applicationName, String commandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandles, Int32 dwCreationFlags, IntPtr lpEnvironment, String lpCurrentDirectory,
            [MarshalAs(UnmanagedType.LPStruct)] [In] Startupinfo lpStartupInfo,
            [MarshalAs(UnmanagedType.LPStruct)] [In] ProcessInformation lpProcessInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(String applicationName, String commandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandles, Int32 dwCreationFlags, IntPtr lpEnvironment, IntPtr lpCurrentDirectory,
            IntPtr lpStartupInfo,
            IntPtr lpProcessInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern bool CreateProcessA(String applicationName, String commandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandles, Int32 dwCreationFlags, IntPtr lpEnvironment, IntPtr lpCurrentDirectory,
            IntPtr lpStartupInfo,
            IntPtr lpProcessInformation);

        [DllImport("kernel32.dll")] //, CharSet=CharSet.Unicode
        public static extern IntPtr GetProcAddress(IntPtr hmod, String name);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(String lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess, // handle to process
            IntPtr lpBaseAddress, // base	of memory area
            IntPtr lpBuffer, // data buffer
            Int32 nSize, // count of	bytes to write
            out Int32 lpNumberOfBytesWritten // count of bytes	written
            );

    }

    public enum FileType : uint {
        Char = 0x0002,
        Disk = 0x0001,
        Pipe = 0x0003,
        Remote = 0x8000,
        Unknown = 0x0000,
    }

}