using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace excel2json
{
    /// <summary>
    /// 将DataTable对象，转换成Ini string，并保存到文件中
    /// </summary>
    class IniExporter : ExcelExporter
    {
        public override void SerializeObject()
        {
            //-- convert to ini string
            foreach (var sheet in sheetValueList)
            {
                var iniContext = SerializeObjectToString(sheet.Key, sheet.Value);
                mContext += iniContext;
                mContext += string.Format("\r\n\r\n");
                mContextList.Add(sheet.Key, iniContext);
            }
        }

        public override string SerializeObjectToString(string sheetName, object sheetValue)
        {
            string strContext = "";
            strContext += string.Format(";===========================\r\n");
            strContext += string.Format(";{0}\r\n", sheetName);
            strContext += string.Format(";===========================\r\n");
            if (sheetValue is Dictionary<string, object>)
            {
                var sheetData = (Dictionary<string, object>)sheetValue;
                foreach (var obj in sheetData)
                {
                    strContext += string.Format("[{0}]\r\n", obj.Key);
                    if (obj.Value is Dictionary<string, object>)
                    {
                        var filedData = (Dictionary<string, object>)obj.Value;
                        foreach (var filed in filedData)
                        {
                            strContext += string.Format("{0}={1}\r\n", filed.Key, filed.Value);
                        }
                    }
                    strContext += string.Format("\r\n");
                }
            }
            return strContext;
        }
        /// <summary>
        /// 构造函数：完成内部数据创建
        /// </summary>
        /// <param name="excel">ExcelLoader Object</param>
        public IniExporter(ExcelLoader excel, Program.Options mOptions) : base(excel, mOptions)
        {
        }
    }
}
