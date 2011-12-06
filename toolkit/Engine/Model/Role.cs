//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Model {
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "Role", Namespace = "http://coapp.org/atom-package-feed-1.0")]
    public class Role {
        [XmlElement(IsNullable = false)]
        public string Name { get; set; }
        [XmlElement(IsNullable = false)]
        public PackageRole PackageRole { get; set; }
    }

    [XmlRoot(ElementName = "Feature", Namespace = "http://coapp.org/atom-package-feed-1.0")]
    public class Feature {
        [XmlElement(IsNullable = false)]
        public string Name { get; set; }

        [XmlElement(IsNullable = false)]
        public string VersionInfo { get; set; }

    }


}