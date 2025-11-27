using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 右侧面板相关逻辑
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 右侧面板属性

        /// <summary>
        /// 右侧面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isRightPanelVisible = false;

        /// <summary>
        /// 详情标题
        /// </summary>
        [ObservableProperty]
        private string _detailTitle = string.Empty;

        /// <summary>
        /// 当前选中项
        /// </summary>
        [ObservableProperty]
        private object? _selectedItem;

        #endregion

        #region 右侧面板命令

        /// <summary>
        /// 解决冲突命令
        /// </summary>
        [RelayCommand]
        private async Task ResolveConflictAsync(ConflictResolutionOption option)
        {
            if (option == null) return;

            try
            {
                // TODO: 调用冲突解决服务
                // var result = await _conflictService.ResolveAsync(option);

                // 模拟解决冲突
                await Task.Delay(100);

                // 从冲突列表中移除对应的冲突
                var conflictToRemove = ConflictList.FirstOrDefault(c => c.Id == option.ConflictId.ToString());
                if (conflictToRemove != null)
                {
                    ConflictList.Remove(conflictToRemove);
                }

                // 更新统计摘要
                await RefreshStatisticsAsync();

                // 更新主内容区表格
                await RefreshScheduleGridAsync();

                // 标记为有未保存更改
                HasUnsavedChanges = true;

                // 显示下一个冲突或关闭面板
                if (ConflictList.Count > 0)
                {
                    await SelectConflictAsync(ConflictList[0]);
                }
                else
                {
                    // 调用 Layout.cs 中定义的 CloseDetailPanel 方法
                    CloseDetailPanel();
                }
            }
            catch (System.Exception ex)
            {
                // TODO: 显示错误对话框
                System.Diagnostics.Debug.WriteLine($"解决冲突失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存详情更改命令
        /// </summary>
        [RelayCommand]
        private async Task SaveDetailChangesAsync()
        {
            try
            {
                // TODO: 实现保存详情更改的逻辑
                // 根据 SelectedItem 的类型执行不同的保存操作
                
                // 标记为有未保存更改
                HasUnsavedChanges = true;

                // 关闭详情面板
                CloseDetailPanel();

                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存详情更改失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消详情更改命令
        /// </summary>
        [RelayCommand]
        private void CancelDetailChanges()
        {
            // 取消更改，直接关闭面板
            CloseDetailPanel();
        }

        #endregion

        #region 右侧面板辅助方法

        /// <summary>
        /// 清除所有选中状态
        /// </summary>
        private void ClearAllSelections()
        {
            // 清除冲突列表选中状态
            foreach (var conflict in ConflictList)
            {
                conflict.IsSelected = false;
            }

            // 清除表格单元格选中状态
            foreach (var row in ScheduleGrid)
            {
                foreach (var cell in row.Cells)
                {
                    cell.IsSelected = false;
                }
            }
        }

        #endregion
    }
}
