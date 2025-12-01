using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs.ImportExport;
using AutoScheduling3.Services.ImportExport.Batch;
using AutoScheduling3.Services.ImportExport.Models;

namespace AutoScheduling3.Services.ImportExport.Importers
{
    /// <summary>
    /// 导入处理器抽象基类
    /// 提供通用的导入流程框架，实现 Replace、Skip、Merge 策略的通用逻辑
    /// </summary>
    /// <typeparam name="TDto">导入数据的 DTO 类型</typeparam>
    /// <typeparam name="TModel">数据库模型类型</typeparam>
    public abstract class ImporterBase<TDto, TModel>
    {
        protected readonly ILogger _logger;
        protected readonly BatchExistenceChecker _existenceChecker;
        protected readonly BatchImporter _batchImporter;

        protected ImporterBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _existenceChecker = new BatchExistenceChecker();
            _batchImporter = new BatchImporter();
        }

        /// <summary>
        /// 执行导入操作的主方法
        /// </summary>
        /// <param name="records">要导入的记录列表</param>
        /// <param name="context">导入上下文</param>
        /// <param name="progress">进度报告器</param>
        public async Task ImportAsync(
            List<TDto> records,
            ImportContext context,
            IProgress<ImportProgress>? progress = null)
        {
            if (records == null || records.Count == 0)
            {
                _logger.Log($"{GetTableName()}: No records to import");
                return;
            }

            var tableName = GetTableName();
            _logger.Log($"{tableName}: Starting import of {records.Count} records");

            context.PerformanceMonitor.StartOperation(tableName);

            var tableStats = new TableImportStats { Total = records.Count };

            try
            {
                // 1. 批量查询现有 IDs
                var idsToCheck = records.Select(GetRecordId).ToList();
                var existingIds = await _existenceChecker.GetExistingIdsAsync(
                    idsToCheck,
                    context.Connection,
                    context.Transaction,
                    tableName);

                _logger.Log($"{tableName}: Found {existingIds.Count} existing records out of {records.Count}");

                // 2. 分类记录
                var (toInsert, toUpdate) = _existenceChecker.ClassifyRecords(
                    records,
                    existingIds,
                    GetRecordId);

                // 3. 根据策略处理
                await ProcessRecordsByStrategyAsync(
                    toInsert,
                    toUpdate,
                    context,
                    tableStats);

                // 4. 更新统计信息
                context.Statistics.DetailsByTable[tableName] = tableStats;
                context.Statistics.RecordsByTable[tableName] = tableStats.Inserted + tableStats.Updated;
                context.Statistics.InsertedRecords += tableStats.Inserted;
                context.Statistics.UpdatedRecords += tableStats.Updated;
                context.Statistics.UnchangedRecords += tableStats.Unchanged;
                context.Statistics.SkippedRecords += tableStats.Skipped;

                // 5. 报告进度
                progress?.Report(new ImportProgress
                {
                    CurrentTable = tableName,
                    ProcessedRecords = records.Count,
                    TotalRecords = records.Count,
                    PercentComplete = CalculateProgressPercentage(tableName)
                });

                _logger.Log($"{tableName}: Import completed - Inserted: {tableStats.Inserted}, " +
                           $"Updated: {tableStats.Updated}, Unchanged: {tableStats.Unchanged}, " +
                           $"Skipped: {tableStats.Skipped}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{tableName}: Import failed - {ex.Message}");
                tableStats.Skipped = records.Count;
                context.Statistics.FailedRecords += records.Count;
                throw;
            }
            finally
            {
                context.PerformanceMonitor.EndOperation(tableName);
                tableStats.Duration = context.PerformanceMonitor.OperationTimings[tableName];
            }
        }

        /// <summary>
        /// 根据导入策略处理记录
        /// </summary>
        private async Task ProcessRecordsByStrategyAsync(
            List<TDto> toInsert,
            List<TDto> toUpdate,
            ImportContext context,
            TableImportStats tableStats)
        {
            var tableName = GetTableName();

            switch (context.Options.Strategy)
            {
                case ConflictResolutionStrategy.Replace:
                    await ProcessReplaceStrategyAsync(toInsert, toUpdate, context, tableStats);
                    break;

                case ConflictResolutionStrategy.Skip:
                    await ProcessSkipStrategyAsync(toInsert, toUpdate, context, tableStats);
                    break;

                case ConflictResolutionStrategy.Merge:
                    await ProcessMergeStrategyAsync(toInsert, toUpdate, context, tableStats);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown strategy: {context.Options.Strategy}");
            }
        }

        /// <summary>
        /// 处理 Replace 策略：插入新记录，直接更新所有现有记录（不比较）
        /// 
        /// 注意：为了避免在事务中读取数据导致锁定，Replace 策略直接更新所有现有记录，
        /// 不进行变化检测。这样可以显著提高性能并避免数据库锁定问题。
        /// </summary>
        private async Task ProcessReplaceStrategyAsync(
            List<TDto> toInsert,
            List<TDto> toUpdate,
            ImportContext context,
            TableImportStats tableStats)
        {
            var tableName = GetTableName();

            // 批量插入新记录
            if (toInsert.Count > 0)
            {
                _logger.Log($"{tableName}: Inserting {toInsert.Count} new records");
                var inserted = await _batchImporter.BatchInsertAsync(
                    toInsert,
                    context.Connection,
                    context.Transaction,
                    tableName,
                    MapToFields);
                tableStats.Inserted = inserted;
            }

            // 直接批量更新所有现有记录（不进行变化检测以避免锁定）
            if (toUpdate.Count > 0)
            {
                _logger.Log($"{tableName}: Updating {toUpdate.Count} existing records (Replace strategy - no change detection)");
                var updated = await _batchImporter.BatchUpdateAsync(
                    toUpdate,
                    context.Connection,
                    context.Transaction,
                    tableName,
                    MapToFields,
                    GetRecordId);
                tableStats.Updated = updated;
                tableStats.Unchanged = 0; // Replace 策略不检测未变化的记录
            }
        }

