using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 排班与班次数据访问仓储
    /// </summary>
    public class SchedulingRepository
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
    Title TEXT NOT NULL,
    PersonalIds TEXT NOT NULL, -- JSON array of ints
    PositionIds TEXT NOT NULL  -- JSON array of ints
);
CREATE TABLE IF NOT EXISTS SingleShifts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleId INTEGER NOT NULL,
    PositionId INTEGER NOT NULL,
    PersonalId INTEGER NOT NULL,
    StartTime TEXT NOT NULL, -- ISO 8601
    EndTime TEXT NOT NULL,   -- ISO 8601
    FOREIGN KEY (ScheduleId) REFERENCES Schedules(Id) ON DELETE CASCADE
);
";
            await cmd.ExecuteNonQueryAsync();
        }

        #region Schedule CRUD
        public async Task<int> AddScheduleAsync(Schedule schedule)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Schedules (Title, PersonalIds, PositionIds) VALUES (@title, @pIds, @posIds); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@title", schedule.Title ?? string.Empty);
            cmd.Parameters.AddWithValue("@pIds", JsonSerializer.Serialize(schedule.PersonalIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@posIds", JsonSerializer.Serialize(schedule.PositionIds, _jsonOptions));
            var newIdObj = await cmd.ExecuteScalarAsync();
            int newId = Convert.ToInt32(newIdObj);
            schedule.Id = newId;

            foreach (var shift in schedule.Shifts)
            {
                shift.ScheduleId = newId;
                await InsertShiftInternalAsync(conn, tx, shift);
            }

            await tx.CommitAsync();
            return newId;
        }

        public async Task<Schedule?> GetScheduleAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, PersonalIds, PositionIds FROM Schedules WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var schedule = new Schedule
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                PersonalIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>()
            };

            schedule.Shifts = await GetShiftsByScheduleAsync(conn, id);
            return schedule;
        }

        public async Task<List<Schedule>> GetAllSchedulesAsync()
        {
            var list = new List<Schedule>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, PersonalIds, PositionIds FROM Schedules ORDER BY Id";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schedule = new Schedule
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    PersonalIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(2)) ?? new List<int>(),
                    PositionIds = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new List<int>()
                };
                schedule.Shifts = await GetShiftsByScheduleAsync(conn, schedule.Id);
                list.Add(schedule);
            }
            return list;
        }

        public async Task UpdateScheduleAsync(Schedule schedule)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Schedules SET Title=@title, PersonalIds=@pIds, PositionIds=@posIds WHERE Id=@id";
            cmd.Parameters.AddWithValue("@title", schedule.Title ?? string.Empty);
            cmd.Parameters.AddWithValue("@pIds", JsonSerializer.Serialize(schedule.PersonalIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@posIds", JsonSerializer.Serialize(schedule.PositionIds, _jsonOptions));
            cmd.Parameters.AddWithValue("@id", schedule.Id);
            await cmd.ExecuteNonQueryAsync();

            // Simplest approach: delete existing shifts then re-insert current collection
            var delCmd = conn.CreateCommand();
            delCmd.Transaction = tx;
            delCmd.CommandText = "DELETE FROM SingleShifts WHERE ScheduleId=@sid";
            delCmd.Parameters.AddWithValue("@sid", schedule.Id);
            await delCmd.ExecuteNonQueryAsync();

            foreach (var shift in schedule.Shifts)
            {
                shift.ScheduleId = schedule.Id;
                await InsertShiftInternalAsync(conn, tx, shift);
            }

            await tx.CommitAsync();
        }

        public async Task DeleteScheduleAsync(int id)
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
            cmd.CommandText = "INSERT INTO SingleShifts (ScheduleId, PositionId, PersonalId, StartTime, EndTime) VALUES (@sid,@pos,@pid,@start,@end); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@sid", shift.ScheduleId);
            cmd.Parameters.AddWithValue("@pos", shift.PositionId);
            cmd.Parameters.AddWithValue("@pid", shift.PersonalId);
            cmd.Parameters.AddWithValue("@start", shift.StartTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@end", shift.EndTime.ToUniversalTime().ToString("o"));
            var obj = await cmd.ExecuteScalarAsync();
            shift.Id = Convert.ToInt32(obj);
            return shift.Id;
        }

        public async Task<SingleShift?> GetShiftAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ScheduleId, PositionId, PersonalId, StartTime, EndTime FROM SingleShifts WHERE Id=@id";
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
            cmd.CommandText = "UPDATE SingleShifts SET ScheduleId=@sid, PositionId=@pos, PersonalId=@pid, StartTime=@start, EndTime=@end WHERE Id=@id";
            cmd.Parameters.AddWithValue("@sid", shift.ScheduleId);
            cmd.Parameters.AddWithValue("@pos", shift.PositionId);
            cmd.Parameters.AddWithValue("@pid", shift.PersonalId);
            cmd.Parameters.AddWithValue("@start", shift.StartTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@end", shift.EndTime.ToUniversalTime().ToString("o"));
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
            cmd.CommandText = "INSERT INTO SingleShifts (ScheduleId, PositionId, PersonalId, StartTime, EndTime) VALUES (@sid,@pos,@pid,@start,@end);";
            cmd.Parameters.AddWithValue("@sid", shift.ScheduleId);
            cmd.Parameters.AddWithValue("@pos", shift.PositionId);
            cmd.Parameters.AddWithValue("@pid", shift.PersonalId);
            cmd.Parameters.AddWithValue("@start", shift.StartTime.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("@end", shift.EndTime.ToUniversalTime().ToString("o"));
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
            cmd.CommandText = "SELECT Id, ScheduleId, PositionId, PersonalId, StartTime, EndTime FROM SingleShifts WHERE ScheduleId=@sid ORDER BY StartTime";
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
                PersonalId = reader.GetInt32(3),
                StartTime = DateTime.Parse(reader.GetString(4)).ToUniversalTime(),
                EndTime = DateTime.Parse(reader.GetString(5)).ToUniversalTime()
            };
        }
        #endregion
    }
}
