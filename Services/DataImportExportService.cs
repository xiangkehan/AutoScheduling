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
/// 数据导入导出服务实现
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
    public DataImportExportService(
        IPersonalRepository personnelRepository,
        IPositionRepository positionRepository,
        ISkillRepository skillRepository,
        ITemplateRepository templateRepository,
        IConstraintRepository constraintRepository,
        DatabaseBackupManager backupManager,
        ILogger logger)
    {
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _constraintRepository = constraintRepository ?? throw new ArgumentNullException(nameof(constraintRepository));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var exportData = new ExportData
            {
                Metadata = new ExportMetadata
                {
                    ExportedAt = DateTime.UtcNow,
                    DatabaseVersion = await GetDatabaseVersionAsync(),
                    ApplicationVersion = GetApplicationVersion()
                }
            };
            
            _logger.Log("Export metadata created");
            
            // 2. 按表导出数据（带进度报告）
            // 导出技能
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Skills", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 0 
            });
            exportData.Skills = await ExportSkillsAsync();
            _logger.Log($"Exported {exportData.Skills.Count} skills");
            
            // 导出人员
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Personnel", 
                ProcessedRecords = exportData.Skills.Count,
                TotalRecords = 0,
                PercentComplete = 20 
            });
            exportData.Personnel = await ExportPersonnelAsync();
            _logger.Log($"Exported {exportData.Personnel.Count} personnel");
            
            // 导出哨位
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Positions", 
                ProcessedRecords = exportData.Skills.Count + exportData.Personnel.Count,
                TotalRecords = 0,
                PercentComplete = 40 
            });
            exportData.Positions = await ExportPositionsAsync();
            _logger.Log($"Exported {exportData.Positions.Count} positions");
            
            // 导出模板
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Templates", 
                ProcessedRecords = exportData.Skills.Count + exportData.Personnel.Count + exportData.Positions.Count,
                TotalRecords = 0,
                PercentComplete = 60 
            });
            exportData.Templates = await ExportTemplatesAsync();
            _logger.Log($"Exported {exportData.Templates.Count} templates");
            
            // 导出约束
            progress?.Report(new ExportProgress 
            { 
                CurrentTable = "Constraints", 
                ProcessedRecords = 0,
                TotalRecords = 0,
                PercentComplete = 80 
            });
            exportData.FixedAssignments = await ExportFixedAssignmentsAsync();
            exportData.ManualAssignments = await ExportManualAssignmentsAsync();
            exportData.HolidayConfigs = await ExportHolidayConfigsAsync();
            _logger.Log($"Exported {exportData.FixedAssignments.Count} fixed assignments, " +
                       $"{exportData.ManualAssignments.Count} manual assignments, " +
                       $"{exportData.HolidayConfigs.Count} holiday configs");
            
            // 3. 更新统计信息
            exportData.Metadata.Statistics = CalculateStatistics(exportData);
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
        
        try
        {
            _logger.Log($"Starting data import from: {filePath}");
            
            // 1. 获取导入锁
            lockManager = new ImportLockManager();
            if (!await lockManager.TryAcquireLockAsync())
            {
                throw new InvalidOperationException("另一个导入操作正在进行中，请稍后再试");
            }
            
            _logger.Log("Import lock acquired");
            
            // 2. 读取并解析 JSON 文件
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
            
            var exportData = JsonSerializer.Deserialize<ExportData>(json, _jsonOptions);
            if (exportData == null)
            {
                throw new InvalidOperationException("无法解析导入文件：反序列化结果为空");
            }
            
            _logger.Log("JSON deserialized successfully");
            
            // 3. 验证数据（事务前）
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
            
            // 6. 创建性能监控器
            var performanceMonitor = new PerformanceMonitor();
            performanceMonitor.StartOperation("Total");
            
            // 7. 开始事务导入
            var connectionString = GetConnectionString();
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            
            var context = new ImportContext
            {
                Connection = connection,
                Transaction = transaction,
                Options = options,
                PerformanceMonitor = performanceMonitor,
                Statistics = result.Statistics,
                Warnings = result.Warnings
            };
            
            try
            {
                int processedRecords = 0;
                
                // 按依赖顺序导入各表（使用旧的导入方法，因为新的 WithTransaction 方法还未实现）
                // 导入技能（无依赖）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Skills", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 30 
                });
                
                var skillsImported = await ImportSkillsAsync(exportData.Skills ?? new List<SkillDto>(), options);
                processedRecords += exportData.Skills?.Count ?? 0;
                result.Statistics.RecordsByTable["Skills"] = skillsImported;
                _logger.Log($"Imported {skillsImported} skills");
                
                // 导入人员（依赖技能）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Personnel", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 45 
                });
                
                var personnelImported = await ImportPersonnelAsync(exportData.Personnel ?? new List<PersonnelDto>(), options);
                processedRecords += exportData.Personnel?.Count ?? 0;
                result.Statistics.RecordsByTable["Personnel"] = personnelImported;
                _logger.Log($"Imported {personnelImported} personnel");
                
                // 导入哨位（依赖技能和人员）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Positions", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 60 
                });
                
                var positionsImported = await ImportPositionsAsync(exportData.Positions ?? new List<PositionDto>(), options);
                processedRecords += exportData.Positions?.Count ?? 0;
                result.Statistics.RecordsByTable["Positions"] = positionsImported;
                _logger.Log($"Imported {positionsImported} positions");
                
                // 导入节假日配置（无依赖）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "HolidayConfigs", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 70 
                });
                
                var holidayConfigsImported = await ImportHolidayConfigsAsync(exportData.HolidayConfigs ?? new List<HolidayConfigDto>(), options);
                processedRecords += exportData.HolidayConfigs?.Count ?? 0;
                result.Statistics.RecordsByTable["HolidayConfigs"] = holidayConfigsImported;
                _logger.Log($"Imported {holidayConfigsImported} holiday configs");
                
                // 导入模板（依赖人员、哨位、节假日配置）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "Templates", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 80 
                });
                
                var templatesImported = await ImportTemplatesAsync(exportData.Templates ?? new List<SchedulingTemplateDto>(), options);
                processedRecords += exportData.Templates?.Count ?? 0;
                result.Statistics.RecordsByTable["Templates"] = templatesImported;
                _logger.Log($"Imported {templatesImported} templates");
                
                // 导入固定分配（依赖人员、哨位）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "FixedAssignments", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 90 
                });
                
                var fixedAssignmentsImported = await ImportFixedAssignmentsAsync(exportData.FixedAssignments ?? new List<FixedAssignmentDto>(), options);
                processedRecords += exportData.FixedAssignments?.Count ?? 0;
                result.Statistics.RecordsByTable["FixedAssignments"] = fixedAssignmentsImported;
                _logger.Log($"Imported {fixedAssignmentsImported} fixed assignments");
                
                // 导入手动分配（依赖人员、哨位）
                progress?.Report(new ImportProgress 
                { 
                    CurrentTable = "ManualAssignments", 
                    CurrentOperation = "Importing",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    PercentComplete = 95 
                });
                
                var manualAssignmentsImported = await ImportManualAssignmentsAsync(exportData.ManualAssignments ?? new List<ManualAssignmentDto>(), options);
                processedRecords += exportData.ManualAssignments?.Count ?? 0;
                result.Statistics.RecordsByTable["ManualAssignments"] = manualAssignmentsImported;
                _logger.Log($"Imported {manualAssignmentsImported} manual assignments");
                
                // 提交事务
                await transaction.CommitAsync();
                _logger.Log("Transaction committed successfully");
                
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
                
                // 生成性能报告
                var perfReport = performanceMonitor.GenerateReport(result.Statistics.TotalRecords);
                _logger.Log($"Import performance: {perfReport.Summary}");
                
                _logger.Log($"Import completed successfully in {result.Duration.TotalSeconds:F2} seconds. " +
                           $"Imported: {result.Statistics.ImportedRecords}, Skipped: {result.Statistics.SkippedRecords}");
            }
            catch (Exception ex)
            {
                // 回滚事务
                await transaction.RollbackAsync();
                _logger.Log("Transaction rolled back due to error");
                throw;
            }
        }
        catch (Exception ex)
        {
            // 记录详细的技术错误信息
            _logger.LogError($"Import failed: {ex.Message}");
            _logger.LogError($"Detailed error info: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            
            // 转换为用户友好的错误消息
            var userFriendlyMessage = ErrorMessageTranslator.TranslateException(ex, "数据导入");
            result.Success = false;
            result.ErrorMessage = userFriendlyMessage;
            result.Duration = DateTime.UtcNow - startTime;
            
            // 记录审计日志
            _auditLogger.LogImportOperation(filePath, options, result);
            
            // 获取并记录恢复建议
            var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
            var formattedSuggestions = ErrorRecoverySuggester.FormatSuggestions(suggestions);
            _logger.Log($"Recovery suggestions: {formattedSuggestions}");
        }
        finally
        {
            // 释放导入锁
            lockManager?.ReleaseLock();
            _logger.Log("Import lock released");
        }
        
        // 记录成功的审计日志
        if (result.Success)
        {
            _auditLogger.LogImportOperation(filePath, options, result);
        }
        
        return result;
    }

    /// <summary>
    /// 验证导入数据的完整性和正确性
    /// </summary>
    /// <param name="filePath">要验证的文件路径</param>
    /// <returns>验证结果</returns>
    public async Task<ValidationResult> ValidateImportDataAsync(string filePath)
    {
        _logger.Log($"Starting data validation for: {filePath}");
        
        var result = new ValidationResult
        {
            IsValid = true,
            Errors = new List<ValidationError>(),
            Warnings = new List<ValidationWarning>()
        };
        
        try
        {
            // 1. 验证文件存在性
            if (!File.Exists(filePath))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Table = "File",
                    Message = $"文件不存在: {filePath}",
                    Type = ValidationErrorType.MissingField
                });
                return result;
            }
            
            // 2. 读取并解析 JSON 文件
            string json;
            try
            {
                json = await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Table = "File",
                    Message = $"无法读取文件: {ex.Message}",
                    Type = ValidationErrorType.InvalidDataType
                });
                return result;
            }
            
            // 3. 验证 JSON 格式
            ExportData? exportData;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                exportData = JsonSerializer.Deserialize<ExportData>(json, options);
                
                if (exportData == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "JSON",
                        Message = "JSON 反序列化结果为空",
                        Type = ValidationErrorType.InvalidDataType
                    });
                    return result;
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Table = "JSON",
                    Message = $"JSON 格式无效: {ex.Message}",
                    Type = ValidationErrorType.InvalidDataType
                });
                return result;
            }
            
            // 4. 验证元数据
            if (exportData.Metadata == null)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Table = "Metadata",
                    Message = "缺少元数据信息"
                });
            }
            else
            {
                result.Metadata = exportData.Metadata;
            }
            
            // 5. 验证必需字段和数据类型
            ValidateRequiredFields(exportData, result);
            
            // 6. 验证数据约束
            ValidateDataConstraints(exportData, result);
            
            // 7. 验证外键引用
            await ValidateForeignKeyReferences(exportData, result);
            
            _logger.Log($"Validation completed. IsValid: {result.IsValid}, Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count}");
            
            // 记录审计日志
            _auditLogger.LogValidationOperation(filePath, result);
        }
        catch (Exception ex)
        {
            // 记录详细的技术错误信息
            _logger.LogError($"Validation failed with exception: {ex.Message}");
            _logger.LogError($"Detailed error info: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            
            // 转换为用户友好的错误消息
            var userFriendlyMessage = ErrorMessageTranslator.TranslateException(ex, "数据验证");
            
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Table = "System",
                Message = userFriendlyMessage,
                Type = ValidationErrorType.InvalidValue
            });
            
            // 记录审计日志
            _auditLogger.LogErrorOperation("Validation", ex, filePath);
            
            // 获取并记录恢复建议
            var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "验证");
            var formattedSuggestions = ErrorRecoverySuggester.FormatSuggestions(suggestions);
            _logger.Log($"Recovery suggestions: {formattedSuggestions}");
        }
        
        return result;
    }

    #region Helper Methods for Export

    /// <summary>
    /// 获取数据库版本
    /// </summary>
    private async Task<int> GetDatabaseVersionAsync()
    {
        try
        {
            // 使用 SkillRepository 的连接字符串来访问数据库
            // 由于我们没有直接访问连接字符串，我们将使用一个合理的默认值
            // 在实际实现中，这应该从配置或数据库服务获取
            return 1; // 当前数据库版本
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to get database version: {ex.Message}");
            return 1; // 默认版本
        }
    }

    /// <summary>
    /// 获取应用程序版本
    /// </summary>
    private string GetApplicationVersion()
    {
        try
        {
            // 从程序集获取版本信息
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to get application version: {ex.Message}");
            return "1.0.0.0";
        }
    }

    /// <summary>
    /// 计算数据统计信息
    /// </summary>
    private DataStatistics CalculateStatistics(ExportData exportData)
    {
        return new DataStatistics
        {
            SkillCount = exportData.Skills?.Count ?? 0,
            PersonnelCount = exportData.Personnel?.Count ?? 0,
            PositionCount = exportData.Positions?.Count ?? 0,
            TemplateCount = exportData.Templates?.Count ?? 0,
            ConstraintCount = (exportData.FixedAssignments?.Count ?? 0) +
                            (exportData.ManualAssignments?.Count ?? 0) +
                            (exportData.HolidayConfigs?.Count ?? 0)
        };
    }

    /// <summary>
    /// 导出技能数据
    /// </summary>
    private async Task<List<SkillDto>> ExportSkillsAsync()
    {
        try
        {
            var skills = await _skillRepository.GetAllAsync();
            return skills.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export skills: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    /// <summary>
    /// 导出人员数据
    /// </summary>
    private async Task<List<PersonnelDto>> ExportPersonnelAsync()
    {
        try
        {
            var personnel = await _personnelRepository.GetAllAsync();
            return personnel.Select(p => new PersonnelDto
            {
                Id = p.Id,
                Name = p.Name,
                SkillIds = p.SkillIds,
                IsAvailable = p.IsAvailable,
                IsRetired = p.IsRetired,
                RecentShiftIntervalCount = p.RecentShiftIntervalCount,
                RecentHolidayShiftIntervalCount = p.RecentHolidayShiftIntervalCount,
                RecentPeriodShiftIntervals = p.RecentPeriodShiftIntervals
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export personnel: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    /// <summary>
    /// 导出哨位数据
    /// </summary>
    private async Task<List<PositionDto>> ExportPositionsAsync()
    {
        try
        {
            var positions = await _positionRepository.GetAllAsync();
            return positions.Select(p => new PositionDto
            {
                Id = p.Id,
                Name = p.Name,
                Location = p.Location,
                Description = p.Description,
                Requirements = p.Requirements,
                RequiredSkillIds = p.RequiredSkillIds,
                AvailablePersonnelIds = p.AvailablePersonnelIds,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export positions: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    /// <summary>
    /// 导出排班模板数据
    /// </summary>
    private async Task<List<SchedulingTemplateDto>> ExportTemplatesAsync()
    {
        try
        {
            var templates = await _templateRepository.GetAllAsync();
            return templates.Select(t => new SchedulingTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                TemplateType = t.TemplateType,
                IsDefault = t.IsDefault,
                PersonnelIds = t.PersonnelIds,
                PositionIds = t.PositionIds,
                HolidayConfigId = t.HolidayConfigId,
                UseActiveHolidayConfig = t.UseActiveHolidayConfig,
                EnabledFixedRuleIds = t.EnabledFixedRuleIds,
                EnabledManualAssignmentIds = t.EnabledManualAssignmentIds,
                DurationDays = t.DurationDays,
                StrategyConfig = t.StrategyConfig,
                UsageCount = t.UsageCount,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LastUsedAt = t.LastUsedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export templates: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    /// <summary>
    /// 导出固定分配约束数据
    /// </summary>
    private async Task<List<FixedAssignmentDto>> ExportFixedAssignmentsAsync()
    {
        try
        {
            var fixedAssignments = await _constraintRepository.GetAllFixedPositionRulesAsync(enabledOnly: false);
            return fixedAssignments.Select(f => new FixedAssignmentDto
            {
                Id = f.Id,
                PersonnelId = f.PersonalId,
                AllowedPositionIds = f.AllowedPositionIds,
                AllowedTimeSlots = f.AllowedPeriods,
                StartDate = DateTime.MinValue, // Not stored in database
                EndDate = DateTime.MaxValue, // Not stored in database
                IsEnabled = f.IsEnabled,
                RuleName = $"Rule_{f.Id}",
                Description = f.Description,
                CreatedAt = DateTime.UtcNow, // Not stored in database
                UpdatedAt = DateTime.UtcNow // Not stored in database
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export fixed assignments: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    /// <summary>
    /// 导出手动分配约束数据
    /// </summary>
    private async Task<List<ManualAssignmentDto>> ExportManualAssignmentsAsync()
    {
        try
        {
            // Get manual assignments for a wide date range to capture all records
            var startDate = DateTime.Now.AddYears(-10);
            var endDate = DateTime.Now.AddYears(10);
            var manualAssignments = await _constraintRepository.GetManualAssignmentsByDateRangeAsync(startDate, endDate, enabledOnly: false);
            return manualAssignments.Select(m => new ManualAssignmentDto
            {
                Id = m.Id,
                PositionId = m.PositionId,
                TimeSlot = m.PeriodIndex,
                PersonnelId = m.PersonalId,
                Date = m.Date,
                IsEnabled = m.IsEnabled,
                Remarks = m.Remarks,
                CreatedAt = DateTime.UtcNow, // Not stored in database
                UpdatedAt = DateTime.UtcNow // Not stored in database
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export manual assignments: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    /// <summary>
    /// 导出节假日配置数据
    /// </summary>
    private async Task<List<HolidayConfigDto>> ExportHolidayConfigsAsync()
    {
        try
        {
            var holidayConfigs = await _constraintRepository.GetAllHolidayConfigsAsync();
            return holidayConfigs.Select(h => new HolidayConfigDto
            {
                Id = h.Id,
                ConfigName = h.ConfigName,
                EnableWeekendRule = h.EnableWeekendRule,
                WeekendDays = h.WeekendDays,
                LegalHolidays = h.LegalHolidays,
                CustomHolidays = h.CustomHolidays,
                ExcludedDates = h.ExcludedDates,
                IsActive = h.IsActive
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export holiday configs: {ex.Message}");
            _logger.LogError($"Details: {ErrorMessageTranslator.GetDetailedErrorInfo(ex)}");
            throw;
        }
    }

    #endregion

    #region Validation Helper Methods

    /// <summary>
    /// 验证必需字段存在性和数据类型
    /// </summary>
    private void ValidateRequiredFields(ExportData exportData, ValidationResult result)
    {
        // 验证 Skills
        if (exportData.Skills != null)
        {
            for (int i = 0; i < exportData.Skills.Count; i++)
            {
                var skill = exportData.Skills[i];
                
                if (string.IsNullOrWhiteSpace(skill.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Skills",
                        RecordId = skill.Id,
                        Field = "Name",
                        Message = $"技能名称不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
            }
        }
        
        // 验证 Personnel
        if (exportData.Personnel != null)
        {
            for (int i = 0; i < exportData.Personnel.Count; i++)
            {
                var personnel = exportData.Personnel[i];
                
                if (string.IsNullOrWhiteSpace(personnel.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Personnel",
                        RecordId = personnel.Id,
                        Field = "Name",
                        Message = $"人员名称不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
                
                if (personnel.SkillIds == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Personnel",
                        RecordId = personnel.Id,
                        Field = "SkillIds",
                        Message = $"人员技能列表不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
                
                if (personnel.RecentPeriodShiftIntervals == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Personnel",
                        RecordId = personnel.Id,
                        Field = "RecentPeriodShiftIntervals",
                        Message = $"人员班次间隔数组不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
            }
        }
        
        // 验证 Positions
        if (exportData.Positions != null)
        {
            for (int i = 0; i < exportData.Positions.Count; i++)
            {
                var position = exportData.Positions[i];
                
                if (string.IsNullOrWhiteSpace(position.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "Name",
                        Message = $"哨位名称不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
                
                if (string.IsNullOrWhiteSpace(position.Location))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "Location",
                        Message = $"哨位地点不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
                
                if (position.RequiredSkillIds == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "RequiredSkillIds",
                        Message = $"哨位所需技能列表不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
                
                if (position.AvailablePersonnelIds == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "AvailablePersonnelIds",
                        Message = $"哨位可用人员列表不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
            }
        }
        
        // 验证 Templates
        if (exportData.Templates != null)
        {
            for (int i = 0; i < exportData.Templates.Count; i++)
            {
                var template = exportData.Templates[i];
                
                if (string.IsNullOrWhiteSpace(template.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Templates",
                        RecordId = template.Id,
                        Field = "Name",
                        Message = $"模板名称不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
            }
        }
        
        // 验证 FixedAssignments
        if (exportData.FixedAssignments != null)
        {
            for (int i = 0; i < exportData.FixedAssignments.Count; i++)
            {
                var assignment = exportData.FixedAssignments[i];
                
                if (assignment.AllowedPositionIds == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "FixedAssignments",
                        RecordId = assignment.Id,
                        Field = "AllowedPositionIds",
                        Message = $"固定分配允许哨位列表不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
                
                if (assignment.AllowedTimeSlots == null)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "FixedAssignments",
                        RecordId = assignment.Id,
                        Field = "AllowedTimeSlots",
                        Message = $"固定分配允许时间段列表不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
            }
        }
        
        // 验证 HolidayConfigs
        if (exportData.HolidayConfigs != null)
        {
            for (int i = 0; i < exportData.HolidayConfigs.Count; i++)
            {
                var config = exportData.HolidayConfigs[i];
                
                if (string.IsNullOrWhiteSpace(config.ConfigName))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "HolidayConfigs",
                        RecordId = config.Id,
                        Field = "ConfigName",
                        Message = $"节假日配置名称不能为空 (记录索引: {i})",
                        Type = ValidationErrorType.MissingField
                    });
                }
            }
        }
    }

    /// <summary>
    /// 验证数据约束（字符串长度、数值范围、唯一性）
    /// </summary>
    private void ValidateDataConstraints(ExportData exportData, ValidationResult result)
    {
        // 验证 Skills 约束
        if (exportData.Skills != null)
        {
            var skillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            for (int i = 0; i < exportData.Skills.Count; i++)
            {
                var skill = exportData.Skills[i];
                
                // 验证名称长度
                if (!string.IsNullOrWhiteSpace(skill.Name))
                {
                    if (skill.Name.Length > 50)
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "Skills",
                            RecordId = skill.Id,
                            Field = "Name",
                            Message = $"技能名称长度不能超过50字符 (当前: {skill.Name.Length}, 记录索引: {i})",
                            Type = ValidationErrorType.ConstraintViolation
                        });
                    }
                    
                    // 验证名称唯一性
                    if (skillNames.Contains(skill.Name))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "Skills",
                            RecordId = skill.Id,
                            Field = "Name",
                            Message = $"技能名称重复: {skill.Name} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        skillNames.Add(skill.Name);
                    }
                }
                
                // 验证描述长度
                if (!string.IsNullOrWhiteSpace(skill.Description) && skill.Description.Length > 200)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Skills",
                        RecordId = skill.Id,
                        Field = "Description",
                        Message = $"技能描述长度不能超过200字符 (当前: {skill.Description.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
            }
        }
        
        // 验证 Personnel 约束
        if (exportData.Personnel != null)
        {
            for (int i = 0; i < exportData.Personnel.Count; i++)
            {
                var personnel = exportData.Personnel[i];
                
                // 验证名称长度（假设最大100字符）
                if (!string.IsNullOrWhiteSpace(personnel.Name) && personnel.Name.Length > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Personnel",
                        RecordId = personnel.Id,
                        Field = "Name",
                        Message = $"人员名称长度不能超过100字符 (当前: {personnel.Name.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
                
                // 验证数值范围
                if (personnel.RecentShiftIntervalCount < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Personnel",
                        RecordId = personnel.Id,
                        Field = "RecentShiftIntervalCount",
                        Message = $"班次间隔计数不能为负数 (当前: {personnel.RecentShiftIntervalCount}, 记录索引: {i})",
                        Type = ValidationErrorType.InvalidValue
                    });
                }
                
                if (personnel.RecentHolidayShiftIntervalCount < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Personnel",
                        RecordId = personnel.Id,
                        Field = "RecentHolidayShiftIntervalCount",
                        Message = $"节假日班次间隔计数不能为负数 (当前: {personnel.RecentHolidayShiftIntervalCount}, 记录索引: {i})",
                        Type = ValidationErrorType.InvalidValue
                    });
                }
                
                // 验证数组长度
                if (personnel.RecentPeriodShiftIntervals != null && personnel.RecentPeriodShiftIntervals.Length != 12)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Table = "Personnel",
                        Message = $"人员 {personnel.Name} 的班次间隔数组长度应为12 (当前: {personnel.RecentPeriodShiftIntervals.Length}, 记录索引: {i})"
                    });
                }
            }
        }
        
        // 验证 Positions 约束
        if (exportData.Positions != null)
        {
            for (int i = 0; i < exportData.Positions.Count; i++)
            {
                var position = exportData.Positions[i];
                
                // 验证名称长度
                if (!string.IsNullOrWhiteSpace(position.Name) && position.Name.Length > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "Name",
                        Message = $"哨位名称长度不能超过100字符 (当前: {position.Name.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
                
                // 验证地点长度
                if (!string.IsNullOrWhiteSpace(position.Location) && position.Location.Length > 200)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "Location",
                        Message = $"哨位地点长度不能超过200字符 (当前: {position.Location.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
                
                // 验证描述长度
                if (!string.IsNullOrWhiteSpace(position.Description) && position.Description.Length > 500)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "Description",
                        Message = $"哨位描述长度不能超过500字符 (当前: {position.Description.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
                
                // 验证要求长度
                if (!string.IsNullOrWhiteSpace(position.Requirements) && position.Requirements.Length > 1000)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Positions",
                        RecordId = position.Id,
                        Field = "Requirements",
                        Message = $"哨位要求长度不能超过1000字符 (当前: {position.Requirements.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
            }
        }
        
        // 验证 Templates 约束
        if (exportData.Templates != null)
        {
            for (int i = 0; i < exportData.Templates.Count; i++)
            {
                var template = exportData.Templates[i];
                
                // 验证名称长度
                if (!string.IsNullOrWhiteSpace(template.Name) && template.Name.Length > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Templates",
                        RecordId = template.Id,
                        Field = "Name",
                        Message = $"模板名称长度不能超过100字符 (当前: {template.Name.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
                
                // 验证描述长度
                if (!string.IsNullOrWhiteSpace(template.Description) && template.Description.Length > 500)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Templates",
                        RecordId = template.Id,
                        Field = "Description",
                        Message = $"模板描述长度不能超过500字符 (当前: {template.Description.Length}, 记录索引: {i})",
                        Type = ValidationErrorType.ConstraintViolation
                    });
                }
                
                // 验证持续天数
                if (template.DurationDays <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "Templates",
                        RecordId = template.Id,
                        Field = "DurationDays",
                        Message = $"模板持续天数必须大于0 (当前: {template.DurationDays}, 记录索引: {i})",
                        Type = ValidationErrorType.InvalidValue
                    });
                }
            }
        }
    }

    /// <summary>
    /// 验证外键引用完整性
    /// </summary>
    private async Task ValidateForeignKeyReferences(ExportData exportData, ValidationResult result)
    {
        // 收集所有有效的 ID
        var validSkillIds = new HashSet<int>();
        var validPersonnelIds = new HashSet<int>();
        var validPositionIds = new HashSet<int>();
        var validHolidayConfigIds = new HashSet<int>();
        
        if (exportData.Skills != null)
        {
            foreach (var skill in exportData.Skills)
            {
                validSkillIds.Add(skill.Id);
            }
        }
        
        if (exportData.Personnel != null)
        {
            foreach (var personnel in exportData.Personnel)
            {
                validPersonnelIds.Add(personnel.Id);
            }
        }
        
        if (exportData.Positions != null)
        {
            foreach (var position in exportData.Positions)
            {
                validPositionIds.Add(position.Id);
            }
        }
        
        if (exportData.HolidayConfigs != null)
        {
            foreach (var config in exportData.HolidayConfigs)
            {
                validHolidayConfigIds.Add(config.Id);
            }
        }
        
        // 验证 Personnel.SkillIds 引用
        if (exportData.Personnel != null)
        {
            for (int i = 0; i < exportData.Personnel.Count; i++)
            {
                var personnel = exportData.Personnel[i];
                
                if (personnel.SkillIds != null)
                {
                    foreach (var skillId in personnel.SkillIds)
                    {
                        if (!validSkillIds.Contains(skillId))
                        {
                            result.IsValid = false;
                            result.Errors.Add(new ValidationError
                            {
                                Table = "Personnel",
                                RecordId = personnel.Id,
                                Field = "SkillIds",
                                Message = $"人员 {personnel.Name} 引用了不存在的技能ID: {skillId} (记录索引: {i})",
                                Type = ValidationErrorType.BrokenReference
                            });
                        }
                    }
                }
            }
        }
        
        // 验证 Position.RequiredSkillIds 引用
        if (exportData.Positions != null)
        {
            for (int i = 0; i < exportData.Positions.Count; i++)
            {
                var position = exportData.Positions[i];
                
                if (position.RequiredSkillIds != null)
                {
                    foreach (var skillId in position.RequiredSkillIds)
                    {
                        if (!validSkillIds.Contains(skillId))
                        {
                            result.IsValid = false;
                            result.Errors.Add(new ValidationError
                            {
                                Table = "Positions",
                                RecordId = position.Id,
                                Field = "RequiredSkillIds",
                                Message = $"哨位 {position.Name} 引用了不存在的技能ID: {skillId} (记录索引: {i})",
                                Type = ValidationErrorType.BrokenReference
                            });
                        }
                    }
                }
                
                // 验证 Position.AvailablePersonnelIds 引用
                if (position.AvailablePersonnelIds != null)
                {
                    foreach (var personnelId in position.AvailablePersonnelIds)
                    {
                        if (!validPersonnelIds.Contains(personnelId))
                        {
                            result.IsValid = false;
                            result.Errors.Add(new ValidationError
                            {
                                Table = "Positions",
                                RecordId = position.Id,
                                Field = "AvailablePersonnelIds",
                                Message = $"哨位 {position.Name} 引用了不存在的人员ID: {personnelId} (记录索引: {i})",
                                Type = ValidationErrorType.BrokenReference
                            });
                        }
                    }
                }
            }
        }
        
        // 验证 Template 中的引用
        if (exportData.Templates != null)
        {
            for (int i = 0; i < exportData.Templates.Count; i++)
            {
                var template = exportData.Templates[i];
                
                // 验证 PersonnelIds
                if (template.PersonnelIds != null)
                {
                    foreach (var personnelId in template.PersonnelIds)
                    {
                        if (!validPersonnelIds.Contains(personnelId))
                        {
                            result.Warnings.Add(new ValidationWarning
                            {
                                Table = "Templates",
                                Message = $"模板 {template.Name} 引用了不存在的人员ID: {personnelId} (记录索引: {i})"
                            });
                        }
                    }
                }
                
                // 验证 PositionIds
                if (template.PositionIds != null)
                {
                    foreach (var positionId in template.PositionIds)
                    {
                        if (!validPositionIds.Contains(positionId))
                        {
                            result.Warnings.Add(new ValidationWarning
                            {
                                Table = "Templates",
                                Message = $"模板 {template.Name} 引用了不存在的哨位ID: {positionId} (记录索引: {i})"
                            });
                        }
                    }
                }
                
                // 验证 HolidayConfigId (nullable int)
                if (template.HolidayConfigId.HasValue && template.HolidayConfigId.Value > 0)
                {
                    if (!validHolidayConfigIds.Contains(template.HolidayConfigId.Value))
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            Table = "Templates",
                            Message = $"模板 {template.Name} 引用了不存在的节假日配置ID: {template.HolidayConfigId.Value} (记录索引: {i})"
                        });
                    }
                }
            }
        }
        
        // 验证 FixedAssignments 中的引用
        if (exportData.FixedAssignments != null)
        {
            for (int i = 0; i < exportData.FixedAssignments.Count; i++)
            {
                var assignment = exportData.FixedAssignments[i];
                
                // 验证 PersonnelId (int type, not nullable)
                if (assignment.PersonnelId > 0 && !validPersonnelIds.Contains(assignment.PersonnelId))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "FixedAssignments",
                        RecordId = assignment.Id,
                        Field = "PersonnelId",
                        Message = $"固定分配引用了不存在的人员ID: {assignment.PersonnelId} (记录索引: {i})",
                        Type = ValidationErrorType.BrokenReference
                    });
                }
                
                // 验证 AllowedPositionIds
                if (assignment.AllowedPositionIds != null)
                {
                    foreach (var positionId in assignment.AllowedPositionIds)
                    {
                        if (!validPositionIds.Contains(positionId))
                        {
                            result.IsValid = false;
                            result.Errors.Add(new ValidationError
                            {
                                Table = "FixedAssignments",
                                RecordId = assignment.Id,
                                Field = "AllowedPositionIds",
                                Message = $"固定分配引用了不存在的哨位ID: {positionId} (记录索引: {i})",
                                Type = ValidationErrorType.BrokenReference
                            });
                        }
                    }
                }
            }
        }
        
        // 验证 ManualAssignments 中的引用
        if (exportData.ManualAssignments != null)
        {
            for (int i = 0; i < exportData.ManualAssignments.Count; i++)
            {
                var assignment = exportData.ManualAssignments[i];
                
                // 验证 PersonnelId (int type, not nullable)
                if (assignment.PersonnelId > 0 && !validPersonnelIds.Contains(assignment.PersonnelId))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "ManualAssignments",
                        RecordId = assignment.Id,
                        Field = "PersonnelId",
                        Message = $"手动分配引用了不存在的人员ID: {assignment.PersonnelId} (记录索引: {i})",
                        Type = ValidationErrorType.BrokenReference
                    });
                }
                
                // 验证 PositionId
                if (!validPositionIds.Contains(assignment.PositionId))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Table = "ManualAssignments",
                        RecordId = assignment.Id,
                        Field = "PositionId",
                        Message = $"手动分配引用了不存在的哨位ID: {assignment.PositionId} (记录索引: {i})",
                        Type = ValidationErrorType.BrokenReference
                    });
                }
            }
        }
        
        await Task.CompletedTask;
    }

    #endregion

    #region Helper Methods for Import

    /// <summary>
    /// 导入技能数据
    /// </summary>
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
    /// 导入人员数据
    /// </summary>
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

    /// <summary>
    /// 将 SkillDto 映射到 Skill 模型
    /// </summary>
    private Models.Skill MapToSkill(SkillDto dto)
    {
        return new Models.Skill
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// 将 PersonnelDto 映射到 Personal 模型
    /// </summary>
    private Models.Personal MapToPersonnel(PersonnelDto dto)
    {
        return new Models.Personal
        {
            Id = dto.Id,
            Name = dto.Name,
            SkillIds = dto.SkillIds,
            IsAvailable = dto.IsAvailable,
            IsRetired = dto.IsRetired,
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = dto.RecentPeriodShiftIntervals
        };
    }

    /// <summary>
    /// 将 PositionDto 映射到 PositionLocation 模型
    /// </summary>
    private Models.PositionLocation MapToPosition(PositionDto dto)
    {
        return new Models.PositionLocation
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            Description = dto.Description,
            Requirements = dto.Requirements,
            RequiredSkillIds = dto.RequiredSkillIds,
            AvailablePersonnelIds = dto.AvailablePersonnelIds,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// 将 HolidayConfigDto 映射到 HolidayConfig 模型
    /// </summary>
    private Models.Constraints.HolidayConfig MapToHolidayConfig(HolidayConfigDto dto)
    {
        return new Models.Constraints.HolidayConfig
        {
            Id = dto.Id,
            ConfigName = dto.ConfigName,
            EnableWeekendRule = dto.EnableWeekendRule,
            WeekendDays = dto.WeekendDays,
            LegalHolidays = dto.LegalHolidays,
            CustomHolidays = dto.CustomHolidays,
            ExcludedDates = dto.ExcludedDates,
            IsActive = dto.IsActive
        };
    }

    /// <summary>
    /// 将 SchedulingTemplateDto 映射到 SchedulingTemplate 模型
    /// </summary>
    private Models.SchedulingTemplate MapToTemplate(SchedulingTemplateDto dto)
    {
        return new Models.SchedulingTemplate
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            TemplateType = dto.TemplateType,
            IsDefault = dto.IsDefault,
            PersonnelIds = dto.PersonnelIds,
            PositionIds = dto.PositionIds,
            HolidayConfigId = dto.HolidayConfigId,
            UseActiveHolidayConfig = dto.UseActiveHolidayConfig,
            EnabledFixedRuleIds = dto.EnabledFixedRuleIds,
            EnabledManualAssignmentIds = dto.EnabledManualAssignmentIds,
            DurationDays = dto.DurationDays,
            StrategyConfig = dto.StrategyConfig,
            UsageCount = dto.UsageCount,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            LastUsedAt = dto.LastUsedAt
        };
    }

    /// <summary>
    /// 将 FixedAssignmentDto 映射到 FixedPositionRule 模型
    /// </summary>
    private Models.Constraints.FixedPositionRule MapToFixedPositionRule(FixedAssignmentDto dto)
    {
        return new Models.Constraints.FixedPositionRule
        {
            Id = dto.Id,
            PersonalId = dto.PersonnelId,
            AllowedPositionIds = dto.AllowedPositionIds,
            AllowedPeriods = dto.AllowedTimeSlots,
            IsEnabled = dto.IsEnabled,
            Description = dto.Description
        };
    }

    /// <summary>
    /// 将 ManualAssignmentDto 映射到 ManualAssignment 模型
    /// </summary>
    private Models.Constraints.ManualAssignment MapToManualAssignment(ManualAssignmentDto dto)
    {
        return new Models.Constraints.ManualAssignment
        {
            Id = dto.Id,
            PositionId = dto.PositionId,
            PeriodIndex = dto.TimeSlot,
            PersonalId = dto.PersonnelId,
            Date = dto.Date,
            IsEnabled = dto.IsEnabled,
            Remarks = dto.Remarks
        };
    }

    #endregion
}
