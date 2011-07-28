//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    internal static class ReparsePointError {
        /// <summary>
        ///   The file or directory is not a reparse point.
        /// </summary>
        internal const int NotAReparsePoint = 4390;

        /// <summary>
        ///   The reparse point attribute cannot be set because it conflicts with an existing attribute.
        /// </summary>
        internal const int ReparseAttributeConflict = 4391;

        /// <summary>
        ///   The data present in the reparse point buffer is invalid.
        /// </summary>
        internal const int InvalidReparseData = 4392;

        /// <summary>
        ///   The tag present in the reparse point buffer is invalid.
        /// </summary>
        internal const int ReparseTagInvalid = 4393;

        /// <summary>
        ///   There is a mismatch between the tag specified in the request and the tag present in the reparse point.
        /// </summary>
        internal const int ReparseTagMismatch = 4394;
    }
}