using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 哨位模糊匹配器 - 提供增强的多层匹配策略的哨位搜索功能
/// 支持完整拼音匹配、编辑距离容错、精细化分数计算
/// </summary>
public static class PositionFuzzyMatcher
{
    /// <summary>
    /// 执行模糊匹配搜索
    /// </summary>
    /// <param name="query">搜索查询文本</param>
    /// <param name="candidates">候选哨位列表</param>
    /// <param name="options">匹配选项</param>
    /// <returns>匹配结果列表，按分数降序排序</returns>
    public static List<PositionMatchResult> Match(
        string query,
        IEnumerable<PositionScheduleData> candidates,
        FuzzyMatchOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // 空查询返回所有候选哨位
            return candidates.Select(p => new PositionMatchResult
            {
                Position = p,
                Score = 0,
                Type = MatchType.None
            }).ToList();
        }

        options ??= new FuzzyMatchOptions();
        var results = new List<PositionMatchResult>();

        // 标准化查询文本
        var normalizedQuery = options.CaseSensitive ? query : query.ToLower();

        foreach (var candidate in candidates)
        {
            var matchResult = MatchSingle(normalizedQuery, candidate, options);
            // 只添加有匹配的结果（分数 > 0）
            if (matchResult.Score > 0 && matchResult.Score >= options.MinScore)
            {
                results.Add(matchResult);
            }
        }

