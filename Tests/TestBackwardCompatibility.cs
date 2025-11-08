using System;
using System.IO;
using System.Threading.Tasks;
using AutoScheduling3.Data;
using AutoScheduling3.Data.Exceptions;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// Simple test program to verify backward compatibility of DatabaseService.InitializeAsync()
    /// </summary>
    public class TestBackwardCompatibility
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Testing DatabaseService Backward Compatibility ===\n");
            
            await TestSuccessfulInitialization();
            await TestExceptionOnFailure();
            
            Console.WriteLine("\n=== All Backward Compatibility Tests Completed ===");
        }
        
        private static async Task TestSuccessfulInitialization()
        {
            Console.WriteLine("Test 1: Successful initialization with parameterless InitializeAsync()");
            
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_backward_compat_{Guid.NewGuid()}.db");
            
            try
            {
                var dbService = new DatabaseService(tempPath);
                
                // Call parameterless InitializeAsync (backward compatibility method)
                await dbService.InitializeAsync();
                
                Console.WriteLine("✅ PASSED: Parameterless InitializeAsync() completed without exception");
                
                // Verify database was created
                if (File.Exists(tempPath))
                {
                    Console.WriteLine("✅ PASSED: Database file was created");
                    
                    var dbInfo = await dbService.GetDatabaseInfoAsync();
                    Console.WriteLine($"   Database Version: {dbInfo.Version}");
                    Console.WriteLine($"   Database Size: {dbInfo.FileSize} bytes");
                }
                else
                {
                    Console.WriteLine("❌ FAILED: Database file was not created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Unexpected exception: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch { }
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestExceptionOnFailure()
        {
            Console.WriteLine("Test 2: Exception throwing on failure (backward compatibility)");
            
            // Use an invalid path that will cause permission error
            var invalidPath = Path.Combine("C:\\Windows\\System32", $"test_db_{Guid.NewGuid()}.db");
            
            try
            {
                var dbService = new DatabaseService(invalidPath);
                
                // Call parameterless InitializeAsync - should throw exception
                await dbService.InitializeAsync();
                
                Console.WriteLine("❌ FAILED: Should have thrown DatabaseInitializationException");
            }
            catch (DatabaseInitializationException ex)
            {
                Console.WriteLine("✅ PASSED: Correctly threw DatabaseInitializationException");
                Console.WriteLine($"   Failed Stage: {ex.FailedStage}");
                Console.WriteLine($"   Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: Wrong exception type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
            }
            
            Console.WriteLine();
        }
    }
}
