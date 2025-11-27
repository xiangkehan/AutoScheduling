using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel - 性能优化相关功能
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 性能优化 - 虚拟化渲染

        private int _visibleRowStart = 0;
        private int _visibleRowEnd = 50;
        private const int RowBufferSize = 10; // 缓冲区大小

        /// <summary>
        /// 可见行起始索引
        /// </summary>
        public int VisibleRowStart
        {
            get => _visibleRowStart;
            set => SetProperty(ref _visibleRowStart, value);
        }

        /// <summary>
        /// 可见行结束索引
        /// </summary>
        public int VisibleRowEnd
        {
            get => _visibleRowEnd;
            set => SetProperty(ref _visibleRowEnd, value);
        }

        /// <summary>
        /// 更新可见范围
        /// </summary>
        /// <param name="firstVisibleIndex">第一个可见项索引</param>
        /// <param name="lastVisibleIndex">最后一个可见项索引</param>
        public void UpdateVisibleRange(int firstVisibleIndex, int lastVisibleIndex)
        {
            // 添加缓冲区
            VisibleRowStart = Math.Max(0, firstVisibleIndex - RowBufferSize);
            VisibleRowEnd = Math.Min(ScheduleGrid.Count, lastVisibleIndex + RowBufferSize);
        }

        #endregion

        #region 性能优化 - 滚动节流

        private DateTime _lastScrollTime = DateTime.MinValue;
        private const int ScrollThrottleMs = 100;
        private DispatcherQueueTimer? _scrollTimer;

        /// <summary>
        /// 处理滚动事件（节流）
        /// </summary>
        /// <param name="firstVisibleIndex">第一个可见项索引</param>
        /// <param name="lastVisibleIndex">最后一个可见项索引</param>
        public void OnScroll(int firstVisibleIndex, int lastVisibleIndex)
        {
            var now = DateTime.Now;
            if ((now - _lastScrollTime).TotalMilliseconds < ScrollThrottleMs)
            {
                // 在节流期间，使用定时器延迟更新
                _scrollTimer?.Stop();
                _scrollTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
                _scrollTimer.Interval = TimeSpan.FromMilliseconds(ScrollThrottleMs);
                _scrollTimer.Tick += (s, e) =>
                {
                    _scrollTimer.Stop();
                    UpdateVisibleRange(firstVisibleIndex, lastVisibleIndex);
                };
                _scrollTimer.Start();
                return;
            }

            _lastScrollTime = now;
            UpdateVisibleRange(firstVisibleIndex, lastVisibleIndex);
        }

        #endregion

        #region 性能优化 - 窗口大小调整节流

        private DispatcherQueueTimer? _resizeTimer;
        private const int ResizeThrottleMs = 200;

        /// <summary>
        /// 处理窗口大小变化（节流）
        /// </summary>
        /// <param name="width">新宽度</param>
        /// <param name="height">新高度</param>
        public void OnSizeChanged(double width, double height)
        {
            _resizeTimer?.Stop();
            _resizeTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _resizeTimer.Interval = TimeSpan.FromMilliseconds(ResizeThrottleMs);
            _resizeTimer.Tick += (s, e) =>
            {
                _resizeTimer.Stop();
                UpdateLayoutForSize(width, height);
            };
            _resizeTimer.Start();
        }

        /// <summary>
        /// 根据窗口大小更新布局
        /// </summary>
        private void UpdateLayoutForSize(double width, double height)
        {
            // 根据宽度调整布局模式
            if (width >= 1920)
            {
                CurrentLayoutMode = LayoutMode.Large;
            }
            else if (width >= 1366)
            {
                CurrentLayoutMode = LayoutMode.Medium;
            }
            else if (width >= 1024)
            {
                CurrentLayoutMode = LayoutMode.Small;
            }
            else
            {
                CurrentLayoutMode = LayoutMode.Compact;
            }
        }

        #endregion

        #region 性能优化 - 数据缓存

        private StatisticsSummary? _cachedStatistics;
        private DateTime _statisticsCacheTime = DateTime.MinValue;
        private const int StatisticsCacheSeconds = 5;

        private Dictionary<string, object> _searchResultsCache = new();
        private DateTime _searchCacheTime = DateTime.MinValue;
        private const int SearchCacheSeconds = 10;

        /// <summary>
        /// 获取统计数据（带缓存）
        /// </summary>
        public async Task<StatisticsSummary> GetStatisticsWithCacheAsync()
        {
            var now = DateTime.Now;
            if (_cachedStatistics != null &&
                (now - _statisticsCacheTime).TotalSeconds < StatisticsCacheSeconds)
            {
                return _cachedStatistics;
            }

            // 重新计算统计数据
            _cachedStatistics = await CalculateStatisticsAsync();
            _statisticsCacheTime = now;
            return _cachedStatistics;
        }

        /// <summary>
        /// 计算统计数据
        /// </summary>
        private async Task<StatisticsSummary> CalculateStatisticsAsync()
        {
            if (Schedule == null)
            {
                return new StatisticsSummary();
            }

            await Task.CompletedTask; // 模拟异步操作

            var hardConflictCount = Schedule.Conflicts?.Count(c => c.IsHardConstraint) ?? 0;
            var softConflictCount = Schedule.Conflicts?.Count(c => !c.IsHardConstraint) ?? 0;
            var unassignedCount = Schedule.Shifts?.Count(s => s.PersonnelId == 0) ?? 0;
            var totalShiftCount = Schedule.Shifts?.Count ?? 0;
            var assignedShiftCount = totalShiftCount - unassignedCount;

            return new StatisticsSummary
            {
                HardConflictCount = hardConflictCount,
                SoftConflictCount = softConflictCount,
                UnassignedCount = unassignedCount,
                TotalShiftCount = totalShiftCount,
                AssignedShiftCount = assignedShiftCount,
                CoverageRate = totalShiftCount > 0 ? (double)assignedShiftCount / totalShiftCount : 0.0
            };
        }

        /// <summary>
        /// 清除统计缓存
        /// </summary>
        public void ClearStatisticsCache()
        {
            _cachedStatistics = null;
            _statisticsCacheTime = DateTime.MinValue;
        }

        /// <summary>
        /// 清除搜索缓存
        /// </summary>
        public void ClearSearchCache()
        {
            _searchResultsCache.Clear();
            _searchCacheTime = DateTime.MinValue;
        }

        #endregion

        #region 性能优化 - 懒加载

        private bool _isDetailContentLoaded = false;

        /// <summary>
        /// 详情内容是否已加载
        /// </summary>
        public bool IsDetailContentLoaded
        {
            get => _isDetailContentLoaded;
            set => SetProperty(ref _isDetailContentLoaded, value);
        }

        /// <summary>
        /// 按需加载详情内容
        /// </summary>
        /// <param name="item">选中项</param>
        public async Task LoadDetailContentAsync(object? item)
        {
            if (item == null)
            {
                IsDetailContentLoaded = false;
                return;
            }

            // 标记为正在加载
            IsDetailContentLoaded = false;

            // 根据类型加载不同的详情内容
            switch (item)
            {
                case ConflictItemViewModel conflict:
                    await LoadConflictDetailAsync(conflict);
                    break;

                case ScheduleCellViewModel cell:
                    await LoadCellDetailAsync(cell);
                    break;

                case PersonnelScheduleData personnel:
                    await LoadPersonnelDetailAsync(personnel);
                    break;

                case PositionScheduleData position:
                    await LoadPositionDetailAsync(position);
                    break;
            }

            // 标记为已加载
            IsDetailContentLoaded = true;
        }

        /// <summary>
        /// 加载冲突详情
        /// </summary>
        private async Task LoadConflictDetailAsync(ConflictItemViewModel conflict)
        {
            // 模拟异步加载
            await Task.Delay(50);
            // TODO: 加载冲突详细信息
        }

        /// <summary>
        /// 加载单元格详情
        /// </summary>
        private async Task LoadCellDetailAsync(ScheduleCellViewModel cell)
        {
            // 模拟异步加载
            await Task.Delay(50);
            // TODO: 加载单元格详细信息
        }

        /// <summary>
        /// 加载人员详情
        /// </summary>
        private async Task LoadPersonnelDetailAsync(PersonnelScheduleData personnel)
        {
            // 模拟异步加载
            await Task.Delay(50);
            // TODO: 加载人员详细信息
        }

        /// <summary>
        /// 加载哨位详情
        /// </summary>
        private async Task LoadPositionDetailAsync(PositionScheduleData position)
        {
            // 模拟异步加载
            await Task.Delay(50);
            // TODO: 加载哨位详细信息
        }

        #endregion

        #region 性能优化 - 快速切换防抖

        private CancellationTokenSource? _selectionChangeCts;
        private const int SelectionDebounceMs = 150;

        /// <summary>
        /// 处理选中项变化（防抖）
        /// </summary>
        /// <param name="item">新选中项</param>
        public async Task OnSelectionChangedAsync(object? item)
        {
            // 取消之前的操作
            _selectionChangeCts?.Cancel();
            _selectionChangeCts = new CancellationTokenSource();

            try
            {
                // 防抖延迟
                await Task.Delay(SelectionDebounceMs, _selectionChangeCts.Token);

                // 更新选中项
                SelectedItem = item;

                // 按需加载详情内容
                await LoadDetailContentAsync(item);
            }
            catch (TaskCanceledException)
            {
                // 操作被取消，忽略
            }
        }

        #endregion

        #region 性能监控

        private Dictionary<string, DateTime> _performanceMarkers = new();

        /// <summary>
        /// 开始性能监控
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public void StartPerformanceMonitoring(string operationName)
        {
            _performanceMarkers[operationName] = DateTime.Now;
        }

        /// <summary>
        /// 结束性能监控并记录
        /// </summary>
        /// <param name="operationName">操作名称</param>
        public void EndPerformanceMonitoring(string operationName)
        {
            if (_performanceMarkers.TryGetValue(operationName, out var startTime))
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                System.Diagnostics.Debug.WriteLine($"[性能] {operationName}: {elapsed:F2}ms");

                // 如果超过200ms，记录警告
                if (elapsed > 200)
                {
                    System.Diagnostics.Debug.WriteLine($"[性能警告] {operationName} 耗时 {elapsed:F2}ms，超过200ms阈值");
                }

                _performanceMarkers.Remove(operationName);
            }
        }

        #endregion
    }
}
