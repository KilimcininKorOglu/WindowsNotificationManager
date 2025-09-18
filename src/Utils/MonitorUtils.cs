using System;
using System.Collections.Generic;
using static WindowsNotificationManager.src.Utils.Win32Helper;

namespace WindowsNotificationManager.src.Utils
{
    /// <summary>
    /// Data transfer object containing comprehensive information about a display monitor.
    /// Encapsulates Windows monitor properties including physical bounds, work area, and primary status.
    /// Used throughout the application for multi-monitor notification positioning and window tracking.
    /// </summary>
    public class MonitorInfo
    {
        /// <summary>
        /// Windows monitor handle (HMONITOR) that uniquely identifies this display device
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Complete monitor rectangle including areas behind taskbar and other system UI elements
        /// </summary>
        public RECT Bounds { get; set; }

        /// <summary>
        /// Usable work area rectangle excluding taskbar, docked windows, and system UI elements.
        /// This is the preferred area for notification positioning to avoid UI conflicts.
        /// </summary>
        public RECT WorkArea { get; set; }

        /// <summary>
        /// Indicates if this is the primary monitor (contains the Windows taskbar and start button)
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Zero-based index assigned during monitor enumeration for consistent identification
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Determines if a screen coordinate point falls within this monitor's boundaries.
        /// Uses the full monitor bounds (not work area) for accurate point-in-monitor detection.
        /// Critical for multi-monitor window and notification positioning logic.
        /// </summary>
        /// <param name="x">Screen X coordinate to test</param>
        /// <param name="y">Screen Y coordinate to test</param>
        /// <returns>True if the point is within this monitor's bounds, false otherwise</returns>
        public bool ContainsPoint(int x, int y)
        {
            return x >= Bounds.Left && x < Bounds.Right &&
                   y >= Bounds.Top && y < Bounds.Bottom;
        }

        /// <summary>
        /// Determines if a window belongs to this monitor based on its center point location.
        /// Uses center-point detection rather than overlap percentage for consistent monitor assignment.
        /// This method is crucial for routing notifications to the correct monitor in multi-monitor setups.
        /// </summary>
        /// <param name="windowRect">Window rectangle coordinates to test</param>
        /// <returns>True if the window's center point is within this monitor, false otherwise</returns>
        public bool ContainsWindow(RECT windowRect)
        {
            // Calculate window center point for reliable monitor assignment
            // Center-point method prevents ambiguity when windows span multiple monitors
            int centerX = windowRect.Left + windowRect.Width / 2;
            int centerY = windowRect.Top + windowRect.Height / 2;
            return ContainsPoint(centerX, centerY);
        }
    }

    /// <summary>
    /// Utility class providing Windows API-based monitor detection and management functions.
    /// Encapsulates complex Windows display enumeration APIs into simple, reusable methods.
    /// Used by MonitorManager and other components that need monitor information and geometry.
    /// </summary>
    public static class MonitorUtils
    {
        /// <summary>
        /// Enumerates all display monitors in the system and returns comprehensive information for each.
        /// Uses Windows EnumDisplayMonitors API to discover all attached displays including extended monitors.
        /// Critical method that forms the foundation of multi-monitor notification routing capabilities.
        /// </summary>
        /// <returns>List of MonitorInfo objects representing all detected monitors with bounds, work areas, and primary status</returns>
        public static List<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo>();
            int index = 0;

            // Enumerate all display monitors using Windows API callback pattern
            // Callback is invoked once for each monitor attached to the desktop
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                // Initialize MONITORINFO structure for detailed monitor information retrieval
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);

                // Retrieve comprehensive monitor information including bounds and work area
                if (GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    // Create MonitorInfo object with all relevant display properties
                    monitors.Add(new MonitorInfo
                    {
                        Handle = hMonitor,                    // Unique monitor handle for API calls
                        Bounds = monitorInfo.rcMonitor,       // Full monitor rectangle including system UI areas
                        WorkArea = monitorInfo.rcWork,        // Usable area excluding taskbar and docked windows
                        IsPrimary = (monitorInfo.dwFlags & MONITORINFO.MONITORINFOF_PRIMARY) != 0,  // Primary monitor detection
                        Index = index++                       // Sequential index for consistent identification
                    });
                }

                return true; // Continue enumeration to process all monitors
            }, IntPtr.Zero);

            return monitors;
        }

        /// <summary>
        /// Retrieves monitor information for the display containing the specified window.
        /// Uses Windows MonitorFromWindow API with NEAREST fallback to handle edge cases gracefully.
        /// Essential for determining which monitor a notification should be routed to based on window location.
        /// </summary>
        /// <param name="windowHandle">Windows handle (HWND) of the window to locate</param>
        /// <returns>MonitorInfo for the monitor containing the window, or null if window handle is invalid</returns>
        public static MonitorInfo GetMonitorFromWindow(IntPtr windowHandle)
        {
            // Get monitor handle using NEAREST fallback to handle minimized/off-screen windows
            // MONITOR_DEFAULTTONEAREST ensures we always get a valid monitor even for edge cases
            IntPtr monitorHandle = MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);

            // Initialize structure for detailed monitor information retrieval
            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);

            // Retrieve complete monitor details including bounds, work area, and primary status
            if (GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return new MonitorInfo
                {
                    Handle = monitorHandle,                   // Monitor handle for future API calls
                    Bounds = monitorInfo.rcMonitor,           // Full monitor rectangle
                    WorkArea = monitorInfo.rcWork,            // Usable area excluding system UI
                    IsPrimary = (monitorInfo.dwFlags & MONITORINFO.MONITORINFOF_PRIMARY) != 0  // Primary monitor flag
                };
            }

            // Return null if monitor information retrieval fails (rare edge case)
            return null;
        }

        /// <summary>
        /// Finds the monitor containing the specified screen coordinate point.
        /// Useful for determining monitor assignment based on cursor position or arbitrary screen coordinates.
        /// Used by components that need to place UI elements at specific screen locations.
        /// </summary>
        /// <param name="x">Screen X coordinate to locate</param>
        /// <param name="y">Screen Y coordinate to locate</param>
        /// <returns>MonitorInfo containing the specified point, or null if point is outside all monitors</returns>
        public static MonitorInfo GetMonitorFromPoint(int x, int y)
        {
            // Get all available monitors for point-in-bounds testing
            var monitors = GetAllMonitors();

            // Search through all monitors to find the one containing the specified point
            foreach (var monitor in monitors)
            {
                if (monitor.ContainsPoint(x, y))
                    return monitor;
            }

            // Return null if the point falls outside all monitor boundaries
            // This can occur with invalid coordinates or during monitor configuration changes
            return null;
        }
    }
}