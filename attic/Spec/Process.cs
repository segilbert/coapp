//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Properties;
    using Scripting.Languages.PropertySheet;
    using mkSpec.Tool.Properties;
    using Toolkit.Extensions;

    public class Process : PropertySheetItem {
        /*
        public DictionaryProperty Defines;

        public IList<string> IncludeDirectory {
            get {
                return Rule.PropertyAsList("include-directory");
            }
        }

        public bool ReadOnlyStringPooling {
            get {
                return Rule["read-only-string-pooling"].AsBoolean();
            }
            set {
                Rule.SetSingleValue("read-only-string-pooling", value.ToString());
            }
        }

        public ProcessCompileAs CompileAs {
            get {
                return Rule["compile-as"].As<ProcessCompileAs>();
            }
            set {
                Rule.SetSingleValue("compile-as", value.ToString());
            }
        }
         * 
         *     Defines = new DictionaryProperty(Rule, "define");
        */


        public ProcessProperties ProcessProperties;

        public Process(Rule rule): base(rule) {
        
        }

        // Since we're storing the values in the ProcessProperties, we're gonna
        // push the values into the propertysheet at save time
        public void Save() {
            if (ProcessProperties != null) {


                foreach (var p in ProcessProperties.GetType().GetProperties(BindingFlags.Public)) {
                    var propertyValue = p.GetValue(this, null);
                    var propertyName = p.Name;

                    var customAttribute = p.GetCustomAttributes(typeof (PropertySheetAttribute), true);

                    var propertySheetAttribute = customAttribute.GetValue(0) as PropertySheetAttribute;
                    if (propertySheetAttribute != null) {
                        propertyName = propertySheetAttribute.PropertyName;
                    }
                    Rule.ClearProperties(propertyName);

                    if (propertyValue != null) {
                        var propertyType = p.PropertyType;
                        if (propertyType == typeof (List<string>)) {
                            Rule.Properties.Add(new RuleProperty {
                                Name = propertyName,
                                Values = propertyValue as List<string>
                            });
                        }
                        else if (propertyType == typeof (List<int>)) {
                            Rule.Properties.Add(new RuleProperty {
                                Name = propertyName,
                                Values = (propertyValue as List<int>).Select(v => v.ToString()).ToList()
                            });
                        }
                        else if (propertyType == typeof (Dictionary<int, int>)) {
                            var dic = propertyValue as Dictionary<int, int>;
                            foreach (var key in dic.Keys) {
                                Rule.Properties.Add(new RuleProperty {
                                    Name = propertyName,
                                    LValue = key.ToString(),
                                    RValue = dic[key].ToString()
                                });
                            }
                        }
                        else {
                            // better support tostring!
                            Rule.SetSingleValue(propertyName, propertyValue.ToString());
                        }
                    }
                }
            }
        }

        // Since we're storing the values in the ProcessProperties, we're gonna
        // pull the values into the ProcessProperties when its loaded.
        public void Load() {
            foreach (var p in ProcessProperties.GetType().GetProperties(BindingFlags.Public)) {
                var propertyName = p.Name;
                var customAttribute = p.GetCustomAttributes(typeof (PropertySheetAttribute), true);
                var propertySheetAttribute = customAttribute.GetValue(0) as PropertySheetAttribute;
                if (propertySheetAttribute != null) {
                    propertyName = propertySheetAttribute.PropertyName;
                }

                if( Rule.HasProperty(propertyName) ) {
                    var propertyType = p.PropertyType;
                    if( propertyType == typeof(string)) {
                        p.SetValue(this, Rule[propertyName].AsString(), null );
                    }
                    else if (propertyType == typeof(int?)) {
                        p.SetValue(this, Rule[propertyName].AsInt(), null );
                    }
                    else if (propertyType == typeof(bool?)) {
                        p.SetValue(this, Rule[propertyName].AsBoolean(), null );
                    }
                    else if (propertyType == typeof(List<string>)) {
                        p.SetValue(this, Rule[propertyName].AsStrings(), null );
                    }
                    else if (propertyType == typeof(List<int>)) {
                        p.SetValue(this, Rule[propertyName].AsStrings().Select(s => s.ToInt32()).ToList(), null );
                    }
                    else if (propertyType == typeof(Dictionary<int,int>)) {
                        var dic = Rule[propertyName].AsDictionary(false);
                        var newdic = new Dictionary<int, int>();

                        foreach(var key in dic.Keys) {
                            try {
                                newdic.Add(key.ToInt32(), dic[key].FirstOrDefault().ToInt32());
                            } catch {
                            }
                        }

                        p.SetValue(this, newdic,null);
                    }
                }
            }
        }
    }
}