using System;
using System.Runtime.InteropServices;

/// <summary>
/// This class is used by the DllExport utility to generate a C-style
/// native binding for any static methods in a .NET assembly.
///
/// Namespace is not important--feel free to set the namespace to anything
/// convenient for your project.
/// -----------------------------------------------------------------------
/// (c) 2009 Microsoft Corporation -- All rights reserved
/// This code is licensed under the MS-PL
/// http://www.opensource.org/licenses/ms-pl.html
/// Courtesy of the Open Source Techology Center: http://port25.technet.com
/// -----------------------------------------------------------------------
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DllExportAttribute : Attribute {
    public DllExportAttribute(string exportName)
        : this(CallingConvention.StdCall, exportName) {
    }

    public DllExportAttribute(CallingConvention convention, string name) {
        ExportedName = name;
        this.CallingConvention = convention;
    }

    public string ExportedName { get; set; }
    public CallingConvention CallingConvention { get; set; }
}