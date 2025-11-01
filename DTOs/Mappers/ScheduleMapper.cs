using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 排班数据映射器 - Model 与 DTO 互转
/// 需求: 3.1, 3.2
/// </summary>
public class ScheduleMapper
{
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;

    public ScheduleMapper(IPersonalRepository personnelRepository, IPositionRepository positionRepository)
    {
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
    }

    /// <summary>
    /// Model 转 DTO（同步版本，不加载关联名称）
    /// </summary>
    public ScheduleDto ToDto(Schedule model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new ScheduleDto
        {
            Id = model.Id,
            Title = model.Header,
            PersonnelIds = new List<int>(model.PersonnelIds),
            PositionIds = new List<int>(model.PositionIds),
            Shifts = model.Results?.Select(ToShiftDto).ToList() ?? new List<ShiftDto>(),
            Conflicts = new List<ConflictDto>(), // 空列表，需要业务逻辑填充
            CreatedAt = model.CreatedAt,
            ConfirmedAt = model.IsConfirmed ? model.UpdatedAt : null,
            StartDate = model.StartDate,
            EndDate = model.EndDate
        };
    }

    /// <summary>
    /// Model 转 DTO（异步版本，加载关联名称）
    /// </summary>
    public async Task<ScheduleDto> ToDtoAsync(Schedule model)
    {
        var dto = ToDto(model);

        // 加载班次的关联名称
        if (model.Results != null && model.Results.Count > 0)
        {
            dto.Shifts = await ToShiftDtoListAsync(model.Results);
        }

        return dto;
    }

    /// <summary>
    /// SingleShift Model 转 ShiftDto（同步版本）
    /// </summary>
    public ShiftDto ToShiftDto(SingleShift model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new ShiftDto
        {
            Id = model.Id,
            ScheduleId = model.ScheduleId,
            PositionId = model.PositionId,
            PositionName = string.Empty, // 需要异步加载
            PersonnelId = model.PersonnelId,
            PersonnelName = string.Empty, // 需要异步加载
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            PeriodIndex = model.TimeSlotIndex
        };
    }

    /// <summary>
    /// SingleShift Model 转 ShiftDto（异步版本，加载关联名称）
    /// </summary>
    public async Task<ShiftDto> ToShiftDtoAsync(SingleShift model)
    {
        var dto = ToShiftDto(model);

        // 加载哨位名称
        var position = await _positionRepository.GetByIdAsync(model.PositionId);
        dto.PositionName = position?.Name ?? "未知哨位";

        // 加载人员名称
        var personnel = await _personnelRepository.GetByIdAsync(model.PersonnelId);
        dto.PersonnelName = personnel?.Name ?? "未知人员";

        return dto;
    }

    /// <summary>
    /// 批量转换 ShiftDto（异步版本）
    /// </summary>
    public async Task<List<ShiftDto>> ToShiftDtoListAsync(IEnumerable<SingleShift> models)
    {
        if (models == null)
            return new List<ShiftDto>();

        var tasks = models.Select(m => ToShiftDtoAsync(m));
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// SchedulingRequestDto 转 Schedule Model
    /// </summary>
    public Schedule ToModel(SchedulingRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Schedule
        {
            Header = dto.Title,
            PersonnelIds = new List<int>(dto.PersonnelIds),
            PositionIds = new List<int>(dto.PositionIds),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Results = new List<SingleShift>(),
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// ShiftDto 转 SingleShift Model
    /// </summary>
    public SingleShift ToShiftModel(ShiftDto dto, int scheduleId)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new SingleShift
        {
            Id = dto.Id,
            ScheduleId = scheduleId,
            PositionId = dto.PositionId,
            PersonnelId = dto.PersonnelId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            TimeSlotIndex = dto.PeriodIndex,
            DayIndex = (dto.StartTime.Date - DateTime.Today).Days,
            IsNightShift = IsNightTimeSlot(dto.PeriodIndex)
        };
    }

    /// <summary>
    /// Schedule Model 转 ScheduleSummaryDto
    /// </summary>
    public ScheduleSummaryDto ToSummaryDto(Schedule model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new ScheduleSummaryDto
        {
            Id = model.Id,
            Title = model.Header,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            PersonnelCount = model.PersonnelIds?.Count ?? 0,
            PositionCount = model.PositionIds?.Count ?? 0,
            ShiftCount = model.Results?.Count ?? 0,
            CreatedAt = model.CreatedAt,
            ConfirmedAt = model.IsConfirmed ? model.UpdatedAt : null
        };
    }

    /// <summary>
    /// 批量转换 ScheduleDto（同步版本）
    /// </summary>
    public List<ScheduleDto> ToDtoList(IEnumerable<Schedule> models)
    {
        if (models == null)
            return new List<ScheduleDto>();

        return models.Select(ToDto).ToList();
    }

    /// <summary>
    /// 批量转换 ScheduleDto（异步版本，加载关联名称）
    /// </summary>
    public async Task<List<ScheduleDto>> ToDtoListAsync(IEnumerable<Schedule> models)
    {
        if (models == null)
            return new List<ScheduleDto>();

        var tasks = models.Select(m => ToDtoAsync(m));
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// 批量转换 ScheduleSummaryDto
    /// </summary>
    public List<ScheduleSummaryDto> ToSummaryDtoList(IEnumerable<Schedule> models)
    {
        if (models == null)
            return new List<ScheduleSummaryDto>();

        return models.Select(ToSummaryDto).ToList();
    }

    /// <summary>
    /// 判断时段是否为夜班
    /// </summary>
    private static bool IsNightTimeSlot(int timeSlotIndex)
    {
        // 假设时段0-11对应0:00-23:59，夜班为22:00-06:00（时段11,0,1,2）
        return timeSlotIndex == 11 || timeSlotIndex <= 2;
    }
}