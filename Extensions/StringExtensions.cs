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
    using System.Text.RegularExpressions;

    public static class StringExtensions {
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
    }
}