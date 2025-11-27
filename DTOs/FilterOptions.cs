using System;
using System.Collections.Generic;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 筛选选项数据传输对象
    /// </summary>
    public class FilterOptions
    {
        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// 筛选的人员ID列表
        /// </summary>
        public List<int> PersonnelIds { get; set; } = new();

        /// <summary>
        /// 筛选的哨位ID列表
        /// </summary>
        public List<int> PositionIds { get; set; } = new();

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 时段类型筛选（日哨/夜哨）
        /// </summary>
        public List<string> TimeSlotTypes { get; set; } = new();

        /// <summary>
        /// 状态筛选（有冲突/无冲突/未分配）
        /// </summary>
        public List<string> StatusFilters { get; set; } = new();

        /// <summary>
        /// 是否只显示硬约束冲突
        /// </summary>
        public bool ShowHardConflictsOnly { get; set; }

        /// <summary>
        /// 是否只显示软约束冲突
        /// </summary>
        public bool ShowSoftConflictsOnly { get; set; }

        /// <summary>
        /// 是否有任何筛选条件
        /// </summary>
        public bool HasAnyFilter =>
            !string.IsNullOrWhiteSpace(SearchText) ||
            PersonnelIds.Count > 0 ||
            PositionIds.Count > 0 ||
            StartDate.HasValue ||
            EndDate.HasValue ||
            TimeSlotTypes.Count > 0 ||
            StatusFilters.Count > 0 ||
            ShowHardConflictsOnly ||
            ShowSoftConflictsOnly;

        /// <summary>
        /// 清除所有筛选条件
        /// </summary>
        public void Clear()
        {
            SearchText = string.Empty;
            PersonnelIds.Clear();
            PositionIds.Clear();
            StartDate = null;
            EndDate = null;
            TimeSlotTypes.Clear();
            StatusFilters.Clear();
            ShowHardConflictsOnly = false;
            ShowSoftConflictsOnly = false;
        }

        /// <summary>
        /// 克隆筛选选项
        /// </summary>
        public FilterOptions Clone()
        {
            return new FilterOptions
            {
                SearchText = SearchText,
                PersonnelIds = new List<int>(PersonnelIds),
                PositionIds = new List<int>(PositionIds),
                StartDate = StartDate,
                EndDate = EndDate,
                TimeSlotTypes = new List<string>(TimeSlotTypes),
                StatusFilters = new List<string>(StatusFilters),
                ShowHardConflictsOnly = ShowHardConflictsOnly,
                ShowSoftConflictsOnly = ShowSoftConflictsOnly
            };
        }
    }
}
