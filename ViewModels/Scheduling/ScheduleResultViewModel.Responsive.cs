using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 布局模式枚举
    /// </summary>
    public enum LayoutMode
    {
        /// <summary>
        /// 大屏幕模式 (>= 1920px)
        /// </summary>
        Large,

        /// <summary>
        /// 中等屏幕模式 (1366px - 1920px)
        /// </summary>
        Medium,

        /// <summary>
        /// 小屏幕模式 (1024px - 1366px)
        /// </summary>
        Small,

        /// <summary>
        /// 紧凑模式 (< 1024px)
        /// </summary>
        Compact
    }

    /// <summary>
    /// ScheduleResultViewModel - 响应式布局相关功能
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 响应式布局属性

        [ObservableProperty]
        private LayoutMode _currentLayoutMode = LayoutMode.Large;

        [ObservableProperty]
        private bool _isLeftPanelCollapsed = false;

        [ObservableProperty]
        private bool _isFullScreenMode = false;

        /// <summary>
        /// 当前窗口宽度
        /// </summary>
        [ObservableProperty]
        private double _currentWindowWidth = 1920;

        /// <summary>
        /// 当前窗口高度
        /// </summary>
        [ObservableProperty]
        private double _currentWindowHeight = 1080;

        #endregion

        #region 响应式布局命令

        /// <summary>
        /// 切换全屏模式命令
        /// </summary>
        [RelayCommand]
        private async Task ToggleFullScreenAsync()
        {
            IsFullScreenMode = !IsFullScreenMode;

            if (IsFullScreenMode)
            {
                // 进入全屏：隐藏左右面板
                _previousLeftPanelVisible = IsLeftPanelVisible;
                _previousRightPanelVisible = IsRightPanelVisible;

                IsLeftPanelVisible = false;
                IsRightPanelVisible = false;
            }
            else
            {
                // 退出全屏：恢复之前的状态
                IsLeftPanelVisible = _previousLeftPanelVisible;
                IsRightPanelVisible = _previousRightPanelVisible;
            }

            await Task.CompletedTask;
        }

        private bool _previousLeftPanelVisible = true;
        private bool _previousRightPanelVisible = false;

        /// <summary>
        /// 折叠/展开左侧面板命令
        /// </summary>
        [RelayCommand]
        private void ToggleLeftPanelCollapse()
        {
            IsLeftPanelCollapsed = !IsLeftPanelCollapsed;

            // 如果折叠，调整宽度为图标模式
            if (IsLeftPanelCollapsed)
            {
                _previousLeftPanelWidth = LeftPanelWidth;
                LeftPanelWidth = new GridLength(60); // 图标模式宽度
            }
            else
            {
                LeftPanelWidth = _previousLeftPanelWidth;
            }
        }

        private GridLength _previousLeftPanelWidth = new GridLength(300);

        #endregion

        #region 响应式布局逻辑

        /// <summary>
        /// 当布局模式改变时
        /// </summary>
        partial void OnCurrentLayoutModeChanged(LayoutMode value)
        {
            ApplyLayoutMode(value);
        }

        /// <summary>
        /// 应用布局模式
        /// </summary>
        private void ApplyLayoutMode(LayoutMode mode)
        {
            switch (mode)
            {
                case LayoutMode.Large:
                    // 大屏幕：左侧20%，中间55%，右侧25%
                    if (!IsLeftPanelCollapsed)
                    {
                        LeftPanelWidth = new GridLength(0.2, GridUnitType.Star);
                    }
                    RightPanelWidth = new GridLength(0.25, GridUnitType.Star);
                    IsLeftPanelVisible = true;
                    break;

                case LayoutMode.Medium:
                    // 中等屏幕：左侧20%，中间60%，右侧20%
                    if (!IsLeftPanelCollapsed)
                    {
                        LeftPanelWidth = new GridLength(0.2, GridUnitType.Star);
                    }
                    RightPanelWidth = new GridLength(0.2, GridUnitType.Star);
                    IsLeftPanelVisible = true;
                    break;

                case LayoutMode.Small:
                    // 小屏幕：左侧15%，中间85%，默认隐藏右侧
                    if (!IsLeftPanelCollapsed)
                    {
                        LeftPanelWidth = new GridLength(0.15, GridUnitType.Star);
                    }
                    IsLeftPanelVisible = true;
                    IsRightPanelVisible = false;
                    break;

                case LayoutMode.Compact:
                    // 紧凑模式：左侧折叠为图标，中间占据主要空间
                    IsLeftPanelCollapsed = true;
                    IsLeftPanelVisible = true;
                    IsRightPanelVisible = false;
                    LeftPanelWidth = new GridLength(60); // 图标模式
                    break;
            }
        }

        /// <summary>
        /// 根据窗口宽度自动调整布局模式
        /// </summary>
        /// <param name="width">窗口宽度</param>
        public void AutoAdjustLayoutMode(double width)
        {
            CurrentWindowWidth = width;

            var newMode = width switch
            {
                >= 1920 => LayoutMode.Large,
                >= 1366 => LayoutMode.Medium,
                >= 1024 => LayoutMode.Small,
                _ => LayoutMode.Compact
            };

            if (newMode != CurrentLayoutMode)
            {
                CurrentLayoutMode = newMode;
            }
        }

        #endregion

        #region 小屏幕适配

        /// <summary>
        /// 是否为小屏幕模式
        /// </summary>
        public bool IsSmallScreen => CurrentLayoutMode == LayoutMode.Small || CurrentLayoutMode == LayoutMode.Compact;

        /// <summary>
        /// 是否为紧凑模式
        /// </summary>
        public bool IsCompactMode => CurrentLayoutMode == LayoutMode.Compact;

        #endregion

        #region 平板适配

        /// <summary>
        /// 是否为平板模式
        /// </summary>
        public bool IsTabletMode => CurrentWindowWidth < 1024;

        /// <summary>
        /// 左侧面板是否应该显示为浮层
        /// </summary>
        public bool ShouldShowLeftPanelAsOverlay => IsTabletMode && !IsLeftPanelCollapsed;

        #endregion

        #region 折叠模式

        /// <summary>
        /// 左侧面板图标模式宽度
        /// </summary>
        public const double IconModeWidth = 60;

        /// <summary>
        /// 左侧面板正常模式最小宽度
        /// </summary>
        public const double NormalModeMinWidth = 200;

        /// <summary>
        /// 左侧面板正常模式最大宽度
        /// </summary>
        public const double NormalModeMaxWidth = 500;

        /// <summary>
        /// 展开左侧面板（从图标模式）
        /// </summary>
        [RelayCommand]
        private void ExpandLeftPanel()
        {
            if (IsLeftPanelCollapsed)
            {
                IsLeftPanelCollapsed = false;
                LeftPanelWidth = _previousLeftPanelWidth;
            }
        }

        /// <summary>
        /// 折叠左侧面板（到图标模式）
        /// </summary>
        [RelayCommand]
        private void CollapseLeftPanel()
        {
            if (!IsLeftPanelCollapsed)
            {
                _previousLeftPanelWidth = LeftPanelWidth;
                IsLeftPanelCollapsed = true;
                LeftPanelWidth = new GridLength(IconModeWidth);
            }
        }

        #endregion

        #region 布局动画

        /// <summary>
        /// 布局切换动画持续时间（毫秒）
        /// </summary>
        public const int LayoutTransitionDuration = 300;

        /// <summary>
        /// 是否正在进行布局动画
        /// </summary>
        [ObservableProperty]
        private bool _isLayoutAnimating = false;

        /// <summary>
        /// 执行布局切换动画
        /// </summary>
        private async Task AnimateLayoutTransitionAsync()
        {
            IsLayoutAnimating = true;
            await Task.Delay(LayoutTransitionDuration);
            IsLayoutAnimating = false;
        }

        #endregion
    }
}
