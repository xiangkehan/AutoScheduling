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

namespace AutoScheduling3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
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
            ConfigureServices();
            UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
            // Optionally, prevent the application from crashing
            e.Handled = true;
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
        /// Requirements: 1.1, 1.4
        /// </summary>
        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            var initStartTime = DateTime.UtcNow;
            
            try
            {
                // 首先初始化数据库 - 使用新的 InitializeAsync 方法
                var databaseService = ServiceProvider.GetRequiredService<DatabaseService>();
                
                System.Diagnostics.Debug.WriteLine("Starting database initialization...");
                
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
                System.Diagnostics.Debug.WriteLine($"Database initialization completed in {initResult.Duration.TotalMilliseconds:F2}ms");
                
                // Handle initialization result
                if (!initResult.Success)
                {
                    var errorMsg = $"数据库初始化失败";
                    if (initResult.FailedStage.HasValue)
                    {
                        errorMsg += $" (阶段: {initResult.FailedStage.Value})";
                    }
                    errorMsg += $": {initResult.ErrorMessage}";
                    
                    System.Diagnostics.Debug.WriteLine(errorMsg);
                    
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
                    System.Diagnostics.Debug.WriteLine($"Database initialization completed with {initResult.Warnings.Count} warning(s):");
                    foreach (var warning in initResult.Warnings)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {warning}");
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
                    System.Diagnostics.Debug.WriteLine("Database initialization completed successfully with no warnings");
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
                
                // Log total initialization duration
                var totalDuration = DateTime.UtcNow - initStartTime;
                System.Diagnostics.Debug.WriteLine($"Total application initialization completed in {totalDuration.TotalMilliseconds:F2}ms");
            }
            catch(AutoScheduling3.Data.Exceptions.DatabaseInitializationException dbEx)
            {
                // Handle database initialization exceptions with stage information
                var errorMsg = $"数据库初始化失败 (阶段: {dbEx.FailedStage}): {dbEx.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                
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
                System.Diagnostics.Debug.WriteLine($"服务初始化失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
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
                System.Diagnostics.Debug.WriteLine($"Services initialization failed: {ex.Message}");
                
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
