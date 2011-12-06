//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.AutoItX3.x64 {
    using System.Runtime.InteropServices;
    using CoApp.Toolkit.AutoItX3;

    /// <summary>
    /// Native Code Access for the X64 version of the AutoItX3 library
    /// </summary>
    internal static class NativeMethods {
        #region | Notes |

        // From memory optional parameters are not supported in DotNet so fill in all fields even if just string.Empty.
        // Be prepared to play around a bit with which fields need values and what those value are.
        //
        // The big advantage of using AutoItX3 like this is that you don't have to register
        //   the dll with windows and more importantly you get away from the many issues involved in
        //   publishing the application and the binding to the dll required.
        //
        // Get definitions by using "DLL Export Viewer" utility to get Properties Definitions
        //   "DLL Export Viewer" is from http://www.nirsoft.net
        #endregion

        #region | AutoItX3.dll Exported Methods |

        // AU3_API void WINAPI AU3_Init(void);
        // Uncertain if this is needed
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_Init();

        // AU3_API long AU3_error(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_error();

        // AU3_API long WINAPI AU3_AutoItSetOption(const char *szOption, long nValue);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_AutoItSetOption([MarshalAs(UnmanagedType.LPWStr)] string option, int value);

        // AU3_API void WINAPI AU3_BlockInput(long nFlag);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_BlockInput(int flag);

        // AU3_API long WINAPI AU3_CDTray(const char *szDrive, const char *szAction);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_CDTray([MarshalAs(UnmanagedType.LPWStr)] string drive, [MarshalAs(UnmanagedType.LPWStr)] string action);

        // AU3_API void WINAPI AU3_ClipGet(char *szClip, int nBufSize);
        /* 
        // Use like this:
        byte[] returnclip = new byte[200]; //any sufficiently long length will do
        AU3_ClipGet(returnclip, returnclip.Length);
        clipdata = new UnicodeEncoding().GetString(returnclip);
        */

        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ClipGet(byte[] clip, int bufSize);

        // AU3_API void WINAPI AU3_ClipPut(const char *szClip);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ClipPut([MarshalAs(UnmanagedType.LPWStr)] string clip);

        // AU3_API long WINAPI AU3_ControlClick(const char *szTitle, const char *szText, const char *szControl, const char *szButton, long nNumClicks, /*[in,defaultvalue(AutoItMaxInt)]*/long nX, /*[in,defaultvalue(AutoItMaxInt)]*/long nY);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlClick([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, [MarshalAs(UnmanagedType.LPWStr)] string button, int numClicks, int xpos, int ypos);

        // AU3_API void WINAPI AU3_ControlCommand(const char *szTitle, const char *szText, const char *szControl, const char *szCommand, const char *szExtra, char *szResult, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ControlCommand([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, [MarshalAs(UnmanagedType.LPWStr)] string command, [MarshalAs(UnmanagedType.LPWStr)] string extra, [MarshalAs(UnmanagedType.LPWStr)] byte[] result, int bufSize);

        // AU3_API void WINAPI AU3_ControlListView(const char *szTitle, const char *szText, const char *szControl, const char *szCommand, const char *szExtra1, const char *szExtra2, char *szResult, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ControlListView([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, [MarshalAs(UnmanagedType.LPWStr)] string command, [MarshalAs(UnmanagedType.LPWStr)] string extral1, [MarshalAs(UnmanagedType.LPWStr)] string extra2, byte[] result, int bufSize);

        // AU3_API long WINAPI AU3_ControlDisable(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlDisable([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API long WINAPI AU3_ControlEnable(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlEnable([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API long WINAPI AU3_ControlFocus(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlFocus([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API void WINAPI AU3_ControlGetFocus(const char *szTitle, const char *szText, char *szControlWithFocus, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ControlGetFocus([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, byte[] controlWithFocus, int bufSize);

        // AU3_API void WINAPI AU3_ControlGetHandle(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, const char *szControl, char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ControlGetHandle([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, byte[] retText, int bufSize);

        // AU3_API long WINAPI AU3_ControlGetPosX(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlGetPosX([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API long WINAPI AU3_ControlGetPosY(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlGetPosY([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API long WINAPI AU3_ControlGetPosHeight(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlGetPosHeight([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API long WINAPI AU3_ControlGetPosWidth(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlGetPosWidth([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API void WINAPI AU3_ControlGetText(const char *szTitle, const char *szText, const char *szControl, char *szcontrolText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ControlGetText([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, byte[] controlText, int bufSize);

        // AU3_API long WINAPI AU3_ControlHide(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlHide([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API long WINAPI AU3_ControlMove(const char *szTitle, const char *szText, const char *szControl, long nX, long nY, /*[in,defaultvalue(-1)]*/long nWidth, /*[in,defaultvalue(-1)]*/long nHeight);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlMove([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, int xpos, int ypos, int width, int height);

        // AU3_API long WINAPI AU3_ControlSend(const char *szTitle, const char *szText, const char *szControl, const char *szSendText, /*[in,defaultvalue(0)]*/long nMode);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlSend([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, [MarshalAs(UnmanagedType.LPWStr)] string sendText, int mode);

        // AU3_API long WINAPI AU3_ControlSetText(const char *szTitle, const char *szText, const char *szControl, const char *szcontrolText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlSetText([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, [MarshalAs(UnmanagedType.LPWStr)] string controlText);

        // AU3_API long WINAPI AU3_ControlShow(const char *szTitle, const char *szText, const char *szControl);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ControlShow([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control);

        // AU3_API void WINAPI AU3_ControlTreeView(const char *szTitle, const char *szText, const char *szControl, const char *szCommand, const char *szExtra1, const char *szExtra2, char *szResult, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ControlTreeView([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string control, [MarshalAs(UnmanagedType.LPWStr)] string command, [MarshalAs(UnmanagedType.LPWStr)] string extra1, [MarshalAs(UnmanagedType.LPWStr)] string extra2, byte[] result, int bufSize);

        // AU3_API void WINAPI AU3_DriveMapAdd(const char *szDevice, const char *szShare, long nFlags, /*[in,defaultvalue(string.Empty)]*/const char *szUser, /*[in,defaultvalue(string.Empty)]*/const char *szPwd, char *szResult, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_DriveMapAdd([MarshalAs(UnmanagedType.LPWStr)] string device, [MarshalAs(UnmanagedType.LPWStr)] string share, int flags, [MarshalAs(UnmanagedType.LPWStr)] string user, [MarshalAs(UnmanagedType.LPWStr)] string pwd, byte[] result, int bufSize);

        // AU3_API long WINAPI AU3_DriveMapDel(const char *szDevice);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_DriveMapDel([MarshalAs(UnmanagedType.LPWStr)] string device);

        // AU3_API void WINAPI AU3_DriveMapGet(const char *szDevice, char *szMapping, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_DriveMapDel([MarshalAs(UnmanagedType.LPWStr)] string device, byte[] mapping, int bufSize);

        // AU3_API long WINAPI AU3_IniDelete(const char *szFilename, const char *szSection, const char *szKey);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_IniDelete([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string section, [MarshalAs(UnmanagedType.LPWStr)] string key);

        // AU3_API void WINAPI AU3_IniRead(const char *szFilename, const char *szSection, const char *szKey, const char *szDefault, char *szValue, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_IniRead([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string section, [MarshalAs(UnmanagedType.LPWStr)] string key, [MarshalAs(UnmanagedType.LPWStr)] string @default, byte[] value, int bufSize);

        // AU3_API long WINAPI AU3_IniWrite(const char *szFilename, const char *szSection, const char *szKey, const char *szValue);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_IniWrite([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string section, [MarshalAs(UnmanagedType.LPWStr)] string key, [MarshalAs(UnmanagedType.LPWStr)] string value);

        // AU3_API long WINAPI AU3_IsAdmin(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_IsAdmin();

        // AU3_API long WINAPI AU3_MouseClick(/*[in,defaultvalue("LEFT")]*/const char *szButton, /*[in,defaultvalue(AutoItMaxInt)]*/long nX, /*[in,defaultvalue(AutoItMaxInt)]*/long nY, /*[in,defaultvalue(1)]*/long nClicks, /*[in,defaultvalue(-1)]*/long nSpeed);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_MouseClick([MarshalAs(UnmanagedType.LPWStr)] string button, int x, int y, int clicks, int speed);

        // AU3_API long WINAPI AU3_MouseClickDrag(const char *szButton, long nX1, long nY1, long nX2, long nY2, /*[in,defaultvalue(-1)]*/long nSpeed);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_MouseClickDrag([MarshalAs(UnmanagedType.LPWStr)] string button, int xpos1, int ypos1, int xpos2, int ypos2, int speed);

        // AU3_API void WINAPI AU3_MouseDown(/*[in,defaultvalue("LEFT")]*/const char *szButton);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_MouseDown([MarshalAs(UnmanagedType.LPWStr)] string button);

        // AU3_API long WINAPI AU3_MouseGetCursor(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_MouseGetCursor();

        // AU3_API long WINAPI AU3_MouseGetPosX(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_MouseGetPosX();

        // AU3_API long WINAPI AU3_MouseGetPosY(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_MouseGetPosY();

        // AU3_API long WINAPI AU3_MouseMove(long nX, long nY, /*[in,defaultvalue(-1)]*/long nSpeed);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_MouseMove(int xpos, int ypos, int speed);

        // AU3_API void WINAPI AU3_MouseUp(/*[in,defaultvalue("LEFT")]*/const char *szButton);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_MouseUp([MarshalAs(UnmanagedType.LPWStr)] string button);

        // AU3_API void WINAPI AU3_MouseWheel(const char *szDirection, long nClicks);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_MouseWheel([MarshalAs(UnmanagedType.LPWStr)] string direction, int clicks);

        // AU3_API long WINAPI AU3_Opt(const char *szOption, long nValue);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_Opt([MarshalAs(UnmanagedType.LPWStr)] string option, int value);

        // AU3_API long WINAPI AU3_PixelChecksum(long nLeft, long nTop, long nRight, long nBottom, /*[in,defaultvalue(1)]*/long nStep);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_PixelChecksum(int left, int top, int right, int bottom, int step);

        // AU3_API long WINAPI AU3_PixelGetColor(long nX, long nY);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_PixelGetColor(int xpos, int ypos);

        // AU3_API void WINAPI AU3_PixelSearch(long nLeft, long nTop, long nRight, long nBottom, long nCol, /*default 0*/long nVar, /*default 1*/long nStep, LPPOINT pPointResult);

        /* Use like this:
        int[] result = {0,0};
        try {
            AU3_PixelSearch(0, 0, 800, 000,0xFFFFFF, 0, 1, result);
        }
        catch { 
        }

        // It will crash if the color is not found, have not be able to determin why
        // The AutoItX3Lib.AutoItX3Class version has similar problems and is the only function to return an object
        // so contortions are needed to get the data from it ie:
         * 
        int[] result = {0,0};
        object resultObj;
        AutoItX3Lib.AutoItX3Class autoit = new AutoItX3Lib.AutoItX3Class();
        resultObj = autoit.PixelSearch(0, 0, 800, 600, 0xFFFF00,0,1);
        Type t = resultObj.GetType();
        if(t == typeof(object[])) {
            object[] obj = (object[])resultObj;
            result[0] = (int)obj[0];
            result[1] = (int)obj[1];
        }
        // When it fails it returns an object = 1 but when it succeeds it is object[X,Y]
        */

        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_PixelSearch(int left, int top, int right, int bottom, int col, int var, int step, int[] pointResult);

        // AU3_API long WINAPI AU3_ProcessClose(const char *szProcess);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ProcessClose([MarshalAs(UnmanagedType.LPWStr)] string process);

        // AU3_API long WINAPI AU3_ProcessExists(const char *szProcess);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ProcessExists([MarshalAs(UnmanagedType.LPWStr)] string process);

        // AU3_API long WINAPI AU3_ProcessSetPriority(const char *szProcess, long nPriority);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ProcessSetPriority([MarshalAs(UnmanagedType.LPWStr)] string process, int priority);

        // AU3_API long WINAPI AU3_ProcessWait(const char *szProcess, /*[in,defaultvalue(0)]*/long nTimeout);
        // Not checked jde
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ProcessWait([MarshalAs(UnmanagedType.LPWStr)] string process, int timeout);

        // AU3_API long WINAPI AU3_ProcessWaitClose(const char *szProcess, /*[in,defaultvalue(0)]*/long nTimeout);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_ProcessWaitClose([MarshalAs(UnmanagedType.LPWStr)] string process, int timeout);

        // AU3_API long WINAPI AU3_RegDeleteKey(const char *szKeyname);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_RegDeleteKey([MarshalAs(UnmanagedType.LPWStr)] string keyname);

        // AU3_API long WINAPI AU3_RegDeleteVal(const char *szKeyname, const char *szValuename);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_RegDeleteVal([MarshalAs(UnmanagedType.LPWStr)] string keyname, [MarshalAs(UnmanagedType.LPWStr)] string valueName);

        // AU3_API void WINAPI AU3_RegEnumKey(const char *szKeyname, long nInstance, char *szResult, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_RegEnumKey([MarshalAs(UnmanagedType.LPWStr)] string keyname, int instance, byte[] result, int bufSize);

        // AU3_API void WINAPI AU3_RegEnumVal(const char *szKeyname, long nInstance, char *szResult, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_RegEnumVal([MarshalAs(UnmanagedType.LPWStr)] string keyname, int instance, byte[] result, int bufSize);

        // AU3_API void WINAPI AU3_RegRead(const char *szKeyname, const char *szValuename, char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_RegRead([MarshalAs(UnmanagedType.LPWStr)] string keyname, [MarshalAs(UnmanagedType.LPWStr)] string valueName, byte[] retText, int bufSize);

        // AU3_API long WINAPI AU3_RegWrite(const char *szKeyname, const char *szValuename, const char *szType, const char *szValue);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_RegWrite([MarshalAs(UnmanagedType.LPWStr)] string keyname, [MarshalAs(UnmanagedType.LPWStr)] string valuename, [MarshalAs(UnmanagedType.LPWStr)] string type, [MarshalAs(UnmanagedType.LPWStr)] string value);

        // AU3_API long WINAPI AU3_Run(const char *szRun, /*[in,defaultvalue(string.Empty)]*/const char *szDir, /*[in,defaultvalue(1)]*/long nShowFlags);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_Run([MarshalAs(UnmanagedType.LPWStr)] string run, [MarshalAs(UnmanagedType.LPWStr)] string dir, AutoItX.Visibility showFlags);

        // AU3_API long WINAPI AU3_RunAsSet(const char *szUser, const char *szDomain, const char *szPassword, int nOptions);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_RunAsSet([MarshalAs(UnmanagedType.LPWStr)] string user, [MarshalAs(UnmanagedType.LPWStr)] string domain, [MarshalAs(UnmanagedType.LPWStr)] string password, int options);

        // AU3_API long WINAPI AU3_RunWait(const char *szRun, /*[in,defaultvalue(string.Empty)]*/const char *szDir, /*[in,defaultvalue(1)]*/long nShowFlags);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_RunWait([MarshalAs(UnmanagedType.LPWStr)] string run, [MarshalAs(UnmanagedType.LPWStr)] string dir, int showFlags);

        // AU3_API void WINAPI AU3_Send(const char *szSendText, /*[in,defaultvalue(0)]*/long nMode);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_Send([MarshalAs(UnmanagedType.LPWStr)] string sendText, int mode);

        // AU3_API long WINAPI AU3_Shutdown(long nFlags);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_Shutdown(int flags);

        // AU3_API void WINAPI AU3_Sleep(long nMilliseconds);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_Sleep(int milliseconds);

        // AU3_API void WINAPI AU3_StatusbarGetText(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, /*[in,defaultvalue(1)]*/long nPart, char *szStatusText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_StatusbarGetText([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int part, byte[] statusText, int bufSize);

        // AU3_API void WINAPI AU3_ToolTip(const char *szTip, /*[in,defaultvalue(AutoItMaxInt)]*/long nX, /*[in,defaultvalue(AutoItMaxInt)]*/long nY);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_ToolTip([MarshalAs(UnmanagedType.LPWStr)] string tip, int xpos, int ypos);

        // AU3_API void WINAPI AU3_WinActivate(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinActivate([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinActive(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinActive([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinClose(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinClose([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinExists(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinExists([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinGetCaretPosX(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetCaretPosX();

        // AU3_API long WINAPI AU3_WinGetCaretPosY(void);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetCaretPosY();

        // AU3_API void WINAPI AU3_WinGetClassList(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinGetClassList([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, byte[] retText, int bufSize);

        // AU3_API long WINAPI AU3_WinGetClientSizeHeight(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetClientSizeHeight([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinGetClientSizeWidth(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetClientSizeWidth([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API void WINAPI AU3_WinGetHandle(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinGetHandle([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, byte[] retText, int bufSize);

        // AU3_API long WINAPI AU3_WinGetPosX(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetPosX([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinGetPosY(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetPosY([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinGetPosHeight(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetPosHeight([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinGetPosWidth(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetPosWidth([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API void WINAPI AU3_WinGetProcess(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinGetProcess([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, byte[] retText, int bufSize);

        // AU3_API long WINAPI AU3_WinGetState(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinGetState([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API void WINAPI AU3_WinGetText(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinGetText([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, byte[] retText, int bufSize);

        // AU3_API void WINAPI AU3_WinGetTitle(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText , char *szRetText, int nBufSize);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinGetTitle([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, byte[] retText, int bufSize);

        // AU3_API long WINAPI AU3_WinKill(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinKill([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text);

        // AU3_API long WINAPI AU3_WinMenuSelectItem(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, const char *szItem1, const char *szItem2, const char *szItem3, const char *szItem4, const char *szItem5, const char *szItem6, const char *szItem7, const char *szItem8);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinMenuSelectItem([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string item1, [MarshalAs(UnmanagedType.LPWStr)] string item2, [MarshalAs(UnmanagedType.LPWStr)] string item3, [MarshalAs(UnmanagedType.LPWStr)] string item4, [MarshalAs(UnmanagedType.LPWStr)] string item5, [MarshalAs(UnmanagedType.LPWStr)] string item6, [MarshalAs(UnmanagedType.LPWStr)] string item7, [MarshalAs(UnmanagedType.LPWStr)] string item8);

        // AU3_API void WINAPI AU3_WinMinimizeAll();
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinMinimizeAll();

        // AU3_API void WINAPI AU3_WinMinimizeAllUndo();
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void AU3_WinMinimizeAllUndo();

        // AU3_API long WINAPI AU3_WinMove(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, long nX, long nY, /*[in,defaultvalue(-1)]*/long nWidth, /*[in,defaultvalue(-1)]*/long nHeight);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinMove([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int xpos, int ypos, int width, int height);

        // AU3_API long WINAPI AU3_WinSetOnTop(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, long nFlag);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinMove([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int flags);

        // AU3_API long WINAPI AU3_WinSetState(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, long nFlags);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinSetState([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int flags);

        // AU3_API long WINAPI AU3_WinSetTitle(const char *szTitle,/*[in,defaultvalue(string.Empty)]*/ const char *szText, const char *szNewTitle);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinSetTitle([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string newTitle);

        // AU3_API long WINAPI AU3_WinSetTrans(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, long nTrans);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinSetTrans([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int trans);

        // AU3_API long WINAPI AU3_WinWait(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText , /*[in,defaultvalue(0)]*/long nTimeout);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinWait([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int timeout);

        // AU3_API long WINAPI AU3_WinWaitActive(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText  , /*[in,defaultvalue(0)]*/long nTimeout);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinWaitActive([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int timeout);

        // AU3_API long WINAPI AU3_WinWaitClose(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, /*[in,defaultvalue(0)]*/long nTimeout);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinWaitClose([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int timeout);

        // AU3_API long WINAPI AU3_WinWaitNotActive(const char *szTitle, /*[in,defaultvalue(string.Empty)]*/const char *szText, /*[in,defaultvalue(0)]*/long nTimeout);
        [DllImport("AutoItX3_x64.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int AU3_WinWaitNotActive([MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string text, int timeout);

        #endregion
    }
}
