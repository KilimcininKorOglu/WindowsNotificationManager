using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace WindowsNotificationManager.src.Utils
{
    public static class DebugLogger
    {
        private static readonly object _lockObject = new object();
        private static readonly string _logFilePath;

        // Registry caching for performance optimization
        private static bool? _debugLoggingEnabled;
        private static DateTime _lastRegistryCheck = DateTime.MinValue;
        private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30);

        // Registry cache statistics
        private static int _registryCacheHits = 0;
        private static int _registryCacheMisses = 0;

        static DebugLogger()
        {
            // Get exe directory
            var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logFilePath = Path.Combine(exeDirectory, "notification_debug.log");
        }

        public static void WriteLine(string message)
        {
            if (!IsDebugLoggingEnabled())
                return;

            try
            {
                lock (_lockObject)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] {message}";

                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

                    // Also write to debug output if available
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // Ignore logging errors to prevent crashes
            }
        }

        private static bool IsDebugLoggingEnabled()
        {
            var now = DateTime.Now;

            // Use cached value if still valid
            if (_debugLoggingEnabled.HasValue && (now - _lastRegistryCheck) < _cacheTimeout)
            {
                _registryCacheHits++;
                return _debugLoggingEnabled.Value;
            }

            // Read from registry and cache result
            try
            {
                _registryCacheMisses++;
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\KorOglansWindowsNotificationManager"))
                {
                    var value = key?.GetValue("EnableDebugLogging", 1);
                    _debugLoggingEnabled = (int?)value == 1;
                    _lastRegistryCheck = now;
                    return _debugLoggingEnabled.Value;
                }
            }
            catch
            {
                // Default to enabled if can't read registry, cache the result
                _registryCacheMisses++;
                _debugLoggingEnabled = true;
                _lastRegistryCheck = now;
                return true;
            }
        }

        public static void ClearLog()
        {
            try
            {
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
                // Ignore errors
            }
        }

        public static string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Clears the registry cache to force re-reading on next log call
        /// Call this when debug logging setting changes
        /// </summary>
        public static void InvalidateCache()
        {
            _debugLoggingEnabled = null;
            _lastRegistryCheck = DateTime.MinValue;
        }

        /// <summary>
        /// Logs registry cache statistics - called periodically for monitoring
        /// </summary>
        public static void LogRegistryCacheStats()
        {
            var totalRequests = _registryCacheHits + _registryCacheMisses;
            var hitRate = totalRequests > 0 ? (double)_registryCacheHits / totalRequests * 100 : 0;

            WriteLine($"Registry Cache Stats - Hits: {_registryCacheHits}, Misses: {_registryCacheMisses}, Hit Rate: {hitRate:F1}%");
        }
    }
}