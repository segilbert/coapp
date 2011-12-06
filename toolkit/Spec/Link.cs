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

    public class Link : PropertySheetItem {
        public Link(Rule rule): base(rule) {
            
        } 

        public IList<string> Libraries {
            get {
                return Rule.PropertyAsList("libraries");
            }
        }

        public string Output {
            get {
                return Rule["output"].AsString();
            }
            set {
                Rule.SetSingleValue("output", value);
            }
        }

        public LinkSubsystem Subsystem {
            get {
                return Rule["subsystem"].As<LinkSubsystem>();
            }
            set {
                Rule.SetSingleValue("subsystem", value.ToString());
            }
        }

        public LinkType Type {
            get {
                return Rule["type"].As<LinkType>();
            }
            set {
                Rule.SetSingleValue("type", value.ToString());
            }
        }

    }
}