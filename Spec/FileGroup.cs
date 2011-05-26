//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using Scripting.Languages.PropertySheet;

    public class FileGroup : PropertySheetItem {
        public readonly DictionaryProperty Files;

        public FileGroup(Rule rule) : base(rule) {
            Files = new DictionaryProperty(Rule, "files");
        }
    }
}