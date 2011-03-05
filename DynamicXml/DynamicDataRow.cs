//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System.Data;
    using System.Dynamic;

    public class DynamicDataRow : DynamicObject {
        private DataRow _dataRow;

        public DynamicDataRow(DataRow dataRow) {
            _dataRow = dataRow;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            
            result =  _dataRow.Table.Columns.Contains(binder.Name) ? _dataRow.Field<string>(binder.Name) : null;
            /*
            if (binder.ReturnType == typeof(string)) {
                result = _dataRow.Field<string>(binder.Name);
            }
            else if (binder.ReturnType == typeof(int)) {
                result = _dataRow.Field<int>(binder.Name);
            }
            else if (binder.ReturnType == typeof(long)) {
                result = _dataRow.Field<long>(binder.Name);
            }
            */
            return true;
        }
    }
}
