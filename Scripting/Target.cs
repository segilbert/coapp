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

        private string specFilename;
        private string cpsFilename;

        [XmlIgnore]
        public string Filename { 
            set { 
                var p = Path.GetFullPath(value);
                if (Path.HasExtension(p))
                    p = Path.Combine(Path.GetDirectoryName(p), Path.GetFileNameWithoutExtension(p));
                specFilename = p + ".spec";
                cpsFilename = p + ".cps";
            }
        get { return specFilename; }}

        [XmlIgnore] 
        public string Folder {get { return Path.GetDirectoryName(Path.GetFullPath(Filename)); }}

        [XmlIgnore]
        public bool Ignore;

        public static Target Load(string path) {
            using (FileStream fs = new FileStream(path, FileMode.Open)) {
                var target = (Target)_serializer.Deserialize(fs);
                return target;
            }
        }

        public void Save() {
            Save(Filename);
        }

        public void Save(string path) {
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                _serializer.Serialize(fs, this);
            }
        }
    }
}
