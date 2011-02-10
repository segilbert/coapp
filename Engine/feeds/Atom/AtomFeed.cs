//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Feeds.Atom {
    using System;
    using System.IO;
    using System.ServiceModel.Syndication;
    using System.Xml;

    public class AtomFeed : SyndicationFeed {
        public AtomFeed() {
            
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
            XmlWriter writer = XmlWriter.Create(File.OpenWrite(localPath));
            SaveAsAtom10(writer);
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