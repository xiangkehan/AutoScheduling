using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 人员数据访问层：管理人员信息的 CRUD 操作
    /// </summary>
    public class PersonalRepository : IPersonalRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

        public PersonalRepository(string dbPath)
        {
            _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        }

        /// <summary>
        /// 初始化数据库表
        /// </summary>
        public async Task InitAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Personals (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    PositionId INTEGER NOT NULL,
    SkillIds TEXT NOT NULL, -- JSON array of ints
    IsAvailable INTEGER NOT NULL DEFAULT 1, -- SQLite boolean (0/1)
    IsRetired INTEGER NOT NULL DEFAULT 0,
    RecentShiftIntervalCount INTEGER NOT NULL DEFAULT 0,
    RecentHolidayShiftIntervalCount INTEGER NOT NULL DEFAULT 0,
    RecentPeriodShiftIntervals TEXT NOT NULL -- JSON array of 12 ints
);";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 添加人员
        /// </summary>
        public async Task<int> CreateAsync(Personal person)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Personals (Name, PositionId, SkillIds, IsAvailable, IsRetired, 
                       RecentShiftIntervalCount, RecentHolidayShiftIntervalCount, RecentPeriodShiftIntervals)
VALUES (@name, @posId, @skillIds, @isAvail, @isRetired, @recentShift, @recentHoliday, @periodIntervals);
SELECT last_insert_rowid();";
            
            cmd.Parameters.AddWithValue("@name", person.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@posId", person.PositionId);
            cmd.Parameters.AddWithValue("@skillIds", JsonSerializer.Serialize(person.SkillIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@isAvail", person.IsAvailable ? 1 : 0);
            cmd.Parameters.AddWithValue("@isRetired", person.IsRetired ? 1 : 0);
            cmd.Parameters.AddWithValue("@recentShift", person.RecentShiftIntervalCount);
            cmd.Parameters.AddWithValue("@recentHoliday", person.RecentHolidayShiftIntervalCount);
            cmd.Parameters.AddWithValue("@periodIntervals", JsonSerializer.Serialize(person.RecentPeriodShiftIntervals, _jsonOptions));

            var result = await cmd.ExecuteScalarAsync();
            person.Id = Convert.ToInt32(result);
            return person.Id;
        }

        /// <summary>
        /// 根据ID查询人员
        /// </summary>
        public async Task<Personal?> GetByIdAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT Id, Name, PositionId, SkillIds, IsAvailable, IsRetired,
       RecentShiftIntervalCount, RecentHolidayShiftIntervalCount, RecentPeriodShiftIntervals
FROM Personals WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapPerson(reader);
            }

            return null;
        }

        /// <summary>
        /// 查询所有人员
        /// </summary>
        public async Task<List<Personal>> GetAllAsync()
        {
            var list = new List<Personal>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT Id, Name, PositionId, SkillIds, IsAvailable, IsRetired,
       RecentShiftIntervalCount, RecentHolidayShiftIntervalCount, RecentPeriodShiftIntervals
FROM Personals ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPerson(reader));
            }

            return list;
        }

        /// <summary>
        /// 批量查询人员（算法核心功能）
        /// </summary>
        public async Task<List<Personal>> GetByIdsAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<Personal>();

            var list = new List<Personal>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // 构建 IN 子句
            var idsStr = string.Join(",", ids);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
SELECT Id, Name, PositionId, SkillIds, IsAvailable, IsRetired,
       RecentShiftIntervalCount, RecentHolidayShiftIntervalCount, RecentPeriodShiftIntervals
FROM Personals WHERE Id IN ({idsStr}) ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPerson(reader));
            }

            return list;
        }

        /// <summary>
        /// 更新人员信息
        /// </summary>
        public async Task UpdateAsync(Personal person)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE Personals SET 
    Name = @name, 
    PositionId = @posId, 
    SkillIds = @skillIds,
    IsAvailable = @isAvail,
    IsRetired = @isRetired,
    RecentShiftIntervalCount = @recentShift,
    RecentHolidayShiftIntervalCount = @recentHoliday,
    RecentPeriodShiftIntervals = @periodIntervals
WHERE Id = @id";

            cmd.Parameters.AddWithValue("@name", person.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@posId", person.PositionId);
            cmd.Parameters.AddWithValue("@skillIds", JsonSerializer.Serialize(person.SkillIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@isAvail", person.IsAvailable ? 1 : 0);
            cmd.Parameters.AddWithValue("@isRetired", person.IsRetired ? 1 : 0);
            cmd.Parameters.AddWithValue("@recentShift", person.RecentShiftIntervalCount);
            cmd.Parameters.AddWithValue("@recentHoliday", person.RecentHolidayShiftIntervalCount);
            cmd.Parameters.AddWithValue("@periodIntervals", JsonSerializer.Serialize(person.RecentPeriodShiftIntervals, _jsonOptions));
            cmd.Parameters.AddWithValue("@id", person.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 更新人员的间隔计数（排班确认后调用）
        /// </summary>
        public async Task UpdateIntervalCountsAsync(int personalId, int recentShiftInterval, int recentHolidayInterval, int[] periodIntervals)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE Personals SET 
    RecentShiftIntervalCount = @recentShift,
    RecentHolidayShiftIntervalCount = @recentHoliday,
    RecentPeriodShiftIntervals = @periodIntervals
WHERE Id = @id";

            cmd.Parameters.AddWithValue("@recentShift", recentShiftInterval);
            cmd.Parameters.AddWithValue("@recentHoliday", recentHolidayInterval);
            cmd.Parameters.AddWithValue("@periodIntervals", JsonSerializer.Serialize(periodIntervals, _jsonOptions));
            cmd.Parameters.AddWithValue("@id", personalId);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 删除人员
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Personals WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 检查人员是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Personals WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// 映射数据库记录到 Personal 对象
        /// </summary>
        private Personal MapPerson(SqliteDataReader reader)
        {
            var skillIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>();
            var periodIntervals = JsonSerializer.Deserialize<int[]>(reader.GetString(8)) ?? new int[12];

            return new Personal
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                PositionId = reader.GetInt32(2),
                SkillIds = skillIds,
                IsAvailable = reader.GetInt32(4) == 1,
                IsRetired = reader.GetInt32(5) == 1,
                RecentShiftIntervalCount = reader.GetInt32(6),
                RecentHolidayShiftIntervalCount = reader.GetInt32(7),
                RecentPeriodShiftIntervals = periodIntervals
            };
        }
        /// <summary>
        /// 按姓名关键字搜索人员
        /// </summary>
        public async Task<List<Personal>> SearchByNameAsync(string keyword)
        {
            var list = new List<Personal>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT Id, Name, PositionId, SkillIds, IsAvailable, IsRetired,
               RecentShiftIntervalCount, RecentHolidayShiftIntervalCount, RecentPeriodShiftIntervals
        FROM Personals
        WHERE Name LIKE @keyword
        ORDER BY Id";
            cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPerson(reader));
            }

            return list;
        }
    }
}
