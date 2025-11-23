using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 人员搜索辅助类 - 封装搜索逻辑，支持防抖和模糊匹配
/// </summary>
public class PersonnelSearchHelper
{
    private CancellationTokenSource? _debounceTokenSource;
    private const int DebounceDelayMs = 300;

    /// <summary>
    /// 执行搜索（带防抖）
    /// </summary>
    /// <param name="query">搜索查询文本</param>
    /// <param name="candidates">候选人员列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的人员列表</returns>
    public async Task<List<PersonnelDto>> SearchAsync(
        string query,
        IEnumerable<PersonnelDto> candidates,
        CancellationToken cancellationToken = default)
    {
        // 取消之前的搜索
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();

        // 创建组合取消令牌
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _debounceTokenSource.Token,
            cancellationToken);

        try
        {
            // 防抖延迟
            await Task.Delay(DebounceDelayMs, linkedCts.Token);

            // 执行搜索
            return SearchImmediate(query, candidates);
        }
        catch (TaskCanceledException)
        {
            // 搜索被取消，返回空列表
            System.Diagnostics.Debug.WriteLine("PersonnelSearchHelper: 搜索被取消");
            return new List<PersonnelDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PersonnelSearchHelper: 搜索失败: {ex.Message}");
            return new List<PersonnelDto>();
        }
    }

    /// <summary>
    /// 立即执行搜索（不防抖）
    /// </summary>
    /// <param name="query">搜索查询文本</param>
    /// <param name="candidates">候选人员列表</param>
    /// <returns>匹配的人员列表</returns>
    public List<PersonnelDto> SearchImmediate(
        string query,
        IEnumerable<PersonnelDto> candidates)
    {
        if (candidates == null)
        {
            return new List<PersonnelDto>();
        }

        var candidateList = candidates.ToList();

        // 空查询返回所有候选人
        if (string.IsNullOrWhiteSpace(query))
        {
            return candidateList;
        }

        try
        {
            // 使用FuzzyMatcher进行模糊匹配
            var options = new FuzzyMatchOptions
            {
                EnableFuzzyMatch = true,
                EnablePinyinMatch = true,
                CaseSensitive = false,
                MaxResults = 50,
                MinScore = 0
            };

            var matchResults = FuzzyMatcher.Match(query, candidateList, options);

            // 返回匹配的人员列表
            return matchResults.Select(r => r.Personnel).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PersonnelSearchHelper: 搜索执行失败: {ex.Message}");
            
            // 降级到简单的Contains匹配
            return candidateList
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// 取消当前搜索
    /// </summary>
    public void CancelSearch()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = null;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();
        _debounceTokenSource = null;
    }
}
