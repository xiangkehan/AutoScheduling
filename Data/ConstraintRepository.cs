using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Data.Interfaces;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 约束数据访问层：管理定岗规则、手动指定和休息日配置的 CRUD 操作
    /// </summary>
    public class ConstraintRepository : IConstraintRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

        public ConstraintRepository(string dbPath)
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

        #region FixedPositionRule CRUD

        public async Task<int> AddFixedPositionRuleAsync(FixedPositionRule rule)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO FixedPositionRules (PersonalId, AllowedPositionIds, AllowedPeriods, IsEnabled, Description)
VALUES (@personalId, @allowedPos, @allowedPeriods, @isEnabled, @desc);
SELECT last_insert_rowid();";
            
            cmd.Parameters.AddWithValue("@personalId", rule.PersonalId);
            cmd.Parameters.AddWithValue("@allowedPos", JsonSerializer.Serialize(rule.AllowedPositionIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@allowedPeriods", JsonSerializer.Serialize(rule.AllowedPeriods, _jsonOptions));
            cmd.Parameters.AddWithValue("@isEnabled", rule.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@desc", rule.Description ?? string.Empty);

            var result = await cmd.ExecuteScalarAsync();
            rule.Id = Convert.ToInt32(result);
            return rule.Id;
        }

        public async Task<List<FixedPositionRule>> GetAllFixedPositionRulesAsync(bool enabledOnly = true)
        {
            var list = new List<FixedPositionRule>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            var whereClause = enabledOnly ? "WHERE IsEnabled = 1" : "";
            cmd.CommandText = $"SELECT Id, PersonalId, AllowedPositionIds, AllowedPeriods, IsEnabled, Description FROM FixedPositionRules {whereClause} ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new FixedPositionRule
                {
                    Id = reader.GetInt32(0),
                    PersonalId = reader.GetInt32(1),
                    AllowedPositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                    AllowedPeriods = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>(),
                    IsEnabled = reader.GetInt32(4) == 1,
                    Description = reader.GetString(5)
                });
            }

            return list;
        }

        public async Task<List<FixedPositionRule>> GetFixedPositionRulesByPersonAsync(int personalId)
        {
            var list = new List<FixedPositionRule>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, PersonalId, AllowedPositionIds, AllowedPeriods, IsEnabled, Description FROM FixedPositionRules WHERE PersonalId = @pid AND IsEnabled = 1";
            cmd.Parameters.AddWithValue("@pid", personalId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new FixedPositionRule
                {
                    Id = reader.GetInt32(0),
                    PersonalId = reader.GetInt32(1),
                    AllowedPositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                    AllowedPeriods = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>(),
                    IsEnabled = reader.GetInt32(4) == 1,
                    Description = reader.GetString(5)
                });
            }

            return list;
        }

        public async Task UpdateFixedPositionRuleAsync(FixedPositionRule rule)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE FixedPositionRules SET 
    PersonalId = @personalId,
    AllowedPositionIds = @allowedPos,
    AllowedPeriods = @allowedPeriods,
    IsEnabled = @isEnabled,
    Description = @desc
WHERE Id = @id";

            cmd.Parameters.AddWithValue("@personalId", rule.PersonalId);
            cmd.Parameters.AddWithValue("@allowedPos", JsonSerializer.Serialize(rule.AllowedPositionIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@allowedPeriods", JsonSerializer.Serialize(rule.AllowedPeriods, _jsonOptions));
            cmd.Parameters.AddWithValue("@isEnabled", rule.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@desc", rule.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@id", rule.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteFixedPositionRuleAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM FixedPositionRules WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region ManualAssignment CRUD

        public async Task<int> AddManualAssignmentAsync(ManualAssignment assignment)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO ManualAssignments (PositionId, PeriodIndex, PersonalId, Date, IsEnabled, Remarks)
VALUES (@posId, @periodIdx, @personalId, @date, @isEnabled, @remarks);
SELECT last_insert_rowid();";
            
            cmd.Parameters.AddWithValue("@posId", assignment.PositionId);
            cmd.Parameters.AddWithValue("@periodIdx", assignment.PeriodIndex);
            cmd.Parameters.AddWithValue("@personalId", assignment.PersonalId);
            cmd.Parameters.AddWithValue("@date", assignment.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@isEnabled", assignment.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@remarks", assignment.Remarks ?? string.Empty);

            var result = await cmd.ExecuteScalarAsync();
            assignment.Id = Convert.ToInt32(result);
            return assignment.Id;
        }

        public async Task<List<ManualAssignment>> GetManualAssignmentsByDateRangeAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true)
        {
            var list = new List<ManualAssignment>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            var whereClause = enabledOnly ? "AND IsEnabled = 1" : "";
            cmd.CommandText = $"SELECT Id, PositionId, PeriodIndex, PersonalId, Date, IsEnabled, Remarks FROM ManualAssignments WHERE Date >= @start AND Date <= @end {whereClause} ORDER BY Date, PeriodIndex";
            cmd.Parameters.AddWithValue("@start", startDate.ToString("o"));
            cmd.Parameters.AddWithValue("@end", endDate.ToString("o"));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ManualAssignment
                {
                    Id = reader.GetInt32(0),
                    PositionId = reader.GetInt32(1),
                    PeriodIndex = reader.GetInt32(2),
                    PersonalId = reader.GetInt32(3),
                    Date = DateTime.Parse(reader.GetString(4)),
                    IsEnabled = reader.GetInt32(5) == 1,
                    Remarks = reader.GetString(6)
                });
            }

            return list;
        }

        public async Task UpdateManualAssignmentAsync(ManualAssignment assignment)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE ManualAssignments SET 
    PositionId = @posId,
    PeriodIndex = @periodIdx,
    PersonalId = @personalId,
    Date = @date,
    IsEnabled = @isEnabled,
    Remarks = @remarks
