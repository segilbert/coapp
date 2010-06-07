//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    ///   Represents a dynamic interface to an XML Node. 
    ///   This allows loosey-goosey access to XML Documents 
    ///   using c# 4.0 and the dynamic keyword
    /// </summary>
    public class DynamicNode : DynamicObject {
        /// <summary>
        ///   The XML Node this object is fronting for.
        /// </summary>
        private readonly XElement node;

        /// <summary>
        ///   The object representing the XML Attributes for the node. Created on demand only.
        /// </summary>
        private DynamicAttributes attributes;

        /// <summary>
        ///   Creates a DynamicXmlNode from an XElement
        /// </summary>
        /// <param name = "node">An XElement node to use as the actual XML node for this DynamicXmlNode</param>
        public DynamicNode(XElement node) {
            this.node = node;
        }

        /// <summary>
        ///   Creates a DynamicXmlNode From an new XElement with the given name for the node
        /// </summary>
        /// <param name = "name">The new XElement node name to use as the actual XML node for this DynamicXmlNode</param>
        public DynamicNode(string name) {
            node = new XElement(name);
        }

        /// <summary>
        ///   Returns the number of descendent nodes
        /// </summary>
        public int Count {
            get { return node.DescendantNodes().Count(); }
        }

        /// <summary>
        ///   Returns the actual XElement node
        /// </summary>
        public XElement Node {
            get { return node; }
        }

        /// <summary>
        ///   Provides an indexer for the decendent child nodes.
        /// </summary>
        /// <param name = "index">the index of the node requested</param>
        /// <returns></returns>
        public DynamicNode this[int index] {
            get { return new DynamicNode(node.Descendants().ElementAt(index)); }
        }

        /// <summary>
        ///   Provides the implementation for operations that set member values. Classes derived from the DynamicObject class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name = "binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the DynamicObject class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name = "value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the DynamicObject class, the value is "Test".</param>
        /// <returns>True, if successful</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            var setNode = node.Element(binder.Name);
            if(setNode != null) {
                setNode.SetValue(value);
            }
            else {
                node.Add(value.GetType() == typeof(DynamicNode) ? new XElement(binder.Name) : new XElement(binder.Name, value));
            }

            return true;
        }

        /// <summary>
        ///   Provides the implementation for operations that get member values. Classes derived from the DynamicObject class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        ///   Provides a special case for XML Attributes. If the Member name requested is "Attributes", this will return an DynamicXmlAttributes object
        /// </summary>
        /// <param name = "binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the DynamicObject class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name = "result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to result.</param>
        /// <returns>True if successful</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if(binder.Name == "Attributes") {
                return TryGetAttributes(out result);
            }

            var getNode = node.Element(binder.Name);

            if(getNode != null) {
                result = new DynamicNode(getNode);
                return true;
            }

            getNode = new XElement(binder.Name);
            node.Add(getNode);
            result = new DynamicNode(getNode);
            return true;
        }

        /// <summary>
        ///   Gets the dynamic attributes for an XML Node
        /// </summary>
        /// <param name = "result">The Attributes object</param>
        /// <returns>true if successful</returns>
        private bool TryGetAttributes(out object result) {
            if(attributes == null) {
                attributes = new DynamicAttributes(node);
            }

            result = attributes;
            return true;
        }

        /// <summary>
        ///   Some sort of casting thing.
        /// </summary>
        /// <param name = "binder">the member</param>
        /// <param name = "result">the result</param>
        /// <returns>True if succesful</returns>
        public override bool TryConvert(ConvertBinder binder, out object result) {
            if(binder.Type == typeof(string)) {
                result = node.Value;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        ///   Passes thru function calls to the XElement node when there is no matching function in this class.
        /// </summary>
        /// <param name = "binder">Method to call</param>
        /// <param name = "args">Arguments</param>
        /// <param name = "result">Result from function</param>
        /// <returns>True if successful</returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            var xmlType = typeof(XElement);
            try {
                result = xmlType.InvokeMember(binder.Name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, node, args);
                return true;
            }
            catch {
                result = null;
                return false;
            }
        }

        /// <summary>
        ///   Returns the XML Text for the node.
        /// </summary>
        /// <returns>The XML Text</returns>
        public override string ToString() {
            return node.ToString();
        }

        /// <summary>
        ///   Adds a new child node
        /// </summary>
        /// <param name = "name">the new node name</param>
        /// <returns>the DynamicXmlNode for the new node</returns>
        public DynamicNode Add(string name) {
            var e = new XElement(name);
            node.Add(e);
            return new DynamicNode(e);
        }
    }
}