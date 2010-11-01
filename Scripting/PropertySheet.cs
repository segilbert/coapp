//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace CoApp.Toolkit.Scripting {
    public class PropertySheet {
        public Dictionary<string, Rule> Rules = new Dictionary<string, Rule>();

        public static PropertySheet Parse(string text) {
            return PropertySheetParser.Parse(text);
        }

        public static PropertySheet Load(string path) {
            return Parse(System.IO.File.ReadAllText(path));
        }

        public void Save(string path) {
            System.IO.File.WriteAllText(path, ToString());
        }

        public override string ToString() {
            var result = new StringBuilder();
            foreach( var key in Rules.Keys ) {
                result.Append(key);
                result.Append(" {");
                
                foreach( var property in Rules[key].Properties ) {
                    result.AppendLine();
                    result.Append("   ");
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
