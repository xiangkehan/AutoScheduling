using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Services.ImportExport;

/// <summary>
/// 为错误提供恢复建议
/// </summary>
public static class ErrorRecoverySuggester
{
    /// <summary>
    /// 根据异常类型生成恢复建议
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <param name="context">错误上下文（如"导出"、"导入"等）</param>
    /// <returns>恢复建议列表</returns>
    public static List<string> GetRecoverySuggestions(Exception exception, string context)
    {
        if (exception == null)
        {
            return new List<string> { "请联系技术支持获取帮助。" };
        }

        return exception switch
        {
            System.IO.FileNotFoundException => new List<string>
            {
                "确认文件路径是否正确",
                "检查文件是否已被移动或删除",
                "尝试重新选择文件"
            },
            
            System.IO.DirectoryNotFoundException => new List<string>
            {
                "确认目录路径是否正确",
                "检查目录是否已被移动或删除",
                "尝试创建目录或选择其他位置"
            },
            
            System.IO.IOException ioEx when ioEx.Message.Contains("being used by another process") => new List<string>
            {
                "关闭可能正在使用该文件的程序（如 Excel、记事本等）",
                "等待几秒钟后重试",
                "重启应用程序",
                "选择不同的文件名或位置"
            },
            
            System.IO.IOException => new List<string>
            {
                "检查磁盘空间是否充足",
                "确认对文件和目录有读写权限",
                "尝试选择不同的保存位置",
                "以管理员身份运行应用程序"
            },
            
            UnauthorizedAccessException => new List<string>
            {
                "以管理员身份运行应用程序",
                "检查文件和目录的权限设置",
                "确认文件未被设置为只读",
                "选择有写入权限的其他位置"
            },
            
            System.Text.Json.JsonException => new List<string>
            {
                "确认选择的是有效的导出文件",
                "检查文件是否完整（未被截断或损坏）",
                "尝试重新导出数据",
                "使用文本编辑器检查 JSON 格式是否正确"
            },
            
            InvalidOperationException invEx when invEx.Message.Contains("反序列化") => new List<string>
            {
                "确认文件是由相同或兼容版本的应用程序导出的",
                "检查文件是否完整",
                "尝试使用备份文件",
                "联系技术支持获取文件格式转换工具"
            },
            
            Microsoft.Data.Sqlite.SqliteException sqlEx => GetSqliteRecoverySuggestions(sqlEx, context),
            
            OutOfMemoryException => new List<string>
            {
                "关闭其他正在运行的程序以释放内存",
                "重启应用程序",
                "如果数据量很大，考虑分批处理",
                "增加系统内存"
            },
            
            _ => new List<string>
            {
                "检查错误消息中的详细信息",
                "尝试重新执行操作",
                "重启应用程序",
                "如果问题持续存在，请联系技术支持"
            }
        };
    }

    /// <summary>
    /// 获取 SQLite 异常的恢复建议
    /// </summary>
    private static List<string> GetSqliteRecoverySuggestions(Microsoft.Data.Sqlite.SqliteException sqlEx, string context)
    {
        return sqlEx.SqliteErrorCode switch
        {
            1 => new List<string>
            {
                "检查导入数据的格式是否正确",
                "确认数据符合系统要求",
                "查看详细错误日志了解具体问题"
            },
            
            5 => new List<string>
            {
                "等待几秒钟后重试",
                "关闭其他可能正在访问数据库的程序",
                "重启应用程序"
            },
            
            11 => new List<string>
            {
                "从最近的备份恢复数据库",
                "使用数据库修复工具",
                "联系技术支持获取帮助"
            },
            
            13 => new List<string>
            {
                "清理磁盘空间",
                "删除不需要的文件",
                "将数据库移动到有更多空间的磁盘"
            },
            
            19 => new List<string>
            {
                "检查导入数据中是否有重复记录",
                "使用'跳过'或'合并'策略而不是'覆盖'策略",
                "手动清理重复数据后重试"
            },
            
            _ => new List<string>
            {
                "检查数据库文件是否完整",
                "尝试从备份恢复",
                "重启应用程序",
                "联系技术支持"
            }
        };
    }

    /// <summary>
    /// 格式化恢复建议为用户友好的字符串
    /// </summary>
    /// <param name="suggestions">建议列表</param>
    /// <returns>格式化的建议字符串</returns>
    public static string FormatSuggestions(List<string> suggestions)
    {
        if (suggestions == null || !suggestions.Any())
        {
            return string.Empty;
        }

        return "建议的解决方法：\n" + string.Join("\n", suggestions.Select((s, i) => $"{i + 1}. {s}"));
    }
}
