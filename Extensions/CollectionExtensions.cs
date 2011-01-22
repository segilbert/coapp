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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    public static class CollectionExtensions {
        /// <summary>
        ///   Splits a string into a List of strings.
        /// </summary>
        /// <param name = "str"></param>
        /// <param name = "separator"></param>
        /// <returns></returns>
        public static List<string> SplitToList(this string str, params char[] separator) {
            var result = new List<string>();
            if(!string.IsNullOrEmpty(str)) {
                result.AddRange(str.Split(separator));
            }

            return result;
        }

        /// <summary>
        ///   Removes duplicate strings from a list.
        /// </summary>
        /// <param name = "list"></param>
        /// <param name = "stringComparison"></param>
        /// <returns></returns>
        public static List<string> Uniq(this List<string> list, StringComparison stringComparison) {
            for(var i = 0; i < list.Count; i++) {
                for(var j = list.Count - 1; j > i; j--) {
                    if(list[i].Equals(list[j], stringComparison)) {
                        list.RemoveAt(j);
                    }
                }
            }
            return list;
        }

        ///<summary>
        ///  Removes duplicate strings from a list.
        ///
        ///  Assumes Case Sensitivity.
        ///</summary>
        ///<param name = "list"></param>
        ///<returns></returns>
        public static List<string> Uniq(this List<string> list) {
            for(var i = 0; i < list.Count; i++) {
                for(var j = list.Count - 1; j > i; j--) {
                    if(list[i].Equals(list[j])) {
                        list.RemoveAt(j);
                    }
                }
            }
            return list;
        }

        /// <summary>
        ///   Combines a list of strings into a single string seperated by seperator
        /// </summary>
        /// <param name = "list"></param>
        /// <param name = "separator"></param>
        /// <returns></returns>
        public static string Combine(this List<string> list, char separator) {
            var sb = new StringBuilder();
            foreach(string s in list) {
                if(sb.Length > 0) {
                    sb.Append(separator);
                }
                sb.Append(s);
            }
            return sb.ToString();
        }

        /// <summary>
        ///   Combines a list of strings into a single string seperated by seperator
        /// </summary>
        /// <param name = "list"></param>
        /// <param name = "separator"></param>
        /// <returns></returns>
        public static string Combine(this List<string> list, string separator) {
            var sb = new StringBuilder();
            foreach(string s in list) {
                if(sb.Length > 0) {
                    sb.Append(separator);
                }
                sb.Append(s);
            }
            return sb.ToString();
        }

        public static void AddRange<T>(this Collection<T> destination, IEnumerable<T> collection) {
            foreach (var i in collection)
                destination.Add(i);
        }
    }
}