using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 左侧面板相关逻辑
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 左侧面板属性

        /// <summary>
        /// 统计摘要（新UI使用）
        /// </summary>
        [ObservableProperty]
        private StatisticsSummary _statisticsSummary = new();

        /// <summary>
        /// 冲突列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ConflictItemViewModel> _conflictList = new();

        /// <summary>
        /// 冲突筛选类型
        /// </summary>
        [ObservableProperty]
        private ConflictFilterType _conflictFilter = ConflictFilterType.All;

        /// <summary>
        /// 左侧面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isLeftPanelVisible = true;

        #endregion

        #region 左侧面板命令

        /// <summary>
        /// 选择统计指标命令
        /// </summary>
        [RelayCommand]
        private async Task SelectStatisticAsync(StatisticType type)
        {
            // 更新筛选状态
            ConflictFilter = type switch
            {
                StatisticType.HardConflict => ConflictFilterType.HardOnly,
                StatisticType.SoftConflict => ConflictFilterType.SoftOnly,
                StatisticType.Unassigned => ConflictFilterType.All,
                _ => ConflictFilterType.All
            };

            // 高亮主内容区对应的单元格
            await HighlightCellsByStatisticTypeAsync(type);

            // 筛选冲突列表
            await RefreshConflictListAsync();
        }

        /// <summary>
        /// 选择冲突命令
        /// </summary>
        [RelayCommand]
        private async Task SelectConflictAsync(ConflictItemViewModel conflict)
        {
            if (conflict == null) return;

            // 清除其他冲突的选中状态
            foreach (var item in ConflictList)
            {
                item.IsSelected = item == conflict;
            }

            // 在主内容区定位到冲突单元格
            await ScrollToCellByConflictAsync(conflict);

            // 在右侧详情区显示冲突详情
            SelectedItem = conflict;
            DetailTitle = "冲突详情";
            IsRightPanelVisible = true;
        }

        /// <summary>
        /// 切换左侧面板显示/隐藏
        /// </summary>
        [RelayCommand]
        private void ToggleLeftPanel()
        {
            IsLeftPanelVisible = !IsLeftPanelVisible;
        }

        #endregion

        #region 左侧面板辅助方法

        /// <summary>
        /// 刷新统计摘要
        /// </summary>
        private async Task RefreshStatisticsAsync()
        {
            if (Schedule == null) return;

            await Task.Run(() =>
            {
                var stats = new StatisticsSummary
                {
                    HardConflictCount = ConflictList.Count(c => c.Type == ConflictType.Hard),
                    SoftConflictCount = ConflictList.Count(c => c.Type == ConflictType.Soft),
                    UnassignedCount = 0, // TODO: 从Schedule计算
                    TotalShiftCount = 0, // TODO: 从Schedule计算
                    AssignedShiftCount = 0, // TODO: 从Schedule计算
                    CoverageRate = 0.0 // TODO: 计算覆盖率
                };

                StatisticsSummary = stats;
            });
        }

        /// <summary>
        /// 刷新冲突列表
        /// </summary>
        private async Task RefreshConflictListAsync()
        {
            if (Schedule == null) return;

            await Task.Run(() =>
            {
                // TODO: 从Schedule的冲突数据转换为ConflictItemViewModel
                // 这里先使用空实现，后续任务会完善
            });
        }

        /// <summary>
        /// 根据统计类型高亮单元格
        /// </summary>
        private async Task HighlightCellsByStatisticTypeAsync(StatisticType type)
        {
            // TODO: 实现单元格高亮逻辑
            await Task.CompletedTask;
        }

        /// <summary>
        /// 根据冲突滚动到单元格
        /// </summary>
        private async Task ScrollToCellByConflictAsync(ConflictItemViewModel conflict)
        {
            // TODO: 实现滚动到单元格逻辑
            await Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// 统计类型枚举
    /// </summary>
    public enum StatisticType
    {
        /// <summary>
        /// 硬约束冲突
        /// </summary>
        HardConflict,

        /// <summary>
        /// 软约束冲突
        /// </summary>
        SoftConflict,

        /// <summary>
        /// 未分配班次
        /// </summary>
        Unassigned
    }

    /// <summary>
    /// 冲突筛选类型枚举
    /// </summary>
    public enum ConflictFilterType
    {
        /// <summary>
        /// 全部
        /// </summary>
        All,

        /// <summary>
        /// 仅硬约束
        /// </summary>
        HardOnly,

        /// <summary>
        /// 仅软约束
        /// </summary>
        SoftOnly
    }
}
