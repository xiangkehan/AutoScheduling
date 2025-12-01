using System.Collections.Generic;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// DatabaseSchema: Defines the expected schema for all database tables
    /// This class provides a centralized definition of table structures, columns, and indexes
    /// for validation and repair operations.
    /// Requirements: 2.2, 2.3, 2.4
    /// </summary>
    public static class DatabaseSchema
    {
        /// <summary>
        /// Gets the complete expected schema for all tables in the database
        /// </summary>
        public static Dictionary<string, TableSchema> GetExpectedSchema()
        {
            return new Dictionary<string, TableSchema>
            {
                ["DatabaseVersion"] = new TableSchema
                {
                    Name = "DatabaseVersion",
                    IsCritical = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true },
                        new() { Name = "Version", DataType = "INTEGER", IsNullable = false },
                        new() { Name = "UpdatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" }
                    },
                    Indexes = new List<string>()
                },

                ["Personals"] = new TableSchema
                {
                    Name = "Personals",
                    IsCritical = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "Name", DataType = "TEXT", IsNullable = false },
                        new() { Name = "Position", DataType = "TEXT", IsNullable = false, DefaultValue = "''" },
                        new() { Name = "SkillIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "IsAvailable", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "IsRetired", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "RecentShiftInterval", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "RecentHolidayShiftInterval", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "RecentTimeSlotIntervals", DataType = "TEXT", IsNullable = false, DefaultValue = "'[0,0,0,0,0,0,0,0,0,0,0,0]'" },
                        new() { Name = "CreatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" },
                        new() { Name = "UpdatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_personals_name",
                        "idx_personals_available",
                        "idx_personals_retired"
                    }
                },

                ["Positions"] = new TableSchema
                {
                    Name = "Positions",
                    IsCritical = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "Name", DataType = "TEXT", IsNullable = false },
                        new() { Name = "Location", DataType = "TEXT", IsNullable = false, DefaultValue = "''" },
                        new() { Name = "Description", DataType = "TEXT", IsNullable = false, DefaultValue = "''" },
                        new() { Name = "Requirements", DataType = "TEXT", IsNullable = false, DefaultValue = "''" },
                        new() { Name = "RequiredSkillIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "AvailablePersonnelIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "IsActive", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "CreatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" },
                        new() { Name = "UpdatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_positions_name",
                        "idx_positions_active"
                    }
                },

                ["Skills"] = new TableSchema
                {
                    Name = "Skills",
                    IsCritical = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "Name", DataType = "TEXT", IsNullable = false },
                        new() { Name = "Description", DataType = "TEXT", IsNullable = false, DefaultValue = "''" },
                        new() { Name = "IsActive", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "CreatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" },
                        new() { Name = "UpdatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_skills_name",
                        "idx_skills_active"
                    }
                },

                ["Schedules"] = new TableSchema
                {
                    Name = "Schedules",
                    IsCritical = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "Header", DataType = "TEXT", IsNullable = false },
                        new() { Name = "PersonnelIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "PositionIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "StartDate", DataType = "TEXT", IsNullable = false },
                        new() { Name = "EndDate", DataType = "TEXT", IsNullable = false },
                        new() { Name = "IsConfirmed", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "HolidayConfigId", DataType = "INTEGER", IsNullable = true },
                        new() { Name = "UseActiveHolidayConfig", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "EnabledFixedRuleIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "EnabledManualAssignmentIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "IsPartialResult", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "ProgressPercentage", DataType = "REAL", IsNullable = true },
                        new() { Name = "CurrentStage", DataType = "TEXT", IsNullable = true },
                        new() { Name = "SchedulingMode", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "CreatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" },
                        new() { Name = "UpdatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_schedules_confirmed",
                        "idx_schedules_date_range",
                        "idx_schedules_created"
                    }
                },

                ["SingleShifts"] = new TableSchema
                {
                    Name = "SingleShifts",
                    IsCritical = true,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "ScheduleId", DataType = "INTEGER", IsNullable = false, ForeignKey = "Schedules(Id)" },
                        new() { Name = "PositionId", DataType = "INTEGER", IsNullable = false, ForeignKey = "Positions(Id)" },
                        new() { Name = "PersonnelId", DataType = "INTEGER", IsNullable = false, ForeignKey = "Personals(Id)" },
                        new() { Name = "StartTime", DataType = "TEXT", IsNullable = false },
                        new() { Name = "EndTime", DataType = "TEXT", IsNullable = false },
                        new() { Name = "DayIndex", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "TimeSlotIndex", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "IsNightShift", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_shifts_schedule",
                        "idx_shifts_position",
                        "idx_shifts_personnel",
                        "idx_shifts_time",
                        "idx_shifts_day_slot"
                    }
                },

                ["SchedulingTemplates"] = new TableSchema
                {
                    Name = "SchedulingTemplates",
                    IsCritical = false,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "Name", DataType = "TEXT", IsNullable = false },
                        new() { Name = "Description", DataType = "TEXT", IsNullable = true, DefaultValue = "''" },
                        new() { Name = "TemplateType", DataType = "TEXT", IsNullable = false, DefaultValue = "'regular'" },
                        new() { Name = "IsDefault", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "PersonnelIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "PositionIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "HolidayConfigId", DataType = "INTEGER", IsNullable = true },
                        new() { Name = "UseActiveHolidayConfig", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "EnabledFixedRuleIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "EnabledManualAssignmentIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "DurationDays", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "StrategyConfig", DataType = "TEXT", IsNullable = false, DefaultValue = "'{}'" },
                        new() { Name = "UsageCount", DataType = "INTEGER", IsNullable = false, DefaultValue = "0" },
                        new() { Name = "IsActive", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "CreatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" },
                        new() { Name = "UpdatedAt", DataType = "TEXT", IsNullable = false, DefaultValue = "CURRENT_TIMESTAMP" },
                        new() { Name = "LastUsedAt", DataType = "TEXT", IsNullable = true }
                    },
                    Indexes = new List<string>
                    {
                        "idx_templates_active",
                        "idx_templates_usage",
                        "idx_templates_name",
                        "idx_templates_type",
                        "idx_templates_default"
                    }
                },

                ["FixedPositionRules"] = new TableSchema
                {
                    Name = "FixedPositionRules",
                    IsCritical = false,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "PersonalId", DataType = "INTEGER", IsNullable = false, ForeignKey = "Personals(Id)" },
                        new() { Name = "AllowedPositionIds", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "AllowedPeriods", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "IsEnabled", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "Description", DataType = "TEXT", IsNullable = false, DefaultValue = "''" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_fixed_rules_personal",
                        "idx_fixed_rules_enabled"
                    }
                },

                ["HolidayConfigs"] = new TableSchema
                {
                    Name = "HolidayConfigs",
                    IsCritical = false,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "ConfigName", DataType = "TEXT", IsNullable = false },
                        new() { Name = "EnableWeekendRule", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "WeekendDays", DataType = "TEXT", IsNullable = false, DefaultValue = "'[6,0]'" },
                        new() { Name = "LegalHolidays", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "CustomHolidays", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "ExcludedDates", DataType = "TEXT", IsNullable = false, DefaultValue = "'[]'" },
                        new() { Name = "IsActive", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_holiday_configs_active"
                    }
                },

                ["ManualAssignments"] = new TableSchema
                {
                    Name = "ManualAssignments",
                    IsCritical = false,
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", DataType = "INTEGER", IsNullable = false, IsPrimaryKey = true, IsAutoIncrement = true },
                        new() { Name = "PositionId", DataType = "INTEGER", IsNullable = false, ForeignKey = "Positions(Id)" },
                        new() { Name = "PeriodIndex", DataType = "INTEGER", IsNullable = false },
                        new() { Name = "PersonalId", DataType = "INTEGER", IsNullable = false, ForeignKey = "Personals(Id)" },
                        new() { Name = "Date", DataType = "TEXT", IsNullable = false },
                        new() { Name = "IsEnabled", DataType = "INTEGER", IsNullable = false, DefaultValue = "1" },
                        new() { Name = "Remarks", DataType = "TEXT", IsNullable = false, DefaultValue = "''" }
                    },
                    Indexes = new List<string>
                    {
                        "idx_manual_assignments_position",
                        "idx_manual_assignments_personnel",
                        "idx_manual_assignments_date",
                        "idx_manual_assignments_enabled"
                    }
                }
            };
        }

        /// <summary>
        /// Gets the SQL statement to create a specific table
        /// </summary>
        public static string GetCreateTableSql(string tableName)
        {
            var schema = GetExpectedSchema();
            if (!schema.ContainsKey(tableName))
            {
                throw new System.ArgumentException($"Table '{tableName}' not found in schema definition");
            }

            return schema[tableName].GenerateCreateTableSql();
        }

        /// <summary>
        /// Gets the SQL statement to create a specific index
        /// </summary>
        public static string GetCreateIndexSql(string indexName)
        {
            var indexDefinitions = GetIndexDefinitions();
            if (!indexDefinitions.ContainsKey(indexName))
            {
                throw new System.ArgumentException($"Index '{indexName}' not found in schema definition");
            }

            return indexDefinitions[indexName];
        }

        /// <summary>
        /// Gets all index definitions with their SQL statements
        /// </summary>
        public static Dictionary<string, string> GetIndexDefinitions()
        {
            return new Dictionary<string, string>
            {
                // Personals indexes
                ["idx_personals_name"] = "CREATE INDEX IF NOT EXISTS idx_personals_name ON Personals(Name)",
                ["idx_personals_available"] = "CREATE INDEX IF NOT EXISTS idx_personals_available ON Personals(IsAvailable)",
                ["idx_personals_retired"] = "CREATE INDEX IF NOT EXISTS idx_personals_retired ON Personals(IsRetired)",

                // Positions indexes
                ["idx_positions_name"] = "CREATE INDEX IF NOT EXISTS idx_positions_name ON Positions(Name)",
                ["idx_positions_active"] = "CREATE INDEX IF NOT EXISTS idx_positions_active ON Positions(IsActive)",

                // Skills indexes
                ["idx_skills_name"] = "CREATE INDEX IF NOT EXISTS idx_skills_name ON Skills(Name)",
                ["idx_skills_active"] = "CREATE INDEX IF NOT EXISTS idx_skills_active ON Skills(IsActive)",

                // Schedules indexes
                ["idx_schedules_confirmed"] = "CREATE INDEX IF NOT EXISTS idx_schedules_confirmed ON Schedules(IsConfirmed)",
                ["idx_schedules_date_range"] = "CREATE INDEX IF NOT EXISTS idx_schedules_date_range ON Schedules(StartDate, EndDate)",
                ["idx_schedules_created"] = "CREATE INDEX IF NOT EXISTS idx_schedules_created ON Schedules(CreatedAt)",

                // SingleShifts indexes
                ["idx_shifts_schedule"] = "CREATE INDEX IF NOT EXISTS idx_shifts_schedule ON SingleShifts(ScheduleId)",
                ["idx_shifts_position"] = "CREATE INDEX IF NOT EXISTS idx_shifts_position ON SingleShifts(PositionId)",
                ["idx_shifts_personnel"] = "CREATE INDEX IF NOT EXISTS idx_shifts_personnel ON SingleShifts(PersonnelId)",
                ["idx_shifts_time"] = "CREATE INDEX IF NOT EXISTS idx_shifts_time ON SingleShifts(StartTime)",
                ["idx_shifts_day_slot"] = "CREATE INDEX IF NOT EXISTS idx_shifts_day_slot ON SingleShifts(DayIndex, TimeSlotIndex)",

                // SchedulingTemplates indexes
                ["idx_templates_active"] = "CREATE INDEX IF NOT EXISTS idx_templates_active ON SchedulingTemplates(IsActive)",
                ["idx_templates_usage"] = "CREATE INDEX IF NOT EXISTS idx_templates_usage ON SchedulingTemplates(UsageCount)",
                ["idx_templates_name"] = "CREATE INDEX IF NOT EXISTS idx_templates_name ON SchedulingTemplates(Name)",
                ["idx_templates_type"] = "CREATE INDEX IF NOT EXISTS idx_templates_type ON SchedulingTemplates(TemplateType)",
                ["idx_templates_default"] = "CREATE INDEX IF NOT EXISTS idx_templates_default ON SchedulingTemplates(IsDefault)",

                // FixedPositionRules indexes
                ["idx_fixed_rules_personal"] = "CREATE INDEX IF NOT EXISTS idx_fixed_rules_personal ON FixedPositionRules(PersonalId)",
                ["idx_fixed_rules_enabled"] = "CREATE INDEX IF NOT EXISTS idx_fixed_rules_enabled ON FixedPositionRules(IsEnabled)",

                // HolidayConfigs indexes
                ["idx_holiday_configs_active"] = "CREATE INDEX IF NOT EXISTS idx_holiday_configs_active ON HolidayConfigs(IsActive)",

                // ManualAssignments indexes
                ["idx_manual_assignments_position"] = "CREATE INDEX IF NOT EXISTS idx_manual_assignments_position ON ManualAssignments(PositionId)",
                ["idx_manual_assignments_personnel"] = "CREATE INDEX IF NOT EXISTS idx_manual_assignments_personnel ON ManualAssignments(PersonalId)",
                ["idx_manual_assignments_date"] = "CREATE INDEX IF NOT EXISTS idx_manual_assignments_date ON ManualAssignments(Date)",
                ["idx_manual_assignments_enabled"] = "CREATE INDEX IF NOT EXISTS idx_manual_assignments_enabled ON ManualAssignments(IsEnabled)"
            };
        }
    }

    /// <summary>
    /// Represents the schema definition for a database table
    /// </summary>
    public class TableSchema
    {
        /// <summary>
        /// Table name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Column definitions
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();

        /// <summary>
        /// Index names for this table
        /// </summary>
        public List<string> Indexes { get; set; } = new List<string>();

        /// <summary>
        /// Whether this table is critical for application functionality
        /// </summary>
        public bool IsCritical { get; set; } = true;

        /// <summary>
        /// Generates the CREATE TABLE SQL statement for this table
        /// </summary>
        public string GenerateCreateTableSql()
        {
            var sql = new System.Text.StringBuilder();
            sql.AppendLine($"CREATE TABLE IF NOT EXISTS {Name} (");

            var columnDefs = new List<string>();
            var foreignKeys = new List<string>();

            foreach (var column in Columns)
            {
                var colDef = new System.Text.StringBuilder();
                colDef.Append($"    {column.Name} {column.DataType}");

                if (column.IsPrimaryKey)
                {
                    colDef.Append(" PRIMARY KEY");
                    if (column.IsAutoIncrement)
                    {
                        colDef.Append(" AUTOINCREMENT");
                    }
                }

                if (!column.IsNullable && !column.IsPrimaryKey)
                {
                    colDef.Append(" NOT NULL");
                }

                if (!string.IsNullOrEmpty(column.DefaultValue))
                {
                    colDef.Append($" DEFAULT {column.DefaultValue}");
                }

                columnDefs.Add(colDef.ToString());

                // Collect foreign key constraints
                if (!string.IsNullOrEmpty(column.ForeignKey))
                {
                    foreignKeys.Add($"    FOREIGN KEY ({column.Name}) REFERENCES {column.ForeignKey}");
                }
            }

            sql.Append(string.Join(",\n", columnDefs));

            if (foreignKeys.Count > 0)
            {
                sql.Append(",\n");
                sql.Append(string.Join(",\n", foreignKeys));
            }

            sql.AppendLine();
            sql.Append(")");

            return sql.ToString();
        }
    }

    /// <summary>
    /// Represents a column definition in a table schema
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Column name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// SQLite data type (INTEGER, TEXT, REAL, BLOB)
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the column can contain NULL values
        /// </summary>
        public bool IsNullable { get; set; } = true;

        /// <summary>
        /// Default value expression (e.g., "0", "'[]'", "CURRENT_TIMESTAMP")
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Whether this column is the primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; } = false;

        /// <summary>
        /// Whether this column is auto-incrementing
        /// </summary>
        public bool IsAutoIncrement { get; set; } = false;

        /// <summary>
        /// Foreign key reference (e.g., "Personals(Id)")
        /// </summary>
        public string ForeignKey { get; set; } = string.Empty;
    }
}
