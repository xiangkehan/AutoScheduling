using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.Models;
namespace AutoScheduling3.History;
/// <summary>
/// 历史管理接口
/// </summary>
public interface IHistoryManagement 
{ 
    Task InitAsync();
    Task<int> AddToBufferAsync(Schedule schedule); 
    Task ConfirmBufferScheduleAsync(int bufferId); 
    Task<List<(Schedule Schedule, DateTime ConfirmTime)>> GetAllHistorySchedulesAsync();
    Task<List<(Schedule Schedule, DateTime CreateTime, int BufferId)>> GetAllBufferSchedulesAsync(); 
    Task ClearBufferAsync(); Task DeleteHistoryScheduleAsync(int scheduleId); 
    Task DeleteBufferScheduleAsync(int bufferId); 
    Task<Schedule?> GetLastConfirmedScheduleAsync(); 
    Task<(Schedule Schedule, DateTime ConfirmTime)?> GetHistoryScheduleByScheduleIdAsync(int scheduleId); 
}