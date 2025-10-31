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
using System;
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
            services.AddSingleton<ITemplateRepository>(sp => new SchedulingTemplateRepository(DatabasePath));
            services.AddSingleton(sp => new SchedulingRepository(DatabasePath));
            services.AddSingleton(sp => new ConstraintRepository(DatabasePath));
            services.AddSingleton(sp => new HistoryManagement(DatabasePath));

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
            services.AddSingleton<ISchedulingService>(sp =>
            {
                var svc = new SchedulingService(DatabasePath);
                // 初始化数据库所需的内部结构（延迟到 OnLaunched 中统一处理）
                return svc;
            });

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

            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async System.Threading.Tasks.Task InitializeDatabaseAsync()
        {
            try
            {
                var personalRepo = ServiceProvider.GetRequiredService<IPersonalRepository>() as PersonalRepository;
                var positionRepo = ServiceProvider.GetRequiredService<IPositionRepository>() as PositionLocationRepository;
                var skillRepo = ServiceProvider.GetRequiredService<ISkillRepository>() as SkillRepository;
                var templateRepo = ServiceProvider.GetRequiredService<ITemplateRepository>() as SchedulingTemplateRepository;
                var schedulingRepo = ServiceProvider.GetRequiredService<SchedulingRepository>();
                var constraintRepo = ServiceProvider.GetRequiredService<ConstraintRepository>();
                var schedulingService = ServiceProvider.GetRequiredService<ISchedulingService>() as SchedulingService;
                var historyManagement = ServiceProvider.GetRequiredService<HistoryManagement>();

                if (personalRepo != null) await personalRepo.InitAsync();
                if (positionRepo != null) await positionRepo.InitAsync();
                if (skillRepo != null) await skillRepo.InitAsync();
                if (templateRepo != null) await templateRepo.InitAsync();
                if (schedulingRepo != null) await schedulingRepo.InitAsync();
                if (constraintRepo != null) await constraintRepo.InitAsync();
                if (historyManagement != null) await historyManagement.InitAsync();
                if (schedulingService != null) await schedulingService.InitializeAsync();
            }
            catch (System.Exception ex)
            {
                // 日志记录或错误处理
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
