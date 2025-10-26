using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 技能数据访问层：管理技能信息的 CRUD 操作
    /// </summary>
    public class SkillRepository
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
    Description TEXT NOT NULL
);";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 添加技能
        /// </summary>
        public async Task<int> AddAsync(Skill skill)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Skills (Name, Description) VALUES (@name, @desc); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", skill.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@desc", skill.Description ?? string.Empty);

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
            cmd.CommandText = "SELECT Id, Name, Description FROM Skills WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Skill
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2)
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
            cmd.CommandText = "SELECT Id, Name, Description FROM Skills ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Skill
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2)
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
            cmd.CommandText = $"SELECT Id, Name, Description FROM Skills WHERE Id IN ({idsStr}) ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Skill
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2)
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
            cmd.CommandText = "UPDATE Skills SET Name = @name, Description = @desc WHERE Id = @id";
            cmd.Parameters.AddWithValue("@name", skill.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@desc", skill.Description ?? string.Empty);
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
    }
}
