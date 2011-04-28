//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Spec;

    public class Rule {
        public string Selector;
        public string Class;
        public string Id;
        public string Parameter;
        public List<RuleProperty> Properties= new List<RuleProperty>(); 

        public IEnumerable<RuleProperty> this[string name] {
            get { return from p in Properties where p.Name == name select p; }
        }

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
