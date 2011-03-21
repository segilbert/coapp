using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Tasks {
    public class CoTask {
        private static readonly CoTaskFactory _factory = new CoTaskFactory();
        public static CoTaskFactory Factory { get { return _factory; } }
    }

    public class CoTask<TResult> {
        private static readonly CoTaskFactory<TResult> _factory = new CoTaskFactory<TResult>();
        public static CoTaskFactory<TResult> Factory { get { return _factory; } }
    }
}
