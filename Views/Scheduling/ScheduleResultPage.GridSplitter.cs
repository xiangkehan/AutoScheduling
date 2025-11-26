using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;

namespace AutoScheduling3.Views.Scheduling
{
    /// <summary>
    /// ScheduleResultPage - GridSplitter拖拽处理
    /// </summary>
    public sealed partial class ScheduleResultPage : Page
    {
        #region GridSplitter状态

        private bool _isLeftSplitterDragging = false;
        private bool _isRightSplitterDragging = false;
        private double _dragStartX = 0;
        private double _leftPanelStartWidth = 0;
        private double _rightPanelStartWidth = 0;

        #endregion

        #region GridSplitter事件处理

        /// <summary>
        /// GridSplitter鼠标进入 - 显示调整大小光标
        /// </summary>
        private void GridSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        /// <summary>
        /// GridSplitter鼠标离开 - 恢复默认光标
        /// </summary>
        private void GridSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isLeftSplitterDragging && !_isRightSplitterDragging)
            {
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }

        /// <summary>
        /// 左侧GridSplitter按下
        /// </summary>
        private void LeftGridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border splitter)
            {
                _isLeftSplitterDragging = true;
                _dragStartX = e.GetCurrentPoint(this).Position.X;
                
                // 获取当前左侧面板的实际宽度
                if (LeftPanelColumn != null)
                {
                    _leftPanelStartWidth = LeftPanelColumn.ActualWidth;
                }
                
                splitter.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 左侧GridSplitter移动
        /// </summary>
        private void LeftGridSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isLeftSplitterDragging && LeftPanelColumn != null)
            {
                var currentX = e.GetCurrentPoint(this).Position.X;
                var delta = currentX - _dragStartX;
                
                // 计算新宽度
                var newWidth = _leftPanelStartWidth + delta;
                
                // 应用最小和最大宽度限制
                newWidth = System.Math.Max(200, System.Math.Min(500, newWidth));
                
                // 更新列宽
                LeftPanelColumn.Width = new GridLength(newWidth, GridUnitType.Pixel);
                
                // 保存到ViewModel（将在释放时保存到偏好设置）
                if (this.ActualWidth > 0)
                {
                    var ratio = newWidth / this.ActualWidth;
                    ViewModel.LeftPanelWidth = new GridLength(ratio, GridUnitType.Star);
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// 右侧GridSplitter按下
        /// </summary>
        private void RightGridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border splitter)
            {
                _isRightSplitterDragging = true;
                _dragStartX = e.GetCurrentPoint(this).Position.X;
                
                // 获取当前右侧面板的实际宽度
                if (RightPanelColumn != null)
                {
                    _rightPanelStartWidth = RightPanelColumn.ActualWidth;
                }
                
                splitter.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 右侧GridSplitter移动
        /// </summary>
        private void RightGridSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isRightSplitterDragging && RightPanelColumn != null)
            {
                var currentX = e.GetCurrentPoint(this).Position.X;
                var delta = currentX - _dragStartX;
                
                // 计算新宽度（右侧面板向左拖动时delta为负，宽度增加）
                var newWidth = _rightPanelStartWidth - delta;
                
                // 应用最小和最大宽度限制
                newWidth = System.Math.Max(250, System.Math.Min(600, newWidth));
                
                // 更新列宽
                RightPanelColumn.Width = new GridLength(newWidth, GridUnitType.Pixel);
                
                // 保存到ViewModel（将在释放时保存到偏好设置）
                if (this.ActualWidth > 0)
                {
                    var ratio = newWidth / this.ActualWidth;
                    ViewModel.RightPanelWidth = new GridLength(ratio, GridUnitType.Star);
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// GridSplitter释放
        /// </summary>
        private void GridSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isLeftSplitterDragging || _isRightSplitterDragging)
            {
                // 保存布局偏好
                _ = SaveLayoutPreferencesAsync();
                
                _isLeftSplitterDragging = false;
                _isRightSplitterDragging = false;
                
                if (sender is Border splitter)
                {
                    splitter.ReleasePointerCaptures();
                }
                
                // 恢复默认光标
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// 保存布局偏好
        /// </summary>
        private async System.Threading.Tasks.Task SaveLayoutPreferencesAsync()
        {
            try
            {
                var layoutService = ((App)Application.Current).ServiceProvider
                    .GetService(typeof(Services.Interfaces.ILayoutPreferenceService)) 
                    as Services.Interfaces.ILayoutPreferenceService;
                
                if (layoutService != null && this.ActualWidth > 0)
                {
                    // 计算比例
                    double leftRatio = LeftPanelColumn?.ActualWidth / this.ActualWidth ?? 0.2;
                    double rightRatio = RightPanelColumn?.ActualWidth / this.ActualWidth ?? 0.25;
                    
                    // 保存宽度
                    await layoutService.SaveLeftPanelWidthAsync(leftRatio);
                    await layoutService.SaveRightPanelWidthAsync(rightRatio);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存布局偏好失败: {ex.Message}");
            }
        }

        #endregion

        #region 窗口大小变化处理

        /// <summary>
        /// 页面大小变化时更新布局模式
        /// </summary>
        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.UpdateLayoutMode(e.NewSize.Width);
            }
        }

        #endregion
    }
}
