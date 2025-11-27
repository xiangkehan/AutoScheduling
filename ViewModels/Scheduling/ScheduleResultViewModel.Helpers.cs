using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 辅助方法
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 同步更新机制

        private readonly SemaphoreSlim _updateLock = new(1, 1);

        /// <summary>
        /// 同步更新所有区域
        /// </summary>
        private async Task SynchronizeAllAreasAsync()
        {
            await _updateLock.WaitAsync();
            try
            {
                // 批量更新，避免多次触发UI刷新
                await Task.WhenAll(
                    RefreshStatisticsAsync(),
                    RefreshConflictListAsync(),
                    RefreshScheduleGridAsync()
                );
            }
            finally
            {
                _updateLock.Release();
            }
        }

        /// <summary>
        /// 刷新排班表格
        /// </summary>
        private async Task RefreshScheduleGridAsync()
        {
            if (Schedule == null) return;

            // 转换GridData为ScheduleGrid
            ConvertScheduleToGrid();

            await Task.CompletedTask;
        }

        #endregion

        #region 数据转换

        /// <summary>
        /// 将Schedule转换为ScheduleGrid
        /// </summary>
        private void ConvertScheduleToGrid()
        {
            if (Schedule == null || GridData == null)
            {
                ScheduleGrid.Clear();
                return;
            }

            var newGrid = new System.Collections.ObjectModel.ObservableCollection<ScheduleRowViewModel>();

            // 遍历每一行
            foreach (var row in GridData.Rows)
            {
                var rowViewModel = new ScheduleRowViewModel
                {
                    RowIndex = row.RowIndex,
                    RowHeader = row.DisplayText
                };

                // 遍历每一列，创建单元格
                foreach (var col in GridData.Columns)
                {
                    var cellKey = $"{row.RowIndex}_{col.ColumnIndex}";
                    var cellData = GridData.Cells.ContainsKey(cellKey) 
                        ? GridData.Cells[cellKey] 
                        : null;

                    var cellViewModel = new ScheduleCellViewModel
                    {
                        RowIndex = row.RowIndex,
                        ColumnIndex = col.ColumnIndex,
                        PositionId = col.PositionId,
                        PositionName = col.PositionName,
                        Date = row.Date,
                        TimeSlot = row.PeriodIndex,
                        PersonnelId = cellData?.PersonnelId,
                        PersonnelName = cellData?.PersonnelName ?? string.Empty,
                        IsEmpty = cellData == null || !cellData.IsAssigned,
                        HasHardConflict = false, // 将在后续更新
                        HasSoftConflict = false, // 将在后续更新
                        ConflictTooltip = string.Empty
                    };

                    // 检查是否有冲突
                    if (Schedule.Conflicts != null && cellData?.ShiftId != null)
                    {
                        var conflicts = Schedule.Conflicts
                            .Where(c => c.RelatedShiftIds != null && c.RelatedShiftIds.Contains(cellData.ShiftId.Value))
                            .ToList();

                        if (conflicts.Any())
                        {
                            cellViewModel.HasHardConflict = conflicts.Any(c => c.IsHardConstraint);
                            cellViewModel.HasSoftConflict = conflicts.Any(c => !c.IsHardConstraint);
                            
                            // 构建工具提示
                            var tooltipLines = conflicts.Select(c => c.Message).Take(3);
                            cellViewModel.ConflictTooltip = string.Join("\n", tooltipLines);
                            if (conflicts.Count > 3)
                            {
                                cellViewModel.ConflictTooltip += $"\n...还有 {conflicts.Count - 3} 个冲突";
                            }
                        }
                    }

                    rowViewModel.Cells.Add(cellViewModel);
                }

                newGrid.Add(rowViewModel);
            }

            ScheduleGrid = newGrid;
        }

        #endregion
    }
}
