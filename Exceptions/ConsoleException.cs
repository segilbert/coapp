//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;
    using CoApp.Toolkit.Extensions;

    public class ConsoleException : Exception {
        public ConsoleException(string reason, params object[] parameters) : base(reason.format(parameters)) {
        }
    }
}