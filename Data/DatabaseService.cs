using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.Data.Models;
using AutoScheduling3.Data.Enums;
using AutoScheduling3.Data.Exceptions;
using System.Collections.Generic;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 数据库服务：管理数据库初始化、版本管理和迁移
    /// 需求: 1.2, 2.1
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private const int CurrentVersion = 1;

        // New components for enhanced initialization
        private readonly DatabaseHealthChecker _healthChecker;
        private readonly DatabaseSchemaValidator _schemaValidator;
        private readonly DatabaseRepairService _repairService;
        private readonly DatabaseBackupManager _backupManager;
        private readonly InitializationStateManager _stateManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of DatabaseService
        /// Requirements: 1.1, 1.4
        /// </summary>
        /// <param name="dbPath">Path to the database file</param>
        /// <param name="logger">Optional logger for diagnostic messages</param>
        public DatabaseService(string dbPath, ILogger logger = null)
        {
            _dbPath = dbPath;
            _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
            
            // Initialize logger (use DebugLogger if none provided)
            _logger = logger ?? new DebugLogger("DatabaseService");
            
            // Initialize all new components
            _stateManager = new InitializationStateManager();
            _healthChecker = new DatabaseHealthChecker(_connectionString, _logger);
            _schemaValidator = new DatabaseSchemaValidator(_connectionString, _logger);
            _repairService = new DatabaseRepairService(_connectionString, _schemaValidator, _logger);
            
            // Initialize backup manager with default backup directory
            var backupDirectory = Path.Combine(
                Path.GetDirectoryName(_dbPath) ?? string.Empty, 
                "backups");
            _backupManager = new DatabaseBackupManager(_dbPath, backupDirectory, maxBackups: 5);
            
            _logger.Log($"DatabaseService initialized for database: {_dbPath}");
        }

        /// <summary>
        /// Enhanced database initialization with comprehensive lifecycle management
        /// Requirements: 1.1, 1.5, 9.1, 9.2
        /// </summary>
        /// <param name="options">Initialization options (null for defaults)</param>
        /// <returns>Initialization result with success status and details</returns>
        public async Task<InitializationResult> InitializeAsync(InitializationOptions options = null)
        {
            // Use default options if none provided
            options ??= new InitializationOptions();
            
            var startTime = DateTime.UtcNow;
            var warnings = new List<string>();
            
            try
            {
                // Check if initialization is already in progress
                if (!await _stateManager.TryBeginInitializationAsync())
                {
                    var errorMsg = "Database initialization is already in progress";
                    _logger.LogWarning(errorMsg);
                    return new InitializationResult
                    {
                        Success = false,
                        FinalState = InitializationState.InProgress,
                        ErrorMessage = errorMsg,
                        Duration = DateTime.UtcNow - startTime
                    };
                }
                
                _logger.Log("Starting database initialization");
                
                // Step 1: Create directory with error handling
                // Requirements: 1.4, 6.2
                _stateManager.UpdateProgress(InitializationStage.DirectoryCreation, "Creating database directory");
                try
                {
                    var directory = Path.GetDirectoryName(_dbPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        _logger.Log($"Creating database directory: {directory}");
                        Directory.CreateDirectory(directory);
                        _logger.Log("Database directory created successfully");
                    }
                    else
                    {
                        _logger.Log("Database directory already exists");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    var errorMsg = $"Permission denied when creating database directory. Please ensure the application has write permissions to: {Path.GetDirectoryName(_dbPath)}";
                    _logger.LogError(errorMsg);
                    _stateManager.CompleteInitialization(false);
                    throw new DatabaseInitializationException(errorMsg, InitializationStage.DirectoryCreation, ex);
                }
                catch (IOException ex)
                {
                    var errorMsg = $"I/O error when creating database directory: {ex.Message}";
                    _logger.LogError(errorMsg);
                    _stateManager.CompleteInitialization(false);
                    throw new DatabaseInitializationException(errorMsg, InitializationStage.DirectoryCreation, ex);
                }
                
                // Step 2: Test database connection with retry logic
                // Requirements: 6.1
                _stateManager.UpdateProgress(InitializationStage.ConnectionTest, "Testing database connection");
                await ExecuteWithRetryAsync(async () =>
                {
                    using var testConn = new SqliteConnection(_connectionString);
                    await testConn.OpenAsync();
                    var cmd = testConn.CreateCommand();
                    cmd.CommandText = "SELECT 1";
                    await cmd.ExecuteScalarAsync();
                    _logger.Log("Database connection test successful");
                    return true;
                }, options.ConnectionRetryCount, options.ConnectionRetryDelay);
                
                // Step 3: Check if database exists and perform health check if requested
                // Requirements: 1.2, 4.1, 5.4, 6.3
                bool databaseExists = File.Exists(_dbPath);
                bool isNewDatabase = !databaseExists;
                
                if (databaseExists && options.PerformHealthCheck)
                {
                    _stateManager.UpdateProgress(InitializationStage.HealthCheck, "Performing database health check");
                    _logger.Log("Performing health check on existing database");
                    
                    var healthReport = await _healthChecker.CheckHealthAsync();
                    
                    if (healthReport.OverallStatus == HealthStatus.Critical)
                    {
                        _logger.LogError("Database health check detected critical issues");
                        
                        // Check if database is corrupted
                        bool isCorrupted = healthReport.Issues.Exists(i => 
                            i.Category == "Integrity" && i.Severity == IssueSeverity.Critical);
                        
                        if (isCorrupted)
                        {
                            _logger.LogError("Database corruption detected");
                            
                            if (options.CreateBackupBeforeRepair)
                            {
                                try
                                {
                                    _logger.Log("Creating backup before attempting repair");
                                    var backupPath = await _backupManager.CreateBackupAsync();
                                    _logger.Log($"Backup created at: {backupPath}");
                                    warnings.Add($"Database corruption detected. Backup created at: {backupPath}");
                                }
                                catch (Exception backupEx)
                                {
                                    _logger.LogWarning($"Failed to create backup: {backupEx.Message}");
                                    warnings.Add($"Failed to create backup before repair: {backupEx.Message}");
                                }
                            }
                            
                            // For corrupted databases, we'll need to rebuild
                            warnings.Add("Database corruption detected. Database will be rebuilt.");
                            isNewDatabase = true; // Treat as new database
                        }
                    }
                    else if (healthReport.OverallStatus == HealthStatus.Warning)
                    {
                        _logger.LogWarning("Database health check detected warnings");
                        foreach (var issue in healthReport.Issues)
                        {
                            warnings.Add($"{issue.Category}: {issue.Description}");
                        }
                    }
                    else
                    {
                        _logger.Log("Database health check passed");
                    }
                }
                
                // Step 4: Validate schema and repair if needed (for existing databases)
                // Requirements: 1.3, 2.1, 3.1, 3.2, 3.3
                if (!isNewDatabase && options.AutoRepair)
                {
                    _stateManager.UpdateProgress(InitializationStage.SchemaValidation, "Validating database schema");
                    _logger.Log("Validating database schema");
                    
                    var validationResult = await _schemaValidator.ValidateSchemaAsync();
                    
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Database schema validation failed - repair needed");
                        
                        if (options.CreateBackupBeforeRepair)
                        {
                            try
                            {
                                _logger.Log("Creating backup before schema repair");
                                var backupPath = await _backupManager.CreateBackupAsync();
                                _logger.Log($"Backup created at: {backupPath}");
                                warnings.Add($"Schema issues detected. Backup created at: {backupPath}");
                            }
                            catch (Exception backupEx)
                            {
                                _logger.LogWarning($"Failed to create backup: {backupEx.Message}");
                                warnings.Add($"Failed to create backup before repair: {backupEx.Message}");
                            }
                        }
                        
                        _stateManager.UpdateProgress(InitializationStage.SchemaRepair, "Repairing database schema");
                        _logger.Log("Attempting to repair database schema");
                        
                        var repairOptions = new RepairOptions
                        {
                            CreateMissingTables = true,
                            AddMissingColumns = true,
                            CreateMissingIndexes = true,
                            BackupBeforeRepair = false // Already created backup above
                        };
                        
                        var repairResult = await _repairService.RepairSchemaAsync(validationResult, repairOptions);
                        
                        if (repairResult.Success)
                        {
                            _logger.Log("Database schema repaired successfully");
                            foreach (var action in repairResult.ActionsPerformed)
                            {
                                warnings.Add($"Repaired: {action.Description}");
                            }
                            
                            // Verify repair was successful
                            var verificationResult = await _schemaValidator.ValidateSchemaAsync();
                            if (!verificationResult.IsValid)
                            {
                                _logger.LogError("Schema validation failed after repair");
                                warnings.Add("Warning: Some schema issues remain after repair");
                            }
                        }
                        else
                        {
                            _logger.LogError($"Database schema repair failed: {repairResult.ErrorMessage}");
                            warnings.Add($"Schema repair failed: {repairResult.ErrorMessage}");
                            
                            // Continue anyway - we'll try to work with what we have
                        }
                    }
                    else
                    {
                        _logger.Log("Database schema validation passed");
                    }
                }
                
                // Step 5: Create tables and indexes for new databases
                // Requirements: 1.1
                if (isNewDatabase)
                {
                    using var conn = new SqliteConnection(_connectionString);
                    await conn.OpenAsync();
                    
                    _stateManager.UpdateProgress(InitializationStage.TableCreation, "Creating database tables");
                    _logger.Log("Creating database tables");
                    
                    // Create version table first
                    await CreateVersionTableAsync(conn);
                    
                    // Create all tables in a transaction for atomicity
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            await CreateAllTablesAsync(conn);
                            await transaction.CommitAsync();
                            _logger.Log("Database tables created successfully");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError($"Failed to create tables: {ex.Message}");
                            throw new DatabaseInitializationException(
                                "Failed to create database tables",
                                InitializationStage.TableCreation,
                                ex);
                        }
                    }
                    
                    _stateManager.UpdateProgress(InitializationStage.IndexCreation, "Creating database indexes");
                    _logger.Log("Creating database indexes");
                    
                    // Create indexes in a transaction
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            await CreateIndexesAsync(conn);
                            await transaction.CommitAsync();
                            _logger.Log("Database indexes created successfully");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError($"Failed to create indexes: {ex.Message}");
                            // Don't throw - indexes are not critical for basic functionality
                            warnings.Add($"Failed to create some indexes: {ex.Message}");
                        }
                    }
                    
                    // Set database version
                    await SetDatabaseVersionAsync(conn, CurrentVersion);
                    _logger.Log($"Database version set to {CurrentVersion}");
                }
                else
                {
                    // For existing databases, check version and migrate if needed
                    using var conn = new SqliteConnection(_connectionString);
                    await conn.OpenAsync();
                    
                    var currentVersion = await GetDatabaseVersionAsync(conn);
                    
                    if (currentVersion < CurrentVersion)
                    {
                        _stateManager.UpdateProgress(InitializationStage.Migration, "Migrating database");
                        _logger.Log($"Migrating database from version {currentVersion} to {CurrentVersion}");
                        
                        await MigrateDatabaseAsync(conn, currentVersion, CurrentVersion);
                        _logger.Log("Database migration completed successfully");
                    }
                }
                
                // Step 6: Finalization
                // Requirements: 1.4, 9.5
                _stateManager.UpdateProgress(InitializationStage.Finalization, "Finalizing initialization");
                _logger.Log("Database initialization completed successfully");
                
                // Update state manager with final state
                _stateManager.CompleteInitialization(true);
                
                // Calculate duration and return result
                var duration = DateTime.UtcNow - startTime;
                _logger.Log($"Initialization completed in {duration.TotalMilliseconds:F2}ms");
                
                if (warnings.Count > 0)
                {
                    _logger.LogWarning($"Initialization completed with {warnings.Count} warning(s)");
                }
                
                return new InitializationResult
                {
                    Success = true,
                    FinalState = InitializationState.Completed,
                    Duration = duration,
                    Warnings = warnings
                };
            }
            catch (DatabaseInitializationException dbEx)
            {
                // Handle initialization exceptions with stage information
                _logger.LogError($"Database initialization failed at stage {dbEx.FailedStage}: {dbEx.Message}");
                _stateManager.CompleteInitialization(false);
                
                return new InitializationResult
                {
                    Success = false,
                    FinalState = InitializationState.Failed,
                    FailedStage = dbEx.FailedStage,
                    ErrorMessage = dbEx.Message,
                    Duration = DateTime.UtcNow - startTime,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database initialization failed: {ex.Message}");
                _stateManager.CompleteInitialization(false);
                
                return new InitializationResult
                {
                    Success = false,
                    FinalState = InitializationState.Failed,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - startTime,
                    Warnings = warnings
                };
            }
        }

        /// <summary>
        /// Executes an operation with retry logic and exponential backoff
        /// Requirements: 6.1
        /// </summary>
        private async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            TimeSpan? initialDelay = null)
        {
            var delay = initialDelay ?? TimeSpan.FromSeconds(1);
            Exception lastException = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
                {
                    lastException = ex;
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogWarning(
                            $"Database locked, retrying in {delay.TotalSeconds}s (attempt {attempt + 1}/{maxRetries})");
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                    }
                }
            }

            var errorMsg = $"Failed to connect to database after {maxRetries} attempts";
            _logger.LogError(errorMsg);
            throw new DatabaseInitializationException(
                errorMsg,
                InitializationStage.ConnectionTest,
                lastException);
        }

        /// <summary>
        /// 初始化数据库：创建数据库文件、表结构和索引
        /// Maintained for backward compatibility - calls new InitializeAsync with default options
        /// Requirements: 1.1
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.Log("InitializeAsync() called (backward compatibility mode)");
            
            // Call new InitializeAsync with default options
            var result = await InitializeAsync(new InitializationOptions());
            
            // Throw exception on failure for backward compatibility
            if (!result.Success)
            {
                var stage = result.FailedStage ?? InitializationStage.DirectoryCreation;
                throw new DatabaseInitializationException(
                    result.ErrorMessage,
                    stage);
            }
            
            // Log warnings if any
            if (result.Warnings.Count > 0)
            {
                foreach (var warning in result.Warnings)
                {
                    _logger.LogWarning(warning);
                }
            }
        }

        /// <summary>
        /// 创建版本管理表
        /// </summary>
        private async Task CreateVersionTableAsync(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS DatabaseVersion (
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    Version INTEGER NOT NULL,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 获取数据库版本
        /// </summary>
        private async Task<int> GetDatabaseVersionAsync(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Version FROM DatabaseVersion WHERE Id = 1";
            
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        /// <summary>
        /// 设置数据库版本
        /// </summary>
        private async Task SetDatabaseVersionAsync(SqliteConnection conn, int version)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT OR REPLACE INTO DatabaseVersion (Id, Version, UpdatedAt) 
VALUES (1, @version, @updatedAt)";
            cmd.Parameters.AddWithValue("@version", version);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 创建所有数据库表
        /// </summary>
        private async Task CreateAllTablesAsync(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
-- 人员表
CREATE TABLE IF NOT EXISTS Personals (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Position TEXT NOT NULL DEFAULT '',
    SkillIds TEXT NOT NULL DEFAULT '[]', -- JSON array of ints
    IsAvailable INTEGER NOT NULL DEFAULT 1, -- SQLite boolean (0/1)
    IsRetired INTEGER NOT NULL DEFAULT 0,
    RecentShiftInterval INTEGER NOT NULL DEFAULT 0,
    RecentHolidayShiftInterval INTEGER NOT NULL DEFAULT 0,
    RecentTimeSlotIntervals TEXT NOT NULL DEFAULT '[0,0,0,0,0,0,0,0,0,0,0,0]', -- JSON array of 12 ints
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 哨位表
CREATE TABLE IF NOT EXISTS Positions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Location TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Requirements TEXT NOT NULL DEFAULT '',
    RequiredSkillIds TEXT NOT NULL DEFAULT '[]', -- JSON array of ints
    AvailablePersonnelIds TEXT NOT NULL DEFAULT '[]', -- JSON array of ints
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 技能表
CREATE TABLE IF NOT EXISTS Skills (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 排班表
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

-- 单次排班表
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
    FOREIGN KEY (ScheduleId) REFERENCES Schedules(Id) ON DELETE CASCADE,
    FOREIGN KEY (PositionId) REFERENCES Positions(Id),
    FOREIGN KEY (PersonnelId) REFERENCES Personals(Id)
);

-- 排班模板表
CREATE TABLE IF NOT EXISTS SchedulingTemplates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT DEFAULT '',
    TemplateType TEXT NOT NULL DEFAULT 'regular',
    IsDefault INTEGER NOT NULL DEFAULT 0,
    PersonnelIds TEXT NOT NULL DEFAULT '[]',
    PositionIds TEXT NOT NULL DEFAULT '[]',
    HolidayConfigId INTEGER,
    UseActiveHolidayConfig INTEGER NOT NULL DEFAULT 0,
    EnabledFixedRuleIds TEXT NOT NULL DEFAULT '[]',
    EnabledManualAssignmentIds TEXT NOT NULL DEFAULT '[]',
    DurationDays INTEGER NOT NULL DEFAULT 1,
    StrategyConfig TEXT NOT NULL DEFAULT '{}',
    UsageCount INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastUsedAt TEXT
);

-- 定岗规则表
CREATE TABLE IF NOT EXISTS FixedPositionRules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PersonalId INTEGER NOT NULL,
    AllowedPositionIds TEXT NOT NULL DEFAULT '[]', -- JSON array of ints
    AllowedPeriods TEXT NOT NULL DEFAULT '[]', -- JSON array of ints (0-11)
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    Description TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (PersonalId) REFERENCES Personals(Id) ON DELETE CASCADE
);

-- 休息日配置表
CREATE TABLE IF NOT EXISTS HolidayConfigs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ConfigName TEXT NOT NULL,
    EnableWeekendRule INTEGER NOT NULL DEFAULT 1,
    WeekendDays TEXT NOT NULL DEFAULT '[6,0]', -- JSON array of DayOfWeek values
    LegalHolidays TEXT NOT NULL DEFAULT '[]', -- JSON array of ISO dates
    CustomHolidays TEXT NOT NULL DEFAULT '[]', -- JSON array of ISO dates
    ExcludedDates TEXT NOT NULL DEFAULT '[]', -- JSON array of ISO dates
    IsActive INTEGER NOT NULL DEFAULT 1
);

-- 手动指定表
CREATE TABLE IF NOT EXISTS ManualAssignments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PositionId INTEGER NOT NULL,
    PeriodIndex INTEGER NOT NULL CHECK (PeriodIndex >= 0 AND PeriodIndex <= 11),
    PersonalId INTEGER NOT NULL,
    Date TEXT NOT NULL, -- ISO 8601 date
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    Remarks TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (PositionId) REFERENCES Positions(Id),
    FOREIGN KEY (PersonalId) REFERENCES Personals(Id)
);
";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 创建数据库索引
        /// Enhanced to check for existing indexes and only create missing ones
        /// Requirements: 8.1, 8.2, 8.4
        /// </summary>
        private async Task CreateIndexesAsync(SqliteConnection conn, bool checkExisting = false)
        {
            var indexDefinitions = DatabaseSchema.GetIndexDefinitions();
            
            if (checkExisting)
            {
                // Get existing indexes
                var existingIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT name 
                    FROM sqlite_master 
                    WHERE type = 'index' 
                    AND name NOT LIKE 'sqlite_%'";
                
                using (var reader = await checkCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingIndexes.Add(reader.GetString(0));
                    }
                }
                
                _logger?.Log($"Found {existingIndexes.Count} existing indexes");
                
                // Only create missing indexes
                foreach (var indexDef in indexDefinitions)
                {
                    if (!existingIndexes.Contains(indexDef.Key))
                    {
                        try
                        {
                            var cmd = conn.CreateCommand();
                            cmd.CommandText = indexDef.Value;
                            await cmd.ExecuteNonQueryAsync();
                            _logger?.Log($"Created index: {indexDef.Key}");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning($"Failed to create index {indexDef.Key}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                // Create all indexes (for new databases)
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
-- 人员表索引
CREATE INDEX IF NOT EXISTS idx_personals_name ON Personals(Name);
CREATE INDEX IF NOT EXISTS idx_personals_available ON Personals(IsAvailable);
CREATE INDEX IF NOT EXISTS idx_personals_retired ON Personals(IsRetired);

-- 哨位表索引
CREATE INDEX IF NOT EXISTS idx_positions_name ON Positions(Name);
CREATE INDEX IF NOT EXISTS idx_positions_active ON Positions(IsActive);

-- 技能表索引
CREATE INDEX IF NOT EXISTS idx_skills_name ON Skills(Name);
CREATE INDEX IF NOT EXISTS idx_skills_active ON Skills(IsActive);

-- 排班表索引
CREATE INDEX IF NOT EXISTS idx_schedules_confirmed ON Schedules(IsConfirmed);
CREATE INDEX IF NOT EXISTS idx_schedules_date_range ON Schedules(StartDate, EndDate);
CREATE INDEX IF NOT EXISTS idx_schedules_created ON Schedules(CreatedAt);

-- 单次排班表索引
CREATE INDEX IF NOT EXISTS idx_shifts_schedule ON SingleShifts(ScheduleId);
CREATE INDEX IF NOT EXISTS idx_shifts_position ON SingleShifts(PositionId);
CREATE INDEX IF NOT EXISTS idx_shifts_personnel ON SingleShifts(PersonnelId);
CREATE INDEX IF NOT EXISTS idx_shifts_time ON SingleShifts(StartTime);
CREATE INDEX IF NOT EXISTS idx_shifts_day_slot ON SingleShifts(DayIndex, TimeSlotIndex);

-- 排班模板表索引
CREATE INDEX IF NOT EXISTS idx_templates_active ON SchedulingTemplates(IsActive);
CREATE INDEX IF NOT EXISTS idx_templates_usage ON SchedulingTemplates(UsageCount);
CREATE INDEX IF NOT EXISTS idx_templates_name ON SchedulingTemplates(Name);
CREATE INDEX IF NOT EXISTS idx_templates_type ON SchedulingTemplates(TemplateType);
CREATE INDEX IF NOT EXISTS idx_templates_default ON SchedulingTemplates(IsDefault);

-- 约束表索引
CREATE INDEX IF NOT EXISTS idx_fixed_rules_personal ON FixedPositionRules(PersonalId);
CREATE INDEX IF NOT EXISTS idx_fixed_rules_enabled ON FixedPositionRules(IsEnabled);
CREATE INDEX IF NOT EXISTS idx_holiday_configs_active ON HolidayConfigs(IsActive);
CREATE INDEX IF NOT EXISTS idx_manual_assignments_position ON ManualAssignments(PositionId);
CREATE INDEX IF NOT EXISTS idx_manual_assignments_personnel ON ManualAssignments(PersonalId);
CREATE INDEX IF NOT EXISTS idx_manual_assignments_date ON ManualAssignments(Date);
CREATE INDEX IF NOT EXISTS idx_manual_assignments_enabled ON ManualAssignments(IsEnabled);
";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// 数据库迁移
        /// </summary>
        private async Task MigrateDatabaseAsync(SqliteConnection conn, int fromVersion, int toVersion)
        {
            for (int version = fromVersion + 1; version <= toVersion; version++)
            {
                await MigrateToVersionAsync(conn, version);
                await SetDatabaseVersionAsync(conn, version);
            }
        }

        /// <summary>
        /// 迁移到指定版本
        /// </summary>
        private async Task MigrateToVersionAsync(SqliteConnection conn, int version)
        {
            switch (version)
            {
                case 1:
                    // 初始版本，无需迁移
                    break;
                // 未来版本的迁移逻辑在这里添加
                default:
                    throw new NotSupportedException($"Migration to version {version} is not supported.");
            }
        }

        /// <summary>
        /// 检查数据库连接
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();
                
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync();
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取数据库信息
        /// </summary>
        public async Task<DatabaseInfo> GetDatabaseInfoAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var version = await GetDatabaseVersionAsync(conn);
            var fileInfo = new FileInfo(_dbPath);
            
            return new DatabaseInfo
            {
                Version = version,
                FilePath = _dbPath,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue
            };
        }

        /// <summary>
        /// 初始化所有仓储
        /// </summary>
        public async Task InitializeRepositoriesAsync(
            IPersonalRepository personalRepo,
            IPositionRepository positionRepo,
            ISkillRepository skillRepo,
            ISchedulingRepository schedulingRepo,
            ITemplateRepository templateRepo,
            IConstraintRepository constraintRepo)
        {
            await personalRepo.InitAsync();
            await positionRepo.InitAsync();
            await skillRepo.InitAsync();
            await schedulingRepo.InitAsync();
            await templateRepo.InitAsync();
            await constraintRepo.InitAsync();
        }
    }

    /// <summary>
    /// 数据库信息
    /// </summary>
    public class DatabaseInfo
    {
        public int Version { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
    }
}