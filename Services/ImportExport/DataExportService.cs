using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.Services.ImportExport
{
    /// <summary>
    /// 数据导出服务实现
    /// 负责所有数据导出逻辑
    /// </summary>
    public class DataExportService : IDataExportService
    {
        private readonly IPersonalRepository _personnelRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly ISkillRepository _skillRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IConstraintRepository _constraintRepository;
        private readonly ILogger _logger;

        /// <summary>
        /// 初始化数据导出服务
        /// </summary>
        /// <param name="personnelRepository">人员仓储</param>
        /// <param name="positionRepository">哨位仓储</param>
        /// <param name="skillRepository">技能仓储</param>
        /// <param name="templateRepository">模板仓储</param>
        /// <param name="constraintRepository">约束仓储</param>
        /// <param name="logger">日志记录器</param>
        public DataExportService(
            IPersonalRepository personnelRepository,
            IPositionRepository positionRepository,
            ISkillRepository skillRepository,
            ITemplateRepository templateRepository,
            IConstraintRepository constraintRepository,
            ILogger logger)
        {
            _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
            _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _constraintRepository = constraintRepository ?? throw new ArgumentNullException(nameof(constraintRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 导出技能数据
        /// </summary>
        public async Task<List<SkillDto>> ExportSkillsAsync()
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
        public async Task<List<PersonnelDto>> ExportPersonnelAsync()
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
        public async Task<List<PositionDto>> ExportPositionsAsync()
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
        public async Task<List<SchedulingTemplateDto>> ExportTemplatesAsync()
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
        public async Task<List<FixedAssignmentDto>> ExportFixedAssignmentsAsync()
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
        public async Task<List<ManualAssignmentDto>> ExportManualAssignmentsAsync()
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
        public async Task<List<HolidayConfigDto>> ExportHolidayConfigsAsync()
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

        /// <summary>
        /// 计算数据统计信息
        /// </summary>
        public DataStatistics CalculateStatistics(ExportData exportData)
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
        /// 获取数据库版本
        /// </summary>
        public async Task<int> GetDatabaseVersionAsync()
        {
            try
            {
                // 使用 SkillRepository 的连接字符串来访问数据库
                // 由于我们没有直接访问连接字符串，我们将使用一个合理的默认值
                // 在实际实现中，这应该从配置或数据库服务获取
                return await Task.FromResult(1); // 当前数据库版本
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
        public string GetApplicationVersion()
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
    }
}
