using System;
using System.Linq;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 跨日约束验证器
    /// 负责验证涉及相邻天数时段的约束
    /// 对应需求1.1-1.5, 7.3, 7.4
    /// </summary>
    public class CrossDayConstraintValidator
    {
        private readonly SchedulingContext _context;
        private readonly PeriodMapper _periodMapper;

        public CrossDayConstraintValidator(SchedulingContext context, PeriodMapper periodMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _periodMapper = periodMapper ?? throw new ArgumentNullException(nameof(periodMapper));
        }

        /// <summary>
        /// 验证跨日休息时间约束
        /// 确保人员在相邻时段（包括跨日）有足够的休息时间
        /// 对应需求1.3, 7.4
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateCrossDayRestConstraint(int personIdx, int globalPeriodIdx)
        {
            // 边界检查
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return false;

            var (date, localPeriod) = _periodMapper.ToDateTime(globalPeriodIdx);

            // 检查前一个时段（可能在前一天）
            // 对应需求1.1, 1.3
            if (globalPeriodIdx > 0)
            {
                int prevGlobalPeriod = globalPeriodIdx - 1;
                var (prevDate, prevLocalPeriod) = _periodMapper.ToDateTime(prevGlobalPeriod);

                // 检查该人员在前一时段是否有分配
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(prevDate, prevLocalPeriod, posIdx);
                    if (assignedPersonIdx == personIdx)
                    {
                        // 违反时段不连续约束
                        return false;
                    }
                }
            }

            // 检查后一个时段（可能在后一天）
            // 对应需求1.2
            if (globalPeriodIdx < _periodMapper.TotalPeriods - 1)
            {
                int nextGlobalPeriod = globalPeriodIdx + 1;
                var (nextDate, nextLocalPeriod) = _periodMapper.ToDateTime(nextGlobalPeriod);

                // 检查该人员在后一时段是否有分配
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(nextDate, nextLocalPeriod, posIdx);
                    if (assignedPersonIdx == personIdx)
                    {
                        // 违反时段不连续约束
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 验证跨日夜哨唯一约束
        /// 确保一个人在跨日的夜哨周期（时段11, 0, 1, 2）中最多只能被分配一次
        /// 对应需求1.5, 7.3
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateCrossDayNightShiftConstraint(int personIdx, int globalPeriodIdx)
        {
            // 边界检查
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return false;

            var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);
            
            // 夜哨时段定义：11, 0, 1, 2
            int[] nightPeriods = { 11, 0, 1, 2 };

            if (!nightPeriods.Contains(localPeriod))
                return true; // 非夜哨时段，无需检查

            // 识别跨日的夜哨周期
            // 时段11属于当天夜哨的开始，时段0-2属于次日夜哨的延续
            // 对应需求1.5
            int nightCycleDay = localPeriod == 11 ? dayIndex : dayIndex - 1;

            // 检查同一夜哨周期的其他时段
            foreach (var np in nightPeriods)
            {
                if (np == localPeriod)
                    continue; // 跳过当前时段

                // 计算目标时段所在的天
                int targetDay = np == 11 ? nightCycleDay : nightCycleDay + 1;

                // 确保目标日期在范围内
                // 对应需求10.3（边界条件处理）
                if (targetDay < 0 || targetDay >= _periodMapper.TotalPeriods / 12)
                    continue;

                int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);
                var (targetDate, targetLocalPeriod) = _periodMapper.ToDateTime(targetGlobalPeriod);

                // 检查该人员在同一夜哨周期的其他时段是否已分配
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int assignedPersonIdx = _context.GetAssignment(targetDate, targetLocalPeriod, posIdx);
                    if (assignedPersonIdx == personIdx)
                    {
                        // 违反夜哨唯一约束
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 综合验证所有跨日约束
        /// 对应需求1.1-1.5, 7.3, 7.4
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>是否满足所有跨日约束</returns>
        public bool ValidateAllCrossDayConstraints(int personIdx, int globalPeriodIdx)
        {
            // 验证跨日休息时间约束
            if (!ValidateCrossDayRestConstraint(personIdx, globalPeriodIdx))
                return false;

            // 验证跨日夜哨唯一约束
            if (!ValidateCrossDayNightShiftConstraint(personIdx, globalPeriodIdx))
                return false;

            return true;
        }

        /// <summary>
        /// 获取跨日约束违反详情
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>约束违反详情</returns>
        public string GetCrossDayConstraintViolations(int personIdx, int globalPeriodIdx)
        {
            if (!ValidateCrossDayRestConstraint(personIdx, globalPeriodIdx))
                return "违反跨日休息时间约束：该人员在相邻时段（跨日）已有分配";

            if (!ValidateCrossDayNightShiftConstraint(personIdx, globalPeriodIdx))
                return "违反跨日夜哨唯一约束：该人员在同一夜哨周期（跨日）已有其他分配";

            return string.Empty;
        }
    }
}
