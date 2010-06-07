//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    #region Enums

    public enum ScriptLanguage {
        JScript,
        VBScript
    } ;

    internal enum ScriptState : uint {
        Uninitialized = 0,
        Started = 1,
        Connected = 2,
        Disconnected = 3,
        Closed = 4,
        Initialized = 5,
    }

    internal enum ScriptThreadState : uint {
        NotInScript = 0,
        Running = 1,
    }

    [Flags]
    internal enum ScriptText : uint {
        None = 0x0000,

        DelayExecution = 0x0001,
        IsVisible = 0x0002,
        IsExpression = 0x0020,
        IsPersistent = 0x0040,
        HostManageSource = 0x0080,
    }

    [Flags]
    internal enum ScriptItem : uint {
        None = 0x0000,

        IsVisible = 0x0002,
        IsSource = 0x0004,
        GlobalMembers = 0x0008,
        IsPersistent = 0x0040,
        CodeOnly = 0x0200,
        NoCode = 0x0400,
    }

    [Flags]
    internal enum ScriptInfo : uint {
        None = 0x0000,
        // ReSharper disable InconsistentNaming
        IUnknown = 0x0001,
        ITypeInfo = 0x0002,
        // ReSharper restore InconsistentNaming
    }

    #endregion

    #region ActiveScripting

    [ComImport, Guid("B54F3741-5B07-11CF-A4B0-00AA004A55E8")]
    public class VBScript {
    }
    [ComImport, Guid("F414C260-6AC0-11CF-B6D1-00AA00BBBB58")]
    public class JScript {
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00020400-0000-0000-C000-000000000046")]
    public interface IDispatch {
        int GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.Interface)]
        ITypeInfo GetTypeInfo([In, MarshalAs(UnmanagedType.U4)] int iTInfo, [In, MarshalAs(UnmanagedType.U4)] int lcid);

        [PreserveSig]
        int GetIDsOfNames([In] ref Guid riid, [In, MarshalAs(UnmanagedType.LPArray)] string[] rgszNames, [In, MarshalAs(UnmanagedType.U4)] int cNames, [In, MarshalAs(UnmanagedType.U4)] int lcid, [Out, MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

        [PreserveSig]
        int Invoke(int dispIdMember, ref Guid riid, int lcid, ushort wFlags, out DISPPARAMS pDispParams, out object varResult, out EXCEPINFO pExcepInfo, out int puArgErr);
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("EAE1BA61-A4ED-11CF-8F20-00805F2CD064")]
    public interface IActiveScriptError {
        void GetExceptionInfo(out EXCEPINFO excepinfo);
        void GetSourcePosition(out int sourceContext, out int pulLineNumber, out int plCharacterPosition);
        void GetSourceLineText(out string sourceLine);
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BB1A2AE1-A4F9-11cf-8F20-00805F2CD064")]
    internal interface IActiveScript {
        void SetScriptSite([In, MarshalAs(UnmanagedType.Interface)] IActiveScriptSite site);
        void GetScriptSite(ref Guid riid, out IntPtr ppvObject);
        void SetScriptState(uint ss);
        void GetScriptState(out uint ss);
        void Close();
        void AddNamedItem([In, MarshalAs(UnmanagedType.BStr)] string pstrName, [In, MarshalAs(UnmanagedType.U4)] ScriptItem dwFlags);
        void AddTypeLib(ref Guid rguidTypeLib, uint dwMajor, uint dwMinor, uint dwFlags);
        void GetScriptDispatch(string pstrItemName, [Out, MarshalAs(UnmanagedType.IDispatch)] out object ppdisp);
        void GetCurrentScriptThreadiD(out uint id);
        void GetScriptThreadID(uint threadid, out uint id);
        void GetScriptThreadState(uint id, out uint state);
        void InterruptScriptThread(uint id, ref EXCEPINFO info, uint flags);
        void Clone(out IActiveScript item);
    } ;
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BB1A2AE2-A4F9-11cf-8F20-00805F2CD064")]
    internal interface IActiveScriptParse {
        void InitNew();
        void AddScriptlet(string defaultName, string code, string itemName, string subItemName, string eventName, string delimiter, uint sourceContextCookie, uint startingLineNumber, uint flags, out string name, out EXCEPINFO info);
        void ParseScriptText(string code, string itemName, IntPtr context, string delimiter, uint sourceContextCookie, uint startingLineNumber, uint flags, IntPtr result, out EXCEPINFO info);
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("71ee5b20-fb04-11d1-b3a8-00a0c911e8b2")]
    internal interface IActiveScriptParseProcedure {
        void ParseProcedureText(string code, string formalParams, string procedureName, string itemName, IntPtr context, string delimiter, int sourceContextCookie, uint startingLineNumber, uint flags, [Out, MarshalAs(UnmanagedType.IDispatch)] out object ppdisp);
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("DB01A1E3-A42B-11cf-8F20-00805F2CD064")]
    internal interface IActiveScriptSite {
        void GetLCID(out uint id);
        void GetItemInfo(string pstrName, uint dwReturnMask, [Out, MarshalAs(UnmanagedType.IUnknown)] out object item, IntPtr ppti);
        void GetDocVersionString(out string v);
        void OnScriptTerminate(ref object result, ref EXCEPINFO info);
        void OnStateChange(uint state);
        void OnScriptError(IActiveScriptError err);
        void OnEnterScript();
        void OnLeaveScript();
    }

    #endregion

    [ComVisible(true)]
    public class ActiveScriptHost : IActiveScriptSite, IDisposable {
        private object scriptEngine;
        private int returnValue;
        private readonly ScriptLanguage scriptLanguage;
        private readonly Dictionary<string, object> visibleGlobalMembers = new Dictionary<string, object>();

        private IActiveScript ActiveScript {
            get { return (scriptEngine as IActiveScript); }
        }
        private IActiveScriptParse ActiveScriptParse {
            get { return (scriptEngine as IActiveScriptParse); }
        }

        public ActiveScriptHost(ScriptLanguage language) {
            scriptLanguage = language;
        }

        public int ReturnValue {
            get { return returnValue; }
        }

        public void Dispose() {
            Close();
        }

        public void Close() {
            if(null != scriptEngine) {
                ActiveScript.SetScriptState((uint) ScriptState.Disconnected);
                ActiveScript.Close();
            }
            scriptEngine = null;
        }

        #region WScript style methods

        // ReSharper disable InconsistentNaming

        public void Quit(int i) {
            returnValue = i;
            ActiveScript.SetScriptState((uint) ScriptState.Disconnected);
        }

        public string ScriptFullName() {
            return "scriptfullname";
        }

        public string ScriptName() {
            return "scriptname";
        }

        public string FullName() {
            return "fullanme";
        }

        public void echo(string text) {
            Console.WriteLine(text);
        }

        // ReSharper restore InconsistentNaming

        #endregion

        public Dictionary<string, object> GlobalMembers {
            get { return visibleGlobalMembers; }
        }

        public string ScriptText { get; set; }

        public void GetItemInfo([In, MarshalAs(UnmanagedType.BStr)] string pstrName, [In, MarshalAs(UnmanagedType.U4)] uint dwReturnMask, [Out, MarshalAs(UnmanagedType.IUnknown)] out object item, IntPtr ppti) {
            if(GlobalMembers.ContainsKey(pstrName)) {
                item = GlobalMembers[pstrName];
            }
            else {
                item = null;
                return;
            }

            if(ppti != IntPtr.Zero) {
                Marshal.WriteIntPtr(ppti, Marshal.GetITypeInfoForType(item.GetType()));
            }
        }

        public void OnScriptError(IActiveScriptError err) {
            EXCEPINFO excepinfo;
            int ctx, line, col;
            err.GetSourcePosition(out ctx, out line, out col);
            err.GetExceptionInfo(out excepinfo);
            if(excepinfo.bstrSource.Equals("ScriptControl")) {
                return;
            }

            Console.WriteLine("Script Error ({0},{1}) {2}", line, col, excepinfo.bstrDescription);
        }

        public void Run() {
            try {
                switch(scriptLanguage) {
                    case ScriptLanguage.JScript:
                        scriptEngine = new JScript();
                        break;
                    case ScriptLanguage.VBScript:
                        scriptEngine = new VBScript();
                        break;
                    default:
                        throw new Exception("Invalid Script Language");
                }
                EXCEPINFO info;
                ActiveScriptParse.InitNew();
                ActiveScript.SetScriptSite(this);

                // add this object in 
                GlobalMembers.Add("WScript", this);

                foreach(string key in GlobalMembers.Keys)
                    ActiveScript.AddNamedItem(key, ScriptItem.IsVisible | ScriptItem.GlobalMembers);

                ActiveScriptParse.ParseScriptText(ScriptText, null, IntPtr.Zero, null, 0, 0, 0, IntPtr.Zero, out info);
                ActiveScript.SetScriptState((uint) ScriptState.Connected);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void Invoke(string functionName, params object[] args) {
            object dispatch;
            ActiveScript.GetScriptDispatch(null, out dispatch);
            var t = dispatch.GetType();
            t.InvokeMember(functionName, BindingFlags.InvokeMethod, null, dispatch, args);
        }

        public void GetLCID(out uint id) {
            id = 0x80004001;
        }

        public void OnEnterScript() {
        }

        public void OnLeaveScript() {
        }

        public void OnScriptTerminate(ref object result, ref EXCEPINFO info) {
        }

        public void OnStateChange(uint state) {
        }

        public void GetDocVersionString(out string v) {
            v = "ScriptHost Version 1";
        }
    }
}