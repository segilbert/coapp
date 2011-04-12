//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Defines a delegate for Message handling
    /// </summary>
    public delegate void MessageEventHandler(object sender, ref Message msg, ref bool handled);


    /// <summary>
    /// Inherits from System.Windows.Form.NativeWindow. Provides an Event for Message handling
    /// </summary>
    public class NativeWindowWithEvent: NativeWindow, IDisposable {
        public event MessageEventHandler ProcessMessage;

        private static NativeWindowWithEvent _instance;
        private static readonly Object InstanceLock = new Object();

        protected override void WndProc(ref Message m) {
            bool handled=false;

            if (ProcessMessage!=null)
                ProcessMessage(this,ref m,ref handled);
                
            if (!handled) 
                base.WndProc(ref m);
        }

        protected NativeWindowWithEvent() {
            var parms=new CreateParams();
            CreateHandle(parms);
        }

        public void Dispose() {
            if (Handle!=IntPtr.Zero)
                DestroyHandle();
        }

        public static NativeWindowWithEvent Instance {
            get {
                lock(InstanceLock) {
                    return _instance ?? (_instance = new NativeWindowWithEvent());
                }
            }
        }
    }
}