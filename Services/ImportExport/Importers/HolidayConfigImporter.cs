using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services.ImportExport.Comparison;
using AutoScheduling3.Services.ImportExport.Mappers;
using AutoScheduling3.Services.ImportExport.Models;

namespace AutoScheduling3.Services.ImportExport.Importers
{
    /// <summary>
    /// 节假日配置导入器：处理节假日配置数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// </summary>
    public class HolidayConfigImporter : ImporterBase<HolidayConfigDto, HolidayConfig>
    {
        private readonly IConstraintRepository _constraintRepository;
        private readonly ConstraintComparer _comparer;

        /// <summary>
        /// 初始化节假日配置导入器
        /// </summary>
        /// <param name="constraintRepository">约束仓储</param>
        /// <param name="logger">日志记录器</param>
        public HolidayConfigImporter(IConstraintRepository constraintRepository, ILogger logger)
            : base(logger)
        {
            _constraintRepository = constraintRepository ?? throw new ArgumentNullException(nameof(constraintRepository));
            _comparer = new ConstraintComparer();
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        protected override string GetTableName()
        {
            return "HolidayConfigs";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(HolidayConfigDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 ConstraintComparer 进行比较
        /// </summary>
        protected override bool CompareRecords(HolidayConfigDto imported, HolidayConfig existing)
        {
            return _comparer.AreHolidayConfigsEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射
        /// </summary>
        protected override Dictionary<string, object> MapToFields(HolidayConfigDto record)
        {
            return FieldMapper.MapHolidayConfigToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录
        /// </summary>
        protected override async Task<HolidayConfig?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 获取所有节假日配置并查找匹配的 ID
            var allConfigs = await _constraintRepository.GetAllHolidayConfigsAsync();
            return allConfigs.Find(c => c.Id == id);
        }
    }
}
