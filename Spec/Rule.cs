//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using CoApp.Toolkit.Scripting;

namespace CoApp.Toolkit.Spec {
    public class Rule {
        public string Selector;
        public string Class;
        public string Id;
        public string Parameter;
        public List<RuleProperty> Properties= new List<RuleProperty>(); 

        public string FullSelector {
            get {
                var result = new StringBuilder(); 
                result.Append( string.IsNullOrEmpty(Selector) ? "*" : Selector);
                
                if(!string.IsNullOrEmpty(Parameter)) {
                    result.Append("[");
                    result.Append(Parameter);
                    result.Append("]");
                }

                if(!string.IsNullOrEmpty(Class)) {
                    result.Append(".");
                    result.Append(Class);
                }

                if(!string.IsNullOrEmpty(Id)) {
                    result.Append("#");
                    result.Append(Id);
                }

                return result.ToString();
            }
        }
    }
}
