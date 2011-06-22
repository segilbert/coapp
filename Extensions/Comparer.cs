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

     public class EqualityComparer<T> : IEqualityComparer<T> {
        private readonly Func<T, T, bool> _equalityCompareFn;
        private readonly Func<T, int> _getHashCodeFn;
        public EqualityComparer(Func<T,T,bool> equalityCompareFn,Func<T,int> getHashCodeFn) {
            _equalityCompareFn= equalityCompareFn;
            _getHashCodeFn = getHashCodeFn;
        }

         public bool Equals(T x, T y) {
             return _equalityCompareFn(x, y);
         }

         public int GetHashCode(T obj) {
             return _getHashCodeFn(obj);
         }
     }
}