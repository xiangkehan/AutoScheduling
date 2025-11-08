using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Data.Models;
using AutoScheduling3.Data.Enums;
using AutoScheduling3.Data.Logging;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// DatabaseSchemaValidator: Validates database schema against expected structure
    /// This class compares the actual database schema with the expected schema defined
    /// in DatabaseSchema and reports any discrepancies.
    /// Requirements: 2.1, 2.2, 2.3, 2.4
    /// </summary>
    public class DatabaseSchemaValidator
    {
        private readonly string _connectionString;
        private readonly Dictionary<string, TableSchema> _expectedSchema;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of DatabaseSchemaValidator
        /// </summary>
        /// <param name="connectionString">SQLite connection string</param>
        /// <param name="logger">Optional logger for diagnostic messages</param>
        public DatabaseSchemaValidator(string connectionString, ILogger logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _expectedSchema = DatabaseSchema.GetExpectedSchema();
            _logger = logger;
        }

        /// <summary>
        /// Validates the database schema against expected structure
        /// Requirements: 2.1, 2.2, 2.3, 2.4
        /// </summary>
        /// <returns>Validation result containing all detected issues</returns>
        public async Task<SchemaValidationResult> ValidateSchemaAsync()
        {
            var result = new SchemaValidationResult
            {
                IsValid = true
            };

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                _logger?.Log("Starting database schema validation");

                // Check for missing tables
                result.MissingTables = await GetMissingTablesAsync(conn);
                if (result.MissingTables.Count > 0)
                {
                    result.IsValid = false;
                    _logger?.LogWarning($"Found {result.MissingTables.Count} missing tables");
                }

                // Validate columns for existing tables
                var existingTables = await GetExistingTablesAsync(conn);
                foreach (var tableName in existingTables)
                {
                    if (_expectedSchema.ContainsKey(tableName))
                    {
                        var columnMismatches = await ValidateTableColumnsAsync(conn, tableName);
                        result.ColumnMismatches.AddRange(columnMismatches);
                        
                        if (columnMismatches.Count > 0)
                        {
                            result.IsValid = false;
                            _logger?.LogWarning($"Found {columnMismatches.Count} column issues in table '{tableName}'");
                        }
                    }
                }

                // Check for missing indexes
                result.MissingIndexes = await GetMissingIndexesAsync(conn);
                if (result.MissingIndexes.Count > 0)
                {
                    result.IsValid = false;
                    _logger?.LogWarning($"Found {result.MissingIndexes.Count} missing indexes");
                }

                // Check for extra tables (optional - for information only)
                var extraTables = existingTables.Where(t => !_expectedSchema.ContainsKey(t)).ToList();
                if (extraTables.Count > 0)
                {
                    result.ExtraObjects.AddRange(extraTables.Select(t => $"Table: {t}"));
                    _logger?.Log($"Found {extraTables.Count} unexpected tables (not necessarily an error)");
                }

                if (result.IsValid)
                {
                    _logger?.Log("Schema validation completed successfully - no issues found");
                }
                else
                {
                    _logger?.LogWarning("Schema validation completed with issues");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Schema validation failed with exception: {ex.Message}");
                result.IsValid = false;
                throw;
            }

            return result;
        }

        /// <summary>
        /// Gets a list of tables that exist in the database
        /// </summary>
        private async Task<List<string>> GetExistingTablesAsync(SqliteConnection conn)
        {
            var tables = new List<string>();
            
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT name 
                FROM sqlite_master 
                WHERE type = 'table' 
                AND name NOT LIKE 'sqlite_%'
                ORDER BY name";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        /// <summary>
        /// Detects tables that are missing from the database
        /// Requirements: 2.2
        /// </summary>
        private async Task<List<string>> GetMissingTablesAsync(SqliteConnection conn)
        {
            var missingTables = new List<string>();
            var existingTables = await GetExistingTablesAsync(conn);

            foreach (var expectedTable in _expectedSchema.Keys)
            {
                if (!existingTables.Contains(expectedTable))
                {
                    missingTables.Add(expectedTable);
                    _logger?.LogWarning($"Missing table: {expectedTable}");
                }
            }

            return missingTables;
        }

        /// <summary>
        /// Validates column definitions for a specific table
        /// Requirements: 2.2, 2.3
        /// </summary>
        private async Task<List<ColumnMismatch>> ValidateTableColumnsAsync(SqliteConnection conn, string tableName)
        {
            var mismatches = new List<ColumnMismatch>();

            if (!_expectedSchema.ContainsKey(tableName))
            {
                return mismatches;
            }

            var expectedColumns = _expectedSchema[tableName].Columns;
            var actualColumns = await GetTableColumnsAsync(conn, tableName);

            // Check for missing columns
            foreach (var expectedColumn in expectedColumns)
            {
                var actualColumn = actualColumns.FirstOrDefault(c => 
                    c.Name.Equals(expectedColumn.Name, StringComparison.OrdinalIgnoreCase));

                if (actualColumn == null)
                {
                    mismatches.Add(new ColumnMismatch
                    {
                        TableName = tableName,
                        ColumnName = expectedColumn.Name,
                        Type = MismatchType.Missing,
                        Expected = $"{expectedColumn.DataType} {(expectedColumn.IsNullable ? "NULL" : "NOT NULL")}",
                        Actual = "Column does not exist"
                    });
                    _logger?.LogWarning($"Missing column: {tableName}.{expectedColumn.Name}");
                }
                else
                {
                    // Check data type mismatch
                    if (!actualColumn.DataType.Equals(expectedColumn.DataType, StringComparison.OrdinalIgnoreCase))
                    {
                        mismatches.Add(new ColumnMismatch
                        {
                            TableName = tableName,
                            ColumnName = expectedColumn.Name,
                            Type = MismatchType.TypeMismatch,
                            Expected = expectedColumn.DataType,
                            Actual = actualColumn.DataType
                        });
                        _logger?.LogWarning($"Type mismatch in {tableName}.{expectedColumn.Name}: expected {expectedColumn.DataType}, found {actualColumn.DataType}");
                    }

                    // Check nullability mismatch (only if not primary key, as SQLite handles PK differently)
                    if (!expectedColumn.IsPrimaryKey && actualColumn.IsNullable != expectedColumn.IsNullable)
                    {
                        mismatches.Add(new ColumnMismatch
                        {
                            TableName = tableName,
                            ColumnName = expectedColumn.Name,
                            Type = MismatchType.NullabilityMismatch,
                            Expected = expectedColumn.IsNullable ? "NULL" : "NOT NULL",
                            Actual = actualColumn.IsNullable ? "NULL" : "NOT NULL"
                        });
                        _logger?.LogWarning($"Nullability mismatch in {tableName}.{expectedColumn.Name}");
                    }
                }
            }

            // Check for extra columns (informational)
            foreach (var actualColumn in actualColumns)
            {
                var expectedColumn = expectedColumns.FirstOrDefault(c => 
                    c.Name.Equals(actualColumn.Name, StringComparison.OrdinalIgnoreCase));

                if (expectedColumn == null)
                {
                    mismatches.Add(new ColumnMismatch
                    {
                        TableName = tableName,
                        ColumnName = actualColumn.Name,
                        Type = MismatchType.ExtraColumn,
                        Expected = "Column should not exist",
                        Actual = $"{actualColumn.DataType} {(actualColumn.IsNullable ? "NULL" : "NOT NULL")}"
                    });
                    _logger?.Log($"Extra column found: {tableName}.{actualColumn.Name}");
                }
            }

            return mismatches;
        }

        /// <summary>
        /// Gets column information for a specific table from the database
        /// </summary>
        private async Task<List<ColumnDefinition>> GetTableColumnsAsync(SqliteConnection conn, string tableName)
        {
            var columns = new List<ColumnDefinition>();

            var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName})";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // PRAGMA table_info returns: cid, name, type, notnull, dflt_value, pk
                var column = new ColumnDefinition
                {
                    Name = reader.GetString(1),
                    DataType = reader.GetString(2),
                    IsNullable = reader.GetInt32(3) == 0, // notnull=0 means nullable
                    IsPrimaryKey = reader.GetInt32(5) > 0  // pk>0 means primary key
                };

                // Get default value if present
                if (!reader.IsDBNull(4))
                {
                    column.DefaultValue = reader.GetString(4);
                }

                columns.Add(column);
            }

            return columns;
        }

        /// <summary>
        /// Detects indexes that are missing from the database
        /// Requirements: 2.4
        /// </summary>
        private async Task<List<string>> GetMissingIndexesAsync(SqliteConnection conn)
        {
            var missingIndexes = new List<string>();
            var existingIndexes = await GetExistingIndexesAsync(conn);

            // Get all expected indexes from all tables
            var expectedIndexes = new HashSet<string>();
            foreach (var table in _expectedSchema.Values)
            {
                foreach (var indexName in table.Indexes)
                {
                    expectedIndexes.Add(indexName);
                }
            }

            // Check which expected indexes are missing
            foreach (var expectedIndex in expectedIndexes)
            {
                if (!existingIndexes.Contains(expectedIndex))
                {
                    missingIndexes.Add(expectedIndex);
                    _logger?.LogWarning($"Missing index: {expectedIndex}");
                }
            }

            return missingIndexes;
        }

        /// <summary>
        /// Gets a list of indexes that exist in the database
        /// </summary>
        private async Task<HashSet<string>> GetExistingIndexesAsync(SqliteConnection conn)
        {
            var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT name 
                FROM sqlite_master 
                WHERE type = 'index' 
                AND name NOT LIKE 'sqlite_%'
                ORDER BY name";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }

            return indexes;
        }
    }
}
