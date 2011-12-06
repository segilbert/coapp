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
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class VirtualEnumerator<T> : IEnumerator<T> {
        private readonly IEnumerator _backingEnumerator;
        private readonly Func<IEnumerator, T> _currentFunction;

        public VirtualEnumerator(IEnumerator backingEnumerator, Func<IEnumerator, T> fn) {
            _currentFunction = fn;
            _backingEnumerator = backingEnumerator;
        }

        public T Current {
            get {
                return _currentFunction(_backingEnumerator);
            }
        }

        public void Dispose() {
            if (_backingEnumerator is IDisposable)
                (_backingEnumerator as IDisposable).Dispose();
        }

        object IEnumerator.Current {
            get {
                return _currentFunction(_backingEnumerator);
            }
        }

        public bool MoveNext() {
            return _backingEnumerator.MoveNext();
        }

        public void Reset() {
            _backingEnumerator.Reset();
        }
    }
}