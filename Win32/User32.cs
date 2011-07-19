//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Summary description for Win32.
    /// </summary>
    public static class User32
    {

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd,int id,int fsModifiers,int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        [DllImport("user32.dll")]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Sends the specified message to a window or windows. The function 
        /// calls the window procedure for the specified window and does not 
        /// return until the window procedure has processed the message. 
        /// </summary>
        /// <param name="hWnd">
        /// Handle to the window whose window procedure will receive the 
        /// message.
        /// </param>
        /// <param name="Msg">Specifies the message to be sent.</param>
        /// <param name="wParam">
        /// Specifies additional message-specific information.
        /// </param>
        /// <param name="lParam">
        /// Specifies additional message-specific information.
        /// </param>
        /// <returns></returns>
        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hwnd, Int32 x, Int32 y, Int32 h, Int32 w, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hwnd, out WindowInfo wi);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, Int32 msg, Int32 wparam, Int32 lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, Int32 msg, Int32 wparam, Int32 lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(Int32 hwnd, Int32 msg, Int32 wparam, Int32 lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(Int32 hwnd, Int32 msg, Int32 wparam, [MarshalAs(UnmanagedType.LPStr)] string lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern bool RemoveMenu(IntPtr hMenu, Int32 nPosition, Int32 nFlags);

        [DllImport("user32.dll")]
        public static extern Int32 GetMenuItemCount(IntPtr hMenu);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMenuItemInfo(IntPtr hMenu, Int32 item, bool bByPosition, [MarshalAs(UnmanagedType.LPStruct)] [In] [Out] MenuItemInfo mii);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, Int32 uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();
    }
}