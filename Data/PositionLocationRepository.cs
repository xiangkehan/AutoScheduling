using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;

namespace AutoScheduling3.Data
{
    public class PositionLocationRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

        public PositionLocationRepository(string dbPath)
        {
            // dbPath can be a file path like "positions.db" or ":memory:" for testing
            _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        }

        public async Task InitAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Positions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Location TEXT NOT NULL,
    Description TEXT NOT NULL,
    Requirements TEXT NOT NULL,
    RequiredSkillIds TEXT NOT NULL DEFAULT '[]' -- JSON array of ints
);";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> AddAsync(PositionLocation item)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Positions (Name, Location, Description, Requirements, RequiredSkillIds) VALUES (@name, @location, @description, @requirements, @skillIds); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", item.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", item.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@requirements", item.Requirements ?? string.Empty);
            cmd.Parameters.AddWithValue("@skillIds", JsonSerializer.Serialize(item.RequiredSkillIds, _jsonOptions));

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<PositionLocation>> GetAllAsync()
        {
            var list = new List<PositionLocation>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds FROM Positions ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPosition(reader));
            }

            return list;
        }

        public async Task<PositionLocation?> GetByIdAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds FROM Positions WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapPosition(reader);
            }

            return null;
        }

        /// <summary>
        /// 批量查询哨位（算法核心功能）
        /// </summary>
        public async Task<List<PositionLocation>> GetByIdsAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<PositionLocation>();

            var list = new List<PositionLocation>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // 构建 IN 子句
            var idsStr = string.Join(",", ids);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds FROM Positions WHERE Id IN ({idsStr}) ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPosition(reader));
            }

            return list;
        }

        public async Task UpdateAsync(PositionLocation item)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Positions SET Name = @name, Location = @location, Description = @description, Requirements = @requirements, RequiredSkillIds = @skillIds WHERE Id = @id";
            cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", item.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", item.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@requirements", item.Requirements ?? string.Empty);
            cmd.Parameters.AddWithValue("@skillIds", JsonSerializer.Serialize(item.RequiredSkillIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@id", item.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Positions WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        private PositionLocation MapPosition(SqliteDataReader reader)
        {
            var skillIds = new List<int>();
            try
            {
                skillIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(5)) ?? new List<int>();
            }
            catch
            {
                // 如果旧数据没有这个字段，使用空列表
            }

            return new PositionLocation
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Location = reader.GetString(2),
                Description = reader.GetString(3),
                Requirements = reader.GetString(4),
                RequiredSkillIds = skillIds
            };
        }
    }
}
