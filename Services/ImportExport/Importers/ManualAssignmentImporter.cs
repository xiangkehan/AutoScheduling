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
    /// 手动分配导入器：处理手动分配数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// </summary>
    public class ManualAssignmentImporter : ImporterBase<ManualAssignmentDto, ManualAssignment>
    {
        private readonly IConstraintRepository _constraintRepository;
        private readonly ConstraintComparer _comparer;

        /// <summary>
        /// 初始化手动分配导入器
        /// </summary>
        /// <param name="constraintRepository">约束仓储</param>
        /// <param name="logger">日志记录器</param>
        public ManualAssignmentImporter(IConstraintRepository constraintRepository, ILogger logger)
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
            return "ManualAssignments";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(ManualAssignmentDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 ConstraintComparer 进行比较
        /// </summary>
        protected override bool CompareRecords(ManualAssignmentDto imported, ManualAssignment existing)
        {
            return _comparer.AreManualAssignmentsEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射
        /// </summary>
        protected override Dictionary<string, object> MapToFields(ManualAssignmentDto record)
        {
            return FieldMapper.MapManualAssignmentToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录
        /// </summary>
        protected override async Task<ManualAssignment?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 获取一个较大的日期范围内的所有手动分配并查找匹配的 ID
            // 使用一个足够大的范围来覆盖可能的所有手动分配
            var startDate = DateTime.MinValue;
            var endDate = DateTime.MaxValue;
            var allAssignments = await _constraintRepository.GetManualAssignmentsByDateRangeAsync(
                startDate, endDate, enabledOnly: false);
            return allAssignments.Find(a => a.Id == id);
        }
    }
}
