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

            // 从历史排班中加载数据
            if (LastConfirmedSchedule != null)
            {
                foreach (var shift in LastConfirmedSchedule.Results)
                {
                    // 计算历史班次的时间戳（可能为负数）
                    int timestamp = CalculateTimestamp(shift.StartTime.Date, shift.StartTime.Hour / 2);

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
                        (shift.StartTime.Date, shift.StartTime.Hour / 2, positionIdx);
                }
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
            if (Assignments.ContainsKey(date.Date))
            {
                Assignments[date.Date][periodIdx, positionIdx] = personIdx;
            }

            // 更新人员分配索引
            int personId = PersonIdxToId[personIdx];
            int timestamp = CalculateTimestamp(date, periodIdx);

            PersonAssignmentTimestamps[personId].Add(timestamp);
            PersonAssignmentDetails[personId][timestamp] = (date, periodIdx, positionIdx);
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