        // 按分数降序排序，分数相同时按哨位名称排序
        var sortedResults = results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Position.PositionName)
            .Take(options.MaxResults)
            .ToList();

        // 输出排序后的结果
        System.Diagnostics.Debug.WriteLine($"PositionFuzzyMatcher: 搜索 '{query}' 返回 {sortedResults.Count} 个结果:");
        foreach (var result in sortedResults.Take(5))  // 只显示前5个
        {
            System.Diagnostics.Debug.WriteLine($"  - {result.Position.PositionName} (分数: {result.Score}, 类型: {result.Type})");
        }

        return sortedResults;
    }

    /// <summary>
    /// 匹配单个候选哨位 - 使用增强的多层匹配策略
    /// </summary>
    private static PositionMatchResult MatchSingle(
        string normalizedQuery,
        PositionScheduleData candidate,
        FuzzyMatchOptions options)
    {
        var name = options.CaseSensitive ? candidate.PositionName : candidate.PositionName.ToLower();
        var baseScore = 0;
        var matchType = MatchType.None;
        var matchPosition = 0;
        var editDistance = 0;

        // 1. 完全匹配（基础分：100）
        if (name == normalizedQuery)
        {
            baseScore = 100;
            matchType = MatchType.ExactMatch;
            matchPosition = 0;
        }
        // 2. 前缀匹配（基础分：90）
        else if (name.StartsWith(normalizedQuery))
        {
            baseScore = 90;
            matchType = MatchType.PrefixMatch;
            matchPosition = 0;
        }
        // 3. 子串匹配（基础分：70）
        else if (name.Contains(normalizedQuery))
        {
            baseScore = 70;
            matchType = MatchType.SubstringMatch;
            matchPosition = name.IndexOf(normalizedQuery);
        }
        // 4. 拼音完全匹配（基础分：85）
        else if (options.EnablePinyinMatch && options.EnableFullPinyinMatch && MatchFullPinyinExact(normalizedQuery, candidate.PositionName))
        {
            baseScore = 85;
            matchType = MatchType.PinyinExactMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音完全匹配成功: '{normalizedQuery}' 匹配 '{candidate.PositionName}'");
        }
        // 5. 拼音前缀匹配（基础分：75）
        else if (options.EnablePinyinMatch && options.EnableFullPinyinMatch && MatchFullPinyinPrefix(normalizedQuery, candidate.PositionName))
        {
            baseScore = 75;
            matchType = MatchType.PinyinPrefixMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音前缀匹配成功: '{normalizedQuery}' 匹配 '{candidate.PositionName}'");
        }
        // 6. 拼音首字母完全匹配（基础分：65）
        else if (options.EnablePinyinMatch && options.EnablePinyinInitialsMatch && MatchPinyinInitialsExact(normalizedQuery, candidate.PositionName))
        {
            baseScore = 65;
            matchType = MatchType.PinyinInitialsExactMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音首字母完全匹配成功: '{normalizedQuery}' 匹配 '{candidate.PositionName}'");
        }
        // 7. 拼音首字母前缀匹配（基础分：55）
        else if (options.EnablePinyinMatch && options.EnablePinyinInitialsMatch && MatchPinyinInitialsPrefix(normalizedQuery, candidate.PositionName))
        {
            baseScore = 55;
            matchType = MatchType.PinyinInitialsPrefixMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音首字母前缀匹配成功: '{normalizedQuery}' 匹配 '{candidate.PositionName}'");
        }
        // 8. 编辑距离模糊匹配（基础分：30-50，根据编辑距离动态计算）
        else if (options.EnableEditDistanceMatch)
        {
            editDistance = CalculateEditDistance(normalizedQuery, name);
            int maxEditDistance = options.MaxEditDistance;

            // 根据查询长度自动调整编辑距离阈值
            if (options.AutoAdjustEditDistance)
            {
                maxEditDistance = normalizedQuery.Length <= 3 ? 1 : 2;
            }

            if (editDistance <= maxEditDistance && editDistance > 0)
            {
                baseScore = 50 - (editDistance * 10);
                matchType = MatchType.EditDistanceMatch;
                matchPosition = 0;
                System.Diagnostics.Debug.WriteLine($"编辑距离匹配成功: '{normalizedQuery}' 匹配 '{candidate.PositionName}' (距离: {editDistance})");
            }
        }
        // 9. 序列模糊匹配（基础分：25）
        else if (options.EnableFuzzyMatch)
        {
            var fuzzyMatch = FuzzySequenceMatch(normalizedQuery, name);
            if (fuzzyMatch)
            {
                baseScore = 25;
                matchType = MatchType.SequenceFuzzyMatch;
                System.Diagnostics.Debug.WriteLine($"序列模糊匹配成功: '{normalizedQuery}' 匹配 '{candidate.PositionName}'");
            }
        }

        // 计算精细化最终分数
        int finalScore = baseScore;
        if (baseScore > 0)
        {
            finalScore = CalculateFinalScore(baseScore, normalizedQuery, name, matchPosition, options);
        }

        // 计算相似度
        double similarity = 0.0;
        if (finalScore > 0)
        {
            similarity = finalScore / 100.0;
        }

        return new PositionMatchResult
        {
            Position = candidate,
            Score = finalScore,
            Type = matchType,
            EditDistance = editDistance,
            Similarity = similarity
        };
    }

    /// <summary>
    /// 模糊序列匹配 - 检查查询字符是否按顺序出现在目标字符串中
    /// </summary>
    private static bool FuzzySequenceMatch(string query, string target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
        {
            return false;
        }

        int queryIndex = 0;
        int targetIndex = 0;

        while (queryIndex < query.Length && targetIndex < target.Length)
        {
            if (query[queryIndex] == target[targetIndex])
            {
                queryIndex++;
            }
            targetIndex++;
        }

        return queryIndex == query.Length;
    }

    /// <summary>
    /// 计算Levenshtein编辑距离
    /// </summary>
    private static int CalculateEditDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return target?.Length ?? 0;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        int sourceLength = source.Length;
        int targetLength = target.Length;

        // 创建距离矩阵
        int[,] distance = new int[sourceLength + 1, targetLength + 1];

        // 初始化第一行和第一列
        for (int i = 0; i <= sourceLength; i++)
        {
            distance[i, 0] = i;
        }

        for (int j = 0; j <= targetLength; j++)
        {
            distance[0, j] = j;
        }

        // 动态规划计算编辑距离
        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // 删除
                        distance[i, j - 1] + 1),     // 插入
                    distance[i - 1, j - 1] + cost);  // 替换
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// 完整拼音完全匹配
    /// </summary>
    private static bool MatchFullPinyinExact(string query, string name)
    {
        try
        {
            var fullPinyin = PinyinHelper.GetFullPinyin(name);
            if (string.IsNullOrEmpty(fullPinyin))
            {
                return false;
            }

            return fullPinyin.Equals(query, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PositionFuzzyMatcher: 完整拼音匹配失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 完整拼音前缀匹配
    /// </summary>
    private static bool MatchFullPinyinPrefix(string query, string name)
    {
        try
        {
            var fullPinyin = PinyinHelper.GetFullPinyin(name);
            if (string.IsNullOrEmpty(fullPinyin))
            {
                return false;
            }

            return fullPinyin.StartsWith(query, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PositionFuzzyMatcher: 完整拼音前缀匹配失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 拼音首字母完全匹配
    /// </summary>
    private static bool MatchPinyinInitialsExact(string query, string name)
    {
        try
        {
            var initials = PinyinHelper.GetPinyinInitials(name);
            if (string.IsNullOrEmpty(initials))
            {
                return false;
            }

            return initials.Equals(query, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PositionFuzzyMatcher: 拼音首字母完全匹配失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 拼音首字母前缀匹配
    /// </summary>
    private static bool MatchPinyinInitialsPrefix(string query, string name)
    {
        try
        {
            var initials = PinyinHelper.GetPinyinInitials(name);
            if (string.IsNullOrEmpty(initials))
            {
                return false;
            }

            return initials.StartsWith(query, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PositionFuzzyMatcher: 拼音首字母前缀匹配失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 计算长度相似度
    /// </summary>
    private static double CalculateLengthSimilarity(int queryLength, int targetLength)
    {
        if (queryLength == 0 && targetLength == 0)
        {
            return 1.0;
        }

        int maxLength = Math.Max(queryLength, targetLength);
        if (maxLength == 0)
        {
            return 1.0;
        }

        return 1.0 - (double)Math.Abs(queryLength - targetLength) / maxLength;
    }

    /// <summary>
    /// 计算匹配位置加成
    /// </summary>
    private static int CalculatePositionBonus(int position, int targetLength)
    {
        if (targetLength == 0)
        {
            return 0;
        }

        // 位置越靠前，加成越高（最多+3分）
        double positionRatio = (double)position / targetLength;
        if (positionRatio <= 0.2)
        {
            return 3;
        }
        else if (positionRatio <= 0.5)
        {
            return 2;
        }
        else if (positionRatio <= 0.8)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 计算连续匹配加成
    /// </summary>
    private static int CalculateContinuousMatchBonus(string query, string target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
        {
            return 0;
        }

        int maxContinuous = 0;
        int currentContinuous = 0;

        int queryIndex = 0;
        for (int i = 0; i < target.Length && queryIndex < query.Length; i++)
        {
            if (target[i] == query[queryIndex])
            {
                currentContinuous++;
                queryIndex++;
                maxContinuous = Math.Max(maxContinuous, currentContinuous);
            }
            else
            {
                currentContinuous = 0;
            }
        }

        // 连续匹配字符越多，加成越高（最多+2分）
        if (maxContinuous >= query.Length)
        {
            return 2;
        }
        else if (maxContinuous >= query.Length / 2)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 计算精细化最终分数
    /// </summary>
    private static int CalculateFinalScore(
        int baseScore,
        string query,
        string target,
        int matchPosition,
        FuzzyMatchOptions options)
    {
        int finalScore = baseScore;

        // 长度相似度加成
        if (options.EnableLengthSimilarityBonus)
        {
            double lengthSimilarity = CalculateLengthSimilarity(query.Length, target.Length);
            finalScore += (int)(lengthSimilarity * 5);
        }

        // 匹配位置加成
        if (options.EnablePositionBonus)
        {
            int positionBonus = CalculatePositionBonus(matchPosition, target.Length);
            finalScore += positionBonus;
        }

        // 连续匹配加成
        if (options.EnableContinuousMatchBonus)
        {
            int continuousBonus = CalculateContinuousMatchBonus(query, target);
            finalScore += continuousBonus;
        }

        return finalScore;
    }
}

/// <summary>
/// 哨位匹配结果
/// </summary>
public class PositionMatchResult
{
    /// <summary>
    /// 匹配的哨位
    /// </summary>
    public PositionScheduleData Position { get; set; } = null!;

    /// <summary>
    /// 匹配分数（越高越匹配，0-100+）
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 匹配类型
    /// </summary>
    public MatchType Type { get; set; }

    /// <summary>
    /// 编辑距离（用于编辑距离匹配）
    /// </summary>
    public int EditDistance { get; set; }

    /// <summary>
    /// 相似度（0-1之间）
    /// </summary>
    public double Similarity { get; set; }
}
