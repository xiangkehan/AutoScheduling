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

        // 分配记录：[日期][时段][哨位索引] = 人员索引（-1表示未分配）
        public Dictionary<DateTime, int[,]> Assignments { get; set; } = new();

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
                var state = new PersonScoreState(
                    person.Id,
                    person.RecentShiftIntervalCount,
                    person.RecentHolidayShiftIntervalCount,
                    person.RecentPeriodShiftIntervals
                );
                PersonScoreStates[person.Id] = state;
            }
            IntegrateHistoryIntoScoreStates();
        }

        /// <summary>
        /// 根据最近一次已确认排班整合历史信息（设置最后分配日期与时段）
        /// </summary>
        private void IntegrateHistoryIntoScoreStates()
        {
            if (LastConfirmedSchedule == null) return;
            // 找到每个人员最近的一个班次
            var lastByPerson = new Dictionary<int, SingleShift>();
            foreach (var shift in LastConfirmedSchedule.ShiftsInternal)
            {
                if (!lastByPerson.TryGetValue(shift.PersonnelId, out var existing) || shift.StartTime > existing.StartTime)
                {
                    lastByPerson[shift.PersonnelId] = shift;
                }
            }
            foreach (var kvp in lastByPerson)
            {
                if (PersonScoreStates.TryGetValue(kvp.Key, out var state))
                {
                    var lastShift = kvp.Value;
                    state.LastAssignedDate = lastShift.StartTime.Date;
                    state.LastAssignedPeriod = lastShift.StartTime.Hour / 2; //2小时一个时段
                }
            }
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
            if (Assignments.ContainsKey(date.Date))
            {
                Assignments[date.Date][periodIdx, positionIdx] = personIdx;
            }
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
    }
}
