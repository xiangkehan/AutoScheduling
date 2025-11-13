using System;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导出文件的元数据信息
    /// </summary>
    public class ExportMetadata
    {
        public string ExportVersion { get; set; }
        public DateTime ExportedAt { get; set; }
        public int DatabaseVersion { get; set; }
        public string ApplicationVersion { get; set; }
        public DataStatistics Statistics { get; set; }

        // 添加 PositionCount 属性以修复 CS1061
        public int PositionCount
        {
            get => Statistics?.PositionCount ?? 0;
        }

        // 如果还需要 SkillCount 和 PersonnelCount，可一并添加
        public int SkillCount
        {
            get => Statistics?.SkillCount ?? 0;
        }

        public int PersonnelCount
        {
            get => Statistics?.PersonnelCount ?? 0;
        }
    }
}
