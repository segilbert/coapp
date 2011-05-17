//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;
    using Extensions;
    using Scripting.Languages.PropertySheet;
    
    public class EndUserException : Exception {
        public EndUserException (string SourceFile, int SourceRow, int SourceColumn,string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(SourceFile, SourceRow, SourceColumn, errorcode, message.format(parameters))) {
        }
        
        public EndUserException (string errorcode, string message, params object[] parameters)
            : base(" :{0}:{1}".format(errorcode, message.format(parameters))) {
        }
    }
}