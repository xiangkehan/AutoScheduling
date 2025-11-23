using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 模糊匹配器 - 提供多层匹配策略的人员搜索功能
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
            if (matchResult.Score >= options.MinScore)
            {
                results.Add(matchResult);
            }
        }

        // 按分数降序排序，分数相同时按姓名排序
        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Personnel.Name)
            .Take(options.MaxResults)
            .ToList();
    }

    /// <summary>
    /// 匹配单个候选人
    /// </summary>

    private static MatchResult MatchSingle(
        string normalizedQuery,
        PersonnelDto candidate,
        FuzzyMatchOptions options)
    {
        var name = options.CaseSensitive ? candidate.Name : candidate.Name.ToLower();
        var score = 0;
        var matchType = MatchType.None;

        // 1. 完全匹配（分数：100）
        if (name == normalizedQuery)
        {
            score = 100;
            matchType = MatchType.ExactMatch;
        }
        // 2. 前缀匹配（分数：90）
        else if (name.StartsWith(normalizedQuery))
        {
            score = 90;
            matchType = MatchType.PrefixMatch;
        }
        // 3. 子串匹配（分数：70）
        else if (name.Contains(normalizedQuery))
        {
            score = 70;
            matchType = MatchType.SubstringMatch;
        }
        // 4. 拼音首字母匹配（分数：60）
        else if (options.EnablePinyinMatch && MatchPinyinInitials(normalizedQuery, candidate.Name))
        {
            score = 60;
            matchType = MatchType.PinyinMatch;
        }
        // 5. 模糊匹配（分数：40）
        else if (options.EnableFuzzyMatch && FuzzySequenceMatch(normalizedQuery, name))
        {
            score = 40;
            matchType = MatchType.FuzzyMatch;
        }

        return new MatchResult
        {
            Personnel = candidate,
            Score = score,
            Type = matchType
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
            return !string.IsNullOrEmpty(initials) && initials.Contains(query);
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
    /// 匹配分数（越高越匹配）
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 匹配类型
    /// </summary>
    public MatchType Type { get; set; }
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
    /// 拼音匹配
    /// </summary>
    PinyinMatch = 4,

    /// <summary>
    /// 模糊匹配
    /// </summary>
    FuzzyMatch = 5
}

/// <summary>
/// 模糊匹配选项
/// </summary>
public class FuzzyMatchOptions
{
    /// <summary>
    /// 是否启用模糊匹配
    /// </summary>
    public bool EnableFuzzyMatch { get; set; } = true;

    /// <summary>
    /// 是否启用拼音匹配
    /// </summary>
    public bool EnablePinyinMatch { get; set; } = true;

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
}
