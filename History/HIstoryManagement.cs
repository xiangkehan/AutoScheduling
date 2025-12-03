using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;

namespace AutoScheduling3.History
{
    /// <summary>
    /// 历史管理类：负责管理排哨历史记录，包括已确认的历史和缓冲区
    /// </summary>
    public class HistoryManagement : IHistoryManagement
    {
        private readonly string _connectionString;
        private readonly Data.SchedulingRepository _schedulingRepo;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbPath">数据库文件路径</param>
        public HistoryManagement(string dbPath)
        {
            // 使用优化的连接字符串，启用 WAL 模式以支持并发访问
            _connectionString = Data.DatabaseConfiguration.GetOptimizedConnectionString(dbPath);
            _schedulingRepo = new Data.SchedulingRepository(dbPath);
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public async Task InitAsync()
        {
            // 初始化排班表
            await _schedulingRepo.InitAsync();

            // 初始化历史记录表
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS HistorySchedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleId INTEGER NOT NULL,
    ConfirmTime TEXT NOT NULL, -- ISO8601
    FOREIGN KEY (ScheduleId) REFERENCES Schedules(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS BufferSchedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleId INTEGER NOT NULL UNIQUE,
    CreateTime TEXT NOT NULL, -- ISO8601
    FOREIGN KEY (ScheduleId) REFERENCES Schedules(Id) ON DELETE CASCADE
);
";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 添加排哨结果到缓冲区
        /// </summary>
        /// <param name="schedule">排班表对象</param>
        /// <returns>缓冲区ID</returns>
        public async Task<int> AddToBufferAsync(Schedule schedule)
        {
            // 先保存排班表（使用 SchedulingRepository 的内部事务）
            int scheduleId = await _schedulingRepo.CreateAsync(schedule);

            // 然后在单独的连接中添加到缓冲区
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO BufferSchedules (ScheduleId, CreateTime) VALUES (@scheduleId, @createTime); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
            cmd.Parameters.AddWithValue("@createTime", DateTime.UtcNow.ToString("o"));
            var bufferIdObj = await cmd.ExecuteScalarAsync();
            int bufferId = Convert.ToInt32(bufferIdObj);

            return bufferId;
        }

        /// <summary>
        /// 确认实施某一次排哨，将其从缓冲区移到历史记录
        /// </summary>
        /// <param name="bufferId">缓冲区ID</param>
        public async Task ConfirmBufferScheduleAsync(int bufferId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            try
            {
                // 获取缓冲区中的排班ID
                var getCmd = conn.CreateCommand();
                getCmd.Transaction = tx;
                getCmd.CommandText = "SELECT ScheduleId FROM BufferSchedules WHERE Id = @bufferId";
                getCmd.Parameters.AddWithValue("@bufferId", bufferId);
                var result = await getCmd.ExecuteScalarAsync();

                if (result == null)
                    throw new InvalidOperationException("缓冲区中未找到指定的排哨记录");

                int scheduleId = Convert.ToInt32(result);

                // 添加到历史记录
                var historyCmd = conn.CreateCommand();
                historyCmd.Transaction = tx;
                historyCmd.CommandText = "INSERT INTO HistorySchedules (ScheduleId, ConfirmTime) VALUES (@scheduleId, @confirmTime)";
                historyCmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                historyCmd.Parameters.AddWithValue("@confirmTime", DateTime.UtcNow.ToString("o"));
                await historyCmd.ExecuteNonQueryAsync();

                // 从缓冲区移除
                var bufferCmd = conn.CreateCommand();
                bufferCmd.Transaction = tx;
                bufferCmd.CommandText = "DELETE FROM BufferSchedules WHERE Id = @bufferId";
                bufferCmd.Parameters.AddWithValue("@bufferId", bufferId);
                await bufferCmd.ExecuteNonQueryAsync();

                // 清空其余缓冲区内容
                var clearCmd = conn.CreateCommand();
                clearCmd.Transaction = tx;
                clearCmd.CommandText = "DELETE FROM BufferSchedules";
                await clearCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 获取所有历史记录的排班表
        /// </summary>
        /// <returns>历史记录列表</returns>
        public async Task<List<(Schedule Schedule, DateTime ConfirmTime)>> GetAllHistorySchedulesAsync()
        {
            var result = new List<(Schedule, DateTime)>();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ScheduleId, ConfirmTime FROM HistorySchedules ORDER BY ConfirmTime DESC";
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int scheduleId = reader.GetInt32(0);
                DateTime confirmTime = DateTime.Parse(reader.GetString(1)).ToUniversalTime();

                var schedule = await _schedulingRepo.GetByIdAsync(scheduleId);
                if (schedule != null)
                {
                    result.Add((schedule, confirmTime));
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有缓冲区中的排班表
        /// </summary>
        /// <returns>缓冲区列表</returns>
        public async Task<List<(Schedule Schedule, DateTime CreateTime, int BufferId)>> GetAllBufferSchedulesAsync()
        {
            var result = new List<(Schedule, DateTime, int)>();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ScheduleId, CreateTime FROM BufferSchedules ORDER BY CreateTime DESC";
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int bufferId = reader.GetInt32(0);
                int scheduleId = reader.GetInt32(1);
                DateTime createTime = ParseDateTime(reader.GetString(2));

                var schedule = await _schedulingRepo.GetByIdAsync(scheduleId);
                if (schedule != null)
                {
                    result.Add((schedule, createTime, bufferId));
                }
            }

            return result;
        }

        /// <summary>
        /// 清空所有缓冲区内容
        /// </summary>
        public async Task ClearBufferAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            try
            {
                // 获取所有缓冲区中的ScheduleId
                var getCmd = conn.CreateCommand();
                getCmd.Transaction = tx;
                getCmd.CommandText = "SELECT ScheduleId FROM BufferSchedules";
                var scheduleIds = new List<int>();
                
                using (var reader = await getCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        scheduleIds.Add(reader.GetInt32(0));
                    }
                }

                // 删除缓冲区记录
                var deleteBufferCmd = conn.CreateCommand();
                deleteBufferCmd.Transaction = tx;
                deleteBufferCmd.CommandText = "DELETE FROM BufferSchedules";
                await deleteBufferCmd.ExecuteNonQueryAsync();
                
                // 删除对应的排班记录
                foreach (var scheduleId in scheduleIds)
                {
                    await _schedulingRepo.DeleteAsync(scheduleId);
                }
                
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 删除指定的历史记录
        /// </summary>
        /// <param name="scheduleId">排班ID</param>
        public async Task DeleteHistoryScheduleAsync(int scheduleId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            try
            {
                // 从历史记录表中删除
                var historyCmd = conn.CreateCommand();
                historyCmd.Transaction = tx;
                historyCmd.CommandText = "DELETE FROM HistorySchedules WHERE ScheduleId = @scheduleId";
                historyCmd.Parameters.AddWithValue("@scheduleId", scheduleId);
                await historyCmd.ExecuteNonQueryAsync();

                // 从排班表中删除
                await _schedulingRepo.DeleteAsync(scheduleId);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 删除指定的缓冲区记录
        /// </summary>
        /// <param name="bufferId">缓冲区ID</param>
        public async Task DeleteBufferScheduleAsync(int bufferId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            try
            {
                // 获取对应的ScheduleId
                var getCmd = conn.CreateCommand();
                getCmd.Transaction = tx;
                getCmd.CommandText = "SELECT ScheduleId FROM BufferSchedules WHERE Id = @bufferId";
                getCmd.Parameters.AddWithValue("@bufferId", bufferId);
                var result = await getCmd.ExecuteScalarAsync();

                if (result != null)
                {
                    int scheduleId = Convert.ToInt32(result);

                    // 从缓冲区表中删除
                    var bufferCmd = conn.CreateCommand();
                    bufferCmd.Transaction = tx;
                    bufferCmd.CommandText = "DELETE FROM BufferSchedules WHERE Id = @bufferId";
                    bufferCmd.Parameters.AddWithValue("@bufferId", bufferId);
                    await bufferCmd.ExecuteNonQueryAsync();

                    // 从排班表中删除
                    await _schedulingRepo.DeleteAsync(scheduleId);
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 获取最近一次确认的排班表（用于算法历史约束处理）
        /// </summary>
        /// <returns>最近确认的排班表，如果没有则返回 null</returns>
        public async Task<Schedule?> GetLastConfirmedScheduleAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ScheduleId FROM HistorySchedules ORDER BY ConfirmTime DESC LIMIT 1";
            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                return null;

            int scheduleId = Convert.ToInt32(result);
            return await _schedulingRepo.GetByIdAsync(scheduleId);
        }

        /// <summary>
        /// 通过排班ID获取单个历史记录项
        /// </summary>
        /// <param name="scheduleId">排班ID</param>
        /// <returns>历史记录项，如果未找到则返回null</returns>
        public async Task<(Schedule Schedule, DateTime ConfirmTime)?> GetHistoryScheduleByScheduleIdAsync(int scheduleId)
        {
            var all = await GetAllHistorySchedulesAsync();
            var item = all.FirstOrDefault(h => h.Schedule.Id == scheduleId);
            if (item.Schedule == null) return null;
            return item;
        }

        /// <summary>
        /// 更新缓冲区中的排班表（用于保存进度草稿）
        /// </summary>
        public async Task UpdateBufferScheduleAsync(Schedule schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            // 更新排班表
            await _schedulingRepo.UpdateAsync(schedule);

            // 更新缓冲区记录的时间戳
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE BufferSchedules SET CreateTime = @createTime WHERE ScheduleId = @scheduleId";
            cmd.Parameters.AddWithValue("@createTime", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@scheduleId", schedule.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 从缓冲区获取排班表（用于恢复进度草稿）
        /// </summary>
        public async Task<Schedule?> GetBufferScheduleAsync(int bufferId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ScheduleId FROM BufferSchedules WHERE Id = @bufferId";
            cmd.Parameters.AddWithValue("@bufferId", bufferId);
            
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
            {
                return null;
            }

            int scheduleId = Convert.ToInt32(result);
            return await _schedulingRepo.GetByIdAsync(scheduleId);
        }

        /// <summary>
        /// 解析日期时间字符串，支持 ISO 8601 格式和 Ticks 格式（兼容旧数据）
        /// </summary>
        private DateTime ParseDateTime(string value)
        {
            // 尝试解析为 ISO 8601 格式
            if (DateTime.TryParse(value, out var dateTime))
            {
                return dateTime.ToUniversalTime();
            }

            // 尝试解析为 Ticks 格式（兼容旧数据）
            if (long.TryParse(value, out var ticks))
            {
                try
                {
                    return new DateTime(ticks, DateTimeKind.Utc);
                }
                catch
                {
                    // Ticks 值无效，抛出异常
                    throw new FormatException($"无法将字符串 '{value}' 解析为有效的日期时间。");
                }
            }

            throw new FormatException($"无法将字符串 '{value}' 解析为有效的日期时间。");
        }
    }
}
