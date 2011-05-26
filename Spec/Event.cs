//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
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