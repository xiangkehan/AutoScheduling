using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 历史排班数据传输对象
    /// </summary>
    public class HistoryScheduleDto
    {
        /// <summary>
        /// 排班表ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 排班表名称
        /// </summary>
        [Required(ErrorMessage = "排班表名称不能为空")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "排班表名称长度必须在1-100字符之间")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 开始日期
        /// </summary>
        [Required(ErrorMessage = "开始日期不能为空")]
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        [Required(ErrorMessage = "结束日期不能为空")]
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 参与人员数量
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "人员数量必须大于0")]
        [JsonPropertyName("numberOfPersonnel")]
        public int NumberOfPersonnel { get; set; }

        /// <summary>
        /// 参与哨位数量
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "哨位数量必须大于0")]
        [JsonPropertyName("numberOfPositions")]
        public int NumberOfPositions { get; set; }

        /// <summary>
        /// 确认时间
        /// </summary>
        [Required(ErrorMessage = "确认时间不能为空")]
        [JsonPropertyName("confirmTime")]
        public DateTime ConfirmTime { get; set; }
    }
}
