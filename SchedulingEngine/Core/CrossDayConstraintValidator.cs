using System;
using System.Linq;
using AutoScheduling3.Constants;

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
        /// 对应需求1.3, 7.4, 10.3
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateCrossDayRestConstraint(int personIdx, int globalPeriodIdx)
        {
            // 边界检查：验证全局时段索引是否有效
            // 对应需求10.3
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return false;

            var (date, localPeriod) = _periodMapper.ToDateTime(globalPeriodIdx);

            // 检查前一个时段（可能在前一天）
            // 对应需求1.1, 1.3
            // 边界条件：如果是第一个全局时段(globalPeriodIdx == 0)，则没有前一时段，跳过检查
            // 对应需求10.3
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
            // else: 第一个全局时段，没有前一时段，无需检查前一时段的休息约束

            // 检查后一个时段（可能在后一天）
            // 对应需求1.2
            // 边界条件：如果是最后一个全局时段(globalPeriodIdx == TotalPeriods - 1)，则没有后一时段，跳过检查
            // 对应需求10.3
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
            // else: 最后一个全局时段，没有后一时段，无需检查后一时段的休息约束

            return true;
        }

        /// <summary>
        /// 验证跨日夜哨唯一约束
        /// 确保一个人在跨日的夜哨周期（时段11, 0, 1, 2）中最多只能被分配一次
        /// 对应需求1.5, 7.3, 10.3
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>是否满足约束</returns>
        public bool ValidateCrossDayNightShiftConstraint(int personIdx, int globalPeriodIdx)
        {
            // 边界检查：验证全局时段索引是否有效
            // 对应需求10.3
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return false;

            var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);
            
            // 夜哨时段定义：11, 0, 1, 2
            if (!SchedulingConstants.NightShiftPeriods.Contains(localPeriod))
                return true; // 非夜哨时段，无需检查

            int personId = _context.PersonIdxToId[personIdx];

            // 特殊处理：第一天的时段0-2需要检查历史数据
            // 对应需求10.3
            if (dayIndex == 0 && localPeriod <= 2)
            {
                // 1. 检查同一天内的其他夜哨时段（时段0-2）
                foreach (var np in new[] { 0, 1, 2 })
                {
                    if (np == localPeriod) continue;
                    
                    int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(0, np);
                    var (targetDate, targetLocalPeriod) = _periodMapper.ToDateTime(targetGlobalPeriod);
                    
                    for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                    {
                        int assignment = _context.GetAssignment(targetDate, targetLocalPeriod, posIdx);
                        if (assignment == personIdx)
                        {
                            return false; // 违反夜哨唯一约束
                        }
                    }
                }
                
                // 2. 检查前一天的时段11（历史数据）
                DateTime prevDayDate = _context.StartDate.Date.AddDays(-1);
                int prevDayPeriod11Timestamp = _context.CalculateTimestamp(prevDayDate, 11);
                
                // 查询该人员在前一天时段11是否有分配
                if (_context.PersonAssignmentTimestamps.TryGetValue(personId, out var timestamps))
                {
                    if (timestamps.Contains(prevDayPeriod11Timestamp))
                    {
                        // 该人员在前一天时段11有分配，违反夜哨唯一约束
                        return false;
                    }
                }
                
                return true;
            }

            // 识别跨日的夜哨周期
            // 时段11属于当天夜哨的开始，时段0-2属于次日夜哨的延续
            // 对应需求1.5
            int nightCycleDay = localPeriod == SchedulingConstants.MaxPeriodIndex ? dayIndex : dayIndex - 1;

            // 检查同一夜哨周期的其他时段
            foreach (var np in SchedulingConstants.NightShiftPeriods)
            {
                if (np == localPeriod)
                    continue; // 跳过当前时段

                // 计算目标时段所在的天
                int targetDay = np == SchedulingConstants.MaxPeriodIndex ? nightCycleDay : nightCycleDay + 1;

                // 边界条件检查：确保目标日期在有效范围内
                // 对应需求10.3
                if (!_periodMapper.IsValidDayIndex(targetDay))
                    continue; // 目标日期超出范围，跳过该时段检查

                // 验证目标全局时段索引是否有效
                int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);
                if (!_periodMapper.IsValidGlobalPeriod(targetGlobalPeriod))
                    continue; // 目标全局时段无效，跳过

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

        /// <summary>
        /// 检查全局时段索引是否在边界位置
        /// 对应需求10.3
        /// </summary>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>边界信息：(是否是第一个时段, 是否是最后一个时段, 是否是第一天, 是否是最后一天)</returns>
        public (bool isFirstPeriod, bool isLastPeriod, bool isFirstDay, bool isLastDay) CheckBoundaryPosition(int globalPeriodIdx)
        {
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return (false, false, false, false);

            var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);

            bool isFirstPeriod = globalPeriodIdx == 0;
            bool isLastPeriod = globalPeriodIdx == _periodMapper.TotalPeriods - 1;
            bool isFirstDay = dayIndex == 0;
            bool isLastDay = dayIndex == _periodMapper.TotalDays - 1;

            return (isFirstPeriod, isLastPeriod, isFirstDay, isLastDay);
        }

        /// <summary>
        /// 获取跨日夜哨周期中的有效时段
        /// 考虑边界条件，返回实际存在的夜哨时段
        /// 对应需求10.3
        /// </summary>
        /// <param name="globalPeriodIdx">当前全局时段索引</param>
        /// <returns>同一夜哨周期中的有效全局时段索引列表</returns>
        public List<int> GetValidNightShiftPeriods(int globalPeriodIdx)
        {
            var validPeriods = new List<int>();

            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return validPeriods;

            var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);

            // 夜哨时段定义：11, 0, 1, 2
            if (!SchedulingConstants.NightShiftPeriods.Contains(localPeriod))
                return validPeriods; // 非夜哨时段

            // 识别跨日的夜哨周期
            int nightCycleDay = localPeriod == SchedulingConstants.MaxPeriodIndex ? dayIndex : dayIndex - 1;

            // 收集同一夜哨周期的所有有效时段
            foreach (var np in SchedulingConstants.NightShiftPeriods)
            {
                int targetDay = np == SchedulingConstants.MaxPeriodIndex ? nightCycleDay : nightCycleDay + 1;

                // 边界检查
                if (!_periodMapper.IsValidDayIndex(targetDay))
                    continue;

                int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);

                if (_periodMapper.IsValidGlobalPeriod(targetGlobalPeriod))
                {
                    validPeriods.Add(targetGlobalPeriod);
                }
            }

            return validPeriods;
        }
    }
}
