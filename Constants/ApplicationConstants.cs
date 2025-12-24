namespace AutoScheduling3.Constants;

/// <summary>
/// 应用程序常量定义
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// 数据库相关常量
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// 默认数据库文件名
        /// </summary>
        public const string DefaultFileName = "AutoScheduling.db";
        
        /// <summary>
        /// 数据库连接超时时间（秒）
        /// </summary>
        public const int ConnectionTimeout = 30;
        
        /// <summary>
        /// 数据库命令超时时间（秒）
        /// </summary>
        public const int CommandTimeout = 60;
    }

    /// <summary>
    /// 排班相关常量
    /// </summary>
    public static class Scheduling
    {
        /// <summary>
        /// 时段数量（一天分为12个时段）
        /// </summary>
        public const int TimeSlotCount = 12;
        
        /// <summary>
        /// 每个时段的小时数
        /// </summary>
        public const int HoursPerTimeSlot = 2;
        
        /// <summary>
        /// 夜哨开始时段（18:00-06:00）
        /// </summary>
        public const int NightShiftStartSlot = 9; // 18:00
        
        /// <summary>
        /// 夜哨结束时段
        /// </summary>
        public const int NightShiftEndSlot = 3; // 06:00
        
        /// <summary>
        /// 默认最小休息间隔（时段数）
        /// </summary>
        public const int DefaultMinRestInterval = 2;
    }

    /// <summary>
    /// UI相关常量
    /// </summary>
    public static class UI
    {
        /// <summary>
        /// 默认页面大小
        /// </summary>
        public const int DefaultPageSize = 50;
        
        /// <summary>
        /// 搜索延迟时间（毫秒）
        /// </summary>
        public const int SearchDelayMs = 300;
        
        /// <summary>
        /// 动画持续时间（毫秒）
        /// </summary>
        public const int AnimationDurationMs = 250;
    }

    /// <summary>
    /// 配置键常量
    /// </summary>
    public static class ConfigKeys
    {
        /// <summary>
        /// 窗口状态配置键前缀
        /// </summary>
        public const string WindowStatePrefix = "WindowState.";
        
        /// <summary>
        /// 用户偏好设置前缀
        /// </summary>
        public const string UserPreferencesPrefix = "UserPreferences.";
        
        /// <summary>
        /// 排班参数前缀
        /// </summary>
        public const string SchedulingParametersPrefix = "SchedulingParameters.";
        
        /// <summary>
        /// 主题设置
        /// </summary>
        public const string ThemeSetting = "UserPreferences.Theme";
        
        /// <summary>
        /// 语言设置
        /// </summary>
        public const string LanguageSetting = "UserPreferences.Language";
    }

    /// <summary>
    /// 文件路径常量
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// 日志文件夹名称
        /// </summary>
        public const string LogsFolderName = "Logs";
        
        /// <summary>
        /// 备份文件夹名称
        /// </summary>
        public const string BackupsFolderName = "Backups";
        
        /// <summary>
        /// 导出文件夹名称
        /// </summary>
        public const string ExportsFolderName = "Exports";
        
        /// <summary>
        /// 模板文件夹名称
        /// </summary>
        public const string TemplatesFolderName = "Templates";
        
        /// <summary>
        /// 遗传算法配置文件名
        /// </summary>
        public const string GeneticSchedulerConfigFileName = "GeneticSchedulerConfig.json";
    }

    /// <summary>
    /// 遗传算法相关常量
    /// </summary>
    public static class GeneticAlgorithm
    {
        /// <summary>
        /// 默认种群大小
        /// </summary>
        public const int DefaultPopulationSize = 50;
        
        /// <summary>
        /// 最小种群大小
        /// </summary>
        public const int MinPopulationSize = 10;
        
        /// <summary>
        /// 最大种群大小
        /// </summary>
        public const int MaxPopulationSize = 200;
        
        /// <summary>
        /// 默认最大代数
        /// </summary>
        public const int DefaultMaxGenerations = 100;
        
        /// <summary>
        /// 最小代数
        /// </summary>
        public const int MinGenerations = 10;
        
        /// <summary>
        /// 最大代数
        /// </summary>
        public const int MaxGenerations = 500;
        
        /// <summary>
        /// 默认交叉率
        /// </summary>
        public const double DefaultCrossoverRate = 0.8;
        
        /// <summary>
        /// 最小交叉率
        /// </summary>
        public const double MinCrossoverRate = 0.0;
        
        /// <summary>
        /// 最大交叉率
        /// </summary>
        public const double MaxCrossoverRate = 1.0;
        
        /// <summary>
        /// 默认变异率
        /// </summary>
        public const double DefaultMutationRate = 0.1;
        
        /// <summary>
        /// 最小变异率
        /// </summary>
        public const double MinMutationRate = 0.0;
        
        /// <summary>
        /// 最大变异率
        /// </summary>
        public const double MaxMutationRate = 1.0;
        
        /// <summary>
        /// 默认精英保留数量
        /// </summary>
        public const int DefaultEliteCount = 2;
        
        /// <summary>
        /// 最小精英保留数量
        /// </summary>
        public const int MinEliteCount = 0;
        
        /// <summary>
        /// 最大精英保留数量
        /// </summary>
        public const int MaxEliteCount = 10;
        
        /// <summary>
        /// 默认锦标赛大小
        /// </summary>
        public const int DefaultTournamentSize = 5;
        
        /// <summary>
        /// 最小锦标赛大小
        /// </summary>
        public const int MinTournamentSize = 2;
        
        /// <summary>
        /// 最大锦标赛大小
        /// </summary>
        public const int MaxTournamentSize = 10;
        
        /// <summary>
        /// 默认未分配时段惩罚权重
        /// </summary>
        public const double DefaultUnassignedPenaltyWeight = 1000.0;
        
        /// <summary>
        /// 默认硬约束违反惩罚权重
        /// </summary>
        public const double DefaultHardConstraintPenaltyWeight = 10000.0;
        
        /// <summary>
        /// 早期终止：连续无改进代数阈值
        /// </summary>
        public const int EarlyTerminationGenerations = 20;
        
        /// <summary>
        /// 早期终止：改进阈值（百分比）
        /// </summary>
        public const double EarlyTerminationImprovementThreshold = 0.001;
        
        /// <summary>
        /// 进度报告节流间隔（毫秒）
        /// </summary>
        public const int ProgressReportThrottleMs = 100;
        
        /// <summary>
        /// 交叉/变异操作最大重试次数
        /// </summary>
        public const int MaxOperationRetries = 3;
    }

    /// <summary>
    /// 错误消息常量
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// 通用错误消息
        /// </summary>
        public const string GenericError = "操作失败，请重试。";
        
        /// <summary>
        /// 数据库连接错误
        /// </summary>
        public const string DatabaseConnectionError = "无法连接到数据库。";
        
        /// <summary>
        /// 数据验证错误
        /// </summary>
        public const string ValidationError = "数据验证失败。";
        
        /// <summary>
        /// 权限不足错误
        /// </summary>
        public const string InsufficientPermissions = "权限不足，无法执行此操作。";
    }
}