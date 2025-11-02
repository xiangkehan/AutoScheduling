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
        /// </summary>
        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            try
            {
                // 首先初始化数据库
                var databaseService = ServiceProvider.GetRequiredService<DatabaseService>();
                await databaseService.InitializeAsync();

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
                    ServiceProvider.GetRequiredService<ISchedulingService>()
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
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"服务初始化失败: {ex.Message}");
                
                // 显示错误对话框
                var dialogService = ServiceProvider.GetService<Helpers.DialogService>();
                if (dialogService != null)
                {
                    await dialogService.ShowErrorAsync($"应用程序初始化失败：{ex.Message}");
                }
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
