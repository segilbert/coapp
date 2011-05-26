//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using Scripting.Languages.PropertySheet;

    public class Library : PropertySheetItem {
        public Library(Rule rule): base(rule) {
            
        } 

        public string File{
            get {
                return Rule["file"].AsString();
            }
            set {
                Rule.SetSingleValue("file", value);
            }
        }

        public string Version{
            get {
                return Rule["version"].AsString();
            }
            set {
                Rule.SetSingleValue("version", value);
            }
        }
    }
}