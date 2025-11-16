using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using System.IO;
using AutoScheduling3.Models;

namespace AutoScheduling3.Services;

/// <summary>
/// 排班模板服务实现
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ISchedulingService _schedulingService;
    private readonly TemplateMapper _mapper;

    public TemplateService(
        ITemplateRepository templateRepository,
        IPersonalRepository personnelRepository,
        IPositionRepository positionRepository,
        ISchedulingService schedulingService,
        TemplateMapper mapper)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    public async Task<List<SchedulingTemplateDto>> GetAllAsync()
    {
        var templates = await _templateRepository.GetAllAsync();
        return _mapper.ToDtoList(templates);
    }

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    public async Task<SchedulingTemplateDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的模板ID", nameof(id));

        var template = await _templateRepository.GetByIdAsync(id);
        return template != null ? _mapper.ToDto(template) : null;
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    public async Task<SchedulingTemplateDto> CreateAsync(CreateTemplateDto dto)
    {
        await ValidateCreateDtoAsync(dto);

        // 名称唯一校验
        var nameExists = await _templateRepository.ExistsByNameAsync(dto.Name, null);
        if (nameExists) throw new ArgumentException("模板名称已存在，请使用其他名称", nameof(dto.Name));

        // 默认模板唯一（同类型只能一个）
        if (dto.IsDefault)
        {
            await _templateRepository.ClearDefaultForTypeAsync(dto.TemplateType);
        }

        var model = _mapper.ToModel(dto);
        var id = await _templateRepository.CreateAsync(model);

        model.Id = id;
        return _mapper.ToDto(model);
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    public async Task UpdateAsync(int id, UpdateTemplateDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("无效的模板ID", nameof(id));

        await ValidateUpdateDtoAsync(dto);

        var existingTemplate = await _templateRepository.GetByIdAsync(id);
        if (existingTemplate == null)
            throw new ArgumentException($"模板 ID {id} 不存在", nameof(id));

        // 名称唯一（排除自身）
        var nameExists = await _templateRepository.ExistsByNameAsync(dto.Name, id);
        if (nameExists) throw new ArgumentException("模板名称已存在，请使用其他名称", nameof(dto.Name));

        // 默认模板唯一
        if (dto.IsDefault && !existingTemplate.IsDefault)
        {
            await _templateRepository.ClearDefaultForTypeAsync(dto.TemplateType);
        }

        _mapper.UpdateModel(existingTemplate, dto);
        await _templateRepository.UpdateAsync(existingTemplate);
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的模板ID", nameof(id));

        var exists = await _templateRepository.ExistsAsync(id);
        if (!exists)
            throw new ArgumentException($"模板 ID {id} 不存在", nameof(id));

        await _templateRepository.DeleteAsync(id);
    }

    /// <summary>
    /// 获取默认模板
    /// </summary>
    public async Task<SchedulingTemplateDto?> GetDefaultAsync()
    {
        var template = await _templateRepository.GetDefaultAsync();
        return template != null ? _mapper.ToDto(template) : null;
    }

    /// <summary>
    /// 根据类型获取模板
    /// </summary>
    public async Task<List<SchedulingTemplateDto>> GetByTypeAsync(string templateType)
    {
        if (string.IsNullOrWhiteSpace(templateType))
            throw new ArgumentException("模板类型不能为空", nameof(templateType));

        var validTypes = new[] { "regular", "holiday", "special" };
        if (!validTypes.Contains(templateType.ToLower()))
            throw new ArgumentException($"无效的模板类型。有效值：{string.Join(", ", validTypes)}", nameof(templateType));

        var templates = await _templateRepository.GetByTypeAsync(templateType);
        return _mapper.ToDtoList(templates);
    }

    /// <summary>
    /// 验证模板配置
    /// </summary>
    public async Task<TemplateValidationResult> ValidateAsync(int templateId)
    {
        var result = new TemplateValidationResult { IsValid = true };

        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationMessage { Message = "模板不存在" });
            return result;
        }

        // 验证人员是否存在
        foreach (var personnelId in template.PersonnelIds)
        {
            var exists = await _personnelRepository.ExistsAsync(personnelId);
            if (!exists)
            {
                // This is a warning, not a fatal error. The template is still "valid" but has issues.
                result.Warnings.Add(new ValidationMessage { Message = $"参与人员 ID {personnelId} 已不存在或被删除。", ResourceId = personnelId });
            }
        }

        // 验证哨位是否存在
        foreach (var positionId in template.PositionIds)
        {
            var exists = await _positionRepository.ExistsAsync(positionId);
            if (!exists)
            {
                // This is a warning, not a fatal error.
                result.Warnings.Add(new ValidationMessage { Message = $"参与哨位 ID {positionId} 已不存在或被删除。", ResourceId = positionId });
            }
        }

        if (template.PersonnelIds.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationMessage { Message = "模板必须包含至少一名人员" });
        }

        if (template.PositionIds.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationMessage { Message = "模板必须包含至少一个哨位" });
        }

        // The template is only truly invalid if it has critical errors.
        // The presence of warnings does not make it invalid.
        if (result.Errors.Any())
        {
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// 使用模板创建排班
    /// </summary>
    public async Task<ScheduleDto> UseTemplateAsync(UseTemplateDto dto)
    {
        ValidateUseTemplateDto(dto);

        var template = await _templateRepository.GetByIdAsync(dto.TemplateId);
        if (template == null)
            throw new ArgumentException($"模板 ID {dto.TemplateId} 不存在", nameof(dto.TemplateId));

        // 验证模板配置
        var validationResult = await ValidateAsync(dto.TemplateId);
        if (!validationResult.IsValid)
        {
            // We throw an exception only for fatal errors. Warnings are allowed.
            var errorMessages = validationResult.Errors.Select(e => e.Message);
            throw new InvalidOperationException($"模板验证失败: {string.Join("; ", errorMessages)}");
        }

        // 构建排班请求
        var schedulingRequest = new SchedulingRequestDto
        {
            Title = dto.Title,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            PersonnelIds = dto.OverridePersonnelIds ?? new List<int>(template.PersonnelIds),
            PositionIds = dto.OverridePositionIds ?? new List<int>(template.PositionIds),
            HolidayConfigId = template.HolidayConfigId,
            UseActiveHolidayConfig = template.UseActiveHolidayConfig,
            EnabledFixedRuleIds = new List<int>(template.EnabledFixedRuleIds),
            EnabledManualAssignmentIds = new List<int>(template.EnabledManualAssignmentIds)
        };

        // 执行排班
        var result = await _schedulingService.ExecuteSchedulingAsync(schedulingRequest, null, System.Threading.CancellationToken.None);
        if (!result.IsSuccess || result.Schedule == null)
        {
            throw new InvalidOperationException($"使用模板排班失败: {result.ErrorMessage}");
        }

        // 更新模板使用记录
        await _templateRepository.UpdateUsageAsync(dto.TemplateId);

        return result.Schedule;
    }

    /// <summary>
    /// 导出排班
    /// </summary>
    public async Task<byte[]> ExportScheduleAsync(int id, string format)
    {
        // This method should now delegate to the SchedulingService
        return await _schedulingService.ExportScheduleAsync(id, format);
    }

    private async Task<byte[]> GenerateCsvAsync(ScheduleDto schedule)
    {
        // This method is likely obsolete as the logic is now in SchedulingService/GridExporter
        // But to fix compilation, I'll adapt it to the new DTO.
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Schedule");

            // Add header row
            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Time";
            worksheet.Cells[1, 3].Value = "Position";
            worksheet.Cells[1, 4].Value = "Personnel";
            worksheet.Cells[1, 5].Value = "Is Manual";
            worksheet.Cells[1, 6].Value = "Has Conflict";


            // Add data rows
            var row = 2;
            foreach (var shift in schedule.Shifts.OrderBy(s => s.StartTime).ThenBy(s => s.PositionName))
            {
                worksheet.Cells[row, 1].Value = shift.StartTime.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = shift.StartTime.ToString("HH:mm") + "-" + shift.EndTime.ToString("HH:mm");
                worksheet.Cells[row, 3].Value = shift.PositionName;
                worksheet.Cells[row, 4].Value = shift.PersonnelName;
                
                // These properties are not directly on the ShiftDto, they are on the GridCell.
                // This method is ill-suited for the new model. The call to ExportScheduleAsync is the correct path.
                // I will leave these blank to fix compilation.
                worksheet.Cells[row, 5].Value = ""; 
                worksheet.Cells[row, 6].Value = "";
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }
    }

    /// <summary>
    /// 验证创建 DTO
    /// </summary>
    private async Task ValidateCreateDtoAsync(CreateTemplateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("模板名称为必填项", nameof(dto.Name));

        if (dto.Name.Length > 100)
            throw new ArgumentException("模板名称不能超过100个字符", nameof(dto.Name));

        if (dto.Description != null && dto.Description.Length > 500)
            throw new ArgumentException("模板描述不能超过500个字符", nameof(dto.Description));

        var validTypes = new[] { "regular", "holiday", "special" };
        if (!validTypes.Contains(dto.TemplateType.ToLower()))
            throw new ArgumentException($"无效的模板类型。有效值：{string.Join(", ", validTypes)}", nameof(dto.TemplateType));

        if (dto.PersonnelIds == null || dto.PersonnelIds.Count == 0)
            throw new ArgumentException("模板必须包含至少一名人员", nameof(dto.PersonnelIds));

        if (dto.PositionIds == null || dto.PositionIds.Count == 0)
            throw new ArgumentException("模板必须包含至少一个哨位", nameof(dto.PositionIds));

        // 验证人员ID是否存在
        foreach (var personnelId in dto.PersonnelIds)
        {
            var exists = await _personnelRepository.ExistsAsync(personnelId);
            if (!exists)
                throw new ArgumentException($"人员 ID {personnelId} 不存在", nameof(dto.PersonnelIds));
        }

        // 验证哨位ID是否存在
        foreach (var positionId in dto.PositionIds)
        {
            var exists = await _positionRepository.ExistsAsync(positionId);
            if (!exists)
                throw new ArgumentException($"哨位 ID {positionId} 不存在", nameof(dto.PositionIds));
        }
    }

    /// <summary>
    /// 验证更新 DTO
    /// </summary>
    private async Task ValidateUpdateDtoAsync(UpdateTemplateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("模板名称为必填项", nameof(dto.Name));

        if (dto.Name.Length > 100)
            throw new ArgumentException("模板名称不能超过100个字符", nameof(dto.Name));

        if (dto.Description != null && dto.Description.Length > 500)
            throw new ArgumentException("模板描述不能超过500个字符", nameof(dto.Description));

        var validTypes = new[] { "regular", "holiday", "special" };
        if (!validTypes.Contains(dto.TemplateType.ToLower()))
            throw new ArgumentException($"无效的模板类型。有效值：{string.Join(", ", validTypes)}", nameof(dto.TemplateType));

        if (dto.PersonnelIds == null || dto.PersonnelIds.Count == 0)
            throw new ArgumentException("模板必须包含至少一名人员", nameof(dto.PersonnelIds));

        if (dto.PositionIds == null || dto.PositionIds.Count == 0)
            throw new ArgumentException("模板必须包含至少一个哨位", nameof(dto.PositionIds));

        // 验证人员ID是否存在
        foreach (var personnelId in dto.PersonnelIds)
        {
            var exists = await _personnelRepository.ExistsAsync(personnelId);
            if (!exists)
                throw new ArgumentException($"人员 ID {personnelId} 不存在", nameof(dto.PersonnelIds));
        }

        // 验证哨位ID是否存在
        foreach (var positionId in dto.PositionIds)
        {
            var exists = await _positionRepository.ExistsAsync(positionId);
            if (!exists)
                throw new ArgumentException($"哨位 ID {positionId} 不存在", nameof(dto.PositionIds));
        }
    }

    /// <summary>
    /// 验证使用模板 DTO
    /// </summary>
    private void ValidateUseTemplateDto(UseTemplateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.TemplateId <= 0)
            throw new ArgumentException("无效的模板ID", nameof(dto.TemplateId));

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("排班表名称为必填项", nameof(dto.Title));

        if (dto.Title.Length > 100)
            throw new ArgumentException("排班表名称不能超过100个字符", nameof(dto.Title));

        if (dto.StartDate < DateTime.Today)
            throw new ArgumentException("开始日期不能早于今天", nameof(dto.StartDate));

        if (dto.EndDate < dto.StartDate)
            throw new ArgumentException("结束日期不能早于开始日期", nameof(dto.EndDate));

        var daysDiff = (dto.EndDate - dto.StartDate).Days + 1;
        if (daysDiff > 365)
            throw new ArgumentException("排班周期不能超过365天", nameof(dto.EndDate));

        if (dto.OverridePersonnelIds != null && dto.OverridePersonnelIds.Count == 0)
            throw new ArgumentException("覆盖人员列表不能为空，请传入null使用模板配置", nameof(dto.OverridePersonnelIds));

        if (dto.OverridePositionIds != null && dto.OverridePositionIds.Count == 0)
            throw new ArgumentException("覆盖哨位列表不能为空，请传入null使用模板配置", nameof(dto.OverridePositionIds));
    }
}
