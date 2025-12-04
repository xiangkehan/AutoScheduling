using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core;

/// <summary>
/// 遗传算法中的个体类，表示一个完整的排班方案
/// </summary>
public class Individual
{
    /// <summary>
    /// 基因编码：[日期][时段, 哨位] = 人员索引（-1表示未分配）
    /// </summary>
    public Dictionary<DateTime, int[,]> Genes { get; set; } = new();

    /// <summary>
    /// 适应度值（越高越好）
    /// </summary>
    public double Fitness { get; set; }

    /// <summary>
    /// 硬约束违反数量
    /// </summary>
    public int HardConstraintViolations { get; set; }

    /// <summary>
    /// 软约束得分
    /// </summary>
    public double SoftConstraintScore { get; set; }

    /// <summary>
    /// 未分配时段数量
    /// </summary>
    public int UnassignedSlots { get; set; }

    /// <summary>
    /// 从 Schedule 创建个体
    /// </summary>
    /// <param name="schedule">排班方案</param>
    /// <param name="context">调度上下文</param>
    /// <returns>个体实例</returns>
    public static Individual FromSchedule(Schedule schedule, SchedulingContext context)
    {
        var individual = new Individual();

        // 初始化基因编码
        var currentDate = context.StartDate.Date;
        while (currentDate <= context.EndDate.Date)
        {
            individual.Genes[currentDate] = new int[12, context.Positions.Count];
            
            // 初始化为未分配
            for (int p = 0; p < 12; p++)
            {
                for (int x = 0; x < context.Positions.Count; x++)
                {
                    individual.Genes[currentDate][p, x] = -1;
                }
            }
            
            currentDate = currentDate.AddDays(1);
        }

        // 从 Schedule 的 Results 填充基因
        foreach (var shift in schedule.Results)
        {
            var date = shift.StartTime.Date;
            var periodIdx = shift.TimeSlotIndex;
            
            // 获取哨位索引
            if (context.PositionIdToIdx.TryGetValue(shift.PositionId, out int positionIdx))
            {
                // 获取人员索引
                if (context.PersonIdToIdx.TryGetValue(shift.PersonnelId, out int personIdx))
                {
                    if (individual.Genes.ContainsKey(date))
                    {
                        individual.Genes[date][periodIdx, positionIdx] = personIdx;
                    }
                }
            }
        }

        // 识别未分配时段
        individual.IdentifyUnassignedSlots();

        return individual;
    }

    /// <summary>
    /// 转换为 Schedule
    /// </summary>
    /// <param name="context">调度上下文</param>
    /// <returns>排班方案</returns>
    public Schedule ToSchedule(SchedulingContext context)
    {
        // 调试：输出Genes字典中的所有日期
        System.Diagnostics.Debug.WriteLine($"[Individual.ToSchedule] Genes字典包含 {Genes.Count} 个日期:");
        foreach (var date in Genes.Keys.OrderBy(d => d))
        {
            var assignedCount = 0;
            var assignments = Genes[date];
            for (int p = 0; p < 12; p++)
            {
                for (int x = 0; x < assignments.GetLength(1); x++)
                {
                    if (assignments[p, x] >= 0) assignedCount++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"  {date:yyyy-MM-dd}: {assignedCount} 个分配");
        }
        
        var schedule = new Schedule
        {
            StartDate = context.StartDate,
            EndDate = context.EndDate,
            PersonnelIds = context.Personals.Select(p => p.Id).ToList(),
            PositionIds = context.Positions.Select(p => p.Id).ToList(),
            Results = new List<SingleShift>()
        };

        // 遍历所有基因，生成 SingleShift
        foreach (var (date, assignments) in Genes)
        {
            // 验证日期在范围内 - 修复遗传算法可能产生的日期越界问题
            if (date.Date < context.StartDate.Date || date.Date > context.EndDate.Date)
            {
                System.Diagnostics.Debug.WriteLine($"[Individual.ToSchedule] 跳过范围外的日期: {date:yyyy-MM-dd}");
                continue;
            }
            
            for (int periodIdx = 0; periodIdx < 12; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < context.Positions.Count; positionIdx++)
                {
                    int personIdx = assignments[periodIdx, positionIdx];
                    
                    // 跳过未分配的时段
                    if (personIdx == -1)
                        continue;

                    // 获取实际ID
                    int positionId = context.PositionIdxToId[positionIdx];
                    int personnelId = context.PersonIdxToId[personIdx];

                    // 计算时间（明确指定为本地时间）
                    var startTime = DateTime.SpecifyKind(date.AddHours(periodIdx * 2), DateTimeKind.Local);
                    var endTime = DateTime.SpecifyKind(startTime.AddHours(2), DateTimeKind.Local);

                    // 判断是否为夜哨
                    bool isNightShift = periodIdx == 11 || periodIdx == 0 || periodIdx == 1 || periodIdx == 2;

                    // 计算天数索引
                    int dayIndex = (date.Date - context.StartDate.Date).Days;



                    var shift = new SingleShift
                    {
                        PositionId = positionId,
                        PersonnelId = personnelId,
                        StartTime = startTime,
                        EndTime = endTime,
                        DayIndex = dayIndex,
                        TimeSlotIndex = periodIdx,
                        IsNightShift = isNightShift
                    };

                    schedule.Results.Add(shift);
                }
            }
        }

        return schedule;
    }

    /// <summary>
    /// 深拷贝个体
    /// </summary>
    /// <returns>新的个体实例</returns>
    public Individual Clone()
    {
        var clone = new Individual
        {
            Fitness = this.Fitness,
            HardConstraintViolations = this.HardConstraintViolations,
            SoftConstraintScore = this.SoftConstraintScore,
            UnassignedSlots = this.UnassignedSlots
        };

        // 深拷贝基因
        foreach (var (date, assignments) in Genes)
        {
            int rows = assignments.GetLength(0);
            int cols = assignments.GetLength(1);
            clone.Genes[date] = new int[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    clone.Genes[date][i, j] = assignments[i, j];
                }
            }
        }

        return clone;
    }

    /// <summary>
    /// 识别未分配时段
    /// </summary>
    public void IdentifyUnassignedSlots()
    {
        int count = 0;
        
        foreach (var (date, assignments) in Genes)
        {
            int periods = assignments.GetLength(0);
            int positions = assignments.GetLength(1);
            
            for (int p = 0; p < periods; p++)
            {
                for (int x = 0; x < positions; x++)
                {
                    if (assignments[p, x] == -1)
                    {
                        count++;
                    }
                }
            }
        }
        
        UnassignedSlots = count;
    }
}
