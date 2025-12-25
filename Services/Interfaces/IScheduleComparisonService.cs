using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.DTOs.Comparison;

namespace AutoScheduling3.Services.Interfaces
{
    /// <summary>
    /// 排班对比服务接口
    /// </summary>
    public interface IScheduleComparisonService
    {
        /// <summary>
        /// 计算两个排班表的差异
        /// </summary>
        /// <param name="scheduleId1">排班表1的ID</param>
        /// <param name="scheduleId2">排班表2的ID</param>
        /// <returns>对比结果</returns>
        Task<ComparisonResultDto> CompareSchedulesAsync(int scheduleId1, int scheduleId2);

        /// <summary>
        /// 筛选差异列表
        /// </summary>
        /// <param name="differences">原始差异列表</param>
        /// <param name="typeFilter">差异类型筛选（可选）</param>
        /// <param name="positionFilter">哨位ID筛选（可选）</param>
        /// <param name="personnelFilter">人员ID筛选（可选）</param>
        /// <returns>筛选后的差异列表</returns>
        List<ShiftDiffDto> FilterDifferences(
            List<ShiftDiffDto> differences,
            DiffType? typeFilter,
            int? positionFilter,
            int? personnelFilter);

        /// <summary>
        /// 导出对比结果为Excel
        /// </summary>
        /// <param name="result">对比结果</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> ExportToExcelAsync(ComparisonResultDto result);

        /// <summary>
        /// 导出对比结果为JSON
        /// </summary>
        /// <param name="result">对比结果</param>
        /// <returns>JSON字符串</returns>
        string ExportToJson(ComparisonResultDto result);

        /// <summary>
        /// 构建网格对比数据
        /// </summary>
        /// <param name="result">对比结果</param>
        /// <returns>网格对比数据</returns>
        GridComparisonDto BuildGridData(ComparisonResultDto result);
    }
}
