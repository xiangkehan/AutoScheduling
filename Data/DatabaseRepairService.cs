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
    /// DatabaseRepairService: Repairs database schema issues automatically
    /// This class can create missing tables, add missing columns, and create missing indexes
    /// to bring the database schema into compliance with the expected structure.
    /// Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6
    /// </summary>
    public class DatabaseRepairService
    {
        private readonly string _connectionString;
        private readonly DatabaseSchemaValidator _validator;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of DatabaseRepairService
        /// </summary>
        /// <param name="connectionString">SQLite connection string</param>
        /// <param name="validator">Schema validator for checking repair results</param>
        /// <param name="logger">Optional logger for diagnostic messages</param>
        public DatabaseRepairService(
            string connectionString, 
            DatabaseSchemaValidator validator,
            ILogger logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger;
        }

        /// <summary>
        /// Repairs database schema based on validation results
        /// Requirements: 3.1, 3.2, 3.3, 3.4, 3.5
        /// </summary>
        /// <param name="validationResult">Schema validation result containing issues to fix</param>
        /// <param name="options">Options controlling which repairs to perform</param>
        /// <returns>Result containing details of all repair actions performed</returns>
        public async Task<RepairResult> RepairSchemaAsync(
            SchemaValidationResult validationResult,
            RepairOptions options = null)
        {
            options ??= new RepairOptions();

            var result = new RepairResult
            {
                Success = true
            };

            if (validationResult == null)
            {
                result.Success = false;
                result.ErrorMessage = "Validation result cannot be null";
                return result;
            }

            if (validationResult.IsValid)
            {
                _logger?.Log("Schema is already valid, no repairs needed");
                return result;
            }

            _logger?.Log("Starting database schema repair");

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                // Repair missing tables
                if (options.CreateMissingTables && validationResult.MissingTables.Count > 0)
                {
                    _logger?.Log($"Repairing {validationResult.MissingTables.Count} missing tables");
                    await CreateMissingTablesAsync(conn, validationResult.MissingTables, result);
                }

                // Repair missing columns
                if (options.AddMissingColumns && validationResult.ColumnMismatches.Count > 0)
                {
                    var missingColumns = validationResult.ColumnMismatches
                        .Where(m => m.Type == MismatchType.Missing)
                        .ToList();

                    if (missingColumns.Count > 0)
                    {
                        _logger?.Log($"Repairing {missingColumns.Count} missing columns");
                        await AddMissingColumnsAsync(conn, missingColumns, result);
                    }
                }

                // Repair missing indexes
                if (options.CreateMissingIndexes && validationResult.MissingIndexes.Count > 0)
                {
                    _logger?.Log($"Repairing {validationResult.MissingIndexes.Count} missing indexes");
                    await CreateMissingIndexesAsync(conn, validationResult.MissingIndexes, result);
                }

                // Check if any repairs failed
                if (result.FailedActions.Count > 0)
                {
                    result.Success = false;
                    result.ErrorMessage = $"{result.FailedActions.Count} repair action(s) failed";
                    _logger?.LogError($"Repair completed with {result.FailedActions.Count} failures");
                }
                else
                {
                    _logger?.Log($"Repair completed successfully. {result.ActionsPerformed.Count} action(s) performed");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Repair failed with exception: {ex.Message}";
                _logger?.LogError($"Repair failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates missing tables in the database
        /// Requirements: 3.1, 3.4, 3.6
        /// </summary>
        /// <param name="conn">Open database connection</param>
        /// <param name="missingTables">List of table names to create</param>
        /// <param name="result">Repair result to update with actions performed</param>
        private async Task CreateMissingTablesAsync(
            SqliteConnection conn, 
            List<string> missingTables,
            RepairResult result)
        {
            foreach (var tableName in missingTables)
            {
                var action = new RepairAction
                {
                    Type = RepairActionType.CreateTable,
                    Target = tableName,
                    Description = $"Create missing table '{tableName}'"
                };

                using var transaction = conn.BeginTransaction();
                try
                {
                    _logger?.Log($"Creating missing table: {tableName}");

                    // Get the CREATE TABLE SQL from DatabaseSchema
                    var createTableSql = DatabaseSchema.GetCreateTableSql(tableName);

                    var cmd = conn.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = createTableSql;
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    action.Success = true;
                    result.ActionsPerformed.Add(action);
                    _logger?.Log($"Successfully created table: {tableName}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    action.Success = false;
                    action.ErrorMessage = ex.Message;
                    result.FailedActions.Add(action);
                    _logger?.LogError($"Failed to create table '{tableName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Adds missing columns to existing tables
        /// Requirements: 3.3, 3.4, 3.6
        /// </summary>
        /// <param name="conn">Open database connection</param>
        /// <param name="missingColumns">List of column mismatches representing missing columns</param>
        /// <param name="result">Repair result to update with actions performed</param>
        private async Task AddMissingColumnsAsync(
            SqliteConnection conn,
            List<ColumnMismatch> missingColumns,
            RepairResult result)
        {
            // Get expected schema for reference
            var expectedSchema = DatabaseSchema.GetExpectedSchema();

            foreach (var mismatch in missingColumns)
            {
                var action = new RepairAction
                {
                    Type = RepairActionType.AddColumn,
                    Target = $"{mismatch.TableName}.{mismatch.ColumnName}",
                    Description = $"Add missing column '{mismatch.ColumnName}' to table '{mismatch.TableName}'"
                };

                using var transaction = conn.BeginTransaction();
                try
                {
                    _logger?.Log($"Adding missing column: {mismatch.TableName}.{mismatch.ColumnName}");

                    // Find the column definition in the expected schema
                    if (!expectedSchema.ContainsKey(mismatch.TableName))
                    {
                        throw new InvalidOperationException($"Table '{mismatch.TableName}' not found in expected schema");
                    }

                    var tableSchema = expectedSchema[mismatch.TableName];
                    var columnDef = tableSchema.Columns.FirstOrDefault(c => 
                        c.Name.Equals(mismatch.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (columnDef == null)
                    {
                        throw new InvalidOperationException($"Column '{mismatch.ColumnName}' not found in expected schema for table '{mismatch.TableName}'");
                    }

                    // Build ALTER TABLE statement
                    var alterSql = $"ALTER TABLE {mismatch.TableName} ADD COLUMN {columnDef.Name} {columnDef.DataType}";

                    // Add NOT NULL constraint if applicable (only if there's a default value)
                    if (!columnDef.IsNullable && !string.IsNullOrEmpty(columnDef.DefaultValue))
                    {
                        alterSql += " NOT NULL";
                    }

                    // Add DEFAULT clause if specified
                    if (!string.IsNullOrEmpty(columnDef.DefaultValue))
                    {
                        alterSql += $" DEFAULT {columnDef.DefaultValue}";
                    }

                    var cmd = conn.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = alterSql;
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    action.Success = true;
                    result.ActionsPerformed.Add(action);
                    _logger?.Log($"Successfully added column: {mismatch.TableName}.{mismatch.ColumnName}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    action.Success = false;
                    action.ErrorMessage = ex.Message;
                    result.FailedActions.Add(action);
                    _logger?.LogError($"Failed to add column '{mismatch.TableName}.{mismatch.ColumnName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates missing indexes in the database
        /// Requirements: 3.2, 3.4, 3.6
        /// </summary>
        /// <param name="conn">Open database connection</param>
        /// <param name="missingIndexes">List of index names to create</param>
        /// <param name="result">Repair result to update with actions performed</param>
        private async Task CreateMissingIndexesAsync(
            SqliteConnection conn,
            List<string> missingIndexes,
            RepairResult result)
        {
            // Get index definitions from DatabaseSchema
            var indexDefinitions = DatabaseSchema.GetIndexDefinitions();

            foreach (var indexName in missingIndexes)
            {
                var action = new RepairAction
                {
                    Type = RepairActionType.CreateIndex,
                    Target = indexName,
                    Description = $"Create missing index '{indexName}'"
                };

                using var transaction = conn.BeginTransaction();
                try
                {
                    _logger?.Log($"Creating missing index: {indexName}");

                    // Get the CREATE INDEX SQL from DatabaseSchema
                    if (!indexDefinitions.ContainsKey(indexName))
                    {
                        throw new InvalidOperationException($"Index '{indexName}' not found in schema definition");
                    }

                    var createIndexSql = indexDefinitions[indexName];

                    var cmd = conn.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = createIndexSql;
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    action.Success = true;
                    result.ActionsPerformed.Add(action);
                    _logger?.Log($"Successfully created index: {indexName}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    action.Success = false;
                    action.ErrorMessage = ex.Message;
                    result.FailedActions.Add(action);
                    _logger?.LogError($"Failed to create index '{indexName}': {ex.Message}");
                }
            }
        }
    }
}
