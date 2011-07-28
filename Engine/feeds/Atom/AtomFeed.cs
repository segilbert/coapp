//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
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
        private readonly string _outputFilename;
        private readonly string _packageUrlPrefix;
        private readonly string _actualUrl;
        private readonly string _rootUrl;

        public AtomFeed() {
            LastUpdatedTime = DateTime.Now;
        }
        /* 
         output-file=<f>        
         root-url=<url>         
         package-source=<dir>   
         package-url=<url>     
         
         actual-url=<url>       
         recursive				
         title=<title>
         */

        public AtomFeed(string outputFilename, string rootUrl, string packageUrl, string actualUrl = null, string title = null): this() {
            Title = new TextSyndicationContent(title ?? "CoApp Package Feed");
            
            _rootUrl = rootUrl.EndsWith("/") ? rootUrl : rootUrl + "/";
            _actualUrl = actualUrl ?? _rootUrl + Path.GetFileName(outputFilename);

            if( !packageUrl.Contains("://")) {
                packageUrl = _rootUrl + (packageUrl.StartsWith("/") ? packageUrl.Substring(1) : packageUrl);
            }
            _packageUrlPrefix = (packageUrl.EndsWith("/") ? packageUrl : packageUrl+"/");

            var selfLink = CreateLink();
            selfLink.RelationshipType = "self";
            selfLink.MediaType = "application/atom+xml";
            selfLink.Title = "Feed Location";
            selfLink.Uri = new Uri(_actualUrl);
            Links.Add(selfLink);

            Generator = "CoAppEngine";
            _outputFilename = outputFilename;
        }

        public static AtomFeed Load(string localPath) {
            XmlReader reader = null;
            StringReader sr = null;

            if( File.Exists(localPath) ) {
                sr = new StringReader(File.ReadAllText(localPath));
                reader = XmlReader.Create(sr);
            }
            
            if( reader != null) {
                var result = Load<AtomFeed>(reader);
                sr.Dispose();
                return result;
            }
            
            return null;
        }

        public void Save() {
            Save(_outputFilename);
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

        public void AddPackage(Package package, string relativePath = null) {
            var item = CreateItem() as AtomItem;
            item.Populate(package, relativePath, _packageUrlPrefix);

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