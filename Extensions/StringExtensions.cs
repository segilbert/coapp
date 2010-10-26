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

        public static bool OnlyContains(this string str, char[] characters ) {
            
            for(int x = 0;x<str.Length;x++) {
                char ch = str[x];
                bool found = false;
                for(int y=0;y<characters.Length ; y++) {
                    if( ch == characters[y] ) {
                        found = true;
                        break;
                    }
                }
                if( !found )
                    return false;
            }
            return true;
        }

        public static bool OnlyContains(this string str, string characters) {
            return OnlyContains(str,characters.ToCharArray());
        }

    }
}