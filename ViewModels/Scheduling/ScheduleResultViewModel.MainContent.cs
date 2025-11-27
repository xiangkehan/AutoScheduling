using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.RightPanel;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 主内容区相关逻辑
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 主内容区属性

        /// <summary>
        /// 表格数据（按行组织）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ScheduleRowViewModel> _scheduleGrid = new();

        /// <summary>
        /// 筛选选项
        /// </summary>
        [ObservableProperty]
        private FilterOptions _filterOptions = new();

        /// <summary>
        /// 筛选栏是否展开
        /// </summary>
        [ObservableProperty]
        private bool _isFilterExpanded = false;

        /// <summary>
        /// 搜索文本
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        #endregion

        #region 主内容区命令

        // 注意：ChangeViewModeCommand 已在主ViewModel中定义

        /// <summary>
        /// 选择单元格命令
        /// </summary>
        [RelayCommand]
        private async Task SelectCellAsync(ScheduleCellViewModel cell)
        {
            if (cell == null) return;

            // 清除其他单元格的选中状态
            foreach (var row in ScheduleGrid)
            {
                foreach (var c in row.Cells)
                {
                    c.IsSelected = c == cell;
                }
            }

            // 更新选中项
            SelectedItem = cell;
            IsRightPanelVisible = true;

            // 如果单元格有冲突，在左侧冲突列表中高亮
            if (cell.HasHardConflict || cell.HasSoftConflict)
            {
                await HighlightConflictByCellAsync(cell);
            }

            DetailTitle = cell.HasHardConflict || cell.HasSoftConflict ? "冲突详情" : "班次详情";
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync(string query)
        {
            // 取消之前的搜索
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            try
            {
                // 防抖300ms
                await Task.Delay(300, _searchCts.Token);

                // 执行搜索
                FilterOptions.SearchText = query;
                await ApplyFiltersAsync();
            }
            catch (TaskCanceledException)
            {
                // 搜索被取消，忽略
            }
        }

        /// <summary>
        /// 切换筛选栏展开/折叠
        /// </summary>
        [RelayCommand]
        private void ToggleFilterExpanded()
        {
            IsFilterExpanded = !IsFilterExpanded;
        }

        /// <summary>
        /// 清除筛选条件
        /// </summary>
        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            FilterOptions.Clear();
            SearchText = string.Empty;
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// 保存班次编辑命令
        /// </summary>
        [RelayCommand]
        private async Task SaveShiftEditAsync(ShiftEditData editData)
        {
            if (editData == null) return;
            
            // 1. 更新对应的班次数据
            await UpdateShiftDataAsync(editData);
            
            // 2. 更新主内容区表格
            await RefreshScheduleGridAsync();
            
            // 3. 更新左侧统计摘要
            await UpdateStatisticsSummaryAsync();
            
            // 4. 标记为有未保存更改
            HasUnsavedChanges = true;
            
            // 5. 关闭右侧详情面板
            IsRightPanelVisible = false;
        }

        #endregion

        #region 主内容区辅助方法

        private CancellationTokenSource? _searchCts;

        // 注意：ApplyFiltersAsync 已在主ViewModel中定义

        /// <summary>
        /// 根据单元格高亮冲突
        /// </summary>
        private async Task HighlightConflictByCellAsync(ScheduleCellViewModel cell)
        {
            // TODO: 在冲突列表中找到对应的冲突并高亮
            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新班次数据
        /// </summary>
        /// <param name="editData">编辑数据</param>
        private async Task UpdateShiftDataAsync(ShiftEditData editData)
        {
            if (Schedule == null || Schedule.Shifts == null) return;
            
            // 根据日期、时段和哨位找到对应的班次
            var shift = Schedule.Shifts.FirstOrDefault(s => 
                s.StartTime.ToString("yyyy-MM-dd") == editData.Date &&
                s.PositionName == editData.Position);
            
            if (shift != null)
            {
                // 更新人员
                if (!string.IsNullOrWhiteSpace(editData.Personnel))
                {
                    // 这里需要根据人员名称找到对应的人员ID
                    // 实际项目中应该从人员列表中查找
                    shift.PersonnelName = editData.Personnel;
                    // shift.PersonnelId = 找到的人员ID;
                }
                
                // 更新时间段
                if (!string.IsNullOrWhiteSpace(editData.StartTime) && !string.IsNullOrWhiteSpace(editData.EndTime))
                {
                    // 这里需要根据时间段更新StartTime和EndTime
                    // 实际项目中应该解析时间段并更新
                }
                
                // 更新备注
                // 实际项目中应该更新班次的备注字段
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新统计摘要
        /// </summary>
        private async Task UpdateStatisticsSummaryAsync()
        {
            // 重新计算统计数据
            if (Schedule == null) return;
            
            // 计算硬约束冲突数量
            var hardConflictCount = Schedule.Conflicts?.Count(c => c.IsHardConstraint) ?? 0;
            
            // 计算软约束冲突数量
            var softConflictCount = Schedule.Conflicts?.Count(c => !c.IsHardConstraint) ?? 0;
            
            // 计算未分配班次数量
            var unassignedCount = Schedule.Shifts?.Count(s => s.PersonnelId == 0 || string.IsNullOrWhiteSpace(s.PersonnelName)) ?? 0;
            
            // 更新统计摘要
            StatisticsSummary = new StatisticsSummary
            {
                HardConflictCount = hardConflictCount,
                SoftConflictCount = softConflictCount,
                UnassignedCount = unassignedCount,
                TotalShiftCount = Schedule.Shifts?.Count ?? 0,
                AssignedShiftCount = Schedule.Shifts?.Count(s => s.PersonnelId > 0) ?? 0,
                CoverageRate = 0.0 // TODO: 计算覆盖率
            };
            
            await Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// 排班行ViewModel
    /// </summary>
    public partial class ScheduleRowViewModel : ObservableObject
    {
        /// <summary>
        /// 行标题（日期+时段）
        /// </summary>
        [ObservableProperty]
        private string _rowHeader = string.Empty;

        /// <summary>
        /// 单元格列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ScheduleCellViewModel> _cells = new();

        /// <summary>
        /// 行索引
        /// </summary>
        [ObservableProperty]
        private int _rowIndex;
    }
}
