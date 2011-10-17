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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using Atom;
    using Extensions;

    [XmlRoot(ElementName = "Package", Namespace = "http://coapp.org/atom-package-feed-1.0")]
    public class PackageModel {
        // Elements marked with XmlIgnore won't persist in the package feed as themselves
        // they get persisted as elements in the Atom Format (so that we have a suitable Atom feed to look at)
        
        public PackageModel() {
            XmlSerializer = new XmlSerializer(GetType());
            PackageDetails = new PackageDetails();
        }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public Architecture Architecture { get; set; }

        [XmlAttribute]
        public UInt64 Version { get; set; }

        [XmlAttribute]
        public string PublicKeyToken { get; set; }

        [XmlElement(IsNullable = false)]
        public UInt64 BindingPolicyMinVersion { get; set; }

        [XmlElement(IsNullable = false)]
        public UInt64 BindingPolicyMaxVersion { get; set; }

        [XmlAttribute]
        public string RelativeLocation { get; set; }

        [XmlAttribute]
        public string Filename { get; set; }

        [XmlArray(IsNullable = false)]
        public List<Role> Roles { get; set; }

        [XmlArray(IsNullable = false)]
        public List<Guid> Dependencies { get; set; }

        [XmlArray(IsNullable = false)]
        public List<Feature> Features { get; set; } // must be a canonically recognized feature.

        [XmlArray(IsNullable = false)]
        public List<Feature> RequiredFeatures { get; set; }

        [XmlArray(IsNullable = false)]
        public List<string> PackageFeeds {
            get { return Feeds.IsNullOrEmpty() ? new List<string>() : Feeds.Select(each => each.AbsoluteUri).ToList(); }
            set { Feeds = new List<Uri>(value.Select(each => each.ToUri())); }
        }

        [XmlElement(IsNullable = false, ElementName = "Details")]
        public PackageDetails PackageDetails { get; set; }

        /// <summary>
        /// Guid representing the package.
        /// </summary>
        [XmlIgnore]
        public Guid ProductCode { get { return CanonicalName.CreateGuid(); } }

        [XmlIgnore]
        public string CanonicalName {
            get {
                return "{0}-{1}-{2}-{3}".format(Name, Version.UInt64VersiontoString(), Architecture, PublicKeyToken);
            }
        }

        [XmlIgnore]
        public string CosmeticName {
            get { return "{0}-{1}-{2}".format(Name, Version.UInt64VersiontoString(), Architecture); }
        }

        [XmlIgnore]
        public List<Uri> Locations { get; set; }

        [XmlIgnore]
        public List<Uri> Feeds { get; set; }

        [XmlIgnore]
        public List<CompositionRule> CompositionRules { get; set; }

        [XmlIgnore]
        public XmlSerializer XmlSerializer;

        [XmlAnyElement]
        public object[] UnknownElements;
    }

    [XmlRoot(ElementName = "Details", Namespace = "http://coapp.org/atom-package-feed-1.0")]
    public class PackageDetails {

        // Elements marked with XmlIgnore won't persist in the package feed as themselves
        // they get persisted as elements in the Atom Format (so that we have a suitable Atom feed to look at)
        public PackageDetails() {
            Publisher = new Identity();
            Contributors = new List<Identity>();
        }

        [XmlElement(IsNullable = false)]
        public string AuthorVersion { get; set; }

        [XmlElement(IsNullable = false)]
        public string BugTracker { get; set; }

        [XmlElement(IsNullable = false)]
        public string Icon { get; set; }

        [XmlArray(IsNullable = false)]
        public List<License> Licenses { get; set; }

        [XmlElement(IsNullable = false)]
        public bool IsNsfw { get; set; }

        /// <summary>
        /// -100 = DEATHLY_UNSTABLE ... 0 == release ... +100 = CERTIFIED_NEVER_GONNA_GIVE_YOU_UP.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public sbyte Stability { get; set; }

        [XmlIgnore]
        public string SummaryDescription { get; set; }

        [XmlIgnore]
        public DateTime PublishDate { get; set; }

        [XmlIgnore]
        public Identity Publisher { get; set; }

        [XmlIgnore]
        public List<Identity> Contributors { get; set; }

        [XmlIgnore]
        public string CopyrightStatement { get; set; }

        [XmlIgnore]
        public List<string> Tags { get; set; }

        [XmlIgnore]
        public List<string> Categories { get; set; }

        [XmlIgnore]
        public string Description { get; set; }

#if COAPP_ENGINE_CORE 
        internal string GetAtomItemText(Package package) {
            var item = new AtomItem(package);
            using(var sw = new StringWriter() ) {
                using(var xw = XmlWriter.Create(sw) ) {
                    item.SaveAsAtom10(xw);
                }
                return sw.ToString();
            }
        }
#endif
    }
}