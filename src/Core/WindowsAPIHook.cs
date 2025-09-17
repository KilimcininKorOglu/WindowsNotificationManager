using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    public class WindowsAPIHook : IDisposable
    {
        private readonly MonitorManager _monitorManager;
        private readonly WindowTracker _windowTracker;
        private bool _isHookActive = false;

        // Process name caching for performance optimization
        private readonly Dictionary<uint, string> _processCache = new();
        private readonly Dictionary<uint, DateTime> _processCacheTime = new();
        private readonly TimeSpan _processCacheTimeout = TimeSpan.FromMinutes(5);
        private DateTime _lastCacheCleanup = DateTime.Now;

        // Performance statistics
        private int _totalEvents = 0;
        private int _filteredEvents = 0;
        private int _processedEvents = 0;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;
        private DateTime _lastStatsReport = DateTime.Now;

        // SetWindowPos hook using DLL injection approach
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool SetWindowPosDelegate(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        // Windows message hook approach (alternative to API hooking)
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        private IntPtr _winEventHook;
        private WinEventDelegate _winEventDelegate;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public WindowsAPIHook(MonitorManager monitorManager, WindowTracker windowTracker)
        {
            _monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
            _windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
        }

        public bool StartHooking()
        {
            if (_isHookActive) return true;

            try
            {
                DebugLogger.WriteLine("Starting Windows API hook for notifications...");

                // Use WinEvent hook instead of direct API hooking (safer and easier)
                _winEventDelegate = new WinEventDelegate(WinEventCallback);

                // Hook window creation and location change events
                _winEventHook = SetWinEventHook(
                    EVENT_OBJECT_CREATE, EVENT_OBJECT_LOCATIONCHANGE,
                    IntPtr.Zero, _winEventDelegate,
                    0, 0, WINEVENT_OUTOFCONTEXT);

                if (_winEventHook != IntPtr.Zero)
                {
                    _isHookActive = true;
                    DebugLogger.WriteLine("Windows API hook installed successfully");
                    return true;
                }

                DebugLogger.WriteLine("Failed to install Windows API hook");
                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error starting Windows API hook: {ex.Message}");
                return false;
            }
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                // Track total events received
                _totalEvents++;

                // Early filtering for performance optimization
                if (idObject != 0 || hwnd == IntPtr.Zero)
                {
                    _filteredEvents++;
                    return;
                }

                // Only process relevant event types for notifications
                if (eventType != EVENT_OBJECT_CREATE && eventType != EVENT_OBJECT_SHOW && eventType != EVENT_OBJECT_LOCATIONCHANGE)
                {
                    _filteredEvents++;
                    return;
                }

                // Report statistics every 1 minute
                var now = DateTime.Now;
                if (now - _lastStatsReport > TimeSpan.FromMinutes(1))
                {
                    LogPerformanceStats();
                    _lastStatsReport = now;
                }

                // Check if this is a notification window
                if (IsTargetNotificationWindow(hwnd))
                {
                    _processedEvents++;
                    DebugLogger.WriteLine($"DETECTED NOTIFICATION WINDOW: {hwnd:X8} - Event: {eventType}");

                    // Get current window position
                    if (GetWindowRect(hwnd, out RECT rect))
                    {
                        var width = rect.Right - rect.Left;
                        var height = rect.Bottom - rect.Top;

                        DebugLogger.WriteLine($"Current position: ({rect.Left},{rect.Top}) size: ({width},{height})");

                        // Calculate new position for target monitor (preserve Windows default positioning)
                        if (CalculateTargetPosition(out int newX, out int newY, width, height, rect.Left, rect.Top))
                        {
                            // Move the notification window to the target monitor
                            var success = SetWindowPos(hwnd, IntPtr.Zero, newX, newY, 0, 0,
                                0x0001 | 0x0004 | 0x0010); // SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE

                            if (success)
                            {
                                DebugLogger.WriteLine($"SUCCESSFULLY MOVED NOTIFICATION to ({newX},{newY}) - preserving Windows default position");
                            }
                            else
                            {
                                DebugLogger.WriteLine($"FAILED to move notification window");
                            }
                        }
                        else
                        {
                            DebugLogger.WriteLine($"SKIPPED MOVE - notification already on correct monitor");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error in WinEvent callback: {ex.Message}");
            }
        }

        private string GetProcessName(uint processId)
        {
            // Cleanup expired cache entries periodically (every 5 minutes)
            var now = DateTime.Now;
            if (now - _lastCacheCleanup > TimeSpan.FromMinutes(5))
            {
                CleanupProcessCache();
                _lastCacheCleanup = now;
            }

            // Check if we have a valid cached entry
            if (_processCache.TryGetValue(processId, out var cachedName))
            {
                if (_processCacheTime.TryGetValue(processId, out var cacheTime) &&
                    now - cacheTime < _processCacheTimeout)
                {
                    _cacheHits++;
                    return cachedName; // Return cached value for performance
                }
            }

            // Cache miss or expired, fetch from system
            try
            {
                _cacheMisses++;
                var process = Process.GetProcessById((int)processId);
                var processName = process.ProcessName;

                // Cache the result
                _processCache[processId] = processName;
                _processCacheTime[processId] = now;

                return processName;
            }
            catch
            {
                // Process might have ended, cache "Unknown" briefly
                _cacheMisses++;
                var unknownName = "Unknown";
                _processCache[processId] = unknownName;
                _processCacheTime[processId] = now;
                return unknownName;
            }
        }

        private void CleanupProcessCache()
        {
            var now = DateTime.Now;
            var expiredKeys = new List<uint>();

            // Find expired entries
            foreach (var kvp in _processCacheTime)
            {
                if (now - kvp.Value > _processCacheTimeout)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            // Remove expired entries
            foreach (var key in expiredKeys)
            {
                _processCache.Remove(key);
                _processCacheTime.Remove(key);
            }

            DebugLogger.WriteLine($"Process cache cleanup: removed {expiredKeys.Count} expired entries, cache size: {_processCache.Count}");
        }

        private void LogPerformanceStats()
        {
            var filterEfficiency = _totalEvents > 0 ? (double)_filteredEvents / _totalEvents * 100 : 0;
            var cacheHitRate = (_cacheHits + _cacheMisses) > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) * 100 : 0;

            DebugLogger.WriteLine($"=== PERFORMANCE STATISTICS (1min window) ===");
            DebugLogger.WriteLine($"Hook Events - Total: {_totalEvents}, Filtered: {_filteredEvents}, Processed: {_processedEvents}");
            DebugLogger.WriteLine($"Filter Efficiency: {filterEfficiency:F1}% (higher is better)");
            DebugLogger.WriteLine($"Process Cache - Hits: {_cacheHits}, Misses: {_cacheMisses}, Size: {_processCache.Count}");
            DebugLogger.WriteLine($"Cache Hit Rate: {cacheHitRate:F1}% (higher is better)");

            // Also log registry cache statistics
            DebugLogger.LogRegistryCacheStats();

            DebugLogger.WriteLine($"===============================================");
        }

        private bool IsTargetNotificationWindow(IntPtr hWnd)
        {
            try
            {
                // Check process
                if (GetWindowThreadProcessId(hWnd, out uint processId) == 0)
                    return false;

                var processName = GetProcessName(processId); // Use cached process name lookup

                // Windows version specific process check
                var isWindows11 = WindowsVersionHelper.IsWindows11OrLater();
                var validProcess = isWindows11 ?
                    (processName == "explorer" || processName == "ShellExperienceHost" || processName == "dwm") :
                    (processName == "ShellExperienceHost");

                if (!validProcess)
                    return false;

                // Check class name
                var className = new StringBuilder(256);
                if (GetClassName(hWnd, className, className.Capacity) == 0)
                    return false;

                var classNameStr = className.ToString();

                // Windows 11 vs Windows 10 class checks
                var validClass = false;
                if (isWindows11)
                {
                    validClass = classNameStr == "Windows.UI.Core.CoreWindow" ||
                               classNameStr.Contains("NotificationWindow") ||
                               classNameStr.Contains("ToastWindow") ||
                               classNameStr == "ApplicationFrameWindow";
                }
                else
                {
                    validClass = classNameStr == "Windows.UI.Core.CoreWindow";
                }

                if (!validClass)
                    return false;

                // Additional validation: check window title
                var windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);
                var title = windowText.ToString();

                // Log for debugging
                DebugLogger.WriteLine($"Potential notification: {hWnd:X8} - Process: {processName}, Class: {classNameStr}, Title: '{title}'");

                // Consider it a notification if it matches our criteria (optimized string operations)
                var isNotification = string.IsNullOrEmpty(title) ||
                                   title.Contains("notification", StringComparison.OrdinalIgnoreCase) ||
                                   title.Contains("bildirim", StringComparison.OrdinalIgnoreCase) ||
                                   title.Contains("toast", StringComparison.OrdinalIgnoreCase) ||
                                   title.Equals("New notification", StringComparison.OrdinalIgnoreCase);

                return isNotification;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error checking notification window {hWnd:X8}: {ex.Message}");
                return false;
            }
        }

        private bool CalculateTargetPosition(out int newX, out int newY, int width, int height, int currentX, int currentY)
        {
            newX = 0;
            newY = 0;

            try
            {
                // Get the current foreground window to determine target monitor
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero)
                {
                    // Get process name and window title for debugging
                    var foregroundProcessName = GetProcessName(GetWindowThreadProcessId(foregroundWindow, out var fgProcessId) != 0 ? fgProcessId : 0);
                    var foregroundTitle = new StringBuilder(256);
                    GetWindowText(foregroundWindow, foregroundTitle, foregroundTitle.Capacity);

                    // Get window position for debugging
                    if (GetWindowRect(foregroundWindow, out RECT fgRect))
                    {
                        DebugLogger.WriteLine($"Foreground window: {foregroundWindow:X8} - Process: {foregroundProcessName}, Title: '{foregroundTitle}', Position: ({fgRect.Left},{fgRect.Top},{fgRect.Right},{fgRect.Bottom})");
                    }
                    else
                    {
                        DebugLogger.WriteLine($"Foreground window: {foregroundWindow:X8} - Process: {foregroundProcessName}, Title: '{foregroundTitle}', Position: FAILED TO GET RECT");
                    }

                    var targetMonitor = _windowTracker.GetWindowMonitor(foregroundWindow);
                    if (targetMonitor != null)
                    {
                        // Find which monitor the notification is currently on
                        var currentMonitor = _monitorManager.GetAllMonitors()
                            .FirstOrDefault(m => currentX >= m.Bounds.Left && currentX < m.Bounds.Right &&
                                                currentY >= m.Bounds.Top && currentY < m.Bounds.Bottom);

                        if (currentMonitor != null)
                        {
                            // Use Windows native notification positioning (universal solution)
                            // This ensures consistent positioning across all monitor resolutions
                            const int WINDOWS_NOTIFICATION_MARGIN = 16; // Windows default margin

                            // Position notification in bottom-right corner using WorkArea (excludes taskbar)
                            newX = targetMonitor.WorkArea.Right - width - WINDOWS_NOTIFICATION_MARGIN;
                            newY = targetMonitor.WorkArea.Bottom - height - WINDOWS_NOTIFICATION_MARGIN;

                            DebugLogger.WriteLine($"Using Windows native positioning: margin={WINDOWS_NOTIFICATION_MARGIN}px");
                            DebugLogger.WriteLine($"Current notification monitor: {currentMonitor.Index}, Target app monitor: {targetMonitor.Index}");

                            if (currentMonitor.Index == targetMonitor.Index)
                            {
                                DebugLogger.WriteLine($"✓ NOTIFICATION ALREADY ON CORRECT MONITOR {targetMonitor.Index} - No move needed");
                                // Don't move if already on correct monitor
                                return false;
                            }
                            else
                            {
                                DebugLogger.WriteLine($"→ MOVING NOTIFICATION from Monitor {currentMonitor.Index} to Monitor {targetMonitor.Index}");
                            }

                            DebugLogger.WriteLine($"Target monitor {targetMonitor.Index}: bounds=({targetMonitor.Bounds.Left},{targetMonitor.Bounds.Top},{targetMonitor.Bounds.Right},{targetMonitor.Bounds.Bottom})");
                            return true;
                        }
                    }
                }

                // Fallback: keep current position (no move)
                newX = currentX;
                newY = currentY;
                DebugLogger.WriteLine($"Using fallback: keeping current position ({currentX},{currentY})");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error calculating target position: {ex.Message}");
                newX = currentX;
                newY = currentY;
                return false;
            }
        }

        public void StopHooking()
        {
            if (!_isHookActive) return;

            try
            {
                DebugLogger.WriteLine("Stopping Windows API hook...");

                if (_winEventHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_winEventHook);
                    _winEventHook = IntPtr.Zero;
                }

                _isHookActive = false;
                DebugLogger.WriteLine("Windows API hook stopped");
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error stopping Windows API hook: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopHooking();

            // Clear process cache on dispose
            _processCache.Clear();
            _processCacheTime.Clear();
        }
    }
}