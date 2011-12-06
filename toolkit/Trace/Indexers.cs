//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    public class FileIndexer {
        private readonly List<File> _fileList;

        public FileIndexer(List<File> collection) {
            _fileList = collection;
        }

        public File this[string path] {
            get {
                path = path.ToLower().NormalizePath();

                lock (_fileList) {
                    var result = _fileList.Where(x => x.FullPath.Equals(path)).LastOrDefault();

                    if (result == null) {
                        result = new File {FullPath = path};
                        _fileList.Add(result);
                    }
                    return result;
                }
            }
        }
    }

    public class EnvironmentVariableIndexer {
        private readonly List<Variable> _envList;

        public EnvironmentVariableIndexer(List<Variable> collection) {
            _envList = collection;
        }

        public Variable this[string varname] {
            get {
                varname = varname.ToLower();

                lock (_envList) {
                    var result = _envList.Where(x => x.name.Equals(varname)).LastOrDefault();
                    return result;
                }
            }
        }
    }

    public class FileIndexerByHandle {
        private readonly List<File> _fileList;

        public FileIndexerByHandle(List<File> collection) {
            _fileList = collection;
        }

        public File this[long handle] {
            get {
                lock (_fileList) {
                    var result = _fileList.Where(x => x.Handle.Equals(handle)).LastOrDefault();
                    return result;
                }
            }
        }
    }

    public class ProcessIndexer {
        private readonly List<Process> _processList;

        public ProcessIndexer(List<Process> collection) {
            _processList = collection;
        }

        public Process this[int pid] {
            get {
                lock (_processList) {
                    var result = _processList.Traverse(c => c.process).Where(x => x.id == pid).LastOrDefault();

                    if (result == null) {
                        result = new Process {id = pid, ParentCollection = _processList};

                        _processList.Add(result);
                    }
                    return result;
                }
            }
        }
    }
}