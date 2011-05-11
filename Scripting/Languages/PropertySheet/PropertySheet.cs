//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class PropertySheet {
        internal readonly Dictionary<string, Rule> _rules = new Dictionary<string, Rule>();
        
        public IEnumerable<Rule> Rules {
            get { return _rules.Values; }
        }

        public IEnumerable<Rule> this[string selector] {
            get { return from r in _rules.Values where r.Selector == selector select r; }
        }

        public static PropertySheet Parse(string text, string originalFilename) {
            return PropertySheetParser.Parse(text, originalFilename);
        }

        public static PropertySheet Load(string path) {
            return Parse(System.IO.File.ReadAllText(path),path);
        }

        public void Save(string path) {
            System.IO.File.WriteAllText(path, ToString());
        }

        public override string ToString() {
            var result = new StringBuilder();
            foreach( var key in _rules.Keys ) {
                result.Append(key);
                result.Append(" {");
                
                foreach( var property in _rules[key].Properties ) {
                    result.AppendLine();
                    result.Append("    ");
                    result.Append(property.Name);
                    result.Append(":");
                    result.Append(property.Value);
                    result.Append(";");
                }
                result.AppendLine();
                result.Append("}");
                result.AppendLine();
                result.AppendLine();
            }
            return result.ToString();
        }
    }
}
