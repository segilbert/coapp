//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.Collections.Generic;

    public static class LinqExtensions {
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> recurseFunction) {
            foreach(T item in source) {
                yield return item;
                IEnumerable<T> enumerable = recurseFunction(item);

                if(enumerable != null) {
                    foreach(T itemRecurse in Traverse(enumerable, recurseFunction)) {
                        yield return itemRecurse;
                    }
                }
            }
        }

        public static T MaxElement<T, U>(this IEnumerable<T> source, Func<T, U> selector) where U : IComparable<U> {
            if (source == null) throw new ArgumentNullException("source");
            bool first = true;
            T maxObj = default(T);
            U maxKey = default(U);
            foreach (var item in source) {
                if (first) {
                    maxObj = item;
                    maxKey = selector(maxObj);
                    first = false;
                }
                else {
                    U currentKey = selector(item);
                    if (currentKey.CompareTo(maxKey) > 0) {
                        maxKey = currentKey;
                        maxObj = item;
                    }
                }
            }
            return maxObj;
        }
        public static IEnumerable<T> AsArray<T>(this T source) {
            return new[] {source};
        }
    }
}