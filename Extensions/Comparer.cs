//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.Collections.Generic;

    public class Comparer<T> : IComparer<T> {
        private readonly Func<T, T, int> _compareFuction;
        public Comparer(Func<T,T,int> compareFn) {
            _compareFuction = compareFn;
        }
        public int Compare(T x, T y) {
            return _compareFuction(x,y);
        }
    }
}