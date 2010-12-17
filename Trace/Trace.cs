//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;

    public partial class Trace  {
        private static XmlSerializer _serializer = new XmlSerializer(typeof(Trace));

        public static Trace Load(string path) {
            using(FileStream fs = new FileStream(path, FileMode.Open)) {
                var target = (Trace)_serializer.Deserialize(fs);
                return target;
            }
        }

        public void Save(string path) {
            using(FileStream fs = new FileStream(path, FileMode.Create)) {
                _serializer.Serialize(fs, this);
            }
        }

        [XmlIgnore]
        public ProcessIndexer Processes;

        public Process Add(Process aProcess) {
            var result = process.Where(x => x.id == aProcess.id).FirstOrDefault();
            if(result != null)
                throw new Exception("Duplicate Process IDs in Trace file not permitted.");

            process.Add(aProcess);
            return aProcess;
        }

        public Trace() {
            Processes = new ProcessIndexer(process);
        }
    }
}