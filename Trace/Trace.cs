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
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Scripting;

    public partial class Trace : IEnumerable<Process> {
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

        public IEnumerator<Process> GetEnumerator() {
            return processes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return processes.GetEnumerator();
        }

        [XmlIgnore]
        public ProcessIndexer Processes;

        public Process Add(Process process) {
            var result = processes.Where(x => x.id == process.id).FirstOrDefault();
            if(result != null)
                throw new Exception("Duplicate Process IDs in Trace file not permitted.");

            processes.Add(process);
            return process;
        }

        public Trace() {
            Processes = new ProcessIndexer(processes);
        }
    }
}