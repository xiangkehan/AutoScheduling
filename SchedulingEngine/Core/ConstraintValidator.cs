using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 硬约束验证器 - 对应需求5.1-5.8
    /// 实现所有硬约束检查，确保排班方案的合规性和可行性
    /// </summary>
    public class ConstraintValidator
    {
        private readonly SchedulingContext _context;

        public ConstraintValidator(SchedulingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 验证夜哨唯一性约束 - 对应需求5.1
        /// 确保一个人同一个晚上只能上一个夜哨
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateNightShiftUniqueness(int personIdx, int periodIdx, DateTime date)
        {
            // 夜哨时段定义：23:00-01:00, 01:00-03:00, 03:00-05:00, 05:00-07:00 (时段11, 0, 1, 2)
            int[] nightPeriods = { 11, 0, 1, 2 };
            
            if (!nightPeriods.Contains(periodIdx))
                return true; // 非夜哨时段，无需检查

            // 检查该人员在同一晚上是否已有其他夜哨分配
            foreach (var nightPeriod in nightPeriods)
            {
                if (nightPeriod == periodIdx) continue;

                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(date, nightPeriod, posIdx);
                    if (assignedPersonIdx == personIdx)
                        return false; // 该人员已在其他夜哨时段分配
                }
            }

            return true;
        }

        /// <summary>
        /// 验证时段不连续约束 - 对应需求5.2
        /// 防止一个人在相邻时段连续上哨
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateNonConsecutiveShifts(int personIdx, int periodIdx, DateTime date)
        {
            // 检查前一个时段
            if (periodIdx > 0)
            {
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(date, periodIdx - 1, posIdx);
                    if (assignedPersonIdx == personIdx)
                        return false; // 前一时段已分配给该人员
                }
            }

            // 检查后一个时段
            if (periodIdx < 11)
            {
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(date, periodIdx + 1, posIdx);
                    if (assignedPersonIdx == personIdx)
                        return false; // 后一时段已分配给该人员
                }
            }

            // 跨日检查：如果是第一个时段(0)，检查前一天的最后时段(11)
            if (periodIdx == 0)
            {
                var previousDate = date.AddDays(-1);
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(previousDate, 11, posIdx);
                    if (assignedPersonIdx == personIdx)
                        return false;
                }
            }

            // 跨日检查：如果是最后时段(11)，检查后一天的第一个时段(0)
            if (periodIdx == 11)
            {
                var nextDate = date.AddDays(1);
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(nextDate, 0, posIdx);
                    if (assignedPersonIdx == personIdx)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证人员可用性约束 - 对应需求5.3
        /// 仅为可用状态的人员分配哨位
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidatePersonnelAvailability(int personIdx)
        {
            if (personIdx < 0 || personIdx >= _context.Personals.Count)
                return false;

            var person = _context.Personals[personIdx];
            return person.IsAvailable && !person.IsRetired;
        }

        /// <summary>
        /// 验证定岗要求约束 - 对应需求5.4
        /// 将人员限制在指定哨位或时段
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">哨位索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateFixedAssignment(int personIdx, int positionIdx, int periodIdx, DateTime date)
        {
            int personId = _context.PersonIdxToId[personIdx];
            int positionId = _context.PositionIdxToId[positionIdx];

            // 查找该人员的定岗规则
            var fixedRules = _context.FixedPositionRules
                .Where(r => r.IsEnabled && r.PersonalId == personId)
                .ToList();

            if (!fixedRules.Any())
                return true; // 无定岗规则，允许分配

            foreach (var rule in fixedRules)
            {
                bool positionAllowed = rule.AllowedPositionIds.Count == 0 || 
                                     rule.AllowedPositionIds.Contains(positionId);
                bool periodAllowed = rule.AllowedPeriods.Count == 0 || 
                                   rule.AllowedPeriods.Contains(periodIdx);

                if (positionAllowed && periodAllowed)
                    return true; // 至少有一个规则允许此分配
            }

            return false; // 所有规则都不允许此分配
        }

        /// <summary>
        /// 验证人员-哨位可用性约束 - 对应需求4.2
        /// 验证人员是否在哨位的可用人员列表中
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">哨位索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidatePersonnelPositionAvailability(int personIdx, int positionIdx)
        {
            if (personIdx < 0 || personIdx >= _context.Personals.Count ||
                positionIdx < 0 || positionIdx >= _context.Positions.Count)
                return false;

            int personId = _context.PersonIdxToId[personIdx];
            var position = _context.Positions[positionIdx];

            // 检查人员是否在哨位的可用人员列表中
            return position.AvailablePersonnelIds.Contains(personId);
        }

        /// <summary>
        /// 验证技能匹配约束 - 对应需求5.5
        /// 验证人员技能与哨位要求的匹配性
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">哨位索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateSkillMatch(int personIdx, int positionIdx)
        {
            if (personIdx < 0 || personIdx >= _context.Personals.Count ||
                positionIdx < 0 || positionIdx >= _context.Positions.Count)
                return false;

            var person = _context.Personals[personIdx];
            var position = _context.Positions[positionIdx];

            // 如果哨位没有技能要求，则允许任何人员
            if (position.RequiredSkillIds == null || position.RequiredSkillIds.Count == 0)
                return true;

            // 检查人员是否具备所有必需技能
            return position.RequiredSkillIds.All(skillId => person.SkillIds.Contains(skillId));
        }

        /// <summary>
        /// 验证单人上哨约束 - 对应需求5.6
        /// 确保每个哨位每个时段仅分配一人
        /// </summary>
        /// <param name="positionIdx">哨位索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateSinglePersonPerShift(int positionIdx, int periodIdx, DateTime date)
        {
            int assignedPersonIdx = _context.GetAssignment(date, periodIdx, positionIdx);
            return assignedPersonIdx == -1; // -1表示未分配，可以分配
        }

        /// <summary>
        /// 验证人员时段唯一性约束 - 对应需求5.7
        /// 确保一个人在一个时段仅在一个哨位值班
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="positionIdx">当前哨位索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否满足约束</returns>
        public bool ValidatePersonTimeSlotUniqueness(int personIdx, int periodIdx, int positionIdx, DateTime date)
        {
            // 检查该人员在同一时段是否已在其他哨位分配
            for (int otherPosIdx = 0; otherPosIdx < _context.Positions.Count; otherPosIdx++)
            {
                if (otherPosIdx == positionIdx) continue;

                int assignedPersonIdx = _context.GetAssignment(date, periodIdx, otherPosIdx);
                if (assignedPersonIdx == personIdx)
                    return false; // 该人员已在其他哨位分配
            }

            return true;
        }

        /// <summary>
        /// 验证手动指定约束 - 对应需求5.8
        /// 按照指定分配执行
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">哨位索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否为手动指定的分配</returns>
        public bool ValidateManualAssignment(int personIdx, int positionIdx, int periodIdx, DateTime date)
        {
            int personId = _context.PersonIdxToId[personIdx];
            int positionId = _context.PositionIdxToId[positionIdx];

            // 查找该时段的手动指定
            var manualAssignment = _context.ManualAssignments
                .FirstOrDefault(m => m.IsEnabled && 
                               m.Date.Date == date.Date &&
                               m.PositionId == positionId &&
                               m.PeriodIndex == periodIdx);

            if (manualAssignment == null)
                return true; // 无手动指定，允许自动分配

            // 如果有手动指定，必须匹配指定的人员
            return manualAssignment.PersonalId == personId;
        }

        /// <summary>
        /// 综合验证所有硬约束
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">哨位索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>是否满足所有硬约束</returns>
        public bool ValidateAllConstraints(int personIdx, int positionIdx, int periodIdx, DateTime date)
        {
            // 验证人员可用性
            if (!ValidatePersonnelAvailability(personIdx))
                return false;

            // 验证人员-哨位可用性（新数据模型约束）
            if (!ValidatePersonnelPositionAvailability(personIdx, positionIdx))
                return false;

            // 验证技能匹配
            if (!ValidateSkillMatch(personIdx, positionIdx))
                return false;

            // 验证单人上哨
            if (!ValidateSinglePersonPerShift(positionIdx, periodIdx, date))
                return false;

            // 验证人员时段唯一性
            if (!ValidatePersonTimeSlotUniqueness(personIdx, periodIdx, positionIdx, date))
                return false;

            // 验证夜哨唯一性
            if (!ValidateNightShiftUniqueness(personIdx, periodIdx, date))
                return false;

            // 验证时段不连续
            if (!ValidateNonConsecutiveShifts(personIdx, periodIdx, date))
                return false;

            // 验证定岗要求
            if (!ValidateFixedAssignment(personIdx, positionIdx, periodIdx, date))
                return false;

            // 验证手动指定
            if (!ValidateManualAssignment(personIdx, positionIdx, periodIdx, date))
                return false;

            return true;
        }

        /// <summary>
        /// 获取约束违反详情
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">哨位索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>约束违反详情列表</returns>
        public List<string> GetConstraintViolations(int personIdx, int positionIdx, int periodIdx, DateTime date)
        {
            var violations = new List<string>();

            if (!ValidatePersonnelAvailability(personIdx))
                violations.Add("人员不可用或已退役");

            if (!ValidatePersonnelPositionAvailability(personIdx, positionIdx))
                violations.Add("人员不在哨位可用人员列表中");

            if (!ValidateSkillMatch(personIdx, positionIdx))
                violations.Add("人员技能不匹配哨位要求");

            if (!ValidateSinglePersonPerShift(positionIdx, periodIdx, date))
                violations.Add("该哨位时段已有人员分配");

            if (!ValidatePersonTimeSlotUniqueness(personIdx, periodIdx, positionIdx, date))
                violations.Add("该人员在此时段已有其他哨位分配");

            if (!ValidateNightShiftUniqueness(personIdx, periodIdx, date))
                violations.Add("该人员同一晚上已有其他夜哨分配");

            if (!ValidateNonConsecutiveShifts(personIdx, periodIdx, date))
                violations.Add("该人员在相邻时段已有分配");

            if (!ValidateFixedAssignment(personIdx, positionIdx, periodIdx, date))
                violations.Add("违反定岗规则限制");

            if (!ValidateManualAssignment(personIdx, positionIdx, periodIdx, date))
                violations.Add("与手动指定不符");

            return violations;
        }
    }
}