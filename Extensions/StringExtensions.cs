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
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class StringExtensions {
        public const string LettersNumbersUnderscoresAndDashes = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-";

        public static string format(this string formatString, params object[] args) {
            return string.Format(formatString, args);
        }

        public static void Print(this string formatString, params object[] args) {
            Console.WriteLine(formatString, args);
        }

        public static void Error(this string formatString, params object[] args) {
            Console.Error.WriteLine(formatString, args);
        }

        public static Match Match(this string input, string rxExpression) {
            return new Regex(rxExpression).Match(input);
        }

        public static Match MatchIgnoreCase(this string input, string rxExpression) {
            return new Regex(rxExpression, RegexOptions.IgnoreCase).Match(input);
        }

        public static int ToInt32(this string str) {
            return str.ToInt32(0);
        }

        public static int ToInt32(this string str, int defaultValue) {
            var i = defaultValue;
            Int32.TryParse(str, out i);
            return i;
        }

        public static bool OnlyContains(this string str, char[] characters) {
            for (int x = 0; x < str.Length; x++) {
                char ch = str[x];
                bool found = false;
                for (int y = 0; y < characters.Length; y++) {
                    if (ch == characters[y]) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    return false;
                }
            }
            return true;
        }

        public static bool OnlyContains(this string str, string characters) {
            return OnlyContains(str, characters.ToCharArray());
        }

        public static int PositionOfFirstCharacterNotIn(this string str, char[] characters) {
            int p = 0;
            while (p < str.Length) {
                if (!characters.Contains(str[p])) {
                    return p;
                }
                p++;
            }
            return p;
        }

        public static int PositionOfFirstCharacterNotIn(this string str, string characters) {
            return PositionOfFirstCharacterNotIn(str, characters.ToCharArray());
        }

        public static Guid CreateGuid(this string str) {
            Guid guid;
            if (!Guid.TryParse(str, out guid)) {
                guid = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str)));
            }
            return guid;
        }

        public static bool IsWildcardMatch(this string text, string wildcardMask) {
            var mask = new Regex(
                '^' +
                wildcardMask
                    .Replace(".", "[.]")
                    .Replace("\\", "\\\\")
                    .Replace("*", ".*")
                    .Replace("?", ".")
                + '$',
                RegexOptions.IgnoreCase);
            return mask.IsMatch(text);
        }

        public static bool IsTrue(this string text) {
            return text.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool IsFalse(this string text) {
            return text.Equals("false", StringComparison.CurrentCultureIgnoreCase);
        }

        public static byte[] ToByteArray(this string text) {
            return Encoding.UTF8.GetBytes(text);
        }

        public static string ToUtf8String(this byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] Protect(this string text, string salt = "CoAppToolkit") {
            return ProtectedData.Protect((text ?? string.Empty).ToByteArray(), salt.ToByteArray(), DataProtectionScope.CurrentUser);
        }

        public static string Unprotect(this byte[] data, string salt = "CoAppToolkit") {
            if (data == null || data.Length == 0)
                return string.Empty;
            try {
                return ProtectedData.Unprotect(data, salt.ToByteArray(), DataProtectionScope.CurrentUser).ToUtf8String();    
            }
            catch {
                /* suppress */
            }
            return string.Empty;
        }

        public static UInt64 VersionStringToUInt64(this string version) {
            if (string.IsNullOrEmpty(version))
                return 0;
            var vers = version.Split('.');
            var major = vers.Length > 0 ? vers[0].ToInt32(0) : 0;
            var minor = vers.Length > 1 ? vers[1].ToInt32(0) : 0;
            var build = vers.Length > 2 ? vers[2].ToInt32(0) : 0;
            var revision = vers.Length > 3 ? vers[3].ToInt32(0) : 0;

            return (((UInt64)major) << 48) + (((UInt64)minor) << 32) + (((UInt64)build) << 16) + (UInt64)revision;
        }
    }
}