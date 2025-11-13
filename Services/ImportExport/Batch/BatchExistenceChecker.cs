using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.ImportExport.Batch
{
    /// <summary>
    /// Provides batch operations for checking record existence in the database.
    /// Avoids N+1 query problems by performing bulk existence checks.
    /// </summary>
    public class BatchExistenceChecker
    {
        /// <summary>
        /// Retrieves existing record IDs from the database in a single batch query.
        /// </summary>
        /// <param name="idsToCheck">Collection of IDs to check for existence</param>
        /// <param name="connection">Active database connection</param>
        /// <param name="transaction">Active database transaction</param>
        /// <param name="tableName">Name of the table to query</param>
        /// <returns>HashSet of existing IDs for fast lookup</returns>
        public async Task<HashSet<int>> GetExistingIdsAsync(
            IEnumerable<int> idsToCheck,
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName)
        {
            if (idsToCheck == null)
            {
                throw new ArgumentNullException(nameof(idsToCheck));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            }

            var idsList = idsToCheck.ToList();

            // Handle empty list case
            if (idsList.Count == 0)
            {
                return new HashSet<int>();
            }

            var existingIds = new HashSet<int>();

            // Build parameterized IN query
            var placeholders = string.Join(",", idsList.Select((_, i) => $"@id{i}"));
            var query = $"SELECT Id FROM {tableName} WHERE Id IN ({placeholders})";

            using var command = new SqliteCommand(query, connection, transaction);

            // Add parameters to prevent SQL injection
            for (int i = 0; i < idsList.Count; i++)
            {
                command.Parameters.AddWithValue($"@id{i}", idsList[i]);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingIds.Add(reader.GetInt32(0));
            }

            return existingIds;
        }

        /// <summary>
        /// Classifies records into those that need to be inserted (new) and those that need to be updated (existing).
        /// </summary>
        /// <typeparam name="T">Type of records to classify</typeparam>
        /// <param name="records">List of records to classify</param>
        /// <param name="existingIds">Set of IDs that already exist in the database</param>
        /// <param name="idSelector">Function to extract the ID from a record</param>
        /// <returns>Tuple containing lists of records to insert and records to update</returns>
        public (List<T> toInsert, List<T> toUpdate) ClassifyRecords<T>(
            List<T> records,
            HashSet<int> existingIds,
            Func<T, int> idSelector)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            if (existingIds == null)
            {
                throw new ArgumentNullException(nameof(existingIds));
            }

            if (idSelector == null)
            {
                throw new ArgumentNullException(nameof(idSelector));
            }

            var toInsert = new List<T>();
            var toUpdate = new List<T>();

            foreach (var record in records)
            {
                var id = idSelector(record);

                if (existingIds.Contains(id))
                {
                    toUpdate.Add(record);
                }
                else
                {
                    toInsert.Add(record);
                }
            }

            return (toInsert, toUpdate);
        }
    }
}
