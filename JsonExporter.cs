using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace excel2json
{
    /// <summary>
    /// 将DataTable对象，转换成JSON string，并保存到文件中
    /// </summary>
    class JsonExporter : ExcelExporter
    {
        public override void SerializeObject()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                DateFormatString = mOptions.DateFormat,
                Formatting = Formatting.Indented
            };
            //-- convert to json string
            mContext = JsonConvert.SerializeObject(sheetValueList, jsonSettings);
        }
        /// <summary>
        /// 构造函数：完成内部数据创建
        /// </summary>
        /// <param name="excel">ExcelLoader Object</param>
        public JsonExporter(ExcelLoader excel, Program.Options mOptions) : base(excel, mOptions)
        {
        }

        public override string SerializeObjectToString(string sheetName, object sheetValue)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                DateFormatString = mOptions.DateFormat,
                Formatting = Formatting.Indented
            };
            //-- convert to json string
            string strContext = JsonConvert.SerializeObject(sheetValue, jsonSettings);
            return strContext;
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
        /// 将内部数据转换成Json文本，并保存至文件
        /// </summary>
        /// <param name="jsonPath">输出文件路径</param>
        public void SaveToFile(string filePath, Encoding encoding)
        {
            //-- 保存文件
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(file, encoding))
                    writer.Write(mContext);
            }
        }
    }
}
