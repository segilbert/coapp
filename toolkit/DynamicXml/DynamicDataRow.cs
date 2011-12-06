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
    using System.Data;
    using System.Dynamic;

    /// <summary>
    /// A wrapper that creates a dynamic (untyped) accessor for data rows in a DataTable.
    /// 
    /// Allows simplistic access to the data in a data row.
    /// </summary>
    public class DynamicDataRow : DynamicObject {
        /// <summary>
        /// the backing field for the actual data row.
        /// </summary>
        private readonly DataRow _dataRow;

        /// <summary>
        /// Creates an instance that wraps a data row.
        /// </summary>
        /// <param name="dataRow">the row to wrap</param>
        public DynamicDataRow(DataRow dataRow) {
            _dataRow = dataRow;
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. 
        /// 
        /// This method is used to specify dynamic behavior for operations such as getting a value for a property.
        /// 
        /// This provides the ability to have a dynamic (untyped) access to a datarow field based on column name.
        /// </summary>
        /// <returns>
        /// true if the operation is successful; otherwise, false. 
        /// 
        /// If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param><param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
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
