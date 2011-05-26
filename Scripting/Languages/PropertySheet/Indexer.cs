//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Collections;

    public class Indexer<T> : IEnumerable<T> where T : class {
        private readonly Func<IEnumerable<string>> _keysFn;
        private readonly Func<string, IEnumerable<Rule>> _lookupFn;
        private readonly Func<string, Rule> _newRuleFn;

        public T this[string index] {
            get {
                var rule = _lookupFn(index).FirstOrDefault() ?? _newRuleFn(index);
                return (T)Activator.CreateInstance(typeof(T), rule);
            }
        }

        public IEnumerable<string> Keys {
            get {
                return _keysFn();
            }
        }

        public Indexer(Func<IEnumerable<string>> keysFn, Func<string, IEnumerable<Rule>> lookupFn, Func<string, Rule> newRuleFn ) {
            _keysFn = keysFn;
            _lookupFn = lookupFn;
            _newRuleFn = newRuleFn;
        }

        /*
        public class IndexerEnumerator : IEnumerator<T> {
            private Indexer<T> _indexer;
            private IEnumerator<string> _enumerator;

            public IndexerEnumerator(Indexer<T> indexer) {
                _indexer = indexer;
                _enumerator = indexer.Keys.GetEnumerator();
            }

            public void Dispose() {
                _indexer = null;
                _enumerator = null;
            }

            public bool MoveNext() {
                return _enumerator.MoveNext();
            }

            public void Reset() {
                _enumerator.Reset();
            }

            public T Current {
                get {
                    return _indexer[_enumerator.Current];
                }
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }
        }
        */

        public IEnumerator<T> GetEnumerator() {
            return new VirtualEnumerator<T>(Keys.GetEnumerator(), enumerator => this[(string)enumerator.Current]);
            // return new IndexerEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new VirtualEnumerator<T>(Keys.GetEnumerator(), enumerator => this[(string)enumerator.Current]);
            // return new IndexerEnumerator(this);
        }
    }
}