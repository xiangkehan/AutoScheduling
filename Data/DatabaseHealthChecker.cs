using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Data.Enums;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.Data.Models;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// Performs comprehensive health checks on the database
    /// Requirements: 2.1, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 2.5
    /// </summary>
    public class DatabaseHealthChecker
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of DatabaseHealthChecker
        /// </summary>
        /// <param name="connectionString">SQLite connection string</param>
        /// <param name="logger">Optional logger for diagnostic messages</param>
        public DatabaseHealthChecker(string connectionString, ILogger logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        /// <summary>
        /// Performs a comprehensive health check on the database
        /// Requirements: 2.1, 4.1, 4.2
        /// </summary>
        /// <returns>A detailed health report</returns>
        public async Task<DatabaseHealthReport> CheckHealthAsync()
        {
            _logger?.Log("Starting database health check");
            
            var report = new DatabaseHealthReport
            {
                CheckedAt = DateTime.UtcNow,
                Issues = new List<HealthIssue>()
            };

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                // Perform all health checks
                await CheckFileIntegrityAsync(conn, report.Issues);
                await CheckVersionConsistencyAsync(conn, report.Issues);
                await CheckForLocksAsync(report.Issues);
                await CheckForeignKeyIntegrityAsync(conn, report.Issues);
                
                // Collect metrics
                report.Metrics = await CollectMetricsAsync(conn);

                // Determine overall status based on issues
                report.OverallStatus = DetermineOverallStatus(report.Issues);

                _logger?.Log($"Health check completed. Status: {report.OverallStatus}, Issues found: {report.Issues.Count}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Health check failed: {ex.Message}");
                report.Issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = "Connection",
                    Description = $"Failed to perform health check: {ex.Message}",
                    Recommendation = "Verify database file exists and is accessible"
                });
                report.OverallStatus = HealthStatus.Critical;
            }

            return report;
        }

        /// <summary>
        /// Checks database file integrity using PRAGMA integrity_check
        /// Requirements: 2.1, 4.2
        /// </summary>
        private async Task CheckFileIntegrityAsync(SqliteConnection conn, List<HealthIssue> issues)
        {
            try
            {
                _logger?.Log("Checking file integrity");
                
                var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check";
                
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new List<string>();
                
                while (await reader.ReadAsync())
                {
                    results.Add(reader.GetString(0));
                }

                // If integrity_check returns "ok", the database is fine
                if (results.Count == 1 && results[0].Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Log("File integrity check passed");
                    return;
                }

                // Otherwise, we have corruption issues
                foreach (var result in results)
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Critical,
                        Category = "Integrity",
                        Description = $"Database corruption detected: {result}",
                        Recommendation = "Create a backup and attempt repair, or restore from a previous backup"
                    });
                }
                
                _logger?.LogError($"File integrity check failed with {results.Count} issues");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to check file integrity: {ex.Message}");
                issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = "Integrity",
                    Description = $"Could not perform integrity check: {ex.Message}",
                    Recommendation = "Verify database file is not locked or corrupted"
                });
            }
        }

        /// <summary>
        /// Validates database version consistency
        /// Requirements: 4.3
        /// </summary>
        private async Task CheckVersionConsistencyAsync(SqliteConnection conn, List<HealthIssue> issues)
        {
            try
            {
                _logger?.Log("Checking version consistency");
                
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='DatabaseVersion'";
                var versionTableExists = await cmd.ExecuteScalarAsync() != null;

                if (!versionTableExists)
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Schema",
                        Description = "DatabaseVersion table is missing",
                        Recommendation = "Initialize the database or run schema repair"
                    });
                    return;
                }

                cmd.CommandText = "SELECT Version FROM DatabaseVersion WHERE Id = 1";
                var versionObj = await cmd.ExecuteScalarAsync();

                if (versionObj == null)
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Schema",
                        Description = "Database version is not set",
                        Recommendation = "Run database initialization to set version"
                    });
                }
                else
                {
                    var version = Convert.ToInt32(versionObj);
                    if (version < 1)
                    {
                        issues.Add(new HealthIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Category = "Schema",
                            Description = $"Invalid database version: {version}",
                            Recommendation = "Run database migration to update version"
                        });
                    }
                    else
                    {
                        _logger?.Log($"Database version is {version}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to check version consistency: {ex.Message}");
                issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = "Schema",
                    Description = $"Could not verify database version: {ex.Message}",
                    Recommendation = "Check database schema integrity"
                });
            }
        }

        /// <summary>
        /// Checks if the database file is locked by other processes
        /// Requirements: 4.6
        /// </summary>
        private async Task CheckForLocksAsync(List<HealthIssue> issues)
        {
            try
            {
                _logger?.Log("Checking for database locks");
                
                // Extract database path from connection string
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                var dbPath = builder.DataSource;

                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Critical,
                        Category = "File",
                        Description = "Database file does not exist",
                        Recommendation = "Initialize the database"
                    });
                    return;
                }

                // Try to open the file exclusively to check for locks
                // If we can't, it might be locked
                try
                {
                    using var fileStream = File.Open(dbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _logger?.Log("No exclusive locks detected");
                }
                catch (IOException)
                {
                    // File might be locked, but this is not always a problem
                    // SQLite uses shared locks, so this is informational
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Info,
                        Category = "File",
                        Description = "Database file may be in use by other processes",
                        Recommendation = "This is normal if the application is running. Close other connections if experiencing issues."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to check for locks: {ex.Message}");
                issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Info,
                    Category = "File",
                    Description = $"Could not check for file locks: {ex.Message}",
                    Recommendation = "Verify file permissions and accessibility"
                });
            }
        }

        /// <summary>
        /// Checks for orphaned foreign key references
        /// Requirements: 4.4
        /// </summary>
        private async Task CheckForeignKeyIntegrityAsync(SqliteConnection conn, List<HealthIssue> issues)
        {
            try
            {
                _logger?.Log("Checking foreign key integrity");
                
                // Enable foreign key checking
                var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA foreign_keys = ON";
                await cmd.ExecuteNonQueryAsync();

                // Check foreign key integrity
                cmd.CommandText = "PRAGMA foreign_key_check";
                using var reader = await cmd.ExecuteReaderAsync();
                
                var violations = new List<string>();
                while (await reader.ReadAsync())
                {
                    var table = reader.GetString(0);
                    var rowid = reader.GetInt64(1);
                    var parent = reader.GetString(2);
                    var fkid = reader.GetInt32(3);
                    
                    violations.Add($"Table '{table}' row {rowid} has invalid reference to '{parent}' (FK #{fkid})");
                }

                if (violations.Any())
                {
                    foreach (var violation in violations)
                    {
                        issues.Add(new HealthIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Category = "Integrity",
                            Description = $"Foreign key violation: {violation}",
                            Recommendation = "Clean up orphaned records or restore referential integrity"
                        });
                    }
                    _logger?.LogWarning($"Found {violations.Count} foreign key violations");
                }
                else
                {
                    _logger?.Log("Foreign key integrity check passed");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to check foreign key integrity: {ex.Message}");
                issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Info,
                    Category = "Integrity",
                    Description = $"Could not check foreign key integrity: {ex.Message}",
                    Recommendation = "Verify database supports foreign keys"
                });
            }
        }

        /// <summary>
        /// Collects database metrics
        /// Requirements: 4.5
        /// </summary>
        private async Task<DatabaseMetrics> CollectMetricsAsync(SqliteConnection conn)
        {
            var metrics = new DatabaseMetrics();

            try
            {
                _logger?.Log("Collecting database metrics");
                
                // Get file size
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                var dbPath = builder.DataSource;
                if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    metrics.FileSizeBytes = fileInfo.Length;
                }

                // Count tables
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                metrics.TableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // Count indexes
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%'";
                metrics.IndexCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // Count total rows (approximate)
                cmd.CommandText = @"
                    SELECT SUM(cnt) FROM (
                        SELECT COUNT(*) as cnt FROM Personals
                        UNION ALL SELECT COUNT(*) FROM Positions
                        UNION ALL SELECT COUNT(*) FROM Skills
                        UNION ALL SELECT COUNT(*) FROM Schedules
                        UNION ALL SELECT COUNT(*) FROM SingleShifts
                        UNION ALL SELECT COUNT(*) FROM SchedulingTemplates
                        UNION ALL SELECT COUNT(*) FROM FixedPositionRules
                        UNION ALL SELECT COUNT(*) FROM HolidayConfigs
                        UNION ALL SELECT COUNT(*) FROM ManualAssignments
                    )";
                
                var totalRowsObj = await cmd.ExecuteScalarAsync();
                metrics.TotalRowCount = totalRowsObj != null && totalRowsObj != DBNull.Value 
                    ? Convert.ToInt32(totalRowsObj) 
                    : 0;

                // Get fragmentation percentage (using page count)
                cmd.CommandText = "PRAGMA page_count";
                var pageCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                
                cmd.CommandText = "PRAGMA freelist_count";
                var freePages = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                if (pageCount > 0)
                {
                    metrics.FragmentationPercentage = (double)freePages / pageCount * 100.0;
                }

                _logger?.Log($"Metrics collected: {metrics.TableCount} tables, {metrics.IndexCount} indexes, {metrics.TotalRowCount} rows");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to collect some metrics: {ex.Message}");
                // Return partial metrics
            }

            return metrics;
        }

        /// <summary>
        /// Determines overall health status based on issues found
        /// Requirements: 2.5, 4.7
        /// </summary>
        private HealthStatus DetermineOverallStatus(List<HealthIssue> issues)
        {
            if (!issues.Any())
            {
                return HealthStatus.Healthy;
            }

            if (issues.Any(i => i.Severity == IssueSeverity.Critical))
            {
                return HealthStatus.Critical;
            }

            if (issues.Any(i => i.Severity == IssueSeverity.Warning))
            {
                return HealthStatus.Warning;
            }

            return HealthStatus.Healthy;
        }
    }
}
