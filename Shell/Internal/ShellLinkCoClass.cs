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

namespace CoApp.Toolkit.Shell.Internal {
    using System.Runtime.InteropServices;

    /// <summary>
    ///   This is the CoClass that impliments the shell link interfaces.
    /// </summary>
    [
        ComImport,
        Guid("00021401-0000-0000-C000-000000000046")
    ]
    internal class ShellLinkCoClass // : IPersistFile, IShellLink, IShellLinkDataList
    {
    }
}