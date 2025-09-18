using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace WindowsNotificationManager.src.Utils
{
    /// <summary>
    /// High-performance debug logging utility with registry-based configuration and aggressive caching.
    /// Provides thread-safe file logging with 30-second registry cache to minimize performance impact.
    /// Designed for high-frequency logging scenarios like Windows API hook callbacks.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Thread synchronization object for safe file access across multiple threads
        /// </summary>
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Absolute path to the debug log file (located in executable directory)
        /// </summary>
        private static readonly string _logFilePath;

        // Performance optimization: Registry caching system
        /// <summary>
        /// Cached value of debug logging enabled/disabled state to avoid frequent registry reads
        /// </summary>
        private static bool? _debugLoggingEnabled;

        /// <summary>
        /// Timestamp of last registry read for cache timeout calculation
        /// </summary>
        private static DateTime _lastRegistryCheck = DateTime.MinValue;

        /// <summary>
        /// Registry cache timeout duration (30 seconds for balance between performance and responsiveness)
        /// </summary>
        private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30);

        // Performance monitoring and statistics
        /// <summary>
        /// Number of times cached registry value was used (performance metric)
        /// </summary>
        private static int _registryCacheHits = 0;

        /// <summary>
        /// Number of times registry had to be read from system (performance metric)
        /// </summary>
        private static int _registryCacheMisses = 0;

        /// <summary>
        /// Static constructor that initializes the log file path based on executable location.
        /// Creates the log file path in the same directory as the application executable.
        /// </summary>
        static DebugLogger()
        {
            // Determine log file location: same directory as the executable
            var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logFilePath = Path.Combine(exeDirectory, "notification_debug.log");
        }

        /// <summary>
        /// Writes a timestamped message to the debug log file if logging is enabled.
        /// Thread-safe method that checks registry cache before writing to improve performance.
        /// Critical for high-frequency scenarios like Windows API hook callbacks.
        /// </summary>
        /// <param name="message">Message to log (timestamp will be automatically added)</param>
        public static void WriteLine(string message)
        {
            // Early exit for performance if logging is disabled (uses cached registry value)
            if (!IsDebugLoggingEnabled())
                return;

            try
            {
                // Thread-safe file writing with automatic timestamping
                lock (_lockObject)
                {
                    // Create timestamp with millisecond precision for detailed tracing
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] {message}";

                    // Write to both file and debug output for comprehensive logging
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

                    // Also output to Visual Studio debug console when available
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // Silently ignore logging errors to prevent application crashes
                // Logging failures should never break the main application functionality
            }
        }

        /// <summary>
        /// Determines if debug logging is enabled using aggressive registry caching for performance.
        /// Uses a 30-second cache to minimize registry access during high-frequency logging scenarios.
        /// Critical performance method called before every log write operation.
        /// </summary>
        /// <returns>True if debug logging is enabled, false otherwise</returns>
        private static bool IsDebugLoggingEnabled()
        {
            var now = DateTime.Now;

            // PERFORMANCE OPTIMIZATION: Use cached value if within timeout period
            if (_debugLoggingEnabled.HasValue && (now - _lastRegistryCheck) < _cacheTimeout)
            {
                _registryCacheHits++;
                return _debugLoggingEnabled.Value;
            }

            // Cache miss or timeout: read from registry and update cache
            try
            {
                _registryCacheMisses++;
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\KorOglansWindowsNotificationManager"))
                {
                    // Read EnableDebugLogging setting with default value of 1 (enabled)
                    var value = key?.GetValue("EnableDebugLogging", 1);
                    _debugLoggingEnabled = (int?)value == 1;
                    _lastRegistryCheck = now;
                    return _debugLoggingEnabled.Value;
                }
            }
            catch
            {
                // Registry access failed: default to enabled and cache the result
                // This ensures logging works even if registry is inaccessible
                _registryCacheMisses++;
                _debugLoggingEnabled = true;
                _lastRegistryCheck = now;
                return true;
            }
        }

        /// <summary>
        /// Deletes the debug log file to start fresh or free up disk space.
        /// Thread-safe operation that handles file deletion safely.
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                // Thread-safe file deletion
                lock (_lockObject)
                {
                    if (File.Exists(_logFilePath))
                    {
                        File.Delete(_logFilePath);
                    }
                }
            }
            catch
            {
                // Silently ignore file deletion errors (file may be in use, etc.)
            }
        }

        /// <summary>
        /// Gets the absolute path to the debug log file.
        /// Useful for displaying log location to users or opening the log externally.
        /// </summary>
        /// <returns>Full path to the notification_debug.log file</returns>
        public static string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Invalidates the registry cache to force re-reading of debug logging setting.
        /// Must be called when the debug logging setting changes in the UI to ensure
        /// the cache reflects the new setting immediately.
        /// </summary>
        public static void InvalidateCache()
        {
            // Clear cached value and timestamp to force registry re-read
            _debugLoggingEnabled = null;
            _lastRegistryCheck = DateTime.MinValue;
        }

        /// <summary>
        /// Logs performance statistics about registry cache efficiency.
        /// Called periodically by performance monitoring systems to track cache effectiveness.
        /// High hit rates (97%+) indicate optimal performance with minimal registry access.
        /// </summary>
        public static void LogRegistryCacheStats()
        {
            var totalRequests = _registryCacheHits + _registryCacheMisses;
            var hitRate = totalRequests > 0 ? (double)_registryCacheHits / totalRequests * 100 : 0;

            // Log cache performance metrics for monitoring and optimization
            WriteLine($"Registry Cache Stats - Hits: {_registryCacheHits}, Misses: {_registryCacheMisses}, Hit Rate: {hitRate:F1}% (target: 97%+)");
        }
    }
}