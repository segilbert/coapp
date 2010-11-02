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
    }
}