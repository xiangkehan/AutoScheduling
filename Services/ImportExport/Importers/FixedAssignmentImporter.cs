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
    /// 固定分配导入器：处理固定分配（定岗规则）数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// </summary>
    public class FixedAssignmentImporter : ImporterBase<FixedAssignmentDto, FixedPositionRule>
    {
        private readonly IConstraintRepository _constraintRepository;
        private readonly ConstraintComparer _comparer;

        /// <summary>
        /// 初始化固定分配导入器
        /// </summary>
        /// <param name="constraintRepository">约束仓储</param>
        /// <param name="logger">日志记录器</param>
        public FixedAssignmentImporter(IConstraintRepository constraintRepository, ILogger logger)
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
            return "FixedPositionRules";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(FixedAssignmentDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 ConstraintComparer 进行比较
        /// </summary>
        protected override bool CompareRecords(FixedAssignmentDto imported, FixedPositionRule existing)
        {
            return _comparer.AreFixedAssignmentsEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射
        /// </summary>
        protected override Dictionary<string, object> MapToFields(FixedAssignmentDto record)
        {
            return FieldMapper.MapFixedAssignmentToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录（单个）
        /// </summary>
        [Obsolete("此方法已过时，请使用 GetExistingRecordsBatchAsync", true)]
        protected override async Task<FixedPositionRule?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 获取所有固定分配规则并查找匹配的 ID
            var allRules = await _constraintRepository.GetAllFixedPositionRulesAsync(enabledOnly: false);
            return allRules.Find(r => r.Id == id);
        }

        /// <summary>
        /// 批量从数据库获取现有记录
        /// 使用一次性查询获取所有记录，避免逐个查询导致锁定
        /// </summary>
        protected override async Task<Dictionary<int, FixedPositionRule>> GetExistingRecordsBatchAsync(List<int> ids, ImportContext context)
        {
            var result = new Dictionary<int, FixedPositionRule>();

            if (ids == null || ids.Count == 0)
            {
                return result;
            }

            // 获取所有固定分配规则
            var allRules = await _constraintRepository.GetAllFixedPositionRulesAsync(enabledOnly: false);
            
            // 过滤出需要的记录
            foreach (var rule in allRules)
            {
                if (ids.Contains(rule.Id))
                {
                    result[rule.Id] = rule;
                }
            }

            return result;
        }
    }
}
