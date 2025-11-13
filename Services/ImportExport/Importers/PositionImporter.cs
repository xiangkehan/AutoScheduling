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
    /// 哨位导入器：处理哨位数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// 处理数组字段（RequiredSkillIds, AvailablePersonnelIds）
    /// </summary>
    public class PositionImporter : ImporterBase<PositionDto, PositionLocation>
    {
        private readonly IPositionRepository _positionRepository;
        private readonly CoreEntityComparer _comparer;

        /// <summary>
        /// 初始化哨位导入器
        /// </summary>
        /// <param name="positionRepository">哨位仓储</param>
        /// <param name="logger">日志记录器</param>
        public PositionImporter(IPositionRepository positionRepository, ILogger logger)
            : base(logger)
        {
            _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
            _comparer = new CoreEntityComparer();
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        protected override string GetTableName()
        {
            return "Positions";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(PositionDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 CoreEntityComparer.ArePositionsEqual 进行比较
        /// 处理数组字段（RequiredSkillIds, AvailablePersonnelIds）
        /// </summary>
        protected override bool CompareRecords(PositionDto imported, PositionLocation existing)
        {
            return _comparer.ArePositionsEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射，处理数组字段的序列化
        /// </summary>
        protected override Dictionary<string, object> MapToFields(PositionDto record)
        {
            return FieldMapper.MapPositionToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录
        /// </summary>
        protected override async Task<PositionLocation?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 使用仓储直接查询，因为我们需要完整的模型对象进行比较
            return await _positionRepository.GetByIdAsync(id);
        }
    }
}
