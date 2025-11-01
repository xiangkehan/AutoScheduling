using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 排班统计数据传输对象
    /// </summary>
    public class ScheduleStatisticsDto
    {
        /// <summary>
        /// 总班次数
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "总班次数不能为负数")]
        [JsonPropertyName("totalShifts")]
        public int TotalShifts { get; set; }

        /// <summary>
        /// 人均班次数
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "人均班次数不能为负数")]
        [JsonPropertyName("averageShiftsPerPerson")]
        public double AverageShiftsPerPerson { get; set; }

        /// <summary>
        /// 周末班次数
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "周末班次数不能为负数")]
        [JsonPropertyName("weekendShifts")]
        public int WeekendShifts { get; set; }

        /// <summary>
        /// 按时段分布的班次统计
        /// </summary>
        [Required(ErrorMessage = "时段班次统计不能为空")]
        [JsonPropertyName("shiftsByTimeOfDay")]
        public Dictionary<string, int> ShiftsByTimeOfDay { get; set; } = new();

        /// <summary>
        /// 按人员分布的班次统计
        /// </summary>
        [Required(ErrorMessage = "人员班次统计不能为空")]
        [JsonPropertyName("shiftsPerPerson")]
        public Dictionary<string, int> ShiftsPerPerson { get; set; } = new();

        /// <summary>
        /// 哨位覆盖率（0.0-1.0）
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "哨位覆盖率必须在0.0-1.0之间")]
        [JsonPropertyName("positionCoverage")]
        public double PositionCoverage { get; set; }

        /// <summary>
        /// 总确认排班数
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "确认排班数不能为负数")]
        [JsonPropertyName("totalSchedules")]
        public int TotalSchedules { get; set; }

        /// <summary>
        /// 已确认排班数
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "已确认排班数不能为负数")]
        [JsonPropertyName("confirmedSchedules")]
        public int ConfirmedSchedules { get; set; }

        /// <summary>
        /// 草稿排班数
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "草稿排班数不能为负数")]
        [JsonPropertyName("draftSchedules")]
        public int DraftSchedules { get; set; }

        /// <summary>
        /// 人员班次计数字典
        /// </summary>
        [Required(ErrorMessage = "人员班次计数不能为空")]
        [JsonPropertyName("personnelShiftCounts")]
        public Dictionary<int, int> PersonnelShiftCounts { get; set; } = new();

        /// <summary>
        /// 时段分布字典
        /// </summary>
        [Required(ErrorMessage = "时段分布不能为空")]
        [JsonPropertyName("timeSlotDistribution")]
        public Dictionary<int, double> TimeSlotDistribution { get; set; } = new();
    }
}
