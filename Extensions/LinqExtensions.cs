//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class LinqExtensions {
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> recurseFunction) {
            foreach(var item in source) {
                yield return item;
                var enumerable = recurseFunction(item);

                if(enumerable != null) {
                    foreach(var itemRecurse in Traverse(enumerable, recurseFunction)) {
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
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T source) {
            return new[] {source};
        }

        /// <summary>
        /// Returns a new collection including the given item.
        /// 
        /// DOES NOT MODIFY THE ORIGINAL COLLECTION.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="newItem"></param>
        /// <returns></returns>
        public static IEnumerable<T> Add<T>(this IEnumerable<T> collection, T newItem) {
            return collection.Union( new[] { newItem } );
        }

        private class IndexedEnumerator : IEnumerator<int> {
            private int _max;

            internal IndexedEnumerator(int i) {
                _max = i;
                Current = -1;
            }

            public void Dispose() {
            }

            public bool MoveNext() {
                return ++Current < _max;
            }

            public void Reset() {
                Current = -1;
            }

            public int Current { get; private set; }

            object IEnumerator.Current {
                get { return Current; }
            }
        }

        private class ListIndex : IEnumerable<int> {
            private int _max;
            internal ListIndex(int i) {
                _max = i;
            }
            public IEnumerator<int> GetEnumerator() {
                return new IndexedEnumerator(_max);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        public static IEnumerable<int> ByIndex<T>(this IList<T> lst) {
            return new ListIndex(lst.Count);
        }
    }
}