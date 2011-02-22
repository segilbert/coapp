//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds.Atom {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Xml;
    using Extensions;

    public class AtomFeed : SyndicationFeed {
        private readonly List<AtomItem> _items = new List<AtomItem>();

        public AtomFeed() {
            Title = new TextSyndicationContent( "CoApp Package Feed" );
            LastUpdatedTime = DateTime.Now;
        }

        public void Populate() {
            var link = CreateLink();
            link.RelationshipType = "self";
            link.MediaType = "application/atom+xml";
            link.Title = "Feed Location";
            link.Uri = new Uri("http://localhost:8080/atom.xml");
            Links.Add(link);

            Generator = "CoAppEngine";
        }

        public static AtomFeed Load(string localPath) {
            XmlReader reader = null;

            if( File.Exists(localPath) ) {
                reader = XmlReader.Create(File.OpenText(localPath));
            }

            if( reader != null) {
                return Load<AtomFeed>(reader);
            }

            return null;
        }

        public static AtomFeed Load(Uri uri) {
            return null;
        }

        public void Save(string localPath ) {
            using (var ms = new MemoryStream()) {
                var writer = XmlWriter.Create(ms);
                var formatter = GetAtom10Formatter();
                formatter.PreserveAttributeExtensions = true;
                formatter.WriteTo(writer);
                writer.WriteEndDocument();
                writer.Close();
                ms.PrettySaveXml(localPath);
            }
        }

        public void AddPackages(IEnumerable<Package>packages) {
            foreach (var p in packages)
                AddPackage(p);
        }

        public void AddPackage(Package package) {
            var item = CreateItem() as AtomItem;
            item.Populate(package);

            // TODO: set lastupdatedtime to the last package timestamp?
            _items.Add(item);
            Items = _items;
        }

        protected override void WriteAttributeExtensions(XmlWriter writer, string version) {
            writer.WriteAttributeString("xmlns", "package", null, "http://coapp.org/atom-package-feed-1.0");
        }
        protected override SyndicationCategory CreateCategory() {
            return base.CreateCategory();
        }

        protected override SyndicationLink CreateLink() {
            return base.CreateLink();
        }

        protected override SyndicationPerson CreatePerson() {
            return base.CreatePerson();
        }

        protected override SyndicationItem CreateItem() {
            return new AtomItem();
        }
    }
}