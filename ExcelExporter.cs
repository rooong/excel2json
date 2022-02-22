using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace excel2json
{
    enum ColValueType
    {
        ColValueType_Default = 0,
        ColValueType_String = ColValueType_Default,
        ColValueType_Int = 1,
        ColValueType_Float = 2,
        ColValueType_Double = 3,
    }
    struct ColumnInfo
    {
        public int colNum;
        public string colName;
        public ColValueType valueType;

        public ColumnInfo(int col, string name)
        {
            colNum = col;
            colName = name;
            valueType = ColValueType.ColValueType_Default;
        }
    }

    /// <summary>
    /// 将DataTable对象，转换成string，并保存到文件中
    /// </summary>
    class ExcelExporter
    {
        protected Dictionary<string, string> mContextList = new Dictionary<string, string>();
        protected Dictionary<string, ColValueType> valueTypeNameToInt = new Dictionary<string, ColValueType>();
        protected Dictionary<string, object> sheetValueList = new Dictionary<string, object>();
        protected string mContext = "";
        protected int mHeaderRows = 0;
        protected Program.Options mOptions;

        public string context {
            get {
                return mContext;
            }
        }

        public string ErrorMsg
        {
            get;
            set;
        }

        private void InitValueTypeToInt()
        {
            valueTypeNameToInt.Add("string", ColValueType.ColValueType_String);
            valueTypeNameToInt.Add("int", ColValueType.ColValueType_Int);
            valueTypeNameToInt.Add("float", ColValueType.ColValueType_Float);
            valueTypeNameToInt.Add("double", ColValueType.ColValueType_Double);
        }

        public virtual void SerializeObject()
        {
            //-- convert to ini string
            foreach (var sheet in sheetValueList)
            {
                var objContext = SerializeObjectToString(sheet.Key, sheet.Value);
                mContext += objContext;
                mContextList.Add(sheet.Key, objContext);
            }
        }

        public virtual string SerializeObjectToString(string sheetName, object sheetValue)
        {
            return "";
        }

        /// <summary>
        /// 构造函数：完成内部数据创建
        /// </summary>
        /// <param name="excel">ExcelLoader Object</param>
        public ExcelExporter(ExcelLoader excel, Program.Options mOptions)
        {
            this.mOptions = mOptions;
            InitValueTypeToInt();
            mHeaderRows = mOptions.HeaderRows;
            var excludePrefix = mOptions.ExcludePrefix;
            var dateFormat = mOptions.DateFormat;
            var forceSheetName = mOptions.ForceSheetName;
            var Sheets = excel.Sheets;
            for (int i = 0; i < Sheets.Count; i++)
            {
                DataTable sheet = Sheets[i].table;

                // 过滤掉包含特定前缀的表单
                string sheetName = sheet.TableName;
                if (excludePrefix.Length > 0 && sheetName.StartsWith(excludePrefix))
                    continue;

                if (sheet.Columns.Count > 0 && sheet.Rows.Count > 0)
                {
                    object sheetValue = convertSheet(sheet, Sheets[i].MergeCells, mOptions);
                    sheetValueList.Add(sheet.TableName, sheetValue);
                }
            }
            SerializeObject();
        }

        private Dictionary<int, ColumnInfo> InitSheetColValueType(DataTable sheet, Program.Options mOptions)
        {
            // 初始化列值类型
            Dictionary<int, ColumnInfo> colTypeData = new Dictionary<int, ColumnInfo>();
            if (sheet.Rows.Count >= 2)
            {
                int valueNameRow = mOptions.ColumnNameRow;
                int valueTypeRow = mOptions.ValueTypeRow;
                DataRow colValueNameRow = sheet.Rows[valueNameRow];
                DataRow colValueTypeRow = sheet.Rows[valueTypeRow];
                for (int i = 0; i < sheet.Columns.Count; i++)
                {
                    var colName = colValueNameRow[i].ToString();
                    if (colName.Length <= 0)
                        break;
                    ColumnInfo colInfo = new ColumnInfo();
                    colInfo.colNum = i;
                    colInfo.colName = colName;
                    colInfo.valueType = ColValueType.ColValueType_Default;
                    string cellText = colValueTypeRow[i].ToString();
                    if (valueTypeNameToInt.ContainsKey(cellText))
                        colInfo.valueType = valueTypeNameToInt[cellText];
                    colTypeData.Add(i, colInfo);
                    sheet.Columns[i].ColumnName = colName;
                }
            }
            return colTypeData;
        }

        private object convertSheet(DataTable sheet, ExcelDataReader.CellRange[] MergeCells, Program.Options mOptions)
        {
            Dictionary<int, ColumnInfo> colTypeData = InitSheetColValueType(sheet, mOptions);
            if (mOptions.ExportArray)
                return convertSheetToArray(sheet, MergeCells, colTypeData, mOptions);
            else
                return convertSheetToDict(sheet, MergeCells, colTypeData, mOptions);
        }

        public virtual object convertSheetToArray(DataTable sheet, ExcelDataReader.CellRange[] MergeCells, Dictionary<int, ColumnInfo> colTypeData, Program.Options mOptions)
        {
            List<object> values = new List<object>();

            int firstDataRow = mHeaderRows;
            for (int i = firstDataRow; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];

                values.Add(
                    convertRowToDict(sheet, i, row, firstDataRow, MergeCells, colTypeData, mOptions)
                    );
            }

            return values;
        }

        /// <summary>
        /// 以设定列为ID，转换成ID->Object的字典对象
        /// </summary>
        public virtual object convertSheetToDict(DataTable sheet, ExcelDataReader.CellRange[] MergeCells, Dictionary<int, ColumnInfo> colTypeData, Program.Options mOptions)
        {
            Dictionary<string, object> importData =
                new Dictionary<string, object>();
            
            int keyColumn = mOptions.KeyColumn;
            if (keyColumn < 0 || keyColumn >= sheet.Columns.Count)
            {
                keyColumn = 0;
            }
            int firstDataRow = mHeaderRows;
            for (int i = firstDataRow; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                int realRow = i;
                int realCol = keyColumn;
                if (i > firstDataRow && MergeCells != null)
                {
                    GetMergeCellIndex(i, keyColumn, ref realRow, ref realCol, MergeCells);
                    row = sheet.Rows[realRow];
                }
                string ID = row[sheet.Columns[keyColumn]].ToString();
                if (ID.Length <= 0)
                {
                    ID = string.Format("row_{0}", i);
                    LogUtil.Error(string.Format("row:{0} key column is null", i));
                    if (ErrorMsg == null)
                    {
                        ErrorMsg = "load data something is error";
                    }
                    continue;
                }

                var rowObject = convertRowToDict(sheet, i, row, firstDataRow, MergeCells, colTypeData, mOptions);
                // 多余的字段
                // rowObject[ID] = ID;
                importData[ID] = rowObject;
            }

            return importData;
        }

        /// <summary>
        /// 把一行数据转换成一个对象，每一列是一个属性
        /// </summary>
        public virtual Dictionary<string, object> convertRowToDict(DataTable sheet, int rowNum, DataRow row, int firstDataRow, ExcelDataReader.CellRange[] MergeCells, Dictionary<int, ColumnInfo> colTypeData, Program.Options mOptions)
        {
            var rowData = new Dictionary<string, object>();
            int col = 0;
            var exlucdeColumns = mOptions.excludeColums;
            var excludePrefix = mOptions.ExcludePrefix;
            var cellJson = mOptions.CellJson;
            var allString = mOptions.AllString;
            var lowcase = mOptions.Lowcase;
            foreach (DataColumn column in sheet.Columns)
            {
                // 过滤掉key列
                if (col == mOptions.KeyColumn)
                {
                    col++;
                    continue;
                }
                int realRow = rowNum;
                int realCol = col;
                ColumnInfo colInfo;
                if (colTypeData.ContainsKey(realCol))
                {
                    colInfo = colTypeData[realCol];
                }
                else
                {
                    colInfo = new ColumnInfo(realCol, column.ToString());
                }
                // 过滤掉包含指定前缀的列
                string columnName = colInfo.colName;
                if (exlucdeColumns.Contains(columnName))
                {
                    col++;
                    continue;
                }
                if (excludePrefix.Length > 0 && columnName.StartsWith(excludePrefix))
                {
                    col++;
                    continue;
                }
                if (MergeCells != null)
                {
                    GetMergeCellIndex(rowNum, col, ref realRow, ref realCol, MergeCells);
                }
                DataRow newRow = sheet.Rows[realRow];
                object value = newRow[realCol];

                // 尝试将单元格字符串转换成 Json Array 或者 Json Object
                if (cellJson)
                {
                    string cellText = value.ToString().Trim();
                    if (cellText.StartsWith("[") || cellText.StartsWith("{"))
                    {
                        try
                        {
                            object cellJsonObj = JsonConvert.DeserializeObject(cellText);
                            if (cellJsonObj != null)
                                value = cellJsonObj;
                        }
                        catch (Exception exp)
                        {
                            LogUtil.Error(exp.Message);
                        }
                    }
                }
                
                if (value.GetType() == typeof(System.DBNull))
                {
                    switch (colInfo.valueType)
                    {
                        case ColValueType.ColValueType_Int:
                        case ColValueType.ColValueType_Float:
                        case ColValueType.ColValueType_Double:
                            value = 0;
                            break;
                        case ColValueType.ColValueType_Default:
                            value = "";
                            break;
                    }
                }
                else if (value.GetType() == typeof(double) && colInfo.valueType == ColValueType.ColValueType_Int)
                { // 去掉数值字段的“.0”
                    double num = (double)value;
                    if ((int)num == num)
                        value = (int)num;
                }

                //全部转换为string
                //方便LitJson.JsonMapper.ToObject<List<Dictionary<string, string>>>(textAsset.text)等使用方式 之后根据自己的需求进行解析
                if (allString && !(value is string))
                {
                    value = value.ToString();
                }

                string fieldName = colInfo.colName;
                // 表头自动转换成小写
                if (lowcase)
                    fieldName = fieldName.ToLower();

                if (string.IsNullOrEmpty(fieldName))
                    fieldName = string.Format("col_{0}", col);

                rowData[fieldName] = value;
                col++;
            }

            return rowData;
        }

        public void GetMergeCellIndex(int row, int col, ref int mergeRow, ref int mergeCol, ExcelDataReader.CellRange[] MergeCells)
        {
            mergeRow = row;
            mergeCol = col;
            foreach (var mergeInfo in MergeCells)
            {
                if (row >= mergeInfo.FromRow && row <= mergeInfo.ToRow && col >= mergeInfo.FromColumn && col <= mergeInfo.ToColumn)
                {
                    mergeRow = mergeInfo.FromRow;
                    mergeCol = mergeInfo.FromColumn;
                    break;
                }
            }
        }

        /// <summary>
        /// 对于表格中的空值，找到一列中的非空值，并构造一个同类型的默认值
        /// </summary>
        private object getColumnDefault(DataTable sheet, DataColumn column, int firstDataRow)
        {
            for (int i = firstDataRow; i < sheet.Rows.Count; i++)
            {
                object value = sheet.Rows[i][column];
                Type valueType = value.GetType();
                if (valueType != typeof(System.DBNull))
                {
                    if (valueType.IsValueType)
                        return Activator.CreateInstance(valueType);
                    break;
                }
            }
            return "";
        }
        /// <summary>
        /// 将内部数据转换成文本，并保存至文件
        /// </summary>
        /// <param name="filePath">输出文件路径</param>
        public void SaveToFile(string filePath, Encoding encoding, string context)
        {
            //-- 保存文件
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(file, encoding))
                    writer.Write(context);
            }
        }
        /// <summary>
        /// 将内部数据转换成文本，并保存至文件
        /// </summary>
        /// <param name="filePath">输出文件路径</param>
        public void SaveToFile(string filePath, Encoding encoding, bool forceSheetName)
        {
            //-- 保存文件
            if (!forceSheetName && mContextList.Count == 1)
            {
                SaveToFile(filePath, encoding, mContext);
            }
            else
            {
                string fileExt = Path.GetExtension(filePath);
                string strPrePath = Path.GetFileNameWithoutExtension(filePath);
                foreach (var sheet in mContextList)
                {
                    string newFilePath = string.Format("{0}_{1}.{2}",strPrePath, sheet.Key, fileExt);
                    SaveToFile(newFilePath, encoding, sheet.Value);
                }
            }
        }
    }
}
