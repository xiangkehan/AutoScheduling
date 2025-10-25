using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Models;

namespace AutoScheduling3.Data
{
    public class PositionLocationRepository
    {
        private readonly string _connectionString;

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
    Requirements TEXT NOT NULL
);";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> AddAsync(PositionLocation item)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Positions (Name, Location, Description, Requirements) VALUES (@name, @location, @description, @requirements); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", item.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", item.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@requirements", item.Requirements ?? string.Empty);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<PositionLocation>> GetAllAsync()
        {
            var list = new List<PositionLocation>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements FROM Positions ORDER BY Id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PositionLocation
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Location = reader.GetString(2),
                    Description = reader.GetString(3),
                    Requirements = reader.GetString(4)
                });
            }

            return list;
        }

        public async Task<PositionLocation?> GetByIdAsync(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Location, Description, Requirements FROM Positions WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PositionLocation
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Location = reader.GetString(2),
                    Description = reader.GetString(3),
                    Requirements = reader.GetString(4)
                };
            }

            return null;
        }

        public async Task UpdateAsync(PositionLocation item)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Positions SET Name = @name, Location = @location, Description = @description, Requirements = @requirements WHERE Id = @id";
            cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", item.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", item.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@requirements", item.Requirements ?? string.Empty);
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
    }
}