        /// <summary>
        /// 处理 Skip 策略：仅插入新记录，跳过现有记录
        /// </summary>
        private async Task ProcessSkipStrategyAsync(
            List<TDto> toInsert,
            List<TDto> toUpdate,
            ImportContext context,
            TableImportStats tableStats)
        {
            var tableName = GetTableName();

            // 仅插入新记录
            if (toInsert.Count > 0)
            {
                _logger.Log($"{tableName}: Inserting {toInsert.Count} new records");
                var inserted = await _batchImporter.BatchInsertAsync(
                    toInsert,
                    context.Connection,
                    context.Transaction,
                    tableName,
                    MapToFields);
                tableStats.Inserted = inserted;
            }

            // 跳过现有记录
            tableStats.Skipped = toUpdate.Count;
            if (toUpdate.Count > 0)
            {
                _logger.Log($"{tableName}: Skipped {toUpdate.Count} existing records");
            }
        }

        /// <summary>
        /// 处理 Merge 策略：插入新记录，更新现有记录
        /// 当前实现与 Replace 相同
        /// </summary>
        private async Task ProcessMergeStrategyAsync(
            List<TDto> toInsert,
            List<TDto> toUpdate,
            ImportContext context,
            TableImportStats tableStats)
        {
            // Merge 策略当前与 Replace 策略行为相同
            await ProcessReplaceStrategyAsync(toInsert, toUpdate, context, tableStats);
        }

        /// <summary>
        /// 过滤掉未变化的记录，仅返回需要更新的记录
        /// </summary>
        private async Task<List<TDto>> FilterUnchangedRecordsAsync(
            List<TDto> records,
            ImportContext context)
        {
            var recordsToUpdate = new List<TDto>();
            var tableName = GetTableName();

            // 批量获取所有需要比较的现有记录，避免逐个查询导致锁定
            var idsToFetch = records.Select(GetRecordId).ToList();
            var existingRecords = await GetExistingRecordsBatchAsync(idsToFetch, context);

            foreach (var record in records)
            {
                try
                {
                    var recordId = GetRecordId(record);
                    
                    // 从批量获取的结果中查找对应的记录
                    if (existingRecords.TryGetValue(recordId, out var existing))
                    {
                        if (!CompareRecords(record, existing))
                        {
                            recordsToUpdate.Add(record);
                        }
                    }
                    else
                    {
                        // 如果找不到现有记录，保守地包含该记录进行更新
                        _logger.LogWarning($"{tableName}: Record {recordId} not found in batch fetch, including for update");
                        recordsToUpdate.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{tableName}: Failed to compare record {GetRecordId(record)} - {ex.Message}");
                    // 如果比较失败，保守地包含该记录进行更新
                    recordsToUpdate.Add(record);
                }
            }

            return recordsToUpdate;
        }

        /// <summary>
        /// 计算当前表的进度百分比
        /// 子类可以重写以提供更准确的进度计算
        /// </summary>
        protected virtual double CalculateProgressPercentage(string tableName)
        {
            // 默认实现：根据表名返回固定的进度百分比
            // 这是一个简化的实现，实际应用中可能需要更复杂的逻辑
            return tableName switch
            {
                "Skills" => 30,
                "Personnel" => 40,
                "Positions" => 50,
                "HolidayConfigs" => 60,
                "Templates" => 70,
                "FixedAssignments" => 80,
                "ManualAssignments" => 90,
                _ => 50
            };
        }

        #region Abstract Methods - Must be implemented by subclasses

        /// <summary>
        /// 获取表名
        /// </summary>
        protected abstract string GetTableName();

        /// <summary>
        /// 获取记录的 ID
        /// </summary>
        protected abstract int GetRecordId(TDto record);

        /// <summary>
        /// 比较两条记录是否相等
        /// </summary>
        /// <returns>如果记录相等返回 true，否则返回 false</returns>
        protected abstract bool CompareRecords(TDto imported, TModel existing);

        /// <summary>
        /// 将 DTO 映射为字段字典（用于 INSERT/UPDATE）
        /// </summary>
        protected abstract Dictionary<string, object> MapToFields(TDto record);

        /// <summary>
        /// 从数据库获取现有记录（单个）
        /// </summary>
        [Obsolete("此方法已过时，请使用 GetExistingRecordsBatchAsync 进行批量获取以避免锁定问题", true)]
        protected abstract Task<TModel?> GetExistingRecordAsync(int id, ImportContext context);

        /// <summary>
        /// 批量从数据库获取现有记录
        /// 使用批量查询避免在事务中逐个查询导致表锁定
        /// </summary>
        /// <param name="ids">要获取的记录 ID 列表</param>
        /// <param name="context">导入上下文</param>
        /// <returns>ID 到模型对象的字典</returns>
        protected abstract Task<Dictionary<int, TModel>> GetExistingRecordsBatchAsync(List<int> ids, ImportContext context);

        #endregion
    }
}
