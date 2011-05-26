//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using System.ComponentModel;

    public enum EventCondition {
        [Description("pre-build")]
        preBuild,

        [Description("post-build")]
        postBuild,

        [Description("failure")]
        failure,
    }
}