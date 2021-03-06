﻿//---------------------------------------------------------------------
// <copyright file="Attributes.cs" company="Microsoft">
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

namespace Microsoft.Deployment.WindowsInstaller.Linq
{
    using System;

    /// <summary>
    /// Apply to a subclass of QRecord to indicate the name of
    /// the table the record type is to be used with.
    /// </summary>
    /// <remarks>
    /// If this attribute is not used on a record type, the default
    /// table name will be derived from the record type name. (An
    /// optional underscore suffix is stripped.)
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class DatabaseTableAttribute : Attribute
    {
        /// <summary>
        /// Creates a new DatabaseTableAttribute for the specified table.
        /// </summary>
        /// <param name="table">name of the table associated with the record type</param>
        internal DatabaseTableAttribute(string table)
        {
            this.Table = table;
        }

        /// <summary>
        /// Gets or sets the table associated with the record type.
        /// </summary>
        internal string Table { get; set; }
    }

    /// <summary>
    /// Apply to a property on a subclass of QRecord to indicate
    /// the name of the column the property is to be associated with.
    /// </summary>
    /// <remarks>
    /// If this attribute is not used on a property, the default
    /// column name will be the same as the property name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class DatabaseColumnAttribute : Attribute
    {
        /// <summary>
        /// Creates a new DatabaseColumnAttribute which maps a
        /// record property to a column.
        /// </summary>
        /// <param name="column">name of the column associated with the property</param>
        internal DatabaseColumnAttribute(string column)
        {
            this.Column = column;
        }

        /// <summary>
        /// Gets or sets the column associated with the record property.
        /// </summary>
        internal string Column { get; set; }
    }
}
