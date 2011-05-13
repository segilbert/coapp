//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;
    using Extensions;
    using Scripting.Languages.PropertySheet;
    
    public class PropertyRuleException : Exception {
        public RuleProperty Property;
        public PropertyRuleException(RuleProperty property, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(property.SourceFile, property.SourceRow, property.SourceColumn, errorcode, message.format(parameters))) {
            Property = property;
        }
    }
}