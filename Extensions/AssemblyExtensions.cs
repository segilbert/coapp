//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.Diagnostics;
    using System.Reflection;

    public static class AssemblyExtensions {
        private static string logo;

        public static Assembly Assembly(this object obj) {
            return obj.GetType().Assembly;
        }

        public static string Title(this Assembly assembly) {
            try {
                return ((AssemblyTitleAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute))).Title;
            }
            catch {
            }
            return "";
        }

        public static string Description(this Assembly assembly) {
            try {
                return ((AssemblyDescriptionAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description;
            }
            catch {
            }
            return "";
        }

        public static string Copyright(this Assembly assembly) {
            try {
                return FileVersionInfo.GetVersionInfo(assembly.Location).LegalCopyright;
            }
            catch {
            }
            return "";
        }

        public static string Company(this Assembly assembly) {
            try {
                return FileVersionInfo.GetVersionInfo(assembly.Location).CompanyName;
            }
            catch {
            }
            return "";
        }

        public static string Version(this Assembly assembly) {
            try {
                var vi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return "{0}.{1}.{2}".format(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart);
            }
            catch {
            }
            return "";
        }

        public static string Comments(this Assembly assembly) {
            try {
                return FileVersionInfo.GetVersionInfo(assembly.Location).Comments;
            }
            catch {
            }
            return "";
        }

        public static string Logo(this Assembly assembly) {
            if(logo == null) {
                logo =
                    @"{0} {1} Version {2} for {3}
{4}. All rights reserved
{5}
-------------------------------------------------------------------------------".format(assembly.Company(), assembly.Title(), assembly.Version(), IntPtr.Size == 8? "x64":"x86", assembly.Copyright().Replace("©", "(c)"), assembly.Comments());
            }
            return logo;
        }

        public static void SetLogo(this Assembly assembly, string logoText) {
            logo = logoText;
        }
    }
}