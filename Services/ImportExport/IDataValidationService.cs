using System.Threading.Tasks;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.Services.ImportExport
{
    /// <summary>
    /// 数据验证服务接口
    /// 负责所有数据验证逻辑
    /// </summary>
    public interface IDataValidationService
    {
        /// <summary>
        /// 验证导入数据的完整性和正确性
        /// </summary>
        /// <param name="filePath">要验证的文件路径</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateImportDataAsync(string filePath);
        
        /// <summary>
        /// 验证必需字段存在性和数据类型
        /// </summary>
        /// <param name="exportData">导出数据对象</param>
        /// <param name="result">验证结果对象</param>
        void ValidateRequiredFields(ExportData exportData, ValidationResult result);
        
        /// <summary>
        /// 验证数据约束（字符串长度、数值范围、唯一性）
        /// </summary>
        /// <param name="exportData">导出数据对象</param>
        /// <param name="result">验证结果对象</param>
        void ValidateDataConstraints(ExportData exportData, ValidationResult result);
        
        /// <summary>
        /// 验证外键引用完整性
        /// </summary>
        /// <param name="exportData">导出数据对象</param>
        /// <param name="result">验证结果对象</param>
        Task ValidateForeignKeyReferences(ExportData exportData, ValidationResult result);
    }
}
