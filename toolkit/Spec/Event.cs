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
    using System;
    using Scripting.Languages.PropertySheet;

    public class Event : PropertySheetItem {
        public EventCondition Condition {
            get {
                return Rule["condition"].As<EventCondition>();
            }
            set {
                Rule.SetSingleValue("condition", value.ToString());
            }
        }

        public int Priority {
            get {
                int p;
                Int32.TryParse(Rule.Id, out p);
                return p;
            }
            set {
                Rule.Id = value.ToString();
            }
        }

        public string Script {
            get {
                return Rule["script"].AsString();
            }
            set {
                Rule.SetSingleValue("script", value);
            }
        }

        public Event(Rule rule): base(rule) {
            
        }
    }
}