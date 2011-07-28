//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

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
