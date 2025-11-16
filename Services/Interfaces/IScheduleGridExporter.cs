using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 排班表格导出接口
/// </summary>
public interface IScheduleGridExporter
{
    /// <summary>
    /// 导出排班表格
    /// </summary>
    /// <param name="gridData">表格数据</param>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的文件字节数组</returns>
    Task<byte[]> ExportAsync(
        ScheduleGridData gridData,
        string format,
        ExportOptions? options = null);

    /// <summary>
    /// 获取支持的导出格式列表
    /// </summary>
    /// <returns>支持的格式列表</returns>
    List<string> GetSupportedFormats();

    /// <summary>
    /// 验证导出格式是否支持
    /// </summary>
    /// <param name="format">格式名称</param>
    /// <returns>是否支持该格式</returns>
    bool IsFormatSupported(string format);
}
