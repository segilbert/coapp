//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.AutoItX3 {
    using System;
    using System.Text;
    using x64;

    /// <summary>
    /// Some Summary
    /// </summary>
    public static class AutoItX {
        /// <summary>
        /// Determines if the current platform is 64 bit.
        /// </summary>
        private static readonly bool X64 = Win32.WindowsVersionInfo.IsCurrentProcess64Bit;

        #region | Constants |
        /// <summary>
        /// "Default" value for _some_ int parameters (largest negative number)
        /// </summary>
        public const int AutoItMaxVal = -2147483647; 

        /// <summary>
        /// Error Value
        /// </summary>
        public const int Error = 1;

        /// <summary>
        /// AutoIt Version number?
        /// </summary>
        public const int Version = 109;

        #endregion

        #region | Public Methods |

        /// <summary>
        /// Disable/enable the mouse and keyboard.
        /// </summary>
        /// <param name="disableUserInput">1 = Disable user input 0 = Enable user input</param>
        public static void BlockInput(int disableUserInput) {
            if(X64) {
                NativeMethods.AU3_BlockInput(disableUserInput);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_BlockInput(disableUserInput);
            }
        }

        /// <summary>
        /// Sends a mouse click command to a given control.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <param name="button">[optional] The button to click, "left", "right", "middle", "main", "menu", "primary", "secondary". Default is the left button.</param>
        /// <param name="numClicks">[optional] The number of times to click the mouse. Default is 1.</param>
        /// <param name="xcoord">[optional] The x position to click within the control. Default is center.</param>
        /// <param name="ycoord">[optional] The y position to click within the control. Default is center.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0.</returns>
        public static int ControlClick(string title, string text, string control, string button, int numClicks, int xcoord, int ycoord) {
            return X64 ? NativeMethods.AU3_ControlClick(title, text, control, button, numClicks, xcoord, ycoord) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlClick(title, text, control, button, numClicks, xcoord, ycoord);
        }

        /// <summary>
        /// Sends a mouse click command to a given control.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0.</returns>
        public static int ControlClick(string title, string control) {
            return X64 ? NativeMethods.AU3_ControlClick(title, string.Empty, control, string.Empty, 1, 0, 0) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlClick(title, string.Empty, control, string.Empty, 1, 0, 0);
        }

        /// <summary>
        /// Enables a "grayed-out" control.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0.</returns>
        public static int ControlDisable(string title, string text, string control) {
            return X64 ? NativeMethods.AU3_ControlDisable(title, text, control) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlDisable(title, text, control);
        }

        /// <summary>
        /// Disables a "grayed-out" control.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0.</returns>
        public static int ControlEnable(string title, string text, string control) {
            return X64 ? NativeMethods.AU3_ControlEnable(title, text, control) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlEnable(title, text, control);
        }

        /// <summary>
        /// Sets input focus to a given control on a window.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0.</returns>
        public static int ControlFocus(string title, string text, string control) {
            return X64 ? NativeMethods.AU3_ControlFocus(title, text, control) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlFocus(title, text, control);
        }

        /// <summary>
        /// Sets input focus to a given control on a window.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0.</returns>
        public static int ControlFocus(string title, string control) {
            return X64 ? NativeMethods.AU3_ControlFocus(title, string.Empty, control) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlFocus(title, string.Empty, control);
        }

        /// <summary>
        /// Retrieves the internal handle of a control.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>
        ///     Handle of the control
        /// </returns>
        public static string ControlGetHandle(string title, string text, string control) {
            //----------------------------------------------------------------------
            // A number big enought to hold result, trailing bytes will be 0
            //----------------------------------------------------------------------
            var retText = new byte[50];

            if(X64) {
                NativeMethods.AU3_ControlGetHandle(title, text, control, retText, retText.Length);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlGetHandle(title, text, control, retText, retText.Length);
            }

            //----------------------------------------------------------------------
            // May need to convert back to a string or int
            //----------------------------------------------------------------------
            return Encoding.Unicode.GetString(retText).TrimEnd('\0');
        }

        /// <summary>
        /// Retrieves the position and size of a control relative to it's window.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Returns a ControlPosition struct containing the size and the control's position relative to it's client window:
        /// ControlPosition { int xpos, int ypos, int width, int height }</returns>
        public static Position2D ControlGetPos(string title, string text, string control) {
            Position2D pos;
            if (X64) {
                pos.Xpos = NativeMethods.AU3_ControlGetPosX(title, text, control);
                pos.Ypos = NativeMethods.AU3_ControlGetPosY(title, text, control);
                pos.Height = NativeMethods.AU3_ControlGetPosHeight(title, text, control);
                pos.Width = NativeMethods.AU3_ControlGetPosWidth(title, text, control);
                return pos;
            }

            pos.Xpos = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlGetPosX(title, text, control);
            pos.Ypos = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlGetPosY(title, text, control);
            pos.Height = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlGetPosHeight(title, text, control);
            pos.Width = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlGetPosWidth(title, text, control);

            return pos;
        }

        /// <summary>
        /// Hides a control.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0 if window/control is not found.</returns>
        public static int ControlHide(string title, string text, string control) {
            return X64 ? NativeMethods.AU3_ControlHide(title, text, control) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlHide(title, text, control);
        }

        /// <summary>
        /// Moves a control within a window.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <param name="vsX">xpos coordinate to move to relative to the window client area.</param>
        /// <param name="vsY">ypos coordinate to move to relative to the window client area.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0 if window/control is not found.</returns>
        public static int ControlMove(string title, string text, string control, int vsX, int vsY) {
            int height = ControlGetPos(title, text, control).Height;
            int width = ControlGetPos(title, text, control).Width;
            return X64 ? NativeMethods.AU3_ControlMove(title, text, control, vsX, vsY, height, width) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlMove(title, text, control, vsX, vsY, height, width);
        }

        /// <summary>
        /// Moves a control within a window.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <param name="vsX">xpos coordinate to move to relative to the window client area.</param>
        /// <param name="vsY">ypos coordinate to move to relative to the window client area.</param>
        /// <param name="height">New width of the window.</param>
        /// <param name="width">New height of the window.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0 if window/control is not found.</returns>
        public static int ControlMove(string title, string text, string control, int vsX, int vsY, int height, int width) {
            return X64 ? NativeMethods.AU3_ControlMove(title, text, control, vsX, vsY, height, width) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlMove(title, text, control, vsX, vsY, height, width);
        }

        /// <summary>
        /// Sends a string of characters to a control
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <param name="sendText">string of characters to send to the control.</param>
        /// <param name="mode">
        /// [optional] Changes how "keys" is processed:
        /// flag = 0 (default), Text contains special characters like + to indicate
        ///     SHIFT and {LEFT} to indicate left arrow.
        /// flag = 1, keys are sent raw.
        /// </param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0 if window/control is not found.</returns>
        public static int ControlSend(string title, string text, string control, string sendText, int mode) {
            return X64 ? NativeMethods.AU3_ControlSend(title, text, control, sendText, mode) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlSend(title, text, control, sendText, mode);
        }

        /// <summary>
        /// Shows a control that was hidden.
        /// </summary>
        /// <param name="title">The title of the window to access.</param>
        /// <param name="text">The text of the window to access.</param>
        /// <param name="control">The control to interact with. See Controls.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0 if window/control is not found.</returns>
        public static int ControlShow(string title, string text, string control) {
            return X64 ? NativeMethods.AU3_ControlShow(title, text, control) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ControlShow(title, text, control);
        }

        /// <summary>
        /// Perform a mouse click operation.
        /// </summary>
        /// <param name="button">The button to click: "left", "right", "middle", "main", "menu", "primary", "secondary".</param>
        /// <param name="xcoord">[optional] The x coordinates to move the mouse to. If no x and y coords are given, the current position is used (default).</param>
        /// <param name="ycoord">[optional] The y coordinates to move the mouse to. If no x and y coords are given, the current position is used (default).</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0, the button is not in the list.</returns>
        public static int MouseClick(string button, int xcoord, int ycoord) {
            return MouseClick(button, xcoord, ycoord, 1, 1);
        }

        /// <summary>
        /// Perform a mouse click operation.
        /// </summary>
        /// <param name="button">The button to click: "left", "right", "middle", "main", "menu", "primary", "secondary".</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0, the button is not in the list.</returns>
        public static int MouseClick(string button) {
            int mousex;
            int mousey;
            if(X64) {
                mousex = NativeMethods.AU3_MouseGetPosX();
                mousey = NativeMethods.AU3_MouseGetPosY();
            }
            else {
                mousex = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseGetPosX();
                mousey = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseGetPosY();
            }

            return MouseClick(button, mousex, mousey);
        }

        /// <summary>
        /// Perform a mouse click operation.
        /// </summary>
        /// <param name="button">The button to click: "left", "right", "middle", "main", "menu", "primary", "secondary".</param>
        /// <param name="xcoord">[optional] The x coordinates to move the mouse to. If no x and y coords are given, the current position is used (default).</param>
        /// <param name="ycoord">[optional] The y coordinates to move the mouse to. If no x and y coords are given, the current position is used (default).</param>
        /// <param name="clicks">[optional] The number of times to click the mouse. Default is 1.</param>
        /// <param name="speed">[optional] the speed to move the mouse in the range 1 (fastest) to 100 (slowest). A speed of 0 will move the mouse instantly. Default speed is 10.</param>
        /// <returns>Success:   Returns 1.
        /// Failure:  Returns 0, the button is not in the list.</returns>
        public static int MouseClick(string button, int xcoord, int ycoord, int clicks, int speed) {
            //----------------------------------------------------------------------
            // MouseClick wasn't working with out first MouseMove call
            //----------------------------------------------------------------------
            if(X64) {
                NativeMethods.AU3_MouseMove(xcoord, ycoord, 10);
                return NativeMethods.AU3_MouseClick(button, xcoord, ycoord, clicks, speed);
            }
            
            global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseMove(xcoord, ycoord, 10);
            return global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseClick(button, xcoord, ycoord, clicks, speed);
        }

        /// <summary>
        /// Perform a mouse click and drag operation.
        /// </summary>
        /// <param name="button">The button to click: "left", "right", "middle", "main", "menu", "primary", "secondary".</param>
        /// <param name="xcoord1">The x coords to start the drag operation from.</param>
        /// <param name="ycoord1">The y coords to start the drag operation from.</param>
        /// <param name="xcoord2">The x coords to end the drag operation at.</param>
        /// <param name="ycoord2">The y coords to end the drag operation at.</param>
        /// <param name="speed">[optional] the speed to move the mouse in the range 1 (fastest) to 100 (slowest). A speed of 0 will move the mouse instantly. Default speed is 10.</param>
        /// <returns>
        /// Success: Returns 1.
        /// Failure: Returns 0, the button is not in the list.
        /// </returns>
        public static int MouseClickDrag(string button, int xcoord1, int ycoord1, int xcoord2, int ycoord2, int speed) {
            return X64 ? NativeMethods.AU3_MouseClickDrag(button, xcoord1, ycoord1, xcoord2, ycoord2, speed) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseClickDrag(button, xcoord1, ycoord1, xcoord2, ycoord2, speed);
        }

        /// <summary>
        /// Perform a mouse down event at the current mouse position.
        /// </summary>
        /// <param name="button">The button to click: "left", "right", "middle", "main", "menu", "primary", "secondary".</param>
        public static void MouseDown(string button) {
            if(X64) {
                NativeMethods.AU3_MouseDown(button);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseDown(button);
            }
        }

        /// <summary>
        /// Retrieves the current position of the mouse cursor.
        /// </summary>
        /// <returns>PointXY struct with x, y values.</returns>
        public static PointXY MouseGetPos() {
            PointXY point;
            if(X64) {
                point.X = NativeMethods.AU3_MouseGetPosX();
                point.Y = NativeMethods.AU3_MouseGetPosY();
            } else {
                point.X = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseGetPosX();
                point.Y = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseGetPosY();
            }

            return point;
        }

        /// <summary>
        /// Moves the mouse pointer.
        /// </summary>
        /// <param name="xcoord">The screen x coordinate to move the mouse to.</param>
        /// <param name="ycoord">The screen y coordinate to move the mouse to.</param>
        public static void MouseMove(int xcoord, int ycoord) {
            MouseMove(xcoord, ycoord, 1);
        }

        /// <summary>
        /// Moves the mouse pointer.
        /// </summary>
        /// <param name="xcoord">The screen x coordinate to move the mouse to.</param>
        /// <param name="ycoord">The screen y coordinate to move the mouse to.</param>
        /// <param name="speed">[optional] the speed to move the mouse in the range 1 (fastest) to 100 (slowest). A speed of 0 will move the mouse instantly. Default speed is 10.</param>
        public static void MouseMove(int xcoord, int ycoord, int speed) {
            if(X64) {
                NativeMethods.AU3_MouseMove(xcoord, ycoord, speed);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseMove(xcoord, ycoord, speed);
            }
        }

        /// <summary>
        /// Perform a mouse up event at the current mouse position.
        /// </summary>
        /// <param name="button">The button to click: "left", "right", "middle", "main", "menu", "primary", "secondary".</param>
        public static void MouseUp(string button) {
            if(X64) {
                NativeMethods.AU3_MouseUp(button);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_MouseUp(button);
            }
        }

        /// <summary>
        /// Generates a checksum for a region of pixels.
        /// </summary>
        /// <param name="left">left coordinate of rectangle.</param>
        /// <param name="top">top coordinate of rectangle.</param>
        /// <param name="right">right coordinate of rectangle.</param>
        /// <param name="bottom">bottom coordinate of rectangle.</param>
        /// <param name="step">[optional] Instead of checksumming each pixel use a value larger than 1 to skip pixels (for speed). E.g. A value of 2 will only check every other pixel. Default is 1. It is not recommended to use a step value greater than 1.</param>
        /// <returns>Returns the checksum value of the region.</returns>
        public static int PixelChecksum(int left, int top, int right, int bottom, int step) {
            // object sum = AU3_PixelChecksum(left, top, right, bottom, step);
            // return Convert.ToUInt32(sum);
            int sum = 0;
            if(X64) {
                for(int xcoord = left; xcoord <= right; xcoord += step) {
                    for(int ycoord = top; ycoord <= bottom; ycoord += step) {
                        sum += NativeMethods.AU3_PixelGetColor(xcoord, ycoord);
                    }
                }
            }
            else {
                for(int xcoord = left; xcoord <= right; xcoord += step) {
                    for(int ycoord = top; ycoord <= bottom; ycoord += step) {
                        sum += global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_PixelGetColor(xcoord, ycoord);
                    }
                }
            }

            return sum;
        }

        /// <summary>
        /// Generates a checksum for a region of pixels.
        /// </summary>
        /// <param name="left">left coordinate of rectangle.</param>
        /// <param name="top">top coordinate of rectangle.</param>
        /// <param name="right">right coordinate of rectangle.</param>
        /// <param name="bottom">bottom coordinate of rectangle.</param>
        /// <returns>Returns the checksum value of the region.</returns>
        public static int PixelChecksum(int left, int top, int right, int bottom) {
            return PixelChecksum(left, top, right, bottom, 10);
        }

        /// <summary>
        /// Returns a pixel color according to x,y pixel coordinates.
        /// </summary>
        /// <param name="xcoord">x coordinate of pixel.</param>
        /// <param name="ycoord">y coordinate of pixel.</param>
        /// <returns>The Pixel Color</returns>
        public static int PixelGetColor(int xcoord, int ycoord) {
            return X64 ? NativeMethods.AU3_PixelGetColor(xcoord, ycoord) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_PixelGetColor(xcoord, ycoord);
        }

        /// <summary>
        /// Returns a pixel color according to x,y pixel coordinates.
        /// </summary>
        /// <param name="xcoord">x coordinate of pixel.</param>
        /// <param name="ycoord">y coordinate of pixel.</param>
        /// <param name="handle">No Idea?</param>
        /// <returns>The Pixel Color</returns>
        public static int PixelGetColor(int xcoord, int ycoord, IntPtr handle) {
            if(X64) {
                NativeMethods.AU3_AutoItSetOption("PixelCoordMode", 2);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_AutoItSetOption("PixelCoordMode", 2);
            }

            return 0;
        }

#if IMPLEMENTED
    /// <summary>
    /// Searches a rectangle of pixels for the pixel color provided. ( NOT IMPLIMENTED )
    /// </summary>
    /// <param name="left">left coordinate of rectangle.</param>
    /// <param name="top">top coordinate of rectangle.</param>
    /// <param name="right">right coordinate of rectangle.</param>
    /// <param name="bottom">bottom coordinate of rectangle.</param>
    /// <param name="color">Color value of pixel to find (in decimal or hex).</param>
    /// <param name="shade">[optional] A number between 0 and 255 to indicate the allowed number of shades of variation of the red, green, and blue components of the colour. Default is 0 (exact match).</param>
    /// <param name="step">[optional] Instead of searching each pixel use a value larger than 1 to skip pixels (for speed). E.g. A value of 2 will only check every other pixel. Default is 1.</param>
    /// <returns>
    /// an array [0]=x [1]=y if the pixel is found. returns null otherwise
    /// </returns>
        public static int[] PixelSearch(int left, int top, int right, int bottom, int color, int shade, int step) {
            object coord = AU3_PixelSearch(left, top, right, bottom, color, shade, step);

            //Here we check to see if it found the pixel or not. It always returns a 1 in C# if it did not.
            if (coord.ToString() != "1")
            {
                //We have to turn "object coord" into a useable array since it contains the coordinates we need.
                object[] pixelCoord = (object[])coord;

                //Now we cast the object array to integers so that we can use the data inside.
                return new int[] { (int)pixelCoord[0], (int)pixelCoord[1] };
            }
            return null;
        }
#endif

        /// <summary>
        /// Terminates a named process.
        /// </summary>
        /// <param name="process">The title or PID of the process to terminate.</param>
        public static void ProcessClose(string process) {
            if(X64) {
                NativeMethods.AU3_ProcessClose(process);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ProcessClose(process);
            }
        }

        /// <summary>
        /// Checks to see if a specified process exists.
        /// </summary>
        /// <param name="process">The name or PID of the process to check. </param>
        /// <returns>true if it exist, false otherwise</returns>
        public static bool ProcessExists(string process) {
            return X64 ? NativeMethods.AU3_ProcessExists(process) != 0 : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ProcessExists(process) != 0;
        }

        /// <summary>
        /// Pauses script execution until a given process exists.
        /// </summary>
        /// <param name="process">The name of the process to check.</param>
        /// <param name="timeout">[optional] Specifies how long to wait (in seconds). Default is to wait indefinitely.</param>
        public static void ProcessWait(string process, int timeout) {
            if(X64) {
                NativeMethods.AU3_ProcessWait(process, timeout);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ProcessWait(process, timeout);
            }
        }

        /// <summary>
        /// Pauses script execution until a given process does not exist.
        /// </summary>
        /// <param name="process">The name or PID of the process to check.</param>
        /// <param name="timeout">[optional] Specifies how long to wait (in seconds). Default is to wait indefinitely.</param>
        public static void ProcessWaitClose(string process, int timeout) {
            if(X64) {
                NativeMethods.AU3_ProcessWaitClose(process, timeout);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ProcessWaitClose(process, timeout);
            }
        }

        /// <summary>
        /// Runs an external program.
        /// </summary>
        /// <param name="process">The name of the executable (EXE, BAT, COM, or PIF) to run.</param>
        public static void Run(string process) {
            Run(process, string.Empty);
        }

        /// <summary>
        /// Runs an external program.
        /// </summary>
        /// <param name="process">The name of the executable (EXE, BAT, COM, or PIF) to run.</param>
        /// <param name="dir">[optional] The working directory.</param>
        public static void Run(string process, string dir) {
            Run(process, dir, Visibility.showMaximized);
        }

        /// <summary>
        /// Runs an external program.
        /// </summary>
        /// <param name="process">The name of the executable (EXE, BAT, COM, or PIF) to run.</param>
        /// <param name="dir">[optional] The working directory.</param>
        /// <param name="showflag">
        ///   [optional] The "show" flag of the executed program:
        ///   @SW_HIDE = Hidden window (or Default keyword)
        ///   @SW_MINIMIZE = Minimized window
        ///   @SW_MAXIMIZE = Maximized window
        /// </param>
        public static void Run(string process, string dir, Visibility showflag) {
            if(X64) {
                NativeMethods.AU3_Run(process, dir, showflag);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_Run(process, dir, showflag);
            }
        }

        /// <summary>
        /// Sends simulated keystrokes to the active window.
        /// </summary>
        /// <param name="text">The sequence of keys to send.</param>
        /// <defaults>KeyDownDelay and KeySendDelay are default 5 ms.</defaults>
        public static void Send(string text) {
            Send(text, 5, 5);
        }

        /// <summary>
        /// Sends simulated keystrokes to the active window.
        /// </summary>
        /// <param name="text">The sequence of keys to send.</param>
        /// <param name="speed">
        /// Alters the the length of the brief pause in between sent keystrokes.
        ///   Time in milliseconds to pause (default=5). Sometimes a value of 0 does
        ///   not work; use 1 instead.
        /// </param>
        public static void Send(string text, int speed) {
            Send(text, speed, 5);
        }

        /// <summary>
        /// Sends simulated keystrokes to the active window.
        /// </summary>
        /// <param name="text">The sequence of keys to send.</param>
        /// <param name="speed">
        /// Alters the the length of the brief pause in between sent keystrokes.
        ///   Time in milliseconds to pause (default=5). Sometimes a value of 0 does
        ///   not work; use 1 instead.
        /// </param>
        /// <param name="downLen">
        /// Alters the length of time a key is held down before being released during a keystroke.
        ///   For applications that take a while to register keypresses (and many games) you may need
        ///   to raise this value from the default.
        ///   Time in milliseconds to pause (default=5).
        /// </param>
        public static void Send(string text, int speed, int downLen) {
            SetOptions("AuXWrapper.SendKeyDelay", speed);
            SetOptions("AuXWrapper.SendKeyDownDelay", downLen);
            Send(0, text);
        }

        /// <summary>
        /// Sends simulated keystrokes to the active window.
        /// </summary>
        /// <param name="mode">[optional] Changes how "keys" is processed:
        ///   flag = 0 (default), Text contains special characters like + and ! to indicate SHIFT and ALT key-presses.
        ///   flag = 1, keys are sent raw.
        /// </param>
        /// <param name="text">The sequence of keys to send.</param>
        public static void Send(int mode, string text) {
            if(X64) {
                NativeMethods.AU3_Send(text, mode);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_Send(text, mode);
            }
        }

        /// <summary>
        /// Sends simulated keystrokes to the specified control
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// /// <param name="title">The window title.</param>
        /// <param name="control">The control in the window.</param>
        public static void Send(string text, string title, string control) {
            //----------------------------------------------------------------------
            // Set control focus and then send text to that control
            //----------------------------------------------------------------------
            ControlFocus(title, control);
            Send(text);
        }

        /// <summary>
        /// No Idea?
        /// </summary>
        /// <param name="text">No Idea</param>
        /// <param name="speedMin">Who Knows</param>
        /// <param name="speedMax">Not I</param>
        public static void SendRand(string text, int speedMin, int speedMax) {
            var rand = new Random();
            if(X64) {
                for(int a = 0; a < text.Length; a++) {
                    SetOptions("AuXWrapper.SendKeyDownDelay", rand.Next(10, 25));
                    Console.WriteLine(Convert.ToString(text[a]));
                    NativeMethods.AU3_Send(text[a].ToString(), 0);
                    /* Bing_Bot.Macro.delay(rand.Next(speedMin, speedMax)); */
                }
            }
            else {
                for(int a = 0; a < text.Length; a++) {
                    SetOptions("AuXWrapper.SendKeyDownDelay", rand.Next(10, 25));
                    Console.WriteLine(Convert.ToString(text[a]));
                    global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_Send(text[a].ToString(), 0);
                    /* Bing_Bot.Macro.delay(rand.Next(speedMin, speedMax)); */
                }
            }
        }

        /// <summary>
        /// Set recommended default AutoIt functions/parameters
        /// </summary>
        public static void SetOptions() {
            SetOptions(true, 250);
        }

        /// <summary>
        /// Changes the operation of various AutoIt functions/parameters.
        /// </summary>
        /// <param name="option">The option to change. See Remarks.</param>
        /// <param name="value">
        /// [optional] The value to assign to the option. The type and meaning
        ///   vary by option. The keyword Default can be used for the parameter
        ///   to reset the option to its default value.
        /// </param>
        public static void SetOptions(string option, int value) {
            if(X64) {
                NativeMethods.AU3_AutoItSetOption(option, value);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_AutoItSetOption(option, value);
            }
        }

        /// <summary>
        /// Set recommended default AutoIt functions/parameters
        /// </summary>
        /// <param name="windowTitleExactMatch" >if True, matches only exact title</param>
        /// <param name="sendKeyDelay" >Delay between sending keys</param>
        public static void SetOptions(bool windowTitleExactMatch, int sendKeyDelay) {
            //----------------------------------------------------------------------
            // WinTitleMatchMode
            // Alters the method that is used to match window titles during search operations.
            // 1 = Match the title from the start (default)
            // 2 = Match any substring in the title
            // 3 = Exact title match
            // 4 = Advanced mode, see Window Titles & Text (Advanced)
            // -1 to -4 = force lower case match according to other type of match.
            //----------------------------------------------------------------------
            SetOptions("WinTitleMatchMode", windowTitleExactMatch ? 1 : 2);

            //----------------------------------------------------------------------
            // SendKeyDelay
            // Alters the the length of the brief pause in between sent keystrokes.
            // Time in milliseconds to pause (default=5). Sometimes a value of 0
            //   does not work; use 1 instead.
            //----------------------------------------------------------------------
            SetOptions("SendKeyDelay", sendKeyDelay);

            //----------------------------------------------------------------------
            // WinWaitDelay
            // Alters how long a script should briefly pause after a successful
            //   window-related operation. Time in milliseconds to pause (default=250).
            //----------------------------------------------------------------------
            SetOptions("WinWaitDelay", 250);

            //----------------------------------------------------------------------
            // WinDetectHiddenText
            // Specifies if hidden window text can be "seen" by the window matching functions.
            // 0 = Do not detect hidden text (default)
            // 1 = Detect hidden text
            //----------------------------------------------------------------------
            SetOptions("WinDetectHiddenText", 1);

            //----------------------------------------------------------------------
            // CaretCoordMode
            // Sets the way coords are used in the caret functions, either absolute
            //  coords or coords relative to the current active window:
            // 0 = relative coords to the active window
            // 1 = absolute screen coordinates (default)
            // 2 = relative coords to the client area of the active window
            //----------------------------------------------------------------------
            SetOptions("CaretCoordMode", 2);

            //----------------------------------------------------------------------
            // PixelCoordMode
            // Sets the way coords are used in the pixel functions, either absolute
            // coords or coords relative to the window defined by hwnd (default active window):
            // 0 = relative coords to the defined window
            // 1 = absolute screen coordinates (default)
            // 2 = relative coords to the client area of the defined window
            //----------------------------------------------------------------------
            SetOptions("PixelCoordMode", 2);

            //----------------------------------------------------------------------
            // MouseCoordMode
            // Sets the way coords are used in the mouse functions, either absolute
            //   coords or coords relative to the current active window:
            // 0 = relative coords to the active window
            // 1 = absolute screen coordinates (default)
            // 2 = relative coords to the client area of the active window
            //----------------------------------------------------------------------
            SetOptions("MouseCoordMode", 2);
        }

        /// <summary>
        /// Pause script execution.
        /// </summary>
        /// <param name="milliseconds">Amount of time to pause (in milliseconds).</param>
        public static void Sleep(int milliseconds) {
            if(X64) {
                NativeMethods.AU3_Sleep(milliseconds);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_Sleep(milliseconds);
            }           
        }

        /// <summary>
        /// Creates a tooltip anywhere on the screen.
        /// </summary>
        /// <param name="tip">The text of the tooltip. (An empty string clears a displaying tooltip)</param>
        /// <param name="xcoord">[optional] The x position of the tooltip.</param>
        /// <param name="ycoord">[optional] The y position of the tooltip.</param>
        public static void ToolTip(string tip, int xcoord, int ycoord) {
            if(X64) {
                NativeMethods.AU3_ToolTip(tip, xcoord, ycoord);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_ToolTip(tip, xcoord, ycoord);
            }
        }

        /// <summary>
        /// Creates a padded tooltip in the top-left part of the screen.
        /// </summary>
        /// <param name="message">The text of the tooltip. (An empty string clears a displaying tooltip)</param>
        public static void ToolTip(string message) {
            //----------------------------------------------------------------------
            // Pad the message being displayed
            //----------------------------------------------------------------------
            if(!string.IsNullOrEmpty(message)) {
                message = string.Format("\r\n      {0}      \r\n  ", message);
            }

            //----------------------------------------------------------------------
            // Set the tooltip to display the message
            //----------------------------------------------------------------------
            ToolTip(message, 0, 0);
        }

        /// <summary>
        /// Activate a window
        /// </summary>
        /// <param name="title">the complete title of the window to activate, case sensitive</param>
        public static void WinActivate(string title) {
            if(X64) {
                NativeMethods.AU3_WinActivate(title, string.Empty);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinActivate(title, string.Empty);
            }
        }

        /// <summary>
        /// Activates (gives focus to) a window. ( incorporates WinWaitActive )
        /// </summary>
        /// <param name="title">The title of the window to activate.</param>
        /// <param name="waitactivetimeout">[optional] Timeout in seconds</param>
        public static void WinActivate(string title, int waitactivetimeout) {
            if(X64) {
                NativeMethods.AU3_WinActivate(title, string.Empty);
                NativeMethods.AU3_WinWaitActive(title, string.Empty, waitactivetimeout);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinActivate(title, string.Empty);
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinWaitActive(title, string.Empty, waitactivetimeout);
            }

            System.Threading.Thread.Sleep(1000);
        }

        /// <summary>
        /// Activates (gives focus to) a window.
        /// </summary>
        /// <param name="title">The title of the window to activate.</param>
        /// <param name="text">[optional] The text of the window to activate.</param>
        public static void WinActivate(string title, string text) {
            if(X64) {
                NativeMethods.AU3_WinActivate(title, text);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinActivate(title, text);
            }
        }

        /// <summary>
        /// Checks to see if a specified window exists.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        /// <returns>
        /// Success: Returns 1 if the window exists.
        /// Failure: Returns 0 otherwise.
        /// </returns>
        public static int WinExists(string title) {
            return X64 ? NativeMethods.AU3_WinExists(title, string.Empty) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinExists(title, string.Empty);
        }

        /// <summary>
        /// Checks to see if a specified window exists.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        /// <param name="text">[optional] The text of the window to check.</param>
        /// <returns>
        /// Success: Returns 1 if the window exists.
        /// Failure: Returns 0 otherwise.
        /// </returns>
        public static int WinExists(string title, string text) {
            return X64 ? NativeMethods.AU3_WinExists(title, text) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinExists(title, text);
        }

        /// <summary>
        /// Retrieves the internal handle of a window.
        /// </summary>
        /// <param name="title">The title of the window to read.</param>
        /// <returns>the internal handle</returns>
        public static string WinGetHandle(string title) {
            //----------------------------------------------------------------------
            // A number big enought to hold result, trailing bytes will be 0
            //----------------------------------------------------------------------
            var retText = new byte[50];
            if(X64) {
                NativeMethods.AU3_WinGetHandle(title, string.Empty, retText, retText.Length);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetHandle(title, string.Empty, retText, retText.Length);
            }

            //----------------------------------------------------------------------
            // May need to convert back to a string or int
            //----------------------------------------------------------------------
            return Encoding.Unicode.GetString(retText).TrimEnd('\0');
        }

        /// <summary>
        /// Retrieves the text from a window.
        /// </summary>
        /// <param name="title">The title of the window to read.</param>
        /// <param name="text">[optional] The text of the window to read.</param>
        /// <returns>the text from the window</returns>
        public static string WinGetText(string title, string text) {
            //----------------------------------------------------------------------
            // A number big enought to hold result, trailing bytes will be 0
            //----------------------------------------------------------------------
            var byteRetText = new byte[10000];

            if(X64) {
                NativeMethods.AU3_WinGetText(title, text, byteRetText, byteRetText.Length);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetText(title, text, byteRetText, byteRetText.Length);
            }

            //----------------------------------------------------------------------
            // May need to convert back to a string or int
            //----------------------------------------------------------------------
            return Encoding.Unicode.GetString(byteRetText).TrimEnd('\0');
        }

        /// <summary>
        /// Retrieves the text from a window.
        /// </summary>
        /// <param name="title">The title of the window to read.</param>
        /// <returns>the text from a window</returns>
        public static string WinGetText(string title) {
            return WinGetText(title, string.Empty);
        }

        /// <summary>
        /// Gets the Position of a window
        /// </summary>
        /// <param name="title">The Title of the window</param>
        /// <param name="text">The text of the window in the Window?</param>
        /// <returns>The Position</returns>
        public static Position2D WinGetPos(string title, string text) {
            Position2D pos;
            if(X64) {
                pos.Xpos = NativeMethods.AU3_WinGetPosX(title, text);
                pos.Ypos = NativeMethods.AU3_WinGetPosY(title, text);
                pos.Height = NativeMethods.AU3_WinGetPosHeight(title, text);
                pos.Width = NativeMethods.AU3_WinGetPosWidth(title, text);
            } else {
                pos.Xpos = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetPosX(title, text);
                pos.Ypos = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetPosY(title, text);
                pos.Height = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetPosHeight(title, text);
                pos.Width = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetPosWidth(title, text);
            }

            return pos;
        }

        /// <summary>
        /// Moves and/or resizes a window.
        /// </summary>
        /// <param name="title">The title of the window to move/resize.</param>
        /// <param name="xcoord">xpos coordinate to move to.</param>
        /// <param name="ycoord">ypos coordinate to move to.</param>
        public static void WinMove(string title, int xcoord, int ycoord) {
            int width;
            int height;
            if(X64) {
                width = NativeMethods.AU3_WinGetPosWidth(title, string.Empty);
                height = NativeMethods.AU3_WinGetPosHeight(title, string.Empty);
            } else {
                width = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetPosWidth(title, string.Empty);
                height = global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinGetPosHeight(title, string.Empty);
            }

            WinMove(title, xcoord, ycoord, width, height);
        }

        /// <summary>
        /// Moves and/or resizes a window.
        /// </summary>
        /// <param name="title">The title of the window to move/resize.</param>
        /// <param name="xcoord">X coordinate to move to.</param>
        /// <param name="ycoord">ypos coordinate to move to.</param>
        /// <param name="width">[optional] New width of the window.</param>
        /// <param name="height">[optional] New height of the window.</param>
        public static void WinMove(string title, int xcoord, int ycoord, int width, int height) {
            if(X64) {
                NativeMethods.AU3_WinMove(title, string.Empty, xcoord, ycoord, width, height);
            }
            else {
                global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinMove(title, string.Empty, xcoord, ycoord, width, height);
            }
        }

        /// <summary>
        /// Pauses execution of the script until the requested window exists,
        ///   activates it and then waits again until it is active.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        public static void WinWaitActiveWindow(string title) {
            //----------------------------------------------------------------------
            // Wait for the window
            //----------------------------------------------------------------------
            WinWait(title);

            //----------------------------------------------------------------------
            // Set the window as the active window
            //----------------------------------------------------------------------
            WinActivate(title);

            //----------------------------------------------------------------------
            // Wait until the window is active, then proceed
            //----------------------------------------------------------------------
            WinWaitActive(title);
        }

        /// <summary>
        /// Pauses execution of the script until the requested window exists.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        /// <returns>Good Question. Some status value?</returns>
        public static int WinWait(string title) {
            return X64 ? NativeMethods.AU3_WinWait(title, string.Empty, 3000) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinWait(title, string.Empty, 3000);
        }

        /// <summary>
        /// Pauses execution of the script until the requested window is active.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        /// <param name="text">[optional] The text of the window to check.</param>
        /// <param name="timeout">[optional] Timeout in seconds</param>
        /// <returns>
        /// Success: Returns 1.
        /// Failure: Returns 0 if timeout occurred.
        /// </returns>
        public static int WinWaitActive(string title, string text, int timeout) {
            return X64 ? NativeMethods.AU3_WinWaitActive(title, text, timeout) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinWaitActive(title, text, timeout);
        }

        /// <summary>
        /// Pauses execution of the script until the requested window is active.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        /// <param name="timeout">[optional] Timeout in seconds</param>
        /// <returns>
        /// Success: Returns 1.
        /// Failure: Returns 0 if timeout occurred.
        /// </returns>
        public static int WinWaitActive(string title, int timeout) {
            return X64 ? NativeMethods.AU3_WinWaitActive(title, string.Empty, timeout) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinWaitActive(title, string.Empty, timeout);
        }

        /// <summary>
        /// Pauses execution of the script until the requested window is active.
        /// </summary>
        /// <param name="title">The title of the window to check.</param>
        /// <returns>
        /// Success: Returns 1.
        /// Failure: Returns 0 if timeout occurred.
        /// </returns>
        public static int WinWaitActive(string title) {
            return X64 ? NativeMethods.AU3_WinWaitActive(title, string.Empty, 3000) : global::CoApp.Toolkit.AutoItX3.x86.NativeMethods.AU3_WinWaitActive(title, string.Empty, 3000);
        }

        #endregion

        #region | DATA Structs |

        /// <summary>
        /// These are used in a few functions to return multiple values(GetWinPos, MouseGetPos)
        /// </summary>
        public struct Position2D {
            /// <summary>
            /// X position
            /// </summary>
            public int Xpos;

            /// <summary>
            /// Y Position
            /// </summary>
            public int Ypos;

            /// <summary>
            /// Width
            /// </summary>
            public int Width;

            /// <summary>
            /// Height
            /// </summary>
            public int Height;
        }

        /// <summary>
        /// Some Summary
        /// </summary>
        public struct PointXY {
            /// <summary>
            ///  X Coordinate
            /// </summary>
            public int X;

            /// <summary>
            /// Y Coordiante
            /// </summary>
            public int Y;
        }

        #endregion //end Data Structs

        /// <summary>
        /// some summary
        /// </summary>
        public enum Visibility {
            hide = 2,
            maximize = 3,
            minimize = 4,
            restore = 5,
            show = 6,
            showDefault = 7,
            showMaximized = 8,
            showMinimized = 9,
            showMinNoActive = 10,
            showNA = 11,
            showNoActivate = 12,
            showNormal = 13
        }
    }
}
