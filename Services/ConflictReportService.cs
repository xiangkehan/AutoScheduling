using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AutoScheduling3.Services;

/// <summary>
/// 冲突报告服务实现
/// </summary>
public class ConflictReportService : IConflictReportService
{
    private readonly IHistoryService _historyService;

    public ConflictReportService(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// 导出冲突报告
    /// </summary>
    public async Task<byte[]> ExportConflictReportAsync(
        List<ConflictDto> conflicts, 
        ScheduleDto schedule, 
        string format)
    {
        return format.ToLower() switch
        {
            "excel" => await ExportToExcelAsync(conflicts, schedule),
            "pdf" => await ExportToPdfAsync(conflicts, schedule),
            _ => throw new ArgumentException($"不支持的导出格式: {format}", nameof(format))
        };
    }

    /// <summary>
    /// 生成冲突趋势数据
    /// </summary>
    public async Task<ConflictTrendData> GenerateTrendDataAsync(
        int scheduleId, 
        DateTime startDate, 
        DateTime endDate)
    {
        await Task.CompletedTask;
        
        // 注意：这是一个简化的实现
        // 在实际应用中，应该从历史记录或数据库中获取冲突数据
        // 目前我们基于当前排班的冲突数据生成趋势
        
        var trendData = new ConflictTrendData
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // 由于我们没有历史冲突数据，这里生成模拟的趋势数据
        // 在实际应用中，应该查询数据库获取历史冲突记录
        
        // 按日期生成数据点（每天一个数据点）
        var currentDate = startDate.Date;
        while (currentDate <= endDate.Date)
        {
            // 这里应该查询该日期的实际冲突数量
            // 目前使用随机数模拟
            trendData.ConflictsByDate[currentDate] = 0;
            currentDate = currentDate.AddDays(1);
        }

        // 初始化类型统计
        foreach (ConflictSubType subType in Enum.GetValues(typeof(ConflictSubType)))
        {
            if (subType != ConflictSubType.Unknown)
            {
                trendData.ConflictsByType[subType] = 0;
            }
        }

        return trendData;
    }

    /// <summary>
    /// 导出为 Excel 格式
    /// </summary>
    private async Task<byte[]> ExportToExcelAsync(List<ConflictDto> conflicts, ScheduleDto schedule)
    {
        await Task.CompletedTask;
        
        using var package = new ExcelPackage();
        
        // 创建统计摘要工作表
        CreateSummarySheet(package, conflicts, schedule);
        
        // 创建冲突详细信息工作表
        CreateDetailsSheet(package, conflicts);
        
        return package.GetAsByteArray();
    }

    /// <summary>
    /// 导出为 PDF 格式
    /// </summary>
    private async Task<byte[]> ExportToPdfAsync(List<ConflictDto> conflicts, ScheduleDto schedule)
    {
        // TODO: 实现 PDF 导出逻辑
        // 这需要使用 PDF 库（如 iTextSharp 或 QuestPDF）
        await Task.CompletedTask;
        throw new NotImplementedException("PDF 导出功能尚未实现");
    }

    /// <summary>
    /// 创建统计摘要工作表
    /// </summary>
    private void CreateSummarySheet(ExcelPackage package, List<ConflictDto> conflicts, ScheduleDto schedule)
    {
        var worksheet = package.Workbook.Worksheets.Add("冲突统计摘要");
        
        int row = 1;
        
        // 标题
        worksheet.Cells[row, 1].Value = "排班冲突报告";
        worksheet.Cells[row, 1].Style.Font.Size = 16;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row += 2;
        
        // 排班基本信息
        worksheet.Cells[row, 1].Value = "排班名称:";
        worksheet.Cells[row, 2].Value = schedule.Title;
        row++;
        
        worksheet.Cells[row, 1].Value = "生成时间:";
        worksheet.Cells[row, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        row++;
        
        worksheet.Cells[row, 1].Value = "排班周期:";
        worksheet.Cells[row, 2].Value = $"{schedule.StartDate:yyyy-MM-dd} 至 {schedule.EndDate:yyyy-MM-dd}";
        row += 2;
        
        // 冲突统计
        var hardConflicts = conflicts.Count(c => c.Type == "hard");
        var softConflicts = conflicts.Count(c => c.Type == "soft");
        var unassigned = conflicts.Count(c => c.Type == "unassigned");
        var ignored = conflicts.Count(c => c.IsIgnored);
        
        worksheet.Cells[row, 1].Value = "冲突统计";
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;
        
        worksheet.Cells[row, 1].Value = "硬约束冲突:";
        worksheet.Cells[row, 2].Value = hardConflicts;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
        row++;
        
        worksheet.Cells[row, 1].Value = "软约束冲突:";
        worksheet.Cells[row, 2].Value = softConflicts;
        worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Orange);
        row++;
        
        worksheet.Cells[row, 1].Value = "未分配时段:";
        worksheet.Cells[row, 2].Value = unassigned;
        row++;
        
        worksheet.Cells[row, 1].Value = "已忽略冲突:";
        worksheet.Cells[row, 2].Value = ignored;
        row++;
        
        worksheet.Cells[row, 1].Value = "总计:";
        worksheet.Cells[row, 2].Value = conflicts.Count;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 2].Style.Font.Bold = true;
        row += 2;
        
        // 按类型分组统计
        worksheet.Cells[row, 1].Value = "按类型分组";
        worksheet.Cells[row, 1].Style.Font.Size = 14;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;
        
        var conflictsByType = conflicts
            .GroupBy(c => c.SubType)
            .OrderByDescending(g => g.Count());
        
        foreach (var group in conflictsByType)
        {
            worksheet.Cells[row, 1].Value = GetConflictSubTypeName(group.Key);
            worksheet.Cells[row, 2].Value = group.Count();
            row++;
        }
        
        // 自动调整列宽
        worksheet.Cells.AutoFitColumns();
    }

