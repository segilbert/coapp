//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Collections {
    using System.Collections;

    public class EasyListDictionary<TKey, TCollectionValue> : IDictionary<TKey, IList<TCollectionValue>> {
        readonly IDictionary<TKey, IList<TCollectionValue>> _dictionary;

        public EasyListDictionary() {
            _dictionary = new Dictionary<TKey, IList<TCollectionValue>>();
        }

        public void Add(TKey key, IList<TCollectionValue> value) {
            if (_dictionary.ContainsKey(key)) {
                foreach (var item in value.Where(item => !_dictionary[key].Contains<TCollectionValue>(item))) {
                    _dictionary[key].Add(item);
                }
            }
            else {
                _dictionary.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key) {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys {
            get {
                return _dictionary.Keys;
            }
        }

        public bool Remove(TKey key) {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out IList<TCollectionValue> value) {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<IList<TCollectionValue>> Values {
            get {
                return _dictionary.Values;
            }
        }

        public IList<TCollectionValue> this[TKey key] {
            get {
                if (_dictionary.ContainsKey(key))
                    return _dictionary[key];

                var result = new List<TCollectionValue>();
                _dictionary.Add(key, result);
                return result;
            }
            set {
                if (_dictionary.ContainsKey(key)) {
                    foreach (var item in value.Where(item => !_dictionary[key].Contains<TCollectionValue>(item))) {
                        _dictionary[key].Add(item);
                    }
                }
                else {
                    _dictionary.Add(key, value);
                }
            }
        }

        public void Add(KeyValuePair<TKey, IList<TCollectionValue>> item) {
            this[item.Key] = item.Value;
        }

        public void Clear() {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, IList<TCollectionValue>> item) {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, IList<TCollectionValue>>[] array, int arrayIndex) {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public int Count {
            get {
                return _dictionary.Count;
            }
        }

        public bool IsReadOnly {
            get {
                return _dictionary.IsReadOnly;
            }
        }

        public bool Remove(KeyValuePair<TKey, IList<TCollectionValue>> item) {
            return _dictionary.Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, IList<TCollectionValue>>> GetEnumerator() {
            return _dictionary.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}