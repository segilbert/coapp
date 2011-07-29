//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Collections;

    public class DictionaryProperty : IDictionary<string, IList<string>>, IList<string> {
        private readonly Rule _rule;
        private readonly string _name;
        public DictionaryProperty(Rule rule, string name) {
            _rule = rule;
            _name = name;
        }

        #region IDictionary
        public void Add(string key, IList<string> value) {
            var lst = Lookup(key);
            foreach (var v in value)
                lst.Add(v);
        }

        public bool ContainsKey(string key) {
            return _rule.HasCompoundProperty(_name, key);
        }

        public ICollection<string> Keys {
            get {
                return _rule.CompoundPropertyKeys(_name).ToList();
            }
        }

        bool IDictionary<string, IList<string>>.Remove(string key) {
            return _rule.RemoveCompoundPropertyValues(_name, key);
        }

        bool IDictionary<string, IList<string>>.TryGetValue(string key, out IList<string> value) {
            value = _rule.HasCompoundProperty(_name, key) ? Lookup(key) : null;
            return value != null;
        }

        ICollection<IList<string>> IDictionary<string, IList<string>>.Values {
            get {
                return Keys.Select(Lookup).ToList();
            }
        }

        public IList<string> this[string key] {
            get {
                return Lookup(key);
            }
            set {
                var lst = Lookup(key);
                lst.Clear();
                foreach (var v in value)
                    lst.Add(v);
            }
        }

        void ICollection<KeyValuePair<string, IList<string>>>.Add(KeyValuePair<string, IList<string>> item) {
            this[item.Key] = item.Value;
        }

        void ICollection<KeyValuePair<string, IList<string>>>.Clear() {
            _rule.ClearProperties(_name);
        }

        bool ICollection<KeyValuePair<string, IList<string>>>.Contains(KeyValuePair<string, IList<string>> item) {
            return _rule.HasCompoundProperty(_name, item.Key) && Lookup(_name).SequenceEqual(item.Value);
        }

        void ICollection<KeyValuePair<string, IList<string>>>.CopyTo(KeyValuePair<string, IList<string>>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public int Count {
            get {
                return Keys.Count;
            }
        }

        bool ICollection<KeyValuePair<string, IList<string>>>.IsReadOnly {
            get {
                return true;
            }
        }

        bool ICollection<KeyValuePair<string, IList<string>>>.Remove(KeyValuePair<string, IList<string>> item) {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, IList<string>>> IEnumerable<KeyValuePair<string, IList<string>>>.GetEnumerator() {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator() {
            return new VirtualEnumerator<IList<string>>(Keys.GetEnumerator(), enumerator => this[(string) enumerator.Current]);
        }

        #endregion

        private IList<string> Lookup(string key) {
            return _rule.CompoundPropertyAsList(_name, key);
        }

        #region IList
        int IList<string>.IndexOf(string item) {
            return Lookup(string.Empty).IndexOf(item);
        }

        void IList<string>.Insert(int index, string item) {
            Lookup(string.Empty).Insert(index, item);
        }

        void IList<string>.RemoveAt(int index) {
            Lookup(string.Empty).RemoveAt(index);
        }

        string IList<string>.this[int index] {
            get {
                return Lookup(string.Empty)[index];
            }
            set {
                Lookup(string.Empty)[index] = value;
            }
        }

        void ICollection<string>.Add(string item) {
            Lookup(string.Empty).Add(item);
        }

        void ICollection<string>.Clear() {
            Lookup(string.Empty).Clear();
        }

        bool ICollection<string>.Contains(string item) {
            return Lookup(string.Empty).Contains(item);
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex) {
            Lookup(string.Empty).CopyTo(array, arrayIndex);
        }

        bool ICollection<string>.IsReadOnly {
            get {
                return Lookup(string.Empty).IsReadOnly;
            }
        }

        bool ICollection<string>.Remove(string item) {
            return Lookup(string.Empty).Remove(item);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator() {
            return Lookup(string.Empty).GetEnumerator();
        }
        #endregion
    }
}