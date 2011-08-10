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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Creates a property container that has a 'current' value and a collection of 'other' values.
    /// </summary>
    /// <typeparam name="T">Type of the property container</typeparam>
    /// <remarks></remarks>
    public class MultiplexedProperty<T> : IEnumerable<T> {
        /// <summary>
        /// The current value of the property
        /// </summary>
        private T _currentValue;
        /// <summary>
        /// determines if overwriting the property always sets the currentValue
        /// </summary>
        private readonly bool _lastValueWins;
        /// <summary>
        /// The actual container that is backing this property class
        /// </summary>
        private readonly Lazy<List<T>> _values = new Lazy<List<T>>();

        /// <summary>
        /// Gets the collection values as a list.
        /// </summary>
        /// <remarks></remarks>
        private List<T> ValueList {
            get { return _values.Value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplexedProperty&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="lastValueWins">sets the _lastValueWins member (defaults to <c>true</c>).</param>
        /// <remarks></remarks>
        public MultiplexedProperty(bool lastValueWins = true) {
            _lastValueWins = lastValueWins;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplexedProperty&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="onChanged">The on changed event handler.</param>
        /// <param name="lastValueWins">sets the _lastValueWins member (defaults to <c>true</c>).</param>
        /// <remarks></remarks>
        public MultiplexedProperty(EventHandler onChanged, bool lastValueWins = true) {
            OnChanged += onChanged;
            _lastValueWins = lastValueWins;
        }

        /// <summary>
        /// Gets the other values (except for the 'current' value).
        /// </summary>
        /// <remarks></remarks>
        public IEnumerable<T> OtherValues {
            get {
                return _values.Value.Except(new[] {
                    _currentValue
                });
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has additional values other than the 'current' value.
        /// </summary>
        /// <remarks></remarks>
        public bool HasOtherValues {
            get { return (_values.IsValueCreated && _values.Value.Count > 1); }
        }

        /// <summary>
        /// Determines whether the collection contains a given value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> if the collection contains the given value; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public bool ContainsValue(T value) {
            return (_values.IsValueCreated && _values.Value.Contains(value));
        }

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        /// <remarks></remarks>
        public event EventHandler OnChanged;

        /// <summary>
        /// Gets or sets the 'current' value (subject to the _lastValueWins setting).
        /// 
        /// Fires the onChanged event if the current value actually changed.
        /// </summary>
        /// <value>The value.</value>
        /// <remarks></remarks>
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

        /// <summary>
        /// Adds the specified new value to the collection.
        /// 
        /// Sets the 'current' value if no other value is set.
        /// 
        /// Fires the onChanged event if the current value actually changed.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        /// <remarks></remarks>
        public void Add(T newValue) {
            if (_currentValue == null) {
                _currentValue = newValue;
            }

            if (!ValueList.Contains(newValue)) {
                ValueList.Add(newValue);
                OnChanged(this, null);
            }
        }

        /// <summary>
        /// Adds a whole collection of values.
        /// 
        /// Fires the onChanged event for each new value added to the collection.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <remarks></remarks>
        public void Add(IEnumerable<T> values) {
            foreach (var v in values.Where(v => !ValueList.Contains(v))) {
                ValueList.Add(v);
                OnChanged(this, null);
            }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="CoApp.Toolkit.Engine.MultiplexedProperty&lt;T&gt;"/> to <see cref="T"/>.
        /// 
        /// A convienience method to make casting the value easier.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator T(MultiplexedProperty<T> property) {
            return property.Value;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> a string containing the 'current' value
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        /// <remarks></remarks>
        public override string ToString() {
            return _currentValue == null ? null : _currentValue.ToString();
        }


        /// <summary>
        /// Returns an enumerator that iterates through the collection. (implemented to support IEnumerable)
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
        /// <remarks></remarks>
        public IEnumerator<T> GetEnumerator() {
            return _values.Value.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection. (implemented to support IEnumerable)
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
        /// <remarks></remarks>
        IEnumerator IEnumerable.GetEnumerator() {
            return _values.Value.GetEnumerator();
        }
    }
}