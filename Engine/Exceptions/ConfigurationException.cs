//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;

    internal class ConfigurationException : Exception {
        public string Key { get; set; }
        public string Detail { get; set; }

        public ConfigurationException(string message, string key, string detail) : base(message) {
            Key = key;
            Detail = detail;
        }
    }
}