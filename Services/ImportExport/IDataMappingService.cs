using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.ImportExport;

/// <summary>
/// 数据映射服务接口 - 负责DTO与模型之间的双向转换
/// </summary>
public interface IDataMappingService
{
    #region DTO to Model Mapping
    
    /// <summary>
    /// 将 SkillDto 映射到 Skill 模型
    /// </summary>
    AutoScheduling3.Models.Skill MapToSkill(SkillDto dto);
    
    /// <summary>
    /// 将 PersonnelDto 映射到 Personal 模型
    /// </summary>
    AutoScheduling3.Models.Personal MapToPersonnel(PersonnelDto dto);
    
    /// <summary>
    /// 将 PositionDto 映射到 PositionLocation 模型
    /// </summary>
    AutoScheduling3.Models.PositionLocation MapToPosition(PositionDto dto);
    
    /// <summary>
    /// 将 HolidayConfigDto 映射到 HolidayConfig 模型
    /// </summary>
    AutoScheduling3.Models.Constraints.HolidayConfig MapToHolidayConfig(HolidayConfigDto dto);
    
    /// <summary>
    /// 将 SchedulingTemplateDto 映射到 SchedulingTemplate 模型
    /// </summary>
    AutoScheduling3.Models.SchedulingTemplate MapToTemplate(SchedulingTemplateDto dto);
    
    /// <summary>
    /// 将 FixedAssignmentDto 映射到 FixedPositionRule 模型
    /// </summary>
    AutoScheduling3.Models.Constraints.FixedPositionRule MapToFixedPositionRule(FixedAssignmentDto dto);
    
    /// <summary>
    /// 将 ManualAssignmentDto 映射到 ManualAssignment 模型
    /// </summary>
    AutoScheduling3.Models.Constraints.ManualAssignment MapToManualAssignment(ManualAssignmentDto dto);
    
    #endregion
    
    #region Model to DTO Mapping
    
    /// <summary>
    /// 将 Skill 模型映射到 SkillDto
    /// </summary>
    SkillDto MapToSkillDto(AutoScheduling3.Models.Skill model);
    
    /// <summary>
    /// 将 Personal 模型映射到 PersonnelDto
    /// </summary>
    PersonnelDto MapToPersonnelDto(AutoScheduling3.Models.Personal model);
    
    /// <summary>
    /// 将 PositionLocation 模型映射到 PositionDto
    /// </summary>
    PositionDto MapToPositionDto(AutoScheduling3.Models.PositionLocation model);
    
    /// <summary>
    /// 将 HolidayConfig 模型映射到 HolidayConfigDto
    /// </summary>
    HolidayConfigDto MapToHolidayConfigDto(AutoScheduling3.Models.Constraints.HolidayConfig model);
    
    /// <summary>
    /// 将 SchedulingTemplate 模型映射到 SchedulingTemplateDto
    /// </summary>
    SchedulingTemplateDto MapToTemplateDto(AutoScheduling3.Models.SchedulingTemplate model);
    
    /// <summary>
    /// 将 FixedPositionRule 模型映射到 FixedAssignmentDto
    /// </summary>
    FixedAssignmentDto MapToFixedAssignmentDto(AutoScheduling3.Models.Constraints.FixedPositionRule model);
    
    /// <summary>
    /// 将 ManualAssignment 模型映射到 ManualAssignmentDto
    /// </summary>
    ManualAssignmentDto MapToManualAssignmentDto(AutoScheduling3.Models.Constraints.ManualAssignment model);
    
    #endregion
}
