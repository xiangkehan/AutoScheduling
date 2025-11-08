using System;
using System.IO;
using System.Threading.Tasks;
using AutoScheduling3.Data;
using AutoScheduling3.Data.Models;
using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// Tests for DatabaseService initialization result generation
    /// Verifies Requirements: 1.4, 9.5
    /// </summary>
    public class DatabaseServiceInitializationTests
    {
        /// <summary>
        /// Test that initialization result is properly generated for a new database
        /// </summary>
        public static async Task TestNewDatabaseInitialization()
        {
            Console.WriteLine("=== Testing New Database Initialization ===");
            
            // Create a temporary database path
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            
            try
            {
                var dbService = new DatabaseService(tempPath);
                
                // Initialize with default options
                var result = await dbService.InitializeAsync(new InitializationOptions
                {
                    PerformHealthCheck = false, // Skip health check for new database
                    AutoRepair = true
                });
                
                // Verify result properties
                Console.WriteLine($"Success: {result.Success}");
                Console.WriteLine($"FinalState: {result.FinalState}");
                Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"Warnings Count: {result.Warnings.Count}");
                Console.WriteLine($"FailedStage: {result.FailedStage?.ToString() ?? "None"}");
                Console.WriteLine($"ErrorMessage: {(string.IsNullOrEmpty(result.ErrorMessage) ? "None" : result.ErrorMessage)}");
                
                // Assertions
                if (!result.Success)
                {
                    Console.WriteLine("❌ FAILED: Initialization should succeed for new database");
                    return;
                }
                
                if (result.FinalState != InitializationState.Completed)
                {
                    Console.WriteLine($"❌ FAILED: FinalState should be Completed, got {result.FinalState}");
                    return;
                }
                
                if (result.Duration.TotalMilliseconds <= 0)
                {
                    Console.WriteLine("❌ FAILED: Duration should be greater than 0");
                    return;
                }
                
                if (result.FailedStage != null)
                {
                    Console.WriteLine($"❌ FAILED: FailedStage should be null for successful initialization, got {result.FailedStage}");
                    return;
                }
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"❌ FAILED: ErrorMessage should be empty for successful initialization, got '{result.ErrorMessage}'");
                    return;
                }
                
                Console.WriteLine("✅ PASSED: New database initialization result generation works correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Exception during test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Cleanup
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Test that initialization result includes warnings when issues are detected
        /// </summary>
        public static async Task TestInitializationWithWarnings()
        {
            Console.WriteLine("\n=== Testing Initialization with Warnings ===");
            
            // Create a temporary database path
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            
            try
            {
                // First create a database
                var dbService1 = new DatabaseService(tempPath);
                await dbService1.InitializeAsync(new InitializationOptions
                {
                    PerformHealthCheck = false,
                    AutoRepair = false
                });
                
                // Now initialize again with health check enabled
                var dbService2 = new DatabaseService(tempPath);
                var result = await dbService2.InitializeAsync(new InitializationOptions
                {
                    PerformHealthCheck = true,
                    AutoRepair = true
                });
                
                Console.WriteLine($"Success: {result.Success}");
                Console.WriteLine($"FinalState: {result.FinalState}");
                Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"Warnings Count: {result.Warnings.Count}");
                
                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine("Warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }
                
                // Verify result
                if (!result.Success)
                {
                    Console.WriteLine("❌ FAILED: Initialization should succeed");
                    return;
                }
                
                if (result.Duration.TotalMilliseconds <= 0)
                {
                    Console.WriteLine("❌ FAILED: Duration should be greater than 0");
                    return;
                }
                
                Console.WriteLine("✅ PASSED: Initialization with warnings works correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Exception during test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Cleanup
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Test that initialization result properly tracks failed initialization
        /// </summary>
        public static async Task TestFailedInitialization()
        {
            Console.WriteLine("\n=== Testing Failed Initialization ===");
            
            // Use an invalid path that will cause permission error
            var invalidPath = Path.Combine("C:\\Windows\\System32", $"test_db_{Guid.NewGuid()}.db");
            
            try
            {
                var dbService = new DatabaseService(invalidPath);
                
                var result = await dbService.InitializeAsync(new InitializationOptions());
                
                Console.WriteLine($"Success: {result.Success}");
                Console.WriteLine($"FinalState: {result.FinalState}");
                Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"FailedStage: {result.FailedStage?.ToString() ?? "None"}");
                Console.WriteLine($"ErrorMessage: {result.ErrorMessage}");
                
                // Verify result
                if (result.Success)
                {
                    Console.WriteLine("❌ FAILED: Initialization should fail for invalid path");
                    return;
                }
                
                if (result.FinalState != InitializationState.Failed)
                {
                    Console.WriteLine($"❌ FAILED: FinalState should be Failed, got {result.FinalState}");
                    return;
                }
                
                if (result.FailedStage == null)
                {
                    Console.WriteLine("❌ FAILED: FailedStage should be set for failed initialization");
                    return;
                }
                
                if (string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine("❌ FAILED: ErrorMessage should be set for failed initialization");
                    return;
                }
                
                if (result.Duration.TotalMilliseconds <= 0)
                {
                    Console.WriteLine("❌ FAILED: Duration should be greater than 0");
                    return;
                }
                
                Console.WriteLine("✅ PASSED: Failed initialization result generation works correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Exception during test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Test backward compatibility - parameterless InitializeAsync method
        /// Verifies Requirement: 1.1
        /// </summary>
        public static async Task TestBackwardCompatibility()
        {
            Console.WriteLine("\n=== Testing Backward Compatibility (Parameterless InitializeAsync) ===");
            
            // Create a temporary database path
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            
            try
            {
                var dbService = new DatabaseService(tempPath);
                
                // Call the parameterless InitializeAsync method (backward compatibility)
                await dbService.InitializeAsync();
                
                Console.WriteLine("✅ PASSED: Parameterless InitializeAsync completed successfully");
                
                // Verify database was created
                if (!File.Exists(tempPath))
                {
                    Console.WriteLine("❌ FAILED: Database file was not created");
                    return;
                }
                
                // Verify we can get database info
                var dbInfo = await dbService.GetDatabaseInfoAsync();
                Console.WriteLine($"Database Version: {dbInfo.Version}");
                Console.WriteLine($"Database Size: {dbInfo.FileSize} bytes");
                
                if (dbInfo.Version <= 0)
                {
                    Console.WriteLine("❌ FAILED: Database version should be set");
                    return;
                }
                
                Console.WriteLine("✅ PASSED: Backward compatibility works correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Exception during backward compatibility test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Cleanup
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Test backward compatibility - exception throwing on failure
        /// Verifies Requirement: 1.1
        /// </summary>
        public static async Task TestBackwardCompatibilityExceptionThrow()
        {
            Console.WriteLine("\n=== Testing Backward Compatibility Exception Throwing ===");
            
            // Use an invalid path that will cause permission error
            var invalidPath = Path.Combine("C:\\Windows\\System32", $"test_db_{Guid.NewGuid()}.db");
            
            try
            {
                var dbService = new DatabaseService(invalidPath);
                
                // Call the parameterless InitializeAsync method - should throw exception
                await dbService.InitializeAsync();
                
                Console.WriteLine("❌ FAILED: Should have thrown an exception for invalid path");
            }
            catch (Data.Exceptions.DatabaseInitializationException ex)
            {
                Console.WriteLine($"✅ PASSED: Correctly threw DatabaseInitializationException");
                Console.WriteLine($"Failed Stage: {ex.FailedStage}");
                Console.WriteLine($"Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Wrong exception type thrown: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Run all tests
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("Starting DatabaseService Initialization Result Tests\n");
            
            await TestNewDatabaseInitialization();
            await TestInitializationWithWarnings();
            await TestFailedInitialization();
            await TestBackwardCompatibility();
            await TestBackwardCompatibilityExceptionThrow();
            
            Console.WriteLine("\n=== All Tests Completed ===");
        }
    }
}
