//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using CoApp.Toolkit.Extensions;

namespace CoApp.Toolkit.Spec {
    public class RuleProperty {
        public string Name;

        public string Value { 
            get {
                if(string.IsNullOrEmpty(LValue))
                    return "";

                if(IsCompoundRule) {
                    if(RValue.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashes))
                        return String.Format(@"{0}={1}",LValue, RValue);

                    return String.Format(@"{0}=""{1}""", LValue, RValue);
                }
                if(!LValue.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashes) )
                    return String.Format(@"""{0}""", LValue );
                
                return LValue;
            }

            set {
                int p = value.IndexOf('=');
                if( p != -1 ) {
                    LValue = value.Substring(0, p - 1).Trim();
                    RValue = value.Substring(p + 1).Trim();
                    return;
                } 

                LValue = value;
                RValue = null;
            }
        }

        public string LValue;
        public string RValue;

        public bool IsCompoundRule { get { return !string.IsNullOrEmpty(RValue); }}
    }
}
