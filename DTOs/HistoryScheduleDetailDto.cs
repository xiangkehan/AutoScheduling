using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AutoScheduling3.Models;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 历史排班详情数据传输对象
    /// </summary>
    public class HistoryScheduleDetailDto : HistoryScheduleDto
    {
        /// <summary>
        /// 参与人员列表
        /// </summary>
        [Required(ErrorMessage = "人员列表不能为空")]
        [JsonPropertyName("personnel")]
        public List<PersonnelDto> Personnel { get; set; } = new();

        /// <summary>
        /// 参与哨位列表
        /// </summary>
        [Required(ErrorMessage = "哨位列表不能为空")]
        [JsonPropertyName("positions")]
        public List<PositionDto> Positions { get; set; } = new();

        /// <summary>
        /// 排班网格数据（用于表格显示）
        /// </summary>
        [JsonPropertyName("scheduleGrid")]
        public List<List<string>> ScheduleGrid { get; set; } = new();

        /// <summary>
        /// 排班统计信息
        /// </summary>
        [Required(ErrorMessage = "统计信息不能为空")]
        [JsonPropertyName("statistics")]
        public ScheduleStatisticsDto Statistics { get; set; } = new();

        /// <summary>
        /// 班次详情列表
        /// </summary>
        [Required(ErrorMessage = "班次列表不能为空")]
        [JsonPropertyName("shifts")]
        public List<SingleShift> Shifts { get; set; } = new();
    }
}
