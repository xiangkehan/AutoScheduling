using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 技能数据访问层：管理技能信息的 CRUD 操作
    /// </summary>
    public class SkillRepository : ISkillRepository
    {
        private readonly string _connectionString;

        public SkillRepository(string dbPath)
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
CREATE TABLE IF NOT EXISTS Skills (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 添加技能
        /// </summary>
        public async Task<int> CreateAsync(Skill skill)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Skills (Name, Description, IsActive, CreatedAt, UpdatedAt) VALUES (@name, @desc, @isActive, @createdAt, @updatedAt); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", skill.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@desc", skill.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@isActive", skill.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@createdAt", skill.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@updatedAt", skill.UpdatedAt.ToString("o"));

            var result = await cmd.ExecuteScalarAsync();
            skill.Id = Convert.ToInt32(result);
            return skill.Id;
        }

        /// <summary>
        /// 根据ID查询技能
        /// </summary>
        public async Task<Skill?> GetByIdAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt FROM Skills WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Skill
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    IsActive = reader.IsDBNull(3) ? true : reader.GetInt32(3) == 1,
                    CreatedAt = reader.IsDBNull(4) ? DateTime.Now : DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = reader.IsDBNull(5) ? DateTime.Now : DateTime.Parse(reader.GetString(5))
                };
            }

            return null;
        }

        /// <summary>
        /// 查询所有技能
        /// </summary>
        public async Task<List<Skill>> GetAllAsync()
        {
            var list = new List<Skill>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt FROM Skills ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Skill
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    IsActive = reader.IsDBNull(3) ? true : reader.GetInt32(3) == 1,
                    CreatedAt = reader.IsDBNull(4) ? DateTime.Now : DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = reader.IsDBNull(5) ? DateTime.Now : DateTime.Parse(reader.GetString(5))
                });
            }

            return list;
        }

        /// <summary>
        /// 批量查询技能
        /// </summary>
        public async Task<List<Skill>> GetByIdsAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<Skill>();

            var list = new List<Skill>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var idsStr = string.Join(",", ids);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt FROM Skills WHERE Id IN ({idsStr}) ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Skill
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    IsActive = reader.IsDBNull(3) ? true : reader.GetInt32(3) == 1,
                    CreatedAt = reader.IsDBNull(4) ? DateTime.Now : DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = reader.IsDBNull(5) ? DateTime.Now : DateTime.Parse(reader.GetString(5))
                });
            }

            return list;
        }

        /// <summary>
        /// 更新技能
        /// </summary>
        public async Task UpdateAsync(Skill skill)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Skills SET Name = @name, Description = @desc, IsActive = @isActive, UpdatedAt = @updatedAt WHERE Id = @id";
            cmd.Parameters.AddWithValue("@name", skill.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@desc", skill.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@isActive", skill.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", skill.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 删除技能
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Skills WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 检查技能是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Skills WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
    }
}
