using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 回溯诊断报告
    /// Diagnostic report for backtracking failures
    /// 对应需求: 4.3, 8.3
    /// </summary>
    public class BacktrackingDiagnosticReport
    {
        /// <summary>
        /// 是否找到完整解
        /// </summary>
        public bool HasCompleteSolution { get; set; }

        /// <summary>
        /// 未分配时段列表
        /// </summary>
        public List<UnassignedSlotInfo> UnassignedSlots { get; set; } = new();

        /// <summary>
        /// 回溯历史（最近的N次回溯）
        /// </summary>
        public List<string> BacktrackHistory { get; set; } = new();

        /// <summary>
        /// 失败原因分析
        /// </summary>
        public string FailureAnalysis { get; set; } = string.Empty;

        /// <summary>
        /// 回溯统计信息
        /// </summary>
        public BacktrackingStatistics? Statistics { get; set; }

        /// <summary>
        /// 建议措施
        /// </summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// 报告生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 总时段数
        /// </summary>
        public int TotalSlots { get; set; }

        /// <summary>
        /// 已分配时段数
        /// </summary>
        public int AssignedSlots { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public double CompletionRate
        {
            get
            {
                if (TotalSlots == 0) return 0.0;
                return (double)AssignedSlots / TotalSlots * 100.0;
            }
        }

        /// <summary>
        /// 添加未分配时段
        /// </summary>
        public void AddUnassignedSlot(UnassignedSlotInfo slot)
        {
            UnassignedSlots.Add(slot);
        }

        /// <summary>
        /// 添加回溯历史记录
        /// </summary>
        public void AddBacktrackHistory(string entry)
        {
            BacktrackHistory.Add(entry);
        }

        /// <summary>
        /// 添加建议
        /// </summary>
        public void AddRecommendation(string recommendation)
        {
            Recommendations.Add(recommendation);
        }

        /// <summary>
        /// 生成失败原因分析
        /// </summary>
        public void GenerateFailureAnalysis()
        {
            var sb = new StringBuilder();
            sb.AppendLine("失败原因分析:");

            if (UnassignedSlots.Count == 0)
            {
                sb.AppendLine("  所有时段已成功分配");
                FailureAnalysis = sb.ToString();
                return;
            }

            // 分析未分配时段的分布
            var byPosition = UnassignedSlots.GroupBy(s => s.PositionName).ToList();
            sb.AppendLine($"  未分配时段总数: {UnassignedSlots.Count}");
            sb.AppendLine($"  涉及哨位数: {byPosition.Count}");

            // 按哨位统计
            sb.AppendLine("\n  按哨位分布:");
            foreach (var group in byPosition.OrderByDescending(g => g.Count()))
            {
                sb.AppendLine($"    {group.Key}: {group.Count()} 个时段");
            }

            // 分析候选人员不足的情况
            var noCandidates = UnassignedSlots.Where(s => s.CandidateCount == 0).ToList();
            if (noCandidates.Any())
            {
                sb.AppendLine($"\n  无候选人员的时段: {noCandidates.Count}");
                sb.AppendLine("    可能原因: 技能不匹配、人员不可用、约束冲突");
            }

            // 分析候选人员少的情况
            var fewCandidates = UnassignedSlots.Where(s => s.CandidateCount > 0 && s.CandidateCount <= 2).ToList();
            if (fewCandidates.Any())
            {
                sb.AppendLine($"\n  候选人员不足的时段 (≤2人): {fewCandidates.Count}");
                sb.AppendLine("    可能原因: 人员紧张、约束严格");
            }

            // 回溯深度分析
            if (Statistics != null)
            {
                sb.AppendLine($"\n  回溯统计:");
                sb.AppendLine($"    总回溯次数: {Statistics.TotalBacktracks}");
                sb.AppendLine($"    最大深度: {Statistics.MaxDepthReached}");
                sb.AppendLine($"    成功率: {Statistics.SuccessRate:F1}%");
            }

            FailureAnalysis = sb.ToString();
        }

        /// <summary>
        /// 生成建议措施
        /// </summary>
        public void GenerateRecommendations()
        {
            Recommendations.Clear();

            if (UnassignedSlots.Count == 0)
            {
                Recommendations.Add("排班已完成，无需调整");
                return;
            }

            // 基于候选人员数量的建议
            var noCandidates = UnassignedSlots.Where(s => s.CandidateCount == 0).Count();
            if (noCandidates > 0)
            {
                Recommendations.Add($"有 {noCandidates} 个时段无候选人员，建议:");
                Recommendations.Add("  - 检查技能配置是否合理");
                Recommendations.Add("  - 增加具备相关技能的人员");
                Recommendations.Add("  - 放宽部分约束条件");
            }

            // 基于回溯深度的建议
            if (Statistics != null && Statistics.MaxDepthReached >= Statistics.TotalBacktracks * 0.8)
            {
                Recommendations.Add("回溯深度接近上限，建议:");
                Recommendations.Add("  - 增加最大回溯深度配置");
                Recommendations.Add("  - 优化人员和哨位配置");
            }

            // 基于完成率的建议
            if (CompletionRate < 50)
            {
                Recommendations.Add("完成率较低，建议:");
                Recommendations.Add("  - 增加人员数量");
                Recommendations.Add("  - 减少哨位数量或时段");
                Recommendations.Add("  - 检查约束配置是否过于严格");
            }
            else if (CompletionRate < 90)
            {
                Recommendations.Add("完成率中等，建议:");
                Recommendations.Add("  - 微调人员技能配置");
                Recommendations.Add("  - 调整部分软约束权重");
            }

            // 基于内存使用的建议
            if (Statistics != null && Statistics.MemoryPressureEvents > 0)
            {
                Recommendations.Add("检测到内存压力，建议:");
                Recommendations.Add("  - 减少最大回溯深度");
                Recommendations.Add("  - 增加快照保存间隔");
                Recommendations.Add("  - 禁用路径记忆功能");
            }
        }

        /// <summary>
        /// 获取格式化的诊断报告
        /// </summary>
        public string GetFormattedReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 回溯诊断报告 ===");
            sb.AppendLine($"生成时间: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"完成状态: {(HasCompleteSolution ? "完整解" : "部分解")}");
            sb.AppendLine($"完成率: {CompletionRate:F1}% ({AssignedSlots}/{TotalSlots})");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(FailureAnalysis))
            {
                sb.AppendLine(FailureAnalysis);
                sb.AppendLine();
            }

            if (UnassignedSlots.Any())
            {
                sb.AppendLine("未分配时段详情:");
                foreach (var slot in UnassignedSlots.Take(10)) // 只显示前10个
                {
                    sb.AppendLine($"  - {slot}");
                }
                if (UnassignedSlots.Count > 10)
                {
                    sb.AppendLine($"  ... 还有 {UnassignedSlots.Count - 10} 个未分配时段");
                }
                sb.AppendLine();
            }

            if (BacktrackHistory.Any())
            {
                sb.AppendLine("回溯历史 (最近10次):");
                foreach (var entry in BacktrackHistory.TakeLast(10))
                {
                    sb.AppendLine($"  - {entry}");
                }
                sb.AppendLine();
            }

            if (Statistics != null)
            {
                sb.AppendLine("回溯统计:");
                sb.AppendLine(Statistics.GetDetailedSummary());
                sb.AppendLine();
            }

            if (Recommendations.Any())
            {
                sb.AppendLine("建议措施:");
                foreach (var rec in Recommendations)
                {
                    sb.AppendLine($"  {rec}");
                }
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return GetFormattedReport();
        }
    }
}
