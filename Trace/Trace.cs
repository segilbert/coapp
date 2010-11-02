//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System.IO;
    using System.Xml.Serialization;
    using Scripting;

    public partial class Trace {
        private static XmlSerializer _serializer = new XmlSerializer(typeof(Trace));

        public static Target Load(string path) {
            using(FileStream fs = new FileStream(path, FileMode.Open)) {
                var target = (Target)_serializer.Deserialize(fs);
                return target;
            }
        }

        public void Save(string path) {
            using(FileStream fs = new FileStream(path, FileMode.Create)) {
                _serializer.Serialize(fs, this);
            }
        }
    }
}