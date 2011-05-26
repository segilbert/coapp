//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;
    using Extensions;
    using Scripting.Languages.PropertySheet;
    
    public class EndUserRuleException : Exception {
        public Rule Rule;
        public EndUserRuleException(Rule rule, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(rule.SourceFile, rule.SourceRow, rule.SourceColumn, errorcode, message.format(parameters))) {
            Rule = rule;
        }
    }
}