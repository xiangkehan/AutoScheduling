using System.Collections.Generic;

namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 人员变更数据传输对象
    /// </summary>
    public class PersonnelChangeDto
    {
        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonnelId { get; set; }

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName { get; set; } = string.Empty;

        /// <summary>
        /// 在排班表1中的班次数
        /// </summary>
        public int ShiftsInSchedule1 { get; set; }

        /// <summary>
        /// 在排班表2中的班次数
        /// </summary>
        public int ShiftsInSchedule2 { get; set; }

        /// <summary>
        /// 新增班次数（只在排班表2中的班次）
        /// </summary>
        public int AddedShifts { get; set; }

        /// <summary>
        /// 减少班次数（只在排班表1中的班次）
        /// </summary>
        public int RemovedShifts { get; set; }

        /// <summary>
        /// 净变化（AddedShifts - RemovedShifts）
        /// </summary>
        public int NetChange { get; set; }

        /// <summary>
        /// 人员状态
        /// </summary>
        public PersonnelStatus Status { get; set; }

        /// <summary>
        /// 详细变更列表
        /// </summary>
        public List<ShiftDiffDto> DetailedChanges { get; set; } = new();
    }
}
