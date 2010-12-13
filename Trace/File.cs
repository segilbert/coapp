//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using Scripting;

    public partial class File {
        private string fullPath;

        [XmlIgnore]
        public IntPtr Handle { get; set; }

        [XmlIgnore]
        public string FullPath {
            get { return fullPath; }
            set {
                fullPath = value.ToLower();

                extension = Path.GetExtension(fullPath);
                name = Path.GetFileName(fullPath);
                folder = Path.GetDirectoryName(fullPath);
            }
        }

        
    }
}