//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Linq;

    public class DynamicDataSet : DynamicObject  {
        private readonly DataSet _dataSet;
        private readonly Dictionary<string,object> _cache = new Dictionary<string, object>();

        public DynamicDataSet(DataSet dataSet) {
            _dataSet = dataSet;
        }

        public string this[string propertyName]  {
            get {
                return (from property in _dataSet.Tables["property"].AsEnumerable()
                    where property.Field<string>("Property") == propertyName
                    select property).FirstOrDefault().Field<string>("Value") ?? string.Empty;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = null;

            if (_cache.ContainsKey(binder.Name))
                result = _cache[binder.Name];
            else {
                if (_dataSet.Tables.Contains(binder.Name)) {
                    result = new DynamicDataTable(_dataSet.Tables[binder.Name]);
                }
                _cache.Add(binder.Name, result);
            }
            return true;
        }
    }
}
