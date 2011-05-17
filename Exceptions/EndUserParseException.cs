//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;
    using Extensions;
    using Scripting.Utility;

    public class EndUserParseException : Exception {
        public Token Token;
        public EndUserParseException(Token token,string filename, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(filename, token.Row,token.Column, errorcode ,message.format(parameters))) {
            Token = token;
        }
    }
}