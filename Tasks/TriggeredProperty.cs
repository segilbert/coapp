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

    public class TriggeredProperty<T> {
        private readonly Func<T, bool> _condition;
        private readonly List<Tuple<Func<T, bool>, Action<T>>> _conditionNotifiers = new List<Tuple<Func<T, bool>, Action<T>>>();

        private Action<T> _notification;
        private T _value;

        public bool TripOnce = true;

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

        public TriggeredProperty(T initialValue, Func<T, bool> condition) {
            _value = initialValue;
            _condition = condition;
        }

        public TriggeredProperty(Func<T, bool> condition) {
            _condition = condition;
        }

        public TriggeredProperty(T initialValue) {
            _value = initialValue;
        }

        public TriggeredProperty() {
        }

        public static implicit operator T(TriggeredProperty<T> v) {
            return v.Value;
        }

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