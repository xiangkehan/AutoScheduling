using System;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 键盘快捷键相关功能
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 键盘快捷键处理

        /// <summary>
        /// 处理键盘快捷键
        /// </summary>
        /// <param name="args">键盘事件参数</param>
        /// <returns>是否已处理</returns>
        public async Task<bool> HandleKeyboardShortcutAsync(KeyRoutedEventArgs args)
        {
            var key = args.Key;
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            // Ctrl + F: 展开筛选搜索栏并聚焦
            if (ctrl && key == VirtualKey.F)
            {
                await ExpandFilterAndFocusSearchAsync();
                return true;
            }

            // Ctrl + E: 打开导出对话框
            if (ctrl && key == VirtualKey.E)
            {
                await ExportExcelAsync();
                return true;
            }

            // Ctrl + L: 切换左侧面板显示/隐藏
            if (ctrl && key == VirtualKey.L)
            {
                ToggleLeftPanel();
                return true;
            }

            // Ctrl + R: 切换右侧面板显示/隐藏
            if (ctrl && key == VirtualKey.R)
            {
                ToggleRightPanel();
                return true;
            }

            // Esc: 折叠筛选栏或关闭对话框
            if (key == VirtualKey.Escape)
            {
                await HandleEscapeKeyAsync();
                return true;
            }

            // F11: 进入/退出全屏模式
            if (key == VirtualKey.F11)
            {
                await ToggleFullScreenAsync();
                return true;
            }

            // Ctrl + 1/2/3/4: 切换视图模式
            if (ctrl && key >= VirtualKey.Number1 && key <= VirtualKey.Number4)
            {
                var viewMode = (ViewMode)(key - VirtualKey.Number1);
                CurrentViewMode = viewMode;
                return true;
            }

            // Ctrl + Z: 撤销
            if (ctrl && !shift && key == VirtualKey.Z)
            {
                await UndoAsync();
                return true;
            }

            // Ctrl + Y 或 Ctrl + Shift + Z: 重做
            if ((ctrl && key == VirtualKey.Y) || (ctrl && shift && key == VirtualKey.Z))
            {
                await RedoAsync();
                return true;
            }

            // Ctrl + S: 保存
            if (ctrl && key == VirtualKey.S)
            {
                if (SaveChangesCommand.CanExecute(null))
                {
                    await SaveChangesCommand.ExecuteAsync(null);
                }
                return true;
            }

            // 方向键导航（在表格中）
            if (CurrentViewMode == ViewMode.Grid)
            {
                if (key == VirtualKey.Up || key == VirtualKey.Down || 
                    key == VirtualKey.Left || key == VirtualKey.Right)
                {
                    await HandleGridNavigationAsync(key);
                    return true;
                }

                // Enter: 打开编辑界面
                if (key == VirtualKey.Enter)
                {
                    await OpenEditInterfaceAsync();
                    return true;
                }

                // Space: 显示详情
                if (key == VirtualKey.Space)
                {
                    await ShowDetailsAsync();
                    return true;
                }
            }

            // 方向键导航（在冲突列表中）
            if (IsConflictListFocused)
            {
                if (key == VirtualKey.Up || key == VirtualKey.Down)
                {
                    await HandleConflictListNavigationAsync(key);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 全局快捷键命令

        /// <summary>
        /// 展开筛选栏并聚焦搜索框
        /// </summary>
        [RelayCommand]
        private async Task ExpandFilterAndFocusSearchAsync()
        {
            IsFilterExpanded = true;
            // 触发搜索框聚焦事件
            OnPropertyChanged(nameof(ShouldFocusSearchBox));
            await Task.CompletedTask;
        }

        /// <summary>
        /// 是否应该聚焦搜索框
        /// </summary>
        public bool ShouldFocusSearchBox { get; private set; }

        /// <summary>
        /// 处理 Escape 键
        /// </summary>
        private async Task HandleEscapeKeyAsync()
        {
            if (IsFilterExpanded)
            {
                // 折叠筛选栏
                IsFilterExpanded = false;
            }
            else if (IsRightPanelVisible)
            {
                // 关闭右侧详情面板
                CloseDetailPanel();
            }

            await Task.CompletedTask;
        }

        #endregion

        #region 视图切换快捷键

        // 视图切换已在 HandleKeyboardShortcutAsync 中实现

        #endregion

        #region 表格导航快捷键

        private int _selectedRowIndex = 0;
        private int _selectedColumnIndex = 0;

        /// <summary>
        /// 处理表格导航
        /// </summary>
        private async Task HandleGridNavigationAsync(VirtualKey key)
        {
            if (ScheduleGrid == null || ScheduleGrid.Count == 0)
            {
                return;
            }

            var maxRow = ScheduleGrid.Count - 1;
            var maxColumn = ScheduleGrid[0].Cells.Count - 1;

            switch (key)
            {
                case VirtualKey.Up:
                    _selectedRowIndex = Math.Max(0, _selectedRowIndex - 1);
                    break;

                case VirtualKey.Down:
                    _selectedRowIndex = Math.Min(maxRow, _selectedRowIndex + 1);
                    break;

                case VirtualKey.Left:
                    _selectedColumnIndex = Math.Max(0, _selectedColumnIndex - 1);
                    break;

                case VirtualKey.Right:
                    _selectedColumnIndex = Math.Min(maxColumn, _selectedColumnIndex + 1);
                    break;
            }

            // 选中对应的单元格
            var cell = ScheduleGrid[_selectedRowIndex].Cells[_selectedColumnIndex];
            await SelectCellAsync(cell);
        }

        /// <summary>
        /// 打开编辑界面
        /// </summary>
        private async Task OpenEditInterfaceAsync()
        {
            if (SelectedItem is ScheduleCellViewModel cell)
            {
                // 打开编辑界面
                IsRightPanelVisible = true;
                DetailTitle = "编辑班次";
                // TODO: 切换到编辑模式
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 显示详情
        /// </summary>
        private async Task ShowDetailsAsync()
        {
            if (SelectedItem != null)
            {
                IsRightPanelVisible = true;
                await LoadDetailContentAsync(SelectedItem);
            }
        }

        #endregion

        #region 冲突列表导航快捷键

        private bool _isConflictListFocused = false;

        /// <summary>
        /// 冲突列表是否获得焦点
        /// </summary>
        public bool IsConflictListFocused
        {
            get => _isConflictListFocused;
            set => SetProperty(ref _isConflictListFocused, value);
        }

        private int _selectedConflictIndex = 0;

        /// <summary>
        /// 处理冲突列表导航
        /// </summary>
        private async Task HandleConflictListNavigationAsync(VirtualKey key)
        {
            if (ConflictList == null || ConflictList.Count == 0)
            {
                return;
            }

            var maxIndex = ConflictList.Count - 1;

            switch (key)
            {
                case VirtualKey.Up:
                    _selectedConflictIndex = Math.Max(0, _selectedConflictIndex - 1);
                    break;

                case VirtualKey.Down:
                    _selectedConflictIndex = Math.Min(maxIndex, _selectedConflictIndex + 1);
                    break;
            }

            // 选中对应的冲突项
            var conflict = ConflictList[_selectedConflictIndex];
            await SelectConflictAsync(conflict);
        }

        #endregion

        #region 撤销/重做快捷键

        /// <summary>
        /// 撤销命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private async Task UndoAsync()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            var action = _undoStack.Pop();
            _redoStack.Push(action);

            // 执行撤销操作
            await action.UndoAsync();

            // 更新命令状态
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();

            // 标记为有未保存更改
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// 重做命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRedo))]
        private async Task RedoAsync()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            var action = _redoStack.Pop();
            _undoStack.Push(action);

            // 执行重做操作
            await action.RedoAsync();

            // 更新命令状态
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();

            // 标记为有未保存更改
            HasUnsavedChanges = true;
        }

        private bool CanUndo() => _undoStack.Count > 0;
        private bool CanRedo() => _redoStack.Count > 0;

        #endregion

        #region 撤销/重做栈

        private System.Collections.Generic.Stack<IUndoableAction> _undoStack = new();
        private System.Collections.Generic.Stack<IUndoableAction> _redoStack = new();

        /// <summary>
        /// 添加可撤销操作
        /// </summary>
        /// <param name="action">操作</param>
        public void AddUndoableAction(IUndoableAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear(); // 清空重做栈

            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 清空撤销/重做栈
        /// </summary>
        public void ClearUndoRedoStack()
        {
            _undoStack.Clear();
            _redoStack.Clear();

            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }

    /// <summary>
    /// 可撤销操作接口
    /// </summary>
    public interface IUndoableAction
    {
        /// <summary>
        /// 操作描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 执行撤销
        /// </summary>
        Task UndoAsync();

        /// <summary>
        /// 执行重做
        /// </summary>
        Task RedoAsync();
    }
}
