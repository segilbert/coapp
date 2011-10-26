//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;
    using Extensions;
    using Scripting.Languages.PropertySheet;
    
    public class EndUserRuleException : CoAppException {
        public Rule Rule;
        public EndUserRuleException(Rule rule, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(rule.SourceLocation.SourceFile, rule.SourceLocation.Row, rule.SourceLocation.Column, errorcode, message.format(parameters))) {
            Rule = rule;
        }
    }
}