using System;
using System.Collections.Generic;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 分配记录：记录单次分配的完整信息，用于回溯
    /// 对应需求: 2.1, 2.2
    /// </summary>
    public class AssignmentRecord
    {
        /// <summary>
        /// 哨位索引
        /// </summary>
        public int PositionIdx { get; set; }

        /// <summary>
        /// 时段索引 (0-11)
        /// </summary>
        public int PeriodIdx { get; set; }

        /// <summary>
        /// 分配的人员索引
        /// </summary>
        public int PersonIdx { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 候选人员列表（按评分排序，从高到低）
        /// </summary>
        public List<int> Candidates { get; set; }

        /// <summary>
        /// 当前尝试的候选人员索引（在Candidates列表中的索引）
        /// </summary>
        public int CurrentCandidateIndex { get; set; }

        /// <summary>
        /// 状态快照（分配前的系统状态）
        /// </summary>
        public StateSnapshot Snapshot { get; set; }

        /// <summary>
        /// 分配时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 是否为手动指定
        /// </summary>
        public bool IsManual { get; set; }

        /// <summary>
        /// 回溯深度（用于调试和统计）
        /// </summary>
        public int Depth { get; set; }

        public AssignmentRecord()
        {
            PositionIdx = -1;
            PeriodIdx = -1;
            PersonIdx = -1;
            Date = DateTime.MinValue;
            Candidates = new List<int>();
            CurrentCandidateIndex = 0;
            Snapshot = new StateSnapshot();
            Timestamp = DateTime.UtcNow;
            IsManual = false;
            Depth = 0;
        }

        /// <summary>
        /// 是否还有未尝试的候选人员
        /// </summary>
        public bool HasMoreCandidates => CurrentCandidateIndex < Candidates.Count - 1;

        /// <summary>
        /// 获取下一个候选人员索引
        /// </summary>
        /// <returns>下一个候选人员索引，如果没有更多候选则返回null</returns>
        public int? GetNextCandidate()
        {
            if (HasMoreCandidates)
            {
                CurrentCandidateIndex++;
                return Candidates[CurrentCandidateIndex];
            }
            return null;
        }

        /// <summary>
        /// 获取当前候选人员索引
        /// </summary>
        public int? GetCurrentCandidate()
        {
            if (CurrentCandidateIndex >= 0 && CurrentCandidateIndex < Candidates.Count)
            {
                return Candidates[CurrentCandidateIndex];
            }
            return null;
        }

        /// <summary>
        /// 重置候选人员索引到第一个
        /// </summary>
        public void ResetCandidateIndex()
        {
            CurrentCandidateIndex = 0;
        }

        /// <summary>
        /// 获取剩余候选人员数量
        /// </summary>
        public int RemainingCandidatesCount => Math.Max(0, Candidates.Count - CurrentCandidateIndex - 1);

        /// <summary>
        /// 验证记录的有效性
        /// </summary>
        public bool Validate()
        {
            try
            {
                // 检查基本字段
                if (PositionIdx < 0 || PeriodIdx < 0 || PeriodIdx > 11)
                {
                    return false;
                }

                // 检查候选列表
                if (Candidates == null || Candidates.Count == 0)
                {
                    return false;
                }

                // 检查当前索引
                if (CurrentCandidateIndex < 0 || CurrentCandidateIndex >= Candidates.Count)
                {
                    return false;
                }

                // 检查快照
                if (Snapshot == null || !Snapshot.ValidateIntegrity())
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建分配记录
        /// </summary>
        public static AssignmentRecord Create(
            int positionIdx,
            int periodIdx,
            int personIdx,
            DateTime date,
            List<int> candidates,
            StateSnapshot snapshot,
            int depth = 0,
            bool isManual = false)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("候选人员列表不能为空", nameof(candidates));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            // 找到当前人员在候选列表中的索引
            int currentIndex = candidates.IndexOf(personIdx);
            if (currentIndex < 0)
            {
                throw new ArgumentException($"人员索引 {personIdx} 不在候选列表中", nameof(personIdx));
            }

            return new AssignmentRecord
            {
                PositionIdx = positionIdx,
                PeriodIdx = periodIdx,
                PersonIdx = personIdx,
                Date = date,
                Candidates = new List<int>(candidates), // 深拷贝
                CurrentCandidateIndex = currentIndex,
                Snapshot = snapshot,
                Timestamp = DateTime.UtcNow,
                IsManual = isManual,
                Depth = depth
            };
        }

        /// <summary>
        /// 获取记录的描述信息（用于调试和日志）
        /// </summary>
        public override string ToString()
        {
            var candidateInfo = $"{CurrentCandidateIndex + 1}/{Candidates.Count}";
            var manualFlag = IsManual ? "[手动]" : "[自动]";
            return $"AssignmentRecord {manualFlag} [Depth={Depth}, Pos={PositionIdx}, Period={PeriodIdx}, " +
                   $"Person={PersonIdx}, Candidates={candidateInfo}, Remaining={RemainingCandidatesCount}, " +
                   $"Date={Date:yyyy-MM-dd}, Time={Timestamp:HH:mm:ss.fff}]";
        }

        /// <summary>
        /// 获取简短描述（用于日志）
        /// </summary>
        public string ToShortString()
        {
            return $"[P{PositionIdx}:T{PeriodIdx}→U{PersonIdx}] ({CurrentCandidateIndex + 1}/{Candidates.Count})";
        }
    }
}
