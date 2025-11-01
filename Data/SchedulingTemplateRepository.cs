using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;

namespace AutoScheduling3.Data;

/// <summary>
/// 排班模板仓储实现
/// </summary>
public class SchedulingTemplateRepository : ITemplateRepository
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

    public SchedulingTemplateRepository(string dbPath)
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
CREATE TABLE IF NOT EXISTS SchedulingTemplates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT DEFAULT '',
    PersonnelIds TEXT NOT NULL DEFAULT '[]',
    PositionIds TEXT NOT NULL DEFAULT '[]',
    DurationDays INTEGER NOT NULL DEFAULT 1,
    StrategyConfig TEXT NOT NULL DEFAULT '{}',
    UsageCount INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastUsedAt TEXT
);
CREATE INDEX IF NOT EXISTS idx_template_active ON SchedulingTemplates(IsActive);
CREATE INDEX IF NOT EXISTS idx_template_usage ON SchedulingTemplates(UsageCount);
";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SchedulingTemplate>> GetAllAsync()
    {
        var list = new List<SchedulingTemplate>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, Name, Description, PersonnelIds, PositionIds, DurationDays, StrategyConfig,
       UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
FROM SchedulingTemplates
WHERE IsActive = 1
ORDER BY UsageCount DESC, Name
";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadTemplate(reader));
        }

        return list;
    }

    public async Task<SchedulingTemplate?> GetByIdAsync(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, Name, Description, PersonnelIds, PositionIds, DurationDays, StrategyConfig,
       UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
FROM SchedulingTemplates
WHERE Id = @id
";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadTemplate(reader);
        }

        return null;
    }

    public async Task<int> CreateAsync(SchedulingTemplate template)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO SchedulingTemplates 
(Name, Description, PersonnelIds, PositionIds, DurationDays, StrategyConfig,
 UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt)
VALUES 
(@name, @desc, @personnelIds, @positionIds, @durationDays, @strategyConfig,
 @usageCount, @isActive, @createdAt, @updatedAt, @lastUsedAt);
SELECT last_insert_rowid();
";
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@personnelIds", JsonSerializer.Serialize(template.PersonnelIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@positionIds", JsonSerializer.Serialize(template.PositionIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@durationDays", template.DurationDays);
        cmd.Parameters.AddWithValue("@strategyConfig", template.StrategyConfig ?? string.Empty);
        cmd.Parameters.AddWithValue("@usageCount", template.UsageCount);
        cmd.Parameters.AddWithValue("@isActive", template.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@createdAt", template.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@updatedAt", template.UpdatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@lastUsedAt", template.LastUsedAt?.ToString("o") ?? (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(SchedulingTemplate template)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE SchedulingTemplates 
SET Name = @name, Description = @desc, PersonnelIds = @personnelIds, PositionIds = @positionIds,
    DurationDays = @durationDays, StrategyConfig = @strategyConfig, UsageCount = @usageCount,
    IsActive = @isActive, UpdatedAt = @updatedAt, LastUsedAt = @lastUsedAt
WHERE Id = @id
";
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@personnelIds", JsonSerializer.Serialize(template.PersonnelIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@positionIds", JsonSerializer.Serialize(template.PositionIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@durationDays", template.DurationDays);
        cmd.Parameters.AddWithValue("@strategyConfig", template.StrategyConfig ?? string.Empty);
        cmd.Parameters.AddWithValue("@usageCount", template.UsageCount);
        cmd.Parameters.AddWithValue("@isActive", template.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@lastUsedAt", template.LastUsedAt?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@id", template.Id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SchedulingTemplates WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM SchedulingTemplates WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    public async Task<SchedulingTemplate?> GetDefaultAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, Name, Description, PersonnelIds, PositionIds, DurationDays, StrategyConfig,
       UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
FROM SchedulingTemplates
WHERE IsActive = 1
ORDER BY UsageCount DESC
LIMIT 1
";

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadTemplate(reader);
        }

        return null;
    }

    public async Task<List<SchedulingTemplate>> GetByTypeAsync(string templateType)
    {
        var list = new List<SchedulingTemplate>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, Name, Description, PersonnelIds, PositionIds, DurationDays, StrategyConfig,
       UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
FROM SchedulingTemplates
WHERE IsActive = 1
ORDER BY UsageCount DESC, Name
";
        cmd.Parameters.AddWithValue("@type", templateType);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadTemplate(reader));
        }

        return list;
    }

    public async Task UpdateUsageAsync(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE SchedulingTemplates 
SET UsageCount = UsageCount + 1, 
    LastUsedAt = @lastUsedAt
WHERE Id = @id
";
        cmd.Parameters.AddWithValue("@lastUsedAt", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = excludeId.HasValue
            ? "SELECT COUNT(1) FROM SchedulingTemplates WHERE Name = @name AND Id <> @exclude"
            : "SELECT COUNT(1) FROM SchedulingTemplates WHERE Name = @name";
        cmd.Parameters.AddWithValue("@name", name);
        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@exclude", excludeId.Value);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) >0;
    }

    public async Task ClearDefaultForTypeAsync(string templateType)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE SchedulingTemplates SET IsActive = 0 WHERE IsActive = 1";
        cmd.Parameters.AddWithValue("@type", templateType);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 从 DataReader 读取模板对象
    /// </summary>
    private SchedulingTemplate ReadTemplate(SqliteDataReader reader)
    {
        return new SchedulingTemplate
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3), _jsonOptions) ?? new(),
            PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(4), _jsonOptions) ?? new(),
            DurationDays = reader.GetInt32(5),
            StrategyConfig = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            UsageCount = reader.GetInt32(7),
            IsActive = reader.GetInt32(8) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(9)),
            UpdatedAt = DateTime.Parse(reader.GetString(10)),
            LastUsedAt = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11))
        };
    }
}
