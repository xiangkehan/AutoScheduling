using System;
using System.Diagnostics;

namespace AutoScheduling3.Data.Logging
{
    /// <summary>
    /// Simple logger implementation that writes to Debug output
    /// </summary>
    public class DebugLogger : ILogger
    {
        private readonly string _prefix;

        public DebugLogger(string prefix = "Database")
        {
            _prefix = prefix;
        }

        public void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug.WriteLine($"[{timestamp}] [{_prefix}] [INFO] {message}");
        }

        public void LogWarning(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug.WriteLine($"[{timestamp}] [{_prefix}] [WARN] {message}");
        }

        public void LogError(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug.WriteLine($"[{timestamp}] [{_prefix}] [ERROR] {message}");
        }
    }
}
