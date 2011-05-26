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
                return (T)Activator.CreateInstance(typeof(T),  _lookupFn(index).FirstOrDefault() ?? _newRuleFn(index));
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

        public IEnumerator<T> GetEnumerator() {
            return new VirtualEnumerator<T>(Keys.GetEnumerator(), enumerator => this[(string)enumerator.Current]);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new VirtualEnumerator<T>(Keys.GetEnumerator(), enumerator => this[(string)enumerator.Current]);
        }
    }
}