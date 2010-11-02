//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Scripting;

    public partial class Process {

        private static XmlSerializer _serializer = new XmlSerializer(typeof(Process));

        public static Process Load(string path) {
            using(FileStream fs = new FileStream(path, FileMode.Open)) {
                var target = (Process)_serializer.Deserialize(fs);
                return target;
            }
        }

        public void Save(string path) {
            using(FileStream fs = new FileStream(path, FileMode.Create)) {
                _serializer.Serialize(fs, this);
            }
        }

        /*
        public class Indexer<T>{
            public <T
            public T this [string index] {
                get {
                    
                }
                set {
                    
                }
            }
        }*/

        public class FileIndexer {
            private List<File> fileList;
            public FileIndexer(List<File> collection) {
                fileList = collection;
            }

            public File this[ string path ] {
                get {
                    path = path.ToLower();

                    var result = fileList.Where(x => x.FullPath.Equals(path)).FirstOrDefault();

                    if(result == null) {
                        result = new File() {FullPath = path};
                        fileList.Add(result);
                    }

                    return result;
                }
            }
        }

        [XmlIgnore]
        public FileIndexer Files;

        public Process() {
           Files = new FileIndexer(files);
           
        }
    }
}