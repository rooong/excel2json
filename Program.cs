using System;
using System.IO;
using System.Data;
using System.Text;
using System.Windows.Forms;


namespace excel2json
{
    /// <summary>
    /// 应用程序
    /// </summary>
    sealed partial class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        /// <summary>
        /// 应用程序入口
        /// </summary>
        /// <param name="args">命令行参数</param>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                LogUtil.Configure();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                LogUtil.Error("程序加载失败", ex);
            }
            if (args.Length <= 0)
            {
                //-- GUI MODE ----------------------------------------------------------
                Console.WriteLine("Launch excel2json GUI Mode...");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new GUI.MainForm());
            }
            else
            {
                //-- COMMAND LINE MODE -------------------------------------------------

                //-- 分析命令行参数
                var options = new Options();
                var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);

                if (parser.ParseArgumentsStrict(args, options, () => Environment.Exit(-1)))
                {
                    options.AfterSetOpt();
                    //-- 执行导出操作
                    try
                    {
                        DateTime startTime = DateTime.Now;
                        Run(options);
                        //-- 程序计时
                        DateTime endTime = DateTime.Now;
                        TimeSpan dur = endTime - startTime;
                        Console.WriteLine(
                            string.Format("[{0}]：\tConversion complete in [{1}ms].",
                            Path.GetFileName(options.ExcelPath),
                            dur.TotalMilliseconds)
                            );
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Error: " + exp.Message);
                    }
                }
            }// end of else
        }

        /// <summary>
        /// 根据命令行参数，执行Excel数据导出工作
        /// </summary>
        /// <param name="options">命令行参数</param>
        private static void Run(Options options)
        {
            //-- Excel File 
            string excelPath = options.ExcelPath;
            string excelName = Path.GetFileNameWithoutExtension(options.ExcelPath);

            //-- Header
            int header = options.HeaderRows;

            //-- Encoding
            Encoding cd = new UTF8Encoding(false);
            if (options.Encoding != "utf8-nobom")
            {
                foreach (EncodingInfo ei in Encoding.GetEncodings())
                {
                    Encoding e = ei.GetEncoding();
                    if (e.HeaderName == options.Encoding)
                    {
                        cd = e;
                        break;
                    }
                }
            }

            //-- Date Format
            string dateFormat = options.DateFormat;

            //-- Export path
            string exportJsonPath, exportIniPath;
            if (options.JsonPath != null && options.JsonPath.Length > 0)
            {
                exportJsonPath = options.JsonPath;
            }
            else
            {
                exportJsonPath = Path.ChangeExtension(excelPath, ".json");
            }
            if (options.IniPath != null && options.IniPath.Length > 0)
            {
                exportIniPath = options.IniPath;
            }
            else
            {
                exportIniPath = Path.ChangeExtension(excelPath, ".ini");
            }

            //-- Load Excel
            ExcelLoader excel = new ExcelLoader(excelPath, header, false);

            //-- export json
            JsonExporter json_exporter = new JsonExporter(excel, options);
            json_exporter.SaveToFile(exportJsonPath, cd);

            //-- export ini
            IniExporter ini_exporter = new IniExporter(excel, options);
            ini_exporter.SaveToFile(exportIniPath, cd, options.ForceSheetName);

            //-- 生成C#定义文件
            if (options.CSharpPath != null && options.CSharpPath.Length > 0)
            {
                CSDefineGenerator generator = new CSDefineGenerator(excelName, excel, options);
                generator.SaveToFile(options.CSharpPath, cd);
            }
        }
    }
}
