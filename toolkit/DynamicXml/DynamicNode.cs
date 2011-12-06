//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    ///   Represents a dynamic interface to an XML Node. 
    ///   This allows loosey-goosey access to XML Documents 
    ///   using c# 4.0 and the dynamic keyword
    /// </summary>
    public class DynamicNode : DynamicObject , IEnumerable<DynamicNode> {
        /// <summary>
        ///   The XML Node this object is fronting for.
        /// </summary>
        protected readonly XElement _element;

        /// <summary>
        ///   The object representing the XML Attributes for the node. Created on demand only.
        /// </summary>
        protected DynamicAttributes attributes;

        public DynamicNode(XDocument document) {
            _element = document.Root;
            
        }

        /// <summary>
        ///   Creates a DynamicXmlNode from an XElement
        /// </summary>
        /// <param name = "element">An XElement node to use as the actual XML node for this DynamicXmlNode</param>
        public DynamicNode(XElement element) {
            _element = element;
        }

        /// <summary>
        ///   Creates a DynamicXmlNode From an new XElement with the given name for the node
        /// </summary>
        /// <param name = "elementName">The new XElement node name to use as the actual XML node for this DynamicXmlNode</param>
        public DynamicNode(string elementName) {
            _element = new XElement(elementName);
        }

        /// <summary>
        ///   Returns the number of descendent nodes
        /// </summary>
        public int Count {
            get { return _element.DescendantNodes().Count(); }
        }

        /// <summary>
        ///   Returns the actual XElement node
        /// </summary>
        public XElement Element {
            get { return _element; }
        }

        /// <summary>
        ///   Provides an indexer for the decendent child nodes.
        /// </summary>
        /// <param name = "index">the index of the node requested</param>
        /// <returns></returns>
        public DynamicNode this[int index] {
            get { return new DynamicNode(_element.Descendants().ElementAt(index)); }
        }

        /// <summary>
        /// Provides an indexer to get a child element by it's an attribute value 
        /// 
        /// 
        /// </summary>
        /// <param name="query">must be in the form of "attributename=attributevalue"
        /// so to find the child element with the attribute 'Name' of 'foo'
        /// value should be : 
        ///      "Name=foo"
        /// 
        /// If no '=' is in the string, it defaults to assuming that the attribute is 'id'
        /// and the value is the whole query parameter.
        /// 
        /// If there are multiple '=' characters, value ends up being the content after the last '='
        /// </param>
        /// <returns></returns>
        public DynamicNode this[string query] {
            get {
                var p = query.Split('=');
                var attr = "id";
                if (p.Length > 1 ) {
                    attr = p[0];
                }
                var value = p[p.Length - 1] ;

                var match = _element.Descendants().Where(each => each.Attributes().Where(a => a.Name == attr && a.Value == value).Any()).FirstOrDefault();

                return match == null ? null : new DynamicNode(match);
            }
        }

        /// <summary>
        ///   Provides the implementation for operations that set member values. Classes derived from the DynamicObject class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name = "binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the DynamicObject class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name = "value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the DynamicObject class, the value is "Test".</param>
        /// <returns>True, if successful</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            var setNode = _element.Element(binder.Name);
            if(setNode != null) {
                setNode.SetValue(value);
            }
            else {
                _element.Add(value.GetType() == typeof(DynamicNode) ? new XElement(binder.Name) : new XElement(binder.Name, value));
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
            var getNode = _element.Element(binder.Name);

            if(getNode != null) {
                result = new DynamicNode(getNode);
                return true;
            }

            getNode = new XElement(binder.Name);
            _element.Add(getNode);
            result = new DynamicNode(getNode);
            return true;
        }

        public dynamic Attributes {
            get { return attributes ?? (attributes = new DynamicAttributes(_element)); }
        }

       

        /// <summary>
        ///   Some sort of casting thing.
        /// </summary>
        /// <param name = "binder">the member</param>
        /// <param name = "result">the result</param>
        /// <returns>True if succesful</returns>
        public override bool TryConvert(ConvertBinder binder, out object result) {
            if(binder.Type == typeof(string)) {
                result = _element.Value;
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
                result = xmlType.InvokeMember(binder.Name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, _element, args);
                return true;
            }
            catch {
                result = null;
                return false;
            }
        }

        public IEnumerator<DynamicNode> GetEnumerator() {
            return _element.Descendants().Select(each => new DynamicNode(each)).GetEnumerator();
        }

        /// <summary>
        ///   Returns the XML Text for the node.
        /// </summary>
        /// <returns>The XML Text</returns>
        public override string ToString() {
            return _element.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        ///   Adds a new child node
        /// </summary>
        /// <param name = "name">the new node name</param>
        /// <returns>the DynamicXmlNode for the new node</returns>
        public DynamicNode Add(string name) {
            var e = new XElement(name);
            _element.Add(e);
            return new DynamicNode(e);
        }

        public DynamicNode Add(DynamicNode dynamicNode ) {
            _element.Add(dynamicNode._element);
            return dynamicNode;
        }

        public DynamicNode Add( XElement element ) {
            _element.Add(element);
            return new DynamicNode(element);
        }

        public DynamicNode Add(string name, string value) {
            var e = new XElement(name) {Value = value};
            _element.Add(e);
            return new DynamicNode(e);
        }
    }

    /* internal static class DynamicTypeAssigner {
        internal static bool IsDelegateType(Type d) {
            return d.BaseType == typeof(MulticastDelegate) && d.GetMethod("Invoke") != null;
        }

        internal static Type[] GetDelegateParameterTypes(Type d) {
            if (!IsDelegateType(d))
                throw new ApplicationException("Not a delegate.");

            var parameters = d.GetMethod("Invoke").GetParameters();
            var typeParameters = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; i++) {
                typeParameters[i] = parameters[i].ParameterType;
            }
            return typeParameters;
        }

        /// <summary>
        /// Gets the Return type of a delegate
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static Type GetDelegateReturnType(Type d) {
            if (!IsDelegateType(d))
                throw new ApplicationException("Not a delegate.");

            return d.GetMethod("Invoke").ReturnType;
        }

        internal static void CopyMethodsToDelegates(this object destination, object from) {
            var destType = from.GetType();
            
            foreach (var field in destination.GetType().GetFields(BindingFlags.NonPublic).Where(f => f.FieldType.BaseType == typeof(MulticastDelegate))) {
                var name = field.Name.TrimStart('_');
                var method = destType.GetMethod(name, GetDelegateParameterTypes(field.GetType()));
                if( method != null && method.ReturnType == GetDelegateReturnType(field.GetType()) ) {
                    field.SetValue(destination, Delegate.CreateDelegate(destType, from, method));
                }
            }
        }

    }*/
}