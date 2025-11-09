using System;
using System.Threading.Tasks;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 数据导入导出服务接口
/// </summary>
public interface IDataImportExportService
{
    /// <summary>
    /// 导出所有核心数据到文件
    /// </summary>
    /// <param name="filePath">导出文件路径</param>
    /// <param name="progress">进度报告（可选）</param>
    /// <returns>导出结果</returns>
    Task<ExportResult> ExportDataAsync(string filePath, IProgress<ExportProgress>? progress = null);

    /// <summary>
    /// 从文件导入数据
    /// </summary>
    /// <param name="filePath">导入文件路径</param>
    /// <param name="options">导入选项</param>
    /// <param name="progress">进度报告（可选）</param>
    /// <returns>导入结果</returns>
    Task<ImportResult> ImportDataAsync(string filePath, ImportOptions options, IProgress<ImportProgress>? progress = null);

    /// <summary>
    /// 验证导入数据的完整性和正确性
    /// </summary>
    /// <param name="filePath">要验证的文件路径</param>
    /// <returns>验证结果</returns>
    Task<ValidationResult> ValidateImportDataAsync(string filePath);
}
