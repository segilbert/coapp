//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Engine.Feeds.Atom {
    using System.ServiceModel.Syndication;

    public class AtomItem : SyndicationItem  {
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
        <package:Description>HTML-DESCRIPTION-TEXT</package:Description>
        <package:Icon>BASE-64-ENCODED-IMAGE(PNG, 256x256)</package:Icon>
        <!-- soon: potential package metadata tags:
         *  license: license text (or license URL?)
         *  etc...
         * -->
    </package:Package>
  </entry> 
         */

        public AtomItem() {
            
        }

        



    }
}
