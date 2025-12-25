using System;

namespace AutoScheduling3.SchedulingEngine.Core;

/// <summary>
/// 时段映射工具：全局时段索引与(日期索引, 局部时段索引)之间的转换
/// 对应需求5.1-5.5
/// </summary>
public class PeriodMapper
{
    private readonly DateTime _startDate;
    private readonly int _totalDays;
    private readonly int _periodsPerDay = 12;

    /// <summary>
    /// 初始化时段映射器
    /// </summary>
    /// <param name="startDate">排班开始日期</param>
    /// <param name="endDate">排班结束日期</param>
    public PeriodMapper(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate.Date;
        _totalDays = (endDate.Date - startDate.Date).Days + 1;

        if (_totalDays <= 0)
        {
            throw new ArgumentException("结束日期必须大于或等于开始日期");
        }
    }

    /// <summary>
    /// 将(日期索引, 局部时段索引)转换为全局时段索引
    /// 对应需求5.1
    /// </summary>
    /// <param name="dayIndex">日期索引 (0-based)</param>
    /// <param name="localPeriod">局部时段索引 (0-11)</param>
    /// <returns>全局时段索引</returns>
    public int ToGlobalPeriod(int dayIndex, int localPeriod)
    {
        if (dayIndex < 0 || dayIndex >= _totalDays)
        {
            throw new ArgumentOutOfRangeException(nameof(dayIndex), 
                $"日期索引必须在 0 到 {_totalDays - 1} 之间");
        }

        if (localPeriod < 0 || localPeriod >= _periodsPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(localPeriod), 
                $"局部时段索引必须在 0 到 {_periodsPerDay - 1} 之间");
        }

        return dayIndex * _periodsPerDay + localPeriod;
    }

    /// <summary>
    /// 将全局时段索引转换为(日期索引, 局部时段索引)
    /// 对应需求5.2
    /// </summary>
    /// <param name="globalPeriod">全局时段索引</param>
    /// <returns>日期索引和局部时段索引的元组</returns>
    public (int dayIndex, int localPeriod) ToLocalPeriod(int globalPeriod)
    {
        if (!IsValidGlobalPeriod(globalPeriod))
        {
            throw new ArgumentOutOfRangeException(nameof(globalPeriod), 
                $"全局时段索引必须在 0 到 {TotalPeriods - 1} 之间");
        }

        int dayIndex = globalPeriod / _periodsPerDay;
        int localPeriod = globalPeriod % _periodsPerDay;
        return (dayIndex, localPeriod);
    }

    /// <summary>
    /// 将全局时段索引转换为具体日期和时段
    /// 对应需求5.3, 5.4
    /// </summary>
    /// <param name="globalPeriod">全局时段索引</param>
    /// <returns>日期和局部时段索引的元组</returns>
    public (DateTime date, int localPeriod) ToDateTime(int globalPeriod)
    {
        var (dayIndex, localPeriod) = ToLocalPeriod(globalPeriod);
        // 确保返回的日期只包含日期部分，Kind 为 Unspecified，与 Assignments 字典键一致
        DateTime date = _startDate.AddDays(dayIndex).Date;
        
        return (date, localPeriod);
    }

    /// <summary>
    /// 将日期和时段转换为全局时段索引
    /// 对应需求5.5
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="localPeriod">局部时段索引 (0-11)</param>
    /// <returns>全局时段索引</returns>
    public int ToGlobalPeriod(DateTime date, int localPeriod)
    {
        if (localPeriod < 0 || localPeriod >= _periodsPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(localPeriod), 
                $"局部时段索引必须在 0 到 {_periodsPerDay - 1} 之间");
        }

        int dayIndex = (date.Date - _startDate).Days;
        
        if (dayIndex < 0 || dayIndex >= _totalDays)
        {
            throw new ArgumentOutOfRangeException(nameof(date), 
                $"日期必须在 {_startDate:yyyy-MM-dd} 到 {_startDate.AddDays(_totalDays - 1):yyyy-MM-dd} 之间");
        }

        return ToGlobalPeriod(dayIndex, localPeriod);
    }

    /// <summary>
    /// 获取总时段数
    /// </summary>
    public int TotalPeriods => _totalDays * _periodsPerDay;

    /// <summary>
    /// 获取总天数
    /// </summary>
    public int TotalDays => _totalDays;

    /// <summary>
    /// 获取每天的时段数
    /// </summary>
    public int PeriodsPerDay => _periodsPerDay;

    /// <summary>
    /// 获取开始日期
    /// </summary>
    public DateTime StartDate => _startDate;

    /// <summary>
    /// 获取结束日期
    /// </summary>
    public DateTime EndDate => _startDate.AddDays(_totalDays - 1);

    /// <summary>
    /// 验证全局时段索引是否有效
    /// </summary>
    /// <param name="globalPeriod">全局时段索引</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
    public bool IsValidGlobalPeriod(int globalPeriod)
    {
        return globalPeriod >= 0 && globalPeriod < TotalPeriods;
    }

    /// <summary>
    /// 验证日期索引是否有效
    /// </summary>
    /// <param name="dayIndex">日期索引</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
    public bool IsValidDayIndex(int dayIndex)
    {
        return dayIndex >= 0 && dayIndex < _totalDays;
    }

    /// <summary>
    /// 验证局部时段索引是否有效
    /// </summary>
    /// <param name="localPeriod">局部时段索引</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
    public bool IsValidLocalPeriod(int localPeriod)
    {
        return localPeriod >= 0 && localPeriod < _periodsPerDay;
    }

    /// <summary>
    /// 获取指定全局时段的时间范围
    /// </summary>
    /// <param name="globalPeriod">全局时段索引</param>
    /// <returns>时段的开始和结束时间</returns>
    public (DateTime startTime, DateTime endTime) GetPeriodTimeRange(int globalPeriod)
    {
        var (date, localPeriod) = ToDateTime(globalPeriod);
        
        // 每个时段2小时
        DateTime startTime = date.AddHours(localPeriod * 2);
        DateTime endTime = startTime.AddHours(2);
        
        return (startTime, endTime);
    }
}
