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
    using System.Linq;
    using System.Runtime.Remoting.Metadata.W3cXsd2001;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.IO.Compression;
    using System.IO;

    public static class StringExtensions {
        public const string LettersNumbersUnderscoresAndDashes = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-";
        public const string LettersNumbersUnderscoresAndDashesAndDots = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-.";
        
        
        public const string ValidVersionRegex = @"^\d{1,5}\.\d{1,5}\.\d{1,5}\.\d{1,5}$";
        
        //putting regexs here so they're only compiled once.
        private static Regex versionRegex = new Regex(ValidVersionRegex);
        private static Regex badDirIdCharsRegex = new Regex(@"\s|\.|\-|\\");
        private static Regex majorMinorRegex = new Regex(@"^\d{1,5}\.\d{1,5}$");

        //TODO this SUCKS. Thanks MS.
        private static Regex emailRegex = new Regex(@"^(?<name>\S+)@(?<domain>\S+)$");

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

       

        private static Dictionary<string, Regex> wildcards = new Dictionary<string, Regex>();

        public static bool oldIsWildcardMatch(this string text, string wildcardMask) {
            if (wildcards.ContainsKey(wildcardMask))
                return wildcards[wildcardMask].IsMatch(text);
            
            var mask = new Regex(
                '^' +
                wildcardMask
                    .Replace(".", "[.]")
                    .Replace("\\", "\\\\")
                    .Replace("*", ".*")
                    .Replace("?", ".")
                + '$',
                RegexOptions.IgnoreCase);
            
            wildcards.Add(wildcardMask,mask);
            
            return mask.IsMatch(text);
        }

         public static bool IsWildcardMatch(this string text, string wildcardMask, string ignorePrefix = null) {
             ignorePrefix = ignorePrefix ?? string.Empty;
             
             if( ignorePrefix.EndsWith(@"\" ) )
                ignorePrefix = ignorePrefix.Substring(ignorePrefix.Length -1);

             var key = wildcardMask + (ignorePrefix ?? string.Empty);

             if (wildcards.ContainsKey(key))
                return wildcards[key].IsMatch(text);

             if( !wildcardMask.Contains("\\") )
                wildcardMask = @"**\"+wildcardMask ;

             if( wildcardMask.EndsWith("**") )
                wildcardMask += @"\*";

             var mask = new Regex( '^' + Regex.Escape(ignorePrefix) +
                 (wildcardMask
                    .Replace(".", @"[.]")
                    .Replace(@"\", @"\\")
                    .Replace("?", ".")
                    .Replace("**", @"?")// temporarily move it so the next one doesn't clobber
                    .Replace("*", @"[^\\\/\<\>\|]*") //     \/\<\>\|
                    .Replace("?", @"[^\<\>\|]*") + '$'), RegexOptions.IgnoreCase);
            
            wildcards.Add(key,mask);

            return mask.IsMatch(text);
        }

        public static bool HasWildcardMatch(this IEnumerable<string> source, string value,string ignorePrefix = null) {
            return source.Any(wildcard => value.IsWildcardMatch(wildcard,ignorePrefix));
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

        public static string UInt64VersiontoString(this UInt64 version)
        {
            return string.Format("{0}.{1}.{2}.{3}", (version >> 48) & 0xFFFF, (version >> 32) & 0xFFFF, (version >> 16) & 0xFFFF, (version) & 0xFFFF);
        }
        /// <summary>
        /// Calculates the MD5 hash of a string. Additionally all the letters in the hash are in uppercase.
        /// </summary>
        /// <param name="input">a string to a calculate the hash for</param>
        /// <returns>MD5 hash of the string</returns>
        public static string MD5Hash(this string input)
        {
            using (var hasher = MD5.Create()) {
                return hasher.ComputeHash(Encoding.Unicode.GetBytes(input)).Aggregate(string.Empty,
                    (current, b) => current + b.ToString("x2").ToUpper());
            }
        }

        public static string CreatePublicKeyToken(this string publicKey)
        {
            SHA1Managed m = new SHA1Managed();
            var hashBytes = m.ComputeHash(SoapHexBinary.Parse(publicKey).Value);
            var last8BytesReversed = hashBytes.Reverse().Take(8);

            return new SoapHexBinary(last8BytesReversed.ToArray()).ToString();
           
        }

        /// <summary>
        /// Creates a safe directory ID for MSI for a possibly non-safe one.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Your safe directory ID</returns>
        public static string MakeSafeDirectoryId(this string input)
        {
            return badDirIdCharsRegex.Replace(input, "_");
        }

        /// <summary>
        /// Checks if a string is a valid version string x.x.x.x 
        /// 
        /// TODO: this allows x to have values LARGER than the max number 
        /// for part of a version string. NEED TO FIX
        /// </summary>
        /// <param name="input">a string to be checked</param>
        /// <returns>true if it the string is a valid version, false otherwise</returns>
		public static bool IsValidVersion(this string input)
        {
            return versionRegex.IsMatch(input);
        }


        /// <summary>
        /// Checks if a string is a valid major.minor version string x.x
        /// 
        /// TODO: this allows x to have values LARGER than the max number 
        /// for part of a version string. NEED TO FIX
        /// </summary>
        /// <param name="input">a string to be checked</param>
        /// <returns>true if it the string is a valid major.minor version, false otherwise</returns>
        public static bool IsValidMajorMinorVersion(this string input)
        {
            return majorMinorRegex.IsMatch(input);
        }


        public static byte[] Gzip(this string input)
        {

            var memStream = new MemoryStream();
            using (GZipStream gzStr = new GZipStream(memStream, CompressionMode.Compress))
            {
                gzStr.Write(input.ToByteArray(), 0, input.ToByteArray().Length);
            }

            return memStream.ToArray();
        }

        public static string GzipToBase64(this string input)
        {
            if (input == null || input == String.Empty)
                return input;
            return Convert.ToBase64String(Gzip(input));
        }

        public static string GunzipFromBase64(this string input)
        {
            if (input == null || input == String.Empty)
                return input;
            return Gunzip(Convert.FromBase64String(input));
        }

        public static string Gunzip(this byte[] input)
        {
            var bytes = new List<byte>();
            using (GZipStream gzStr = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
            {
                var bytesRead = new byte[512];
                while (true)
                {
                    int numRead = gzStr.Read(bytesRead, 0, 512);
                    if (numRead > 0)
                    {
                        bytes.AddRange(bytesRead.Take(numRead));
                    }
                    else
                        break;
                }
            }

            return bytes.ToArray().ToUtf8String();
        }


        public static bool IsEmail(this string email)
        {
            return emailRegex.IsMatch(email);
        }
    }
}