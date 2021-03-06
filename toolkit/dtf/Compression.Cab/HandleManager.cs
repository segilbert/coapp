//---------------------------------------------------------------------
// <copyright file="HandleManager.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl1.0.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.Compression.Cab
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Generic class for managing allocations of integer handles
    /// for objects of a certain type.
    /// </summary>
    /// <typeparam name="T">The type of objects the handles refer to.</typeparam>
    internal sealed class HandleManager<T> where T : class
    {
        /// <summary>
        /// Auto-resizing list of objects for which handles have been allocated.
        /// Each handle is just an index into this list. When a handle is freed,
        /// the list item at that index is set to null.
        /// </summary>
        private List<T> handles;

        /// <summary>
        /// Creates a new HandleManager instance.
        /// </summary>
        internal HandleManager()
        {
            this.handles = new List<T>();
        }

        /// <summary>
        /// Gets the object of a handle, or null if the handle is invalid.
        /// </summary>
        /// <param name="handle">The integer handle previously allocated
        /// for the desired object.</param>
        /// <returns>The object for which the handle was allocated.</returns>
        internal T this[int handle]
        {
            get
            {
                if (handle > 0 && handle <= this.handles.Count)
                {
                    return this.handles[handle - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Allocates a new handle for an object.
        /// </summary>
        /// <param name="obj">Object that the handle will refer to.</param>
        /// <returns>New handle that can be later used to retrieve the object.</returns>
        internal int AllocHandle(T obj)
        {
            this.handles.Add(obj);
            int handle = this.handles.Count;
            return handle;
        }

        /// <summary>
        /// Frees a handle that was previously allocated. Afterward the handle
        /// will be invalid and the object it referred to can no longer retrieved.
        /// </summary>
        /// <param name="handle">Handle to be freed.</param>
        internal void FreeHandle(int handle)
        {
            if (handle > 0 && handle <= this.handles.Count)
            {
                this.handles[handle - 1] = null;
            }
        }
    }
}
