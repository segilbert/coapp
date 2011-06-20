//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2011 Eric Schultz, 2010  Garrett Serack. All rights reserved.
//     Version regex string from Wix toolkit
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
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Runtime.Remoting.Metadata.W3cXsd2001;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class StringExtensions {
        public const string LettersNumbersUnderscores = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_";
        public const string LettersNumbersUnderscoresAndDashes = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-";
        public const string LettersNumbersUnderscoresAndDashesAndDots = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-.";
        public const string ValidVersionRegex = @"^\d{1,5}\.\d{1,5}\.\d{1,5}\.\d{1,5}$";

        /// <summary>
        /// These are crazy, but valid filepath characters that cause regexs to puke and fail.
        /// </summary>
        private static readonly string[] _validFpCharsThatHurtRegexs = { ".", "$", "^", "{", "[", "(", "|", ")", "+" };

        //putting regexs here so they're only compiled once.
        #pragma warning disable 169
        private static Regex _versionRegex = new Regex(ValidVersionRegex);
        #pragma warning restore 169

        private static readonly Regex _badDirIdCharsRegex = new Regex(@"\s|\.|\-|\\");
        private static readonly Regex _majorMinorRegex = new Regex(@"^\d{1,5}\.\d{1,5}$");

        //TODO this SUCKS. Thanks MS.
        private static readonly Regex _emailRegex = new Regex(@"^(?<name>\S+)@(?<domain>\S+)$");

        // ReSharper disable InconsistentNaming
        public static string format(this string formatString, params object[] args) {
            return string.Format(formatString, args);
        }
        // ReSharper restore InconsistentNaming

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

        public static int ToInt32(this string str, int defaultValue = 0) {
            int i;
            return Int32.TryParse(str, out i) ? i : defaultValue;
        }

        public static bool OnlyContains(this string str, char[] characters) {
            return str.Select(ch => characters.Any(t => ch == t)).All(found => found);
        }

        public static bool OnlyContains(this string str, string characters) {
            return OnlyContains(str, characters.ToCharArray());
        }

        public static int PositionOfFirstCharacterNotIn(this string str, char[] characters) {
            var p = 0;
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


        private static readonly Dictionary<string, Regex> _wildcards = new Dictionary<string, Regex>();

        public static bool OldIsWildcardMatch(this string text, string wildcardMask) {
            if (_wildcards.ContainsKey(wildcardMask)) {
                return _wildcards[wildcardMask].IsMatch(text);
            }

            var mask = new Regex(
                '^' +
                    wildcardMask
                        .Replace(".", "[.]")
                        .Replace("\\", "\\\\")
                        .Replace("*", ".*")
                        .Replace("?", ".")
                            + '$',
                RegexOptions.IgnoreCase);

            _wildcards.Add(wildcardMask, mask);

            return mask.IsMatch(text);
        }

        public static bool IsWildcardMatch(this string text, string wildcardMask, string ignorePrefix = null, bool escapePrefix = true) {
            ignorePrefix = string.IsNullOrEmpty(ignorePrefix) ? @".*\\?" : escapePrefix ? Regex.Escape(ignorePrefix) : ignorePrefix;

            var key = wildcardMask + ignorePrefix;
            if (_wildcards.ContainsKey(key)) {
                return _wildcards[key].IsMatch(text);
            }

          //  wildcardMask = wildcardMask.CommentEach(_validFpCharsThatHurtRegexs);

            if (wildcardMask.EndsWith("**")) {
                wildcardMask += @"\*";
            }

            var mask =
                new Regex(
                    '^' + ignorePrefix +
                        (wildcardMask.Replace(".", @"[.]").Replace(@"\", @"\\").Replace("?", @".").Replace("+", @"\+").Replace("**",
                            @"?") // temporarily move it so the next one doesn't clobber
                            .Replace("*", @"[^\\\/\<\>\|]*") //     \/\<\>\|
                            .Replace("?", @"[^\<\>\|]*") + '$'), RegexOptions.IgnoreCase);
            lock (_wildcards) {
                if (!_wildcards.ContainsKey(key)) {
                    _wildcards.Add(key, mask);
                }
            }
            return mask.IsMatch(text);
        }

        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value) {
            return (from each in source where each.Equals(value, StringComparison.CurrentCultureIgnoreCase) select each).Any();
        }

        public static bool HasWildcardMatch(this IEnumerable<string> source, string value, string ignorePrefix = null,
            bool escapePrefix = true) {
            return source.Any(wildcard => value.IsWildcardMatch(wildcard, wildcard.Contains(@"\\") ? ignorePrefix : null, escapePrefix));
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

        public static string ToUtf8String(this IEnumerable<byte> bytes) {
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static IEnumerable<byte> ProtectBinaryForMachine(this IEnumerable<byte> binaryData, string salt = "CoAppToolkit") {
            return ProtectedData.Protect(binaryData.ToArray(), salt.ToByteArray(), DataProtectionScope.LocalMachine);
        }

        public static IEnumerable<byte> ProtectBinaryForUser(this IEnumerable<byte> binaryData, string salt = "CoAppToolkit") {
            return ProtectedData.Protect(binaryData.ToArray(), salt.ToByteArray(), DataProtectionScope.CurrentUser);
        }

        public static IEnumerable<byte> ProtectForMachine(this string text, string salt = "CoAppToolkit") {
            return ProtectBinaryForMachine((text ?? string.Empty).ToByteArray(), salt);
        }

        public static IEnumerable<byte> ProtectForUser(this string text, string salt = "CoAppToolkit") {
            return ProtectBinaryForUser((text ?? string.Empty).ToByteArray(), salt);
        }

        public static IEnumerable<byte> UnprotectBinaryForUser(this IEnumerable<byte> binaryData, string salt = "CoAppToolkit") {
            if (binaryData.IsNullOrEmpty()) {
                return Enumerable.Empty<byte>();
            }

            try {
                return ProtectedData.Unprotect(binaryData.ToArray(), salt.ToByteArray(), DataProtectionScope.CurrentUser);
            }
            catch {
                /* suppress */
            }
            return Enumerable.Empty<byte>();
        }

        public static IEnumerable<byte> UnprotectBinaryForMachine(this IEnumerable<byte> binaryData, string salt = "CoAppToolkit") {
            if (binaryData.IsNullOrEmpty()) {
                return Enumerable.Empty<byte>();
            }

            try {
                return ProtectedData.Unprotect(binaryData.ToArray(), salt.ToByteArray(), DataProtectionScope.LocalMachine);
            }
            catch {
                /* suppress */
            }
            return Enumerable.Empty<byte>();
        }

        public static string UnprotectForUser(this IEnumerable<byte> binaryData, string salt = "CoAppToolkit") {
            var data = binaryData.UnprotectBinaryForUser(salt);
            return data.Any() ? data.ToUtf8String() : string.Empty;
        }

        public static string UnprotectForMachine(this IEnumerable<byte> binaryData, string salt = "CoAppToolkit") {
            var data = binaryData.UnprotectBinaryForMachine(salt);
            return data.Any() ? data.ToUtf8String() : string.Empty;
        }

        public static UInt64 VersionStringToUInt64(this string version) {
            if (string.IsNullOrEmpty(version)) {
                return 0;
            }
            var vers = version.Split('.');
            var major = vers.Length > 0 ? vers[0].ToInt32(0) : 0;
            var minor = vers.Length > 1 ? vers[1].ToInt32(0) : 0;
            var build = vers.Length > 2 ? vers[2].ToInt32(0) : 0;
            var revision = vers.Length > 3 ? vers[3].ToInt32(0) : 0;

            return (((UInt64) major) << 48) + (((UInt64) minor) << 32) + (((UInt64) build) << 16) + (UInt64) revision;
        }

        public static string UInt64VersiontoString(this UInt64 version) {
            return string.Format("{0}.{1}.{2}.{3}", (version >> 48) & 0xFFFF, (version >> 32) & 0xFFFF, (version >> 16) & 0xFFFF,
                (version) & 0xFFFF);
        }

        /// <summary>
        ///   Calculates the MD5 hash of a string. Additionally all the letters in the hash are in uppercase.
        /// </summary>
        /// <param name = "input">a string to a calculate the hash for</param>
        /// <returns>MD5 hash of the string</returns>
        public static string MD5Hash(this string input) {
            using (var hasher = MD5.Create()) {
                return hasher.ComputeHash(Encoding.Unicode.GetBytes(input)).Aggregate(string.Empty,
                    (current, b) => current + b.ToString("x2").ToUpper());
            }
        }

        public static string CreatePublicKeyToken(this IEnumerable<byte> publicKey) {
            var m = new SHA1Managed();
            var hashBytes = m.ComputeHash(publicKey.ToArray());
            var last8BytesReversed = hashBytes.Reverse().Take(8);

            return new SoapHexBinary(last8BytesReversed.ToArray()).ToString();
        }

        /// <summary>
        ///   Creates a safe directory ID for MSI for a possibly non-safe one.
        /// </summary>
        /// <param name = "input"></param>
        /// <returns>Your safe directory ID</returns>
        public static string MakeSafeDirectoryId(this string input) {
            return _badDirIdCharsRegex.Replace(input, "_");
        }

        /// <summary>
        ///   Checks if a string is a valid version string x.x.x.x 
        /// 
        ///   TODO: this allows x to have values LARGER than the max number 
        ///   for part of a version string. NEED TO FIX
        /// </summary>
        /// <param name = "input">a string to be checked</param>
        /// <param name="strict">should be strict?</param>
        /// <returns>true if it the string is a valid version, false otherwise</returns>
        public static bool IsValidVersion(this string input, bool strict = true) {
            var verParts = input.Split('.');
            //too many parts!
            return verParts.Length <= 4 && verParts.All(part => part.IsValidVersionPart());
        }

        public static bool IsValidVersionPart(this string input) {
            int part;
            //it's not even an integer so we fail
            if (!Int32.TryParse(input, out part)) {
                return false;
            }

            //part is too damn big
            if (part < 0 || part > 65535) {
                return false;
            }

            return true;
        }

        public static string ExtendVersion(this string input) {
            if (!input.IsValidVersion(false)) {
                return null;
            }

            var partList = input.SplitToList('.');

            if (partList.Count == 4) {
                return input;
            }

            while (partList.Count != 4) {
                partList.Add("0");
            }

            var output = new StringBuilder();
            for (var i = 0; i < 4; i++) {
                output.Append(partList[i]);
                if (i != 3) {
                    output.Append(".");
                }
            }

            return output.ToString();
        }


        /// <summary>
        ///   Checks if a string is a valid major.minor version string x.x
        /// 
        ///   TODO: this allows x to have values LARGER than the max number 
        ///   for part of a version string. NEED TO FIX
        /// </summary>
        /// <param name = "input">a string to be checked</param>
        /// <returns>true if it the string is a valid major.minor version, false otherwise</returns>
        public static bool IsValidMajorMinorVersion(this string input) {
            return _majorMinorRegex.IsMatch(input);
        }


        public static byte[] Gzip(this string input) {
            var memStream = new MemoryStream();
            using (var gzStr = new GZipStream(memStream, CompressionMode.Compress)) {
                gzStr.Write(input.ToByteArray(), 0, input.ToByteArray().Length);
            }

            return memStream.ToArray();
        }

        public static string GzipToBase64(this string input) {
            return string.IsNullOrEmpty(input) ? input : Convert.ToBase64String(Gzip(input));
        }

        public static string GunzipFromBase64(this string input) {
            return string.IsNullOrEmpty(input) ? input : Gunzip(Convert.FromBase64String(input));
        }

        public static string Gunzip(this byte[] input) {
            var bytes = new List<byte>();
            using (var gzStr = new GZipStream(new MemoryStream(input), CompressionMode.Decompress)) {
                var bytesRead = new byte[512];
                while (true) {
                    var numRead = gzStr.Read(bytesRead, 0, 512);
                    if (numRead > 0) {
                        bytes.AddRange(bytesRead.Take(numRead));
                    }
                    else {
                        break;
                    }
                }
            }

            return bytes.ToArray().ToUtf8String();
        }


        public static bool IsEmail(this string email) {
            return _emailRegex.IsMatch(email);
        }

        public static string ReplaceEach(this string input, IEnumerable<string> oldValues, IEnumerable<string> newValues) {
            //TODO I feel like there's a LINQ-ier way to do this.

            if (oldValues.Count() != newValues.Count())
            {
                return null;
            }
            


            //oldValues.Aggregate(input, (output, ))


            return oldValues.Zip(newValues, (first, second) => new { first, second }).Aggregate(input, (accm, x) => accm.Replace(x.first, x.second));

        }

        public static string CommentEach(this string input, IEnumerable<string> toComment)
        {
            return input.ReplaceEach(toComment, toComment.Select((s) => @"\" + s));
        }

        public static byte[] GetKeyTokenFromFullKey(this byte[] fullKey)
        {
            SHA1CryptoServiceProvider csp = new SHA1CryptoServiceProvider();
            byte[] hash = csp.ComputeHash(fullKey);

            byte[] token = new byte[8];
            for (int i = 0; i < 8; i++ )
                token[i] = hash[hash.Length - (i+1)];

            return hash;
        }

    }
}