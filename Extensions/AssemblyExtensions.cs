//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
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
    using System.IO;
    using System.Reflection;
    using Win32;

    public static class AssemblyExtensions {
        private static string logo;

        // warning: case sensitive. 
        public static string ExtractFileResourceToTemp(this Assembly assembly, string name) {
            var tempPath = Path.Combine(Path.GetTempPath(), name);
            var s = assembly.GetManifestResourceStream(name);
            if(s == null) {
                // not specified exactly
                var n = assembly.GetManifestResourceNames();
                foreach(var each in n) {
                    if( each.EndsWith("."+name)) {
                        name = each;
                        break;
                    }
                }
            }
            return ExtractFileResourceToPath(assembly, name, tempPath);
        }

        public static string ExtractFileResourceToPath(this Assembly assembly, string name, string filePath) {
            var s = assembly.GetManifestResourceStream(name);
            var buf = new byte[s.Length];

            var targetFile = new FileStream(filePath, FileMode.Create);
            var sz = s.Read(buf, 0, buf.Length);
            targetFile.Write(buf, 0, sz);
            s.Close();
            targetFile.Close();
            return filePath;
        }

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

#if !COAPP_ENGINE_CORE
        public static string Copyright(this Assembly assembly) {
            try {
                return PEInfo.Scan(assembly.Location).VersionInfo.LegalCopyright;
            }
            catch {
            }
            return "";
        }

        public static string Company(this Assembly assembly) {
            try {
                return PEInfo.Scan(assembly.Location).VersionInfo.CompanyName;
            }
            catch {
            }
            return "";
        }

        public static string Version(this Assembly assembly) {
            try {
                var vi = PEInfo.Scan(assembly.Location).VersionInfo;

                return "{0}.{1}.{2}.{3}".format(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
            }
            catch {
            }
            return "";
        }

        public static string Comments(this Assembly assembly) {
            try {
                return PEInfo.Scan(assembly.Location).VersionInfo.Comments;
            }
            catch {
            }
            return "";
        }

        public static string Logo(this Assembly assembly) {
            if(logo == null) {
                var assemblycomments = assembly.Comments();
                assemblycomments = string.IsNullOrEmpty(assemblycomments) ? string.Empty : "\r\n" + assemblycomments;

                logo =
                    @"{0} {1} Version {2} for {3}
{4}. All rights reserved{5}
-------------------------------------------------------------------------------".format(assembly.Company(), assembly.Title(), assembly.Version(), IntPtr.Size == 8? "x64":"x86", assembly.Copyright().Replace("©", "(c)"), assemblycomments);
            }
            return logo;
        }

        public static void SetLogo(this Assembly assembly, string logoText) {
            logo = logoText;
        }
#endif
    }
}