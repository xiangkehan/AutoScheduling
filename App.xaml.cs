using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Helpers;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.ViewModels.Scheduling;

namespace AutoScheduling3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// 依赖注入服务提供者
        /// </summary>
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>
        /// 主窗口实例
        /// </summary>
        public static Window? MainWindow { get; private set; }

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
            services.AddSingleton(sp => new Services.SchedulingService(DatabasePath));

            // 注册 Helpers
            services.AddSingleton<NavigationService>();
            services.AddSingleton<DialogService>();

            // 注册 ViewModels
            services.AddTransient<PersonnelViewModel>();
            services.AddTransient<PositionViewModel>();
            services.AddTransient<SkillViewModel>();
            services.AddTransient<TemplateViewModel>();

            Services = services.BuildServiceProvider();

            // 初始化数据库
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async System.Threading.Tasks.Task InitializeDatabaseAsync()
        {
            try
            {
                var personalRepo = Services.GetRequiredService<IPersonalRepository>() as PersonalRepository;
                var positionRepo = Services.GetRequiredService<IPositionRepository>() as PositionLocationRepository;
                var skillRepo = Services.GetRequiredService<ISkillRepository>() as SkillRepository;
                var templateRepo = Services.GetRequiredService<ITemplateRepository>() as SchedulingTemplateRepository;
                var schedulingRepo = Services.GetRequiredService<SchedulingRepository>();
                var constraintRepo = Services.GetRequiredService<ConstraintRepository>();

                if (personalRepo != null) await personalRepo.InitAsync();
                if (positionRepo != null) await positionRepo.InitAsync();
                if (skillRepo != null) await skillRepo.InitAsync();
                if (templateRepo != null) await templateRepo.InitAsync();
                if (schedulingRepo != null) await schedulingRepo.InitAsync();
                if (constraintRepo != null) await constraintRepo.InitAsync();
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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
        }
    }
}
