using Microsoft.Win32;
using System;

namespace WindowsNotificationManager.src.Utils
{
    public static class WindowsVersionHelper
    {
        private static WindowsVersion? _cachedVersion = null;
        private static int? _cachedBuildNumber = null;

        public enum WindowsVersion
        {
            Unknown,
            Windows10,
            Windows11
        }

        public static WindowsVersion GetWindowsVersion()
        {
            if (_cachedVersion.HasValue)
                return _cachedVersion.Value;

            try
            {
                // Windows 11 detection method
                var buildNumber = GetWindowsBuildNumber();

                DebugLogger.WriteLine($"Detected Windows build number: {buildNumber}");

                if (buildNumber >= 22000)
                {
                    _cachedVersion = WindowsVersion.Windows11;
                }
                else if (buildNumber >= 10240)
                {
                    _cachedVersion = WindowsVersion.Windows10;
                }
                else
                {
                    _cachedVersion = WindowsVersion.Unknown;
                }

                return _cachedVersion.Value;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error detecting Windows version: {ex.Message}");
                _cachedVersion = WindowsVersion.Unknown;
                return _cachedVersion.Value;
            }
        }

        private static int GetWindowsBuildNumber()
        {
            if (_cachedBuildNumber.HasValue)
                return _cachedBuildNumber.Value;

            try
            {
                // Method 1: Registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var buildNumber = key.GetValue("CurrentBuildNumber")?.ToString();
                        if (int.TryParse(buildNumber, out int build))
                        {
                            _cachedBuildNumber = build;
                            return build;
                        }

                        // Alternative: UBR (Update Build Revision)
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

                // Method 2: Environment
                var version = Environment.OSVersion.Version;
                if (version.Major == 10)
                {
                    _cachedBuildNumber = version.Build;
                    return version.Build;
                }

                _cachedBuildNumber = 0;
                return 0;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error getting Windows build number: {ex.Message}");
                _cachedBuildNumber = 0;
                return 0;
            }
        }

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

        public static bool IsWindows11OrLater()
        {
            return GetWindowsVersion() == WindowsVersion.Windows11;
        }

        public static bool IsWindows10()
        {
            return GetWindowsVersion() == WindowsVersion.Windows10;
        }
    }
}