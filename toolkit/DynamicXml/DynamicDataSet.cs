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
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Linq;

    /// <summary>
    /// A dynamic (untyped) wrapper for DataSet objects (a collection of tables)
    /// </summary>
    /// <remarks></remarks>
    public class DynamicDataSet : DynamicObject  {
        /// <summary>
        /// the backing field for the dataset
        /// </summary>
        private readonly DataSet _dataSet;
        /// <summary>
        /// a cache collection to speed up the access of the tables in the dataset
        /// </summary>
        private readonly Dictionary<string,object> _cache = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDataSet"/> class.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <remarks></remarks>
        public DynamicDataSet(DataSet dataSet) {
            _dataSet = dataSet;
        }

        /// <summary>
        /// A short-cut accessor that provides access to values in the "property' table in the dataset
        /// </summary>
        /// <remarks></remarks>
        public string this[string propertyName]  {
            get {
                return (from property in _dataSet.Tables["property"].AsEnumerable()
                    where property.Field<string>("Property") == propertyName
                    select property).FirstOrDefault().Field<string>("Value") ?? string.Empty;
            }
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)</returns>
        /// <remarks>
        /// This gets the table for the given property name.
        /// 
        /// This attempts to cache the table instance to speed up subsequent operations.
        /// </remarks>
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
