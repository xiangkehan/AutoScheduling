using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using System.Linq; // LINQ for Any Distinct

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 排班数据访问层：管理排班历史和缓冲区
    /// 需求: 1.2, 2.1, 2.2, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    public class SchedulingRepository : ISchedulingRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

        public SchedulingRepository(string dbPath)
        {
            _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        }

        public async Task InitAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Schedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Header TEXT NOT NULL,
    PersonnelIds TEXT NOT NULL DEFAULT '[]', -- JSON array of ints
    PositionIds TEXT NOT NULL DEFAULT '[]',  -- JSON array of ints
    StartDate TEXT NOT NULL, -- ISO 8601 date
    EndDate TEXT NOT NULL,   -- ISO 8601 date
    IsConfirmed INTEGER NOT NULL DEFAULT 0, -- 0=buffer, 1=confirmed
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS SingleShifts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleId INTEGER NOT NULL,
    PositionId INTEGER NOT NULL,
    PersonnelId INTEGER NOT NULL,
    StartTime TEXT NOT NULL, -- ISO 8601
    EndTime TEXT NOT NULL,   -- ISO 8601
    DayIndex INTEGER NOT NULL DEFAULT 0,
    TimeSlotIndex INTEGER NOT NULL DEFAULT 0,
    IsNightShift INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ScheduleId) REFERENCES Schedules(Id) ON DELETE CASCADE
);
";
            await cmd.ExecuteNonQueryAsync();
        }

        #region Schedule CRUD
        public async Task<int> CreateAsync(Schedule schedule)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Schedules (Header, PersonnelIds, PositionIds, StartDate, EndDate, IsConfirmed, CreatedAt, UpdatedAt) VALUES (@header, @pIds, @posIds, @startDate, @endDate, @isConfirmed, @createdAt, @updatedAt); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@header", schedule.Header ?? string.Empty);
            cmd.Parameters.AddWithValue("@pIds", JsonSerializer.Serialize(schedule.PersonnelIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@posIds", JsonSerializer.Serialize(schedule.PositionIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@startDate", schedule.StartDate.ToString("o"));
            cmd.Parameters.AddWithValue("@endDate", schedule.EndDate.ToString("o"));
            cmd.Parameters.AddWithValue("@isConfirmed", schedule.IsConfirmed ? 1 : 0);
            cmd.Parameters.AddWithValue("@createdAt", schedule.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@updatedAt", schedule.UpdatedAt.ToString("o"));
            var newIdObj = await cmd.ExecuteScalarAsync();
            int newId = Convert.ToInt32(newIdObj);
            schedule.Id = newId;

            foreach (var shift in schedule.Results)
            {
                shift.ScheduleId = newId;
                await InsertShiftInternalAsync(conn, tx, shift);
            }

            await tx.CommitAsync();
            return newId;
        }

        public async Task<Schedule?> GetByIdAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Header, PersonnelIds, PositionIds, StartDate, EndDate, IsConfirmed, CreatedAt, UpdatedAt FROM Schedules WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var schedule = new Schedule
            {
                Id = reader.GetInt32(0),
                Header = reader.GetString(1),
                PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>(),
                StartDate = DateTime.Parse(reader.GetString(4)),
                EndDate = DateTime.Parse(reader.GetString(5)),
                IsConfirmed = reader.GetInt32(6) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(7)),
                UpdatedAt = DateTime.Parse(reader.GetString(8))
            };

            schedule.Results = await GetShiftsByScheduleAsync(conn, id);
            return schedule;
        }

        public async Task<List<Schedule>> GetAllAsync()
        {
            var list = new List<Schedule>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Header, PersonnelIds, PositionIds, StartDate, EndDate, IsConfirmed, CreatedAt, UpdatedAt FROM Schedules ORDER BY Id";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schedule = new Schedule
                {
                    Id = reader.GetInt32(0),
                    Header = reader.GetString(1),
                    PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                    PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>(),
                    StartDate = DateTime.Parse(reader.GetString(4)),
                    EndDate = DateTime.Parse(reader.GetString(5)),
                    IsConfirmed = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = DateTime.Parse(reader.GetString(8))
                };
                schedule.Results = await GetShiftsByScheduleAsync(conn, schedule.Id);
                list.Add(schedule);
            }
            return list;
        }

        public async Task UpdateAsync(Schedule schedule)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Schedules SET Header=@header, PersonnelIds=@pIds, PositionIds=@posIds, StartDate=@startDate, EndDate=@endDate, IsConfirmed=@isConfirmed, UpdatedAt=@updatedAt WHERE Id=@id";
            cmd.Parameters.AddWithValue("@header", schedule.Header ?? string.Empty);
            cmd.Parameters.AddWithValue("@pIds", JsonSerializer.Serialize(schedule.PersonnelIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@posIds", JsonSerializer.Serialize(schedule.PositionIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@startDate", schedule.StartDate.ToString("o"));
            cmd.Parameters.AddWithValue("@endDate", schedule.EndDate.ToString("o"));
            cmd.Parameters.AddWithValue("@isConfirmed", schedule.IsConfirmed ? 1 : 0);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@id", schedule.Id);
            await cmd.ExecuteNonQueryAsync();

            // Simplest approach: delete existing shifts then re-insert current collection
            var delCmd = conn.CreateCommand();
            delCmd.Transaction = tx;
            delCmd.CommandText = "DELETE FROM SingleShifts WHERE ScheduleId=@sid";
            delCmd.Parameters.AddWithValue("@sid", schedule.Id);
            await delCmd.ExecuteNonQueryAsync();

            foreach (var shift in schedule.Results)
            {
                shift.ScheduleId = schedule.Id;
                await InsertShiftInternalAsync(conn, tx, shift);
            }

            await tx.CommitAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Schedules WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        #region SingleShift CRUD (standalone)
        public async Task<int> AddShiftAsync(SingleShift shift)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO SingleShifts (ScheduleId, PositionId, PersonnelId, StartTime, EndTime, DayIndex, TimeSlotIndex, IsNightShift) VALUES (@sid,@pos,@pid,@start,@end,@dayIndex,@timeSlot,@isNight); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@sid", shift.ScheduleId);
            cmd.Parameters.AddWithValue("@pos", shift.PositionId);
            cmd.Parameters.AddWithValue("@pid", shift.PersonnelId);
            cmd.Parameters.AddWithValue("@start", shift.StartTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@end", shift.EndTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@dayIndex", shift.DayIndex);
            cmd.Parameters.AddWithValue("@timeSlot", shift.TimeSlotIndex);
            cmd.Parameters.AddWithValue("@isNight", shift.IsNightShift ? 1 : 0);
            var obj = await cmd.ExecuteScalarAsync();
            shift.Id = Convert.ToInt32(obj);
            return shift.Id;
        }

        public async Task<SingleShift?> GetShiftAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ScheduleId, PositionId, PersonnelId, StartTime, EndTime, DayIndex, TimeSlotIndex, IsNightShift FROM SingleShifts WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapShift(reader);
        }

        public async Task<List<SingleShift>> GetShiftsByScheduleAsync(int scheduleId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            return await GetShiftsByScheduleAsync(conn, scheduleId);
        }

        public async Task UpdateShiftAsync(SingleShift shift)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE SingleShifts SET ScheduleId=@sid, PositionId=@pos, PersonnelId=@pid, StartTime=@start, EndTime=@end, DayIndex=@dayIndex, TimeSlotIndex=@timeSlot, IsNightShift=@isNight WHERE Id=@id";
            cmd.Parameters.AddWithValue("@sid", shift.ScheduleId);
            cmd.Parameters.AddWithValue("@pos", shift.PositionId);
            cmd.Parameters.AddWithValue("@pid", shift.PersonnelId);
            cmd.Parameters.AddWithValue("@start", shift.StartTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@end", shift.EndTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@dayIndex", shift.DayIndex);
            cmd.Parameters.AddWithValue("@timeSlot", shift.TimeSlotIndex);
            cmd.Parameters.AddWithValue("@isNight", shift.IsNightShift ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", shift.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteShiftAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SingleShifts WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        #region Internal helpers
        private async Task InsertShiftInternalAsync(SqliteConnection conn, SqliteTransaction tx, SingleShift shift)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO SingleShifts (ScheduleId, PositionId, PersonnelId, StartTime, EndTime, DayIndex, TimeSlotIndex, IsNightShift) VALUES (@sid,@pos,@pid,@start,@end,@dayIndex,@timeSlot,@isNight);";
            cmd.Parameters.AddWithValue("@sid", shift.ScheduleId);
            cmd.Parameters.AddWithValue("@pos", shift.PositionId);
            cmd.Parameters.AddWithValue("@pid", shift.PersonnelId);
            cmd.Parameters.AddWithValue("@start", shift.StartTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@end", shift.EndTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@dayIndex", shift.DayIndex);
            cmd.Parameters.AddWithValue("@timeSlot", shift.TimeSlotIndex);
            cmd.Parameters.AddWithValue("@isNight", shift.IsNightShift ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
            // fetch id
            var idCmd = conn.CreateCommand();
            idCmd.Transaction = tx;
            idCmd.CommandText = "SELECT last_insert_rowid();";
            var obj = await idCmd.ExecuteScalarAsync();
            shift.Id = Convert.ToInt32(obj);
        }

        private async Task<List<SingleShift>> GetShiftsByScheduleAsync(SqliteConnection conn, int scheduleId)
        {
            var list = new List<SingleShift>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ScheduleId, PositionId, PersonnelId, StartTime, EndTime, DayIndex, TimeSlotIndex, IsNightShift FROM SingleShifts WHERE ScheduleId=@sid ORDER BY StartTime";
            cmd.Parameters.AddWithValue("@sid", scheduleId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapShift(reader));
            }
            return list;
        }

        private SingleShift MapShift(SqliteDataReader reader)
        {
            return new SingleShift
            {
                Id = reader.GetInt32(0),
                ScheduleId = reader.GetInt32(1),
                PositionId = reader.GetInt32(2),
                PersonnelId = reader.GetInt32(3),
                StartTime = DateTime.Parse(reader.GetString(4)).ToUniversalTime(),
                EndTime = DateTime.Parse(reader.GetString(5)).ToUniversalTime(),
                DayIndex = reader.GetInt32(6),
                TimeSlotIndex = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                IsNightShift = reader.IsDBNull(8) ? false : reader.GetInt32(8) == 1
            };
        }
        #endregion

        #region ISchedulingRepository Implementation
        public async Task<bool> ExistsAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Schedules WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task<List<Schedule>> GetBufferSchedulesAsync()
        {
            var list = new List<Schedule>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Header, PersonnelIds, PositionIds, StartDate, EndDate, IsConfirmed, CreatedAt, UpdatedAt FROM Schedules WHERE IsConfirmed = 0 ORDER BY CreatedAt DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schedule = new Schedule
                {
                    Id = reader.GetInt32(0),
                    Header = reader.GetString(1),
                    PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                    PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>(),
                    StartDate = DateTime.Parse(reader.GetString(4)),
                    EndDate = DateTime.Parse(reader.GetString(5)),
                    IsConfirmed = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = DateTime.Parse(reader.GetString(8))
                };
                schedule.Results = await GetShiftsByScheduleAsync(conn, schedule.Id);
                list.Add(schedule);
            }
            return list;
        }

        public async Task<List<Schedule>> GetConfirmedSchedulesAsync()
        {
            var list = new List<Schedule>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Header, PersonnelIds, PositionIds, StartDate, EndDate, IsConfirmed, CreatedAt, UpdatedAt FROM Schedules WHERE IsConfirmed = 1 ORDER BY CreatedAt DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schedule = new Schedule
                {
                    Id = reader.GetInt32(0),
                    Header = reader.GetString(1),
                    PersonnelIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                    PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>(),
                    StartDate = DateTime.Parse(reader.GetString(4)),
                    EndDate = DateTime.Parse(reader.GetString(5)),
                    IsConfirmed = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = DateTime.Parse(reader.GetString(8))
                };
                schedule.Results = await GetShiftsByScheduleAsync(conn, schedule.Id);
                list.Add(schedule);
            }
            return list;
        }

        public async Task<List<SingleShift>> GetBufferShiftsAsync()
        {
            var list = new List<SingleShift>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT s.Id, s.ScheduleId, s.PositionId, s.PersonnelId, s.StartTime, s.EndTime, s.DayIndex, s.TimeSlotIndex, s.IsNightShift
FROM SingleShifts s
INNER JOIN Schedules sc ON s.ScheduleId = sc.Id
WHERE sc.IsConfirmed = 0
ORDER BY s.StartTime";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapShift(reader));
            }
            return list;
        }

        public async Task<List<SingleShift>> GetConfirmedShiftsAsync()
        {
            var list = new List<SingleShift>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT s.Id, s.ScheduleId, s.PositionId, s.PersonnelId, s.StartTime, s.EndTime, s.DayIndex, s.TimeSlotIndex, s.IsNightShift
FROM SingleShifts s
INNER JOIN Schedules sc ON s.ScheduleId = sc.Id
WHERE sc.IsConfirmed = 1
ORDER BY s.StartTime";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapShift(reader));
            }
            return list;
        }

        public async Task MoveBufferToHistoryAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Schedules SET IsConfirmed = 1, UpdatedAt = @updatedAt WHERE IsConfirmed = 0";
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ClearBufferAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Schedules WHERE IsConfirmed = 0";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ConfirmScheduleAsync(int scheduleId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Schedules SET IsConfirmed = 1, UpdatedAt = @updatedAt WHERE Id = @id";
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@id", scheduleId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<SingleShift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate, bool confirmedOnly = true)
        {
            var list = new List<SingleShift>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            var whereClause = confirmedOnly ? "WHERE sc.IsConfirmed = 1 AND" : "WHERE";
            cmd.CommandText = $@"
SELECT s.Id, s.ScheduleId, s.PositionId, s.PersonnelId, s.StartTime, s.EndTime, s.DayIndex, s.TimeSlotIndex, s.IsNightShift
FROM SingleShifts s
INNER JOIN Schedules sc ON s.ScheduleId = sc.Id
{whereClause} s.StartTime >= @startDate AND s.StartTime <= @endDate
ORDER BY s.StartTime";

            cmd.Parameters.AddWithValue("@startDate", startDate.ToString("o"));
            cmd.Parameters.AddWithValue("@endDate", endDate.ToString("o"));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapShift(reader));
            }
            return list;
        }

        public async Task<List<SingleShift>> GetRecentShiftsByPersonnelAsync(int personnelId, int days = 30)
        {
            var list = new List<SingleShift>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT s.Id, s.ScheduleId, s.PositionId, s.PersonnelId, s.StartTime, s.EndTime, s.DayIndex, s.TimeSlotIndex, s.IsNightShift
FROM SingleShifts s
INNER JOIN Schedules sc ON s.ScheduleId = sc.Id
WHERE s.PersonnelId = @personnelId AND s.StartTime >= @cutoffDate AND sc.IsConfirmed = 1
ORDER BY s.StartTime DESC";

            cmd.Parameters.AddWithValue("@personnelId", personnelId);
            cmd.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("o"));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapShift(reader));
            }
            return list;
        }

        public async Task SaveToBufferAsync(Schedule schedule)
        {
            schedule.IsConfirmed = false;
            await CreateAsync(schedule);
        }
        #endregion
    }
}
