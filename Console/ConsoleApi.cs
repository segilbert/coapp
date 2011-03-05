namespace CoApp.Toolkit.Console {
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    ///   Declarations of some Console API functions and structures.
    /// </summary>
    public static class ConsoleApi {
        public static void SendStringToStdIn(string text) {
            var stdIn = GetStdHandle(-10);
            foreach (var c in text) {
                SendCharacterToStream(stdIn, c);
            }
        }

        private static void SendCharacterToStream(IntPtr hIn, char c) {
            var count = 0;
            var keyInputRecord = new KEY_INPUT_RECORD {
                bKeyDown = true,
                wRepeatCount = 1,
                wVirtualKeyCode = 0,
                wVirtualScanCode = 0,
                UnicodeChar = c,
                dwControlKeyState = 0
            };
            WriteConsoleInput(hIn, keyInputRecord, 1, out count);
            keyInputRecord.bKeyDown = false;
            WriteConsoleInput(hIn, keyInputRecord, 1, out count);
        }

        public delegate bool ConsoleHandlerRoutine(ConsoleEvents eventId);

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
        public static extern IntPtr GetStdHandle(Int32 nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll")]
        public static extern COORD GetLargestConsoleWindowSize();

        [DllImport("kernel32.dll")]
        public static extern COORD GetConsoleFontSize(IntPtr hOut, Int32 index);

        [DllImport("kernel32.dll")]
        public static extern bool GetCurrentConsoleFont(IntPtr hOut, bool bMaximumWnd, out CONSOLE_FONT_INFO ConsoleCurrentFont);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleActiveScreenBuffer(IntPtr hBuf);

        [DllImport("kernel32.dll")]
        public static extern bool GetConsoleScreenBufferInfo(IntPtr hOut, out CONSOLE_SCREEN_BUFFER_INFO csbi);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool WriteConsoleInput(IntPtr hIn, [MarshalAs(UnmanagedType.LPStruct)] KEY_INPUT_RECORD r, Int32 count,
            out Int32 countOut);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool PeekConsoleInput(IntPtr hConsoleInput,
            [Out] [MarshalAs(UnmanagedType.LPStruct)] FOCUS_INPUT_RECORD lpBuffer, Int32 nLength, out Int32 lpNumberOfEventsRead);

        [DllImport("kernel32.dll")]
        public static extern bool GetNumberOfConsoleInputEvents(IntPtr hIn, out Int32 num);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [CLSCompliant(false)]
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateConsoleScreenBuffer(DESIRED_ACCESS dwDesiredAccess, FILE_SHARE dwShareMode,
            [MarshalAs(UnmanagedType.LPStruct)] SECURITY_ATTRIBUTES lpSecurityAttributes, Int32 dwFlags, IntPtr lpScreenBufferData);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCursorPosition(IntPtr hOut, COORD newPos);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferSize(IntPtr hOut, COORD newSize);

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
            [MarshalAs(UnmanagedType.LPStruct)] [In] STARTUPINFO lpStartupInfo,
            IntPtr lpProcessInformation
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(String applicationName, String commandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandles, Int32 dwCreationFlags, IntPtr lpEnvironment, String lpCurrentDirectory,
            [MarshalAs(UnmanagedType.LPStruct)] [In] STARTUPINFO lpStartupInfo,
            [MarshalAs(UnmanagedType.LPStruct)] [In] PROCESS_INFORMATION lpProcessInformation);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hwnd, Int32 x, Int32 y, Int32 h, Int32 w, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hwnd, out WINDOWINFO wi);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, Int32 msg, Int32 wparam, Int32 lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, Int32 msg, Int32 wparam, Int32 lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern bool RemoveMenu(IntPtr hMenu, Int32 nPosition, Int32 nFlags);

        [DllImport("user32.dll")]
        public static extern Int32 GetMenuItemCount(IntPtr hMenu);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMenuItemInfo(IntPtr hMenu, Int32 item, bool bByPosition,
            [MarshalAs(UnmanagedType.LPStruct)] [In] [Out] MENUITEMINFO mii);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, Int32 uFlags);

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class MENUITEMINFO {
            public readonly Int32 cbSize = Marshal.SizeOf(typeof (MENUITEMINFO));
            public MIIM fMask;
            public Int32 fType;
            public Int32 fState;
            public Int32 wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;

            [MarshalAs(UnmanagedType.LPWStr, SizeConst = 255)] 
            public String dwTypeData = new String(' ', 256);
            public readonly Int32 cch = 255;
            public IntPtr hbmpItem;
        }

        public struct RECT {
            public Int32 left;
            public Int32 top;
            public Int32 right;
            public Int32 bottom;
        }

        public struct WINDOWINFO {
            public Int32 cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public Int32 dwStyle;
            public Int32 dwExStyle;
            public Int32 dwWindowStatus;
            public Int32 cxWindowBorders;
            public Int32 cyWindowBorders;
            public Int16 atomWindowType;
            public Int16 wCreatorVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SECURITY_ATTRIBUTES {
            public readonly Int32 nLength = Marshal.SizeOf(typeof (SECURITY_ATTRIBUTES));
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public struct COORD {
            public Int16 X;
            public Int16 Y;
        }

        public struct CONSOLE_FONT_INFO {
            public Int32 nFont;
            public COORD dwFontSize;
        }

        public struct SMALL_RECT {
            public Int16 Left;
            public Int16 Top;
            public Int16 Right;
            public Int16 Bottom;
        }

        public struct CONSOLE_SCREEN_BUFFER_INFO {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public Int16 wAttributes;
            public SMALL_RECT srWindow;
            public Int16 dwMaximumWindowSize;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class KEY_INPUT_RECORD {
            public Int16 EventType = KEY_EVENT; //this is only one of several possible cases of the INPUT_RECORD structure

            //the rest of fields are from KEY_EVENT_RECORD :
            public bool bKeyDown;
            public Int16 wRepeatCount;
            public Int16 wVirtualKeyCode;
            public Int16 wVirtualScanCode;
            public char UnicodeChar;
            public Int32 dwControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessId;
            public Int32 dwThreadId;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class STARTUPINFO {
            public Int32 cb = Marshal.SizeOf(typeof (STARTUPINFO));
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class FOCUS_INPUT_RECORD {
            public Int16 EventType;
            public bool bSetFocus;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)] public String wszTitle = new String(' ', 256);
        }

        public enum ConsoleEvents {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public const Int32 CONSOLE_TEXTMODE_BUFFER = 1;
        public const Int32 KEY_EVENT = 0x0001;
        public const Int32 FOCUS_EVENT = 0x0010;
        public const Int32 SW_SHOW = 5;
        public const Int32 WM_KEYDOWN = 0x100;
        public const Int32 WM_COMMAND = 0x112;
        public const Int32 WM_CLOSE = 0x0010;
        public const int CREATE_NEW_CONSOLE = 0x00000010;


        [Flags]
        public enum MIIM {
            STATE = 0x00000001,
            ID = 0x00000002,
            SUBMENU = 0x00000004,
            CHECKMARKS = 0x00000008,
            TYPE = 0x00000010,
            DATA = 0x00000020,
            STRING = 0x00000040,
            BITMAP = 0x00000080,
            FTYPE = 0x00000100
        }


        [Flags]
        [CLSCompliant(false)]
        public enum DESIRED_ACCESS : uint {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000
        }

        public enum FILE_SHARE {
            READ = 0x00000001,
            WRITE = 0x00000002,
            DELETE = 0x00000004
        }

        public enum STD_HANDLE {
            INPUT = -10,
            OUTPUT = -11,
            ERROR = -12
        }
    }
}