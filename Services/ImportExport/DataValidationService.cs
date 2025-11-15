using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.Services.ImportExport
{
    /// <summary>
    /// 数据验证服务实现
    /// 负责所有数据验证逻辑
    /// </summary>
    public class DataValidationService : IDataValidationService
    {
        private readonly ILogger _logger;
        private readonly OperationAuditLogger _auditLogger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// 初始化数据验证服务
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public DataValidationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLogger = new OperationAuditLogger();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
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
                    exportData = JsonSerializer.Deserialize<ExportData>(json, _jsonOptions);
                    
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

        /// <summary>
        /// 验证必需字段存在性和数据类型
        /// </summary>
        public void ValidateRequiredFields(ExportData exportData, ValidationResult result)
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
        public void ValidateDataConstraints(ExportData exportData, ValidationResult result)
        {
            // 验证 Skills 约束
            if (exportData.Skills != null)
            {
                var skillIds = new HashSet<int>();
                var skillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicateNames = new List<string>();
                
                for (int i = 0; i < exportData.Skills.Count; i++)
                {
                    var skill = exportData.Skills[i];
                    
                    // 验证主键重复
                    if (skillIds.Contains(skill.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "Skills",
                            RecordId = skill.Id,
                            Field = "Id",
                            Message = $"技能主键ID重复: {skill.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        skillIds.Add(skill.Id);
                    }
                    
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
                        
                        // 检测重复名称并记录警告
                        if (skillNames.Contains(skill.Name))
                        {
                            if (!duplicateNames.Contains(skill.Name))
                            {
                                duplicateNames.Add(skill.Name);
                            }
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
                
                // 记录重复名称警告（不阻止导入）
                foreach (var duplicateName in duplicateNames)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Table = "Skills",
                        Message = $"检测到重复的技能名称: {duplicateName}。记录将通过主键ID进行匹配。"
                    });
                }
            }
            
            // 验证 Personnel 约束
            if (exportData.Personnel != null)
            {
                var personnelIds = new HashSet<int>();
                var personnelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicatePersonnelNames = new List<string>();
                
                for (int i = 0; i < exportData.Personnel.Count; i++)
                {
                    var personnel = exportData.Personnel[i];
                    
                    // 验证主键重复
                    if (personnelIds.Contains(personnel.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "Personnel",
                            RecordId = personnel.Id,
                            Field = "Id",
                            Message = $"人员主键ID重复: {personnel.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        personnelIds.Add(personnel.Id);
                    }
                    
                    // 验证名称长度
                    if (!string.IsNullOrWhiteSpace(personnel.Name))
                    {
                        if (personnel.Name.Length > 100)
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
                        
                        // 检测重复名称并记录警告
                        if (personnelNames.Contains(personnel.Name))
                        {
                            if (!duplicatePersonnelNames.Contains(personnel.Name))
                            {
                                duplicatePersonnelNames.Add(personnel.Name);
                            }
                        }
                        else
                        {
                            personnelNames.Add(personnel.Name);
                        }
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
                
                // 记录重复名称警告（不阻止导入）
                foreach (var duplicateName in duplicatePersonnelNames)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Table = "Personnel",
                        Message = $"检测到重复的人员名称: {duplicateName}。记录将通过主键ID进行匹配。"
                    });
                }
            }
            
            // 验证 Positions 约束
            if (exportData.Positions != null)
            {
                var positionIds = new HashSet<int>();
                var positionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicatePositionNames = new List<string>();
                
                for (int i = 0; i < exportData.Positions.Count; i++)
                {
                    var position = exportData.Positions[i];
                    
                    // 验证主键重复
                    if (positionIds.Contains(position.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "Positions",
                            RecordId = position.Id,
                            Field = "Id",
                            Message = $"哨位主键ID重复: {position.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        positionIds.Add(position.Id);
                    }
                    
                    // 验证名称长度
                    if (!string.IsNullOrWhiteSpace(position.Name))
                    {
                        if (position.Name.Length > 100)
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
                        
                        // 检测重复名称并记录警告
                        if (positionNames.Contains(position.Name))
                        {
                            if (!duplicatePositionNames.Contains(position.Name))
                            {
                                duplicatePositionNames.Add(position.Name);
                            }
                        }
                        else
                        {
                            positionNames.Add(position.Name);
                        }
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
                
                // 记录重复名称警告（不阻止导入）
                foreach (var duplicateName in duplicatePositionNames)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Table = "Positions",
                        Message = $"检测到重复的哨位名称: {duplicateName}。记录将通过主键ID进行匹配。"
                    });
                }
            }
            
            // 验证 Templates 约束
            if (exportData.Templates != null)
            {
                var templateIds = new HashSet<int>();
                var templateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicateTemplateNames = new List<string>();
                
                for (int i = 0; i < exportData.Templates.Count; i++)
                {
                    var template = exportData.Templates[i];
                    
                    // 验证主键重复
                    if (templateIds.Contains(template.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "Templates",
                            RecordId = template.Id,
                            Field = "Id",
                            Message = $"模板主键ID重复: {template.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        templateIds.Add(template.Id);
                    }
                    
                    // 验证名称长度
                    if (!string.IsNullOrWhiteSpace(template.Name))
                    {
                        if (template.Name.Length > 100)
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
                        
                        // 检测重复名称并记录警告
                        if (templateNames.Contains(template.Name))
                        {
                            if (!duplicateTemplateNames.Contains(template.Name))
                            {
                                duplicateTemplateNames.Add(template.Name);
                            }
                        }
                        else
                        {
                            templateNames.Add(template.Name);
                        }
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
                
                // 记录重复名称警告（不阻止导入）
                foreach (var duplicateName in duplicateTemplateNames)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Table = "Templates",
                        Message = $"检测到重复的模板名称: {duplicateName}。记录将通过主键ID进行匹配。"
                    });
                }
            }
            
            // 验证 FixedAssignments 约束
            if (exportData.FixedAssignments != null)
            {
                var fixedAssignmentIds = new HashSet<int>();
                
                for (int i = 0; i < exportData.FixedAssignments.Count; i++)
                {
                    var assignment = exportData.FixedAssignments[i];
                    
                    // 验证主键重复
                    if (fixedAssignmentIds.Contains(assignment.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "FixedAssignments",
                            RecordId = assignment.Id,
                            Field = "Id",
                            Message = $"固定分配主键ID重复: {assignment.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        fixedAssignmentIds.Add(assignment.Id);
                    }
                }
            }
            
            // 验证 ManualAssignments 约束
            if (exportData.ManualAssignments != null)
            {
                var manualAssignmentIds = new HashSet<int>();
                
                for (int i = 0; i < exportData.ManualAssignments.Count; i++)
                {
                    var assignment = exportData.ManualAssignments[i];
                    
                    // 验证主键重复
                    if (manualAssignmentIds.Contains(assignment.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "ManualAssignments",
                            RecordId = assignment.Id,
                            Field = "Id",
                            Message = $"手动分配主键ID重复: {assignment.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        manualAssignmentIds.Add(assignment.Id);
                    }
                }
            }
            
            // 验证 HolidayConfigs 约束
            if (exportData.HolidayConfigs != null)
            {
                var holidayConfigIds = new HashSet<int>();
                var holidayConfigNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicateHolidayConfigNames = new List<string>();
                
                for (int i = 0; i < exportData.HolidayConfigs.Count; i++)
                {
                    var config = exportData.HolidayConfigs[i];
                    
                    // 验证主键重复
                    if (holidayConfigIds.Contains(config.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Table = "HolidayConfigs",
                            RecordId = config.Id,
                            Field = "Id",
                            Message = $"节假日配置主键ID重复: {config.Id} (记录索引: {i})",
                            Type = ValidationErrorType.DuplicateKey
                        });
                    }
                    else
                    {
                        holidayConfigIds.Add(config.Id);
                    }
                    
                    // 检测重复名称并记录警告
                    if (!string.IsNullOrWhiteSpace(config.ConfigName))
                    {
                        if (holidayConfigNames.Contains(config.ConfigName))
                        {
                            if (!duplicateHolidayConfigNames.Contains(config.ConfigName))
                            {
                                duplicateHolidayConfigNames.Add(config.ConfigName);
                            }
                        }
                        else
                        {
                            holidayConfigNames.Add(config.ConfigName);
                        }
                    }
                }
                
                // 记录重复名称警告（不阻止导入）
                foreach (var duplicateName in duplicateHolidayConfigNames)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Table = "HolidayConfigs",
                        Message = $"检测到重复的节假日配置名称: {duplicateName}。记录将通过主键ID进行匹配。"
                    });
                }
            }
        }

        /// <summary>
        /// 验证外键引用完整性
        /// </summary>
        public async Task ValidateForeignKeyReferences(ExportData exportData, ValidationResult result)
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
    }
}
