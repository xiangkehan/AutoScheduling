using System; // 添加此 using 指令
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.ViewModels.Scheduling;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Data;
using AutoScheduling3.DTOs.Mappers;
using WinRT.Interop;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.ViewModels.History;
using AutoScheduling3.History;
using AutoScheduling3.Extensions;
using AutoScheduling3.Data;
using OfficeOpenXml;

namespace AutoScheduling3
{
    /// <summary>
    /// 应用程序主类 - 提供应用程序特定的行为
    /// 
    /// 数据存储说明：
    /// 本应用程序使用 ApplicationData.Current.LocalFolder 作为所有数据的存储位置。
    /// 
    /// 为什么使用 ApplicationData.Current.LocalFolder：
    /// 1. WinUI3 最佳实践：这是 WinUI3 应用程序推荐的数据存储方式
    /// 2. 应用商店兼容性：确保应用程序符合 Windows 应用商店的要求
    /// 3. 沙箱安全性：应用程序在沙箱环境中正确运行，自动拥有读写权限
    /// 4. 系统管理：Windows 系统可以正确管理应用数据的备份和清理
    /// 5. 用户隐私：数据与其他应用程序隔离，提高安全性
    /// 
    /// 数据存储结构：
    /// - 数据库文件：{LocalFolder}\GuardDutyScheduling.db
    /// - 配置文件：{LocalFolder}\Settings\config.json
    /// - 备份文件：{LocalFolder}\backups\*.db
    /// 
    /// 日志记录：
    /// - 应用启动时记录 LocalFolder 路径
    /// - 记录数据库路径和备份目录
    /// - 所有日志使用 [App] 前缀，便于过滤和诊断
    /// 
    /// 需求: 1.1, 1.2, 2.1, 6.2, 8.1, 8.2, 8.3
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// 主窗口实例
        /// </summary>
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// 主窗口句柄
        /// </summary>
        public static nint MainWindowHandle { get; private set; }

        /// <summary>
        /// 获取数据库路径
        /// </summary>
        private static string DatabasePath => DatabaseConfiguration.GetDefaultDatabasePath();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            
            // 设置 EPPlus 8+ 的非商业许可证（需要提供个人或组织名称）
            ExcelPackage.License.SetNonCommercialOrganization("AutoScheduling3");
            
            ConfigureServices();
            UnhandledException += App_UnhandledException;
        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");
            
            // 标记异常已处理，防止应用崩溃
            e.Handled = true;

            // 尝试使用 DialogService 显示错误
            var dialogService = ServiceProvider?.GetService<DialogService>();
            if (dialogService != null)
            {
                try
                {
                    await dialogService.ShowErrorAsync("应用程序遇到未处理的错误。", e.Exception);
                }
                catch (Exception ex)
                {
                    // 如果显示对话框时也发生错误，记录到调试输出
                    System.Diagnostics.Debug.WriteLine($"[App] Failed to show error dialog in unhandled exception handler: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 使用扩展方法注册所有服务
            services.AddApplicationServices(DatabasePath);

            ServiceProvider = services.BuildServiceProvider();
            
            // 初始化服务定位器
            ServiceLocator.Initialize(ServiceProvider);
        }

        /// <summary>
        /// 初始化应用程序服务
        /// Requirements: 1.1, 1.4, 6.2, 8.2
        /// </summary>
        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            var initStartTime = DateTime.UtcNow;
            
            try
            {
                // Log ApplicationData.Current.LocalFolder path at application startup
                // Requirements: 5.1, 5.2, 5.3, 6.2, 8.2, 8.3
                try
                {
                    var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                    System.Diagnostics.Debug.WriteLine($"[App] Application startup - LocalFolder path: {localFolder}");
                    System.Diagnostics.Debug.WriteLine($"[App] Database path: {DatabasePath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = "权限不足，无法访问应用程序数据文件夹";
                    System.Diagnostics.Debug.WriteLine($"[App] {errorMsg}: {ex.Message}");
                    throw new InvalidOperationException(errorMsg, ex);
                }
                catch (System.IO.IOException ex)
                {
                    var errorMsg = "访问应用程序数据文件夹时发生IO错误";
                    System.Diagnostics.Debug.WriteLine($"[App] {errorMsg}: {ex.Message}");
                    throw new InvalidOperationException(errorMsg, ex);
                }
                
                // 首先初始化数据库 - 使用新的 InitializeAsync 方法
                var databaseService = ServiceProvider.GetRequiredService<DatabaseService>();
                
                System.Diagnostics.Debug.WriteLine("[App] Starting database initialization...");
                
                // Create initialization options with default settings
                var initOptions = new AutoScheduling3.Data.Models.InitializationOptions
                {
                    PerformHealthCheck = true,
                    AutoRepair = true,
                    CreateBackupBeforeRepair = true,
                    ConnectionRetryCount = 3,
                    ConnectionRetryDelay = TimeSpan.FromSeconds(1)
                };
                
                // Call new InitializeAsync with options
                var initResult = await databaseService.InitializeAsync(initOptions);
                
                // Log initialization duration
                System.Diagnostics.Debug.WriteLine($"[App] Database initialization completed in {initResult.Duration.TotalMilliseconds:F2}ms");
                
                // Handle initialization result
                if (!initResult.Success)
                {
                    var errorMsg = $"数据库初始化失败";
                    if (initResult.FailedStage.HasValue)
                    {
                        errorMsg += $" (阶段: {initResult.FailedStage.Value})";
                    }
                    errorMsg += $": {initResult.ErrorMessage}";
                    
                    System.Diagnostics.Debug.WriteLine($"[App] {errorMsg}");
                    
                    // 显示错误对话框
                    var dialogService = ServiceProvider.GetService<Helpers.DialogService>();
                    if (dialogService != null)
                    {
                        await dialogService.ShowErrorAsync(errorMsg);
                    }
                    
                    // Throw exception to prevent further initialization
                    throw new AutoScheduling3.Data.Exceptions.DatabaseInitializationException(
                        initResult.ErrorMessage,
                        initResult.FailedStage ?? AutoScheduling3.Data.Enums.InitializationStage.DirectoryCreation);
                }
                
                // Display warnings if any
                if (initResult.Warnings != null && initResult.Warnings.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] Database initialization completed with {initResult.Warnings.Count} warning(s):");
                    foreach (var warning in initResult.Warnings)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App]   - {warning}");
                    }
                    
                    // Optionally show warnings to user (non-blocking)
                    var dialogService = ServiceProvider.GetService<Helpers.DialogService>();
                    if (dialogService != null && initResult.Warnings.Count > 0)
                    {
                        var warningMsg = "数据库初始化完成，但有以下警告：\n\n" + 
                                       string.Join("\n", initResult.Warnings.Take(5));
                        if (initResult.Warnings.Count > 5)
                        {
                            warningMsg += $"\n\n... 以及其他 {initResult.Warnings.Count - 5} 个警告";
                        }
                        
                        // Show warning dialog asynchronously without blocking
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(500); // Small delay to let UI load
                            await dialogService.ShowWarningAsync(warningMsg);
                        });
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[App] Database initialization completed successfully with no warnings");
                }

