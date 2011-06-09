//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace CoApp.Toolkit.Trace {
    using System.IO;
    using System.Xml.Serialization;
    using Utility;

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
        public ToolType ToolType { 
            get { return ToolSniffer.Sniffer.Identify(executablePath).Type; }
        }


        [XmlIgnore]
        public EnvironmentVariableIndexer Environment;

        [XmlIgnore]
        public FileIndexerByHandle FilesByHandle;

        [XmlIgnore]
        public ProcessIndexer Processes;

        [XmlIgnore]
        internal List<Process> ParentCollection;

        public Process()
        {
           Files = new FileIndexer(files);
           Processes = new ProcessIndexer(process);
           FilesByHandle = new FileIndexerByHandle(files);
           Environment = new EnvironmentVariableIndexer(environment);
        }
    }
}