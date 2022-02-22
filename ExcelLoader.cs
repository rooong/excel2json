using System;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Collections.Generic;

namespace excel2json {
    /// <summary>
    /// 将 Excel 文件(*.xls 或者 *.xlsx)加载到内存 DataSet
    /// </summary>
    struct DataTableInfo
    {
        public DataTable table;
        public ExcelDataReader.CellRange[] MergeCells;
    }
    class ExcelLoader {
        private List<DataTableInfo> listDataTable = new List<DataTableInfo>();
        // TODO: add Sheet Struct Define

        public ExcelLoader(string filePath, int headerRow, bool bUseHeaderRow) {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    // Use the AsDataSet extension method
                    // The result of each spreadsheet is in result.Tables
                    var result = reader.AsDataSet(createDataSetReadConfig(headerRow, bUseHeaderRow));
                    foreach (DataTable table in result.Tables)
                    {
                        DataTableInfo info = new DataTableInfo();
                        info.table = table;
                        info.MergeCells = reader.MergeCells;
                        this.listDataTable.Add(info);
                        if (!reader.NextResult())
                        {
                            break;
                        }
                    }
                }
            }

            if (this.Sheets.Count < 1) {
                throw new Exception("Excel file is empty: " + filePath);
            }
        }

        public List<DataTableInfo> Sheets {
            get {
                return this.listDataTable;
            }
        }

        private ExcelDataSetConfiguration createDataSetReadConfig(int headerRow, bool bUseHeaderRow) {
            var tableConfig = new ExcelDataTableConfiguration() {
                // Gets or sets a value indicating whether to use a row from the 
                // data as column names.
                UseHeaderRow = bUseHeaderRow,

                // Gets or sets a callback to determine whether to include the 
                // current row in the DataTable.
                //FilterRow = (rowReader) => {
                //    return rowReader.Depth > headerRow - 1;
                //},
            };

            return new ExcelDataSetConfiguration() {
                // Gets or sets a value indicating whether to set the DataColumn.DataType
                // property in a second pass.
                UseColumnDataType = true,

                // Gets or sets a callback to obtain configuration options for a DataTable. 
                ConfigureDataTable = (tableReader) => { return tableConfig; },
            };
        }
    }
}
