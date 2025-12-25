using System;
using System.Collections.Generic;
using System.Linq; // added for LINQ operations
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 调度上下文：包含排班算法所需的所有数据和映射关系
    /// </summary>
    public class SchedulingContext
    {
        // 基础数据
        public List<Personal> Personals { get; set; } = new();
        public List<PositionLocation> Positions { get; set; } = new();
        public List<Skill> Skills { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // 配置数据
        public HolidayConfig? HolidayConfig { get; set; }
        public List<FixedPositionRule> FixedPositionRules { get; set; } = new();
        public List<ManualAssignment> ManualAssignments { get; set; } = new();

        // 历史数据
        public Schedule? LastConfirmedSchedule { get; set; }

        // 映射关系：序号 <-> 实体ID
        public Dictionary<int, int> PositionIdxToId { get; set; } = new();
        public Dictionary<int, int> PositionIdToIdx { get; set; } = new();
        public Dictionary<int, int> PersonIdxToId { get; set; } = new();
        public Dictionary<int, int> PersonIdToIdx { get; set; } = new();

        // 人员评分状态
        public Dictionary<int, PersonScoreState> PersonScoreStates { get; set; } = new();

        // 全局平均工作量
        public double AverageWorkload { get; private set; }

        // 分配记录：[日期][时段][哨位索引] = 人员索引（-1表示未分配）
        public Dictionary<DateTime, int[,]> Assignments { get; set; } = new();

        // 人员分配索引：personId -> 已分配班次的时间戳集合（有序）
        // 时间戳 = (date - StartDate).Days * 12 + periodIdx
        public Dictionary<int, SortedSet<int>> PersonAssignmentTimestamps { get; set; } = new();

        // 时间戳到班次详情的映射：personId -> timestamp -> (date, period, positionIdx)
        public Dictionary<int, Dictionary<int, (DateTime date, int period, int positionIdx)>> PersonAssignmentDetails { get; set; } = new();

        /// <summary>
        /// 初始化序号映射
        /// </summary>
        public void InitializeMappings()
        {
            // 哨位映射
            for (int i = 0; i < Positions.Count; i++)
            {
                PositionIdxToId[i] = Positions[i].Id;
                PositionIdToIdx[Positions[i].Id] = i;
            }

            // 人员映射
            for (int i = 0; i < Personals.Count; i++)
            {
                PersonIdxToId[i] = Personals[i].Id;
                PersonIdToIdx[Personals[i].Id] = i;
            }
        }

        /// <summary>
        /// 初始化人员评分状态（包含历史整合）
        /// </summary>
        public void InitializePersonScoreStates()
        {
            foreach (var person in Personals)
            {
                var state = new PersonScoreState(person.Id);
                PersonScoreStates[person.Id] = state;
            }
            InitializePersonAssignmentIndex();
        }

        /// <summary>
        /// 初始化人员分配索引（包含历史排班数据）
        /// </summary>
        private void InitializePersonAssignmentIndex()
        {
            // 为每个人员初始化空索引
            foreach (var person in Personals)
            {
                PersonAssignmentTimestamps[person.Id] = new SortedSet<int>();
                PersonAssignmentDetails[person.Id] = new Dictionary<int, (DateTime, int, int)>();
            }

            // 从历史排班中加载数据（只加载当前排班人员的历史数据）
            if (LastConfirmedSchedule != null)
            {
                foreach (var shift in LastConfirmedSchedule.Results)
                {
                    // 跳过不在当前排班人员列表中的历史数据
                    if (!PersonAssignmentTimestamps.ContainsKey(shift.PersonnelId))
                        continue;

                    // 计算历史班次的时间戳（可能为负数）
                    int timestamp = CalculateTimestamp(shift.StartTime.Date, shift.StartTime.Hour / 2);
                    int periodIdx = shift.StartTime.Hour / 2;

                    // 添加到该人员的时间戳集合
                    PersonAssignmentTimestamps[shift.PersonnelId].Add(timestamp);

                    // 尝试获取哨位索引（历史哨位可能已被删除）
                    int positionIdx = -1;
                    if (PositionIdToIdx.TryGetValue(shift.PositionId, out int idx))
                    {
                        positionIdx = idx;
                    }

                    // 记录详细信息
                    PersonAssignmentDetails[shift.PersonnelId][timestamp] =
                        (shift.StartTime.Date, periodIdx, positionIdx);

                    // 更新历史数据的累积计数器
                    if (PersonScoreStates.TryGetValue(shift.PersonnelId, out var state))
                    {
                        state.TotalAssignments++;

                        // 判断是否为夜哨
                        if (periodIdx == 11 || periodIdx == 0 || periodIdx == 1 || periodIdx == 2)
                        {
                            state.NightShiftCount++;
                            state.WorkloadScore += 1.5;
                        }
                        else
                        {
                            state.DayShiftCount++;
                            state.WorkloadScore += 1.0;
                        }
                    }
                }

                // 初始化全局平均工作量
                UpdateAverageWorkload();
            }
        }

        /// <summary>
        /// 计算时间戳（以时段为单位，以 StartDate 为基准）
        /// </summary>
        public int CalculateTimestamp(DateTime date, int periodIdx)
        {
            return (date.Date - StartDate.Date).Days * 12 + periodIdx;
        }

        /// <summary>
        /// 初始化分配记录
        /// </summary>
        public void InitializeAssignments()
        {
            var currentDate = StartDate.Date;
            
            while (currentDate <= EndDate.Date)
            {
                // 每天12个时段，每个哨位一个分配
                Assignments[currentDate] = new int[12, Positions.Count];
                for (int p = 0; p < 12; p++)
                {
                    for (int x = 0; x < Positions.Count; x++)
                    {
                        Assignments[currentDate][p, x] = -1; // -1表示未分配
                    }
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        /// <summary>
        /// 记录分配
        /// </summary>
        public void RecordAssignment(DateTime date, int periodIdx, int positionIdx, int personIdx)
        {
            var dateKey = date.Date;
            if (Assignments.ContainsKey(dateKey))
            {
                Assignments[dateKey][periodIdx, positionIdx] = personIdx;
            }
            else
            {
                return; // 如果日期不存在，不继续更新其他状态
            }

            // 更新人员分配索引
            int personId = PersonIdxToId[personIdx];
            int timestamp = CalculateTimestamp(date, periodIdx);

            PersonAssignmentTimestamps[personId].Add(timestamp);
            PersonAssignmentDetails[personId][timestamp] = (date, periodIdx, positionIdx);

            // 更新累积计数器
            if (PersonScoreStates.TryGetValue(personId, out var state))
            {
                state.TotalAssignments++;

                // 判断是否为夜哨（时段 11, 0, 1, 2）
                if (periodIdx == 11 || periodIdx == 0 || periodIdx == 1 || periodIdx == 2)
                {
                    state.NightShiftCount++;
                    state.WorkloadScore += 1.5; // 夜哨权重 1.5
                }
                else
                {
                    state.DayShiftCount++;
                    state.WorkloadScore += 1.0; // 日哨权重 1.0
                }
            }

            // 更新全局平均工作量
            UpdateAverageWorkload();
        }

        /// <summary>
        /// 更新全局平均工作量
        /// </summary>
        public void UpdateAverageWorkload()
        {
            if (PersonScoreStates.Count == 0)
            {
                AverageWorkload = 0;
                return;
            }

            double totalWorkload = PersonScoreStates.Values.Sum(s => s.WorkloadScore);
            AverageWorkload = totalWorkload / PersonScoreStates.Count;
        }

        /// <summary>
        /// 获取分配的人员索引
        /// </summary>
        public int GetAssignment(DateTime date, int periodIdx, int positionIdx)
        {
            if (Assignments.ContainsKey(date.Date))
            {
                return Assignments[date.Date][periodIdx, positionIdx];
            }
            return -1;
        }

        /// <summary>
        /// 判断指定日期是否为休息日
        /// </summary>
        public bool IsHoliday(DateTime date)
        {
            return HolidayConfig?.IsHoliday(date) ?? false;
        }

        #region 状态快照支持方法

        /// <summary>
        /// 创建人员评分状态的深拷贝
        /// 对应需求: 2.1, 3.1
        /// </summary>
        public Dictionary<int, PersonScoreStateSnapshot> CreatePersonScoreStatesSnapshot()
        {
            var snapshot = new Dictionary<int, PersonScoreStateSnapshot>();
            foreach (var kvp in PersonScoreStates)
            {
                snapshot[kvp.Key] = PersonScoreStateSnapshot.FromPersonScoreState(kvp.Value);
            }
            return snapshot;
        }

        /// <summary>
        /// 从快照恢复人员评分状态
        /// 对应需求: 2.2
        /// </summary>
        public void RestorePersonScoreStates(Dictionary<int, PersonScoreStateSnapshot> snapshot)
        {
            if (snapshot == null) return;

            foreach (var kvp in snapshot)
            {
                if (PersonScoreStates.TryGetValue(kvp.Key, out var state))
                {
                    kvp.Value.RestoreToPersonScoreState(state);
                }
            }
        }

        /// <summary>
        /// 创建人员分配时间戳的深拷贝
        /// 对应需求: 3.1
        /// </summary>
        public Dictionary<int, SortedSet<int>> CreatePersonAssignmentTimestampsSnapshot()
        {
            var snapshot = new Dictionary<int, SortedSet<int>>();
            foreach (var kvp in PersonAssignmentTimestamps)
            {
                snapshot[kvp.Key] = new SortedSet<int>(kvp.Value);
            }
            return snapshot;
        }

        /// <summary>
        /// 从快照恢复人员分配时间戳
        /// 对应需求: 3.3
        /// </summary>
        public void RestorePersonAssignmentTimestamps(Dictionary<int, SortedSet<int>> snapshot)
        {
            if (snapshot == null) return;

            foreach (var kvp in snapshot)
            {
                if (PersonAssignmentTimestamps.ContainsKey(kvp.Key))
                {
                    PersonAssignmentTimestamps[kvp.Key] = new SortedSet<int>(kvp.Value);
                }
            }
        }

        /// <summary>
        /// 创建人员分配详情的深拷贝
        /// 对应需求: 3.2
        /// </summary>
        public Dictionary<int, Dictionary<int, (DateTime date, int period, int positionIdx)>> CreatePersonAssignmentDetailsSnapshot()
        {
            var snapshot = new Dictionary<int, Dictionary<int, (DateTime, int, int)>>();
            foreach (var kvp in PersonAssignmentDetails)
            {
                snapshot[kvp.Key] = new Dictionary<int, (DateTime, int, int)>(kvp.Value);
            }
            return snapshot;
        }

        /// <summary>
        /// 从快照恢复人员分配详情
        /// 对应需求: 3.4
        /// </summary>
        public void RestorePersonAssignmentDetails(Dictionary<int, Dictionary<int, (DateTime date, int period, int positionIdx)>> snapshot)
        {
            if (snapshot == null) return;

            foreach (var kvp in snapshot)
            {
                if (PersonAssignmentDetails.ContainsKey(kvp.Key))
                {
                    PersonAssignmentDetails[kvp.Key] = new Dictionary<int, (DateTime, int, int)>(kvp.Value);
                }
            }
        }

        /// <summary>
        /// 验证状态一致性
        /// 对应需求: 6.1, 6.2, 6.3
        /// </summary>
        public StateConsistencyResult ValidateStateConsistency()
        {
            var result = new StateConsistencyResult
            {
                IsConsistent = true,
                Inconsistencies = new List<string>()
            };

            // 检查 Assignments 与 PersonAssignmentTimestamps 一致性
            foreach (var dateAssignments in Assignments)
            {
                var date = dateAssignments.Key;
                var assignments = dateAssignments.Value;

                for (int periodIdx = 0; periodIdx < assignments.GetLength(0); periodIdx++)
                {
                    for (int positionIdx = 0; positionIdx < assignments.GetLength(1); positionIdx++)
                    {
                        int personIdx = assignments[periodIdx, positionIdx];
                        if (personIdx >= 0)
                        {
                            int personId = PersonIdxToId[personIdx];
                            int timestamp = CalculateTimestamp(date, periodIdx);

                            // 检查时间戳是否存在
                            if (!PersonAssignmentTimestamps.TryGetValue(personId, out var timestamps) ||
                                !timestamps.Contains(timestamp))
                            {
                                result.IsConsistent = false;
                                result.Inconsistencies.Add(
                                    $"分配记录与时间戳不一致: 人员{personId} 在 {date:yyyy-MM-dd} 时段{periodIdx} 有分配，但时间戳索引中不存在");
                            }

                            // 检查详情是否存在
                            if (!PersonAssignmentDetails.TryGetValue(personId, out var details) ||
                                !details.ContainsKey(timestamp))
                            {
                                result.IsConsistent = false;
                                result.Inconsistencies.Add(
                                    $"分配记录与详情不一致: 人员{personId} 在 {date:yyyy-MM-dd} 时段{periodIdx} 有分配，但详情索引中不存在");
                            }
                        }
                    }
                }
            }

            // 检查 PersonScoreStates 与分配记录一致性
            foreach (var kvp in PersonScoreStates)
            {
                int personId = kvp.Key;
                var state = kvp.Value;

                // 统计该人员的实际分配次数
                int actualAssignments = 0;
                int actualNightShifts = 0;
                int actualDayShifts = 0;

                if (PersonAssignmentTimestamps.TryGetValue(personId, out var timestamps))
                {
                    foreach (var timestamp in timestamps)
                    {
                        // 只统计当前排班周期内的分配（时间戳 >= 0）
                        if (timestamp >= 0)
                        {
                            actualAssignments++;
                            int periodIdx = timestamp % 12;
                            if (periodIdx == 11 || periodIdx == 0 || periodIdx == 1 || periodIdx == 2)
                            {
                                actualNightShifts++;
                            }
                            else
                            {
                                actualDayShifts++;
                            }
                        }
                    }
                }

                // 注意：由于历史数据的存在，累积计数器可能大于当前周期的分配数
                // 这里只检查当前周期的分配是否被正确计入
                // 如果累积计数器小于当前周期的分配数，则说明有问题
                if (state.TotalAssignments < actualAssignments)
                {
                    result.IsConsistent = false;
                    result.Inconsistencies.Add(
                        $"人员{personId}的TotalAssignments({state.TotalAssignments})小于实际分配数({actualAssignments})");
                }
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// 状态一致性验证结果
    /// 对应需求: 6.1, 6.2, 6.3
    /// </summary>
    public class StateConsistencyResult
    {
        /// <summary>
        /// 是否一致
        /// </summary>
        public bool IsConsistent { get; set; }

        /// <summary>
        /// 不一致详情列表
        /// </summary>
        public List<string> Inconsistencies { get; set; } = new();

        public override string ToString()
        {
            if (IsConsistent)
            {
                return "状态一致性验证通过";
            }
            return $"状态一致性验证失败: {Inconsistencies.Count} 个问题\n" +
                   string.Join("\n", Inconsistencies.Select(i => $"  - {i}"));
        }
    }
}
