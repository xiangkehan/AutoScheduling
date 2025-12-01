using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Services;

/// <summary>
/// 分页草稿加载器
/// 提供草稿列表的分页加载和过滤功能
/// </summary>
public class PaginatedDraftLoader
{
    private readonly ISchedulingService _schedulingService;
    private const int DefaultPageSize = 20;

    public PaginatedDraftLoader(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    /// <summary>
    /// 获取分页的草稿列表
    /// </summary>
    /// <param name="pageIndex">页码（从0开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="filterMode">过滤模式（null表示不过滤）</param>
    /// <param name="filterResumableOnly">是否只显示可恢复的草稿</param>
    /// <returns>分页结果</returns>
    public async Task<PagedResult<ScheduleSummaryDto>> GetDraftsPagedAsync(
        int pageIndex = 0,
        int pageSize = DefaultPageSize,
        SchedulingMode? filterMode = null,
        bool filterResumableOnly = false)
    {
        // 获取所有草稿
        var allDrafts = await _schedulingService.GetDraftsAsync();

        // 应用过滤
        var filteredDrafts = ApplyFilters(allDrafts, filterMode, filterResumableOnly);

        // 计算总数和总页数
        var totalCount = filteredDrafts.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // 确保页码有效
        pageIndex = Math.Max(0, Math.Min(pageIndex, totalPages - 1));

        // 获取当前页数据
        var pagedDrafts = filteredDrafts
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ScheduleSummaryDto>
        {
            Items = pagedDrafts,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// 应用过滤条件
    /// </summary>
    private List<ScheduleSummaryDto> ApplyFilters(
        List<ScheduleSummaryDto> drafts,
        SchedulingMode? filterMode,
        bool filterResumableOnly)
    {
        var filtered = drafts.AsEnumerable();

        // 按模式过滤
        if (filterMode.HasValue)
        {
            filtered = filtered.Where(d => d.SchedulingMode == filterMode.Value);
        }

        // 按可恢复状态过滤
        if (filterResumableOnly)
        {
            filtered = filtered.Where(d => d.IsResumable);
        }

        // 按创建时间降序排序（最新的在前面）
        return filtered.OrderByDescending(d => d.CreatedAt).ToList();
    }
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 当前页数据
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 当前页码（从0开始）
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages - 1;
}
