using Microsoft.Win32;
using System;

namespace WindowsNotificationManager.src.Utils
{
    /// <summary>
    /// Provides Windows version detection and notification system identification utilities.
    /// Distinguishes between Windows 10 and Windows 11 using build number analysis for proper notification handling.
    /// Caches version information for performance and provides system-specific notification API guidance.
    /// </summary>
    public static class WindowsVersionHelper
    {
        /// <summary>
        /// Cached Windows version to avoid repeated registry and API calls for performance optimization
        /// </summary>
        private static WindowsVersion? _cachedVersion = null;

        /// <summary>
        /// Cached Windows build number to avoid repeated system queries for performance optimization
        /// </summary>
        private static int? _cachedBuildNumber = null;

        /// <summary>
        /// Enumeration of supported Windows versions with notification system implications.
        /// Used to determine appropriate notification handling strategies and API usage.
        /// </summary>
        public enum WindowsVersion
        {
            /// <summary>
            /// Unknown or unsupported Windows version (pre-Windows 10)
            /// </summary>
            Unknown,

            /// <summary>
            /// Windows 10 (builds 10240-21999) - uses ShellExperienceHost.exe for notifications
            /// </summary>
            Windows10,

            /// <summary>
            /// Windows 11 (builds 22000+) - uses explorer.exe and WinRT Toast APIs for notifications
            /// </summary>
            Windows11
        }

        /// <summary>
        /// Detects the current Windows version using build number analysis with intelligent caching.
        /// Distinguishes between Windows 10 and Windows 11 based on official Microsoft build number thresholds.
        /// Critical for selecting appropriate notification interception strategies and API compatibility.
        /// </summary>
        /// <returns>WindowsVersion enumeration value indicating the detected Windows version</returns>
        public static WindowsVersion GetWindowsVersion()
        {
            // Return cached result for performance if already detected
            if (_cachedVersion.HasValue)
                return _cachedVersion.Value;

            try
            {
                // Retrieve Windows build number for version classification
                var buildNumber = GetWindowsBuildNumber();

                DebugLogger.WriteLine($"Detected Windows build number: {buildNumber}");

                // Windows version classification based on official Microsoft build numbers
                if (buildNumber >= 22000)
                {
                    // Windows 11 starts at build 22000 (October 2021 release)
                    _cachedVersion = WindowsVersion.Windows11;
                }
                else if (buildNumber >= 10240)
                {
                    // Windows 10 starts at build 10240 (July 2015 RTM release)
                    _cachedVersion = WindowsVersion.Windows10;
                }
                else
                {
                    // Pre-Windows 10 or invalid build numbers
                    _cachedVersion = WindowsVersion.Unknown;
                }

                return _cachedVersion.Value;
            }
            catch (Exception ex)
            {
                // Log detection errors but don't break functionality
                DebugLogger.WriteLine($"Error detecting Windows version: {ex.Message}");
                _cachedVersion = WindowsVersion.Unknown;
                return _cachedVersion.Value;
            }
        }

        /// <summary>
        /// Retrieves the Windows build number using multiple detection methods with fallback strategies.
        /// Uses registry as primary source with Environment.OSVersion as backup for reliable detection.
        /// Build number is essential for distinguishing Windows 10 vs Windows 11 and notification system APIs.
        /// </summary>
        /// <returns>Windows build number, or 0 if detection fails</returns>
        private static int GetWindowsBuildNumber()
        {
            // Return cached result for performance if already retrieved
            if (_cachedBuildNumber.HasValue)
                return _cachedBuildNumber.Value;

            try
            {
                // PRIMARY METHOD: Registry-based detection (most accurate)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        // First attempt: CurrentBuildNumber registry value (standard location)
                        var buildNumber = key.GetValue("CurrentBuildNumber")?.ToString();
                        if (int.TryParse(buildNumber, out int build))
                        {
                            _cachedBuildNumber = build;
                            return build;
                        }

                        // Alternative approach: UBR (Update Build Revision) with CurrentBuild fallback
                        // Some Windows versions store build information in different registry values
                        var ubr = key.GetValue("UBR");
                        if (ubr != null && int.TryParse(ubr.ToString(), out int ubrValue))
                        {
                            var currentBuild = key.GetValue("CurrentBuild")?.ToString();
                            if (int.TryParse(currentBuild, out int currentBuildValue))
                            {
                                _cachedBuildNumber = currentBuildValue;
                                return currentBuildValue;
                            }
                        }
                    }
                }

                // FALLBACK METHOD: Environment.OSVersion API (less reliable but widely supported)
                var version = Environment.OSVersion.Version;
                if (version.Major == 10)
                {
                    // Windows 10/11 both report major version 10, use build number for differentiation
                    _cachedBuildNumber = version.Build;
                    return version.Build;
                }

                // No valid build number detected
                _cachedBuildNumber = 0;
                return 0;
            }
            catch (Exception ex)
            {
                // Log detection errors for debugging but return safe default
                DebugLogger.WriteLine($"Error getting Windows build number: {ex.Message}");
                _cachedBuildNumber = 0;
                return 0;
            }
        }

        /// <summary>
        /// Generates a human-readable Windows version string including build number for display purposes.
        /// Used in UI components, debug logs, and system information displays for user identification.
        /// Provides complete version information for troubleshooting and support scenarios.
        /// </summary>
        /// <returns>Formatted version string like "Windows 11 (Build 22621)" or "Windows 10 (Build 19045)"</returns>
        public static string GetWindowsVersionString()
        {
            var version = GetWindowsVersion();
            var buildNumber = GetWindowsBuildNumber();

            return version switch
            {
                WindowsVersion.Windows11 => $"Windows 11 (Build {buildNumber})",
                WindowsVersion.Windows10 => $"Windows 10 (Build {buildNumber})",
                _ => $"Unknown Windows version (Build {buildNumber})"
            };
        }

        /// <summary>
        /// Provides detailed information about the notification system architecture for the detected Windows version.
        /// Critical for understanding which processes to monitor and which APIs to use for notification interception.
        /// Used by WindowsAPIHook and other components to select appropriate monitoring strategies.
        /// </summary>
        /// <returns>Descriptive string explaining the notification system implementation for the current Windows version</returns>
        public static string GetNotificationSystemInfo()
        {
            var version = GetWindowsVersion();

            return version switch
            {
                WindowsVersion.Windows11 => "Windows 11: Uses explorer.exe for notifications, WinRT Toast APIs",
                WindowsVersion.Windows10 => "Windows 10: Uses ShellExperienceHost.exe for notifications",
                _ => "Unknown notification system"
            };
        }

        /// <summary>
        /// Convenience method to check if the current system is running Windows 11 or a later version.
        /// Used by components that need to enable Windows 11-specific features or compatibility handling.
        /// Essential for selecting modern notification APIs and explorer.exe monitoring strategies.
        /// </summary>
        /// <returns>True if Windows 11 or later is detected, false for Windows 10 or earlier versions</returns>
        public static bool IsWindows11OrLater()
        {
            return GetWindowsVersion() == WindowsVersion.Windows11;
        }

        /// <summary>
        /// Convenience method to check if the current system is specifically running Windows 10.
        /// Used by components that need Windows 10-specific compatibility handling or legacy notification support.
        /// Important for ShellExperienceHost.exe monitoring and Windows 10 notification API usage.
        /// </summary>
        /// <returns>True if Windows 10 is detected, false for other Windows versions</returns>
        public static bool IsWindows10()
        {
            return GetWindowsVersion() == WindowsVersion.Windows10;
        }
    }
}