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

        // 验证日期范围
        ValidateDateRange(dto.StartDate, dto.EndDate);

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
    /// ScheduleDto 转 Schedule Model（用于更新现有模型）
    /// </summary>
    public Schedule ToModel(ScheduleDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // 验证日期范围
        ValidateDateRange(dto.StartDate, dto.EndDate);

        return new Schedule
        {
            Id = dto.Id,
            Header = dto.Title,
            PersonnelIds = new List<int>(dto.PersonnelIds),
            PositionIds = new List<int>(dto.PositionIds),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Results = dto.Shifts?.Select(s => ToShiftModel(s, dto.Id)).ToList() ?? new List<SingleShift>(),
            IsConfirmed = dto.ConfirmedAt.HasValue,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// ScheduleDto 转 Schedule Model（用于更新现有排班表）
    /// </summary>
    public void UpdateModel(Schedule model, ScheduleDto dto)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // 验证日期范围
        ValidateDateRange(dto.StartDate, dto.EndDate);

        model.Header = dto.Title;
        model.PersonnelIds = new List<int>(dto.PersonnelIds);
        model.PositionIds = new List<int>(dto.PositionIds);
        model.StartDate = dto.StartDate;
        model.EndDate = dto.EndDate;
        model.UpdatedAt = DateTime.UtcNow;

        // 更新班次集合
        if (dto.Shifts != null)
        {
            model.Results = dto.Shifts.Select(s => ToShiftModel(s, model.Id)).ToList();
        }
    }

    /// <summary>
    /// ShiftDto 转 SingleShift Model
    /// </summary>
    public SingleShift ToShiftModel(ShiftDto dto, int scheduleId)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // 验证班次时间
        ValidateShiftTime(dto.StartTime, dto.EndTime);

        return new SingleShift
        {
            Id = dto.Id,
            ScheduleId = scheduleId,
            PositionId = dto.PositionId,
            PersonnelId = dto.PersonnelId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            TimeSlotIndex = dto.PeriodIndex,
            DayIndex = CalculateDayIndex(dto.StartTime),
            IsNightShift = IsNightTimeSlot(dto.PeriodIndex)
        };
    }

    /// <summary>
    /// 批量转换 SingleShift Model
    /// </summary>
    public List<SingleShift> ToShiftModelList(IEnumerable<ShiftDto> dtos, int scheduleId)
    {
        if (dtos == null)
            return new List<SingleShift>();

        return dtos.Select(dto => ToShiftModel(dto, scheduleId)).ToList();
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

    /// <summary>
    /// 计算日期索引（从某个基准日期开始的天数）
    /// </summary>
    private static int CalculateDayIndex(DateTime shiftDate)
    {
        // 使用排班开始日期作为基准（Day 0）
        var baseDate = new DateTime(shiftDate.Year, shiftDate.Month, shiftDate.Day);
        return (baseDate - DateTime.Today).Days;
    }

    /// <summary>
    /// 验证日期范围
    /// </summary>
    private static void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("开始日期不能晚于结束日期");
        }

        if (startDate.Date < DateTime.Today)
        {
            throw new ArgumentException("开始日期不能早于今天");
        }

        var maxDays = 365; // 最大排班天数限制
        if ((endDate - startDate).Days > maxDays)
        {
            throw new ArgumentException($"排班时间范围不能超过{maxDays}天");
        }
    }

    /// <summary>
    /// 验证班次时间
    /// </summary>
    private static void ValidateShiftTime(DateTime startTime, DateTime endTime)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("班次开始时间必须早于结束时间");
        }

        var maxShiftHours = 24; // 最大班次时长限制
        if ((endTime - startTime).TotalHours > maxShiftHours)
        {
            throw new ArgumentException($"单次班次时长不能超过{maxShiftHours}小时");
        }
    }

    /// <summary>
    /// 验证排班表数据完整性
    /// </summary>
    public bool ValidateScheduleIntegrity(ScheduleDto dto)
    {
        if (dto == null)
            return false;

        // 检查基本字段
        if (string.IsNullOrWhiteSpace(dto.Title))
            return false;

        if (dto.PersonnelIds == null || dto.PersonnelIds.Count == 0)
            return false;

        if (dto.PositionIds == null || dto.PositionIds.Count == 0)
            return false;

        // 检查日期范围
        try
        {
            ValidateDateRange(dto.StartDate, dto.EndDate);
        }
        catch
        {
            return false;
        }

        // 检查班次数据
        if (dto.Shifts != null)
        {
            foreach (var shift in dto.Shifts)
            {
                if (shift.PositionId <= 0 || shift.PersonnelId <= 0)
                    return false;

                if (shift.PeriodIndex < 0 || shift.PeriodIndex > 11)
                    return false;

                try
                {
                    ValidateShiftTime(shift.StartTime, shift.EndTime);
                }
                catch
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 验证单次班次数据完整性
    /// </summary>
    public bool ValidateShiftIntegrity(ShiftDto dto)
    {
        if (dto == null)
            return false;

        if (dto.PositionId <= 0 || dto.PersonnelId <= 0)
            return false;

        if (dto.PeriodIndex < 0 || dto.PeriodIndex > 11)
            return false;

        try
        {
            ValidateShiftTime(dto.StartTime, dto.EndTime);
        }
        catch
        {
            return false;
        }

        return true;
    }
}