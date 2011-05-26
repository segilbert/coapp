//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
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