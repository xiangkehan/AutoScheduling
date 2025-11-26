using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

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
