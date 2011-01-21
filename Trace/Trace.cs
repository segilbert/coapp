//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Extensions;

    public partial class Trace {
        private static XmlSerializer _serializer = new XmlSerializer(typeof (Trace));

        public static Trace Load(string path) {
            using (var fs = new FileStream(path, FileMode.Open)) {
                var target = (Trace) _serializer.Deserialize(fs);
                return target;
            }
        }

        public void Save(string path) {
            using (var fs = new FileStream(path, FileMode.Create)) {
                _serializer.Serialize(fs, this);
            }
        }

        [XmlIgnore] public ProcessIndexer Processes;

        public void SetParentProcessId(Process proc, int ppid) {
            if (proc.id == 0 || ppid == 0) {
                return;
            }
            lock (process) {
                var newParent = process.Traverse(c => c.process).Where(x => x.id == ppid).LastOrDefault();
                proc.ParentCollection.Remove(proc);
                proc.ParentCollection = newParent.process;

                newParent.process.Add(proc);
            }
        }


        public Process Add(Process aProcess) {
            lock (process) {
                var result = process.Where(x => x.id == aProcess.id).LastOrDefault();
                if (result != null) {
                    throw new Exception("Duplicate Process IDs in Trace file not permitted.");
                }

                process.Add(aProcess);
                return aProcess;
            }
        }

        public Trace() {
            Processes = new ProcessIndexer(process);
        }
    }
}