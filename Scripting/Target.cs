//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting {
    using System.IO;
    using System.Xml.Serialization;

    public partial class Target {
        private static XmlSerializer _serializer = new XmlSerializer(typeof(Target));

        public static Target Load(string path) {
            using (FileStream fs = new FileStream(path, FileMode.Open)) {
                var target = (Target)_serializer.Deserialize(fs);
                return target;
            }
        }

        public void Save(string path) {
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                _serializer.Serialize(fs, this);
            }
        }
    }
}
