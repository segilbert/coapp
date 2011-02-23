//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.PackageFormatHandlers {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using Engine;
    using Engine.Exceptions;
    using Extensions;
    using Microsoft.Deployment.WindowsInstaller;
    using Tasks;
    using View = Microsoft.Deployment.WindowsInstaller.View;

    internal static class DataSetExtensions {
        internal static string GetProperty(this DataSet dataSet, string propertyName) {
            return (from property in dataSet.Tables["property"].AsEnumerable()
                    where property.Field<string>("Property") == propertyName
                    select property).FirstOrDefault().Field<string>("Value") ?? string.Empty;
        }

        internal static IEnumerable<DataRow> GetTable(this DataSet dataSet, string tableName) {
            return dataSet.Tables[tableName].AsEnumerable();
        }
    }

    internal class MSIBase : IPackageFormatHandler {
        private static readonly IEnumerable<string> SignificantTables = new[] {"CO_*", "property", "MsiAssembly*"};
        private static readonly Dictionary<string, DataSet> MSIData = new Dictionary<string, DataSet>();

        static MSIBase() {
            SetUIHandlersToSilent();
        }

        protected static void SetUIHandlersToSilent() {
            Installer.SetInternalUI(InstallUIOptions.Silent);
            Installer.SetExternalUI(ExternalUI, InstallLogModes.Verbose);
        }

        public static MessageResult ExternalUI(InstallMessage messageType, string message, MessageBoxButtons buttons, MessageBoxIcon icon,
                                        MessageBoxDefaultButton defaultButton) {
            return MessageResult.OK;
        }

        public bool IsInstalled(string productCode) {
            try {
                Installer.OpenProduct(productCode).Close();
                return true;
            }
            catch {
            }
            return false;
        }

        public void Install(string packagePath, Action<int> progress = null) {
            progress = progress ?? ((percent) => { });

            int currentTotalTicks = -1;
            int currentProgress = 0;
            int progressDirection = 1;

            Installer.SetExternalUI(((messageType, message, buttons, icon, defaultButton) => {
                switch (messageType) {
                    case InstallMessage.Progress:
                        if (message.Length >= 2) {
                            var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).ToArray();

                            switch (msg[1]) {
                                // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                    currentTotalTicks = msg[3];
                                    currentProgress = 0;
                                    if (msg.Length >= 6) {
                                        progressDirection = msg[5] == 0 ? 1 : -1;
                                    }
                                    break;
                                case 1:
                                    //Provides information related to progress messages to be sent by the current action.
                                    break;
                                case 2: //Increments the progress bar.
                                    if (currentTotalTicks == -1) {
                                        break;
                                    }
                                    currentProgress += msg[3] * progressDirection;
                                    break;
                                case 3:
                                    //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                    break;
                            }
                        }

                        if (currentTotalTicks > 0) {
                            progress(currentProgress * 100 / currentTotalTicks);
                        }
                        break;
                }
                // capture installer messages to play back to status listener
                return MessageResult.OK;
            }), InstallLogModes.Progress);

            try {
                Installer.InstallProduct(packagePath,
                    @"TARGETDIR=""{0}"" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS".format(PackageManagerSettings.CoAppInstalledDirectory));
            }
            finally {
                SetUIHandlersToSilent();
            } 
        }

        public void Remove(string packagePath, Action<int> progress = null) {
            progress = progress ?? ((percent) => { });
            int currentTotalTicks = -1;
            int currentProgress = 0;
            int progressDirection = 1;

            Installer.SetExternalUI(((messageType, message, buttons, icon, defaultButton) => {
                switch (messageType) {
                    case InstallMessage.Progress:
                        if (message.Length >= 2) {
                            var msg =
                                message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).
                                    ToArray();

                            switch (msg[1]) {
                                    // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                    currentTotalTicks = msg[3];
                                    currentProgress = 0;
                                    if (msg.Length >= 6) {
                                        progressDirection = msg[5] == 0 ? 1 : -1;
                                    }
                                    break;
                                case 1: //Provides information related to progress messages to be sent by the current action.
                                    break;
                                case 2: //Increments the progress bar.
                                    if (currentTotalTicks == -1) {
                                        break;
                                    }
                                    currentProgress += msg[3]*progressDirection;
                                    break;
                                case 3:
                                    //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                    break;
                            }
                            if (currentTotalTicks > 0) {
                                progress(currentProgress*100/currentTotalTicks);
                            }
                        }
                        break;
                }
                // capture installer messages to play back to status listener
                return MessageResult.OK;
            }), InstallLogModes.Progress);

            try {
                Installer.InstallProduct(packagePath, @"REMOVE=ALL COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
            }
            finally {
                SetUIHandlersToSilent();
            } 
        }

        public static void ScanInstalledMSIs() {
            var products = ProductInstallation.AllProducts;
            var n = 0;
            var total = products.Count();
            foreach (var product in products) {
                try {
                    if (CoTask.CurrentCancellationToken.IsCancellationRequested) {
                        return;
                    }
                    int percent = ((n++) * 100) / total;
                    PackageManagerMessages.Invoke.InstallerMessage(PackageInstallerMessage.Scanning, null, percent);
                    Registrar.GetPackage(product.LocalPackage); // let the registrar figure out if this is a package we care about.
                }
                catch {
                }
            }
        }

        public static DataSet GetMSIData(string localPackagePath) {
            localPackagePath = localPackagePath.ToLower();

            if (MSIData.ContainsKey(localPackagePath)) {
                return MSIData[localPackagePath];
            }

            try {
                using (var database = new Database(localPackagePath, DatabaseOpenMode.ReadOnly)) {
                    var dataSet = new DataSet(localPackagePath) {EnforceConstraints = false};

                    foreach (var t in database.Tables) {
                        try {
                            if (!t.Columns[0].IsTemporary) {
                                if (SignificantTables.Any(tn => t.Name.IsWildcardMatch(tn))) {
                                    using (var dr = new MSIDataReader(database, t)) {
                                        dataSet.Tables.Add(t.Name).Load(dr);
                                    }
                                }
                            }
                        }
                        catch (Exception) {
                            // some tables not play nice.
                        }
                    }

                    MSIData.Add(localPackagePath, dataSet);
                    return dataSet;
                }
            }
            catch (InstallerException) {
                throw new InvalidPackageException(InvalidReason.NotValidMSI, localPackagePath);
            }
        }

        #region Nested type: MSIDataReader

        private class MSIDataReader : IDataReader {
            private readonly int _columnCount;
            private Record _currentRecord;
            private Database _database;
            private IEnumerator<Record> _enumerator;
            private DataTable _schema;
            private TableInfo _tableInfo;
            private View _tableView;

            public MSIDataReader(Database msiDatabase, TableInfo tableInfo) {
                _database = msiDatabase;
                _tableInfo = tableInfo;
                _tableView = _database.OpenView(tableInfo.SqlSelectString);
                _tableView.Execute();
                _enumerator = _tableView.GetEnumerator();
                _columnCount = tableInfo.Columns.Count;
            }

            #region IDataReader Members

            /// <summary>
            ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose() {
                if (_currentRecord != null) {
                    _currentRecord.Dispose();
                }

                if (_enumerator != null) {
                    _enumerator.Dispose();
                }

                if (_tableView != null) {
                    _tableView.Dispose();
                }

                _currentRecord = null;
                _enumerator = null;
                _tableView = null;
                _database = null;
                _tableInfo = null;
            }

            /// <summary>
            ///   Gets the name for the field to find.
            /// </summary>
            /// <returns>
            ///   The name of the field or the empty string (""), if there is no value to return.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public string GetName(int i) {
                return _tableInfo.Columns[i].Name ?? string.Empty;
            }

            /// <summary>
            ///   Gets the data type information for the specified field.
            /// </summary>
            /// <returns>
            ///   The data type information for the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public string GetDataTypeName(int i) {
                return _tableInfo.Columns[i].DBType.ToString();
            }

            /// <summary>
            ///   Gets the <see cref = "T:System.Type" /> information corresponding to the type of <see cref = "T:System.Object" /> that would be returned from <see cref = "M:System.Data.IDataRecord.GetValue(System.Int32)" />.
            /// </summary>
            /// <returns>
            ///   The <see cref = "T:System.Type" /> information corresponding to the type of <see cref = "T:System.Object" /> that would be returned from <see cref = "M:System.Data.IDataRecord.GetValue(System.Int32)" />.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public Type GetFieldType(int i) {
                switch (_tableInfo.Columns[i].DBType) {
                    case DbType.String:
                        return typeof (string);
                    case DbType.Int32:
                        return typeof (Int32);
                    case DbType.Int16:
                        return typeof (Int16);
                    case DbType.Binary:
                        return typeof (Byte[]);
                    default:
                        throw new Exception("According to the DTF docs, this shouldn't happen.");
                }
            }

            /// <summary>
            ///   Return the value of the specified field.
            /// </summary>
            /// <returns>
            ///   The <see cref = "T:System.Object" /> which will contain the field value upon return.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public object GetValue(int i) {
                switch (_tableInfo.Columns[i].DBType) {
                    case DbType.String:
                        return GetString(i) ?? String.Empty;
                    case DbType.Int32:
                        return GetInt32(i);
                    case DbType.Int16:
                        return GetInt16(i);
                    case DbType.Binary:
                        return new byte[0]; // we're not pulling out binary data.
                    default:
                        throw new Exception("According to the DTF docs, this shouldn't happen.");
                }
            }

            /// <summary>
            ///   Populates an array of objects with the column values of the current record.
            /// </summary>
            /// <returns>
            ///   The number of instances of <see cref = "T:System.Object" /> in the array.
            /// </returns>
            /// <param name = "values">An array of <see cref = "T:System.Object" /> to copy the attribute fields into. </param>
            /// <filterpriority>2</filterpriority>
            public int GetValues(object[] values) {
                for (int i = 0; i < _columnCount; i++) {
                    values[i] = GetValue(i);
                }
                return _columnCount;
            }

            /// <summary>
            ///   Return the index of the named field.
            /// </summary>
            /// <returns>
            ///   The index of the named field.
            /// </returns>
            /// <param name = "name">The name of the field to find. </param>
            /// <filterpriority>2</filterpriority>
            public int GetOrdinal(string name) {
                for (int i = 0; i < _columnCount; i++) {
                    if (_tableInfo.Columns[name].Equals(name)) {
                        return i;
                    }
                }
                throw new ArgumentException("Column name [{0}] not found in collection".format(name), "name");
            }

            /// <summary>
            ///   Gets the value of the specified column as a Boolean.
            /// </summary>
            /// <returns>
            ///   The value of the column.
            /// </returns>
            /// <param name = "i">The zero-based column ordinal. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public bool GetBoolean(int i) {
                var v = GetString(i);
                return v.Equals("true", StringComparison.CurrentCultureIgnoreCase) || v.Equals("1");
            }

            /// <summary>
            ///   Gets the 8-bit unsigned integer value of the specified column.
            /// </summary>
            /// <returns>
            ///   The 8-bit unsigned integer value of the specified column.
            /// </returns>
            /// <param name = "i">The zero-based column ordinal. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public byte GetByte(int i) {
                return (byte) GetInt16(i);
            }

            /// <summary>
            ///   Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
            /// </summary>
            /// <returns>
            ///   The actual number of bytes read.
            /// </returns>
            /// <param name = "i">The zero-based column ordinal. </param>
            /// <param name = "fieldOffset">The index within the field from which to start the read operation. </param>
            /// <param name = "buffer">The buffer into which to read the stream of bytes. </param>
            /// <param name = "bufferoffset">The index for <paramref name = "buffer" /> to start the read operation. </param>
            /// <param name = "length">The number of bytes to read. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) {
                return 0L;
            }

            /// <summary>
            ///   Gets the character value of the specified column.
            /// </summary>
            /// <returns>
            ///   The character value of the specified column.
            /// </returns>
            /// <param name = "i">The zero-based column ordinal. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public char GetChar(int i) {
                return (char) GetInt16(i);
            }

            /// <summary>
            ///   Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
            /// </summary>
            /// <returns>
            ///   The actual number of characters read.
            /// </returns>
            /// <param name = "i">The zero-based column ordinal. </param>
            /// <param name = "fieldoffset">The index within the row from which to start the read operation. </param>
            /// <param name = "buffer">The buffer into which to read the stream of bytes. </param>
            /// <param name = "bufferoffset">The index for <paramref name = "buffer" /> to start the read operation. </param>
            /// <param name = "length">The number of bytes to read. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) {
                return 0L;
            }

            /// <summary>
            ///   Returns the GUID value of the specified field.
            /// </summary>
            /// <returns>
            ///   The GUID value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public Guid GetGuid(int i) {
                return new Guid(GetString(i));
            }

            /// <summary>
            ///   Gets the 16-bit signed integer value of the specified field.
            /// </summary>
            /// <returns>
            ///   The 16-bit signed integer value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public short GetInt16(int i) {
                return (short) _currentRecord.GetInteger(i + 1);
            }

            /// <summary>
            ///   Gets the 32-bit signed integer value of the specified field.
            /// </summary>
            /// <returns>
            ///   The 32-bit signed integer value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public int GetInt32(int i) {
                return _currentRecord.GetInteger(i + 1);
            }

            /// <summary>
            ///   Gets the 64-bit signed integer value of the specified field.
            /// </summary>
            /// <returns>
            ///   The 64-bit signed integer value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public long GetInt64(int i) {
                return GetInt32(i);
            }

            /// <summary>
            ///   Gets the single-precision floating point number of the specified field.
            /// </summary>
            /// <returns>
            ///   The single-precision floating point number of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public float GetFloat(int i) {
                return GetInt32(i);
            }

            /// <summary>
            ///   Gets the double-precision floating point number of the specified field.
            /// </summary>
            /// <returns>
            ///   The double-precision floating point number of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public double GetDouble(int i) {
                return GetInt32(i);
            }

            /// <summary>
            ///   Gets the string value of the specified field.
            /// </summary>
            /// <returns>
            ///   The string value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public string GetString(int i) {
                return _currentRecord.GetString(i + 1);
            }

            /// <summary>
            ///   Gets the fixed-position numeric value of the specified field.
            /// </summary>
            /// <returns>
            ///   The fixed-position numeric value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public decimal GetDecimal(int i) {
                return GetInt32(i);
            }

            /// <summary>
            ///   Gets the date and time data value of the specified field.
            /// </summary>
            /// <returns>
            ///   The date and time data value of the specified field.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public DateTime GetDateTime(int i) {
                return DateTime.Parse(GetString(i));
            }

            /// <summary>
            ///   Returns an <see cref = "T:System.Data.IDataReader" /> for the specified column ordinal.
            /// </summary>
            /// <returns>
            ///   An <see cref = "T:System.Data.IDataReader" />.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public IDataReader GetData(int i) {
                throw new NotImplementedException();
            }

            /// <summary>
            ///   Return whether the specified field is set to null.
            /// </summary>
            /// <returns>
            ///   true if the specified field is set to null; otherwise, false.
            /// </returns>
            /// <param name = "i">The index of the field to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            public bool IsDBNull(int i) {
                return _currentRecord.IsNull(i);
            }

            /// <summary>
            ///   Gets the number of columns in the current row.
            /// </summary>
            /// <returns>
            ///   When not positioned in a valid recordset, 0; otherwise, the number of columns in the current record. The default is -1.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public int FieldCount {
                get { return _columnCount; }
            }

            /// <summary>
            ///   Gets the column located at the specified index.
            /// </summary>
            /// <returns>
            ///   The column located at the specified index as an <see cref = "T:System.Object" />.
            /// </returns>
            /// <param name = "i">The zero-based index of the column to get. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref = "P:System.Data.IDataRecord.FieldCount" />. </exception>
            /// <filterpriority>2</filterpriority>
            object IDataRecord.this[int i] {
                get { return _currentRecord[i + 1]; }
            }

            /// <summary>
            ///   Gets the column with the specified name.
            /// </summary>
            /// <returns>
            ///   The column with the specified name as an <see cref = "T:System.Object" />.
            /// </returns>
            /// <param name = "name">The name of the column to find. </param>
            /// <exception cref = "T:System.IndexOutOfRangeException">No column with the specified name was found. </exception>
            /// <filterpriority>2</filterpriority>
            object IDataRecord.this[string name] {
                get { return _currentRecord[name]; }
            }

            /// <summary>
            ///   Closes the <see cref = "T:System.Data.IDataReader" /> Object.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Close() {
                _tableView.Close();
            }

            /// <summary>
            ///   Returns a <see cref = "T:System.Data.DataTable" /> that describes the column metadata of the <see cref = "T:System.Data.IDataReader" />.
            /// </summary>
            /// <returns>
            ///   A <see cref = "T:System.Data.DataTable" /> that describes the column metadata.
            /// </returns>
            /// <exception cref = "T:System.InvalidOperationException">The <see cref = "T:System.Data.IDataReader" /> is closed. </exception>
            /// <filterpriority>2</filterpriority>
            public DataTable GetSchemaTable() {
                if (IsClosed) {
                    throw new InvalidOperationException("View is closed");
                }

                if (_schema == null) {
                    _schema = new DataTable("SchemaTable" + _tableInfo.Name);

                    _schema.Columns.AddRange(new[] {
                        new DataColumn(SchemaTableColumn.ColumnName, typeof (string)),
                        new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof (int)),
                        new DataColumn(SchemaTableColumn.ColumnSize, typeof (int)),
                        new DataColumn(SchemaTableColumn.NumericPrecision, typeof (short)),
                        new DataColumn(SchemaTableColumn.NumericScale, typeof (short)),
                        new DataColumn(SchemaTableColumn.DataType, typeof (Type)),
                        new DataColumn(SchemaTableColumn.NonVersionedProviderType, typeof (int)),
                        new DataColumn(SchemaTableColumn.ProviderType, typeof (int)),
                        new DataColumn(SchemaTableColumn.IsLong, typeof (bool)),
                        new DataColumn(SchemaTableColumn.AllowDBNull, typeof (bool)),
                        new DataColumn(SchemaTableColumn.IsUnique, typeof (bool)),
                        new DataColumn(SchemaTableColumn.IsKey, typeof (bool)),
                        new DataColumn(SchemaTableColumn.BaseSchemaName, typeof (string)),
                        new DataColumn(SchemaTableColumn.BaseTableName, typeof (string)),
                        new DataColumn(SchemaTableColumn.BaseColumnName, typeof (string)),
                        new DataColumn(SchemaTableColumn.IsAliased, typeof (bool)),
                        new DataColumn(SchemaTableColumn.IsExpression, typeof (bool)),
                    });

                    for (int i = 0; i < _tableInfo.Columns.Count; i++) {
                        DataRow row = _schema.NewRow();

                        row[SchemaTableColumn.ColumnName] = _tableInfo.Columns[i].Name;
                        row[SchemaTableColumn.ColumnOrdinal] = i;
                        row[SchemaTableColumn.ColumnSize] = (int) byte.MaxValue;
                        row[SchemaTableColumn.NumericPrecision] = (short) 0;
                        row[SchemaTableColumn.NumericScale] = (short) 0;
                        row[SchemaTableColumn.DataType] = GetFieldType(i);
                        row[SchemaTableColumn.NonVersionedProviderType] = 1;
                        row[SchemaTableColumn.ProviderType] = 1;
                        row[SchemaTableColumn.IsLong] = false;
                        row[SchemaTableColumn.AllowDBNull] = true;
                        row[SchemaTableColumn.IsUnique] = false;
                        row[SchemaTableColumn.IsKey] = false;
                        row[SchemaTableColumn.BaseSchemaName] = string.Empty;
                        row[SchemaTableColumn.BaseTableName] = string.Empty;
                        row[SchemaTableColumn.BaseColumnName] = string.Empty;
                        row[SchemaTableColumn.IsAliased] = false;
                        row[SchemaTableColumn.IsExpression] = false;

                        _schema.Rows.Add(row);
                    }
                }

                return _schema;
            }

            /// <summary>
            ///   Advances the data reader to the next result, when reading the results of batch SQL statements.
            /// </summary>
            /// <returns>
            ///   true if there are more rows; otherwise, false.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public bool NextResult() {
                if (_currentRecord != null) {
                    _currentRecord.Dispose();
                }
                _currentRecord = null;
                var result = _enumerator.MoveNext();
                _currentRecord = result ? _enumerator.Current : null;
                return result;
            }

            /// <summary>
            ///   Advances the <see cref = "T:System.Data.IDataReader" /> to the next record.
            /// </summary>
            /// <returns>
            ///   true if there are more rows; otherwise, false.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public bool Read() {
                return NextResult();
            }

            /// <summary>
            ///   Gets a value indicating the depth of nesting for the current row.
            /// </summary>
            /// <returns>
            ///   The level of nesting.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public int Depth {
                get { return 1; }
            }

            /// <summary>
            ///   Gets a value indicating whether the data reader is closed.
            /// </summary>
            /// <returns>
            ///   true if the data reader is closed; otherwise, false.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public bool IsClosed {
                get {
                    if (_tableView != null) {
                        return _tableView.IsClosed;
                    }
                    return true;
                }
            }

            /// <summary>
            ///   Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
            /// </summary>
            /// <returns>
            ///   The number of rows changed, inserted, or deleted; 0 if no rows were affected or the statement failed; and -1 for SELECT statements.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public int RecordsAffected {
                get { return -1; }
            }

            #endregion
        }

        #endregion
    }
}