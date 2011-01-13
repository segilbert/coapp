//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace CoApp.Toolkit.Trace {
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using CoApp.Toolkit.Extensions;

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

        [XmlIgnore]
        internal List<Process> ParentCollection;

        public Process() {
           Files = new FileIndexer(files);
           Processes = new ProcessIndexer(process);
           FilesByHandle = new FileIndexerByHandle(files);
        }

        /*
        public Process Add(Process aProcess) {
            var result = process.Traverse(c => c.process).Where(x => x.id == aProcess.id).FirstOrDefault();
            if(result != null)
                throw new Exception("Duplicate Process IDs in Trace file not permitted.");

            process.Add(aProcess);
            aProcess.Parent = this;
            return aProcess;
        }
         * */
    }
}