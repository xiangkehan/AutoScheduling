using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 冲突报告服务接口
/// </summary>
public interface IConflictReportService
{
    /// <summary>
    /// 导出冲突报告
    /// </summary>
    /// <param name="conflicts">冲突列表</param>
    /// <param name="schedule">排班数据</param>
    /// <param name="format">导出格式（excel/pdf）</param>
    /// <returns>报告文件字节数组</returns>
    Task<byte[]> ExportConflictReportAsync(
        List<ConflictDto> conflicts, 
        ScheduleDto schedule, 
        string format);

    /// <summary>
    /// 生成冲突趋势数据
    /// </summary>
    /// <param name="scheduleId">排班ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>趋势数据</returns>
    Task<ConflictTrendData> GenerateTrendDataAsync(
        int scheduleId, 
        DateTime startDate, 
        DateTime endDate);
}
