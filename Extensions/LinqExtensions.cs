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

        /// <summary>
        /// Creates an enumerable consisting of a single element.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="source"/></typeparam>
        /// <param name="source">The sole element in the return IEnumerable</param>
        /// <returns>An <see cref="IEnumerable{T}"/> with <paramref name="source"/> as it's only element.</returns>
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T source) {
            return new[] {source};
        }

        /// <summary>
        /// Returns all the contiguous elements from sequence except for a specified number from the end.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to not return from the end.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}"/> consisting of all the elements of <paramref name="source"/> 
        /// except for the last <paramref name="count"/> elements.</returns>
        public static IEnumerable<T> TakeAllBut<T>(this IEnumerable<T> source, int count)
        {
            return source.Reverse().Skip(count).Reverse();
        }

        /// <summary>
        /// Returns the given number of elements from the end of the sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return from the end of the array.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}"/> consisting of the last <paramref name="count"/>
        /// elements of <paramref name="source"/>.</returns>
        public static IEnumerable<T> TakeFromEnd<T>(this IEnumerable<T> source, int count)
        {
            return source.Reverse().Take(count).Reverse();
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