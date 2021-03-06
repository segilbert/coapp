//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     ResourceLib Original Code from http://resourcelib.codeplex.com
//     Original Copyright (c) 2008-2009 Vestris Inc.
//     Changes Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
// MIT License
// You may freely use and distribute this software under the terms of the following license agreement.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    /// <summary>
    ///   Specifies whether the menu item is the last item in the menu bar, drop-down menu, submenu, or shortcut 
    ///   menu and whether it is an item that opens a drop-down menu or submenu. This member can be zero or more 
    ///   of these values.
    /// </summary>
    public enum MenuResourceType {
        /// <summary>
        ///   Defines the last menu item in the menu bar, drop-down menu, submenu, or shortcut menu.
        /// </summary>
        Last = 0x80,

        /// <summary>
        ///   Defines a item that opens a drop-down menu or submenu. Subsequent structures define menu 
        ///   items in the corresponding drop-down menu or submenu.
        /// </summary>
        Sub = 0x01
    }
}