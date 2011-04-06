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

    }
}