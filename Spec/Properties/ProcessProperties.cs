//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec.Properties {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Collections;
    using Extensions;

    /// <summary>
    /// classes which inherit from this must abide by the following rule:
    ///  - public properties must be nullable
    ///  - public collections must be instantiated with an empty collection, and must be either a dictionary or a list 
    /// </summary>
    public class ProcessProperties {

        

        private static EasyDictionary<string, Dictionary<string, PropertyInfo>> _propertyInfo = new EasyDictionary<string, Dictionary<string, PropertyInfo>>();
        
        [PropertySheetIgnore]
        private Dictionary<string, PropertyInfo> PropertyInfo {
            get {
                var className = GetType().FullName;
                var propertyInfo = _propertyInfo[className];

                if (!propertyInfo.Any()) {
                    // first run, lets grab the properties.
                    foreach (var p in GetType().GetProperties(BindingFlags.Public)) {
                        var customAttributes = p.GetCustomAttributes(typeof (PropertySheetAttribute), true);
                        if (customAttributes.Where(attr => attr as PropertySheetIgnoreAttribute != null).Any()) {
                            continue;
                        }
                        var propertyNameAttribute = customAttributes.Where(attr => attr as PropertySheetAttribute != null).FirstOrDefault();
                        var name = propertyNameAttribute == null ? p.Name : (propertyNameAttribute as PropertySheetAttribute).PropertyName;
                        propertyInfo.Add(name, p);
                    }
                }
                return propertyInfo;
            }
        }

        [PropertySheetIgnore]
        public IEnumerable<string> PropertyNames {
            get {
                return PropertyInfo.Keys;
            }
        }
       
       
        [PropertySheetIgnore]
        public object this[string propertyName] {
            get {
                var property = PropertyInfo[propertyName];
                return property != null ? property.GetValue(this, null) : null;
            }
            set {
                var property = PropertyInfo[propertyName];
                if (property != null) {

                    if (value == null) {
                        property.SetValue(this, null, null);
                        return;
                    }

                    if (property.PropertyType.IsAssignableFrom(value.GetType())) {
                        property.SetValue(this, value, null);
                        return;
                    }

                    if( property.PropertyType == typeof(string)) {
                        property.SetValue(this, value.ToString(),null);
                        return;
                    }

                    if( property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?)) {
                        property.SetValue(this, value.ToString().Equals(Boolean.TrueString, StringComparison.CurrentCultureIgnoreCase),null);
                        return;
                    }

                    if( property.PropertyType == typeof(int) || property.PropertyType == typeof(int?)) {
                        property.SetValue(this, value.ToString().ToInt32(),null);
                        return;
                    }

                    throw new Exception("Should Expand this method to support the type you are using.");
                }
            }
        }

        public bool? NotSpecifiedOnCommandLine { get; set; } 
        public bool? NotDiscoveredInTrace { get; set; }

    }
}