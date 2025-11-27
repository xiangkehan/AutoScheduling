using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 搜索筛选条件
    /// </summary>
    public class SearchFilters
    {
        /// <summary>
        /// 人员 ID
        /// </summary>
        public int? PersonnelId { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 哨位 ID 列表
        /// </summary>
        public List<int>? PositionIds { get; set; }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// 是否有任何活动筛选
        /// </summary>
        public bool HasAnyFilter =>
            PersonnelId.HasValue ||
            StartDate.HasValue ||
            EndDate.HasValue ||
            (PositionIds != null && PositionIds.Any()) ||
            !string.IsNullOrWhiteSpace(SearchKeyword);

        /// <summary>
        /// 获取筛选条件摘要
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string>();

            if (PersonnelId.HasValue)
                parts.Add($"人员ID: {PersonnelId}");
            
            if (StartDate.HasValue)
                parts.Add($"开始: {StartDate:yyyy-MM-dd}");
            
            if (EndDate.HasValue)
                parts.Add($"结束: {EndDate:yyyy-MM-dd}");
            
            if (PositionIds != null && PositionIds.Any())
                parts.Add($"哨位: {PositionIds.Count}个");

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
                parts.Add($"关键词: {SearchKeyword}");

            return parts.Any() ? string.Join(", ", parts) : "无筛选条件";
        }
    }
}
