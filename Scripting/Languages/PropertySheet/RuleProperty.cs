//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Extensions;

    public class RuleProperty {
        private IEnumerable<string> _values;

        public string LValue;
        public string Name;
        public string RValue;
        public string Expression;
        public IEnumerable<string> Values { 
            get { return IsCollection ? _values : Value.SingleItemAsEnumerable(); }
            set { _values = value; }
        }

        private static string QuoteIfNeeded(string val) {
            if (val.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashesAndDots))
                return val;

            return val.Contains("\r") || val.Contains("\n") || val.Contains("=") || val.Contains("\t")
                ? @"@""{0}""".format(val)
                : @"""{0}""".format(val);
        }

        public string Value {
            get {
                if (IsCollection) {
                    return _values.Aggregate(string.IsNullOrEmpty(LValue) ? "{\r\n" : "{0} = {{\r\n".format(QuoteIfNeeded(LValue)), (result, each) => result + "        " + (QuoteIfNeeded(each)) + " ,\r\n") + "    }";
                }

                if (IsCompoundRule) {
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
                var p = value.IndexOf('=');
                if (p != -1) {
                    LValue = value.Substring(0, p ).Trim();
                    RValue = value.Substring(p + 1).Trim();
                    return;
                }

                LValue = value;
                RValue = null;
            }
        }

        public bool IsCompoundRule {
            get { return !string.IsNullOrEmpty(RValue); }
        }

        public bool IsCollection {
            get { return _values != null; }
        }

        public bool IsExpression {
            get { return Expression != null; }
        }

        public bool IsValue {
            get { return !(IsCollection || IsCompoundRule || IsExpression); }
        }
    }
}