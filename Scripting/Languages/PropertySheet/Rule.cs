//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Rule {
        public string Name = "*";
        public string Class;
        public string Id;
        public string Parameter;
        public int SourceRow;
        public int SourceColumn;
        public string SourceFile;

        public List<RuleProperty> Properties= new List<RuleProperty>(); 

        public IEnumerable<RuleProperty> this[string name] {
            get { return from p in Properties where p.Name == name select p; }
        }

        public void SetSingleValue(string name, string value) {
            var p = this[name];
            if( p.Count() == 1 ) { 
                p.First().RawValue = value;
            } else {
                // remove any that exist
                Properties.RemoveAll(r=> r.Name == name);
                Properties.Add(new RuleProperty {
                    Name = name,
                    RawValue = value
                });
            }
        }

        public IList<string> PropertyAsList(string name) {
            var p = this[name];
            
            if( p.Count() == 1 && p.First().Values is IList<string> ) {
                return (IList<string>)p.First().Values;
            }
            Properties.RemoveAll(r=> r.Name == name);
            var result = new List<string>(p.AsStrings());
            Properties.Add(new RuleProperty {
                    Name = name,
                    Values = result
                });
            return result;
        }

        public bool HasCompoundProperty(string name, string LValue) {
            return (from prp in Properties
                where (prp.IsCompoundCollection || prp.IsCompoundProperty) && prp.Name == name && prp.LValue == LValue
                select prp.Values).Any();
        }

        public IEnumerable<string> CompoundPropertyKeys(string name) {
            return (from p in Properties where p.IsCompoundProperty || p.IsCompoundCollection select p.LValue).Distinct();
        }

        public bool RemoveCompoundPropertyValues(string name, string LValue) {
            return Properties.RemoveAll(each => each.Name == name && each.LValue == LValue) != 0;
        }

        public void ClearProperties(string name) {
            Properties.RemoveAll(each => each.Name == name);
        }

        public IList<string> CompoundPropertyAsList(string name, string LValue) {
            var result = (IList<string>) (from prp in Properties where prp.Name == name && prp.IsCompoundCollection && prp.LValue == LValue && prp.Values is IList<string> select prp.Values).FirstOrDefault();
            if (result == null) {
                var compoundProperties = from prp in Properties where prp.IsCompoundProperty || prp.IsCompoundCollection select prp;

                result = new List<string>(compoundProperties.Where(each => each.LValue == name).SelectMany(each => each.Values));
                Properties.RemoveAll(r => r.Name == name && r.LValue == LValue);

                Properties.Add(new RuleProperty {
                    Name = name,
                    LValue = LValue,
                    Values = result
                });
            }
            return result;
        }


        public string FullSelector {
            get {
                var result = new StringBuilder();
                if (!string.IsNullOrEmpty(Name) && !Name.Equals("*")) {
                    result.Append(Name);
                }

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

        public bool HasProperty( string propertyName ) {
            return this[propertyName].Any();
        }
    }
}
