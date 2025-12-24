using System;

namespace AutoScheduling3.SchedulingEngine.Exceptions
{
    /// <summary>
    /// 回溯深度超限异常
    /// Exception thrown when backtracking depth exceeds the configured maximum
    /// </summary>
    public class BacktrackDepthExceededException : Exception
    {
        /// <summary>
        /// 配置的最大回溯深度
        /// </summary>
        public int MaxDepth { get; }

        /// <summary>
        /// 当前回溯深度
        /// </summary>
        public int CurrentDepth { get; }

        public BacktrackDepthExceededException(int maxDepth, int currentDepth)
            : base($"回溯深度 {currentDepth} 超过最大限制 {maxDepth}")
        {
            MaxDepth = maxDepth;
            CurrentDepth = currentDepth;
        }

        public BacktrackDepthExceededException(int maxDepth, int currentDepth, Exception innerException)
            : base($"回溯深度 {currentDepth} 超过最大限制 {maxDepth}", innerException)
        {
            MaxDepth = maxDepth;
            CurrentDepth = currentDepth;
        }
    }
}
