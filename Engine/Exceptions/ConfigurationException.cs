//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
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