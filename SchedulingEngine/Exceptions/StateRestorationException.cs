using System;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Exceptions
{
    /// <summary>
    /// 状态恢复失败异常
    /// Exception thrown when state restoration from a snapshot fails
    /// </summary>
    public class StateRestorationException : Exception
    {
        /// <summary>
        /// 损坏的状态快照
        /// </summary>
        public StateSnapshot? CorruptedSnapshot { get; }

        public StateRestorationException(string message)
            : base($"状态恢复失败: {message}")
        {
        }

        public StateRestorationException(StateSnapshot snapshot, string message)
            : base($"状态恢复失败: {message}")
        {
            CorruptedSnapshot = snapshot;
        }

        public StateRestorationException(string message, Exception innerException)
            : base($"状态恢复失败: {message}", innerException)
        {
        }

        public StateRestorationException(StateSnapshot snapshot, string message, Exception innerException)
            : base($"状态恢复失败: {message}", innerException)
        {
            CorruptedSnapshot = snapshot;
        }
    }
}
