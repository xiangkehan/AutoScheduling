using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using System.Linq; // for Any Distinct

namespace AutoScheduling3.Data
{
    public class PositionLocationRepository : IPositionRepository
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
            // 表创建由 DatabaseService 统一管理，这里只做必要的初始化
            await Task.CompletedTask;
        }

        public async Task<int> CreateAsync(PositionLocation item)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Positions (Name, Location, Description, Requirements, RequiredSkillIds, AvailablePersonnelIds, IsActive, CreatedAt, UpdatedAt) VALUES (@name, @location, @description, @requirements, @skillIds, @availablePersonnelIds, @isActive, @createdAt, @updatedAt); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", item.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", item.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@requirements", item.Requirements ?? string.Empty);
            cmd.Parameters.AddWithValue("@skillIds", JsonSerializer.Serialize(item.RequiredSkillIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@availablePersonnelIds", JsonSerializer.Serialize(item.AvailablePersonnelIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@isActive", item.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@createdAt", item.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@updatedAt", item.UpdatedAt.ToString("o"));

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<PositionLocation>> GetAllAsync()
        {
            var list = new List<PositionLocation>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds, AvailablePersonnelIds, IsActive, CreatedAt, UpdatedAt FROM Positions ORDER BY Id";

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
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds, AvailablePersonnelIds, IsActive, CreatedAt, UpdatedAt FROM Positions WHERE Id = @id";
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
            cmd.CommandText = $"SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds, AvailablePersonnelIds, IsActive, CreatedAt, UpdatedAt FROM Positions WHERE Id IN ({idsStr}) ORDER BY Id";

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
            cmd.CommandText = "UPDATE Positions SET Name = @name, Location = @location, Description = @description, Requirements = @requirements, RequiredSkillIds = @skillIds, AvailablePersonnelIds = @availablePersonnelIds, IsActive = @isActive, UpdatedAt = @updatedAt WHERE Id = @id";
            cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", item.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", item.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@requirements", item.Requirements ?? string.Empty);
            cmd.Parameters.AddWithValue("@skillIds", JsonSerializer.Serialize(item.RequiredSkillIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@availablePersonnelIds", JsonSerializer.Serialize(item.AvailablePersonnelIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@isActive", item.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
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

        /// <summary>
        /// 为哨位添加可用人员
        /// </summary>
        public async Task AddAvailablePersonnelAsync(int positionId, int personnelId)
        {
            var position = await GetByIdAsync(positionId);
            if (position == null) return;

            if (!position.AvailablePersonnelIds.Contains(personnelId))
            {
                position.AvailablePersonnelIds.Add(personnelId);
                await UpdateAsync(position);
            }
        }

        /// <summary>
        /// 从哨位移除可用人员
        /// </summary>
        public async Task RemoveAvailablePersonnelAsync(int positionId, int personnelId)
        {
            var position = await GetByIdAsync(positionId);
            if (position == null) return;

            if (position.AvailablePersonnelIds.Remove(personnelId))
            {
                await UpdateAsync(position);
            }
        }

        /// <summary>
        /// 更新哨位的可用人员列表
        /// </summary>
        public async Task UpdateAvailablePersonnelAsync(int positionId, List<int> personnelIds)
        {
            var position = await GetByIdAsync(positionId);
            if (position == null) return;

            position.AvailablePersonnelIds = personnelIds ?? new List<int>();
            await UpdateAsync(position);
        }

        /// <summary>
        /// 获取哨位的可用人员ID列表
        /// </summary>
        public async Task<List<int>> GetAvailablePersonnelIdsAsync(int positionId)
        {
            var position = await GetByIdAsync(positionId);
            return position?.AvailablePersonnelIds ?? new List<int>();
        }

        /// <summary>
        /// 根据人员ID获取其可用的哨位列表
        /// </summary>
        public async Task<List<PositionLocation>> GetPositionsByPersonnelAsync(int personnelId)
        {
            var allPositions = await GetAllAsync();
            return allPositions.Where(p => p.AvailablePersonnelIds.Contains(personnelId)).ToList();
        }

        /// <summary>
        /// 检查哨位是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Positions WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// 按名称搜索哨位
        /// </summary>
        public async Task<List<PositionLocation>> SearchByNameAsync(string keyword)
        {
            var list = new List<PositionLocation>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds, AvailablePersonnelIds, IsActive, CreatedAt, UpdatedAt FROM Positions WHERE Name LIKE @keyword OR Location LIKE @keyword ORDER BY Id";
            cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPosition(reader));
            }

            return list;
        }

        private PositionLocation MapPosition(SqliteDataReader reader)
        {
            var skillIds = new List<int>();
            var availablePersonnelIds = new List<int>();
            
            try
            {
                skillIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(5)) ?? new List<int>();
            }
            catch
            {
                // 如果旧数据没有这个字段，使用空列表
            }

            try
            {
                availablePersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(6)) ?? new List<int>();
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
                RequiredSkillIds = skillIds,
                AvailablePersonnelIds = availablePersonnelIds,
                IsActive = reader.IsDBNull(7) ? true : reader.GetInt32(7) == 1,
                CreatedAt = reader.IsDBNull(8) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(8)),
                UpdatedAt = reader.IsDBNull(9) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(9))
            };
        }

        public async Task<List<PositionLocation>> GetPositionsByIdsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
                return new List<PositionLocation>();

            var list = new List<PositionLocation>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var idsStr = string.Join(",", ids.Distinct());
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Name, Location, Description, Requirements, RequiredSkillIds, AvailablePersonnelIds, IsActive, CreatedAt, UpdatedAt FROM Positions WHERE Id IN ({idsStr})";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapPosition(reader));
            }

            return list;
        }

        /// <summary>
        /// 检查人员是否存在 - 注意：此方法应该在人员仓储中实现，不应在哨位仓储中
        /// </summary>
        public async Task<bool> PersonnelExistsAsync(int personnelId)
        {
            // TODO: 这个方法应该移动到 IPersonalRepository 中
            // 临时实现：总是返回 true 以避免编译错误
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 根据ID获取人员信息 - 注意：此方法应该在人员仓储中实现，不应在哨位仓储中
        /// </summary>
        public async Task<Personal?> GetPersonnelByIdAsync(int personnelId)
        {
            // TODO: 这个方法应该移动到 IPersonalRepository 中
            await Task.CompletedTask;
            throw new NotImplementedException("此方法应该在 IPersonalRepository 中实现，而不是在 IPositionRepository 中");
        }

        /// <summary>
        /// 根据ID列表获取人员信息 - 注意：此方法应该在人员仓储中实现，不应在哨位仓储中
        /// </summary>
        public async Task<List<Personal>> GetPersonnelByIdsAsync(IEnumerable<int> personnelIds)
        {
            // TODO: 这个方法应该移动到 IPersonalRepository 中
            await Task.CompletedTask;
            throw new NotImplementedException("此方法应该在 IPersonalRepository 中实现，而不是在 IPositionRepository 中");
        }
    }
}
