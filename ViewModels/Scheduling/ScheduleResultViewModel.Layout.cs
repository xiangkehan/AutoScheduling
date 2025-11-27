using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 布局相关功能
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 新UI布局属性

        private bool _useNewUI = true;
        /// <summary>
        /// 是否使用新UI（Feature Flag）
        /// </summary>
        public bool UseNewUI
        {
            get => _useNewUI;
            set
            {
                if (SetProperty(ref _useNewUI, value))
                {
                    OnPropertyChanged(nameof(UseOldUI));
                }
            }
        }

        /// <summary>
        /// 是否使用旧UI
        /// </summary>
        public bool UseOldUI => !UseNewUI;

        private Microsoft.UI.Xaml.GridLength _leftPanelWidth = new Microsoft.UI.Xaml.GridLength(300);
        /// <summary>
        /// 左侧面板宽度
        /// </summary>
        public Microsoft.UI.Xaml.GridLength LeftPanelWidth
        {
            get => _leftPanelWidth;
            set
            {
                if (SetProperty(ref _leftPanelWidth, value))
                {
                    _ = SaveLeftPanelWidthAsync(value.Value);
                }
            }
        }

        private Microsoft.UI.Xaml.GridLength _rightPanelWidth = new Microsoft.UI.Xaml.GridLength(350);
        /// <summary>
        /// 右侧面板宽度
        /// </summary>
        public Microsoft.UI.Xaml.GridLength RightPanelWidth
        {
            get => _rightPanelWidth;
            set
            {
                if (SetProperty(ref _rightPanelWidth, value))
                {
                    _ = SaveRightPanelWidthAsync(value.Value);
                }
            }
        }

        #endregion

        #region 布局命令

        /// <summary>
        /// 切换左侧面板可见性命令
        /// </summary>
        [RelayCommand]
        private void ToggleLeftPanel()
        {
            IsLeftPanelVisible = !IsLeftPanelVisible;
        }

        /// <summary>
        /// 切换右侧面板可见性命令
        /// </summary>
        [RelayCommand]
        private void ToggleRightPanel()
        {
            IsRightPanelVisible = !IsRightPanelVisible;
        }

        /// <summary>
        /// 关闭详情面板命令
        /// </summary>
        [RelayCommand]
        private void CloseDetailPanel()
        {
            IsRightPanelVisible = false;
        }

        #endregion

        #region 布局偏好加载和保存

        /// <summary>
        /// 加载布局偏好
        /// </summary>
        public async Task LoadLayoutPreferencesAsync()
        {
            if (_layoutPreferenceService == null)
            {
                return;
            }

            try
            {
                var preferences = await _layoutPreferenceService.LoadAsync();
                
                // 应用偏好设置（不触发保存）
                _leftPanelWidth = new Microsoft.UI.Xaml.GridLength(preferences.LeftPanelWidth);
                _rightPanelWidth = new Microsoft.UI.Xaml.GridLength(preferences.RightPanelWidth);
                _isLeftPanelVisible = preferences.IsLeftPanelVisible;
                _isRightPanelVisible = preferences.IsRightPanelVisible;

                // 通知属性变化
                OnPropertyChanged(nameof(LeftPanelWidth));
                OnPropertyChanged(nameof(RightPanelWidth));
                OnPropertyChanged(nameof(IsLeftPanelVisible));
                OnPropertyChanged(nameof(IsRightPanelVisible));

                // 如果有偏好的视图模式，应用它
                if (!string.IsNullOrEmpty(preferences.PreferredViewMode) && 
                    Enum.TryParse<ViewMode>(preferences.PreferredViewMode, out var viewMode))
                {
                    _currentViewMode = viewMode;
                    OnPropertyChanged(nameof(CurrentViewMode));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载布局偏好失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存左侧面板宽度
        /// </summary>
        private async Task SaveLeftPanelWidthAsync(double width)
        {
            if (_layoutPreferenceService == null)
            {
                return;
            }

            try
            {
                await _layoutPreferenceService.SaveLeftPanelWidthAsync(width);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存左侧面板宽度失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存右侧面板宽度
        /// </summary>
        private async Task SaveRightPanelWidthAsync(double width)
        {
            if (_layoutPreferenceService == null)
            {
                return;
            }

            try
            {
                await _layoutPreferenceService.SaveRightPanelWidthAsync(width);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存右侧面板宽度失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存面板可见性
        /// </summary>
        private async Task SavePanelVisibilityAsync()
        {
            if (_layoutPreferenceService == null)
            {
                return;
            }

            try
            {
                await _layoutPreferenceService.SavePanelVisibilityAsync(
                    IsLeftPanelVisible, 
                    IsRightPanelVisible);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存面板可见性失败: {ex.Message}");
            }
        }

        #endregion
    }
}
