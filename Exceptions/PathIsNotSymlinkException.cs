//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Exceptions {
    using System;

    public class PathIsNotSymlinkException : Exception {
        public string Path { get; set; }

        public PathIsNotSymlinkException(string path) {
            Path = path;
        }
    }
}