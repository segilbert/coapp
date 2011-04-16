//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    public class FileIndexer {
        private readonly List<File> fileList;

        public FileIndexer(List<File> collection) {
            fileList = collection;
        }

        public File this[string path] {
            get {
                path = path.ToLower().NormalizePath();

                lock (fileList) {
                    var result = fileList.Where(x => x.FullPath.Equals(path)).LastOrDefault();

                    if (result == null) {
                        result = new File {FullPath = path};
                        fileList.Add(result);
                    }
                    return result;
                }
            }
        }
    }

    public class EnvironmentVariableIndexer
    {
        private readonly List<Variable> envList;

        public EnvironmentVariableIndexer(List<Variable> collection)
        {
            envList = collection;
        }

        public Variable this[string varname]
        {
            get {
                varname = varname.ToLower();

                lock (envList)
                {
                    var result = envList.Where(x => x.name.Equals(varname)).LastOrDefault();
                    return result;
                }
            }
        }
    }

    public class FileIndexerByHandle {
        private readonly List<File> fileList;

        public FileIndexerByHandle(List<File> collection) {
            fileList = collection;
        }

        public File this[IntPtr handle] {
            get {
                lock (fileList) {
                    var result = fileList.Where(x => x.Handle.Equals(handle)).LastOrDefault();
                    return result;
                }
            }
        }
    }

    public class ProcessIndexer {
        private readonly List<Process> processList;

        public ProcessIndexer(List<Process> collection) {
            processList = collection;
        }

        public Process this[int pid] {
            get {
                lock (processList) {
                    var result = processList.Traverse(c => c.process).Where(x => x.id == pid).LastOrDefault();

                    if (result == null) {
                        result = new Process {id = pid, ParentCollection = processList};

                        processList.Add(result);
                    }
                    return result;
                }
            }
        }
    }
}