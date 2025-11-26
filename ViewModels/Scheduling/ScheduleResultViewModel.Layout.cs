using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 布局相关逻辑
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 布局属性

        /// <summary>
        /// 左侧面板宽度
        /// </summary>
        [ObservableProperty]
        private GridLength _leftPanelWidth = new GridLength(0.2, GridUnitType.Star);

        /// <summary>
        /// 右侧面板宽度
        /// </summary>
        [ObservableProperty]
        private GridLength _rightPanelWidth = new GridLength(0.25, GridUnitType.Star);

        /// <summary>
        /// 当前布局模式
        /// </summary>
        [ObservableProperty]
        private LayoutMode _currentLayoutMode = LayoutMode.Large;

        /// <summary>
        /// 是否使用新UI
        /// </summary>
        [ObservableProperty]
        private bool _useNewUI = false;

        /// <summary>
        /// 是否使用旧UI
        /// </summary>
        public bool UseOldUI => !UseNewUI;

        #endregion

        #region 布局方法

        /// <summary>
        /// 当布局模式改变时
        /// </summary>
        partial void OnCurrentLayoutModeChanged(LayoutMode value)
        {
            switch (value)
            {
                case LayoutMode.Large:
                    LeftPanelWidth = new GridLength(0.2, GridUnitType.Star);
                    RightPanelWidth = new GridLength(0.25, GridUnitType.Star);
                    IsLeftPanelVisible = true;
                    break;

                case LayoutMode.Medium:
                    LeftPanelWidth = new GridLength(0.2, GridUnitType.Star);
                    RightPanelWidth = new GridLength(0.2, GridUnitType.Star);
                    IsLeftPanelVisible = true;
                    break;

                case LayoutMode.Small:
                    LeftPanelWidth = new GridLength(0.15, GridUnitType.Star);
                    IsLeftPanelVisible = true;
                    IsRightPanelVisible = false; // 默认隐藏右侧
                    break;

                case LayoutMode.Compact:
                    IsLeftPanelVisible = false; // 折叠为图标模式
                    IsRightPanelVisible = false;
                    break;
            }
        }

        /// <summary>
        /// 更新布局模式（根据窗口大小）
        /// </summary>
        public void UpdateLayoutMode(double windowWidth)
        {
            CurrentLayoutMode = windowWidth switch
            {
                >= 1920 => LayoutMode.Large,
                >= 1366 => LayoutMode.Medium,
                >= 1024 => LayoutMode.Small,
                _ => LayoutMode.Compact
            };
        }

        /// <summary>
        /// 保存当前布局偏好
        /// </summary>
        public async System.Threading.Tasks.Task SaveLayoutPreferencesAsync()
        {
            try
            {
                var layoutService = ((App)Microsoft.UI.Xaml.Application.Current).ServiceProvider
                    .GetService(typeof(Services.Interfaces.ILayoutPreferenceService)) 
                    as Services.Interfaces.ILayoutPreferenceService;
                
                if (layoutService != null)
                {
                    // 保存面板可见性
                    await layoutService.SavePanelVisibilityAsync(IsLeftPanelVisible, IsRightPanelVisible);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存布局偏好失败: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 布局模式枚举
    /// </summary>
    public enum LayoutMode
    {
        /// <summary>
        /// 大屏幕（1920px+）
        /// </summary>
        Large,

        /// <summary>
        /// 中等屏幕（1366px-1920px）
        /// </summary>
        Medium,

        /// <summary>
        /// 小屏幕（1024px-1366px）
        /// </summary>
        Small,

        /// <summary>
        /// 紧凑模式（<1024px）
        /// </summary>
        Compact
    }
}
