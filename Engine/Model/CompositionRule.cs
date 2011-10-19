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
        [XmlElement(IsNullable = false)]
        public CompositionAction Action { get; set; }

        // AKA "Key"
        [XmlElement(IsNullable = false)]
        public string Link { get; set; }

        // AKA "Value"
        [XmlElement(IsNullable = false)]
        public string Target { get; set; }

        [XmlElement(IsNullable = false)]
        public string Parameters { get; set; }

        [XmlElement(IsNullable = false)]
        public string Category { get; set; }
    }
}
