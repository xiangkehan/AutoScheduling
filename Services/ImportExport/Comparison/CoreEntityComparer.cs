using System;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;

namespace AutoScheduling3.Services.ImportExport.Comparison
{
    /// <summary>
    /// 核心实体比较器：比较技能、人员和哨位数据
    /// 用于判断导入数据与现有数据是否相同，避免不必要的更新操作
    /// </summary>
    public class CoreEntityComparer : DataComparerBase
    {
        /// <summary>
        /// 比较技能数据是否相等
        /// </summary>
        /// <param name="imported">导入的技能 DTO</param>
        /// <param name="existing">现有的技能模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool AreSkillsEqual(SkillDto imported, Skill existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较所有字段
            return AreStringsEqual(imported.Name, existing.Name) &&
                   AreStringsEqual(imported.Description, existing.Description) &&
                   imported.IsActive == existing.IsActive;
            // 注意：不比较 CreatedAt 和 UpdatedAt，因为这些是系统管理的时间戳
        }

        /// <summary>
        /// 比较人员数据是否相等
        /// </summary>
        /// <param name="imported">导入的人员 DTO</param>
        /// <param name="existing">现有的人员模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool ArePersonnelEqual(PersonnelDto imported, Personal existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较基本字段
            if (!AreStringsEqual(imported.Name, existing.Name))
                return false;

            if (imported.IsAvailable != existing.IsAvailable)
                return false;

            if (imported.IsRetired != existing.IsRetired)
                return false;

            if (imported.RecentShiftIntervalCount != existing.RecentShiftIntervalCount)
                return false;

            if (imported.RecentHolidayShiftIntervalCount != existing.RecentHolidayShiftIntervalCount)
                return false;

            // 比较技能 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.SkillIds, existing.SkillIds))
                return false;

            // 比较时段班次间隔数组
            if (!AreArraysEqual(imported.RecentPeriodShiftIntervals, existing.RecentPeriodShiftIntervals))
                return false;

            return true;
        }

        /// <summary>
        /// 比较哨位数据是否相等
        /// </summary>
        /// <param name="imported">导入的哨位 DTO</param>
        /// <param name="existing">现有的哨位模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool ArePositionsEqual(PositionDto imported, PositionLocation existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较基本字段
            if (!AreStringsEqual(imported.Name, existing.Name))
                return false;

            if (!AreStringsEqual(imported.Location, existing.Location))
                return false;

            if (!AreStringsEqual(imported.Description, existing.Description))
                return false;

            if (!AreStringsEqual(imported.Requirements, existing.Requirements))
                return false;

            if (imported.IsActive != existing.IsActive)
                return false;

            // 比较所需技能 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.RequiredSkillIds, existing.RequiredSkillIds))
                return false;

            // 比较可用人员 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.AvailablePersonnelIds, existing.AvailablePersonnelIds))
                return false;

            return true;
        }
    }
}
