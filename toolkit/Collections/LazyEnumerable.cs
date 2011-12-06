//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Collections {
    using System.Collections;
    using System.Collections.Generic;


    /// <summary>
    /// This IEnumerable Wrapper will cache the results incrementally on first use of the source collection
    /// into a List, so that subsequent uses of the collection are pulled from the list.
    /// (and it doesn't need to iterate thru the whole collection first, like ToList() )
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LazyEnumerable<T> : IEnumerable<T> {
        private readonly IEnumerable<T> _source;
        private IEnumerator<T> _sourceIterator;
        private List<T> _list;

        public LazyEnumerable(IEnumerable<T> source) {
            _source = source;
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator() {
            lock (this) {
                if (_list == null) {
                    _list = new List<T>();
                }
            }

            return new LazyEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        private bool IsOk(int index) {
            if (index < _list.Count) {
                return true;
            }

            lock (this) {
                if (_sourceIterator == null) {
                    _sourceIterator = _source.GetEnumerator();
                }

                while (_sourceIterator.MoveNext()) {
                    _list.Add(_sourceIterator.Current);
                    if (index < _list.Count) {
                        return true;
                    }
                }
            }
            return false;
        }

        #region Nested type: LazyEnumerator

        private class LazyEnumerator<TT> : IEnumerator<TT> {
            private LazyEnumerable<TT> _collection;
            private int _index = -1;

            internal LazyEnumerator(LazyEnumerable<TT> collection) {
                _collection = collection;
            }

            #region IEnumerator<Tt> Members

            public TT Current {
                get {
                    return _collection._list[_index];
                }
            }

            public void Dispose() {
                _collection = null;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public bool MoveNext() {
                _index++;
                return _collection.IsOk(_index);
            }

            public void Reset() {
                _index = -1;
            }

            #endregion
        }

        #endregion
    }
}