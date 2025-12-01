using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Services.ImportExport.Comparison;
using AutoScheduling3.Services.ImportExport.Mappers;
using AutoScheduling3.Services.ImportExport.Models;

namespace AutoScheduling3.Services.ImportExport.Importers
{
    /// <summary>
    /// 模板导入器：处理排班模板数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// </summary>
    public class TemplateImporter : ImporterBase<SchedulingTemplateDto, SchedulingTemplate>
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ConstraintComparer _comparer;

        /// <summary>
        /// 初始化模板导入器
        /// </summary>
        /// <param name="templateRepository">模板仓储</param>
        /// <param name="logger">日志记录器</param>
        public TemplateImporter(ITemplateRepository templateRepository, ILogger logger)
            : base(logger)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _comparer = new ConstraintComparer();
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        protected override string GetTableName()
        {
            return "SchedulingTemplates";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(SchedulingTemplateDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 ConstraintComparer 进行比较
        /// </summary>
        protected override bool CompareRecords(SchedulingTemplateDto imported, SchedulingTemplate existing)
        {
            return _comparer.AreTemplatesEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射
        /// </summary>
        protected override Dictionary<string, object> MapToFields(SchedulingTemplateDto record)
        {
            return FieldMapper.MapTemplateToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录（单个）
        /// </summary>
        [Obsolete("此方法已过时，请使用 GetExistingRecordsBatchAsync", true)]
        protected override async Task<SchedulingTemplate?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 使用仓储直接查询，因为我们需要完整的模型对象进行比较
            return await _templateRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// 批量从数据库获取现有记录
        /// 使用一次性查询获取所有记录，避免逐个查询导致锁定
        /// </summary>
        protected override async Task<Dictionary<int, SchedulingTemplate>> GetExistingRecordsBatchAsync(List<int> ids, ImportContext context)
        {
            var result = new Dictionary<int, SchedulingTemplate>();

            if (ids == null || ids.Count == 0)
            {
                return result;
            }

            // 使用仓储的批量查询方法
            var allTemplates = await _templateRepository.GetAllAsync();
            
            // 过滤出需要的记录
            foreach (var template in allTemplates)
            {
                if (ids.Contains(template.Id))
                {
                    result[template.Id] = template;
                }
            }

            return result;
        }
    }
}
