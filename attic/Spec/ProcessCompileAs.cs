//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Spec {
    using System.ComponentModel;

    public enum ProcessCompileAs {
        [Description("none")]
        none,

        [Description("c")]
        c,

        [Description("c++")]
        cplusplus,

        [Description("resource")]
        resource,
    }
}