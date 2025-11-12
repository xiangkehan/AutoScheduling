using AutoScheduling3.DTOs.ImportExport;
using System;

namespace AutoScheduling3.TestData.Helpers
{
    /// <summary>
    /// 导出元数据构建器
    /// </summary>
    public class ExportMetadataBuilder
    {
        /// <summary>
        /// 创建导出元数据
        /// </summary>
        /// <param name="data">导出数据</param>
        /// <returns>元数据对象</returns>
        public ExportMetadata Build(ExportData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return new ExportMetadata
            {
                ExportVersion = "1.0",
                ExportedAt = DateTime.UtcNow,
                DatabaseVersion = 1,
                ApplicationVersion = "1.0.0.0",
                Statistics = new DataStatistics
                {
                    SkillCount = data.Skills?.Count ?? 0,
                    PersonnelCount = data.Personnel?.Count ?? 0,
                    PositionCount = data.Positions?.Count ?? 0,
                    TemplateCount = data.Templates?.Count ?? 0,
                    ConstraintCount = (data.FixedAssignments?.Count ?? 0) +
                                    (data.ManualAssignments?.Count ?? 0) +
                                    (data.HolidayConfigs?.Count ?? 0)
                }
            };
        }
    }
}
