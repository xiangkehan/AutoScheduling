using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 应用程序服务基础接口 - 定义应用程序级别的服务操作
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// 初始化服务
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 清理服务资源
    /// </summary>
    Task CleanupAsync();
}

/// <summary>
/// 可配置服务接口 - 支持配置的服务
/// </summary>
public interface IConfigurableService : IApplicationService
{
    /// <summary>
    /// 加载配置
    /// </summary>
    Task LoadConfigurationAsync();

    /// <summary>
    /// 保存配置
    /// </summary>
    Task SaveConfigurationAsync();
}