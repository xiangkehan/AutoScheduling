using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.ImportExport;
using AutoScheduling3.Helpers;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Services.ImportExport;
using AutoScheduling3.Services.ImportExport.Locking;
using AutoScheduling3.Services.ImportExport.Models;
using AutoScheduling3.Services.ImportExport.Monitoring;
using Microsoft.Data.Sqlite;

namespace AutoScheduling3.Services;

/// <summary>
/// 数据导入导出服务实现 - 作为协调器协调导入导出流程
/// 
/// 职责：
/// - 协调导入导出流程
/// - 管理事务和锁
/// - 处理进度报告
/// - 错误处理和日志记录
/// - 审计日志记录
/// 
/// 注意：具体的验证、导出和映射逻辑已迁移到专门的服务中
/// </summary>
public class DataImportExportService : IDataImportExportService
{
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IConstraintRepository _constraintRepository;
    private readonly DatabaseBackupManager _backupManager;
    private readonly ILogger _logger;
    private readonly OperationAuditLogger _auditLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // 新增的专门服务
    private readonly IDataValidationService _validationService;
    private readonly IDataExportService _exportService;
    private readonly IDataMappingService _mappingService;

    /// <summary>
    /// 初始化数据导入导出服务
    /// </summary>
    /// <param name="personnelRepository">人员仓储</param>
    /// <param name="positionRepository">哨位仓储</param>
    /// <param name="skillRepository">技能仓储</param>
    /// <param name="templateRepository">模板仓储</param>
    /// <param name="constraintRepository">约束仓储</param>
    /// <param name="backupManager">数据库备份管理器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="validationService">数据验证服务</param>
    /// <param name="exportService">数据导出服务</param>
    /// <param name="mappingService">数据映射服务</param>
    public DataImportExportService(
        IPersonalRepository personnelRepository,
        IPositionRepository positionRepository,
        ISkillRepository skillRepository,
        ITemplateRepository templateRepository,
        IConstraintRepository constraintRepository,
        DatabaseBackupManager backupManager,
        ILogger logger,
        IDataValidationService validationService,
        IDataExportService exportService,
        IDataMappingService mappingService)
    {
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _constraintRepository = constraintRepository ?? throw new ArgumentNullException(nameof(constraintRepository));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        _auditLogger = new OperationAuditLogger();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 导出所有核心数据到文件
    /// </summary>
    /// <param name="filePath">导出文件路径</param>
    /// <param name="progress">进度报告（可选）</param>
    /// <returns>导出结果</returns>
    public async Task<ExportResult> ExportDataAsync(string filePath, IProgress<ExportProgress>? progress = null)
    {
        var startTime = DateTime.UtcNow;
        var result = new ExportResult();
        
        try
        {
            _logger.Log($"Starting data export to: {filePath}");
            
            // 验证文件路径
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }
            
            // 验证文件路径格式
            if (!FileManagementHelper.IsValidFilePath(filePath))
            {
                throw new ArgumentException("Invalid file path format", nameof(filePath));
            }
            
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (FileManagementHelper.EnsureDirectoryExists(directory))
                {
                    _logger.Log($"Directory ensured: {directory}");
                }
                else
                {
                    throw new IOException($"Failed to create directory: {directory}");
                }
            }
            
            // 1. 创建导出数据对象
            // 使用 DataExportService 获取版本信息
            var exportData = new ExportData
            {
                Metadata = new ExportMetadata
                {
                    ExportedAt = DateTime.UtcNow,
                    DatabaseVersion = await _exportService.GetDatabaseVersionAsync(),
                    ApplicationVersion = _exportService.GetApplicationVersion()
                }
            };
            
            _logger.Log("Export metadata created");
            
            // 2. 按表导出数据（带进度报告）
            // 使用 DataExportService 导出各表数据
            
            // 导出技能
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Skills", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 0 
            });
            exportData.Skills = await _exportService.ExportSkillsAsync();
            _logger.Log($"Exported {exportData.Skills.Count} skills");
            
            // 导出人员
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Personnel", 
                ProcessedRecords = exportData.Skills.Count,
                TotalRecords = 0,
                PercentComplete = 20 
            });
            exportData.Personnel = await _exportService.ExportPersonnelAsync();
            _logger.Log($"Exported {exportData.Personnel.Count} personnel");
            
            // 导出哨位
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Positions", 
                ProcessedRecords = exportData.Skills.Count + exportData.Personnel.Count,
                TotalRecords = 0,
                PercentComplete = 40 
            });
            exportData.Positions = await _exportService.ExportPositionsAsync();
            _logger.Log($"Exported {exportData.Positions.Count} positions");
            
            // 导出模板
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Templates", 
                ProcessedRecords = exportData.Skills.Count + exportData.Personnel.Count + exportData.Positions.Count,
                TotalRecords = 0,
                PercentComplete = 60 
            });
            exportData.Templates = await _exportService.ExportTemplatesAsync();
            _logger.Log($"Exported {exportData.Templates.Count} templates");
            
            // 导出约束
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Constraints", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 80 
            });
            exportData.FixedAssignments = await _exportService.ExportFixedAssignmentsAsync();
            exportData.ManualAssignments = await _exportService.ExportManualAssignmentsAsync();
            exportData.HolidayConfigs = await _exportService.ExportHolidayConfigsAsync();
            _logger.Log($"Exported {exportData.FixedAssignments.Count} fixed assignments, " +
                       $"{exportData.ManualAssignments.Count} manual assignments, " +
                       $"{exportData.HolidayConfigs.Count} holiday configs");
            
            // 3. 更新统计信息
            // 使用 DataExportService 计算统计信息
            exportData.Metadata.Statistics = _exportService.CalculateStatistics(exportData);
            _logger.Log("Statistics calculated");
            
            // 4. 序列化为 JSON
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Serializing", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 90 
            });
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };
            
            var json = JsonSerializer.Serialize(exportData, options);
            _logger.Log($"Data serialized to JSON ({json.Length} characters)");
            
            // 5. 写入文件
            await File.WriteAllTextAsync(filePath, json);
            _logger.Log($"Data written to file: {filePath}");
            
            // 6. 返回结果
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Complete", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 100 
            });
            
            var fileInfo = new FileInfo(filePath);
            result.Success = true;
            result.FilePath = filePath;
            result.FileSize = fileInfo.Length;
            result.Statistics = exportData.Metadata.Statistics;
            result.Duration = DateTime.UtcNow - startTime;
            
            _logger.Log($"Export completed successfully in {result.Duration.TotalSeconds:F2} seconds. File size: {result.FileSize} bytes");
        }
        catch (Exception ex)
        {
            // 记录详细的技术错误信息
            _logger.LogError($"Export failed: {ex.Message}");
            _logger.LogError($"Detailed error info: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            
            // 转换为用户友好的错误消息
            var userFriendlyMessage = ErrorMessageTranslator.TranslateException(ex, "数据导出");
            result.Success = false;
            result.ErrorMessage = userFriendlyMessage;
            result.Duration = DateTime.UtcNow - startTime;
            
            // 记录审计日志
            _auditLogger.LogExportOperation(result);
            
            // 获取并记录恢复建议
            var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导出");
            var formattedSuggestions = ErrorRecoverySuggester.FormatSuggestions(suggestions);
            _logger.Log($"Recovery suggestions: {formattedSuggestions}");
        }
        
        // 记录成功的审计日志
        if (result.Success)
        {
            _auditLogger.LogExportOperation(result);
        }
        
        return result;
    }

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    private string GetConnectionString()
    {
        var databasePath = DatabaseConfiguration.GetDefaultDatabasePath();
        return DatabaseConfiguration.GetConnectionString(databasePath);
    }

    /// <summary>
    /// 从文件导入数据
    /// </summary>
    /// <param name="filePath">导入文件路径</param>
    /// <param name="options">导入选项</param>
    /// <param name="progress">进度报告（可选）</param>
    /// <returns>导入结果</returns>
    public async Task<ImportResult> ImportDataAsync(string filePath, ImportOptions options, IProgress<ImportProgress>? progress = null)
    {
        var startTime = DateTime.UtcNow;
        var result = new ImportResult 
        { 
            Statistics = new ImportStatistics 
            { 
                RecordsByTable = new Dictionary<string, int>(),
                DetailsByTable = new Dictionary<string, TableImportStats>()
            },
            Warnings = new List<string>()
        };
        
        ImportLockManager? lockManager = null;
        ImportContext? context = null;
        ImportErrorContext? tempErrorContext = null;
        
        try
        {
            // 记录导入操作开始（需求 1.4, 7.5）
            _logger.Log($"=== Import Operation Started ===");
            _logger.Log($"File: {filePath}");
            _logger.Log($"Strategy: {options.Strategy}");
            _logger.Log($"Create Backup: {options.CreateBackupBeforeImport}");
            _logger.Log($"Continue On Error: {options.ContinueOnError}");
            _logger.Log($"Start Time: {startTime:yyyy-MM-dd HH:mm:ss.fff}");
            
            // 记录审计日志
            _auditLogger.LogOperationStart("Import", filePath);
            
            // 1. 获取导入锁
            lockManager = new ImportLockManager();
            if (!await lockManager.TryAcquireLockAsync())
            {
                throw new InvalidOperationException("另一个导入操作正在进行中，请稍后再试");
            }
            
            _logger.Log("Import lock acquired successfully");
            
            // 2. 读取并解析 JSON 文件
            tempErrorContext = new ImportErrorContext
            {
                CurrentOperation = "Reading file",
                OperationStartTime = startTime
            };
            
            progress?.Report(new ImportProgress 
            { 
                CurrentOperation = "Reading file", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 0 
            });
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"导入文件不存在: {filePath}");
            }
            
            string json = await File.ReadAllTextAsync(filePath);
            _logger.Log($"File read successfully ({json.Length} characters)");
            
            tempErrorContext.CurrentOperation = "Deserializing JSON";
            var exportData = JsonSerializer.Deserialize<ExportData>(json, _jsonOptions);
            if (exportData == null)
            {
                throw new InvalidOperationException("无法解析导入文件：反序列化结果为空");
            }
            
            _logger.Log("JSON deserialized successfully");
            
            // 3. 验证数据（事务前）
            tempErrorContext.CurrentOperation = "Validating data";
            progress?.Report(new ImportProgress 
            { 
                CurrentOperation = "Validating", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 10 
            });
            
            var validation = await ValidateImportDataAsync(filePath);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = "数据验证失败";
                result.Warnings = validation.Errors.Select(e => e.Message).ToList();
                _logger.Log($"Validation failed with {validation.Errors.Count} errors");
                return result;
            }
            
            _logger.Log("Data validation passed");
            
            // 4. 创建备份
            if (options.CreateBackupBeforeImport)
            {
                tempErrorContext.CurrentOperation = "Creating backup";
                progress?.Report(new ImportProgress 
                { 
                    CurrentOperation = "Creating backup", 
                    ProcessedRecords = 0,
                    TotalRecords = 0,
                    PercentComplete = 20 
                });
                
                try
                {
                    await _backupManager.CreateBackupAsync();
                    _logger.Log("Backup created successfully");
                }
                catch (Exception ex)
                {
                    _logger.Log($"Backup creation failed: {ex.Message}");
                    result.Warnings.Add($"备份创建失败: {ex.Message}");
                }
            }
            
            // 5. 计算总记录数
            int totalRecords = (exportData.Skills?.Count ?? 0) +
                             (exportData.Personnel?.Count ?? 0) +
                             (exportData.Positions?.Count ?? 0) +
                             (exportData.HolidayConfigs?.Count ?? 0) +
                             (exportData.Templates?.Count ?? 0) +
                             (exportData.FixedAssignments?.Count ?? 0) +
                             (exportData.ManualAssignments?.Count ?? 0);
            
            _logger.Log($"Total records to import: {totalRecords}");
            
            // 6. 创建性能监控器（需求 9.1, 9.2, 9.3）
            var performanceMonitor = new PerformanceMonitor();
            performanceMonitor.StartOperation("Total");
            _logger.Log("Performance monitoring started");
            
            // 7. 开始事务导入
            var connectionString = GetConnectionString();
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            
            // 记录事务开始（需求 1.4）
            _logger.Log("Database transaction started");
            _auditLogger.LogTransactionStart();
            
            context = new ImportContext
            {
                Connection = connection,
                Transaction = transaction,
                Options = options,
                PerformanceMonitor = performanceMonitor,
                Statistics = result.Statistics,
                Warnings = result.Warnings
            };
            
            // 初始化错误上下文
            context.ErrorContext.OperationStartTime = startTime;
            context.ErrorContext.TotalRecords = totalRecords;
            
            try
            {
                int processedRecords = 0;
                
                // 按依赖顺序导入各表（使用旧的导入方法，因为新的 WithTransaction 方法还未实现）
                // 导入技能（无依赖）
                context.ErrorContext.CurrentTable = "Skills";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Skills", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 30 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("Skills");
                _logger.Log($"Starting Skills import ({exportData.Skills?.Count ?? 0} records)");
                
                var skillsImported = await ImportSkillsAsync(exportData.Skills ?? new List<SkillDto>(), options);
                processedRecords += exportData.Skills?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["Skills"] = skillsImported;
                
                // 记录表导入完成和统计（需求 9.1, 9.2）
                performanceMonitor.EndOperation("Skills");
                var skillsDuration = performanceMonitor.GetOperationTime("Skills");
                _logger.Log($"Skills import completed: {skillsImported} records in {skillsDuration.TotalSeconds:F2}s");
                
                // 导入人员（依赖技能）
                context.ErrorContext.CurrentTable = "Personnel";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Personnel", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 45 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("Personnel");
                _logger.Log($"Starting Personnel import ({exportData.Personnel?.Count ?? 0} records)");
                
                var personnelImported = await ImportPersonnelAsync(exportData.Personnel ?? new List<PersonnelDto>(), options);
                processedRecords += exportData.Personnel?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["Personnel"] = personnelImported;
                
                // 记录表导入完成和统计
                performanceMonitor.EndOperation("Personnel");
                var personnelDuration = performanceMonitor.GetOperationTime("Personnel");
                _logger.Log($"Personnel import completed: {personnelImported} records in {personnelDuration.TotalSeconds:F2}s");
                
                // 导入哨位（依赖技能和人员）
                context.ErrorContext.CurrentTable = "Positions";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Positions", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 60 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("Positions");
                _logger.Log($"Starting Positions import ({exportData.Positions?.Count ?? 0} records)");
                
                var positionsImported = await ImportPositionsAsync(exportData.Positions ?? new List<PositionDto>(), options);
                processedRecords += exportData.Positions?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["Positions"] = positionsImported;
                
                // 记录表导入完成和统计
                performanceMonitor.EndOperation("Positions");
                var positionsDuration = performanceMonitor.GetOperationTime("Positions");
                _logger.Log($"Positions import completed: {positionsImported} records in {positionsDuration.TotalSeconds:F2}s");
                
                // 导入节假日配置（无依赖）
                context.ErrorContext.CurrentTable = "HolidayConfigs";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "HolidayConfigs", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 70 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("HolidayConfigs");
                _logger.Log($"Starting HolidayConfigs import ({exportData.HolidayConfigs?.Count ?? 0} records)");
                
                var holidayConfigsImported = await ImportHolidayConfigsAsync(exportData.HolidayConfigs ?? new List<HolidayConfigDto>(), options);
                processedRecords += exportData.HolidayConfigs?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["HolidayConfigs"] = holidayConfigsImported;
                
                // 记录表导入完成和统计
                performanceMonitor.EndOperation("HolidayConfigs");
                var holidayConfigsDuration = performanceMonitor.GetOperationTime("HolidayConfigs");
                _logger.Log($"HolidayConfigs import completed: {holidayConfigsImported} records in {holidayConfigsDuration.TotalSeconds:F2}s");
                
                // 导入模板（依赖人员、哨位、节假日配置）
                context.ErrorContext.CurrentTable = "Templates";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Templates", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 80 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("Templates");
                _logger.Log($"Starting Templates import ({exportData.Templates?.Count ?? 0} records)");
                
                var templatesImported = await ImportTemplatesAsync(exportData.Templates ?? new List<SchedulingTemplateDto>(), options);
                processedRecords += exportData.Templates?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["Templates"] = templatesImported;
                
                // 记录表导入完成和统计
                performanceMonitor.EndOperation("Templates");
                var templatesDuration = performanceMonitor.GetOperationTime("Templates");
                _logger.Log($"Templates import completed: {templatesImported} records in {templatesDuration.TotalSeconds:F2}s");
                
                // 导入固定分配（依赖人员、哨位）
                context.ErrorContext.CurrentTable = "FixedAssignments";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "FixedAssignments", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 90 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("FixedAssignments");
                _logger.Log($"Starting FixedAssignments import ({exportData.FixedAssignments?.Count ?? 0} records)");
                
                var fixedAssignmentsImported = await ImportFixedAssignmentsAsync(exportData.FixedAssignments ?? new List<FixedAssignmentDto>(), options);
                processedRecords += exportData.FixedAssignments?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["FixedAssignments"] = fixedAssignmentsImported;
                
                // 记录表导入完成和统计
                performanceMonitor.EndOperation("FixedAssignments");
                var fixedAssignmentsDuration = performanceMonitor.GetOperationTime("FixedAssignments");
                _logger.Log($"FixedAssignments import completed: {fixedAssignmentsImported} records in {fixedAssignmentsDuration.TotalSeconds:F2}s");
                
                // 导入手动分配（依赖人员、哨位）
                context.ErrorContext.CurrentTable = "ManualAssignments";
                context.ErrorContext.CurrentOperation = "Importing";
                context.ErrorContext.ProcessedRecords = processedRecords;
                
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "ManualAssignments", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 95 
                });
                
                // 记录表导入开始
                performanceMonitor.StartOperation("ManualAssignments");
                _logger.Log($"Starting ManualAssignments import ({exportData.ManualAssignments?.Count ?? 0} records)");
                
                var manualAssignmentsImported = await ImportManualAssignmentsAsync(exportData.ManualAssignments ?? new List<ManualAssignmentDto>(), options);
                processedRecords += exportData.ManualAssignments?.Count ?? 0;
                context.ErrorContext.ProcessedRecords = processedRecords;
                result.Statistics.RecordsByTable["ManualAssignments"] = manualAssignmentsImported;
                
                // 记录表导入完成和统计
                performanceMonitor.EndOperation("ManualAssignments");
                var manualAssignmentsDuration = performanceMonitor.GetOperationTime("ManualAssignments");
                _logger.Log($"ManualAssignments import completed: {manualAssignmentsImported} records in {manualAssignmentsDuration.TotalSeconds:F2}s");
                
                // 提交事务（需求 1.4）
                _logger.Log("Committing database transaction...");
                await transaction.CommitAsync();
                _logger.Log("Transaction committed successfully");
                _auditLogger.LogTransactionCommit();
                
                performanceMonitor.EndOperation("Total");
                
                // 验证导入后的数据完整性
                progress?.Report(new ImportProgress 
                { 
                    CurrentOperation = "Verifying", 
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 98 
                });
                
                await VerifyImportedDataIntegrity(result);
                
                // 完成导入
                progress?.Report(new ImportProgress 
                { 
                    CurrentOperation = "Complete", 
                    ProcessedRecords = totalRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 100 
                });
                
                result.Success = true;
                result.Statistics.TotalRecords = totalRecords;
                result.Statistics.ImportedRecords = skillsImported + personnelImported + positionsImported + 
                                                   holidayConfigsImported + templatesImported + 
                                                   fixedAssignmentsImported + manualAssignmentsImported;
                result.Statistics.SkippedRecords = totalRecords - result.Statistics.ImportedRecords;
                result.Duration = DateTime.UtcNow - startTime;
                
                // 生成并记录性能报告（需求 9.1, 9.2, 9.3, 9.4, 9.5）
                var perfReport = performanceMonitor.GenerateReport(result.Statistics.TotalRecords);
                _logger.Log("=== Performance Report ===");
                _logger.Log(perfReport.Summary);
                
                // 记录批量操作统计（需求 9.1, 9.2）
                _logger.Log("=== Import Statistics by Table ===");
                foreach (var kvp in result.Statistics.RecordsByTable)
                {
                    _logger.Log($"{kvp.Key}: {kvp.Value} records imported");
                }
                
                // 记录导入操作结束
                _logger.Log("=== Import Operation Completed Successfully ===");
                _logger.Log($"Total Duration: {result.Duration.TotalSeconds:F2}s");
                _logger.Log($"Total Records: {result.Statistics.TotalRecords}");
                _logger.Log($"Imported: {result.Statistics.ImportedRecords}");
                _logger.Log($"Skipped: {result.Statistics.SkippedRecords}");
                _logger.Log($"Records/Second: {perfReport.RecordsPerSecond:F2}");
                _logger.Log($"End Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                // 记录详细的错误上下文（需求 6.3）
                var errorContext = context.ErrorContext.GetFormattedContext();
                _logger.LogError($"Error occurred during import. Context: {errorContext}");
                
                // 回滚事务（需求 1.4）
                _logger.Log("Rolling back transaction...");
                try
                {
                    await transaction.RollbackAsync();
                    _logger.Log("Transaction rolled back successfully");
                    _auditLogger.LogTransactionRollback($"{ex.Message} | Context: {errorContext}");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"Failed to rollback transaction: {rollbackEx.Message}");
                    // 继续抛出原始异常
                }
                
                throw;
            }
        }
        catch (Exception ex)
        {
            // 记录详细的技术错误信息（需求 6.1, 6.2）
            _logger.LogError("=== Import Operation Failed ===");
            _logger.LogError($"Error Type: {ex.GetType().Name}");
            _logger.LogError($"Error Message: {ex.Message}");
            
            // 记录详细的错误上下文（需求 6.3, 6.4）
            string errorContext = "Unknown context";
            try
            {
                // 尝试从 context 获取错误上下文
                if (context?.ErrorContext != null)
                {
                    errorContext = context.ErrorContext.GetFormattedContext();
                }
                else if (tempErrorContext != null)
                {
                    // 如果 context 不可用，使用临时错误上下文
                    errorContext = tempErrorContext.GetFormattedContext();
                }
            }
            catch
            {
                // 如果获取上下文失败，使用默认值
            }
            
            _logger.LogError($"Error Context: {errorContext}");
            _logger.LogError($"Stack Trace: {ex.StackTrace}");
            _logger.LogError($"Detailed Error Info: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            
            // 转换为用户友好的错误消息（需求 6.4）
            var userFriendlyMessage = ErrorMessageTranslator.TranslateException(ex, "数据导入");
            result.Success = false;
            result.ErrorMessage = $"{userFriendlyMessage}\n\n错误位置: {errorContext}";
            result.Duration = DateTime.UtcNow - startTime;
            
            // 记录失败的审计日志
            _auditLogger.LogImportOperation(filePath, options, result);
            _auditLogger.LogOperationEnd("Import", false, $"{ex.Message} | Context: {errorContext}");
            
            // 获取并记录恢复建议（需求 6.5）
            var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
            var formattedSuggestions = ErrorRecoverySuggester.FormatSuggestions(suggestions);
            _logger.Log($"Recovery Suggestions:\n{formattedSuggestions}");
            
            // 将恢复建议添加到结果中
            if (result.Warnings == null)
            {
                result.Warnings = new List<string>();
            }
            result.Warnings.Add($"恢复建议:\n{formattedSuggestions}");
            
            _logger.LogError($"Import failed after {result.Duration.TotalSeconds:F2}s");
        }
        finally
        {
            // 确保导入锁在异常时也能释放（需求 6.5）
            if (lockManager != null)
            {
                try
                {
                    lockManager.ReleaseLock();
                    _logger.Log("Import lock released");
                }
                catch (Exception lockEx)
                {
                    _logger.LogError($"Failed to release import lock: {lockEx.Message}");
                    // 不抛出异常，避免掩盖原始错误
                }
            }
        }
        
        // 记录成功的审计日志
        if (result.Success)
        {
            _auditLogger.LogImportOperation(filePath, options, result);
            _auditLogger.LogOperationEnd("Import", true, "Import completed successfully");
        }
        
        return result;
    }

    /// <summary>
    /// 验证导入数据的完整性和正确性
    /// 
    /// 验证内容包括：
    /// 1. 文件存在性和格式验证
    /// 2. 必需字段验证
    /// 3. 主键重复检查（需求 10.5, 11.5）
    /// 4. 数据约束验证（长度、范围等）
    /// 5. 重复名称检测（警告，不阻止导入）（需求 11.2, 11.3）
    /// 6. 外键引用完整性验证（需求 10.3, 10.4）
    /// 
    /// 所有验证在事务开始前完成（需求 10.1, 10.2）
    /// </summary>
    /// <param name="filePath">要验证的文件路径</param>
    /// <returns>验证结果</returns>
    public async Task<ValidationResult> ValidateImportDataAsync(string filePath)
    {
        _logger.Log($"Starting data validation for: {filePath}");
        
        // 委托给验证服务
        var result = await _validationService.ValidateImportDataAsync(filePath);
        
        _logger.Log($"Validation completed. IsValid: {result.IsValid}, Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count}");
        
        // 记录审计日志
        _auditLogger.LogValidationOperation(filePath, result);
        
        return result;
    }

    #region Helper Methods for Export
    // Export methods have been migrated to DataExportService
    // This region is kept for backward compatibility with obsolete import methods
    #endregion

    #region Helper Methods for Import

    /// <summary>
    /// 导入技能数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportSkillsWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportSkillsWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportSkillsAsync(List<SkillDto> skills, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var skillDto in skills)
        {
            try
            {
                var exists = await _skillRepository.ExistsAsync(skillDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _skillRepository.DeleteAsync(skillDto.Id);
                        }
                        var skillToCreate = MapToSkill(skillDto);
                        await _skillRepository.CreateAsync(skillToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var skill = MapToSkill(skillDto);
                            await _skillRepository.CreateAsync(skill);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var skill = MapToSkill(skillDto);
                            await _skillRepository.CreateAsync(skill);
                            importedCount++;
                        }
                        // 如果存在，保留现有数据
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import skill {skillDto.Name}: {ex.Message}");
                _logger.LogError($"Skill ID: {skillDto.Id}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 使用事务导入技能数据（新实现）
    /// 使用 SkillImporter 执行批量导入操作
    /// </summary>
    /// <param name="skills">技能列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportSkillsWithTransactionAsync(
        List<SkillDto> skills,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (skills == null || skills.Count == 0)
        {
            _logger.Log("Skills: No records to import");
            return;
        }

        _logger.Log($"Skills: Starting transaction-based import of {skills.Count} records");

        var importer = new ImportExport.Importers.SkillImporter(_skillRepository, _logger);
        await importer.ImportAsync(skills, context, progress);

        _logger.Log($"Skills: Transaction-based import completed");
    }

    /// <summary>
    /// 使用事务导入人员数据（新实现）
    /// 使用 PersonnelImporter 执行批量导入操作
    /// 处理数组字段（SkillIds, RecentPeriodShiftIntervals）
    /// </summary>
    /// <param name="personnelList">人员列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportPersonnelWithTransactionAsync(
        List<PersonnelDto> personnelList,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (personnelList == null || personnelList.Count == 0)
        {
            _logger.Log("Personnel: No records to import");
            return;
        }

        _logger.Log($"Personnel: Starting transaction-based import of {personnelList.Count} records");

        var importer = new ImportExport.Importers.PersonnelImporter(_personnelRepository, _logger);
        await importer.ImportAsync(personnelList, context, progress);

        _logger.Log($"Personnel: Transaction-based import completed");
    }

    /// <summary>
    /// 使用事务导入哨位数据（新实现）
    /// 使用 PositionImporter 执行批量导入操作
    /// 处理数组字段（RequiredSkillIds, AvailablePersonnelIds）
    /// </summary>
    /// <param name="positionList">哨位列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportPositionsWithTransactionAsync(
        List<PositionDto> positionList,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (positionList == null || positionList.Count == 0)
        {
            _logger.Log("Positions: No records to import");
            return;
        }

        _logger.Log($"Positions: Starting transaction-based import of {positionList.Count} records");

        var importer = new ImportExport.Importers.PositionImporter(_positionRepository, _logger);
        await importer.ImportAsync(positionList, context, progress);

        _logger.Log($"Positions: Transaction-based import completed");
    }

    /// <summary>
    /// 使用事务导入模板数据（新实现）
    /// 使用 TemplateImporter 执行批量导入操作
    /// 处理复杂的模板字段（PersonnelIds, PositionIds, EnabledFixedRuleIds, EnabledManualAssignmentIds）
    /// </summary>
    /// <param name="templateList">模板列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportTemplatesWithTransactionAsync(
        List<SchedulingTemplateDto> templateList,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (templateList == null || templateList.Count == 0)
        {
            _logger.Log("Templates: No records to import");
            return;
        }

        _logger.Log($"Templates: Starting transaction-based import of {templateList.Count} records");

        var importer = new ImportExport.Importers.TemplateImporter(_templateRepository, _logger);
        await importer.ImportAsync(templateList, context, progress);

        _logger.Log($"Templates: Transaction-based import completed");
    }

    /// <summary>
    /// 使用事务导入节假日配置数据（新实现）
    /// 使用 HolidayConfigImporter 执行批量导入操作
    /// 处理复杂的日期列表字段（LegalHolidays, CustomHolidays, ExcludedDates）
    /// </summary>
    /// <param name="holidayConfigList">节假日配置列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportHolidayConfigsWithTransactionAsync(
        List<HolidayConfigDto> holidayConfigList,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (holidayConfigList == null || holidayConfigList.Count == 0)
        {
            _logger.Log("HolidayConfigs: No records to import");
            return;
        }

        _logger.Log($"HolidayConfigs: Starting transaction-based import of {holidayConfigList.Count} records");

        var importer = new ImportExport.Importers.HolidayConfigImporter(_constraintRepository, _logger);
        await importer.ImportAsync(holidayConfigList, context, progress);

        _logger.Log($"HolidayConfigs: Transaction-based import completed");
    }

    /// <summary>
    /// 使用事务导入固定分配数据（新实现）
    /// 使用 FixedAssignmentImporter 执行批量导入操作
    /// 处理数组字段（AllowedPositionIds, AllowedTimeSlots）
    /// </summary>
    /// <param name="fixedAssignmentList">固定分配列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportFixedAssignmentsWithTransactionAsync(
        List<FixedAssignmentDto> fixedAssignmentList,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (fixedAssignmentList == null || fixedAssignmentList.Count == 0)
        {
            _logger.Log("FixedAssignments: No records to import");
            return;
        }

        _logger.Log($"FixedAssignments: Starting transaction-based import of {fixedAssignmentList.Count} records");

        var importer = new ImportExport.Importers.FixedAssignmentImporter(_constraintRepository, _logger);
        await importer.ImportAsync(fixedAssignmentList, context, progress);

        _logger.Log($"FixedAssignments: Transaction-based import completed");
    }

    /// <summary>
    /// 使用事务导入手动分配数据（新实现）
    /// 使用 ManualAssignmentImporter 执行批量导入操作
    /// 处理日期字段和时段索引
    /// </summary>
    /// <param name="manualAssignmentList">手动分配列表</param>
    /// <param name="context">导入上下文</param>
    /// <param name="progress">进度报告器</param>
    private async Task ImportManualAssignmentsWithTransactionAsync(
        List<ManualAssignmentDto> manualAssignmentList,
        ImportContext context,
        IProgress<ImportProgress>? progress = null)
    {
        if (manualAssignmentList == null || manualAssignmentList.Count == 0)
        {
            _logger.Log("ManualAssignments: No records to import");
            return;
        }

        _logger.Log($"ManualAssignments: Starting transaction-based import of {manualAssignmentList.Count} records");

        var importer = new ImportExport.Importers.ManualAssignmentImporter(_constraintRepository, _logger);
        await importer.ImportAsync(manualAssignmentList, context, progress);

        _logger.Log($"ManualAssignments: Transaction-based import completed");
    }

    /// <summary>
    /// 导入人员数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportPersonnelWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportPersonnelWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportPersonnelAsync(List<PersonnelDto> personnelList, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var personnelDto in personnelList)
        {
            try
            {
                var exists = await _personnelRepository.ExistsAsync(personnelDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _personnelRepository.DeleteAsync(personnelDto.Id);
                        }
                        var personnelToCreate = MapToPersonnel(personnelDto);
                        await _personnelRepository.CreateAsync(personnelToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var personnel = MapToPersonnel(personnelDto);
                            await _personnelRepository.CreateAsync(personnel);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var personnel = MapToPersonnel(personnelDto);
                            await _personnelRepository.CreateAsync(personnel);
                            importedCount++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import personnel {personnelDto.Name}: {ex.Message}");
                _logger.LogError($"Personnel ID: {personnelDto.Id}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 导入哨位数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportPositionsWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportPositionsWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportPositionsAsync(List<PositionDto> positions, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var positionDto in positions)
        {
            try
            {
                var exists = await _positionRepository.ExistsAsync(positionDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _positionRepository.DeleteAsync(positionDto.Id);
                        }
                        var positionToCreate = MapToPosition(positionDto);
                        await _positionRepository.CreateAsync(positionToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var position = MapToPosition(positionDto);
                            await _positionRepository.CreateAsync(position);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var position = MapToPosition(positionDto);
                            await _positionRepository.CreateAsync(position);
                            importedCount++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import position {positionDto.Name}: {ex.Message}");
                _logger.LogError($"Position ID: {positionDto.Id}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 导入节假日配置数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportHolidayConfigsWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportHolidayConfigsWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportHolidayConfigsAsync(List<HolidayConfigDto> holidayConfigs, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var configDto in holidayConfigs)
        {
            try
            {
                // Check if holiday config exists by getting all and comparing IDs
                var allConfigs = await _constraintRepository.GetAllHolidayConfigsAsync();
                var exists = allConfigs.Any(c => c.Id == configDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _constraintRepository.DeleteHolidayConfigAsync(configDto.Id);
                        }
                        var configToCreate = MapToHolidayConfig(configDto);
                        await _constraintRepository.AddHolidayConfigAsync(configToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var config = MapToHolidayConfig(configDto);
                            await _constraintRepository.AddHolidayConfigAsync(config);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var config = MapToHolidayConfig(configDto);
                            await _constraintRepository.AddHolidayConfigAsync(config);
                            importedCount++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import holiday config {configDto.ConfigName}: {ex.Message}");
                _logger.LogError($"Config ID: {configDto.Id}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 导入排班模板数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportTemplatesWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportTemplatesWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportTemplatesAsync(List<SchedulingTemplateDto> templates, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var templateDto in templates)
        {
            try
            {
                var exists = await _templateRepository.ExistsAsync(templateDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _templateRepository.DeleteAsync(templateDto.Id);
                        }
                        var templateToCreate = MapToTemplate(templateDto);
                        await _templateRepository.CreateAsync(templateToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var template = MapToTemplate(templateDto);
                            await _templateRepository.CreateAsync(template);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var template = MapToTemplate(templateDto);
                            await _templateRepository.CreateAsync(template);
                            importedCount++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import template {templateDto.Name}: {ex.Message}");
                _logger.LogError($"Template ID: {templateDto.Id}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 导入固定分配约束数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportFixedAssignmentsWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportFixedAssignmentsWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportFixedAssignmentsAsync(List<FixedAssignmentDto> fixedAssignments, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var assignmentDto in fixedAssignments)
        {
            try
            {
                // Check if fixed assignment exists by getting all and comparing IDs
                var allAssignments = await _constraintRepository.GetAllFixedPositionRulesAsync(enabledOnly: false);
                var exists = allAssignments.Any(a => a.Id == assignmentDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _constraintRepository.DeleteFixedPositionRuleAsync(assignmentDto.Id);
                        }
                        var assignmentToCreate = MapToFixedPositionRule(assignmentDto);
                        await _constraintRepository.AddFixedPositionRuleAsync(assignmentToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var assignment = MapToFixedPositionRule(assignmentDto);
                            await _constraintRepository.AddFixedPositionRuleAsync(assignment);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var assignment = MapToFixedPositionRule(assignmentDto);
                            await _constraintRepository.AddFixedPositionRuleAsync(assignment);
                            importedCount++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import fixed assignment {assignmentDto.Id}: {ex.Message}");
                _logger.LogError($"Assignment ID: {assignmentDto.Id}, Personnel ID: {assignmentDto.PersonnelId}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 导入手动分配约束数据
    /// </summary>
    /// <remarks>
    /// 此方法已过时，请使用 ImportManualAssignmentsWithTransactionAsync 方法。
    /// 新方法提供了事务保护、批量操作和更好的性能。
    /// </remarks>
    [Obsolete("此方法已过时，请使用 ImportManualAssignmentsWithTransactionAsync 方法，该方法提供事务保护和批量操作支持。", false)]
    private async Task<int> ImportManualAssignmentsAsync(List<ManualAssignmentDto> manualAssignments, ImportOptions options)
    {
        int importedCount = 0;
        
        foreach (var assignmentDto in manualAssignments)
        {
            try
            {
                // Check if manual assignment exists by getting all and comparing IDs
                var startDate = DateTime.Now.AddYears(-10);
                var endDate = DateTime.Now.AddYears(10);
                var allAssignments = await _constraintRepository.GetManualAssignmentsByDateRangeAsync(startDate, endDate, enabledOnly: false);
                var exists = allAssignments.Any(a => a.Id == assignmentDto.Id);
                
                switch (options.Strategy)
                {
                    case ConflictResolutionStrategy.Replace:
                        // 覆盖策略：先删除现有记录，再创建新记录
                        if (exists)
                        {
                            await _constraintRepository.DeleteManualAssignmentAsync(assignmentDto.Id);
                        }
                        var assignmentToCreate = MapToManualAssignment(assignmentDto);
                        await _constraintRepository.AddManualAssignmentAsync(assignmentToCreate);
                        importedCount++;
                        break;
                        
                    case ConflictResolutionStrategy.Skip:
                        if (!exists)
                        {
                            var assignment = MapToManualAssignment(assignmentDto);
                            await _constraintRepository.AddManualAssignmentAsync(assignment);
                            importedCount++;
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        if (!exists)
                        {
                            var assignment = MapToManualAssignment(assignmentDto);
                            await _constraintRepository.AddManualAssignmentAsync(assignment);
                            importedCount++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to import manual assignment {assignmentDto.Id}: {ex.Message}");
                _logger.LogError($"Assignment ID: {assignmentDto.Id}, Position ID: {assignmentDto.PositionId}, Personnel ID: {assignmentDto.PersonnelId}, Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
                
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }
        
        return importedCount;
    }

    /// <summary>
    /// 验证导入后的数据完整性
    /// </summary>
    private async Task VerifyImportedDataIntegrity(ImportResult result)
    {
        try
        {
            // 验证基本数据存在性
            var skills = await _skillRepository.GetAllAsync();
            var personnel = await _personnelRepository.GetAllAsync();
            var positions = await _positionRepository.GetAllAsync();
            
            _logger.Log($"Post-import verification: {skills.Count} skills, {personnel.Count} personnel, {positions.Count} positions");
            
            // 可以添加更多的完整性检查
            // 例如：验证外键引用、检查数据一致性等
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Data integrity verification warning: {ex.Message}");
            _logger.LogWarning($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            
            var userFriendlyMessage = ErrorMessageTranslator.TranslateException(ex, "数据完整性验证");
            result.Warnings.Add(userFriendlyMessage);
        }
    }

    #endregion

    #region Mapping Methods
    // Mapping methods have been migrated to DataMappingService
    // This region is kept for backward compatibility with obsolete import methods
    
    /// <summary>
    /// 将 SkillDto 映射到 Skill 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToSkill", false)]
    private Models.Skill MapToSkill(SkillDto dto)
    {
        return _mappingService.MapToSkill(dto);
    }

    /// <summary>
    /// 将 PersonnelDto 映射到 Personal 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToPersonnel", false)]
    private Models.Personal MapToPersonnel(PersonnelDto dto)
    {
        return _mappingService.MapToPersonnel(dto);
    }

    /// <summary>
    /// 将 PositionDto 映射到 PositionLocation 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToPosition", false)]
    private Models.PositionLocation MapToPosition(PositionDto dto)
    {
        return _mappingService.MapToPosition(dto);
    }

    /// <summary>
    /// 将 HolidayConfigDto 映射到 HolidayConfig 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToHolidayConfig", false)]
    private Models.Constraints.HolidayConfig MapToHolidayConfig(HolidayConfigDto dto)
    {
        return _mappingService.MapToHolidayConfig(dto);
    }

    /// <summary>
    /// 将 SchedulingTemplateDto 映射到 SchedulingTemplate 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToTemplate", false)]
    private Models.SchedulingTemplate MapToTemplate(SchedulingTemplateDto dto)
    {
        return _mappingService.MapToTemplate(dto);
    }

    /// <summary>
    /// 将 FixedAssignmentDto 映射到 FixedPositionRule 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToFixedPositionRule", false)]
    private Models.Constraints.FixedPositionRule MapToFixedPositionRule(FixedAssignmentDto dto)
    {
        return _mappingService.MapToFixedPositionRule(dto);
    }

    /// <summary>
    /// 将 ManualAssignmentDto 映射到 ManualAssignment 模型
    /// </summary>
    /// <remarks>已迁移到 DataMappingService，此方法仅用于向后兼容</remarks>
    [Obsolete("此方法已迁移到 DataMappingService，请使用 IDataMappingService.MapToManualAssignment", false)]
    private Models.Constraints.ManualAssignment MapToManualAssignment(ManualAssignmentDto dto)
    {
        return _mappingService.MapToManualAssignment(dto);
    }

    #endregion
}
