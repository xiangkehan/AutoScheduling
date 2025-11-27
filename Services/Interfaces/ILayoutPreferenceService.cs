using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces
{
    /// <summary>
    /// 布局偏好服务接口
    /// </summary>
    public interface ILayoutPreferenceService
    {
        /// <summary>
        /// 加载布局偏好
        /// </summary>
        Task<LayoutPreferences> LoadAsync();

        /// <summary>
        /// 保存布局偏好
        /// </summary>
        Task SaveAsync(LayoutPreferences preferences);

        /// <summary>
        /// 保存左侧面板宽度
        /// </summary>
        Task SaveLeftPanelWidthAsync(double width);

        /// <summary>
        /// 保存右侧面板宽度
        /// </summary>
        Task SaveRightPanelWidthAsync(double width);

        /// <summary>
        /// 保存面板可见性
        /// </summary>
        Task SavePanelVisibilityAsync(bool isLeftVisible, bool isRightVisible);

        /// <summary>
        /// 保存偏好的视图模式
        /// </summary>
        Task SavePreferredViewModeAsync(string viewMode);

        /// <summary>
        /// 保存视图模式
        /// </summary>
        Task SaveViewModeAsync(string viewMode);

        /// <summary>
        /// 获取左侧面板宽度
        /// </summary>
        Task<double> GetLeftPanelWidthAsync();

        /// <summary>
        /// 获取右侧面板宽度
        /// </summary>
        Task<double> GetRightPanelWidthAsync();

        /// <summary>
        /// 获取左侧面板可见性
        /// </summary>
        Task<bool> GetLeftPanelVisibilityAsync();

        /// <summary>
        /// 获取右侧面板可见性
        /// </summary>
        Task<bool> GetRightPanelVisibilityAsync();
    }
}
