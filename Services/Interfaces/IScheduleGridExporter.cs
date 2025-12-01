using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 排班表格导出接口
/// </summary>
public interface IScheduleGridExporter
{
    /// <summary>
    /// 导出排班表格（Grid View）
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
    /// 导出单个哨位的排班表（Position View）
    /// </summary>
    /// <param name="positionSchedule">哨位排班数据</param>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的文件字节数组</returns>
    Task<byte[]> ExportPositionScheduleAsync(
        PositionScheduleData positionSchedule,
        string format,
        ExportOptions? options = null);

    /// <summary>
    /// 导出单个人员的排班表（Personnel View）
    /// </summary>
    /// <param name="personnelSchedule">人员排班数据</param>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的文件字节数组</returns>
    Task<byte[]> ExportPersonnelScheduleAsync(
        PersonnelScheduleData personnelSchedule,
        string format,
        ExportOptions? options = null);

    /// <summary>
    /// 导出班次列表（List View）
    /// </summary>
    /// <param name="shiftList">班次列表数据</param>
    /// <param name="format">导出格式（如 "excel", "csv"）</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的文件字节数组</returns>
    Task<byte[]> ExportShiftListAsync(
        IEnumerable<ShiftListItem> shiftList,
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
