//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    public class ConsoleColors : IDisposable {
        private readonly ConsoleColor fg;
        private readonly ConsoleColor bg;

        public ConsoleColors(ConsoleColor fg, ConsoleColor bg) {
            this.fg = Console.ForegroundColor;
            this.bg = Console.BackgroundColor;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public void Dispose() {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }
    }

    public static class ConsoleExtensions {
        private enum FileType { Unknown, Disk, Char, Pipe };
        private enum StdHandle { Stdin = -10, Stdout = -11, Stderr = -12 };
        [DllImport("kernel32.dll")]
        private static extern FileType GetFileType(IntPtr hdl);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(StdHandle std);

        public static bool OutputRedirected {
            get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdout)); }
        }
        public static bool InputRedirected {
            get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdin)); }
        }
        public static bool ErrorRedirected {
            get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stderr)); }
        }

        public static void PrintProgressBar(this string message, int percentage) {
            if (!OutputRedirected) {
                if (percentage > -1) {
                    var sz = Console.BufferWidth - (message.Length + 5);
                    var done = (percentage*sz)/100;
                    Console.Write("\r{0} [{1}] ", message, "".PadRight(done, '=').PadRight(sz,' '));
                }
                else {
                    Console.Write("\r".PadRight( Console.BufferWidth - 1, ' '));
                }
            }
        }

        private static string SafeGetProperty(this PropertyInfo propertyInfo, Object obj, string @default = "") {
            try {
                return propertyInfo.GetValue(obj, null).ToString();
            }
            catch {
                return @default;
            }
        }

        private static string SafeGetField(this FieldInfo fieldInfo, Object obj, string @default = "") {
            try {
                return fieldInfo.GetValue(obj).ToString();
            }
            catch {
                return @default;
            }
        }

        public static Dictionary<string, List<string>> ToTable<T>(this IEnumerable<T> rows, IEnumerable<string> propertyNames) {
            var result = new Dictionary<string, List<string>>();
            
            foreach( var prop in propertyNames ) {
                var column = new List<string>();
                result.Add(prop,column);
                var propertyInfo = typeof(T).GetProperty(prop);
                var fieldInfo = typeof(T).GetField(prop);

                column.AddRange(rows.Select(row => propertyInfo.SafeGetProperty(row,null) ?? fieldInfo.SafeGetField(row) ));
            }

            return result;
        }

        private static string TrimTo(this string s, int sz) {
            return s.Length < sz ? s : s.Substring(s.Length - sz);
        }

        public static void Dump(this Dictionary<string, List<string>> data, IEnumerable<string> columnTitles, int maxWidth = 0) {

            if (data[data.Keys.First()].Count == 0)
                return;

            var columnWidths = data.Keys.Select(key => (from f in data[key] select f.Length).Max()).ToList();

            var formatString = new StringBuilder( "|");
            var n = 0;
            foreach( var c in columnWidths) {
                formatString.Append("{");
                formatString.Append(n++);
                formatString.Append(",-");
                formatString.Append(c);
                formatString.Append("}|");
            }
            var fmt = formatString.ToString();

            var breaker =  "-".PadLeft(columnWidths.Sum() + columnWidths.Count+1, '-');
            
            Console.WriteLine(breaker);
            Console.WriteLine(fmt, columnTitles.ToArray());
            Console.WriteLine(breaker);
            for (var rownum = 0; rownum < data[data.Keys.First()].Count; rownum++) {
                Console.WriteLine(fmt, data.Keys.Select(k => data[k][rownum]).ToArray());
            }
            Console.WriteLine(breaker);

            /*
            Packages.Dump(Console.WindowWidth)
            

            string fmt = "|{0,35}|{1,20}|{2,5}|{3,20}|{4,8}|{5,20}|";
            string line = "--------------------------------------------------------";
            Console.WriteLine(fmt, "Filename", "Name", "Arch", "Version", "Key", "GUID");
            Console.WriteLine(fmt, trimto(line, 35), trimto(line, 20), trimto(line, 5), trimto(line, 20), trimto(line, 8), trimto(line, 20));

            foreach (var p in pkgs) {
                Console.WriteLine(fmt, trimto(p.LocalPackagePath ?? "(unknown)", 35), trimto(p.Name, 20), p.Architecture,
                    p.Version.UInt64VersiontoString(), trimto(p.PublicKeyToken, 8), trimto(p.ProductCode, 20));
            }
            Console.WriteLine(fmt, trimto(line, 35), trimto(line, 20), trimto(line, 5), trimto(line, 20), trimto(line, 8), trimto(line, 20));
            Console.WriteLine("\r\n");*/
        }

    }
}