using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    /// <summary>
    /// Data transfer object containing comprehensive information about a tracked window.
    /// Used to maintain state and monitor changes for windows across multiple monitors.
    /// </summary>
    public class WindowInfo
    {
        /// <summary>
        /// Windows handle (HWND) uniquely identifying this window
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Current title/caption text of the window
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Name of the process that owns this window (e.g., "notepad", "chrome")
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// Windows process ID that owns this window
        /// </summary>
        public uint ProcessId { get; set; }

        /// <summary>
        /// Current window rectangle coordinates (position and size)
        /// </summary>
        public Win32Helper.RECT Rectangle { get; set; }

        /// <summary>
        /// Monitor that currently contains this window (or primary monitor if unknown)
        /// </summary>
        public MonitorInfo Monitor { get; set; }

        /// <summary>
        /// Timestamp when this window information was last updated
        /// </summary>
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Tracks active windows and their positions across multiple monitors in real-time.
    /// Monitors window movement, focus changes, and maintains position history for minimized windows.
    /// Uses a periodic timer (500ms intervals) to efficiently track window state changes.
    /// </summary>
    public class WindowTracker
    {
        /// <summary>
        /// Reference to monitor manager for determining which monitor contains each window
        /// </summary>
        private readonly MonitorManager _monitorManager;

        /// <summary>
        /// Dictionary of tracked windows, keyed by process ID for fast lookup
        /// </summary>
        private readonly Dictionary<uint, WindowInfo> _trackedWindows;

        /// <summary>
        /// Timer that periodically checks for window position and focus changes (500ms intervals)
        /// </summary>
        private readonly Timer _trackingTimer;

        /// <summary>
        /// Thread synchronization object to protect shared data structures
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// Cache of last known positions for minimized windows to enable accurate monitor tracking.
        /// Windows places minimized windows at (-32000, -32000) coordinates, so we need to remember
        /// their actual positions before minimization for correct notification routing.
        /// </summary>
        private readonly Dictionary<IntPtr, Win32Helper.RECT> _lastKnownPositions = new();

        /// <summary>
        /// Event fired when a tracked window moves from one monitor to another
        /// </summary>
        public event EventHandler<WindowMovedEventArgs> WindowMoved;

        /// <summary>
        /// Event fired when window focus changes (user switches between applications)
        /// </summary>
        public event EventHandler<WindowFocusChangedEventArgs> WindowFocusChanged;

        /// <summary>
        /// Handle to the last foreground window to detect focus changes
        /// </summary>
        private IntPtr _lastForegroundWindow = IntPtr.Zero;

        /// <summary>
        /// Windows API function to determine if a window is minimized/iconified
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// Initializes a new WindowTracker with required dependencies and starts periodic tracking.
        /// Sets up a 500ms timer to balance responsiveness with system performance.
        /// </summary>
        /// <param name="monitorManager">Monitor manager for determining window-to-monitor associations</param>
        /// <exception cref="ArgumentNullException">Thrown if monitorManager is null</exception>
        public WindowTracker(MonitorManager monitorManager)
        {
            _monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
            _trackedWindows = new Dictionary<uint, WindowInfo>();

            // Initialize periodic tracking with 500ms interval (balance between responsiveness and performance)
            _trackingTimer = new Timer(TrackActiveWindows, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        /// <summary>
        /// Starts or resumes window tracking with the configured 500ms interval.
        /// Called automatically by constructor but can be used to resume after stopping.
        /// </summary>
        public void StartTracking()
        {
            _trackingTimer?.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        /// <summary>
        /// Stops window tracking to reduce system resource usage.
        /// Useful when the application is minimized or tracking is temporarily not needed.
        /// </summary>
        public void StopTracking()
        {
            _trackingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Gets information about the currently active/foreground window.
        /// This is the window that currently has keyboard focus and user attention.
        /// </summary>
        /// <returns>WindowInfo for the active window, or null if no window is active</returns>
        public WindowInfo GetCurrentActiveWindow()
        {
            IntPtr foregroundWindow = Win32Helper.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return null;

            return GetWindowInfo(foregroundWindow);
        }

        /// <summary>
        /// Retrieves cached window information for a specific process ID.
        /// Thread-safe method that returns the most recently tracked window for the given process.
        /// </summary>
        /// <param name="processId">Windows process ID to look up</param>
        /// <returns>WindowInfo if the process is being tracked, null otherwise</returns>
        public WindowInfo GetWindowByProcessId(uint processId)
        {
            lock (_lockObject)
            {
                return _trackedWindows.TryGetValue(processId, out var windowInfo) ? windowInfo : null;
            }
        }

        /// <summary>
        /// Gets a snapshot of all currently tracked windows.
        /// Thread-safe method that returns a copy of the tracked windows collection.
        /// </summary>
        /// <returns>List containing copies of all tracked WindowInfo objects</returns>
        public List<WindowInfo> GetAllTrackedWindows()
        {
            lock (_lockObject)
            {
                return new List<WindowInfo>(_trackedWindows.Values);
            }
        }

        /// <summary>
        /// Determines which monitor contains the specified window, with special handling for minimized windows.
        /// Uses cached position data to accurately track minimized windows that Windows moves to (-32000, -32000).
        /// This is critical for correct notification routing when applications are minimized to the taskbar.
        /// </summary>
        /// <param name="windowHandle">Window handle to locate</param>
        /// <returns>MonitorInfo containing the window, or primary monitor as fallback</returns>
        public MonitorInfo GetWindowMonitor(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return _monitorManager.GetPrimaryMonitor();

            if (Win32Helper.GetWindowRect(windowHandle, out var rect))
            {
                // CRITICAL: Handle minimized windows correctly
                // Windows moves minimized windows to coordinates around (-32000, -32000)
                // We need to use their last known position before minimization
                if (IsIconic(windowHandle) || rect.Left < -30000 || rect.Top < -30000)
                {
                    // Retrieve cached position from before the window was minimized
                    if (_lastKnownPositions.TryGetValue(windowHandle, out var lastRect))
                    {
                        return _monitorManager.GetMonitorContainingWindow(lastRect);
                    }
                    // No cached position available, use primary monitor as safe fallback
                    return _monitorManager.GetPrimaryMonitor();
                }
                else
                {
                    // Window is visible: cache its current position for future minimized state tracking
                    _lastKnownPositions[windowHandle] = rect;
                    return _monitorManager.GetMonitorContainingWindow(rect);
                }
            }

            return _monitorManager.GetPrimaryMonitor();
        }

        /// <summary>
        /// Finds the monitor containing any window belonging to the specified process name.
        /// Useful for determining where an application is located when multiple instances may exist.
        /// </summary>
        /// <param name="processName">Name of the process to locate (e.g., "notepad", "chrome")</param>
        /// <returns>MonitorInfo containing the process's main window, or primary monitor if not found</returns>
        public MonitorInfo GetProcessMonitor(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                // Look for processes with visible main windows
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return GetWindowMonitor(process.MainWindowHandle);
                }
            }
            // No visible windows found for this process name
            return _monitorManager.GetPrimaryMonitor();
        }

        /// <summary>
        /// Main tracking method called by timer every 500ms.
        /// Monitors foreground window changes and updates positions of all tracked windows.
        /// Designed for efficiency to minimize system impact during continuous operation.
        /// </summary>
        /// <param name="state">Timer state parameter (not used)</param>
        private void TrackActiveWindows(object state)
        {
            try
            {
                IntPtr foregroundWindow = Win32Helper.GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return;

                // Detect and handle foreground window changes (user switching between applications)
                if (foregroundWindow != _lastForegroundWindow)
                {
                    var windowInfo = GetWindowInfo(foregroundWindow);
                    if (windowInfo != null)
                    {
                        // Notify subscribers about focus change
                        OnWindowFocusChanged(windowInfo);
                        // Add or update this window in our tracking dictionary
                        UpdateTrackedWindow(windowInfo);
                    }
                    _lastForegroundWindow = foregroundWindow;
                }

                // Update positions of all currently tracked windows to detect monitor changes
                UpdateTrackedWindowPositions();
            }
            catch (Exception ex)
            {
                // Catch and log any exceptions to prevent timer crashes
                Debug.WriteLine($"WindowTracker error: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts comprehensive information about a window from its handle.
        /// Gathers title, process information, position, and monitor association.
        /// Used internally to create WindowInfo objects for tracking and event handling.
        /// </summary>
        /// <param name="windowHandle">Windows handle (HWND) of the window to analyze</param>
        /// <returns>Complete WindowInfo object, or null if window information cannot be retrieved</returns>
        private WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return null;

            try
            {
                // Extract window title/caption text
                var titleBuilder = new StringBuilder(256);
                Win32Helper.GetWindowText(windowHandle, titleBuilder, titleBuilder.Capacity);
                string title = titleBuilder.ToString();

                // Get the process ID that owns this window
                Win32Helper.GetWindowThreadProcessId(windowHandle, out uint processId);

                // Get current window position and size
                Win32Helper.GetWindowRect(windowHandle, out var rect);

                // Determine which monitor contains this window
                var monitor = _monitorManager.GetMonitorContainingWindow(rect);

                // Resolve process ID to process name for identification
                string processName = "";
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                }
                catch
                {
                    // Process may have ended while we were looking it up
                    processName = "Unknown";
                }

                // Create comprehensive window information object
                return new WindowInfo
                {
                    Handle = windowHandle,
                    Title = title,
                    ProcessName = processName,
                    ProcessId = processId,
                    Rectangle = rect,
                    Monitor = monitor,
                    LastUpdate = DateTime.Now
                };
            }
            catch
            {
                // Return null if any API calls fail (window may have closed)
                return null;
            }
        }

        /// <summary>
        /// Updates or adds a window to the tracking dictionary and detects monitor changes.
        /// Thread-safe method that compares new window information with existing data to detect moves.
        /// Fires WindowMoved event when a window changes monitors.
        /// </summary>
        /// <param name="windowInfo">Current window information to update or add</param>
        private void UpdateTrackedWindow(WindowInfo windowInfo)
        {
            if (windowInfo == null)
                return;

            lock (_lockObject)
            {
                // Check if we're already tracking this process and if it moved monitors
                if (_trackedWindows.TryGetValue(windowInfo.ProcessId, out var existingWindow))
                {
                    // Detect monitor changes by comparing monitor handles
                    if (existingWindow.Monitor?.Handle != windowInfo.Monitor?.Handle)
                    {
                        // Window moved to a different monitor - notify subscribers
                        OnWindowMoved(windowInfo, existingWindow.Monitor, windowInfo.Monitor);
                    }
                }

                // Update our tracking dictionary with the latest window information
                _trackedWindows[windowInfo.ProcessId] = windowInfo;
            }
        }

        /// <summary>
        /// Periodically updates positions of all tracked windows to detect monitor changes.
        /// Called during each timer cycle to ensure we catch windows that move between monitors.
        /// Removes windows that no longer exist from the tracking dictionary.
        /// </summary>
        private void UpdateTrackedWindowPositions()
        {
            lock (_lockObject)
            {
                // Create a copy of the keys to avoid collection modification during iteration
                var windowsToUpdate = new List<uint>(_trackedWindows.Keys);

                foreach (var processId in windowsToUpdate)
                {
                    var existingWindow = _trackedWindows[processId];

                    // Check if the window still exists and get its current position
                    if (Win32Helper.GetWindowRect(existingWindow.Handle, out var currentRect))
                    {
                        var currentMonitor = _monitorManager.GetMonitorContainingWindow(currentRect);

                        // Compare current monitor with cached monitor to detect changes
                        if (currentMonitor?.Handle != existingWindow.Monitor?.Handle)
                        {
                            // Create updated window info with new position and monitor
                            var updatedWindow = new WindowInfo
                            {
                                Handle = existingWindow.Handle,
                                Title = existingWindow.Title,
                                ProcessName = existingWindow.ProcessName,
                                ProcessId = existingWindow.ProcessId,
                                Rectangle = currentRect,
                                Monitor = currentMonitor,
                                LastUpdate = DateTime.Now
                            };

                            // Notify subscribers of the monitor change
                            OnWindowMoved(updatedWindow, existingWindow.Monitor, currentMonitor);
                            // Update our tracking dictionary
                            _trackedWindows[processId] = updatedWindow;
                        }
                    }
                    else
                    {
                        // Window no longer exists (closed), remove from tracking
                        _trackedWindows.Remove(processId);
                    }
                }
            }
        }

        /// <summary>
        /// Raises the WindowMoved event to notify subscribers when a window changes monitors.
        /// Used by notification routing logic to learn user preferences and update mappings.
        /// </summary>
        /// <param name="window">Window that moved</param>
        /// <param name="oldMonitor">Monitor the window moved from</param>
        /// <param name="newMonitor">Monitor the window moved to</param>
        private void OnWindowMoved(WindowInfo window, MonitorInfo oldMonitor, MonitorInfo newMonitor)
        {
            WindowMoved?.Invoke(this, new WindowMovedEventArgs(window, oldMonitor, newMonitor));
        }

        /// <summary>
        /// Raises the WindowFocusChanged event when the foreground window changes.
        /// Used to track user attention and update active window information.
        /// </summary>
        /// <param name="window">Window that gained focus</param>
        private void OnWindowFocusChanged(WindowInfo window)
        {
            WindowFocusChanged?.Invoke(this, new WindowFocusChangedEventArgs(window));
        }

        /// <summary>
        /// Implements IDisposable pattern to properly clean up the tracking timer.
        /// Stops window tracking and releases system resources when the tracker is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _trackingTimer?.Dispose();
        }
    }

    /// <summary>
    /// Event arguments for window movement notifications.
    /// Contains information about which window moved and its old/new monitor locations.
    /// Used by NotificationRouter to learn user preferences for application placement.
    /// </summary>
    public class WindowMovedEventArgs : EventArgs
    {
        /// <summary>
        /// Window that moved between monitors
        /// </summary>
        public WindowInfo Window { get; }

        /// <summary>
        /// Monitor the window was previously on
        /// </summary>
        public MonitorInfo OldMonitor { get; }

        /// <summary>
        /// Monitor the window moved to
        /// </summary>
        public MonitorInfo NewMonitor { get; }

        /// <summary>
        /// Initializes event arguments with window movement information
        /// </summary>
        /// <param name="window">Window that moved</param>
        /// <param name="oldMonitor">Previous monitor location</param>
        /// <param name="newMonitor">New monitor location</param>
        public WindowMovedEventArgs(WindowInfo window, MonitorInfo oldMonitor, MonitorInfo newMonitor)
        {
            Window = window;
            OldMonitor = oldMonitor;
            NewMonitor = newMonitor;
        }
    }

    /// <summary>
    /// Event arguments for window focus change notifications.
    /// Contains information about the window that gained focus (became active).
    /// Used to track user attention and update application activity state.
    /// </summary>
    public class WindowFocusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Window that gained focus and is now active
        /// </summary>
        public WindowInfo Window { get; }

        /// <summary>
        /// Initializes event arguments with focus change information
        /// </summary>
        /// <param name="window">Window that gained focus</param>
        public WindowFocusChangedEventArgs(WindowInfo window)
        {
            Window = window;
        }
    }
}