using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsNotificationManager.src.Utils
{
    /// <summary>
    /// Provides Windows API function declarations and data structures for system-level operations.
    /// Encapsulates P/Invoke calls to user32.dll for window management, monitor detection, and display operations.
    /// Critical foundation component enabling multi-monitor notification positioning and window tracking functionality.
    /// </summary>
    public static class Win32Helper
    {
        /// <summary>
        /// Retrieves the window handle of the currently active (foreground) window.
        /// Used by WindowTracker to detect focus changes and determine which application is currently active.
        /// Critical for intelligent notification routing based on user attention.
        /// </summary>
        /// <returns>Handle to the foreground window, or IntPtr.Zero if no window has focus</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the position and size coordinates of a window's bounding rectangle.
        /// Essential for determining window positions across multiple monitors for notification routing.
        /// Used extensively by WindowTracker and MonitorManager for position-based logic.
        /// </summary>
        /// <param name="hWnd">Handle to the window</param>
        /// <param name="lpRect">RECT structure that receives the window coordinates</param>
        /// <returns>True if successful, false if the window handle is invalid</returns>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Retrieves the text caption/title of a window for identification purposes.
        /// Used by WindowTracker to capture window titles for display in the UI and debugging.
        /// Supports Unicode text with proper character encoding for international applications.
        /// </summary>
        /// <param name="hWnd">Handle to the window</param>
        /// <param name="lpString">StringBuilder buffer to receive the window text</param>
        /// <param name="nMaxCount">Maximum number of characters to copy including null terminator</param>
        /// <returns>Length of the copied string, or 0 if the window has no title</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Retrieves the process and thread identifiers of the process that created a window.
        /// Critical for mapping windows to their owning processes for notification routing logic.
        /// Used by WindowTracker to associate windows with application process names.
        /// </summary>
        /// <param name="hWnd">Handle to the window</param>
        /// <param name="lpdwProcessId">Receives the process identifier</param>
        /// <returns>Thread identifier of the thread that created the window</returns>
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Enumerates all display monitors attached to the desktop by calling a callback function for each monitor.
        /// Foundation of multi-monitor detection system used by MonitorManager to discover all available displays.
        /// Callback pattern enables comprehensive monitor enumeration across complex multi-display configurations.
        /// </summary>
        /// <param name="hdc">Device context handle (use IntPtr.Zero for all monitors)</param>
        /// <param name="lprcClip">Clipping rectangle (use IntPtr.Zero for entire desktop)</param>
        /// <param name="lpfnEnum">Callback function called once for each monitor</param>
        /// <param name="dwData">Application-defined data passed to callback</param>
        /// <returns>True if enumeration completes successfully, false otherwise</returns>
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        /// <summary>
        /// Retrieves detailed information about a display monitor including bounds, work area, and primary status.
        /// Essential for obtaining monitor geometry and capabilities for notification positioning calculations.
        /// Provides both full monitor bounds and usable work area excluding taskbar and docked windows.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor</param>
        /// <param name="lpmi">MONITORINFO structure that receives monitor information</param>
        /// <returns>True if successful, false if monitor handle is invalid</returns>
        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        /// <summary>
        /// Retrieves the display monitor handle that contains or is nearest to a specified window.
        /// Critical for determining which monitor a window belongs to for accurate notification routing.
        /// Uses intelligent fallback logic to handle edge cases like minimized or off-screen windows.
        /// </summary>
        /// <param name="hwnd">Handle to the window</param>
        /// <param name="dwFlags">Determines function behavior for edge cases (use MONITOR_DEFAULTTONEAREST)</param>
        /// <returns>Handle to the display monitor, never returns null with NEAREST flag</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        /// Retrieves a window handle by searching for a window with specified class name and/or window name.
        /// Used for finding specific system windows or applications by their known characteristics.
        /// Supports both exact class name matching and window title searching for flexible window location.
        /// </summary>
        /// <param name="lpClassName">Class name to search for (null to ignore class name)</param>
        /// <param name="lpWindowName">Window name/title to search for (null to ignore window name)</param>
        /// <returns>Handle to the window if found, IntPtr.Zero if no matching window exists</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Delegate type for monitor enumeration callback function used with EnumDisplayMonitors.
        /// Called once for each display monitor attached to the desktop during enumeration.
        /// Return true to continue enumeration, false to stop processing additional monitors.
        /// </summary>
        /// <param name="hMonitor">Handle to the current monitor being enumerated</param>
        /// <param name="hdcMonitor">Device context for the monitor (usually not used)</param>
        /// <param name="lprcMonitor">Rectangle representing the monitor's display area</param>
        /// <param name="dwData">Application-defined data passed from EnumDisplayMonitors call</param>
        /// <returns>True to continue enumeration, false to stop</returns>
        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        /// <summary>
        /// Constant for MonitorFromWindow function specifying that the nearest monitor should be returned.
        /// Ensures reliable monitor detection even for windows that are minimized or positioned off-screen.
        /// Critical for robust notification routing that handles edge cases gracefully.
        /// </summary>
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        /// <summary>
        /// Windows RECT structure representing a rectangle with integer coordinates.
        /// Used throughout the application for window bounds, monitor areas, and positioning calculations.
        /// Provides convenient Width and Height properties for size calculations in notification positioning.
        /// Sequential layout ensures proper marshaling with Windows API calls.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            /// <summary>
            /// Left edge X coordinate of the rectangle
            /// </summary>
            public int Left;

            /// <summary>
            /// Top edge Y coordinate of the rectangle
            /// </summary>
            public int Top;

            /// <summary>
            /// Right edge X coordinate of the rectangle (exclusive)
            /// </summary>
            public int Right;

            /// <summary>
            /// Bottom edge Y coordinate of the rectangle (exclusive)
            /// </summary>
            public int Bottom;

            /// <summary>
            /// Calculated width of the rectangle (Right - Left)
            /// </summary>
            public int Width => Right - Left;

            /// <summary>
            /// Calculated height of the rectangle (Bottom - Top)
            /// </summary>
            public int Height => Bottom - Top;
        }

        /// <summary>
        /// Windows MONITORINFO structure containing comprehensive display monitor information.
        /// Retrieved by GetMonitorInfo API call to obtain monitor geometry, work area, and capabilities.
        /// Essential data structure for multi-monitor notification positioning and display management.
        /// Sequential layout ensures correct marshaling with Windows API calls.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            /// <summary>
            /// Size of the structure in bytes (must be set before calling GetMonitorInfo)
            /// </summary>
            public uint cbSize;

            /// <summary>
            /// Full monitor rectangle including areas behind taskbar and system UI elements
            /// </summary>
            public RECT rcMonitor;

            /// <summary>
            /// Work area rectangle excluding taskbar, docked windows, and system UI elements.
            /// This is the usable area for application windows and notification positioning.
            /// </summary>
            public RECT rcWork;

            /// <summary>
            /// Monitor capability flags (use MONITORINFOF_PRIMARY to check for primary monitor)
            /// </summary>
            public uint dwFlags;

            /// <summary>
            /// Constant flag indicating this monitor is the primary display (contains Start button and taskbar)
            /// </summary>
            public const uint MONITORINFOF_PRIMARY = 0x00000001;
        }
    }
}