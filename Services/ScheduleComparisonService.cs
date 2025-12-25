using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Comparison;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace AutoScheduling3.Services
{
    /// <summary>
    /// 排班对比服务实现
    /// </summary>
    public class ScheduleComparisonService : IScheduleComparisonService
    {
        private readonly IHistoryService _historyService;

        // 夜哨时段索引：22:00-06:00 对应时段 11, 0, 1, 2
        private static readonly HashSet<int> NightShiftPeriods = new() { 0, 1, 2, 11 };

        public ScheduleComparisonService(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        /// <inheritdoc/>
        public async Task<ComparisonResultDto> CompareSchedulesAsync(int scheduleId1, int scheduleId2)
        {
            // 加载两个排班表的详细数据
            var (schedule1, schedule2) = await _historyService.GetSchedulesForComparisonAsync(scheduleId1, scheduleId2);

            // 计算重叠日期范围
            var overlappingRange = CalculateOverlappingDateRange(
                schedule1.StartDate, schedule1.EndDate,
                schedule2.StartDate, schedule2.EndDate);

            // 构建人员和哨位名称映射
            var personnelMap = BuildPersonnelMap(schedule1.Personnel, schedule2.Personnel);
            var positionMap = BuildPositionMap(schedule1.Positions, schedule2.Positions);

            // 计算差异
            var differences = ComputeDifferences(
                schedule1, schedule2, 
                personnelMap, positionMap, 
                overlappingRange);

            // 计算统计
            var statistics = ComputeStatistics(
                schedule1, schedule2, 
                differences, 
                positionMap);

            // 分析人员变更
            var personnelChanges = AnalyzePersonnelChanges(
                schedule1, schedule2, 
                differences, 
                personnelMap);

            return new ComparisonResultDto
            {
                Schedule1 = schedule1,
                Schedule2 = schedule2,
                Differences = differences,
                Statistics = statistics,
                PersonnelChanges = personnelChanges,
                OverlappingDateRange = overlappingRange
            };
        }

        /// <inheritdoc/>
        public List<ShiftDiffDto> FilterDifferences(
            List<ShiftDiffDto> differences,
            DiffType? typeFilter,
            int? positionFilter,
            int? personnelFilter)
        {
            var filtered = differences.AsEnumerable();

            // 按类型筛选
            if (typeFilter.HasValue)
            {
                filtered = filtered.Where(d => d.Type == typeFilter.Value);
            }

            // 按哨位筛选
            if (positionFilter.HasValue)
            {
                filtered = filtered.Where(d => d.PositionId == positionFilter.Value);
            }

            // 按人员筛选（涉及该人员的差异）
            if (personnelFilter.HasValue)
            {
                filtered = filtered.Where(d => 
                    d.PersonnelId1 == personnelFilter.Value || 
                    d.PersonnelId2 == personnelFilter.Value);
            }

            return filtered.ToList();
        }

        /// <inheritdoc/>
        public async Task<byte[]> ExportToExcelAsync(ComparisonResultDto result)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();

                // 创建差异列表工作表
                CreateDifferencesWorksheet(package, result);

                // 创建统计摘要工作表
                CreateStatisticsWorksheet(package, result);

                // 创建人员变更工作表
                CreatePersonnelChangesWorksheet(package, result);

                return package.GetAsByteArray();
            });
        }

        /// <inheritdoc/>
        public string ExportToJson(ComparisonResultDto result)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };

            // 创建导出数据对象（排除不需要的详细数据）
            var exportData = new ComparisonExportDto
            {
                ExportTime = DateTime.Now,
                Schedule1Info = new ScheduleInfoDto
                {
                    Id = result.Schedule1.Id,
                    Header = result.Schedule1.Name,
                    StartDate = result.Schedule1.StartDate,
                    EndDate = result.Schedule1.EndDate,
                    ConfirmTime = result.Schedule1.ConfirmTime
                },
                Schedule2Info = new ScheduleInfoDto
                {
                    Id = result.Schedule2.Id,
                    Header = result.Schedule2.Name,
                    StartDate = result.Schedule2.StartDate,
                    EndDate = result.Schedule2.EndDate,
                    ConfirmTime = result.Schedule2.ConfirmTime
                },
                OverlappingDateRange = result.OverlappingDateRange,
                Statistics = result.Statistics,
                Differences = result.Differences,
                PersonnelChanges = result.PersonnelChanges.Select(p => new PersonnelChangeExportDto
                {
                    PersonnelId = p.PersonnelId,
                    PersonnelName = p.PersonnelName,
                    ShiftsInSchedule1 = p.ShiftsInSchedule1,
                    ShiftsInSchedule2 = p.ShiftsInSchedule2,
                    AddedShifts = p.AddedShifts,
                    RemovedShifts = p.RemovedShifts,
                    NetChange = p.NetChange,
                    Status = p.Status
                }).ToList()
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        #region Excel 导出辅助方法

        /// <summary>
        /// 创建差异列表工作表
        /// </summary>
        private void CreateDifferencesWorksheet(ExcelPackage package, ComparisonResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("差异列表");

            // 标题
            worksheet.Cells[1, 1].Value = "排班对比 - 差异列表";
            worksheet.Cells[1, 1, 1, 7].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // 对比信息
            worksheet.Cells[2, 1].Value = $"排班表1: {result.Schedule1.Name} ({result.Schedule1.StartDate:yyyy-MM-dd} ~ {result.Schedule1.EndDate:yyyy-MM-dd})";
            worksheet.Cells[3, 1].Value = $"排班表2: {result.Schedule2.Name} ({result.Schedule2.StartDate:yyyy-MM-dd} ~ {result.Schedule2.EndDate:yyyy-MM-dd})";
            worksheet.Cells[4, 1].Value = $"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            int startRow = 6;

            // 表头
            string[] headers = { "日期", "哨位", "时段", "原人员", "新人员", "差异类型", "说明" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[startRow, i + 1].Value = headers[i];
                worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                worksheet.Cells[startRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            startRow++;

            // 数据行
            foreach (var diff in result.Differences)
            {
                worksheet.Cells[startRow, 1].Value = diff.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[startRow, 2].Value = diff.PositionName;
                worksheet.Cells[startRow, 3].Value = diff.TimeRange;
                worksheet.Cells[startRow, 4].Value = diff.PersonnelName1 ?? "-";
                worksheet.Cells[startRow, 5].Value = diff.PersonnelName2 ?? "-";
                worksheet.Cells[startRow, 6].Value = GetDiffTypeText(diff.Type);
                worksheet.Cells[startRow, 7].Value = GetDiffDescription(diff);

                // 根据差异类型设置背景色
                var bgColor = GetDiffTypeColor(diff.Type);
                for (int col = 1; col <= 7; col++)
                {
                    worksheet.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(bgColor);
                }

                startRow++;
            }

            // 设置边框
            if (result.Differences.Count > 0)
            {
                var dataRange = worksheet.Cells[6, 1, startRow - 1, 7];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
        }

        /// <summary>
        /// 创建统计摘要工作表
        /// </summary>
        private void CreateStatisticsWorksheet(ExcelPackage package, ComparisonResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("统计摘要");

            // 标题
            worksheet.Cells[1, 1].Value = "排班对比 - 统计摘要";
            worksheet.Cells[1, 1, 1, 4].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int currentRow = 3;

            // 差异统计
            worksheet.Cells[currentRow, 1].Value = "差异统计";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "新增班次";
            worksheet.Cells[currentRow, 2].Value = result.Statistics.AddedCount;
            worksheet.Cells[currentRow, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[currentRow, 2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(223, 246, 221));
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "删除班次";
            worksheet.Cells[currentRow, 2].Value = result.Statistics.RemovedCount;
            worksheet.Cells[currentRow, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[currentRow, 2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(253, 231, 233));
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "变更班次";
            worksheet.Cells[currentRow, 2].Value = result.Statistics.ChangedCount;
            worksheet.Cells[currentRow, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[currentRow, 2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 244, 206));
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "总差异数";
            worksheet.Cells[currentRow, 2].Value = result.Differences.Count;
            worksheet.Cells[currentRow, 2].Style.Font.Bold = true;
            currentRow += 2;

            // 排班表对比
            worksheet.Cells[currentRow, 1].Value = "排班表对比";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // 表头
            worksheet.Cells[currentRow, 1].Value = "指标";
            worksheet.Cells[currentRow, 2].Value = "排班表1";
            worksheet.Cells[currentRow, 3].Value = "排班表2";
            worksheet.Cells[currentRow, 4].Value = "变化";
            for (int col = 1; col <= 4; col++)
            {
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }
            currentRow++;

            // 总班次数
            AddStatisticsRow(worksheet, currentRow, "总班次数",
                result.Statistics.TotalShifts1,
                result.Statistics.TotalShifts2);
            currentRow++;

            // 人均班次
            AddStatisticsRow(worksheet, currentRow, "人均班次",
                result.Statistics.AverageShiftsPerPerson1,
                result.Statistics.AverageShiftsPerPerson2,
                "F2");
            currentRow++;

            // 夜哨班次
            AddStatisticsRow(worksheet, currentRow, "夜哨班次",
                result.Statistics.NightShifts1,
                result.Statistics.NightShifts2);
            currentRow++;

            // 休息日班次
            AddStatisticsRow(worksheet, currentRow, "休息日班次",
                result.Statistics.HolidayShifts1,
                result.Statistics.HolidayShifts2);
            currentRow++;

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
        }

        /// <summary>
        /// 添加统计行
        /// </summary>
        private void AddStatisticsRow(ExcelWorksheet worksheet, int row, string label, double value1, double value2, string format = "")
        {
            worksheet.Cells[row, 1].Value = label;
            
            if (string.IsNullOrEmpty(format))
            {
                worksheet.Cells[row, 2].Value = value1;
                worksheet.Cells[row, 3].Value = value2;
            }
            else
            {
                worksheet.Cells[row, 2].Value = value1;
                worksheet.Cells[row, 2].Style.Numberformat.Format = format;
                worksheet.Cells[row, 3].Value = value2;
                worksheet.Cells[row, 3].Style.Numberformat.Format = format;
            }

            double change = value2 - value1;
            worksheet.Cells[row, 4].Value = change;
            
            if (change > 0)
            {
                worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Green);
                worksheet.Cells[row, 4].Value = $"+{change:F2}";
            }
            else if (change < 0)
            {
                worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[row, 4].Value = $"{change:F2}";
            }
            else
            {
                worksheet.Cells[row, 4].Value = "0";
            }
        }

        /// <summary>
        /// 创建人员变更工作表
        /// </summary>
        private void CreatePersonnelChangesWorksheet(ExcelPackage package, ComparisonResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("人员变更");

            // 标题
            worksheet.Cells[1, 1].Value = "排班对比 - 人员变更分析";
            worksheet.Cells[1, 1, 1, 7].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int startRow = 3;

            // 表头
            string[] headers = { "人员", "状态", "排班表1班次", "排班表2班次", "新增", "减少", "净变化" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[startRow, i + 1].Value = headers[i];
                worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                worksheet.Cells[startRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            startRow++;

            // 数据行
            foreach (var change in result.PersonnelChanges)
            {
                worksheet.Cells[startRow, 1].Value = change.PersonnelName;
                worksheet.Cells[startRow, 2].Value = GetPersonnelStatusText(change.Status);
                worksheet.Cells[startRow, 3].Value = change.ShiftsInSchedule1;
                worksheet.Cells[startRow, 4].Value = change.ShiftsInSchedule2;
                worksheet.Cells[startRow, 5].Value = change.AddedShifts;
                worksheet.Cells[startRow, 6].Value = change.RemovedShifts;
                worksheet.Cells[startRow, 7].Value = change.NetChange;

                // 根据状态设置背景色
                Color bgColor;
                switch (change.Status)
                {
                    case PersonnelStatus.Added:
                        bgColor = Color.FromArgb(223, 246, 221);
                        break;
                    case PersonnelStatus.Removed:
                        bgColor = Color.FromArgb(253, 231, 233);
                        break;
                    default:
                        bgColor = Color.White;
                        break;
                }

                for (int col = 1; col <= 7; col++)
                {
                    worksheet.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(bgColor);
                }

                // 净变化颜色
                if (change.NetChange > 0)
                {
                    worksheet.Cells[startRow, 7].Style.Font.Color.SetColor(Color.Green);
                    worksheet.Cells[startRow, 7].Value = $"+{change.NetChange}";
                }
                else if (change.NetChange < 0)
                {
                    worksheet.Cells[startRow, 7].Style.Font.Color.SetColor(Color.Red);
                }

                startRow++;
            }

            // 设置边框
            if (result.PersonnelChanges.Count > 0)
            {
                var dataRange = worksheet.Cells[3, 1, startRow - 1, 7];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
        }

        /// <summary>
        /// 获取差异类型文本
        /// </summary>
        private string GetDiffTypeText(DiffType type)
        {
            return type switch
            {
                DiffType.Added => "新增",
                DiffType.Removed => "删除",
                DiffType.Changed => "变更",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// 获取差异类型颜色
        /// </summary>
        private Color GetDiffTypeColor(DiffType type)
        {
            return type switch
            {
                DiffType.Added => Color.FromArgb(223, 246, 221),    // 绿色 #DFF6DD
                DiffType.Removed => Color.FromArgb(253, 231, 233),  // 红色 #FDE7E9
                DiffType.Changed => Color.FromArgb(255, 244, 206),  // 黄色 #FFF4CE
                _ => Color.White
            };
        }

        /// <summary>
        /// 获取差异描述
        /// </summary>
        private string GetDiffDescription(ShiftDiffDto diff)
        {
            return diff.Type switch
            {
                DiffType.Added => $"新增班次，分配给 {diff.PersonnelName2}",
                DiffType.Removed => $"删除班次，原分配给 {diff.PersonnelName1}",
                DiffType.Changed => $"人员变更：{diff.PersonnelName1} → {diff.PersonnelName2}",
                _ => ""
            };
        }

        /// <summary>
        /// 获取人员状态文本
        /// </summary>
        private string GetPersonnelStatusText(PersonnelStatus status)
        {
            return status switch
            {
                PersonnelStatus.Added => "新增人员",
                PersonnelStatus.Removed => "移除人员",
                PersonnelStatus.Unchanged => "保持",
                _ => status.ToString()
            };
        }

        #endregion


        #region 差异计算核心算法

        /// <summary>
        /// 计算两个排班表的差异
        /// 按 (DayIndex, PositionId, PeriodIndex) 三维度计算
        /// </summary>
        private List<ShiftDiffDto> ComputeDifferences(
            HistoryScheduleDetailDto schedule1,
            HistoryScheduleDetailDto schedule2,
            Dictionary<int, string> personnelMap,
            Dictionary<int, string> positionMap,
            DateRange? overlappingRange)
        {
            var differences = new List<ShiftDiffDto>();

            // 构建排班表1的班次索引：(DayIndex, PositionId, PeriodIndex) -> Shift
            var shifts1Index = BuildShiftIndex(schedule1.Shifts, schedule1.StartDate);
            
            // 构建排班表2的班次索引
            var shifts2Index = BuildShiftIndex(schedule2.Shifts, schedule2.StartDate);

            // 收集所有唯一的键
            var allKeys = new HashSet<(int DayIndex, int PositionId, int PeriodIndex)>();
            foreach (var key in shifts1Index.Keys) allKeys.Add(key);
            foreach (var key in shifts2Index.Keys) allKeys.Add(key);

            foreach (var key in allKeys)
            {
                var (dayIndex, positionId, periodIndex) = key;
                
                shifts1Index.TryGetValue(key, out var shift1);
                shifts2Index.TryGetValue(key, out var shift2);

                // 计算实际日期（基于排班表1的开始日期，如果有重叠范围则使用重叠范围）
                DateTime date;
                if (overlappingRange != null && overlappingRange.IsValid)
                {
                    date = overlappingRange.StartDate.AddDays(dayIndex);
                }
                else
                {
                    // 使用排班表1的开始日期作为基准
                    date = schedule1.StartDate.AddDays(dayIndex);
                }

                // 判断差异类型
                DiffType? diffType = DetermineDiffType(shift1, shift2);
                
                if (diffType.HasValue)
                {
                    differences.Add(new ShiftDiffDto
                    {
                        DayIndex = dayIndex,
                        Date = date,
                        PositionId = positionId,
                        PositionName = positionMap.GetValueOrDefault(positionId, $"哨位{positionId}"),
                        PeriodIndex = periodIndex,
                        TimeRange = GetTimeRangeString(periodIndex),
                        PersonnelId1 = shift1?.PersonnelId,
                        PersonnelName1 = shift1 != null ? personnelMap.GetValueOrDefault(shift1.PersonnelId, $"人员{shift1.PersonnelId}") : null,
                        PersonnelId2 = shift2?.PersonnelId,
                        PersonnelName2 = shift2 != null ? personnelMap.GetValueOrDefault(shift2.PersonnelId, $"人员{shift2.PersonnelId}") : null,
                        Type = diffType.Value
                    });
                }
            }

            // 按日期、哨位、时段排序
            return differences
                .OrderBy(d => d.DayIndex)
                .ThenBy(d => d.PositionId)
                .ThenBy(d => d.PeriodIndex)
                .ToList();
        }

        /// <summary>
        /// 构建班次索引
        /// </summary>
        private Dictionary<(int DayIndex, int PositionId, int PeriodIndex), SingleShift> BuildShiftIndex(
            List<SingleShift> shifts, DateTime startDate)
        {
            var index = new Dictionary<(int, int, int), SingleShift>();
            
            foreach (var shift in shifts)
            {
                // 使用班次自带的 DayIndex 和 TimeSlotIndex
                var key = (shift.DayIndex, shift.PositionId, shift.TimeSlotIndex);
                
                // 如果有重复，保留第一个（理论上不应该有重复）
                if (!index.ContainsKey(key))
                {
                    index[key] = shift;
                }
            }
            
            return index;
        }

        /// <summary>
        /// 判断差异类型
        /// </summary>
        private DiffType? DetermineDiffType(SingleShift? shift1, SingleShift? shift2)
        {
            bool exists1 = shift1 != null;
            bool exists2 = shift2 != null;

            if (exists1 && !exists2)
            {
                // 只在排班表1中存在 -> 删除
                return DiffType.Removed;
            }
            else if (!exists1 && exists2)
            {
                // 只在排班表2中存在 -> 新增
                return DiffType.Added;
            }
            else if (exists1 && exists2)
            {
                // 两边都存在，检查人员是否相同
                if (shift1!.PersonnelId != shift2!.PersonnelId)
                {
                    return DiffType.Changed;
                }
                // 人员相同，无差异
                return null;
            }
            
            // 两边都不存在（不应该发生）
            return null;
        }

        /// <summary>
        /// 获取时段范围字符串
        /// </summary>
        private string GetTimeRangeString(int periodIndex)
        {
            int startHour = periodIndex * 2;
            int endHour = (startHour + 2) % 24;
            return $"{startHour:D2}:00-{endHour:D2}:00";
        }

        #endregion

        #region 日期范围计算

        /// <summary>
        /// 计算两个日期范围的重叠部分
        /// </summary>
        public DateRange? CalculateOverlappingDateRange(
            DateTime start1, DateTime end1,
            DateTime start2, DateTime end2)
        {
            var overlapStart = start1 > start2 ? start1 : start2;
            var overlapEnd = end1 < end2 ? end1 : end2;

            if (overlapStart > overlapEnd)
            {
                // 无重叠
                return null;
            }

            return new DateRange
            {
                StartDate = overlapStart.Date,
                EndDate = overlapEnd.Date
            };
        }

        #endregion


        #region 统计计算

        /// <summary>
        /// 计算对比统计
        /// </summary>
        private ComparisonStatisticsDto ComputeStatistics(
            HistoryScheduleDetailDto schedule1,
            HistoryScheduleDetailDto schedule2,
            List<ShiftDiffDto> differences,
            Dictionary<int, string> positionMap)
        {
            var stats = new ComparisonStatisticsDto
            {
                // 总班次数
                TotalShifts1 = schedule1.Shifts.Count,
                TotalShifts2 = schedule2.Shifts.Count,

                // 人均班次数
                AverageShiftsPerPerson1 = CalculateAverageShifts(schedule1.Shifts, schedule1.Personnel.Count),
                AverageShiftsPerPerson2 = CalculateAverageShifts(schedule2.Shifts, schedule2.Personnel.Count),

                // 夜哨班次数
                NightShifts1 = CountNightShifts(schedule1.Shifts),
                NightShifts2 = CountNightShifts(schedule2.Shifts),

                // 休息日班次数（使用周末作为休息日）
                HolidayShifts1 = CountWeekendShifts(schedule1.Shifts, schedule1.StartDate),
                HolidayShifts2 = CountWeekendShifts(schedule2.Shifts, schedule2.StartDate),

                // 差异统计
                AddedCount = differences.Count(d => d.Type == DiffType.Added),
                RemovedCount = differences.Count(d => d.Type == DiffType.Removed),
                ChangedCount = differences.Count(d => d.Type == DiffType.Changed),

                // 哨位覆盖率
                PositionCoverage1 = CalculatePositionCoverage(schedule1.Shifts, schedule1.Positions, schedule1.StartDate, schedule1.EndDate),
                PositionCoverage2 = CalculatePositionCoverage(schedule2.Shifts, schedule2.Positions, schedule2.StartDate, schedule2.EndDate)
            };

            return stats;
        }

        /// <summary>
        /// 计算人均班次数
        /// </summary>
        private double CalculateAverageShifts(List<SingleShift> shifts, int personnelCount)
        {
            if (personnelCount == 0) return 0;
            return (double)shifts.Count / personnelCount;
        }

        /// <summary>
        /// 统计夜哨班次数
        /// </summary>
        private int CountNightShifts(List<SingleShift> shifts)
        {
            return shifts.Count(s => NightShiftPeriods.Contains(s.TimeSlotIndex));
        }

        /// <summary>
        /// 统计周末班次数
        /// </summary>
        private int CountWeekendShifts(List<SingleShift> shifts, DateTime startDate)
        {
            return shifts.Count(s =>
            {
                var date = startDate.AddDays(s.DayIndex);
                return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            });
        }

        /// <summary>
        /// 计算哨位覆盖率
        /// </summary>
        private Dictionary<int, double> CalculatePositionCoverage(
            List<SingleShift> shifts,
            List<PositionDto> positions,
            DateTime startDate,
            DateTime endDate)
        {
            var coverage = new Dictionary<int, double>();
            int totalDays = (endDate - startDate).Days + 1;
            int totalSlotsPerPosition = totalDays * 12; // 每天12个时段

            foreach (var position in positions)
            {
                int assignedSlots = shifts.Count(s => s.PositionId == position.Id);
                coverage[position.Id] = totalSlotsPerPosition > 0 
                    ? (double)assignedSlots / totalSlotsPerPosition 
                    : 0;
            }

            return coverage;
        }

        #endregion

        #region 人员变更分析

        /// <summary>
        /// 分析人员变更
        /// </summary>
        private List<PersonnelChangeDto> AnalyzePersonnelChanges(
            HistoryScheduleDetailDto schedule1,
            HistoryScheduleDetailDto schedule2,
            List<ShiftDiffDto> differences,
            Dictionary<int, string> personnelMap)
        {
            var changes = new List<PersonnelChangeDto>();

            // 统计每个人员在两个排班表中的班次数
            var shiftsCount1 = schedule1.Shifts
                .GroupBy(s => s.PersonnelId)
                .ToDictionary(g => g.Key, g => g.Count());

            var shiftsCount2 = schedule2.Shifts
                .GroupBy(s => s.PersonnelId)
                .ToDictionary(g => g.Key, g => g.Count());

            // 收集所有涉及的人员ID
            var allPersonnelIds = new HashSet<int>();
            foreach (var id in shiftsCount1.Keys) allPersonnelIds.Add(id);
            foreach (var id in shiftsCount2.Keys) allPersonnelIds.Add(id);

            foreach (var personnelId in allPersonnelIds)
            {
                shiftsCount1.TryGetValue(personnelId, out int count1);
                shiftsCount2.TryGetValue(personnelId, out int count2);

                // 计算该人员相关的差异
                var relatedDiffs = differences
                    .Where(d => d.PersonnelId1 == personnelId || d.PersonnelId2 == personnelId)
                    .ToList();

                // 计算新增和减少的班次
                int addedShifts = relatedDiffs.Count(d => 
                    d.Type == DiffType.Added && d.PersonnelId2 == personnelId);
                int removedShifts = relatedDiffs.Count(d => 
                    d.Type == DiffType.Removed && d.PersonnelId1 == personnelId);
                
                // Changed 类型需要特殊处理
                addedShifts += relatedDiffs.Count(d => 
                    d.Type == DiffType.Changed && d.PersonnelId2 == personnelId && d.PersonnelId1 != personnelId);
                removedShifts += relatedDiffs.Count(d => 
                    d.Type == DiffType.Changed && d.PersonnelId1 == personnelId && d.PersonnelId2 != personnelId);

                // 确定人员状态
                PersonnelStatus status;
                if (count1 == 0 && count2 > 0)
                {
                    status = PersonnelStatus.Added;
                }
                else if (count1 > 0 && count2 == 0)
                {
                    status = PersonnelStatus.Removed;
                }
                else
                {
                    status = PersonnelStatus.Unchanged;
                }

                changes.Add(new PersonnelChangeDto
                {
                    PersonnelId = personnelId,
                    PersonnelName = personnelMap.GetValueOrDefault(personnelId, $"人员{personnelId}"),
                    ShiftsInSchedule1 = count1,
                    ShiftsInSchedule2 = count2,
                    AddedShifts = addedShifts,
                    RemovedShifts = removedShifts,
                    NetChange = addedShifts - removedShifts,
                    Status = status,
                    DetailedChanges = relatedDiffs
                });
            }

            // 按净变化绝对值排序（变化大的在前）
            return changes
                .OrderByDescending(c => Math.Abs(c.NetChange))
                .ThenBy(c => c.PersonnelName)
                .ToList();
        }

        #endregion

        #region 网格数据构建

        /// <inheritdoc/>
        public GridComparisonDto BuildGridData(ComparisonResultDto result)
        {
            var gridData = new GridComparisonDto();

            // 构建哨位列表（两个排班表的并集）
            var positionMap = new Dictionary<int, GridPositionDto>();
            foreach (var p in result.Schedule1.Positions)
            {
                positionMap[p.Id] = new GridPositionDto { Id = p.Id, Name = p.Name };
            }
            foreach (var p in result.Schedule2.Positions)
            {
                if (!positionMap.ContainsKey(p.Id))
                {
                    positionMap[p.Id] = new GridPositionDto { Id = p.Id, Name = p.Name };
                }
            }
            gridData.Positions = positionMap.Values.OrderBy(p => p.Name).ToList();

            // 确定日期范围
            DateTime startDate, endDate;
            if (result.OverlappingDateRange != null && result.OverlappingDateRange.IsValid)
            {
                startDate = result.OverlappingDateRange.StartDate;
                endDate = result.OverlappingDateRange.EndDate;
            }
            else
            {
                // 无重叠时使用排班表1的范围
                startDate = result.Schedule1.StartDate;
                endDate = result.Schedule1.EndDate;
            }

            // 生成日期列表
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                gridData.Dates.Add(date);
            }

            // 构建人员名称映射
            var personnelMap = BuildPersonnelMap(result.Schedule1.Personnel, result.Schedule2.Personnel);

            // 构建班次索引
            var shifts1Index = BuildShiftIndex(result.Schedule1.Shifts, result.Schedule1.StartDate);
            var shifts2Index = BuildShiftIndex(result.Schedule2.Shifts, result.Schedule2.StartDate);

            // 构建差异索引（用于快速查找）
            var diffIndex = result.Differences
                .ToDictionary(d => (d.DayIndex, d.PositionId, d.PeriodIndex), d => d.Type);

            // 生成网格行
            foreach (var date in gridData.Dates)
            {
                int dayIndex1 = (date - result.Schedule1.StartDate.Date).Days;
                int dayIndex2 = (date - result.Schedule2.StartDate.Date).Days;

                for (int periodIndex = 0; periodIndex < 12; periodIndex++)
                {
                    var row = new GridRowDto
                    {
                        Date = date,
                        PeriodIndex = periodIndex,
                        TimeRange = GetTimeRangeString(periodIndex)
                    };

                    foreach (var position in gridData.Positions)
                    {
                        var cell = new GridCellDto { PositionId = position.Id };

                        // 查找排班表1的分配
                        var key1 = (dayIndex1, position.Id, periodIndex);
                        if (shifts1Index.TryGetValue(key1, out var shift1))
                        {
                            cell.PersonnelName1 = personnelMap.GetValueOrDefault(shift1.PersonnelId, $"人员{shift1.PersonnelId}");
                        }

                        // 查找排班表2的分配
                        var key2 = (dayIndex2, position.Id, periodIndex);
                        if (shifts2Index.TryGetValue(key2, out var shift2))
                        {
                            cell.PersonnelName2 = personnelMap.GetValueOrDefault(shift2.PersonnelId, $"人员{shift2.PersonnelId}");
                        }

                        // 查找差异类型（使用重叠范围的dayIndex）
                        int overlapDayIndex = (date - startDate).Days;
                        if (diffIndex.TryGetValue((overlapDayIndex, position.Id, periodIndex), out var diffType))
                        {
                            cell.DiffType = diffType;
                        }

                        row.Cells[position.Id] = cell;
                    }

                    gridData.Rows.Add(row);
                }
            }

            return gridData;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 构建人员ID到姓名的映射
        /// </summary>
        private Dictionary<int, string> BuildPersonnelMap(
            List<PersonnelDto> personnel1,
            List<PersonnelDto> personnel2)
        {
            var map = new Dictionary<int, string>();
            
            foreach (var p in personnel1)
            {
                map[p.Id] = p.Name;
            }
            
            foreach (var p in personnel2)
            {
                if (!map.ContainsKey(p.Id))
                {
                    map[p.Id] = p.Name;
                }
            }
            
            return map;
        }

        /// <summary>
        /// 构建哨位ID到名称的映射
        /// </summary>
        private Dictionary<int, string> BuildPositionMap(
            List<PositionDto> positions1,
            List<PositionDto> positions2)
        {
            var map = new Dictionary<int, string>();
            
            foreach (var p in positions1)
            {
                map[p.Id] = p.Name;
            }
            
            foreach (var p in positions2)
            {
                if (!map.ContainsKey(p.Id))
                {
                    map[p.Id] = p.Name;
                }
            }
            
            return map;
        }

        #endregion
    }
}
