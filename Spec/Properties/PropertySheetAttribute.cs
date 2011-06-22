//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec.Properties {
    using System;

    public class PropertySheetAttribute : Attribute {
        public string PropertyName { get; set; }

        public PropertySheetAttribute(string propertyName) {
            PropertyName = propertyName;
        }
    }

    public class PropertySheetIgnoreAttribute : Attribute {

    }
    
}