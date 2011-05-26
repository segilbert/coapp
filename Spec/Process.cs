//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using System.Collections.Generic;
    using Scripting.Languages.PropertySheet;

    public class Process : PropertySheetItem {
        public DictionaryProperty Defines;

        public IList<string> IncludeDirectory {
            get {
                return Rule.PropertyAsList("include-directory");
            }
        }

        public bool ReadOnlyStringPooling {
            get {
                return Rule["read-only-string-pooling"].AsBoolean();
            }
            set {
                Rule.SetSingleValue("read-only-string-pooling", value.ToString());
            }
        }

        public ProcessCompileAs CompileAs {
            get {
                return Rule["compile-as"].As<ProcessCompileAs>();
            }
            set {
                Rule.SetSingleValue("compile-as", value.ToString());
            }
        }

        public Process(Rule rule): base(rule) {
            Defines = new DictionaryProperty(Rule, "define");
        }
    }
}