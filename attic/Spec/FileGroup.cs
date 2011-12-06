//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using System.Collections.Generic;
    using Scripting.Languages.PropertySheet;

    public class FileGroup : PropertySheetItem {
        public readonly IList<string> Files;

        public FileGroup(Rule rule) : base(rule) {
            Files = new DictionaryProperty(Rule, "files");
        }
    }
}