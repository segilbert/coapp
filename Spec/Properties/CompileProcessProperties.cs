//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec.Properties {
    using System.Collections.Generic;
    using mkSpec.Tool.Properties;

    public class CompileProcessProperties : ProcessProperties {
        [PropertySheet("suppress-warning")]
        public readonly List<int> DisableWarning = new List<int>();

        [PropertySheet("report-warning-once")]
        public readonly List<int> ReportWarningOnce = new List<int>();

        [PropertySheet("warning-as-level")]
        public readonly Dictionary<int, int> SetWarningToLevel = new Dictionary<int, int>();

        [PropertySheet("warning-as-error")]
        public readonly List<int> WarningAsError = new List<int>();

        [PropertySheet("header-search-path")]
        public readonly List<string> HeaderIncludePaths = new List<string>();
        
        [PropertySheet("define")]
        public readonly List<string> Defines = new List<string>();

        [PropertySheet("undefine")]
        public readonly List<string> UnDefines = new List<string>();
        
        [PropertySheet("assembly-search-path")]
        public readonly List<string> AssemblyIncludePaths = new List<string>();

        [PropertySheet("forced-assembly-using")]
        public readonly List<string> ForcedAssemblyUsingFiles = new List<string>();

        [PropertySheet("forced-include")]
        public readonly List<string> ForcedIncludeFiles = new List<string>();

        [PropertySheet("set-stack-size")]
        public int? StackSize { get; set; }

        [PropertySheet("compile-for-clr")]
        public bool? CompileForClr { get; set; }

        [PropertySheet("clr-pure")]
        public bool? ClrPure { get; set; }

        [PropertySheet("clr-safe")]
        public bool? ClrSafe { get; set; }

        [PropertySheet("clr-old-syntax")]
        public bool? ClrOldSyntax { get; set; }

        [PropertySheet("clr-no-assembly")]
        public bool? ClrNoAssembly { get; set; }

        [PropertySheet("clr-initial-app-domain")]
        public bool? ClrInitialAppDomain { get; set; }

        [PropertySheet("clr-no-standard-lib")]
        public bool? ClrNoStdLib { get; set; }

        [PropertySheet("create-xdc")]
        public bool? CreateXdc { get; set; }

        [PropertySheet("xdcfilename")]
        public string XdcFilename { get; set; }

        [PropertySheet("exception-handling-cpp-only")]
        public bool? ExceptionHandlingCppOnly { get; set; }

        [PropertySheet("exception-handling-cpp-no-extern-c")]
        public bool? ExceptionHandlingCppOnlyNoExternC { get; set; }

        [PropertySheet("exception-handling-all")]
        public bool? ExceptionHandlingAll { get; set; }

        [PropertySheet("floating-point-precise")]
        public bool? FloatingPointPrecise { get; set; }

        [PropertySheet("floating-point-fast")]
        public bool? FloatingPointFast { get; set; }

        [PropertySheet("floating-point-except")]
        public bool? FloatingPointExcept { get; set; }

        [PropertySheet("floating-point-strict")]
        public bool? FloatingPointStrict { get; set; }

        [PropertySheet("max-length-of-external-names")]
        public int? MaxLengthOfExternalNames { get; set; }

        [PropertySheet("enable-rtti")]
        public bool? EnableRtti { get; set; }

        [PropertySheet("enable-buffer-security-check")]
        public bool? EnableBufferSecurityCheck { get; set; }

        [PropertySheet("control-stack-probes")]
        public bool? ControlStackProbes { get; set; }

        [PropertySheet("stack-probe-size")]
        public int? StackProbeSize { get; set; }

        [PropertySheet("generate-intrinsic-functions")]
        public bool? GenerateIntrinsicFunctions { get; set; }

        [PropertySheet("omit-frame-pointer")]
        public bool? OmitFramePointer { get; set; }

        [PropertySheet("enable-runtime-check-data-loss")]
        public bool? EnableRuntimeCheckDataLoss { get; set; }
        
        [PropertySheet("enable-runtime-check-stack-frame")]
        public bool? EnableRuntimeCheckStackFrame { get; set; }

        [PropertySheet("enable-runtime-check-uninitialized")]
        public bool? EnableRuntimeCheckUninitialized { get; set; }

        [PropertySheet("warning-level")]
        public int? WarningLevel { get; set; }

        [PropertySheet("warnings-as-errors")]
        public bool? WarningsAsErrors { get; set; }

        [PropertySheet("enable-all-warnings")]
        public bool? EnableAllWarnings { get; set; }

        [PropertySheet("disable-all-warnings")]
        public bool? DisableAllWarnings { get; set; }

        [PropertySheet("use-auto-variables")]
        public bool? UseAutoVariables { get; set; }

        [PropertySheet("use-wchar-t")]
        public bool? UseWCharT { get; set; }

        [PropertySheet("use-for-scope")]
        public bool? UseforScope { get; set; }

        [PropertySheet("pack-structs")]
        public int? PackStructs { get; set; }

        [PropertySheet("big-obj")]
        public bool? BigObj { get; set; }

        [PropertySheet("read-only-string-pooling")]
        public bool? StringPooling { get; set; }

        [PropertySheet("enable-pexit-hook")]
        public bool? EnablePExitHook { get; set; }

        [PropertySheet("enable-penter-hook")]
        public bool? EnablePEnterHook { get; set; }

        [PropertySheet("enable-fibre-safe-tls")]
        public bool? EnableFiberSafeTls { get; set; }

        [PropertySheet("cdecl")]
        public bool? EnableCdecl { get; set; }

        [PropertySheet("fastcall")]
        public bool? EnableFastcall { get; set; }

        [PropertySheet("stdcall")]
        public bool? EnableStdcall { get; set; }

        [PropertySheet("char-type-is-unsigned")]
        public bool? CharTypeIsUnsigned { get; set; }

        [PropertySheet("create-msil-module")]
        public bool? CreateMSILModule { get; set; }

        [PropertySheet("enable-open-mp")]
        public bool? EnableOpenMP { get; set; }

        [PropertySheet("undefine-ms-specific-symbols")]
        public bool? UndefineMSSpecficSymbols { get; set; }

        [PropertySheet("ignore-include-path")]
        public bool? DontSearchIncludePath { get; set; }

        [PropertySheet("disable-ms-language-extensions")]
        public bool? DisableMSLanguageExtensions { get; set; }
    }
}