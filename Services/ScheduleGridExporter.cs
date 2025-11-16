using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services;

/// <summary>
/// 排班表格导出服务（预留实现）
/// </summary>
public class ScheduleGridExporter : IScheduleGridExporter
{
    /// <summary>
    /// 导出排班表格
    /// </summary>
    /// <param name="gridData">表格数据</param>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的文件字节数组</returns>
    public Task<byte[]> ExportAsync(
        ScheduleGridData gridData,
        string format,
        ExportOptions? options = null)
    {
        // 当前版本抛出未实现异常
        throw new NotImplementedException(
            $"表格导出功能正在开发中。计划支持的格式：{string.Join(", ", GetSupportedFormats())}");
    }

    /// <summary>
    /// 获取支持的导出格式列表
    /// </summary>
    public List<string> GetSupportedFormats()
    {
        // 预留支持的格式列表
        return new List<string> { "excel", "csv", "pdf" };
    }

    /// <summary>
    /// 验证导出格式是否支持
    /// </summary>
    public bool IsFormatSupported(string format)
    {
        return GetSupportedFormats().Contains(format.ToLower());
    }
}
