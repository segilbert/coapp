//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Linq;
    using Enumerable = System.Linq.Enumerable;

    /// <summary>
    /// A dynamic (untyped) wrapper for data tables 
    /// </summary>
    /// <remarks></remarks>
    public class DynamicDataTable : DynamicObject, IEnumerable<DynamicDataRow> {
        /// <summary>
        /// backing field for the data table this dynamic object represents.
        /// </summary>
        private readonly DataTable _table;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDataTable"/> class.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <remarks></remarks>
        public DynamicDataTable(DataTable table) {
            _table = table;
        }

        /// <summary>
        /// Gets the <see cref="CoApp.Toolkit.DynamicXml.DynamicDataRow"/> at the specified index.
        /// </summary>
        /// <remarks></remarks>
        public DynamicDataRow this[int index] {
            get { return new DynamicDataRow(_table.Rows[index]); }
        }

        /// <summary>
        /// Gets the <see cref="CoApp.Toolkit.DynamicXml.DynamicDataRow"/> where the first column has the value given (ie, treats the first column in the table as a primary key).
        /// </summary>
        /// <remarks></remarks>
        public DynamicDataRow this[dynamic keyValue] {
            get {
                var key = (from column in _table.Columns.Cast<DataColumn>() where column.Unique select column).FirstOrDefault();

                return key == null ? null: (from DataRow dr in _table.Rows where dr[key].Equals(keyValue) select new DynamicDataRow(dr)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the rows in the table.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
        /// <remarks></remarks>
        public IEnumerator<DynamicDataRow> GetEnumerator() {
            return Enumerable.Select(_table.AsEnumerable(), r => new DynamicDataRow(r)).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the rows in the table.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
        /// <remarks></remarks>
        IEnumerator IEnumerable.GetEnumerator() {
            return Enumerable.Select(_table.AsEnumerable(), r => new DynamicDataRow(r)).GetEnumerator();
        }
    }
}