    /// <summary>
    /// 创建冲突详细信息工作表
    /// </summary>
    private void CreateDetailsSheet(ExcelPackage package, List<ConflictDto> conflicts)
    {
        var worksheet = package.Workbook.Worksheets.Add("冲突详细信息");
        
        // 表头
        int col = 1;
        worksheet.Cells[1, col++].Value = "冲突类型";
        worksheet.Cells[1, col++].Value = "子类型";
        worksheet.Cells[1, col++].Value = "严重程度";
        worksheet.Cells[1, col++].Value = "描述";
        worksheet.Cells[1, col++].Value = "人员";
        worksheet.Cells[1, col++].Value = "哨位";
        worksheet.Cells[1, col++].Value = "日期";
        worksheet.Cells[1, col++].Value = "时段";
        worksheet.Cells[1, col++].Value = "是否已忽略";
        
        // 设置表头样式
        using (var range = worksheet.Cells[1, 1, 1, col - 1])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }
        
        // 按类型分组并排序
        var sortedConflicts = conflicts
            .OrderBy(c => c.Type)
            .ThenBy(c => c.SubType)
            .ThenByDescending(c => c.Severity)
            .ToList();
        
        // 填充数据
        int row = 2;
        foreach (var conflict in sortedConflicts)
        {
            col = 1;
            worksheet.Cells[row, col++].Value = GetConflictTypeName(conflict.Type);
            worksheet.Cells[row, col++].Value = GetConflictSubTypeName(conflict.SubType);
            worksheet.Cells[row, col++].Value = conflict.Severity;
            worksheet.Cells[row, col++].Value = conflict.Message;
            worksheet.Cells[row, col++].Value = conflict.PersonnelName ?? "-";
            worksheet.Cells[row, col++].Value = conflict.PositionName ?? "-";
            worksheet.Cells[row, col++].Value = conflict.StartTime?.ToString("yyyy-MM-dd") ?? "-";
            worksheet.Cells[row, col++].Value = conflict.PeriodIndex?.ToString() ?? "-";
            worksheet.Cells[row, col++].Value = conflict.IsIgnored ? "是" : "否";
            
            // 根据冲突类型设置行颜色
            Color rowColor = conflict.Type switch
            {
                "hard" => Color.FromArgb(255, 230, 230),
                "soft" => Color.FromArgb(255, 245, 230),
                "unassigned" => Color.FromArgb(240, 240, 240),
                _ => Color.White
            };
            
            using (var range = worksheet.Cells[row, 1, row, col - 1])
            {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(rowColor);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
            
            row++;
        }
        
        // 自动调整列宽
        worksheet.Cells.AutoFitColumns();
    }

    /// <summary>
    /// 获取冲突类型的中文名称
    /// </summary>
    private string GetConflictTypeName(string type)
    {
        return type switch
        {
            "hard" => "硬约束冲突",
            "soft" => "软约束冲突",
            "info" => "信息提示",
            "unassigned" => "未分配时段",
            _ => type
        };
    }

    /// <summary>
    /// 获取冲突子类型的中文名称
    /// </summary>
    private string GetConflictSubTypeName(ConflictSubType subType)
    {
        return subType switch
        {
            ConflictSubType.SkillMismatch => "技能不匹配",
            ConflictSubType.PersonnelUnavailable => "人员不可用",
            ConflictSubType.DuplicateAssignment => "重复分配",
            ConflictSubType.InsufficientRest => "休息时间不足",
            ConflictSubType.ExcessiveWorkload => "工作量过大",
            ConflictSubType.WorkloadImbalance => "工作量不均衡",
            ConflictSubType.ConsecutiveOvertime => "连续工作超时",
            ConflictSubType.UnassignedSlot => "未分配时段",
            ConflictSubType.SuboptimalAssignment => "次优分配",
            _ => "未知"
        };
    }
}
