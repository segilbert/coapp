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
    using System.Xml.Serialization;

    public class AtomItem : SyndicationItem {
        private static XmlSerializer _xmlSerializer = new XmlSerializer(typeof(PackageElement));
        internal PackageElement packageElement = new PackageElement();

        /*
    <entry>
    <id>PKG-GUID</id>
    <title type="text">COSMETIC-NAME</title>
    <summary>ONE-LINE-DESCRIPTION(TXT)</summary>
    <published>PACKAGE-DATE</published>
         
    <author>
      <name>PUBLISHER-COMMON-NAME</name>
      <uri>PUBLISHER-VANITY-URL</uri>
      <email>foo@example.com</email>
    </author>
         
    <contributor>  <!-- may have multiple tags -->
        <name>ORIGINAL-DEVELOPER</name>
        <uri>ORIGINAL-DEVELOPER-URL</uri>
        <email>foo@somewhereelse.com</email>
    </contributor>
     
    <rights>AUTHOR-SUPPLIED-COPYRIGHT-STATEMENT</rights> <!-- NOT LICENSE TEXT-->
    <link rel="enclosure" href="http://foo/bar.msi" />
    <link rel="related" title="sourcepackage" href="http://foo/bar-src.msi"/>

    <category term="COSMETIC-TAG" /> <!-- may have multiple tags -->
    <content type="text/html">
         HTML-DESCRIPTION-TEXT *     <!-- this should be limited in supported HTML tags -->
    </content>
         
    <package:Package xmlns:package="http://coapp.org/atom-package-feed">
        <package:Id>PKG-GUID</package:Id>
        <package:Name>PROPER-NAME</package:Name> <!-- name-version-platform -->
        <package:Architecture>x86</package:Architecture>
        <package:Version>281474976710656</package:Version>
        <package:PublicKeyToken>1231231231231231</package:PublicKeyToken>
        <package:BindingPolicyMinVersion>0</package:BindingPolicyMinVersion>
        <package:BindingPolicyMaxVersion>0</package:BindingPolicyMaxVersion>
        <package:Dependencies>COMMA-SEPERATED-GUIDS</package:Dependencies>
        <package:Locations>
            <package:string>./packages/foo.msi</package:string>
            <package:string>http://some-other-place.com/foo.msi</package:string>
        </package:Locations>
        DUPLICATE, OMIT: <package:Description>HTML-DESCRIPTION-TEXT</package:Description>
        <package:Icon>BASE-64-ENCODED-IMAGE(PNG, 256x256)</package:Icon>
        <!-- soon: potential package metadata tags:
         *  license: license text (or license URL?)
         *  etc...
         * -->
    </package:Package>
  </entry> 
         */

        [XmlRoot(ElementName = "Package", Namespace = "http://coapp.org/atom-package-feed-1.0")]
        public class PackageElement  {
            public string Id;
            public string Name;
            public string Architecture;
            public UInt64 Version;
            public string PublicKeyToken;
            public UInt64 BindingPolicyMinVersion;
            public UInt64 BindingPolicyMaxVersion;
            public string Icon;
            public string RelativeLocation;
            public string Filename;

            public string[] Locations;
            public string[] Dependencies;
        }

        public AtomItem() {
            
        }

        protected override bool TryParseElement(System.Xml.XmlReader reader, string version) {
            var extension = _xmlSerializer.Deserialize(reader);
            packageElement = extension as PackageElement;
            return packageElement != null;
        }


        internal void Populate(Package package, string relativeLocation, string packageUrlPrefix) {
            Id = package.ProductCode;
            Title = new TextSyndicationContent(package.CosmeticName);
            Summary = new TextSyndicationContent(package.SummaryDescription);
            PublishDate = package.PublishDate; 

            var author = CreatePerson();
            author.Name = package.Publisher.Name;
            author.Email = package.Publisher.Email;
            author.Uri = package.Publisher.Url;
            Authors.Add(author);

            if (package.Contributors != null) {
                foreach (var contrib in package.Contributors.Select(contributor => CreatePerson())) {
                    contrib.Name = package.Publisher.Name;
                    contrib.Email = package.Publisher.Email;
                    contrib.Uri = package.Publisher.Url;
                    Contributors.Add(contrib);
                }
            }

            if( package.CopyrightStatement != null )
                Copyright = new TextSyndicationContent(package.CopyrightStatement);

            if (package.Tags != null) {
                foreach (var tag in package.Tags) {
                    Categories.Add(new SyndicationCategory(tag));
                }
            }

            if( package.FullDescription != null ) {
                Content = SyndicationContent.CreateHtmlContent(package.FullDescription);
            }

            packageElement.Id = package.ProductCode;
            packageElement.Architecture = package.Architecture;
            packageElement.Icon = string.Empty;
            packageElement.Name = package.Name;
            packageElement.Version = package.Version;
            packageElement.BindingPolicyMaxVersion = package.PolicyMaximumVersion;
            packageElement.BindingPolicyMinVersion = package.PolicyMinimumVersion;
            packageElement.PublicKeyToken = package.PublicKeyToken;
            packageElement.Dependencies = package.Dependencies.Select(p => p.CanonicalName).ToArray();
            packageElement.RelativeLocation = relativeLocation;
            packageElement.Filename = Path.GetFileName(package.LocalPackagePath);

            var link = CreateLink();

            link.Uri = new Uri(new Uri(packageUrlPrefix), packageElement.Filename);
            Links.Add(link);

            link = CreateLink();
            link.RelationshipType = "enclosure";
            link.MediaType = "application/package";
            link.Uri = new Uri(new Uri(packageUrlPrefix), packageElement.Filename);
            link.Title = package.Name;
            Links.Add(link);

            ElementExtensions.Add(packageElement, _xmlSerializer);
        }
    }
}
