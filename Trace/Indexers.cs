//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CoApp.Toolkit.Extensions;

    public class FileIndexer {
        private List<File> fileList;
        public FileIndexer(List<File> collection) {
            fileList = collection;
        }

        public File this[string path] {
            get {
                path = path.ToLower();

                var result = fileList.Where(x => x.FullPath.Equals(path)).FirstOrDefault();

                if(result == null) {
                    result = new File() { FullPath = path };
                    fileList.Add(result);
                }

                return result;
            }
        }
    }

    public class FileIndexerByHandle {
        private List<File> fileList;

        public FileIndexerByHandle(List<File> collection) {
            fileList = collection;
        }

        public File this[IntPtr handle] {
            get {
                var result = fileList.Where(x => x.Handle.Equals(handle)).LastOrDefault();
                return result;
            }
        }
    }

    public class ProcessIndexer {
        private List<Process> processList;

        public ProcessIndexer(List<Process> collection) {
            processList = collection;
        }

        public Process this[long PID] {
            get {
                lock (processList) {
                    var result = processList.Traverse(c => c.process).Where(x => x.id == PID).FirstOrDefault();
                    // var result = processList.Where(x => x.id == PID).FirstOrDefault();)

                    if (result == null) {
                        result = new Process() {id = PID, ParentCollection = processList};

                        processList.Add(result);
                    }
                    return result;
                }
                
            }
        }
    }
}
