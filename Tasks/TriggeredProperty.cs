//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A property wrapper that can call a delegate when the value changes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks></remarks>
    public class TriggeredProperty<T> {
        /// <summary>
        /// 
        /// </summary>
        private readonly Func<T, bool> _condition;
        /// <summary>
        /// 
        /// </summary>
        private readonly List<Tuple<Func<T, bool>, Action<T>>> _conditionNotifiers = new List<Tuple<Func<T, bool>, Action<T>>>();

        /// <summary>
        /// 
        /// </summary>
        private Action<T> _notification;
        /// <summary>
        /// 
        /// </summary>
        private T _value;

        /// <summary>
        /// 
        /// </summary>
        public bool TripOnce = true;

        /// <summary>
        /// Gets or sets the notification.
        /// </summary>
        /// <value>The notification.</value>
        /// <remarks></remarks>
        public Action<T> Notification {
            get { return _notification; }
            set {
                if (value == null) {
                    _notification = null;
                    return;
                }

                if( TripOnce && _condition(_value) ) {
                    value(_value);
                    return;
                }

                if (_condition(_value)) {
                    value(_value);
                }

                _notification = value;

            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// 
        /// This will call the notifier when the value changes.
        /// It can be set with a "TripOnce" where the notifcation only gets called once.
        /// </summary>
        /// <value>The value.</value>
        /// <remarks></remarks>
        public T Value {
            get { return _value; }
            set {
                if (_condition != null && Notification != null && _condition(value) ) {
                    Notification(value);
                    if (TripOnce)
                        Notification = null;
                }

                for (var i = _conditionNotifiers.Count - 1; i >= 0; i--) {
                    if (_conditionNotifiers[i].Item1(value)) {
                        _conditionNotifiers[i].Item2(value);
                        if (TripOnce)
                            _conditionNotifiers.RemoveAt(i);
                    }
                }
                _value = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggeredProperty&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="condition">The condition.</param>
        /// <remarks></remarks>
        public TriggeredProperty(T initialValue, Func<T, bool> condition) {
            _value = initialValue;
            _condition = condition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggeredProperty&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <remarks></remarks>
        public TriggeredProperty(Func<T, bool> condition) {
            _condition = condition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggeredProperty&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <remarks></remarks>
        public TriggeredProperty(T initialValue) {
            _value = initialValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        /// <remarks></remarks>
        public TriggeredProperty() {
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="CoApp.Toolkit.Tasks.TriggeredProperty&lt;T&gt;"/> to <see cref="T"/>.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator T(TriggeredProperty<T> v) {
            return v.Value;
        }

        /// <summary>
        /// Called when [trigger].
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="notification">The notification.</param>
        /// <remarks></remarks>
        public void OnTrigger(Func<T, bool> condition, Action<T> notification) {
            if (condition(_value)) {
                notification(_value);
            }
            else {
                _conditionNotifiers.Add(new Tuple<Func<T, bool>, Action<T>>(condition, notification));
            }
        }
    }
}