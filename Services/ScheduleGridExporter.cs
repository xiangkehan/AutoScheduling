using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AutoScheduling3.Services;

/// <summary>
/// 排班表格导出服务
/// </summary>
public class ScheduleGridExporter : IScheduleGridExporter
{
    public ScheduleGridExporter()
    {
        // 设置 EPPlus 许可证上下文
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// 导出排班表格（Grid View）
    /// </summary>
    public async Task<byte[]> ExportAsync(
        ScheduleGridData gridData,
        string format,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();
        format = format.ToLower();

        return format switch
        {
            "excel" => await ExportGridToExcelAsync(gridData, options),
            "csv" => await ExportGridToCsvAsync(gridData, options),
            "pdf" => throw new NotImplementedException("PDF 导出功能正在开发中"),
            _ => throw new ArgumentException($"不支持的导出格式: {format}")
        };
    }

    /// <summary>
    /// 导出单个哨位的排班表（Position View）
    /// </summary>
    public async Task<byte[]> ExportPositionScheduleAsync(
        PositionScheduleData positionSchedule,
        string format,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();
        format = format.ToLower();

        return format switch
        {
            "excel" => await ExportPositionToExcelAsync(positionSchedule, options),
            "csv" => throw new NotSupportedException("Position View 不支持 CSV 导出"),
            "pdf" => throw new NotImplementedException("PDF 导出功能正在开发中"),
            _ => throw new ArgumentException($"不支持的导出格式: {format}")
        };
    }

    /// <summary>
    /// 导出单个人员的排班表（Personnel View）
    /// </summary>
    public async Task<byte[]> ExportPersonnelScheduleAsync(
        PersonnelScheduleData personnelSchedule,
        string format,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();
        format = format.ToLower();

        return format switch
        {
            "excel" => await ExportPersonnelToExcelAsync(personnelSchedule, options),
            "csv" => throw new NotSupportedException("Personnel View 不支持 CSV 导出"),
            "pdf" => throw new NotImplementedException("PDF 导出功能正在开发中"),
            _ => throw new ArgumentException($"不支持的导出格式: {format}")
        };
    }

    /// <summary>
    /// 导出班次列表（List View）
    /// </summary>
    public async Task<byte[]> ExportShiftListAsync(
        IEnumerable<ShiftListItem> shiftList,
        string format,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();
        format = format.ToLower();

        return format switch
        {
            "excel" => await ExportShiftListToExcelAsync(shiftList, options),
            "csv" => await ExportShiftListToCsvAsync(shiftList, options),
            "pdf" => throw new NotImplementedException("PDF 导出功能正在开发中"),
            _ => throw new ArgumentException($"不支持的导出格式: {format}")
        };
    }

    /// <summary>
    /// 获取支持的导出格式列表
    /// </summary>
    public List<string> GetSupportedFormats()
    {
        return new List<string> { "excel", "csv", "pdf" };
    }

    /// <summary>
    /// 验证导出格式是否支持
    /// </summary>
    public bool IsFormatSupported(string format)
    {
        return GetSupportedFormats().Contains(format.ToLower());
    }

    #region Grid View Excel 导出

    private async Task<byte[]> ExportGridToExcelAsync(ScheduleGridData gridData, ExportOptions options)
    {
        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("排班表");

            // 设置标题
            if (!string.IsNullOrEmpty(options.Title))
            {
                worksheet.Cells[1, 1].Value = options.Title;
                worksheet.Cells[1, 1, 1, gridData.Columns.Count + 1].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int startRow = string.IsNullOrEmpty(options.Title) ? 1 : 2;

            // 写入表头
            if (options.IncludeHeader)
            {
                // 第一列为日期时段
                worksheet.Cells[startRow, 1].Value = "日期/时段";
                worksheet.Cells[startRow, 1].Style.Font.Bold = true;
                worksheet.Cells[startRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[startRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                // 写入哨位列头
                for (int i = 0; i < gridData.Columns.Count; i++)
                {
                    worksheet.Cells[startRow, i + 2].Value = gridData.Columns[i].PositionName;
                    worksheet.Cells[startRow, i + 2].Style.Font.Bold = true;
                    worksheet.Cells[startRow, i + 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[startRow, i + 2].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                startRow++;
            }

            // 写入数据行
            for (int rowIndex = 0; rowIndex < gridData.Rows.Count; rowIndex++)
            {
                var row = gridData.Rows[rowIndex];
                int excelRow = startRow + rowIndex;

                // 写入行头（日期时段）
                worksheet.Cells[excelRow, 1].Value = row.DisplayText;
                worksheet.Cells[excelRow, 1].Style.Font.Bold = true;

                // 写入单元格数据
                for (int colIndex = 0; colIndex < gridData.Columns.Count; colIndex++)
                {
                    var column = gridData.Columns[colIndex];
                    var cellKey = $"{row.Date:yyyy-MM-dd}_{row.PeriodIndex}_{column.PositionId}";
                    
                    if (gridData.Cells.TryGetValue(cellKey, out var cell))
                    {
                        int excelCol = colIndex + 2;
                        
                        // 写入人员姓名
                        if (!string.IsNullOrEmpty(cell.PersonnelName) || options.IncludeEmptyCells)
                        {
                            worksheet.Cells[excelRow, excelCol].Value = cell.PersonnelName ?? "";
                        }

                        // 应用样式
                        if (cell.HasConflict && options.HighlightConflicts)
                        {
                            // 冲突单元格：红色边框
                            worksheet.Cells[excelRow, excelCol].Style.Border.BorderAround(ExcelBorderStyle.Thick, Color.Red);
                            worksheet.Cells[excelRow, excelCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[excelRow, excelCol].Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                        }
                        else if (cell.IsManualAssignment && options.HighlightManualAssignments)
                        {
                            // 手动指定单元格：蓝色边框
                            worksheet.Cells[excelRow, excelCol].Style.Border.BorderAround(ExcelBorderStyle.Medium, Color.Blue);
                            worksheet.Cells[excelRow, excelCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[excelRow, excelCol].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                        }
                    }
                }
            }

            // 设置所有单元格边框
            var dataRange = worksheet.Cells[startRow - 1, 1, startRow + gridData.Rows.Count - 1, gridData.Columns.Count + 1];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        });
    }

    #endregion

    #region Grid View CSV 导出

    private async Task<byte[]> ExportGridToCsvAsync(ScheduleGridData gridData, ExportOptions options)
    {
        return await Task.Run(() =>
        {
            var sb = new StringBuilder();

            // 写入表头
            if (options.IncludeHeader)
            {
                sb.Append("日期/时段");
                foreach (var column in gridData.Columns)
                {
                    sb.Append($",{EscapeCsvValue(column.PositionName)}");
                }
                sb.AppendLine();
            }

            // 写入数据行
            foreach (var row in gridData.Rows)
            {
                sb.Append(EscapeCsvValue(row.DisplayText));

                foreach (var column in gridData.Columns)
                {
                    var cellKey = $"{row.Date:yyyy-MM-dd}_{row.PeriodIndex}_{column.PositionId}";
                    
                    if (gridData.Cells.TryGetValue(cellKey, out var cell))
                    {
                        var value = cell.PersonnelName ?? "";
                        if (!string.IsNullOrEmpty(value) || options.IncludeEmptyCells)
                        {
                            sb.Append($",{EscapeCsvValue(value)}");
                        }
                        else
                        {
                            sb.Append(",");
                        }
                    }
                    else
                    {
                        sb.Append(",");
                    }
                }

                sb.AppendLine();
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        });
    }

    #endregion

    #region Position View Excel 导出

    private async Task<byte[]> ExportPositionToExcelAsync(PositionScheduleData positionSchedule, ExportOptions options)
    {
        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            
            // 为每个周创建一个工作表
            foreach (var week in positionSchedule.Weeks)
            {
                var worksheet = package.Workbook.Worksheets.Add($"第{week.WeekNumber}周");

                // 设置标题
                string title = options.Title ?? $"{positionSchedule.PositionName} - 第{week.WeekNumber}周";
                worksheet.Cells[1, 1].Value = title;
                worksheet.Cells[1, 1, 1, 8].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // 写入日期范围
                worksheet.Cells[2, 1].Value = $"{week.StartDate:yyyy-MM-dd} 至 {week.EndDate:yyyy-MM-dd}";
                worksheet.Cells[2, 1, 2, 8].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                int startRow = 3;

                // 写入表头
                worksheet.Cells[startRow, 1].Value = "时段";
                worksheet.Cells[startRow, 1].Style.Font.Bold = true;
                worksheet.Cells[startRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[startRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                string[] weekDays = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                for (int i = 0; i < 7; i++)
                {
                    var date = week.StartDate.AddDays(i);
                    worksheet.Cells[startRow, i + 2].Value = $"{weekDays[i]}\n{date:MM-dd}";
                    worksheet.Cells[startRow, i + 2].Style.Font.Bold = true;
                    worksheet.Cells[startRow, i + 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[startRow, i + 2].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    worksheet.Cells[startRow, i + 2].Style.WrapText = true;
                }

                startRow++;

                // 写入数据行（12个时段）
                for (int periodIndex = 0; periodIndex < 12; periodIndex++)
                {
                    int excelRow = startRow + periodIndex;
                    
                    // 写入时段标签
                    worksheet.Cells[excelRow, 1].Value = GetTimeSlotLabel(periodIndex);
                    worksheet.Cells[excelRow, 1].Style.Font.Bold = true;

                    // 写入每天的数据
                    for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
                    {
                        var cellKey = $"{periodIndex}_{dayOfWeek}";
                        
                        if (week.Cells.TryGetValue(cellKey, out var cell))
                        {
                            int excelCol = dayOfWeek + 2;
                            
                            worksheet.Cells[excelRow, excelCol].Value = cell.PersonnelName ?? "";

                            // 应用样式
                            if (cell.HasConflict && options.HighlightConflicts)
                            {
                                worksheet.Cells[excelRow, excelCol].Style.Border.BorderAround(ExcelBorderStyle.Thick, Color.Red);
                                worksheet.Cells[excelRow, excelCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[excelRow, excelCol].Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                            }
                            else if (cell.IsManualAssignment && options.HighlightManualAssignments)
                            {
                                worksheet.Cells[excelRow, excelCol].Style.Border.BorderAround(ExcelBorderStyle.Medium, Color.Blue);
                                worksheet.Cells[excelRow, excelCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[excelRow, excelCol].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            }
                        }
                    }
                }

                // 设置边框
                var dataRange = worksheet.Cells[startRow - 1, 1, startRow + 11, 8];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // 自动调整列宽
                worksheet.Cells.AutoFitColumns();
            }

            return package.GetAsByteArray();
        });
    }

    #endregion

    #region Personnel View Excel 导出

    private async Task<byte[]> ExportPersonnelToExcelAsync(PersonnelScheduleData personnelSchedule, ExportOptions options)
    {
        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("人员排班");

            // 设置标题
            string title = options.Title ?? $"{personnelSchedule.PersonnelName} 的排班情况";
            worksheet.Cells[1, 1].Value = title;
            worksheet.Cells[1, 1, 1, 5].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Size = 14;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int currentRow = 3;

            // 工作量统计
            worksheet.Cells[currentRow, 1].Value = "工作量统计";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "总班次";
            worksheet.Cells[currentRow, 2].Value = personnelSchedule.Workload.TotalShifts;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "日哨";
            worksheet.Cells[currentRow, 2].Value = personnelSchedule.Workload.DayShifts;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "夜哨";
            worksheet.Cells[currentRow, 2].Value = personnelSchedule.Workload.NightShifts;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "工作时长";
            worksheet.Cells[currentRow, 2].Value = $"{personnelSchedule.Workload.TotalShifts * 2} 小时";
            currentRow += 2;

            // 班次列表
            worksheet.Cells[currentRow, 1].Value = "班次列表";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // 表头
            worksheet.Cells[currentRow, 1].Value = "日期";
            worksheet.Cells[currentRow, 2].Value = "时段";
            worksheet.Cells[currentRow, 3].Value = "哨位";
            worksheet.Cells[currentRow, 4].Value = "备注";
            
            for (int col = 1; col <= 4; col++)
            {
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }
            currentRow++;

            // 写入班次数据
            foreach (var shift in personnelSchedule.Shifts)
            {
                worksheet.Cells[currentRow, 1].Value = shift.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[currentRow, 2].Value = shift.TimeSlot;
                worksheet.Cells[currentRow, 3].Value = shift.PositionName;
                worksheet.Cells[currentRow, 4].Value = shift.Remarks ?? "";

                // 高亮手动指定和夜哨
                if (shift.IsManualAssignment && options.HighlightManualAssignments)
                {
                    for (int col = 1; col <= 4; col++)
                    {
                        worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    }
                }
                else if (shift.IsNightShift)
                {
                    for (int col = 1; col <= 4; col++)
                    {
                        worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                    }
                }

                currentRow++;
            }

            // 设置边框
            var dataRange = worksheet.Cells[1, 1, currentRow - 1, 4];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        });
    }

    #endregion

    #region Shift List Excel 导出

    private async Task<byte[]> ExportShiftListToExcelAsync(IEnumerable<ShiftListItem> shiftList, ExportOptions options)
    {
        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("班次列表");

            // 设置标题
            if (!string.IsNullOrEmpty(options.Title))
            {
                worksheet.Cells[1, 1].Value = options.Title;
                worksheet.Cells[1, 1, 1, 5].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int startRow = string.IsNullOrEmpty(options.Title) ? 1 : 3;

            // 写入表头
            worksheet.Cells[startRow, 1].Value = "日期";
            worksheet.Cells[startRow, 2].Value = "时段";
            worksheet.Cells[startRow, 3].Value = "哨位";
            worksheet.Cells[startRow, 4].Value = "人员";
            worksheet.Cells[startRow, 5].Value = "备注";

            for (int col = 1; col <= 5; col++)
            {
                worksheet.Cells[startRow, col].Style.Font.Bold = true;
                worksheet.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            startRow++;

            // 写入数据
            int rowIndex = 0;
            foreach (var shift in shiftList)
            {
                int excelRow = startRow + rowIndex;

                worksheet.Cells[excelRow, 1].Value = shift.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[excelRow, 2].Value = shift.TimeSlot;
                worksheet.Cells[excelRow, 3].Value = shift.PositionName;
                worksheet.Cells[excelRow, 4].Value = shift.PersonnelName;
                
                var remarks = new List<string>();
                if (shift.IsManualAssignment) remarks.Add("手动指定");
                if (shift.HasConflict) remarks.Add($"冲突: {shift.ConflictMessage}");
                worksheet.Cells[excelRow, 5].Value = string.Join(", ", remarks);

                // 应用样式
                if (shift.HasConflict && options.HighlightConflicts)
                {
                    for (int col = 1; col <= 5; col++)
                    {
                        worksheet.Cells[excelRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[excelRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                    }
                }
                else if (shift.IsManualAssignment && options.HighlightManualAssignments)
                {
                    for (int col = 1; col <= 5; col++)
                    {
                        worksheet.Cells[excelRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[excelRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    }
                }

                rowIndex++;
            }

            // 设置边框
            var dataRange = worksheet.Cells[startRow - 1, 1, startRow + rowIndex - 1, 5];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        });
    }

    #endregion

    #region Shift List CSV 导出

    private async Task<byte[]> ExportShiftListToCsvAsync(IEnumerable<ShiftListItem> shiftList, ExportOptions options)
    {
        return await Task.Run(() =>
        {
            var sb = new StringBuilder();

            // 写入表头
            sb.AppendLine("日期,时段,哨位,人员,备注");

            // 写入数据
            foreach (var shift in shiftList)
            {
                var remarks = new List<string>();
                if (shift.IsManualAssignment) remarks.Add("手动指定");
                if (shift.HasConflict) remarks.Add($"冲突: {shift.ConflictMessage}");

                sb.AppendLine($"{shift.Date:yyyy-MM-dd},{EscapeCsvValue(shift.TimeSlot)},{EscapeCsvValue(shift.PositionName)},{EscapeCsvValue(shift.PersonnelName)},{EscapeCsvValue(string.Join(", ", remarks))}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        });
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 转义 CSV 值
    /// </summary>
    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// 获取时段标签
    /// </summary>
    private string GetTimeSlotLabel(int periodIndex)
    {
        int startHour = periodIndex * 2;
        int endHour = startHour + 2;
        return $"{startHour:D2}:00-{endHour:D2}:00";
    }

    #endregion
}
