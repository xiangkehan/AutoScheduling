using System;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.Services.ImportExport.Comparison
{
    /// <summary>
    /// 约束实体比较器：比较模板、定岗规则、手动分配和节假日配置数据
    /// 用于判断导入数据与现有数据是否相同，避免不必要的更新操作
    /// </summary>
    public class ConstraintComparer : DataComparerBase
    {
        /// <summary>
        /// 比较排班模板数据是否相等
        /// </summary>
        /// <param name="imported">导入的模板 DTO</param>
        /// <param name="existing">现有的模板模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool AreTemplatesEqual(SchedulingTemplateDto imported, SchedulingTemplate existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较基本字段
            if (!AreStringsEqual(imported.Name, existing.Name))
                return false;

            if (!AreStringsEqual(imported.Description, existing.Description))
                return false;

            if (!AreStringsEqual(imported.TemplateType, existing.TemplateType))
                return false;

            if (imported.IsDefault != existing.IsDefault)
                return false;

            if (imported.IsActive != existing.IsActive)
                return false;

            if (imported.DurationDays != existing.DurationDays)
                return false;

            if (!AreStringsEqual(imported.StrategyConfig, existing.StrategyConfig))
                return false;

            // 比较可空整数字段
            if (!AreNullableIntsEqual(imported.HolidayConfigId, existing.HolidayConfigId))
                return false;

            if (imported.UseActiveHolidayConfig != existing.UseActiveHolidayConfig)
                return false;

            // 比较人员 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.PersonnelIds, existing.PersonnelIds))
                return false;

            // 比较哨位 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.PositionIds, existing.PositionIds))
                return false;

            // 比较启用的定岗规则 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.EnabledFixedRuleIds, existing.EnabledFixedRuleIds))
                return false;

            // 比较启用的手动分配 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.EnabledManualAssignmentIds, existing.EnabledManualAssignmentIds))
                return false;

            // 注意：不比较 UsageCount, CreatedAt, UpdatedAt, LastUsedAt
            // 这些是系统管理的字段，不应该从导入文件中更新

            return true;
        }

        /// <summary>
        /// 比较定岗规则数据是否相等
        /// </summary>
        /// <param name="imported">导入的定岗规则 DTO</param>
        /// <param name="existing">现有的定岗规则模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool AreFixedAssignmentsEqual(FixedAssignmentDto imported, FixedPositionRule existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较人员 ID
            if (imported.PersonnelId != existing.PersonalId)
                return false;

            // 比较是否启用
            if (imported.IsEnabled != existing.IsEnabled)
                return false;

            // 比较规则名称（DTO 中的 RuleName 对应 Model 中的 Description）
            if (!AreStringsEqual(imported.RuleName, existing.Description))
                return false;

            // 比较允许的哨位 ID 列表（忽略顺序）
            if (!AreListsEqual(imported.AllowedPositionIds, existing.AllowedPositionIds))
                return false;

            // 比较允许的时段列表（忽略顺序）
            if (!AreListsEqual(imported.AllowedTimeSlots, existing.AllowedPeriods))
                return false;

            // 注意：DTO 中有 StartDate/EndDate/Description 字段，但 Model 中没有
            // 这些字段可能存储在其他地方或不需要比较

            return true;
        }

        /// <summary>
        /// 比较手动分配数据是否相等
        /// </summary>
        /// <param name="imported">导入的手动分配 DTO</param>
        /// <param name="existing">现有的手动分配模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool AreManualAssignmentsEqual(ManualAssignmentDto imported, ManualAssignment existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较哨位 ID
            if (imported.PositionId != existing.PositionId)
                return false;

            // 比较时段索引
            if (imported.TimeSlot != existing.PeriodIndex)
                return false;

            // 比较人员 ID
            if (imported.PersonnelId != existing.PersonalId)
                return false;

            // 比较日期（仅比较日期部分）
            if (!AreDatesEqual(imported.Date, existing.Date))
                return false;

            // 比较是否启用
            if (imported.IsEnabled != existing.IsEnabled)
                return false;

            // 比较备注
            if (!AreStringsEqual(imported.Remarks, existing.Remarks))
                return false;

            return true;
        }

        /// <summary>
        /// 比较节假日配置数据是否相等
        /// </summary>
        /// <param name="imported">导入的节假日配置 DTO</param>
        /// <param name="existing">现有的节假日配置模型</param>
        /// <returns>如果数据相等返回 true，否则返回 false</returns>
        public bool AreHolidayConfigsEqual(HolidayConfigDto imported, HolidayConfig existing)
        {
            if (imported == null || existing == null)
                return false;

            // 比较主键（必须匹配）
            if (imported.Id != existing.Id)
                return false;

            // 比较配置名称
            if (!AreStringsEqual(imported.ConfigName, existing.ConfigName))
                return false;

            // 比较是否启用周末规则
            if (imported.EnableWeekendRule != existing.EnableWeekendRule)
                return false;

            // 比较是否激活
            if (imported.IsActive != existing.IsActive)
                return false;

            // 比较周末日期列表（忽略顺序）
            if (!AreDayOfWeekListsEqual(imported.WeekendDays, existing.WeekendDays))
                return false;

            // 比较法定节假日列表（忽略顺序，仅比较日期部分）
            if (!AreDateListsEqual(imported.LegalHolidays, existing.LegalHolidays))
                return false;

            // 比较自定义休息日列表（忽略顺序，仅比较日期部分）
            if (!AreDateListsEqual(imported.CustomHolidays, existing.CustomHolidays))
                return false;

            // 比较排除日期列表（忽略顺序，仅比较日期部分）
            if (!AreDateListsEqual(imported.ExcludedDates, existing.ExcludedDates))
                return false;

            return true;
        }
    }
}
