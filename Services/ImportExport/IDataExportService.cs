using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.Services.ImportExport
{
    /// <summary>
    /// 数据导出服务接口
    /// 负责所有数据导出逻辑
    /// </summary>
    public interface IDataExportService
    {
        /// <summary>
        /// 导出技能数据
        /// </summary>
        /// <returns>技能DTO列表</returns>
        Task<List<SkillDto>> ExportSkillsAsync();
        
        /// <summary>
        /// 导出人员数据
        /// </summary>
        /// <returns>人员DTO列表</returns>
        Task<List<PersonnelDto>> ExportPersonnelAsync();
        
        /// <summary>
        /// 导出哨位数据
        /// </summary>
        /// <returns>哨位DTO列表</returns>
        Task<List<PositionDto>> ExportPositionsAsync();
        
        /// <summary>
        /// 导出排班模板数据
        /// </summary>
        /// <returns>排班模板DTO列表</returns>
        Task<List<SchedulingTemplateDto>> ExportTemplatesAsync();
        
        /// <summary>
        /// 导出固定分配约束数据
        /// </summary>
        /// <returns>固定分配DTO列表</returns>
        Task<List<FixedAssignmentDto>> ExportFixedAssignmentsAsync();
        
        /// <summary>
        /// 导出手动分配约束数据
        /// </summary>
        /// <returns>手动分配DTO列表</returns>
        Task<List<ManualAssignmentDto>> ExportManualAssignmentsAsync();
        
        /// <summary>
        /// 导出节假日配置数据
        /// </summary>
        /// <returns>节假日配置DTO列表</returns>
        Task<List<HolidayConfigDto>> ExportHolidayConfigsAsync();
        
        /// <summary>
        /// 计算数据统计信息
        /// </summary>
        /// <param name="exportData">导出数据对象</param>
        /// <returns>数据统计信息</returns>
        DataStatistics CalculateStatistics(ExportData exportData);
        
        /// <summary>
        /// 获取数据库版本
        /// </summary>
        /// <returns>数据库版本号</returns>
        Task<int> GetDatabaseVersionAsync();
        
        /// <summary>
        /// 获取应用程序版本
        /// </summary>
        /// <returns>应用程序版本字符串</returns>
        string GetApplicationVersion();
    }
}
