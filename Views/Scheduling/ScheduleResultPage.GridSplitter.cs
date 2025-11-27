using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;

namespace AutoScheduling3.Views.Scheduling
{
    /// <summary>
    /// ScheduleResultPage - GridSplitter 拖拽处理
    /// </summary>
    public sealed partial class ScheduleResultPage : Page
    {
        #region GridSplitter 状态

        private bool _isLeftSplitterDragging = false;
        private bool _isRightSplitterDragging = false;
        private double _leftSplitterStartX = 0;
        private double _rightSplitterStartX = 0;
        private double _leftPanelStartWidth = 0;
        private double _rightPanelStartWidth = 0;

        #endregion

        #region GridSplitter 事件处理

        /// <summary>
        /// GridSplitter 鼠标进入 - 显示调整大小光标
        /// </summary>
        private void GridSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border)
            {
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            }
        }

        /// <summary>
        /// GridSplitter 鼠标离开 - 恢复默认光标
        /// </summary>
        private void GridSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isLeftSplitterDragging && !_isRightSplitterDragging)
            {
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }

        /// <summary>
        /// 左侧 GridSplitter 按下
        /// </summary>
        private void LeftGridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border splitter)
            {
                _isLeftSplitterDragging = true;
                _leftSplitterStartX = e.GetCurrentPoint(this).Position.X;
                _leftPanelStartWidth = ViewModel.LeftPanelWidth.Value;
                
                splitter.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 左侧 GridSplitter 移动
        /// </summary>
        private void LeftGridSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isLeftSplitterDragging && sender is Border)
            {
                var currentX = e.GetCurrentPoint(this).Position.X;
                var deltaX = currentX - _leftSplitterStartX;
                
                var newWidth = _leftPanelStartWidth + deltaX;
                
                // 限制宽度范围
                const double minWidth = 200;
                const double maxWidth = 500;
                
                if (newWidth >= minWidth && newWidth <= maxWidth)
                {
                    ViewModel.LeftPanelWidth = new Microsoft.UI.Xaml.GridLength(newWidth);
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// 右侧 GridSplitter 按下
        /// </summary>
        private void RightGridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border splitter)
            {
                _isRightSplitterDragging = true;
                _rightSplitterStartX = e.GetCurrentPoint(this).Position.X;
                _rightPanelStartWidth = ViewModel.RightPanelWidth.Value;
                
                splitter.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 右侧 GridSplitter 移动
        /// </summary>
        private void RightGridSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isRightSplitterDragging && sender is Border)
            {
                var currentX = e.GetCurrentPoint(this).Position.X;
                var deltaX = _rightSplitterStartX - currentX; // 注意：右侧是反向的
                
                var newWidth = _rightPanelStartWidth + deltaX;
                
                // 限制宽度范围
                const double minWidth = 250;
                const double maxWidth = 600;
                
                if (newWidth >= minWidth && newWidth <= maxWidth)
                {
                    ViewModel.RightPanelWidth = new Microsoft.UI.Xaml.GridLength(newWidth);
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// GridSplitter 释放
        /// </summary>
        private void GridSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border splitter)
            {
                _isLeftSplitterDragging = false;
                _isRightSplitterDragging = false;
                
                splitter.ReleasePointerCaptures();
                
                // 恢复默认光标
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                
                e.Handled = true;
            }
        }

        #endregion

        #region 窗口大小变化处理

        /// <summary>
        /// 页面大小变化时的处理（响应式布局）
        /// </summary>
        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 根据窗口宽度调整布局
            var width = e.NewSize.Width;
            
            // 小屏幕适配（<1024px）
            if (width < 1024)
            {
                // 自动隐藏右侧面板
                if (ViewModel.IsRightPanelVisible)
                {
                    ViewModel.IsRightPanelVisible = false;
                }
                
                // 缩小左侧面板
                if (ViewModel.LeftPanelWidth.Value > 250)
                {
                    ViewModel.LeftPanelWidth = new Microsoft.UI.Xaml.GridLength(250);
                }
            }
            // 中等屏幕（1024px-1366px）
            else if (width < 1366)
            {
                // 默认隐藏右侧面板，按需显示
                // 不自动改变，由用户控制
            }
            // 大屏幕（>=1366px）
            else
            {
                // 恢复默认宽度
                if (ViewModel.LeftPanelWidth.Value < 300)
                {
                    ViewModel.LeftPanelWidth = new Microsoft.UI.Xaml.GridLength(300);
                }
            }
        }

        #endregion
    }
}
