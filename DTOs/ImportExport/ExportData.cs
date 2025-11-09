using System.Collections.Generic;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导出数据的根对象，包含所有核心数据和元数据
    /// </summary>
    public class ExportData
    {
        public ExportMetadata Metadata { get; set; }
        public List<SkillDto> Skills { get; set; }
        public List<PersonnelDto> Personnel { get; set; }
        public List<PositionDto> Positions { get; set; }
        public List<SchedulingTemplateDto> Templates { get; set; }
        public List<FixedAssignmentDto> FixedAssignments { get; set; }
        public List<ManualAssignmentDto> ManualAssignments { get; set; }
        public List<HolidayConfigDto> HolidayConfigs { get; set; }
    }
}
