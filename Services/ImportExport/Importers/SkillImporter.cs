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
using Microsoft.Data.Sqlite;

namespace AutoScheduling3.Services.ImportExport.Importers
{
    /// <summary>
    /// 技能导入器：处理技能数据的导入操作
    /// 继承 ImporterBase 实现通用的导入流程
    /// </summary>
    public class SkillImporter : ImporterBase<SkillDto, Skill>
    {
        private readonly ISkillRepository _skillRepository;
        private readonly CoreEntityComparer _comparer;

        /// <summary>
        /// 初始化技能导入器
        /// </summary>
        /// <param name="skillRepository">技能仓储</param>
        /// <param name="logger">日志记录器</param>
        public SkillImporter(ISkillRepository skillRepository, ILogger logger)
            : base(logger)
        {
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _comparer = new CoreEntityComparer();
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        protected override string GetTableName()
        {
            return "Skills";
        }

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected override int GetRecordId(SkillDto record)
        {
            return record.Id;
        }

        /// <summary>
        /// 比较两条记录是否相等
        /// 使用 CoreEntityComparer 进行比较
        /// </summary>
        protected override bool CompareRecords(SkillDto imported, Skill existing)
        {
            return _comparer.AreSkillsEqual(imported, existing);
        }

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// 使用 FieldMapper 进行映射
        /// </summary>
        protected override Dictionary<string, object> MapToFields(SkillDto record)
        {
            return FieldMapper.MapSkillToFields(record);
        }

        /// <summary>
        /// 从数据库获取现有记录
        /// </summary>
        protected override async Task<Skill?> GetExistingRecordAsync(int id, ImportContext context)
        {
            // 使用仓储直接查询，因为我们需要完整的模型对象进行比较
            return await _skillRepository.GetByIdAsync(id);
        }
    }
}
