//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;

    public class ConflictingFileOrFolderException : Exception {
        public string ConflictedPath { get; set; }

        public ConflictingFileOrFolderException (string path) {
            ConflictedPath = path;
        }
    }
}