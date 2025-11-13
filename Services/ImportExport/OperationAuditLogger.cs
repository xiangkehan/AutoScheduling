using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.Services.ImportExport;

/// <summary>
/// 操作审计日志记录器
/// </summary>
public class OperationAuditLogger
{
    private readonly string _auditLogPath;
    private readonly object _lockObject = new object();

    /// <summary>
    /// 初始化审计日志记录器
    /// </summary>
    /// <param name="auditLogDirectory">审计日志目录（可选，默认为用户文档目录）</param>
    public OperationAuditLogger(string? auditLogDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(auditLogDirectory))
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            auditLogDirectory = Path.Combine(documentsPath, "AutoScheduling3", "AuditLogs");
        }

        Directory.CreateDirectory(auditLogDirectory);
        _auditLogPath = Path.Combine(auditLogDirectory, $"audit_{DateTime.Now:yyyyMMdd}.log");
    }

    /// <summary>
    /// 记录导出操作
    /// </summary>
    /// <param name="result">导出结果</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogExportOperation(ExportResult result, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = "Export",
            UserName = userName ?? Environment.UserName,
            Success = result.Success,
            Details = new Dictionary<string, object>
            {
                { "FilePath", result.FilePath ?? "N/A" },
                { "FileSize", result.FileSize },
                { "Duration", result.Duration.TotalSeconds },
                { "SkillCount", result.Statistics?.SkillCount ?? 0 },
                { "PersonnelCount", result.Statistics?.PersonnelCount ?? 0 },
                { "PositionCount", result.Statistics?.PositionCount ?? 0 },
                { "TemplateCount", result.Statistics?.TemplateCount ?? 0 },
                { "ConstraintCount", result.Statistics?.ConstraintCount ?? 0 }
            },
            ErrorMessage = result.ErrorMessage
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录导入操作
    /// </summary>
    /// <param name="filePath">导入文件路径</param>
    /// <param name="options">导入选项</param>
    /// <param name="result">导入结果</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogImportOperation(string filePath, ImportOptions options, ImportResult result, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = "Import",
            UserName = userName ?? Environment.UserName,
            Success = result.Success,
            Details = new Dictionary<string, object>
            {
                { "FilePath", filePath },
                { "Strategy", options.Strategy.ToString() },
                { "CreateBackup", options.CreateBackupBeforeImport },
                { "ValidateReferences", options.ValidateReferences },
                { "Duration", result.Duration.TotalSeconds },
                { "TotalRecords", result.Statistics?.TotalRecords ?? 0 },
                { "ImportedRecords", result.Statistics?.ImportedRecords ?? 0 },
                { "SkippedRecords", result.Statistics?.SkippedRecords ?? 0 },
                { "FailedRecords", result.Statistics?.FailedRecords ?? 0 },
                { "WarningCount", result.Warnings?.Count ?? 0 }
            },
            ErrorMessage = result.ErrorMessage
        };

        if (result.Statistics?.RecordsByTable != null)
        {
            foreach (var kvp in result.Statistics.RecordsByTable)
            {
                auditEntry.Details[$"Imported_{kvp.Key}"] = kvp.Value;
            }
        }

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录验证操作
    /// </summary>
    /// <param name="filePath">验证文件路径</param>
    /// <param name="result">验证结果</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogValidationOperation(string filePath, ValidationResult result, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = "Validation",
            UserName = userName ?? Environment.UserName,
            Success = result.IsValid,
            Details = new Dictionary<string, object>
            {
                { "FilePath", filePath },
                { "ErrorCount", result.Errors?.Count ?? 0 },
                { "WarningCount", result.Warnings?.Count ?? 0 }
            },
            ErrorMessage = result.IsValid ? null : $"验证失败，共 {result.Errors?.Count ?? 0} 个错误"
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录错误操作
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="exception">异常</param>
    /// <param name="context">上下文信息</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogErrorOperation(string operationType, Exception exception, string? context = null, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = operationType,
            UserName = userName ?? Environment.UserName,
            Success = false,
            Details = new Dictionary<string, object>
            {
                { "ExceptionType", exception.GetType().Name },
                { "Context", context ?? "N/A" }
            },
            ErrorMessage = exception.Message
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录操作开始
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="context">上下文信息</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogOperationStart(string operationType, string? context = null, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = $"{operationType}_START",
            UserName = userName ?? Environment.UserName,
            Success = true,
            Details = new Dictionary<string, object>
            {
                { "Context", context ?? "N/A" }
            }
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录操作结束
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="success">是否成功</param>
    /// <param name="message">消息</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogOperationEnd(string operationType, bool success, string? message = null, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = $"{operationType}_END",
            UserName = userName ?? Environment.UserName,
            Success = success,
            Details = new Dictionary<string, object>
            {
                { "Message", message ?? "N/A" }
            }
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录事务开始
    /// </summary>
    /// <param name="userName">用户名（可选）</param>
    public void LogTransactionStart(string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = "TRANSACTION_START",
            UserName = userName ?? Environment.UserName,
            Success = true,
            Details = new Dictionary<string, object>
            {
                { "Action", "Database transaction started" }
            }
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录事务提交
    /// </summary>
    /// <param name="userName">用户名（可选）</param>
    public void LogTransactionCommit(string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = "TRANSACTION_COMMIT",
            UserName = userName ?? Environment.UserName,
            Success = true,
            Details = new Dictionary<string, object>
            {
                { "Action", "Database transaction committed successfully" }
            }
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 记录事务回滚
    /// </summary>
    /// <param name="reason">回滚原因</param>
    /// <param name="userName">用户名（可选）</param>
    public void LogTransactionRollback(string? reason = null, string? userName = null)
    {
        var auditEntry = new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            OperationType = "TRANSACTION_ROLLBACK",
            UserName = userName ?? Environment.UserName,
            Success = false,
            Details = new Dictionary<string, object>
            {
                { "Action", "Database transaction rolled back" },
                { "Reason", reason ?? "Unknown error" }
            }
        };

        WriteAuditLog(auditEntry);
    }

    /// <summary>
    /// 写入审计日志
    /// </summary>
    private void WriteAuditLog(AuditLogEntry entry)
    {
        try
        {
            lock (_lockObject)
            {
                var logLine = FormatAuditEntry(entry);
                File.AppendAllText(_auditLogPath, logLine + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            // 如果无法写入审计日志，至少输出到调试窗口
            System.Diagnostics.Debug.WriteLine($"Failed to write audit log: {ex.Message}");
        }
    }

    /// <summary>
    /// 格式化审计日志条目
    /// </summary>
    private string FormatAuditEntry(AuditLogEntry entry)
    {
        var sb = new StringBuilder();
        sb.Append($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
        sb.Append($"[{entry.OperationType}] ");
        sb.Append($"[User: {entry.UserName}] ");
        sb.Append($"[Status: {(entry.Success ? "SUCCESS" : "FAILED")}] ");

        if (entry.Details != null && entry.Details.Count > 0)
        {
            var detailsJson = JsonSerializer.Serialize(entry.Details, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
            sb.Append($"[Details: {detailsJson}] ");
        }

        if (!string.IsNullOrWhiteSpace(entry.ErrorMessage))
        {
            sb.Append($"[Error: {entry.ErrorMessage}]");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 审计日志条目
    /// </summary>
    private class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public Dictionary<string, object>? Details { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
