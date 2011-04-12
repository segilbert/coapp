//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;

    public struct WindowInfo {
        public Int32 cbSize;
        public Rect rcWindow;
        public Rect rcClient;
        public Int32 dwStyle;
        public Int32 dwExStyle;
        public Int32 dwWindowStatus;
        public Int32 cxWindowBorders;
        public Int32 cyWindowBorders;
        public Int16 atomWindowType;
        public Int16 wCreatorVersion;
    }
}