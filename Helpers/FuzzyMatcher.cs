using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 模糊匹配器 - 提供增强的多层匹配策略的人员搜索功能
/// 支持完整拼音匹配、编辑距离容错、精细化分数计算
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// 执行模糊匹配搜索
    /// </summary>
    /// <param name="query">搜索查询文本</param>
    /// <param name="candidates">候选人员列表</param>
    /// <param name="options">匹配选项</param>
    /// <returns>匹配结果列表，按分数降序排序</returns>
    public static List<MatchResult> Match(
        string query,
        IEnumerable<PersonnelDto> candidates,
        FuzzyMatchOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // 空查询返回所有候选人
            return candidates.Select(p => new MatchResult
            {
                Personnel = p,
                Score = 0,
                Type = MatchType.None
            }).ToList();
        }

        options ??= new FuzzyMatchOptions();
        var results = new List<MatchResult>();

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

        // 按分数降序排序，分数相同时按姓名排序
        var sortedResults = results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Personnel.Name)
            .Take(options.MaxResults)
            .ToList();

        // 输出排序后的结果
        System.Diagnostics.Debug.WriteLine($"FuzzyMatcher: 搜索 '{query}' 返回 {sortedResults.Count} 个结果:");
        foreach (var result in sortedResults.Take(5))  // 只显示前5个
        {
            System.Diagnostics.Debug.WriteLine($"  - {result.Personnel.Name} (分数: {result.Score}, 类型: {result.Type})");
        }

        return sortedResults;
    }

    /// <summary>
    /// 匹配单个候选人 - 使用增强的多层匹配策略
    /// </summary>
    private static MatchResult MatchSingle(
        string normalizedQuery,
        PersonnelDto candidate,
        FuzzyMatchOptions options)
    {
        var name = options.CaseSensitive ? candidate.Name : candidate.Name.ToLower();
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
        else if (options.EnablePinyinMatch && options.EnableFullPinyinMatch && MatchFullPinyinExact(normalizedQuery, candidate.Name))
        {
            baseScore = 85;
            matchType = MatchType.PinyinExactMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音完全匹配成功: '{normalizedQuery}' 匹配 '{candidate.Name}'");
        }
        // 5. 拼音前缀匹配（基础分：75）
        else if (options.EnablePinyinMatch && options.EnableFullPinyinMatch && MatchFullPinyinPrefix(normalizedQuery, candidate.Name))
        {
            baseScore = 75;
            matchType = MatchType.PinyinPrefixMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音前缀匹配成功: '{normalizedQuery}' 匹配 '{candidate.Name}'");
        }
        // 6. 拼音首字母完全匹配（基础分：65）
        else if (options.EnablePinyinMatch && options.EnablePinyinInitialsMatch && MatchPinyinInitialsExact(normalizedQuery, candidate.Name))
        {
            baseScore = 65;
            matchType = MatchType.PinyinInitialsExactMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音首字母完全匹配成功: '{normalizedQuery}' 匹配 '{candidate.Name}'");
        }
        // 7. 拼音首字母前缀匹配（基础分：55）
        else if (options.EnablePinyinMatch && options.EnablePinyinInitialsMatch && MatchPinyinInitialsPrefix(normalizedQuery, candidate.Name))
        {
            baseScore = 55;
            matchType = MatchType.PinyinInitialsPrefixMatch;
            matchPosition = 0;
            System.Diagnostics.Debug.WriteLine($"拼音首字母前缀匹配成功: '{normalizedQuery}' 匹配 '{candidate.Name}'");
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
                System.Diagnostics.Debug.WriteLine($"编辑距离匹配成功: '{normalizedQuery}' 匹配 '{candidate.Name}' (距离: {editDistance})");
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
                System.Diagnostics.Debug.WriteLine($"序列模糊匹配成功: '{normalizedQuery}' 匹配 '{candidate.Name}'");
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

        return new MatchResult
        {
            Personnel = candidate,
            Score = finalScore,
            Type = matchType,
            EditDistance = editDistance,
            Similarity = similarity
        };
    }

    /// <summary>
    /// 拼音首字母匹配
    /// </summary>
    private static bool MatchPinyinInitials(string query, string name)
    {
        try
        {
            var initials = PinyinHelper.GetPinyinInitials(name);
            if (string.IsNullOrEmpty(initials))
            {
                return false;
            }

            // 转换为小写进行比较
            var lowerInitials = initials.ToLower();
            var lowerQuery = query.ToLower();

            // 支持完全匹配或前缀匹配
            return lowerInitials == lowerQuery || lowerInitials.StartsWith(lowerQuery);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FuzzyMatcher: 拼音匹配失败: {ex.Message}");
            return false;
        }
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
    /// 计算匹配分数（保留用于未来扩展）
    /// </summary>
    private static int CalculateScore(string query, string target, MatchType matchType)
    {
        return matchType switch
        {
            MatchType.ExactMatch => 100,
            MatchType.PrefixMatch => 90,
            MatchType.SubstringMatch => 70,
            MatchType.PinyinMatch => 60,
            MatchType.FuzzyMatch => 40,
            _ => 0
        };
    }

    /// <summary>
    /// 标准化文本 - 去除空格和特殊字符，转小写
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="removePunctuation">是否去除标点符号</param>
    /// <returns>标准化后的文本</returns>
    private static string NormalizeText(string text, bool removePunctuation = true)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var result = new StringBuilder();
        foreach (char c in text)
        {
            // 跳过空格
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            // 可选：跳过标点符号
            if (removePunctuation && char.IsPunctuation(c))
            {
                continue;
            }

            // 转小写并添加
            result.Append(char.ToLower(c));
        }

        return result.ToString();
    }

    /// <summary>
    /// 计算Levenshtein编辑距离
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="target">目标字符串</param>
    /// <returns>编辑距离（需要的最小编辑操作数）</returns>
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
            System.Diagnostics.Debug.WriteLine($"FuzzyMatcher: 完整拼音匹配失败: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"FuzzyMatcher: 完整拼音前缀匹配失败: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"FuzzyMatcher: 拼音首字母完全匹配失败: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"FuzzyMatcher: 拼音首字母前缀匹配失败: {ex.Message}");
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
/// 匹配结果
/// </summary>
public class MatchResult
{
    /// <summary>
    /// 匹配的人员
    /// </summary>
    public PersonnelDto Personnel { get; set; } = null!;

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

/// <summary>
/// 匹配类型
/// </summary>
public enum MatchType
{
    /// <summary>
    /// 无匹配
    /// </summary>
    None = 0,

    /// <summary>
    /// 完全匹配
    /// </summary>
    ExactMatch = 1,

    /// <summary>
    /// 前缀匹配
    /// </summary>
    PrefixMatch = 2,

    /// <summary>
    /// 子串匹配
    /// </summary>
    SubstringMatch = 3,

    /// <summary>
    /// 拼音完全匹配
    /// </summary>
    PinyinExactMatch = 4,

    /// <summary>
    /// 拼音前缀匹配
    /// </summary>
    PinyinPrefixMatch = 5,

    /// <summary>
    /// 拼音首字母完全匹配
    /// </summary>
    PinyinInitialsExactMatch = 6,

    /// <summary>
    /// 拼音首字母前缀匹配
    /// </summary>
    PinyinInitialsPrefixMatch = 7,

    /// <summary>
    /// 编辑距离模糊匹配
    /// </summary>
    EditDistanceMatch = 8,

    /// <summary>
    /// 序列模糊匹配
    /// </summary>
    SequenceFuzzyMatch = 9,

    /// <summary>
    /// 拼音匹配（向后兼容，已废弃）
    /// </summary>
    [Obsolete("请使用 PinyinInitialsExactMatch 或 PinyinInitialsPrefixMatch")]
    PinyinMatch = 10,

    /// <summary>
    /// 模糊匹配（向后兼容，已废弃）
    /// </summary>
    [Obsolete("请使用 SequenceFuzzyMatch")]
    FuzzyMatch = 11
}

/// <summary>
/// 模糊匹配选项
/// </summary>
public class FuzzyMatchOptions
{
    // ========== 基础选项 ==========

    /// <summary>
    /// 是否启用序列模糊匹配
    /// </summary>
    public bool EnableFuzzyMatch { get; set; } = true;

    /// <summary>
    /// 是否启用拼音匹配
    /// </summary>
    public bool EnablePinyinMatch { get; set; } = true;

    /// <summary>
    /// 是否启用编辑距离匹配
    /// </summary>
    public bool EnableEditDistanceMatch { get; set; } = true;

    /// <summary>
    /// 是否区分大小写
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// 最大返回结果数
    /// </summary>
    public int MaxResults { get; set; } = 50;

    /// <summary>
    /// 最小匹配分数
    /// </summary>
    public int MinScore { get; set; } = 0;

    // ========== 编辑距离选项 ==========

    /// <summary>
    /// 最大编辑距离（默认2）
    /// </summary>
    public int MaxEditDistance { get; set; } = 2;

    /// <summary>
    /// 是否根据查询长度自动调整编辑距离阈值
    /// 长度≤3时，最大编辑距离=1；长度≥4时，最大编辑距离=2
    /// </summary>
    public bool AutoAdjustEditDistance { get; set; } = true;

    // ========== 拼音选项 ==========

    /// <summary>
    /// 是否启用完整拼音匹配
    /// </summary>
    public bool EnableFullPinyinMatch { get; set; } = true;

    /// <summary>
    /// 是否启用拼音首字母匹配
    /// </summary>
    public bool EnablePinyinInitialsMatch { get; set; } = true;

    // ========== 分数调整选项 ==========

    /// <summary>
    /// 是否启用长度相似度加成（最多+5分）
    /// </summary>
    public bool EnableLengthSimilarityBonus { get; set; } = true;

    /// <summary>
    /// 是否启用匹配位置加成（最多+3分）
    /// </summary>
    public bool EnablePositionBonus { get; set; } = true;

    /// <summary>
    /// 是否启用连续匹配加成（最多+2分）
    /// </summary>
    public bool EnableContinuousMatchBonus { get; set; } = true;
}
