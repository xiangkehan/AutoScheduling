using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Models;
namespace AutoScheduling3.Data.Interfaces;
/// <summary>
/// 约束仓储接口：休息日配置、定岗规则、手动指定
/// </summary>
public interface IConstraintRepository
{ 
    Task InitAsync();
    // FixedPositionRule
    Task<int> AddFixedPositionRuleAsync(FixedPositionRule rule);
    Task<List<FixedPositionRule>> GetAllFixedPositionRulesAsync(bool enabledOnly = true); 
    Task<List<FixedPositionRule>> GetFixedPositionRulesByPersonAsync(int personalId);
    Task UpdateFixedPositionRuleAsync(FixedPositionRule rule); 
    Task DeleteFixedPositionRuleAsync(int id); 
    // ManualAssignment 
    Task<int> AddManualAssignmentAsync(ManualAssignment assignment); 
    Task<List<ManualAssignment>> GetManualAssignmentsByDateRangeAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true); 
    Task UpdateManualAssignmentAsync(ManualAssignment assignment); 
    Task DeleteManualAssignmentAsync(int id); 
    // HolidayConfig 
    Task<int> AddHolidayConfigAsync(HolidayConfig config); 
    Task<HolidayConfig?> GetActiveHolidayConfigAsync(); 
    Task<List<HolidayConfig>> GetAllHolidayConfigsAsync(); 
    Task UpdateHolidayConfigAsync(HolidayConfig config); 
    Task DeleteHolidayConfigAsync(int id);
}