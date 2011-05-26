//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    public class RuleProperty {
        private IEnumerable<string> _values;

        public string LValue;
        public string Name;
        public string RValue;
        public string Expression;
        public int SourceRow;
        public int SourceColumn;
        public string SourceFile;
        
        public IEnumerable<string> Values {
            get { return IsCollection ? _values : (IsCompoundProperty ? RValue : LValue).SingleItemAsEnumerable(); }
            set { _values = value; }
        }

        private static string QuoteIfNeeded(string val) {
            if (val.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashesAndDots))
                return val;

            return val.Contains("\r") || val.Contains("\n") || val.Contains("=") || val.Contains("\t")
                ? @"@""{0}""".format(val)
                : @"""{0}""".format(val);
        }

        public string RawValue {
            get {
                if (IsCollection) {
                    return _values.Aggregate(string.IsNullOrEmpty(LValue) ? "{\r\n" : "{0} = {{\r\n".format(QuoteIfNeeded(LValue)), (result, each) => result + "        " + (QuoteIfNeeded(each)) + " ,\r\n") + "    }";
                }

                if (IsCompoundProperty) {
                    return String.Format(@"{0}={1}" , QuoteIfNeeded( LValue), QuoteIfNeeded(RValue));
                }

                if (IsExpression) {
                    return "(" + Expression + ")";
                }

                if (string.IsNullOrEmpty(LValue)) {
                    return "";
                }

                return QuoteIfNeeded(LValue);
            }

            set {
                _values = null;
                Expression = null;

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

        public bool IsExpression {
            get { return Expression != null; }
        }

        public bool IsValue {
            get { return !(IsCollection || IsCompoundProperty || IsExpression); }
        }
    }
}