WHERE Id = @id";

            cmd.Parameters.AddWithValue("@posId", assignment.PositionId);
            cmd.Parameters.AddWithValue("@periodIdx", assignment.PeriodIndex);
            cmd.Parameters.AddWithValue("@personalId", assignment.PersonalId);
            cmd.Parameters.AddWithValue("@date", assignment.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@isEnabled", assignment.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@remarks", assignment.Remarks ?? string.Empty);
            cmd.Parameters.AddWithValue("@id", assignment.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteManualAssignmentAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ManualAssignments WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region HolidayConfig CRUD

        public async Task<int> AddHolidayConfigAsync(HolidayConfig config)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO HolidayConfigs (ConfigName, EnableWeekendRule, WeekendDays, LegalHolidays, CustomHolidays, ExcludedDates, IsActive)
VALUES (@name, @enableWeekend, @weekendDays, @legalHolidays, @customHolidays, @excludedDates, @isActive);
SELECT last_insert_rowid();";
            
            cmd.Parameters.AddWithValue("@name", config.ConfigName ?? string.Empty);
            cmd.Parameters.AddWithValue("@enableWeekend", config.EnableWeekendRule ? 1 : 0);
            cmd.Parameters.AddWithValue("@weekendDays", JsonSerializer.Serialize(config.WeekendDays.ConvertAll(d => (int)d), _jsonOptions));
            cmd.Parameters.AddWithValue("@legalHolidays", JsonSerializer.Serialize(config.LegalHolidays.ConvertAll(d => d.ToString("o")), _jsonOptions));
            cmd.Parameters.AddWithValue("@customHolidays", JsonSerializer.Serialize(config.CustomHolidays.ConvertAll(d => d.ToString("o")), _jsonOptions));
            cmd.Parameters.AddWithValue("@excludedDates", JsonSerializer.Serialize(config.ExcludedDates.ConvertAll(d => d.ToString("o")), _jsonOptions));
            cmd.Parameters.AddWithValue("@isActive", config.IsActive ? 1 : 0);

            var result = await cmd.ExecuteScalarAsync();
            config.Id = Convert.ToInt32(result);
            return config.Id;
        }

        public async Task<HolidayConfig?> GetActiveHolidayConfigAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ConfigName, EnableWeekendRule, WeekendDays, LegalHolidays, CustomHolidays, ExcludedDates, IsActive FROM HolidayConfigs WHERE IsActive = 1 LIMIT 1";

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapHolidayConfig(reader);
            }

            return null;
        }

        public async Task<List<HolidayConfig>> GetAllHolidayConfigsAsync()
        {
            var list = new List<HolidayConfig>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ConfigName, EnableWeekendRule, WeekendDays, LegalHolidays, CustomHolidays, ExcludedDates, IsActive FROM HolidayConfigs ORDER BY Id DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapHolidayConfig(reader));
            }

            return list;
        }

        public async Task UpdateHolidayConfigAsync(HolidayConfig config)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE HolidayConfigs SET 
    ConfigName = @name,
    EnableWeekendRule = @enableWeekend,
    WeekendDays = @weekendDays,
    LegalHolidays = @legalHolidays,
    CustomHolidays = @customHolidays,
    ExcludedDates = @excludedDates,
    IsActive = @isActive
WHERE Id = @id";

            cmd.Parameters.AddWithValue("@name", config.ConfigName ?? string.Empty);
            cmd.Parameters.AddWithValue("@enableWeekend", config.EnableWeekendRule ? 1 : 0);
            cmd.Parameters.AddWithValue("@weekendDays", JsonSerializer.Serialize(config.WeekendDays.ConvertAll(d => (int)d), _jsonOptions));
            cmd.Parameters.AddWithValue("@legalHolidays", JsonSerializer.Serialize(config.LegalHolidays.ConvertAll(d => d.ToString("o")), _jsonOptions));
            cmd.Parameters.AddWithValue("@customHolidays", JsonSerializer.Serialize(config.CustomHolidays.ConvertAll(d => d.ToString("o")), _jsonOptions));
            cmd.Parameters.AddWithValue("@excludedDates", JsonSerializer.Serialize(config.ExcludedDates.ConvertAll(d => d.ToString("o")), _jsonOptions));
            cmd.Parameters.AddWithValue("@isActive", config.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", config.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteHolidayConfigAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM HolidayConfigs WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private HolidayConfig MapHolidayConfig(SqliteDataReader reader)
        {
            var weekendDaysInts = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>();
            var legalHolidayStrs = JsonSerializer.Deserialize<List<string>>(reader.GetString(4)) ?? new List<string>();
            var customHolidayStrs = JsonSerializer.Deserialize<List<string>>(reader.GetString(5)) ?? new List<string>();
            var excludedDateStrs = JsonSerializer.Deserialize<List<string>>(reader.GetString(6)) ?? new List<string>();

            return new HolidayConfig
            {
                Id = reader.GetInt32(0),
                ConfigName = reader.GetString(1),
                EnableWeekendRule = reader.GetInt32(2) == 1,
                WeekendDays = weekendDaysInts.ConvertAll(i => (DayOfWeek)i),
                LegalHolidays = legalHolidayStrs.ConvertAll(s => DateTime.Parse(s)),
                CustomHolidays = customHolidayStrs.ConvertAll(s => DateTime.Parse(s)),
                ExcludedDates = excludedDateStrs.ConvertAll(s => DateTime.Parse(s)),
                IsActive = reader.GetInt32(7) == 1
            };
        }

        #endregion
    }
}
