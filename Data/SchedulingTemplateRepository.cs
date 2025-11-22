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
        // 使用优化的连接字符串，启用 WAL 模式以支持并发访问
        _connectionString = DatabaseConfiguration.GetOptimizedConnectionString(dbPath);
    }

    /// <summary>
    /// 初始化数据库表
    /// </summary>
    public async Task InitAsync()
    {
        // 表创建由 DatabaseService 统一管理，这里只做必要的初始化
        await Task.CompletedTask;
    }

    public async Task<List<SchedulingTemplate>> GetAllAsync()
    {
        var list = new List<SchedulingTemplate>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Id, Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, 
       HolidayConfigId, UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds,
       DurationDays, StrategyConfig, UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
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
SELECT Id, Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, 
       HolidayConfigId, UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds,
       DurationDays, StrategyConfig, UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
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
(Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, HolidayConfigId, 
 UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds, DurationDays, 
 StrategyConfig, UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt)
VALUES 
(@name, @desc, @templateType, @isDefault, @personnelIds, @positionIds, @holidayConfigId,
 @useActiveHolidayConfig, @enabledFixedRuleIds, @enabledManualAssignmentIds, @durationDays, 
 @strategyConfig, @usageCount, @isActive, @createdAt, @updatedAt, @lastUsedAt);
SELECT last_insert_rowid();
";
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@templateType", template.TemplateType ?? "regular");
        cmd.Parameters.AddWithValue("@isDefault", template.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("@personnelIds", JsonSerializer.Serialize(template.PersonnelIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@positionIds", JsonSerializer.Serialize(template.PositionIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@holidayConfigId", template.HolidayConfigId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@useActiveHolidayConfig", template.UseActiveHolidayConfig ? 1 : 0);
        cmd.Parameters.AddWithValue("@enabledFixedRuleIds", JsonSerializer.Serialize(template.EnabledFixedRuleIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@enabledManualAssignmentIds", JsonSerializer.Serialize(template.EnabledManualAssignmentIds, _jsonOptions));
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
SET Name = @name, Description = @desc, TemplateType = @templateType, IsDefault = @isDefault,
    PersonnelIds = @personnelIds, PositionIds = @positionIds, HolidayConfigId = @holidayConfigId,
    UseActiveHolidayConfig = @useActiveHolidayConfig, EnabledFixedRuleIds = @enabledFixedRuleIds,
    EnabledManualAssignmentIds = @enabledManualAssignmentIds, DurationDays = @durationDays, 
    StrategyConfig = @strategyConfig, UsageCount = @usageCount, IsActive = @isActive, 
    UpdatedAt = @updatedAt, LastUsedAt = @lastUsedAt
WHERE Id = @id
";
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@templateType", template.TemplateType ?? "regular");
        cmd.Parameters.AddWithValue("@isDefault", template.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("@personnelIds", JsonSerializer.Serialize(template.PersonnelIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@positionIds", JsonSerializer.Serialize(template.PositionIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@holidayConfigId", template.HolidayConfigId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@useActiveHolidayConfig", template.UseActiveHolidayConfig ? 1 : 0);
        cmd.Parameters.AddWithValue("@enabledFixedRuleIds", JsonSerializer.Serialize(template.EnabledFixedRuleIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@enabledManualAssignmentIds", JsonSerializer.Serialize(template.EnabledManualAssignmentIds, _jsonOptions));
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
SELECT Id, Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, 
       HolidayConfigId, UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds,
       DurationDays, StrategyConfig, UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
FROM SchedulingTemplates
WHERE IsActive = 1 AND IsDefault = 1
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
SELECT Id, Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, 
       HolidayConfigId, UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds,
       DurationDays, StrategyConfig, UsageCount, IsActive, CreatedAt, UpdatedAt, LastUsedAt
FROM SchedulingTemplates
WHERE IsActive = 1 AND TemplateType = @type
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
        cmd.CommandText = "UPDATE SchedulingTemplates SET IsDefault = 0 WHERE TemplateType = @type";
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
            TemplateType = reader.IsDBNull(3) ? "regular" : reader.GetString(3),
            IsDefault = reader.GetInt32(4) == 1,
            PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(5), _jsonOptions) ?? new(),
            PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(6), _jsonOptions) ?? new(),
            HolidayConfigId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            UseActiveHolidayConfig = reader.GetInt32(8) == 1,
            EnabledFixedRuleIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(9), _jsonOptions) ?? new(),
            EnabledManualAssignmentIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(10), _jsonOptions) ?? new(),
            DurationDays = reader.GetInt32(11),
            StrategyConfig = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            UsageCount = reader.GetInt32(13),
            IsActive = reader.GetInt32(14) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(15)),
            UpdatedAt = DateTime.Parse(reader.GetString(16)),
            LastUsedAt = reader.IsDBNull(17) ? null : DateTime.Parse(reader.GetString(17))
        };
    }
}
