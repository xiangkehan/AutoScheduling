using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Data;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.TestData
{
    /// <summary>
    /// Verification tests for ConstraintRepository CRUD operations
    /// Requirements: 1.1, 1.3, 1.4, 1.5
    /// </summary>
    public class ConstraintRepositoryVerification
    {
        private readonly string _testDbPath;
        private readonly DatabaseService _dbService;
        private readonly ConstraintRepository _repository;

        public ConstraintRepositoryVerification()
        {
            // Create a temporary test database
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_constraints_{Guid.NewGuid()}.db");
            _dbService = new DatabaseService(_testDbPath);
            _repository = new ConstraintRepository(_testDbPath);
        }

        /// <summary>
        /// Run all verification tests
        /// </summary>
        public async Task<VerificationResult> RunAllTestsAsync()
        {
            var result = new VerificationResult();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting Constraint Repository Verification ===");
                
                // Initialize database
                System.Diagnostics.Debug.WriteLine("Initializing test database...");
                await _dbService.InitializeAsync();
                await _repository.InitAsync();
                System.Diagnostics.Debug.WriteLine("✓ Database initialized successfully");
                result.DatabaseInitialized = true;

                // Verify schema
                System.Diagnostics.Debug.WriteLine("\nVerifying database schema...");
                var schemaValid = await VerifySchemaAsync();
                result.SchemaValid = schemaValid;
                if (schemaValid)
                {
                    System.Diagnostics.Debug.WriteLine("✓ Schema verification passed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✗ Schema verification failed");
                    result.Errors.Add("Schema verification failed");
                }

                // Test FixedPositionRule CRUD
                System.Diagnostics.Debug.WriteLine("\nTesting FixedPositionRule CRUD operations...");
                var fixedRuleResult = await TestFixedPositionRuleCRUDAsync();
                result.FixedPositionRuleTests = fixedRuleResult;
                if (fixedRuleResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✓ FixedPositionRule CRUD tests passed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ FixedPositionRule CRUD tests failed: {fixedRuleResult.ErrorMessage}");
                    result.Errors.Add($"FixedPositionRule: {fixedRuleResult.ErrorMessage}");
                }

                // Test ManualAssignment CRUD
                System.Diagnostics.Debug.WriteLine("\nTesting ManualAssignment CRUD operations...");
                var manualAssignmentResult = await TestManualAssignmentCRUDAsync();
                result.ManualAssignmentTests = manualAssignmentResult;
                if (manualAssignmentResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✓ ManualAssignment CRUD tests passed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ ManualAssignment CRUD tests failed: {manualAssignmentResult.ErrorMessage}");
                    result.Errors.Add($"ManualAssignment: {manualAssignmentResult.ErrorMessage}");
                }

                // Test HolidayConfig CRUD
                System.Diagnostics.Debug.WriteLine("\nTesting HolidayConfig CRUD operations...");
                var holidayConfigResult = await TestHolidayConfigCRUDAsync();
                result.HolidayConfigTests = holidayConfigResult;
                if (holidayConfigResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✓ HolidayConfig CRUD tests passed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ HolidayConfig CRUD tests failed: {holidayConfigResult.ErrorMessage}");
                    result.Errors.Add($"HolidayConfig: {holidayConfigResult.ErrorMessage}");
                }

                // Test indexes
                System.Diagnostics.Debug.WriteLine("\nVerifying constraint table indexes...");
                var indexesValid = await VerifyIndexesAsync();
                result.IndexesValid = indexesValid;
                if (indexesValid)
                {
                    System.Diagnostics.Debug.WriteLine("✓ Index verification passed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✗ Index verification failed");
                    result.Errors.Add("Index verification failed");
                }

                result.Success = result.Errors.Count == 0;
                
                System.Diagnostics.Debug.WriteLine("\n=== Verification Complete ===");
                System.Diagnostics.Debug.WriteLine($"Overall Result: {(result.Success ? "PASSED" : "FAILED")}");
                if (!result.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"Errors: {result.Errors.Count}");
                    foreach (var error in result.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Verification failed with exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                result.Success = false;
                result.Errors.Add($"Exception: {ex.Message}");
            }
            finally
            {
                // Cleanup
                Cleanup();
            }

            return result;
        }

        private async Task<bool> VerifySchemaAsync()
        {
            try
            {
                var diagnostics = await _dbService.GetDiagnosticsAsync();
                
                // Check that constraint tables exist
                var requiredTables = new[] { "FixedPositionRules", "HolidayConfigs", "ManualAssignments" };
                foreach (var table in requiredTables)
                {
                    if (!diagnostics.TableRowCounts.ContainsKey(table))
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ Missing table: {table}");
                        return false;
                    }
                    System.Diagnostics.Debug.WriteLine($"  ✓ Table exists: {table} (rows: {diagnostics.TableRowCounts[table]})");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ✗ Schema verification error: {ex.Message}");
                return false;
            }
        }

        private async Task<CRUDTestResult> TestFixedPositionRuleCRUDAsync()
        {
            var result = new CRUDTestResult();
            
            try
            {
                // CREATE
                var rule = new FixedPositionRule
                {
                    PersonalId = 1,
                    AllowedPositionIds = new List<int> { 1, 2, 3 },
                    AllowedPeriods = new List<int> { 0, 1, 2 },
                    IsEnabled = true,
                    Description = "Test rule"
                };
                
                var id = await _repository.AddFixedPositionRuleAsync(rule);
                if (id <= 0)
                {
                    result.ErrorMessage = "Failed to create FixedPositionRule (invalid ID)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ CREATE: FixedPositionRule created with ID {id}");
                result.CreateSuccess = true;

                // READ
                var rules = await _repository.GetAllFixedPositionRulesAsync(enabledOnly: false);
                if (rules.Count == 0)
                {
                    result.ErrorMessage = "Failed to read FixedPositionRule (no records found)";
                    return result;
                }
                var retrievedRule = rules.FirstOrDefault(r => r.Id == id);
                if (retrievedRule == null)
                {
                    result.ErrorMessage = $"Failed to read FixedPositionRule (ID {id} not found)";
                    return result;
                }
                if (retrievedRule.PersonalId != rule.PersonalId || 
                    retrievedRule.AllowedPositionIds.Count != rule.AllowedPositionIds.Count)
                {
                    result.ErrorMessage = "Failed to read FixedPositionRule (data mismatch)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ READ: FixedPositionRule retrieved successfully");
                result.ReadSuccess = true;

                // UPDATE
                retrievedRule.Description = "Updated test rule";
                retrievedRule.IsEnabled = false;
                await _repository.UpdateFixedPositionRuleAsync(retrievedRule);
                
                var updatedRule = (await _repository.GetAllFixedPositionRulesAsync(enabledOnly: false))
                    .FirstOrDefault(r => r.Id == id);
                if (updatedRule == null || updatedRule.Description != "Updated test rule" || updatedRule.IsEnabled)
                {
                    result.ErrorMessage = "Failed to update FixedPositionRule";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ UPDATE: FixedPositionRule updated successfully");
                result.UpdateSuccess = true;

                // DELETE
                await _repository.DeleteFixedPositionRuleAsync(id);
                var deletedRule = (await _repository.GetAllFixedPositionRulesAsync(enabledOnly: false))
                    .FirstOrDefault(r => r.Id == id);
                if (deletedRule != null)
                {
                    result.ErrorMessage = "Failed to delete FixedPositionRule (record still exists)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ DELETE: FixedPositionRule deleted successfully");
                result.DeleteSuccess = true;

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"  ✗ Exception: {ex.Message}");
            }

            return result;
        }

        private async Task<CRUDTestResult> TestManualAssignmentCRUDAsync()
        {
            var result = new CRUDTestResult();
            
            try
            {
                // CREATE
                var assignment = new ManualAssignment
                {
                    PositionId = 1,
                    PeriodIndex = 0,
                    PersonalId = 1,
                    Date = DateTime.Today,
                    IsEnabled = true,
                    Remarks = "Test assignment"
                };
                
                var id = await _repository.AddManualAssignmentAsync(assignment);
                if (id <= 0)
                {
                    result.ErrorMessage = "Failed to create ManualAssignment (invalid ID)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ CREATE: ManualAssignment created with ID {id}");
                result.CreateSuccess = true;

                // READ
                var assignments = await _repository.GetManualAssignmentsByDateRangeAsync(
                    DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), enabledOnly: false);
                if (assignments.Count == 0)
                {
                    result.ErrorMessage = "Failed to read ManualAssignment (no records found)";
                    return result;
                }
                var retrievedAssignment = assignments.FirstOrDefault(a => a.Id == id);
                if (retrievedAssignment == null)
                {
                    result.ErrorMessage = $"Failed to read ManualAssignment (ID {id} not found)";
                    return result;
                }
                if (retrievedAssignment.PositionId != assignment.PositionId || 
                    retrievedAssignment.PersonalId != assignment.PersonalId)
                {
                    result.ErrorMessage = "Failed to read ManualAssignment (data mismatch)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ READ: ManualAssignment retrieved successfully");
                result.ReadSuccess = true;

                // UPDATE
                retrievedAssignment.Remarks = "Updated test assignment";
                retrievedAssignment.IsEnabled = false;
                await _repository.UpdateManualAssignmentAsync(retrievedAssignment);
                
                var updatedAssignment = (await _repository.GetManualAssignmentsByDateRangeAsync(
                    DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), enabledOnly: false))
                    .FirstOrDefault(a => a.Id == id);
                if (updatedAssignment == null || updatedAssignment.Remarks != "Updated test assignment" || updatedAssignment.IsEnabled)
                {
                    result.ErrorMessage = "Failed to update ManualAssignment";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ UPDATE: ManualAssignment updated successfully");
                result.UpdateSuccess = true;

                // DELETE
                await _repository.DeleteManualAssignmentAsync(id);
                var deletedAssignment = (await _repository.GetManualAssignmentsByDateRangeAsync(
                    DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), enabledOnly: false))
                    .FirstOrDefault(a => a.Id == id);
                if (deletedAssignment != null)
                {
                    result.ErrorMessage = "Failed to delete ManualAssignment (record still exists)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ DELETE: ManualAssignment deleted successfully");
                result.DeleteSuccess = true;

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"  ✗ Exception: {ex.Message}");
            }

            return result;
        }

        private async Task<CRUDTestResult> TestHolidayConfigCRUDAsync()
        {
            var result = new CRUDTestResult();
            
            try
            {
                // CREATE
                var config = new HolidayConfig
                {
                    ConfigName = "Test Config",
                    EnableWeekendRule = true,
                    WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                    LegalHolidays = new List<DateTime> { new DateTime(2024, 1, 1) },
                    CustomHolidays = new List<DateTime> { new DateTime(2024, 12, 25) },
                    ExcludedDates = new List<DateTime>(),
                    IsActive = true
                };
                
                var id = await _repository.AddHolidayConfigAsync(config);
                if (id <= 0)
                {
                    result.ErrorMessage = "Failed to create HolidayConfig (invalid ID)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ CREATE: HolidayConfig created with ID {id}");
                result.CreateSuccess = true;

                // READ
                var configs = await _repository.GetAllHolidayConfigsAsync();
                if (configs.Count == 0)
                {
                    result.ErrorMessage = "Failed to read HolidayConfig (no records found)";
                    return result;
                }
                var retrievedConfig = configs.FirstOrDefault(c => c.Id == id);
                if (retrievedConfig == null)
                {
                    result.ErrorMessage = $"Failed to read HolidayConfig (ID {id} not found)";
                    return result;
                }
                if (retrievedConfig.ConfigName != config.ConfigName || 
                    retrievedConfig.WeekendDays.Count != config.WeekendDays.Count)
                {
                    result.ErrorMessage = "Failed to read HolidayConfig (data mismatch)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ READ: HolidayConfig retrieved successfully");
                result.ReadSuccess = true;

                // UPDATE
                retrievedConfig.ConfigName = "Updated Test Config";
                retrievedConfig.IsActive = false;
                await _repository.UpdateHolidayConfigAsync(retrievedConfig);
                
                var updatedConfig = (await _repository.GetAllHolidayConfigsAsync())
                    .FirstOrDefault(c => c.Id == id);
                if (updatedConfig == null || updatedConfig.ConfigName != "Updated Test Config" || updatedConfig.IsActive)
                {
                    result.ErrorMessage = "Failed to update HolidayConfig";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ UPDATE: HolidayConfig updated successfully");
                result.UpdateSuccess = true;

                // DELETE
                await _repository.DeleteHolidayConfigAsync(id);
                var deletedConfig = (await _repository.GetAllHolidayConfigsAsync())
                    .FirstOrDefault(c => c.Id == id);
                if (deletedConfig != null)
                {
                    result.ErrorMessage = "Failed to delete HolidayConfig (record still exists)";
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"  ✓ DELETE: HolidayConfig deleted successfully");
                result.DeleteSuccess = true;

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"  ✗ Exception: {ex.Message}");
            }

            return result;
        }

        private async Task<bool> VerifyIndexesAsync()
        {
            try
            {
                var diagnostics = await _dbService.GetDiagnosticsAsync();
                
                // Check that constraint table indexes exist
                var requiredIndexes = new[]
                {
                    "idx_fixed_rules_personal",
                    "idx_fixed_rules_enabled",
                    "idx_holiday_configs_active",
                    "idx_manual_assignments_position",
                    "idx_manual_assignments_personnel",
                    "idx_manual_assignments_date",
                    "idx_manual_assignments_enabled"
                };

                var existingIndexes = diagnostics.IndexStats.Select(i => i.IndexName).ToHashSet();
                
                foreach (var indexName in requiredIndexes)
                {
                    if (!existingIndexes.Contains(indexName))
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ Missing index: {indexName}");
                        return false;
                    }
                    System.Diagnostics.Debug.WriteLine($"  ✓ Index exists: {indexName}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ✗ Index verification error: {ex.Message}");
                return false;
            }
        }

        private void Cleanup()
        {
            try
            {
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                    System.Diagnostics.Debug.WriteLine($"Cleaned up test database: {_testDbPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup test database: {ex.Message}");
            }
        }
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public bool DatabaseInitialized { get; set; }
        public bool SchemaValid { get; set; }
        public bool IndexesValid { get; set; }
        public CRUDTestResult FixedPositionRuleTests { get; set; } = new();
        public CRUDTestResult ManualAssignmentTests { get; set; } = new();
        public CRUDTestResult HolidayConfigTests { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class CRUDTestResult
    {
        public bool Success { get; set; }
        public bool CreateSuccess { get; set; }
        public bool ReadSuccess { get; set; }
        public bool UpdateSuccess { get; set; }
        public bool DeleteSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
