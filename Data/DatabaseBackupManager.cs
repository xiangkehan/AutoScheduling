using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.Data.Models;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// Manages database backup and restoration operations
    /// </summary>
    public class DatabaseBackupManager
    {
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private readonly int _maxBackups;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the DatabaseBackupManager
        /// </summary>
        /// <param name="databasePath">Full path to the database file</param>
        /// <param name="backupDirectory">Directory where backups will be stored (optional, defaults to database directory + "backups")</param>
        /// <param name="maxBackups">Maximum number of backups to retain</param>
        /// <param name="logger">Logger instance for operation logging (optional)</param>
        public DatabaseBackupManager(
            string databasePath,
            string backupDirectory = null,
            int maxBackups = 5,
            ILogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Database path cannot be null or empty", nameof(databasePath));
            }

            if (maxBackups < 1)
            {
                throw new ArgumentException("Max backups must be at least 1", nameof(maxBackups));
            }

            _databasePath = databasePath;
            _maxBackups = maxBackups;
            _logger = logger;

            // Default backup directory is a "backups" subfolder next to the database
            if (string.IsNullOrWhiteSpace(backupDirectory))
            {
                var dbDirectory = Path.GetDirectoryName(databasePath);
                _backupDirectory = Path.Combine(dbDirectory ?? ".", "backups");
            }
            else
            {
                _backupDirectory = backupDirectory;
            }

            // Initialize backup directory if it doesn't exist
            InitializeBackupDirectory();
        }

        /// <summary>
        /// Initializes the backup directory, creating it if necessary
        /// </summary>
        private void InitializeBackupDirectory()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                    _logger?.Log($"Created backup directory: {_backupDirectory}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMsg = $"权限不足，无法创建备份目录: {_backupDirectory}";
                _logger?.LogError(errorMsg);
                throw new UnauthorizedAccessException(errorMsg, ex);
            }
            catch (IOException ex)
            {
                var errorMsg = $"创建备份目录时发生IO错误: {_backupDirectory}";
                _logger?.LogError(errorMsg);
                throw new IOException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to create backup directory: {ex.Message}");
                throw new IOException($"Failed to create backup directory at {_backupDirectory}", ex);
            }
        }

        /// <summary>
        /// Creates a backup of the database
        /// </summary>
        /// <param name="customPath">Optional custom path for the backup file</param>
        /// <returns>Path to the created backup file</returns>
        public async Task<string> CreateBackupAsync(string customPath = null)
        {
            try
            {
                // Check if source database exists
                if (!File.Exists(_databasePath))
                {
                    throw new FileNotFoundException($"Database file not found: {_databasePath}");
                }

                // Generate backup filename with timestamp
                string backupPath;
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    backupPath = customPath;
                }
                else
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var dbFileName = Path.GetFileNameWithoutExtension(_databasePath);
                    var backupFileName = $"{dbFileName}_backup_{timestamp}.db";
                    backupPath = Path.Combine(_backupDirectory, backupFileName);
                }

                _logger?.Log($"Creating backup: {backupPath}");

                // Ensure backup directory exists
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                {
                    try
                    {
                        Directory.CreateDirectory(backupDir);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        var errorMsg = $"权限不足，无法创建备份目录: {backupDir}";
                        _logger?.LogError(errorMsg);
                        throw new UnauthorizedAccessException(errorMsg, ex);
                    }
                    catch (IOException ex)
                    {
                        var errorMsg = $"创建备份目录时发生IO错误: {backupDir}";
                        _logger?.LogError(errorMsg);
                        throw new IOException(errorMsg, ex);
                    }
                }

                // Copy database file to backup location
                try
                {
                    await Task.Run(() => File.Copy(_databasePath, backupPath, overwrite: true));
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = $"权限不足，无法创建备份文件: {backupPath}";
                    _logger?.LogError(errorMsg);
                    throw new UnauthorizedAccessException(errorMsg, ex);
                }
                catch (IOException ex)
                {
                    var errorMsg = $"创建备份文件时发生IO错误: {backupPath}";
                    _logger?.LogError(errorMsg);
                    throw new IOException(errorMsg, ex);
                }

                _logger?.Log($"Backup file created: {backupPath}");

                // Verify the backup
                var isValid = await VerifyBackupAsync(backupPath);
                if (!isValid)
                {
                    _logger?.LogError($"Backup verification failed: {backupPath}");
                    throw new InvalidOperationException($"Backup verification failed for {backupPath}");
                }

                _logger?.Log($"Backup verified successfully: {backupPath}");

                // Cleanup old backups if needed
                await CleanupOldBackupsAsync();

                return backupPath;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to create backup: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Verifies the integrity of a backup file
        /// </summary>
        /// <param name="backupPath">Path to the backup file to verify</param>
        /// <returns>True if the backup is valid, false otherwise</returns>
        public async Task<bool> VerifyBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger?.LogError($"Backup file not found: {backupPath}");
                    return false;
                }

                // Check file size
                var fileInfo = new FileInfo(backupPath);
                if (fileInfo.Length == 0)
                {
                    _logger?.LogError($"Backup file is empty: {backupPath}");
                    return false;
                }

                // Verify SQLite integrity
                var connectionString = $"Data Source={backupPath};Mode=ReadOnly";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check;";

                var result = await command.ExecuteScalarAsync();
                var integrityResult = result?.ToString() ?? "";

                if (integrityResult.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Log($"Backup integrity check passed: {backupPath}");
                    return true;
                }
                else
                {
                    _logger?.LogError($"Backup integrity check failed: {backupPath} - {integrityResult}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error verifying backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores the database from a backup file
        /// </summary>
        /// <param name="backupPath">Path to the backup file to restore from</param>
        public async Task RestoreFromBackupAsync(string backupPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupPath))
                {
                    throw new ArgumentException("Backup path cannot be null or empty", nameof(backupPath));
                }

                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupPath}");
                }

                _logger?.Log($"Starting restore from backup: {backupPath}");

                // Validate backup before restoration
                var isValid = await VerifyBackupAsync(backupPath);
                if (!isValid)
                {
                    throw new InvalidOperationException($"Backup file is invalid or corrupted: {backupPath}");
                }

                // Create a backup of the current database before restoring (if it exists)
                if (File.Exists(_databasePath))
                {
                    var preRestoreBackupPath = _databasePath + ".pre-restore";
                    _logger?.Log($"Creating pre-restore backup: {preRestoreBackupPath}");
                    try
                    {
                        await Task.Run(() => File.Copy(_databasePath, preRestoreBackupPath, overwrite: true));
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        var errorMsg = $"权限不足，无法创建恢复前备份: {preRestoreBackupPath}";
                        _logger?.LogError(errorMsg);
                        throw new UnauthorizedAccessException(errorMsg, ex);
                    }
                    catch (IOException ex)
                    {
                        var errorMsg = $"创建恢复前备份时发生IO错误: {preRestoreBackupPath}";
                        _logger?.LogError(errorMsg);
                        throw new IOException(errorMsg, ex);
                    }
                }

                // Close all connections by forcing garbage collection
                // Note: In a real application, the caller should ensure all connections are closed
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Small delay to ensure file handles are released
                await Task.Delay(100);

                // Restore the backup
                _logger?.Log($"Restoring database from backup: {backupPath}");
                try
                {
                    await Task.Run(() => File.Copy(backupPath, _databasePath, overwrite: true));
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = $"权限不足，无法恢复数据库文件: {_databasePath}";
                    _logger?.LogError(errorMsg);
                    throw new UnauthorizedAccessException(errorMsg, ex);
                }
                catch (IOException ex)
                {
                    var errorMsg = $"恢复数据库文件时发生IO错误: {_databasePath}";
                    _logger?.LogError(errorMsg);
                    throw new IOException(errorMsg, ex);
                }

                _logger?.Log($"Database restored successfully from: {backupPath}");

                // Verify the restored database
                var restoredIsValid = await VerifyBackupAsync(_databasePath);
                if (!restoredIsValid)
                {
                    _logger?.LogError("Restored database failed integrity check");
                    throw new InvalidOperationException("Restored database failed integrity check");
                }

                _logger?.Log("Restored database verified successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to restore from backup: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lists all available backup files
        /// </summary>
        /// <returns>Collection of backup information</returns>
        public async Task<IEnumerable<BackupInfo>> ListBackupsAsync()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    _logger?.LogWarning($"Backup directory does not exist: {_backupDirectory}");
                    return Enumerable.Empty<BackupInfo>();
                }

                string[] backupFiles;
                try
                {
                    backupFiles = Directory.GetFiles(_backupDirectory, "*.db");
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = $"权限不足，无法访问备份目录: {_backupDirectory}";
                    _logger?.LogError(errorMsg);
                    throw new UnauthorizedAccessException(errorMsg, ex);
                }
                catch (IOException ex)
                {
                    var errorMsg = $"访问备份目录时发生IO错误: {_backupDirectory}";
                    _logger?.LogError(errorMsg);
                    throw new IOException(errorMsg, ex);
                }

                var backupFilesList = backupFiles
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                var backupInfoList = new List<BackupInfo>();

                foreach (var backupFile in backupFilesList)
                {
                    try
                    {
                        var fileInfo = new FileInfo(backupFile);
                        var backupInfo = new BackupInfo
                        {
                            FilePath = backupFile,
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            CreatedAt = fileInfo.CreationTime,
                            DatabaseVersion = await GetDatabaseVersionAsync(backupFile),
                            IsVerified = await VerifyBackupAsync(backupFile)
                        };

                        backupInfoList.Add(backupInfo);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger?.LogWarning($"权限不足，无法访问备份文件 {backupFile}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        _logger?.LogWarning($"访问备份文件时发生IO错误 {backupFile}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"处理备份文件时发生错误 {backupFile}: {ex.Message}");
                    }
                }

                _logger?.Log($"Found {backupInfoList.Count} backup files");
                return backupInfoList;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to list backups: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Cleans up old backups, keeping only the most recent ones up to the max backup count
        /// </summary>
        public async Task CleanupOldBackupsAsync()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    return;
                }

                string[] backupFilePaths;
                try
                {
                    backupFilePaths = Directory.GetFiles(_backupDirectory, "*.db");
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = $"权限不足，无法访问备份目录进行清理: {_backupDirectory}";
                    _logger?.LogError(errorMsg);
                    throw new UnauthorizedAccessException(errorMsg, ex);
                }
                catch (IOException ex)
                {
                    var errorMsg = $"访问备份目录时发生IO错误: {_backupDirectory}";
                    _logger?.LogError(errorMsg);
                    throw new IOException(errorMsg, ex);
                }

                var backupFiles = backupFilePaths
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (backupFiles.Count <= _maxBackups)
                {
                    _logger?.Log($"No cleanup needed. Current backups: {backupFiles.Count}, Max: {_maxBackups}");
                    return;
                }

                var filesToDelete = backupFiles.Skip(_maxBackups).ToList();
                _logger?.Log($"Cleaning up {filesToDelete.Count} old backup(s)");

                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        await DeleteBackupAsync(fileToDelete.FullName);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger?.LogWarning($"权限不足，无法删除备份 {fileToDelete.Name}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        _logger?.LogWarning($"删除备份时发生IO错误 {fileToDelete.Name}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Failed to delete backup {fileToDelete.Name}: {ex.Message}");
                    }
                }

                _logger?.Log($"Cleanup complete. Remaining backups: {_maxBackups}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to cleanup old backups: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a specific backup file
        /// </summary>
        /// <param name="backupPath">Path to the backup file to delete</param>
        public async Task DeleteBackupAsync(string backupPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupPath))
                {
                    throw new ArgumentException("Backup path cannot be null or empty", nameof(backupPath));
                }

                if (!File.Exists(backupPath))
                {
                    _logger?.LogWarning($"Backup file not found: {backupPath}");
                    return;
                }

                _logger?.Log($"Deleting backup: {backupPath}");
                try
                {
                    await Task.Run(() => File.Delete(backupPath));
                    _logger?.Log($"Backup deleted: {backupPath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = $"权限不足，无法删除备份文件: {backupPath}";
                    _logger?.LogError(errorMsg);
                    throw new UnauthorizedAccessException(errorMsg, ex);
                }
                catch (IOException ex)
                {
                    var errorMsg = $"删除备份文件时发生IO错误: {backupPath}";
                    _logger?.LogError(errorMsg);
                    throw new IOException(errorMsg, ex);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to delete backup: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the database version from a database file
        /// </summary>
        /// <param name="databasePath">Path to the database file</param>
        /// <returns>Database version number, or 0 if not found</returns>
        private async Task<int> GetDatabaseVersionAsync(string databasePath)
        {
            try
            {
                var connectionString = $"Data Source={databasePath};Mode=ReadOnly";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version;";

                var result = await command.ExecuteScalarAsync();
                if (result != null && int.TryParse(result.ToString(), out int version))
                {
                    return version;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to get database version from {databasePath}: {ex.Message}");
                return 0;
            }
        }
    }
}
