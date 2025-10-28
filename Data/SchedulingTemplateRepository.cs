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
    Description TEXT,
    TemplateType TEXT NOT NULL DEFAULT 'regular',
    IsDefault INTEGER NOT NULL DEFAULT 0,
    PersonnelIds TEXT NOT NULL,
    PositionIds TEXT NOT NULL,
    HolidayConfigId INTEGER,
    UseActiveHolidayConfig INTEGER NOT NULL DEFAULT 0,
    EnabledFixedRuleIds TEXT NOT NULL,
    EnabledManualAssignmentIds TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    LastUsedAt TEXT,
    UsageCount INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_template_type ON SchedulingTemplates(TemplateType);
CREATE INDEX IF NOT EXISTS idx_template_default ON SchedulingTemplates(IsDefault);
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
SELECT Id, Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, 
       HolidayConfigId, UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds,
       CreatedAt, UpdatedAt, LastUsedAt, UsageCount
FROM SchedulingTemplates
ORDER BY IsDefault DESC, UsageCount DESC, Name
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
       CreatedAt, UpdatedAt, LastUsedAt, UsageCount
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
(Name, Description, TemplateType, IsDefault, PersonnelIds, PositionIds, 
 HolidayConfigId, UseActiveHolidayConfig, EnabledFixedRuleIds, EnabledManualAssignmentIds,
 CreatedAt, UpdatedAt, LastUsedAt, UsageCount)
VALUES 
(@name, @desc, @type, @isDefault, @personnelIds, @positionIds, 
 @holidayConfigId, @useActiveHoliday, @fixedRuleIds, @manualAssignmentIds,
 @createdAt, @updatedAt, @lastUsedAt, @usageCount);
SELECT last_insert_rowid();
";
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@type", template.TemplateType);
        cmd.Parameters.AddWithValue("@isDefault", template.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("@personnelIds", JsonSerializer.Serialize(template.PersonnelIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@positionIds", JsonSerializer.Serialize(template.PositionIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@holidayConfigId", template.HolidayConfigId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@useActiveHoliday", template.UseActiveHolidayConfig ? 1 : 0);
        cmd.Parameters.AddWithValue("@fixedRuleIds", JsonSerializer.Serialize(template.EnabledFixedRuleIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@manualAssignmentIds", JsonSerializer.Serialize(template.EnabledManualAssignmentIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@createdAt", template.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@updatedAt", template.UpdatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@lastUsedAt", template.LastUsedAt?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@usageCount", template.UsageCount);

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
SET Name = @name, Description = @desc, TemplateType = @type, IsDefault = @isDefault,
    PersonnelIds = @personnelIds, PositionIds = @positionIds,
    HolidayConfigId = @holidayConfigId, UseActiveHolidayConfig = @useActiveHoliday,
    EnabledFixedRuleIds = @fixedRuleIds, EnabledManualAssignmentIds = @manualAssignmentIds,
    UpdatedAt = @updatedAt, LastUsedAt = @lastUsedAt, UsageCount = @usageCount
WHERE Id = @id
";
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@type", template.TemplateType);
        cmd.Parameters.AddWithValue("@isDefault", template.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("@personnelIds", JsonSerializer.Serialize(template.PersonnelIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@positionIds", JsonSerializer.Serialize(template.PositionIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@holidayConfigId", template.HolidayConfigId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@useActiveHoliday", template.UseActiveHolidayConfig ? 1 : 0);
        cmd.Parameters.AddWithValue("@fixedRuleIds", JsonSerializer.Serialize(template.EnabledFixedRuleIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@manualAssignmentIds", JsonSerializer.Serialize(template.EnabledManualAssignmentIds, _jsonOptions));
        cmd.Parameters.AddWithValue("@updatedAt", template.UpdatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@lastUsedAt", template.LastUsedAt?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@usageCount", template.UsageCount);
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
       CreatedAt, UpdatedAt, LastUsedAt, UsageCount
FROM SchedulingTemplates
WHERE IsDefault = 1
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
       CreatedAt, UpdatedAt, LastUsedAt, UsageCount
FROM SchedulingTemplates
WHERE TemplateType = @type
ORDER BY IsDefault DESC, UsageCount DESC, Name
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
        cmd.Parameters.AddWithValue("@lastUsedAt", DateTime.Now.ToString("o"));
        cmd.Parameters.AddWithValue("@id", id);

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
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            TemplateType = reader.GetString(3),
            IsDefault = reader.GetInt32(4) == 1,
            PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(5), _jsonOptions) ?? new(),
            PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(6), _jsonOptions) ?? new(),
            HolidayConfigId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            UseActiveHolidayConfig = reader.GetInt32(8) == 1,
            EnabledFixedRuleIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(9), _jsonOptions) ?? new(),
            EnabledManualAssignmentIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(10), _jsonOptions) ?? new(),
            CreatedAt = DateTime.Parse(reader.GetString(11)),
            UpdatedAt = DateTime.Parse(reader.GetString(12)),
            LastUsedAt = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13)),
            UsageCount = reader.GetInt32(14)
        };
    }
}
