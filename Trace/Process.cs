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

    public partial class Process  {

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

        [XmlIgnore]
        public FileIndexer Files;

        [XmlIgnore]
        public FileIndexerByHandle FilesByHandle;

        [XmlIgnore]
        public ProcessIndexer Processes;

        public Process() {
           Files = new FileIndexer(files);
           Processes = new ProcessIndexer(processes);
           FilesByHandle = new FileIndexerByHandle(files);
        }

        public Process Add(Process process) {
            var result = processes.Where(x => x.id == process.id).FirstOrDefault();
            if(result != null)
                throw new Exception("Duplicate Process IDs in Trace file not permitted.");

            processes.Add(process);
            return process;
        }
    }
}