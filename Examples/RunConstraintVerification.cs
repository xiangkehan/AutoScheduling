using System;
using System.Threading.Tasks;
using AutoScheduling3.TestData;

namespace AutoScheduling3.Examples
{
    /// <summary>
    /// Example runner for constraint repository verification
    /// This can be called from the UI or run as a standalone test
    /// </summary>
    public static class RunConstraintVerification
    {
        /// <summary>
        /// Execute the constraint repository verification tests
        /// </summary>
        public static async Task<bool> ExecuteAsync()
        {
            Console.WriteLine("Starting Constraint Repository Verification...");
            Console.WriteLine("==============================================\n");

            var verification = new ConstraintRepositoryVerification();
            var result = await verification.RunAllTestsAsync();

            Console.WriteLine("\n==============================================");
            Console.WriteLine($"Verification Result: {(result.Success ? "PASSED ✓" : "FAILED ✗")}");
            Console.WriteLine("==============================================\n");

            if (result.Success)
            {
                Console.WriteLine("All tests passed successfully!");
                Console.WriteLine($"✓ Database initialized: {result.DatabaseInitialized}");
                Console.WriteLine($"✓ Schema valid: {result.SchemaValid}");
                Console.WriteLine($"✓ Indexes valid: {result.IndexesValid}");
                Console.WriteLine($"✓ FixedPositionRule CRUD: {result.FixedPositionRuleTests.Success}");
                Console.WriteLine($"✓ ManualAssignment CRUD: {result.ManualAssignmentTests.Success}");
                Console.WriteLine($"✓ HolidayConfig CRUD: {result.HolidayConfigTests.Success}");
            }
            else
            {
                Console.WriteLine("Some tests failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  ✗ {error}");
                }
            }

            return result.Success;
        }
    }
}
