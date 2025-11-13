using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoScheduling3.Services.ImportExport.Monitoring
{
    /// <summary>
    /// 性能报告
    /// Performance report for import operations
    /// </summary>
    public class PerformanceReport
    {
        /// <summary>
        /// 总执行时间
        /// Total duration of the operation
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// 各操作的时间分解
        /// Breakdown of time spent on each operation
        /// </summary>
        public Dictionary<string, TimeSpan> OperationBreakdown { get; set; }

        /// <summary>
        /// 每秒处理的记录数
        /// Records processed per second
        /// </summary>
        public double RecordsPerSecond { get; set; }

        /// <summary>
        /// 摘要字符串
        /// Summary string
        /// </summary>
        public string Summary { get; set; }

        public PerformanceReport()
        {
            OperationBreakdown = new Dictionary<string, TimeSpan>();
            Summary = string.Empty;
        }

        /// <summary>
        /// 生成性能报告
        /// Generate a performance report from a PerformanceMonitor
        /// </summary>
        /// <param name="monitor">性能监控器</param>
        /// <param name="totalRecords">处理的总记录数</param>
        /// <returns>性能报告</returns>
        public static PerformanceReport Generate(PerformanceMonitor monitor, int totalRecords)
        {
            if (monitor == null)
            {
                throw new ArgumentNullException(nameof(monitor));
            }

            var report = new PerformanceReport
            {
                TotalDuration = monitor.TotalTimer.Elapsed,
                OperationBreakdown = monitor.OperationTimings
            };

            // 计算每秒处理记录数
            if (report.TotalDuration.TotalSeconds > 0)
            {
                report.RecordsPerSecond = totalRecords / report.TotalDuration.TotalSeconds;
            }
            else
            {
                report.RecordsPerSecond = 0;
            }

            // 生成摘要字符串
            report.Summary = GenerateSummary(report, totalRecords);

            return report;
        }

        /// <summary>
        /// 生成摘要字符串
        /// Generate a summary string
        /// </summary>
        private static string GenerateSummary(PerformanceReport report, int totalRecords)
        {
            var sb = new StringBuilder();

            // 总体信息
            sb.AppendLine($"Total Duration: {FormatTimeSpan(report.TotalDuration)}");
            sb.AppendLine($"Total Records: {totalRecords:N0}");
            sb.AppendLine($"Records/Second: {report.RecordsPerSecond:F2}");

            // 操作时间分解
            if (report.OperationBreakdown.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Operation Breakdown:");

                // 按时间降序排序
                var sortedOperations = report.OperationBreakdown
                    .Where(kvp => kvp.Key != "Total") // 排除 Total，因为已经显示了
                    .OrderByDescending(kvp => kvp.Value)
                    .ToList();

                foreach (var operation in sortedOperations)
                {
                    var percentage = report.TotalDuration.TotalMilliseconds > 0
                        ? (operation.Value.TotalMilliseconds / report.TotalDuration.TotalMilliseconds) * 100
                        : 0;

                    sb.AppendLine($"  - {operation.Key}: {FormatTimeSpan(operation.Value)} ({percentage:F1}%)");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 格式化时间跨度为易读字符串
        /// Format a TimeSpan to a readable string
        /// </summary>
        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1)
            {
                return $"{timeSpan.TotalMilliseconds:F0}ms";
            }
            else if (timeSpan.TotalMinutes < 1)
            {
                return $"{timeSpan.TotalSeconds:F2}s";
            }
            else if (timeSpan.TotalHours < 1)
            {
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
        }

        /// <summary>
        /// 获取格式化的摘要字符串（用于日志）
        /// Get a formatted summary string for logging
        /// </summary>
        public override string ToString()
        {
            return Summary;
        }
    }
}