                // 然后初始化配置服务
                var configService = ServiceProvider.GetRequiredService<IConfigurationService>();
                await configService.InitializeAsync();

                // 初始化所有带 InitAsync 方法的组件
                var initTargets = new object[]{
                    ServiceProvider.GetRequiredService<IPersonalRepository>(),
                    ServiceProvider.GetRequiredService<IPositionRepository>(),
                    ServiceProvider.GetRequiredService<ISkillRepository>(),
                    ServiceProvider.GetRequiredService<IConstraintRepository>(),
                    ServiceProvider.GetRequiredService<ITemplateRepository>(),
                    ServiceProvider.GetRequiredService<IHistoryManagement>(),
                    ServiceProvider.GetRequiredService<ISchedulingService>(),
                    ServiceProvider.GetRequiredService<IStoragePathService>()
                };

                foreach(var target in initTargets)
                {
                    var mi = target.GetType().GetMethod("InitAsync");
                    if(mi != null)
                    {
                        var task = mi.Invoke(target, null) as System.Threading.Tasks.Task;
                        if(task != null) await task;
                    }
                }

                // SchedulingService 自身可能定义 InitializeAsync 调度资源
                var schedulingService = ServiceProvider.GetRequiredService<ISchedulingService>();
                var initServiceMi = schedulingService.GetType().GetMethod("InitializeAsync");
                if(initServiceMi != null)
                {
                    var tsk = initServiceMi.Invoke(schedulingService, null) as System.Threading.Tasks.Task;
                    if(tsk != null) await tsk;
                }
                
                // Cleanup expired drafts (older than 7 days)
                // Requirements: 3.3
                try
                {
                    var draftService = ServiceProvider.GetRequiredService<ISchedulingDraftService>();
                    await draftService.CleanupExpiredDraftsAsync();
                    System.Diagnostics.Debug.WriteLine("[App] Draft cleanup completed");
                }
                catch (Exception draftEx)
                {
                    // Log but don't fail initialization if draft cleanup fails
                    System.Diagnostics.Debug.WriteLine($"[App] Draft cleanup failed (non-critical): {draftEx.Message}");
                }
                
                // Log total initialization duration
                var totalDuration = DateTime.UtcNow - initStartTime;
                System.Diagnostics.Debug.WriteLine($"[App] Total application initialization completed in {totalDuration.TotalMilliseconds:F2}ms");
            }
            catch(AutoScheduling3.Data.Exceptions.DatabaseInitializationException dbEx)
            {
                // Handle database initialization exceptions with stage information
                var errorMsg = $"数据库初始化失败 (阶段: {dbEx.FailedStage}): {dbEx.Message}";
                System.Diagnostics.Debug.WriteLine($"[App] {errorMsg}");
                
                // 显示错误对话框
                var dialogService = ServiceProvider.GetService<Helpers.DialogService>();
                if (dialogService != null)
                {
                    await dialogService.ShowErrorAsync(errorMsg);
                }
                
                // Re-throw to be caught by outer handler
                throw;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] 服务初始化失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");
                
                // 显示错误对话框
                var dialogService = ServiceProvider.GetService<Helpers.DialogService>();
                if (dialogService != null)
                {
                    await dialogService.ShowErrorAsync($"应用程序初始化失败：{ex.Message}");
                }
                
                // Re-throw to be caught by outer handler
                throw;
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                await InitializeServicesAsync();
                
                _window = new MainWindow();
                MainWindow = _window;
                MainWindowHandle = WindowNative.GetWindowHandle(_window);
                _window.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Services initialization failed: {ex.Message}");
                
                // 如果初始化失败，仍然创建窗口
                _window = new MainWindow();
                MainWindow = _window;
                MainWindowHandle = WindowNative.GetWindowHandle(_window);
                _window.Activate();
                
                // 延迟显示错误对话框，等待窗口完全加载
                _ = Task.Delay(1000).ContinueWith(async _ =>
                {
                    var dialogService = ServiceProvider.GetService<Helpers.DialogService>();
                    if (dialogService != null)
                    {
                        await dialogService.ShowErrorAsync($"应用程序初始化失败：{ex.Message}");
                    }
                });
            }
        }

        // 在 App 类中添加 MainWindowHandle 字段
        
    }
}
