using System;
using System.Collections.Generic;

namespace AutoScheduling3.Services.ImportExport;

/// <summary>
/// 将技术错误消息转换为用户友好的错误消息
/// </summary>
public static class ErrorMessageTranslator
{
    /// <summary>
    /// 将异常转换为用户友好的错误消息
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <param name="context">错误上下文（如"导出"、"导入"等）</param>
    /// <returns>用户友好的错误消息</returns>
    public static string TranslateException(Exception exception, string context)
    {
        if (exception == null)
        {
            return "发生未知错误";
        }

        // 根据异常类型返回友好消息
        return exception switch
        {
            System.IO.FileNotFoundException => $"{context}失败：找不到指定的文件。请确认文件路径是否正确。",
            System.IO.DirectoryNotFoundException => $"{context}失败：找不到指定的目录。请确认目录路径是否正确。",
            System.IO.IOException ioEx when ioEx.Message.Contains("being used by another process") => 
                $"{context}失败：文件正在被其他程序使用。请关闭相关程序后重试。",
            System.IO.IOException => $"{context}失败：文件操作错误。请检查文件权限和磁盘空间。",
            UnauthorizedAccessException => $"{context}失败：没有访问文件的权限。请以管理员身份运行或检查文件权限。",
            System.Text.Json.JsonException => $"{context}失败：文件格式不正确。请确认这是一个有效的导出文件。",
            InvalidOperationException invEx when invEx.Message.Contains("反序列化") => 
                $"{context}失败：无法解析文件内容。文件可能已损坏或格式不兼容。",
            ArgumentException argEx when argEx.ParamName == "filePath" => 
                $"{context}失败：文件路径无效。请选择一个有效的文件路径。",
            Microsoft.Data.Sqlite.SqliteException sqlEx => TranslateSqliteException(sqlEx, context),
            OutOfMemoryException => $"{context}失败：内存不足。请关闭其他程序后重试，或尝试处理较小的数据集。",
            _ => $"{context}失败：{exception.Message}"
        };
    }

    /// <summary>
    /// 翻译 SQLite 异常
    /// </summary>
    private static string TranslateSqliteException(Microsoft.Data.Sqlite.SqliteException sqlEx, string context)
    {
        // SQLite 错误代码参考：https://www.sqlite.org/rescode.html
        return sqlEx.SqliteErrorCode switch
        {
            1 => $"{context}失败：数据库操作错误。可能是数据格式不正确或违反了数据约束。",
            5 => $"{context}失败：数据库被锁定。请稍后重试。",
            8 => $"{context}失败：数据库操作被中断。",
            11 => $"{context}失败：数据库文件已损坏。请尝试从备份恢复。",
            13 => $"{context}失败：数据库已满。请清理磁盘空间。",
            14 => $"{context}失败：无法打开数据库文件。",
            19 => $"{context}失败：数据违反了唯一性约束。可能存在重复的记录。",
            23 => $"{context}失败：数据库访问被拒绝。",
            _ => $"{context}失败：数据库错误 (代码: {sqlEx.SqliteErrorCode})。{sqlEx.Message}"
        };
    }

    /// <summary>
    /// 获取错误的详细技术信息（用于日志记录）
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <returns>详细的技术错误信息</returns>
    public static string GetDetailedErrorInfo(Exception exception)
    {
        if (exception == null)
        {
            return "No exception information available";
        }

        var details = new List<string>
        {
            $"Exception Type: {exception.GetType().FullName}",
            $"Message: {exception.Message}",
            $"Source: {exception.Source}",
            $"Stack Trace: {exception.StackTrace}"
        };

        if (exception.InnerException != null)
        {
            details.Add("--- Inner Exception ---");
            details.Add(GetDetailedErrorInfo(exception.InnerException));
        }

        return string.Join(Environment.NewLine, details);
    }
}
