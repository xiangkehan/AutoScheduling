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
    /// 人员导入器：处理人员数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// 处理数组字段（SkillIds, RecentPeriodShiftIntervals）
    /// </summary>
    public class PersonnelImporter : ImporterBase<PersonnelDto, Personal>
    {
        private readonly IPersonalRepository _personnelRepository;
        private readonly CoreEntityComparer _comparer;

        /// <summary>
        /// 初始化人员导入器
        /// </summary>
        /// <param name="personnelRepository">人员仓储</param>
        /// <param name="logger">日志记录器</param>
        public PersonnelImporter(IPersonalRepository personnelRepository, ILogger logger)
            : base(logger)
        {
            _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
            _comparer = new CoreEntityComparer();
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        protected override string GetTableName()
        {
            return "Personals";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(PersonnelDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 CoreEntityComparer.ArePersonnelEqual 进行比较
        /// 处理数组字段（SkillIds, RecentPeriodShiftIntervals）
        /// </summary>
        protected override bool CompareRecords(PersonnelDto imported, Personal existing)
        {
            return _comparer.ArePersonnelEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射，处理数组字段的序列化
        /// </summary>
        protected override Dictionary<string, object> MapToFields(PersonnelDto record)
        {
            return FieldMapper.MapPersonnelToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录
        /// </summary>
        protected override async Task<Personal?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 使用仓储直接查询，因为我们需要完整的模型对象进行比较
            return await _personnelRepository.GetByIdAsync(id);
        }
    }
}
