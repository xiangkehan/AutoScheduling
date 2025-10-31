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
        /// 数据库路径
        /// </summary>
        private const string DatabasePath = "AutoScheduling.db";

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

            // 注册 Repositories
            services.AddSingleton<IPersonalRepository>(sp => new PersonalRepository(DatabasePath));
            services.AddSingleton<IPositionRepository>(sp => new PositionLocationRepository(DatabasePath));
            services.AddSingleton<ISkillRepository>(sp => new SkillRepository(DatabasePath));
            services.AddSingleton<IConstraintRepository>(sp => new ConstraintRepository(DatabasePath));
            services.AddSingleton<ITemplateRepository>(sp => new SchedulingTemplateRepository(DatabasePath));
            services.AddSingleton(sp => new SchedulingRepository(DatabasePath));
            services.AddSingleton<IHistoryManagement>(sp => new HistoryManagement(DatabasePath));

            // 注册 Mappers
            services.AddSingleton<PersonnelMapper>();
            services.AddSingleton<PositionMapper>();
            services.AddSingleton<SkillMapper>();
            services.AddSingleton<TemplateMapper>();

            // 注册 Services
            services.AddSingleton<IPersonnelService, PersonnelService>();
            services.AddSingleton<IPositionService, PositionService>();
            services.AddSingleton<ISkillService, SkillService>();
            services.AddSingleton<ITemplateService, TemplateService>();
            services.AddSingleton<IHistoryService, HistoryService>();
            services.AddSingleton<ISchedulingService, SchedulingService>();

            // 注册 Helpers
            services.AddSingleton<NavigationService>();
            services.AddSingleton<DialogService>();

            // 注册 ViewModels
            services.AddTransient<PersonnelViewModel>();
            services.AddTransient<PositionViewModel>();
            services.AddTransient<SkillViewModel>();
            services.AddTransient<TemplateViewModel>();
            services.AddTransient<SchedulingViewModel>();
            services.AddTransient<ScheduleResultViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<HistoryDetailViewModel>();
            services.AddTransient<DraftsViewModel>(); // 新增 DraftsViewModel 注册

            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async System.Threading.Tasks.Task InitializeDatabaseAsync()
        {
            try
            {
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
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");
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
                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
            }

            _window = new MainWindow();
            MainWindow = _window;
            MainWindowHandle = WindowNative.GetWindowHandle(_window);
            _window.Activate();
        }

        // 在 App 类中添加 MainWindowHandle 字段
        
    }
}
