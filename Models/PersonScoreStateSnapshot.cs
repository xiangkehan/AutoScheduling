using System;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 人员评分状态快照
    /// 用于在回溯时保存和恢复人员的累积计数器
    /// 对应需求: 2.1, 2.2, 2.3
    /// </summary>
    public class PersonScoreStateSnapshot
    {
        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonalId { get; set; }

        /// <summary>
        /// 总分配次数
        /// </summary>
        public int TotalAssignments { get; set; }

        /// <summary>
        /// 夜哨分配次数
        /// </summary>
        public int NightShiftCount { get; set; }

        /// <summary>
        /// 日哨分配次数
        /// </summary>
        public int DayShiftCount { get; set; }

        /// <summary>
        /// 加权工作量得分
        /// </summary>
        public double WorkloadScore { get; set; }

        /// <summary>
        /// 从 PersonScoreState 创建快照
        /// </summary>
        public static PersonScoreStateSnapshot FromPersonScoreState(PersonScoreState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return new PersonScoreStateSnapshot
            {
                PersonalId = state.PersonalId,
                TotalAssignments = state.TotalAssignments,
                NightShiftCount = state.NightShiftCount,
                DayShiftCount = state.DayShiftCount,
                WorkloadScore = state.WorkloadScore
            };
        }

        /// <summary>
        /// 恢复到 PersonScoreState
        /// </summary>
        public void RestoreToPersonScoreState(PersonScoreState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            state.TotalAssignments = TotalAssignments;
            state.NightShiftCount = NightShiftCount;
            state.DayShiftCount = DayShiftCount;
            state.WorkloadScore = WorkloadScore;
        }

        public override string ToString()
        {
            return $"PersonScoreStateSnapshot[{PersonalId}]: Total={TotalAssignments}, Night={NightShiftCount}, Day={DayShiftCount}, Workload={WorkloadScore:F2}";
        }
    }
}
