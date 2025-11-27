using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 用户偏好保存相关功能
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 布局偏好

        /// <summary>
        /// 保存布局偏好
        /// </summary>
        [RelayCommand]
        private async Task SaveLayoutPreferencesAsync()
        {
            if (_layoutPreferenceService == null)
            {
                return;
            }

            try
            {
                var preferences = new LayoutPreferences
                {
                    LeftPanelWidth = LeftPanelWidth.Value,
                    RightPanelWidth = RightPanelWidth.Value,
                    IsLeftPanelVisible = IsLeftPanelVisible,
                    IsRightPanelVisible = IsRightPanelVisible,
                    IsLeftPanelCollapsed = IsLeftPanelCollapsed,
                    PreferredViewMode = CurrentViewMode.ToString(),
                    LastUpdated = DateTime.Now
                };

                await _layoutPreferenceService.SaveAsync(preferences);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存布局偏好失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载布局偏好（已在 Layout.cs 中实现，这里提供额外的功能）
        /// </summary>
        [RelayCommand]
        private async Task RestoreDefaultLayoutAsync()
        {
            // 恢复默认布局
            LeftPanelWidth = new Microsoft.UI.Xaml.GridLength(300);
            RightPanelWidth = new Microsoft.UI.Xaml.GridLength(350);
            IsLeftPanelVisible = true;
            IsRightPanelVisible = false;
            IsLeftPanelCollapsed = false;
            CurrentViewMode = ViewMode.Grid;

            // 保存默认布局
            await SaveLayoutPreferencesAsync();
        }

        #endregion

        #region 视图模式偏好

        /// <summary>
        /// 保存视图模式偏好
        /// </summary>
        private async Task SaveViewModePreferenceAsync(ViewMode viewMode)
        {
            if (_layoutPreferenceService == null)
            {
                return;
            }

            try
            {
                await _layoutPreferenceService.SaveViewModeAsync(viewMode.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存视图模式偏好失败: {ex.Message}");
            }
        }



        #endregion

        #region 筛选预设

        private List<FilterPreset> _filterPresets = new();

        /// <summary>
        /// 筛选预设列表
        /// </summary>
        public List<FilterPreset> FilterPresets
        {
            get => _filterPresets;
            set => SetProperty(ref _filterPresets, value);
        }

        /// <summary>
        /// 保存筛选预设
        /// </summary>
        /// <param name="name">预设名称</param>
        [RelayCommand]
        private async Task SaveFilterPresetAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var preset = new FilterPreset
            {
                Name = name,
                FilterOptions = FilterOptions.Clone(),
                CreatedAt = DateTime.Now
            };

            // 检查是否已存在同名预设
            var existing = _filterPresets.FirstOrDefault(p => p.Name == name);
            if (existing != null)
            {
                _filterPresets.Remove(existing);
            }

            _filterPresets.Add(preset);

            // 保存到本地存储
            await SaveFilterPresetsToStorageAsync();
        }

        /// <summary>
        /// 加载筛选预设
        /// </summary>
        /// <param name="preset">预设</param>
        [RelayCommand]
        private async Task LoadFilterPresetAsync(FilterPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            FilterOptions = preset.FilterOptions.Clone();
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// 删除筛选预设
        /// </summary>
        /// <param name="preset">预设</param>
        [RelayCommand]
        private async Task DeleteFilterPresetAsync(FilterPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            _filterPresets.Remove(preset);
            await SaveFilterPresetsToStorageAsync();
        }

        /// <summary>
        /// 保存筛选预设到本地存储
        /// </summary>
        private async Task SaveFilterPresetsToStorageAsync()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_filterPresets);
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["FilterPresets"] = json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存筛选预设失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 从本地存储加载筛选预设
        /// </summary>
        private async Task LoadFilterPresetsFromStorageAsync()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue("FilterPresets", out var value) && value is string json)
                {
                    _filterPresets = System.Text.Json.JsonSerializer.Deserialize<List<FilterPreset>>(json) ?? new();
                    OnPropertyChanged(nameof(FilterPresets));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载筛选预设失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        #endregion

        #region 搜索历史

        private List<string> _searchHistory = new();
        private const int MaxSearchHistoryCount = 10;

        /// <summary>
        /// 搜索历史列表
        /// </summary>
        public List<string> SearchHistory
        {
            get => _searchHistory;
            set => SetProperty(ref _searchHistory, value);
        }

        /// <summary>
        /// 添加搜索历史
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        public async Task AddSearchHistoryAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            // 移除已存在的相同项
            _searchHistory.Remove(searchText);

            // 添加到列表开头
            _searchHistory.Insert(0, searchText);

            // 限制历史记录数量
            if (_searchHistory.Count > MaxSearchHistoryCount)
            {
                _searchHistory = _searchHistory.Take(MaxSearchHistoryCount).ToList();
            }

            OnPropertyChanged(nameof(SearchHistory));

            // 保存到本地存储
            await SaveSearchHistoryToStorageAsync();
        }

        /// <summary>
        /// 清除搜索历史
        /// </summary>
        [RelayCommand]
        private async Task ClearSearchHistoryAsync()
        {
            _searchHistory.Clear();
            OnPropertyChanged(nameof(SearchHistory));
            await SaveSearchHistoryToStorageAsync();
        }

        /// <summary>
        /// 保存搜索历史到本地存储
        /// </summary>
        private async Task SaveSearchHistoryToStorageAsync()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_searchHistory);
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["SearchHistory"] = json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存搜索历史失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 从本地存储加载搜索历史
        /// </summary>
        private async Task LoadSearchHistoryFromStorageAsync()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue("SearchHistory", out var value) && value is string json)
                {
                    _searchHistory = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
                    OnPropertyChanged(nameof(SearchHistory));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载搜索历史失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        #endregion

        #region 视图状态保存和恢复

        /// <summary>
        /// 保存当前视图状态
        /// </summary>
        private void SaveCurrentViewState()
        {
            // 保存筛选条件
            _savedFilters[CurrentViewMode] = new SearchFilters
            {
                SearchText = SearchText,
                FilterOptions = FilterOptions.Clone()
            };

            // 保存滚动位置
            _savedScrollPositions[CurrentViewMode] = new ScrollPosition
            {
                VerticalOffset = _currentVerticalOffset,
                HorizontalOffset = _currentHorizontalOffset
            };
        }

        /// <summary>
        /// 恢复视图状态
        /// </summary>
        /// <param name="viewMode">视图模式</param>
        private void RestoreViewState(ViewMode viewMode)
        {
            // 恢复筛选条件
            if (_savedFilters.TryGetValue(viewMode, out var filters))
            {
                SearchText = filters.SearchText;
                FilterOptions = filters.FilterOptions.Clone();
            }

            // 恢复滚动位置
            if (_savedScrollPositions.TryGetValue(viewMode, out var position))
            {
                _currentVerticalOffset = position.VerticalOffset;
                _currentHorizontalOffset = position.HorizontalOffset;
                OnPropertyChanged(nameof(ShouldRestoreScrollPosition));
            }
        }

        private double _currentVerticalOffset = 0;
        private double _currentHorizontalOffset = 0;

        /// <summary>
        /// 是否应该恢复滚动位置
        /// </summary>
        public bool ShouldRestoreScrollPosition { get; private set; }

        /// <summary>
        /// 更新滚动位置
        /// </summary>
        /// <param name="verticalOffset">垂直偏移</param>
        /// <param name="horizontalOffset">水平偏移</param>
        public void UpdateScrollPosition(double verticalOffset, double horizontalOffset)
        {
            _currentVerticalOffset = verticalOffset;
            _currentHorizontalOffset = horizontalOffset;
        }

        #endregion

        #region 初始化偏好设置

        /// <summary>
        /// 初始化所有偏好设置
        /// </summary>
        public async Task InitializePreferencesAsync()
        {
            await LoadLayoutPreferencesAsync();
            await LoadFilterPresetsFromStorageAsync();
            await LoadSearchHistoryFromStorageAsync();
        }

        #endregion
    }

    /// <summary>
    /// 筛选预设
    /// </summary>
    public class FilterPreset
    {
        /// <summary>
        /// 预设名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 筛选选项
        /// </summary>
        public FilterOptions FilterOptions { get; set; } = new();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 搜索筛选条件
    /// </summary>
    public class SearchFilters
    {
        public string SearchText { get; set; } = string.Empty;
        public FilterOptions FilterOptions { get; set; } = new();
        public int? PersonnelId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<int>? PositionIds { get; set; }
        public string SearchKeyword { get; set; } = string.Empty;
    }
}
