using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AutoScheduling3.Data.Interfaces;

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

        public DatabaseService(string dbPath)
        {
            _dbPath = dbPath;
            _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        }

        /// <summary>
        /// 初始化数据库：创建数据库文件、表结构和索引
        /// </summary>
        public async Task InitializeAsync()
        {
            // 确保数据库目录存在
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // 创建版本管理表
            await CreateVersionTableAsync(conn);

            // 检查当前版本
            var currentVersion = await GetDatabaseVersionAsync(conn);
            
            if (currentVersion == 0)
            {
                // 全新数据库，创建所有表
                await CreateAllTablesAsync(conn);
                await SetDatabaseVersionAsync(conn, CurrentVersion);
            }
            else if (currentVersion < CurrentVersion)
            {
                // 需要迁移
                await MigrateDatabaseAsync(conn, currentVersion, CurrentVersion);
            }

            // 创建索引
            await CreateIndexesAsync(conn);
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
    PersonnelIds TEXT NOT NULL DEFAULT '[]',
    PositionIds TEXT NOT NULL DEFAULT '[]',
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
        /// </summary>
        private async Task CreateIndexesAsync(SqliteConnection conn)
        {
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