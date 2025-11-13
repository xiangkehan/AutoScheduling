using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoScheduling3.Services.ImportExport.Monitoring
{
    /// <summary>
    /// 监控导入操作的性能指标
    /// Monitors performance metrics for import operations
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly Stopwatch _totalTimer;
        private readonly Dictionary<string, Stopwatch> _activeOperations;
        private readonly Dictionary<string, TimeSpan> _operationTimings;
        private readonly object _lock = new object();

        /// <summary>
        /// 总计时器
        /// Total timer for the entire operation
        /// </summary>
        public Stopwatch TotalTimer => _totalTimer;

        /// <summary>
        /// 各操作的计时结果
        /// Timing results for each operation
        /// </summary>
        public Dictionary<string, TimeSpan> OperationTimings
        {
            get
            {
                lock (_lock)
                {
                    return new Dictionary<string, TimeSpan>(_operationTimings);
                }
            }
        }

        public PerformanceMonitor()
        {
            _totalTimer = new Stopwatch();
            _activeOperations = new Dictionary<string, Stopwatch>();
            _operationTimings = new Dictionary<string, TimeSpan>();
        }

        /// <summary>
        /// 开始监控指定操作
        /// Start monitoring a specific operation
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public void StartOperation(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
            {
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
            }

            lock (_lock)
            {
                // 如果是 "Total" 操作，启动总计时器
                if (operationName == "Total" && !_totalTimer.IsRunning)
                {
                    _totalTimer.Start();
                }

                // 如果操作已经在运行，先结束它
                if (_activeOperations.ContainsKey(operationName))
                {
                    EndOperation(operationName);
                }

                // 创建并启动新的计时器
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                _activeOperations[operationName] = stopwatch;
            }
        }

        /// <summary>
        /// 结束监控指定操作
        /// End monitoring a specific operation
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public void EndOperation(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
            {
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
            }

            lock (_lock)
            {
                if (_activeOperations.TryGetValue(operationName, out var stopwatch))
                {
                    stopwatch.Stop();

                    // 累加时间（支持同一操作多次调用）
                    if (_operationTimings.ContainsKey(operationName))
                    {
                        _operationTimings[operationName] += stopwatch.Elapsed;
                    }
                    else
                    {
                        _operationTimings[operationName] = stopwatch.Elapsed;
                    }

                    _activeOperations.Remove(operationName);
                }

                // 如果是 "Total" 操作，停止总计时器
                if (operationName == "Total" && _totalTimer.IsRunning)
                {
                    _totalTimer.Stop();
                }
            }
        }

        /// <summary>
        /// 获取指定操作的耗时
        /// Get the elapsed time for a specific operation
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>耗时，如果操作不存在则返回 TimeSpan.Zero</returns>
        public TimeSpan GetOperationTime(string operationName)
        {
            lock (_lock)
            {
                return _operationTimings.TryGetValue(operationName, out var time) ? time : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 重置所有计时器
        /// Reset all timers
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _totalTimer.Reset();
                _activeOperations.Clear();
                _operationTimings.Clear();
            }
        }

        /// <summary>
        /// 生成性能报告
        /// Generate a performance report
        /// </summary>
        /// <param name="totalRecords">处理的总记录数</param>
        /// <returns>性能报告</returns>
        public PerformanceReport GenerateReport(int totalRecords)
        {
            return PerformanceReport.Generate(this, totalRecords);
        }
    }
}
