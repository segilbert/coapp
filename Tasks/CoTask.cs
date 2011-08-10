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
    /// <summary>
    /// A wrapper to use CoTaskFactory similar to TaskFactory
    /// </summary>
    /// <remarks></remarks>
    public class CoTask {
        /// <summary>
        /// 
        /// </summary>
        private static readonly CoTaskFactory _factory = new CoTaskFactory();
        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <remarks></remarks>
        public static CoTaskFactory Factory { get { return _factory; } }
    }

    /// <summary>
    /// A wrapper to use CoTaskFactory similar to TaskFactory
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <remarks></remarks>
    public class CoTask<TResult> {
        /// <summary>
        /// 
        /// </summary>
        private static readonly CoTaskFactory<TResult> _factory = new CoTaskFactory<TResult>();
        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <remarks></remarks>
        public static CoTaskFactory<TResult> Factory { get { return _factory; } }
    }
}
