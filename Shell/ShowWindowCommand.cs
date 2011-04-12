//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original (c) Author: Richard G Russell (Foredecker) 
//     Changes Copyright (c) 2010  Garrett Serack, CoApp Contributors. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) Author: Richard G Russell (Foredecker) 
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Shell {
    /// <summary>
    ///   Window 'show' commands - seee the Win32 ShowWindow() API for more info.
    /// </summary>
    public enum ShowWindowCommand {
        /// <summary>
        ///   SW_FORCEMINIMIZE - minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
        /// </summary>
        ForceMinimize = 11,

        /// <summary>
        ///   SW_HIDE - Hides the window and activates another window.
        /// </summary>
        Hide = 0,

        /// <summary>
        ///   SW_MAXIMIZE - Maximizes the specified window.
        /// </summary>
        Maximize = 3,

        /// <summary>
        ///   SW_MINIMIZE - Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        Minimize = 6,

        /// <summary>
        ///   SW_RESTORE - Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,

        /// <summary>
        ///   SW_SHOW - Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,

        /// <summary>
        ///   SW_SHOWDEFAULT - Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        /// </summary>
        ShowDeafult = 10,

        /// <summary>
        ///   SW_SHOWMAXIMIZED - Activates the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = 3,

        /// <summary>
        ///   SW_SHOWMINIMIZED - Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,

        /// <summary>
        ///   SW_SHOWMINNOACTIVE - Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
        /// </summary>
        ShowMinNoActive = 7,

        /// <summary>
        ///   SW_SHOWNORMAL - Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
        /// </summary>
        ShowNormal = 1
    }
}