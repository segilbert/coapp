//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

/// -----------------------------------------------------------------------
/// Original Code: 
/// (c) 2009 Microsoft Corporation -- All rights reserved
/// This code is licensed under the MS-PL
/// http://www.opensource.org/licenses/ms-pl.html
/// Courtesy of the Open Source Techology Center: http://port25.technet.com
/// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;

    public static class XmlExtensions {
        private static readonly Dictionary<string, XmlDocument> docCache = new Dictionary<string, XmlDocument>();

        /// <summary>
        /// Returns a list of strings for the inner text of a collection of XmlNodes
        /// </summary>
        /// <param name="nodeList"></param>
        /// <returns></returns>
        public static List<string> InnerText(this XmlNodeList nodeList) {
            var result = new List<string>();
            foreach(XmlNode node in nodeList) {
                result.Add(node.InnerText);
            }
            return result;
        }

        public static XmlDocument XmlDoc(this string xmlDoc) {
            if(docCache.ContainsKey(xmlDoc))
                return docCache[xmlDoc];

            var doc = new XmlDocument();
            doc.LoadXml(xmlDoc);
            docCache.Add(xmlDoc, doc);
            return doc;
        }

        public static XmlDocument JsonDoc(this string jsonDoc) {
            if(docCache.ContainsKey(jsonDoc))
                return docCache[jsonDoc];

            var stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(jsonDoc));
            var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

            var doc = new XmlDocument();
            doc.Load(reader);

            docCache.Add(jsonDoc, doc);
            return doc;
        }

        public static string ToJsonString(this XmlDocument xmlDoc) {
            var stream = new MemoryStream();
            var writer = JsonReaderWriterFactory.CreateJsonWriter(stream);
            xmlDoc.WriteTo(writer);
            return ASCIIEncoding.Default.GetString(stream.GetBuffer());
        }

        public static TType JsonDeserialize<TType>(this string jsonText) {
            var stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(jsonText));
            var json = new DataContractJsonSerializer(typeof(TType));
            return (TType)json.ReadObject(stream);
        }

        public static string JsonSerialize(this object obj) {
            var stream = new MemoryStream();
            var json = new DataContractJsonSerializer(obj.GetType());
            json.WriteObject(stream, obj);
            return ASCIIEncoding.Default.GetString(stream.GetBuffer());
        }
        public static XmlNodeList XPath(this XmlDocument doc, string XPathExpression, params object[] args) {
            return doc.SelectNodes(XPathExpression.format(args));
        }

        public static XmlNode XPathSingle(this XmlDocument doc, string XPathExpression, params object[] args) {
            return doc.SelectSingleNode(XPathExpression.format(args));
        }

        public static XmlNodeList XPathContains(this XmlDocument doc, string XPathExpression, string containsText) {
            return doc.XPath(string.Format(@"{0}[contains( translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') , '{1}')]", XPathExpression, containsText));
        }

        public static XmlNodeList XPathExcludes(this XmlDocument doc, string XPathExpression, string containsText) {
            return doc.XPath(string.Format(@"{0}[not(contains( translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') , '{1}'))]", XPathExpression, containsText));
        }

        public static XmlNodeList XPath(this string doc, string XPathExpression, params object[] args) {
            return doc.XmlDoc().XPath(XPathExpression, args);
        }

        public static XmlNode XPathSingle(this string doc, string XPathExpression, params object[] args) {
            return doc.XmlDoc().XPathSingle(XPathExpression, args);
        }

        public static XmlNodeList XPathContains(this string doc, string XPathExpression, string containsText) {
            return doc.XmlDoc().XPathContains(XPathExpression, containsText);
        }

        public static XmlNodeList XPathExcludes(this string doc, string XPathExpression, string containsText) {
            return doc.XmlDoc().XPathExcludes(XPathExpression, containsText);
        }

        public static XmlNodeList XPath(this XmlNode doc, string XPathExpression, params object[] args) {
            return doc.SelectNodes(XPathExpression.format(args));
        }

        public static XmlNode XPathSingle(this XmlNode doc, string XPathExpression, params object[] args) {
            return doc.SelectSingleNode(XPathExpression.format(args));
        }

        public static XmlNodeList XPathContains(this XmlNode doc, string XPathExpression, string containsText) {
            return doc.XPath(string.Format(@"{0}[contains( translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') , '{1}')]", XPathExpression, containsText));
        }

        public static XmlNodeList XPathExcludes(this XmlNode doc, string XPathExpression, string containsText) {
            return doc.XPath(string.Format(@"{0}[not(contains( translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') , '{1}'))]", XPathExpression, containsText));
        }

        public static XmlDocument ToXmlDocument(this XmlNodeList nodeList) {
            var tmpDoc = new StringBuilder();
            tmpDoc.AppendLine("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>\r\n<content>\r\n");

            foreach(XmlNode n in nodeList)
                tmpDoc.Append(n.OuterXml);

            tmpDoc.AppendLine("</content>");
            return tmpDoc.ToString().XmlDoc();
        }

        public static XmlNodeList XPath(this XmlNodeList doc, string XPathExpression, params object[] args) {
            return doc.ToXmlDocument().SelectNodes(XPathExpression.format(args));
        }

        public static XmlNode XPathSingle(this XmlNodeList doc, string XPathExpression, params object[] args) {
            return doc.ToXmlDocument().SelectSingleNode(XPathExpression.format(args));
        }

        public static XmlNodeList XPathContains(this XmlNodeList doc, string XPathExpression, string containsText) {
            return doc.ToXmlDocument().XPath(string.Format(@"{0}[contains( translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') , '{1}')]", XPathExpression, containsText));
        }

        public static XmlNodeList XPathExcludes(this XmlNodeList doc, string XPathExpression, string containsText) {
            return doc.ToXmlDocument().XPath(string.Format(@"{0}[not(contains( translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') , '{1}'))]", XPathExpression, containsText));
        }

        public static XmlNodeList DocumentNodes(this XmlDocument doc) {
            return doc.ChildNodes[1].ChildNodes;
        }

        
    }
}
