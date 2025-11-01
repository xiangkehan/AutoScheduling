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
    public static IServiceCollection AddRepositories(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPersonalRepository>(sp => new PersonalRepository(databasePath));
        services.AddSingleton<IPositionRepository>(sp => new PositionLocationRepository(databasePath));
        services.AddSingleton<ISkillRepository>(sp => new SkillRepository(databasePath));
        services.AddSingleton<IConstraintRepository>(sp => new ConstraintRepository(databasePath));
        services.AddSingleton<ITemplateRepository>(sp => new SchedulingTemplateRepository(databasePath));
        services.AddSingleton(sp => new SchedulingRepository(databasePath));
        services.AddSingleton<IHistoryManagement>(sp => new HistoryManagement(databasePath));

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
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<ISchedulingService, SchedulingService>();

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
        services.AddSingleton<DialogService>();

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

        // 历史ViewModels
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<HistoryDetailViewModel>();
        services.AddTransient<DraftsViewModel>();
        services.AddTransient<CompareViewModel>();

        return services;
    }

    /// <summary>
    /// 注册所有应用程序服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="databasePath">数据库路径</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string databasePath)
    {
        return services
            .AddRepositories(databasePath)
            .AddMappers()
            .AddBusinessServices()
            .AddHelperServices()
            .AddViewModels();
    }
}