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
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    /*
    public class RuleProperty {
        private List<string> _values;
        // each expression is supposed to be like
        // foo => lvalue = rvalue;
        private IEnumerable<Tuple<string, string, string>> _expressions;
        
        public string LValue { get; set; }
        public string Name { get; set; }
        public string RValue { get; set; }

        public int SourceRow;
        public int SourceColumn;
        public string SourceFile;
        
        public IEnumerable<string> Values {
            get { return IsCollection ? _values : (IsCompoundProperty ? RValue : LValue).SingleItemAsEnumerable(); }
            set {
                if( _values == null ) {
                    _values = new List<string>();
                }
                _values.Clear();
                _values.AddRange(value);
            }
        }

        private static string QuoteIfNeeded(string val) {
            if (val.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashesAndDots))
                return val;

            return val.Contains("\r") || val.Contains("\n") || val.Contains("=") || val.Contains("\t")
                ? @"@""{0}""".format(val)
                : @"""{0}""".format(val);
        }

        

        internal string RawValue {
            get {
                if (IsCollection) {
                    return _values.Aggregate(string.IsNullOrEmpty(LValue) ? "{\r\n" : "{0} = {{\r\n".format(QuoteIfNeeded(LValue)), (result, each) => result + "        " + (QuoteIfNeeded(each)) + " ,\r\n") + "    }";
                }

                if (IsCompoundProperty) {
                    return String.Format(@"{0}={1}" , QuoteIfNeeded( LValue), QuoteIfNeeded(RValue));
                }

                if (string.IsNullOrEmpty(LValue)) {
                    return "";
                }

                return QuoteIfNeeded(LValue);
            }

            set {
                _values = null;

                if (!(value.Contains("\r") || value.Contains("\n"))) {
                    var p = value.IndexOf('=');
                    if (p != -1) {
                        LValue = value.Substring(0, p).Trim();
                        RValue = value.Substring(p + 1).Trim();
                        return;
                    }
                }

                LValue = value;
                RValue = null;
            }
        }

        public bool IsCompoundProperty {
            get { return !string.IsNullOrEmpty(RValue); }
        }

        public bool IsCompoundCollection {
            get { return !string.IsNullOrEmpty(LValue) & IsCollection; }
        }

        public bool IsCollection {
            get { return _values != null; }
        }

        public bool IsValue {
            get { return !(IsCollection || IsCompoundProperty); }
        }
    } */
}