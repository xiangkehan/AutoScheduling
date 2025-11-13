using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.ImportExport.Batch
{
    /// <summary>
    /// Provides batch operations for inserting and updating records in the database.
    /// Improves performance by processing multiple records in batches instead of one at a time.
    /// </summary>
    public class BatchImporter
    {
        private const int DefaultBatchSize = 100;

        /// <summary>
        /// Performs batch insert operations for multiple records.
        /// </summary>
        /// <typeparam name="T">Type of records to insert</typeparam>
        /// <param name="records">List of records to insert</param>
        /// <param name="connection">Active database connection</param>
        /// <param name="transaction">Active database transaction</param>
        /// <param name="tableName">Name of the table to insert into</param>
        /// <param name="fieldMapper">Function to map a record to its field values</param>
        /// <param name="batchSize">Number of records to process per batch (default: 100)</param>
        /// <returns>Total number of records inserted</returns>
        public async Task<int> BatchInsertAsync<T>(
            List<T> records,
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            Func<T, Dictionary<string, object>> fieldMapper,
            int batchSize = DefaultBatchSize)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            }

            if (fieldMapper == null)
            {
                throw new ArgumentNullException(nameof(fieldMapper));
            }

            if (batchSize <= 0)
            {
                throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
            }

            if (records.Count == 0)
            {
                return 0;
            }

            int totalInserted = 0;

            // Process records in batches
            for (int i = 0; i < records.Count; i += batchSize)
            {
                var batch = records.Skip(i).Take(batchSize).ToList();
                totalInserted += await InsertBatchAsync(batch, connection, transaction, tableName, fieldMapper);
            }

            return totalInserted;
        }

        /// <summary>
        /// Performs batch update operations for multiple records.
        /// </summary>
        /// <typeparam name="T">Type of records to update</typeparam>
        /// <param name="records">List of records to update</param>
        /// <param name="connection">Active database connection</param>
        /// <param name="transaction">Active database transaction</param>
        /// <param name="tableName">Name of the table to update</param>
        /// <param name="fieldMapper">Function to map a record to its field values</param>
        /// <param name="idSelector">Function to extract the ID from a record</param>
        /// <param name="batchSize">Number of records to process per batch (default: 100)</param>
        /// <returns>Total number of records updated</returns>
        public async Task<int> BatchUpdateAsync<T>(
            List<T> records,
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            Func<T, Dictionary<string, object>> fieldMapper,
            Func<T, int> idSelector,
            int batchSize = DefaultBatchSize)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            }

            if (fieldMapper == null)
            {
                throw new ArgumentNullException(nameof(fieldMapper));
            }

            if (idSelector == null)
            {
                throw new ArgumentNullException(nameof(idSelector));
            }

            if (batchSize <= 0)
            {
                throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
            }

            if (records.Count == 0)
            {
                return 0;
            }

            int totalUpdated = 0;

            // Process records in batches
            for (int i = 0; i < records.Count; i += batchSize)
            {
                var batch = records.Skip(i).Take(batchSize).ToList();
                totalUpdated += await UpdateBatchAsync(batch, connection, transaction, tableName, fieldMapper, idSelector);
            }

            return totalUpdated;
        }

        /// <summary>
        /// Inserts a single batch of records using a multi-row INSERT statement.
        /// </summary>
        private async Task<int> InsertBatchAsync<T>(
            List<T> batch,
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            Func<T, Dictionary<string, object>> fieldMapper)
        {
            if (batch.Count == 0)
            {
                return 0;
            }

            // Get field names from the first record
            var firstRecordFields = fieldMapper(batch[0]);
            var fieldNames = firstRecordFields.Keys.ToList();
            var fieldList = string.Join(", ", fieldNames);

            var valuesClauses = new List<string>();
            using var command = new SqliteCommand { Connection = connection, Transaction = transaction };

            // Build VALUES clauses for each record in the batch
            for (int j = 0; j < batch.Count; j++)
            {
                var record = batch[j];
                var fieldValues = fieldMapper(record);

                var placeholders = new List<string>();

                foreach (var fieldName in fieldNames)
                {
                    var paramName = $"@{fieldName}_{j}";
                    placeholders.Add(paramName);

                    // Add parameter with proper null handling
                    var value = fieldValues.ContainsKey(fieldName) ? fieldValues[fieldName] : null;
                    command.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                }

                valuesClauses.Add($"({string.Join(", ", placeholders)})");
            }

            // Build and execute the INSERT statement
            command.CommandText = $"INSERT INTO {tableName} ({fieldList}) VALUES {string.Join(", ", valuesClauses)}";

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Updates a single batch of records using individual UPDATE statements.
        /// Does not update the primary key (Id field).
        /// </summary>
        private async Task<int> UpdateBatchAsync<T>(
            List<T> batch,
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            Func<T, Dictionary<string, object>> fieldMapper,
            Func<T, int> idSelector)
        {
            if (batch.Count == 0)
            {
                return 0;
            }

            int totalUpdated = 0;

            // Execute individual UPDATE statements for each record
            // SQLite doesn't support multi-row UPDATE efficiently, so we use individual statements
            foreach (var record in batch)
            {
                var fieldValues = fieldMapper(record);
                var id = idSelector(record);

                var setClauses = new List<string>();
                using var command = new SqliteCommand { Connection = connection, Transaction = transaction };

                // Build SET clauses, excluding the primary key
                foreach (var kvp in fieldValues)
                {
                    if (kvp.Key.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip primary key - never update it
                    }

                    setClauses.Add($"{kvp.Key} = @{kvp.Key}");
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                // Add the WHERE clause parameter
                command.Parameters.AddWithValue("@Id", id);

                // Build and execute the UPDATE statement
                command.CommandText = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE Id = @Id";

                totalUpdated += await command.ExecuteNonQueryAsync();
            }

            return totalUpdated;
        }
    }
}
