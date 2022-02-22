using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

public static class LogUtil
{
    private static log4net.ILog Log { get {return log4net.LogManager.GetLogger("log");} }

    /// <summary>
    /// 日志加载设置
    /// </summary>
    /// <param name="exeConfigFile">日志配置文件名称</param>
    /// <param name="day">保留天数，-1表示不删除</param>
    public static void Configure(string exeConfigFile, int day = -1)
    {
        log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(exeConfigFile));
        if (day == -1) return;
        var files = new System.IO.DirectoryInfo("log").GetFiles();
        foreach (var file in files)
        {
            // 定时删除日志文件
            if ((DateTime.Now - file.CreationTime).TotalDays > day)
            {
                file.Delete();
            }
        }
    }

    public static void Configure(int day = -1)
    {
        log4net.Config.XmlConfigurator.Configure();
        if (day == -1) return;
        var files = new System.IO.DirectoryInfo("log").GetFiles();
        foreach (var file in files)
        {
            // 定时删除日志文件
            if ((DateTime.Now - file.CreationTime).TotalDays > day)
            {
                file.Delete();
            }
        }
    }

    private static string WrapException(string msg, Exception e)
    {
        var builder = new StringBuilder(msg);
        builder.Append("\t[").Append(e.Message).Append("]");
        if (e.InnerException != null)
        {
            builder.Append(" --> [").Append(e.InnerException.Message).Append("]");
        }

        return builder.ToString();
    }

    public static void Debug(string msg)
    {
        Log.Debug(msg);
    }
    public static void Debug(string msg, Exception e)
    {
        Log.Debug(WrapException(msg, e), e);
    }

    public static void Info(string msg)
    {
        Log.Info(msg);
    }
    public static void Info(string msg, Exception e)
    {
        Log.Info(WrapException(msg, e), e);
    }

    public static void Warn(string msg)
    {
        Log.Warn(msg);
    }
    public static void Warn(string msg, Exception e)
    {
        Log.Warn(WrapException(msg, e), e);
    }

    public static void Error(string msg)
    {
        Log.Error(msg);
    }
    public static void Error(string msg, Exception e)
    {
        Log.Error(WrapException(msg, e), e);
    }

    public static void Fatal(string msg)
    {
        Log.Fatal(msg);
    }
    public static void Fatal(string msg, Exception e)
    {
        Log.Fatal(WrapException(msg, e), e);
    }
}