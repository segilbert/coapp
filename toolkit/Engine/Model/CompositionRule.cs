//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace CoApp.Toolkit.Engine {
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "CompositionRule", Namespace = "http://coapp.org/atom-package-feed-1.0")]
    public class CompositionRule {
        [XmlAttribute]
        public CompositionAction Action { get; set; }

        // AKA "Key"
        [XmlAttribute]
        public string Destination { get; set; }

        // AKA "Value"
        [XmlAttribute]
        public string Source { get; set; }

        [XmlIgnore]
        public string Value { get { return Source; } }

        [XmlIgnore]
        public string Key { get { return Destination; } }

        [XmlAttribute]
        public string Parameters { get; set; }

        [XmlAttribute]
        public string Category { get; set; }
    }
}
