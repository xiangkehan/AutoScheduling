using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Helpers;
using AutoScheduling3.History;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.ViewModels.History;
using AutoScheduling3.ViewModels.Scheduling;
using AutoScheduling3.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AutoScheduling3.Extensions;

/// <summary>
/// 依赖注入服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册所有仓储服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="databasePath">数据库路径</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// DatabaseService is registered as a singleton and must be initialized before other services.
    /// Requirements: 1.1
    /// </remarks>
    public static IServiceCollection AddRepositories(this IServiceCollection services, string databasePath)
    {
        // 注册数据库服务为单例 - 必须在其他服务之前初始化
        // Register DatabaseService as singleton - must be initialized before other services
        services.AddSingleton<DatabaseService>(sp => new DatabaseService(databasePath));
        
        // 注册其他仓储服务 - 这些服务依赖于已初始化的数据库
        // Register other repository services - these depend on initialized database
        services.AddSingleton<IPersonalRepository>(sp => new PersonalRepository(databasePath));
        services.AddSingleton<IPositionRepository>(sp => new PositionLocationRepository(databasePath));
        services.AddSingleton<ISkillRepository>(sp => new SkillRepository(databasePath));
        services.AddSingleton<IConstraintRepository>(sp => new ConstraintRepository(databasePath));
        services.AddSingleton<ITemplateRepository>(sp => new SchedulingTemplateRepository(databasePath));
        services.AddSingleton(sp => new SchedulingRepository(databasePath));
        services.AddSingleton<IHistoryManagement>(sp => new HistoryManagement(databasePath));

        // 注册数据库备份管理器
        services.AddSingleton(sp => 
        {
            var logger = new AutoScheduling3.Data.Logging.DebugLogger("DatabaseBackup");
            return new DatabaseBackupManager(databasePath, logger: logger);
        });

        return services;
    }

    /// <summary>
    /// 注册所有映射器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        services.AddSingleton<PersonnelMapper>();
        services.AddSingleton<PositionMapper>();
        services.AddSingleton<SkillMapper>();
        services.AddSingleton<ConstraintMapper>();
        services.AddSingleton<TemplateMapper>();

        return services;
    }

    /// <summary>
    /// 注册所有业务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IPersonnelService, PersonnelService>();
        services.AddSingleton<IPositionService, PositionService>();
        services.AddSingleton<ISkillService, SkillService>();
        services.AddSingleton<IConstraintService, ConstraintService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<ISchedulingService, SchedulingService>();
        services.AddSingleton<IStoragePathService, StoragePathService>();
        services.AddSingleton<ISchedulingDraftService, SchedulingDraftService>();
        services.AddSingleton<IScheduleGridExporter, ScheduleGridExporter>();

        // 注册数据导入导出相关服务
        // Register data validation service
        services.AddSingleton<Services.ImportExport.IDataValidationService>(sp =>
        {
            var logger = new AutoScheduling3.Data.Logging.DebugLogger("DataValidation");
            return new Services.ImportExport.DataValidationService(logger);
        });

        // Register data export service
        services.AddSingleton<Services.ImportExport.IDataExportService>(sp =>
        {
            var logger = new AutoScheduling3.Data.Logging.DebugLogger("DataExport");
            return new Services.ImportExport.DataExportService(
                sp.GetRequiredService<IPersonalRepository>(),
                sp.GetRequiredService<IPositionRepository>(),
                sp.GetRequiredService<ISkillRepository>(),
                sp.GetRequiredService<ITemplateRepository>(),
                sp.GetRequiredService<IConstraintRepository>(),
                logger
            );
        });

        // Register data mapping service
        services.AddSingleton<Services.ImportExport.IDataMappingService, Services.ImportExport.DataMappingService>();

        // 注册数据导入导出服务
        services.AddSingleton<IDataImportExportService>(sp =>
        {
            var logger = new AutoScheduling3.Data.Logging.DebugLogger("DataImportExport");
            return new DataImportExportService(
                sp.GetRequiredService<IPersonalRepository>(),
                sp.GetRequiredService<IPositionRepository>(),
                sp.GetRequiredService<ISkillRepository>(),
                sp.GetRequiredService<ITemplateRepository>(),
                sp.GetRequiredService<IConstraintRepository>(),
                sp.GetRequiredService<DatabaseBackupManager>(),
                logger,
                sp.GetRequiredService<Services.ImportExport.IDataValidationService>(),
                sp.GetRequiredService<Services.ImportExport.IDataExportService>(),
                sp.GetRequiredService<Services.ImportExport.IDataMappingService>()
            );
        });

        return services;
    }

    /// <summary>
    /// 注册所有辅助服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHelperServices(this IServiceCollection services)
    {
        services.AddSingleton<NavigationService>();
        services.AddSingleton<Helpers.DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();

        return services;
    }

    /// <summary>
    /// 注册所有ViewModel
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // 数据管理ViewModels
        services.AddTransient<PersonnelViewModel>();
        services.AddTransient<PositionViewModel>();
        services.AddTransient<SkillViewModel>();
        services.AddTransient<TemplateViewModel>();

        // 排班ViewModels
        services.AddTransient<SchedulingViewModel>();
        services.AddTransient<ScheduleResultViewModel>();
        services.AddTransient<SchedulingProgressViewModel>();

        // 历史ViewModels
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<HistoryDetailViewModel>();
        services.AddTransient<DraftsViewModel>();
        services.AddTransient<CompareViewModel>();

        // 设置 ViewModel
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<TestDataGeneratorViewModel>(sp =>
        {
            var logger = new AutoScheduling3.Data.Logging.DebugLogger("TestDataGenerator");
            return new TestDataGeneratorViewModel(
                sp.GetRequiredService<IDataImportExportService>(),
                logger
            );
        });

        return services;
    }

    /// <summary>
    /// 注册所有应用程序服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="databasePath">数据库路径</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// Services are registered in dependency order:
    /// 1. Repositories (including DatabaseService as singleton) - must be initialized first
    /// 2. Mappers - data transformation utilities
    /// 3. Business Services - depend on repositories
    /// 4. Helper Services - UI and utility services
    /// 5. ViewModels - depend on all above services
    /// 
    /// DatabaseService must be explicitly initialized via InitializeAsync() before other services use it.
    /// Requirements: 1.1
    /// </remarks>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string databasePath)
    {
        return services
            .AddRepositories(databasePath)      // DatabaseService registered here as singleton
            .AddMappers()
            .AddBusinessServices()
            .AddHelperServices()
            .AddViewModels();
    }
}