//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Linq;
    using Enumerable = System.Linq.Enumerable;

    public class DynamicDataTable : DynamicObject, IEnumerable<DynamicDataRow> {
        private readonly DataTable _table;

        public DynamicDataTable(DataTable table) {
            _table = table;
        }

        public DynamicDataRow this[int index] {
            get { return new DynamicDataRow(_table.Rows[index]); }
        }

        public DynamicDataRow this[dynamic keyValue] {
            get {
                var key = (from column in _table.Columns.Cast<DataColumn>() where column.Unique select column).FirstOrDefault();

                return key == null ? null: (from DataRow dr in _table.Rows where dr[key].Equals(keyValue) select new DynamicDataRow(dr)).FirstOrDefault();
            }
        }

        public IEnumerator<DynamicDataRow> GetEnumerator() {
            return Enumerable.Select(_table.AsEnumerable(), r => new DynamicDataRow(r)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Enumerable.Select(_table.AsEnumerable(), r => new DynamicDataRow(r)).GetEnumerator();
        }
    }
}
