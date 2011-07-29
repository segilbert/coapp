//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MultiplexedProperty<T> : IEnumerable<T> {
        private T _currentValue;
        private bool _lastValueWins;
        private readonly Lazy<List<T>> _values = new Lazy<List<T>>();

        private List<T> ValueList {
            get { return _values.Value; }
        }

        public MultiplexedProperty(bool lastValueWins = true) {
            _lastValueWins = lastValueWins;
        }

        public MultiplexedProperty(EventHandler onChanged, bool lastValueWins = true) {
            OnChanged += onChanged;
            _lastValueWins = lastValueWins;
        }

        public IEnumerable<T> OtherValues {
            get { return _values.Value.Except(new[] {_currentValue}); }
        }

        public bool HasOtherValues {
            get { return (_values.IsValueCreated && _values.Value.Count > 1); }
        }

        public bool ContainsValue(T value) {
            return (_values.IsValueCreated && _values.Value.Contains(value));
        }

        public event EventHandler OnChanged;

        public T Value {
            get { return _currentValue; }
            set {
                Add(value);
                if (_lastValueWins || _currentValue == null) {
                    if (!Equals(_currentValue, value)) {
                        _currentValue = value;
                        OnChanged(this, null);
                    }
                }
            }
        }

        public void Add(T newValue) {
            if (_currentValue == null) {
                _currentValue = newValue;
            }

            if (!ValueList.Contains(newValue)) {
                ValueList.Add(newValue);
                OnChanged(this, null);
            }
        }

        public void Add(IEnumerable<T> values) {
            foreach (var v in values.Where(v => !ValueList.Contains(v))) {
                ValueList.Add(v);
                OnChanged(this, null);
            }
        }

        public static implicit operator T(MultiplexedProperty<T> property) {
            return property.Value;
        }

        public override string ToString() {
            return _currentValue == null ? null: _currentValue.ToString();
        }


        public IEnumerator<T> GetEnumerator() {
            return _values.Value.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _values.Value.GetEnumerator();
        }
    }
}