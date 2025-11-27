using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services
{
    /// <summary>
    /// 布局偏好服务实现
    /// </summary>
    public class LayoutPreferenceService : ILayoutPreferenceService
    {
        private const string SettingsKey = "ScheduleResultPage_LayoutPreferences";
        private readonly ApplicationDataContainer _localSettings;
        private LayoutPreferences? _cachedPreferences;

        public LayoutPreferenceService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        /// <summary>
        /// 加载布局偏好
        /// </summary>
        public async Task<LayoutPreferences> LoadAsync()
        {
            // 如果有缓存，直接返回
            if (_cachedPreferences != null)
            {
                return _cachedPreferences;
            }

            await Task.CompletedTask;

            // 从本地设置读取
            if (_localSettings.Values.TryGetValue(SettingsKey, out var value) && value is string json)
            {
                try
                {
                    _cachedPreferences = JsonSerializer.Deserialize<LayoutPreferences>(json);
                    if (_cachedPreferences != null)
                    {
                        return _cachedPreferences;
                    }
                }
                catch (JsonException)
                {
                    // 反序列化失败，使用默认值
                }
            }

            // 返回默认值
            _cachedPreferences = new LayoutPreferences();
            return _cachedPreferences;
        }

        /// <summary>
        /// 保存布局偏好
        /// </summary>
        public async Task SaveAsync(LayoutPreferences preferences)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            await Task.CompletedTask;

            try
            {
                var json = JsonSerializer.Serialize(preferences);
                _localSettings.Values[SettingsKey] = json;
                _cachedPreferences = preferences;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存布局偏好失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 保存左侧面板宽度
        /// </summary>
        public async Task SaveLeftPanelWidthAsync(double width)
        {
            var preferences = await LoadAsync();
            preferences.LeftPanelWidth = width;
            await SaveAsync(preferences);
        }

        /// <summary>
        /// 保存右侧面板宽度
        /// </summary>
        public async Task SaveRightPanelWidthAsync(double width)
        {
            var preferences = await LoadAsync();
            preferences.RightPanelWidth = width;
            await SaveAsync(preferences);
        }

        /// <summary>
        /// 保存面板可见性
        /// </summary>
        public async Task SavePanelVisibilityAsync(bool isLeftVisible, bool isRightVisible)
        {
            var preferences = await LoadAsync();
            preferences.IsLeftPanelVisible = isLeftVisible;
            preferences.IsRightPanelVisible = isRightVisible;
            await SaveAsync(preferences);
        }

        /// <summary>
        /// 保存偏好的视图模式
        /// </summary>
        public async Task SavePreferredViewModeAsync(string viewMode)
        {
            var preferences = await LoadAsync();
            preferences.PreferredViewMode = viewMode;
            await SaveAsync(preferences);
        }

        /// <summary>
        /// 保存视图模式
        /// </summary>
        public async Task SaveViewModeAsync(string viewMode)
        {
            await SavePreferredViewModeAsync(viewMode);
        }

        /// <summary>
        /// 获取左侧面板宽度
        /// </summary>
        public async Task<double> GetLeftPanelWidthAsync()
        {
            var preferences = await LoadAsync();
            return preferences.LeftPanelWidth;
        }

        /// <summary>
        /// 获取右侧面板宽度
        /// </summary>
        public async Task<double> GetRightPanelWidthAsync()
        {
            var preferences = await LoadAsync();
            return preferences.RightPanelWidth;
        }

        /// <summary>
        /// 获取左侧面板可见性
        /// </summary>
        public async Task<bool> GetLeftPanelVisibilityAsync()
        {
            var preferences = await LoadAsync();
            return preferences.IsLeftPanelVisible;
        }

        /// <summary>
        /// 获取右侧面板可见性
        /// </summary>
        public async Task<bool> GetRightPanelVisibilityAsync()
        {
            var preferences = await LoadAsync();
            return preferences.IsRightPanelVisible;
        }
    }
}
