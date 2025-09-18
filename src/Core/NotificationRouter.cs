using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    /// <summary>
    /// Data transfer object containing information about a notification to be routed.
    /// Used to pass notification details between components and determine routing logic.
    /// </summary>
    public class NotificationData
    {
        /// <summary>
        /// The title/subject of the notification
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The main body text content of the notification
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The display name of the application that sent the notification
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// The process name of the application that sent the notification (used for routing logic)
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// The target monitor where this notification should be displayed
        /// </summary>
        public MonitorInfo TargetMonitor { get; set; }

        /// <summary>
        /// When this notification was created/received
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Core component responsible for determining which monitor a notification should be displayed on.
    /// Uses intelligent routing logic based on application location, window tracking, and cached mappings.
    /// </summary>
    public class NotificationRouter
    {
        /// <summary>
        /// Reference to the monitor manager for getting monitor information
        /// </summary>
        private readonly MonitorManager _monitorManager;

        /// <summary>
        /// Reference to the window tracker for getting current window positions
        /// </summary>
        private readonly WindowTracker _windowTracker;

        /// <summary>
        /// Cache of application process names to their preferred monitors for faster routing
        /// </summary>
        private readonly Dictionary<string, MonitorInfo> _appMonitorMappings;

        /// <summary>
        /// Event fired when a notification has been successfully routed to a target monitor
        /// </summary>
        public event EventHandler<NotificationRoutedEventArgs> NotificationRouted;

        /// <summary>
        /// Initializes a new NotificationRouter with required dependencies.
        /// Sets up the routing system for intelligent notification placement.
        /// </summary>
        /// <param name="monitorManager">Monitor manager for accessing monitor information</param>
        /// <param name="windowTracker">Window tracker for getting current window positions</param>
        /// <exception cref="ArgumentNullException">Thrown if either parameter is null</exception>
        public NotificationRouter(MonitorManager monitorManager, WindowTracker windowTracker)
        {
            _monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
            _windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
            _appMonitorMappings = new Dictionary<string, MonitorInfo>();
        }

        /// <summary>
        /// Routes a notification to the appropriate monitor using intelligent targeting logic.
        /// This method determines the best monitor for the notification and updates internal mappings.
        /// </summary>
        /// <param name="notification">Notification data containing app info and content</param>
        /// <returns>Task with true if routing succeeded, false if an error occurred</returns>
        public Task<bool> RouteNotificationAsync(NotificationData notification)
        {
            try
            {
                // Determine the best target monitor if not already specified
                var targetMonitor = notification.TargetMonitor ?? DetermineTargetMonitor(notification);
                notification.TargetMonitor = targetMonitor;

                // Log the routing decision for debugging purposes
                // Note: Actual positioning is handled by WindowsAPIHook component
                DebugLogger.WriteLine($"Notification routed: {notification.Title} -> Monitor {targetMonitor?.Index}");

                // Notify subscribers that routing is complete
                OnNotificationRouted(notification);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Notification routing failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Determines the best target monitor for a notification using a priority-based algorithm.
        /// Uses a fallback chain: current app location -> cached mapping -> active window -> primary monitor.
        /// </summary>
        /// <param name="notification">Notification containing process info for routing logic</param>
        /// <returns>MonitorInfo object representing the best target monitor</returns>
        private MonitorInfo DetermineTargetMonitor(NotificationData notification)
        {
            DebugLogger.WriteLine($"Determining target monitor for notification: {notification.Title} from {notification.ProcessName}");

            // Priority 1: Check which monitor the application is currently active on
            if (!string.IsNullOrEmpty(notification.ProcessName))
            {
                var currentMonitor = _windowTracker.GetProcessMonitor(notification.ProcessName);
                if (currentMonitor != null)
                {
                    DebugLogger.WriteLine($"Found current monitor for process {notification.ProcessName}: Monitor {currentMonitor.Index}");
                    // Cache this mapping for future notifications from this app
                    _appMonitorMappings[notification.ProcessName] = currentMonitor;
                    return currentMonitor;
                }
                else
                {
                    DebugLogger.WriteLine($"No current monitor found for process: {notification.ProcessName}");
                }
            }

            // Priority 2: Use previously cached monitor mapping for this application
            if (!string.IsNullOrEmpty(notification.ProcessName) &&
                _appMonitorMappings.TryGetValue(notification.ProcessName, out var mappedMonitor))
            {
                DebugLogger.WriteLine($"Using mapped monitor for process {notification.ProcessName}: Monitor {mappedMonitor.Index}");
                return mappedMonitor;
            }

            // Priority 3: Use the monitor where the currently active window is located
            var activeWindow = _windowTracker.GetCurrentActiveWindow();
            if (activeWindow?.Monitor != null)
            {
                DebugLogger.WriteLine($"Using active window monitor: Monitor {activeWindow.Monitor.Index}");
                return activeWindow.Monitor;
            }

            // Priority 4: Fall back to primary monitor as safe default
            var primaryMonitor = _monitorManager.GetPrimaryMonitor();
            DebugLogger.WriteLine($"Falling back to primary monitor: Monitor {primaryMonitor?.Index}");
            return primaryMonitor;
        }





        /// <summary>
        /// Manually updates the monitor mapping for a specific application process.
        /// This is useful for learning user preferences or correcting routing decisions.
        /// </summary>
        /// <param name="processName">Name of the process to update mapping for</param>
        /// <param name="monitor">Monitor to associate with this process</param>
        public void UpdateAppMonitorMapping(string processName, MonitorInfo monitor)
        {
            if (!string.IsNullOrEmpty(processName) && monitor != null)
            {
                _appMonitorMappings[processName] = monitor;
            }
        }

        /// <summary>
        /// Retrieves the cached monitor mapping for a specific application process.
        /// </summary>
        /// <param name="processName">Name of the process to look up</param>
        /// <returns>MonitorInfo if mapping exists, null otherwise</returns>
        public MonitorInfo GetAppMonitorMapping(string processName)
        {
            return _appMonitorMappings.TryGetValue(processName, out var monitor) ? monitor : null;
        }

        /// <summary>
        /// Removes the cached monitor mapping for a specific application process.
        /// Forces the router to re-determine the best monitor for future notifications.
        /// </summary>
        /// <param name="processName">Name of the process to clear mapping for</param>
        public void ClearAppMonitorMapping(string processName)
        {
            if (!string.IsNullOrEmpty(processName))
            {
                _appMonitorMappings.Remove(processName);
            }
        }

        /// <summary>
        /// Clears all cached application-to-monitor mappings.
        /// Useful for resetting the learning system or when monitor configuration changes.
        /// </summary>
        public void ClearAllMappings()
        {
            _appMonitorMappings.Clear();
        }

        /// <summary>
        /// Gets a copy of all current application-to-monitor mappings.
        /// Useful for debugging or persisting routing preferences.
        /// </summary>
        /// <returns>Dictionary copy of all current mappings</returns>
        public Dictionary<string, MonitorInfo> GetAllMappings()
        {
            return new Dictionary<string, MonitorInfo>(_appMonitorMappings);
        }

        /// <summary>
        /// Raises the NotificationRouted event to notify subscribers that routing is complete.
        /// </summary>
        /// <param name="notification">The notification that was routed</param>
        private void OnNotificationRouted(NotificationData notification)
        {
            NotificationRouted?.Invoke(this, new NotificationRoutedEventArgs(notification));
        }

        /// <summary>
        /// Event handler for window moved events from WindowTracker.
        /// Automatically updates the application-to-monitor mapping when windows are moved.
        /// This helps the router learn user preferences dynamically.
        /// </summary>
        /// <param name="sender">Event sender (WindowTracker)</param>
        /// <param name="e">Event arguments containing window and monitor information</param>
        public void HandleWindowMoved(object sender, WindowMovedEventArgs e)
        {
            // Learn from user behavior: when they move a window to a different monitor,
            // update our mapping so future notifications go to the new location
            if (e.Window != null && e.NewMonitor != null)
            {
                UpdateAppMonitorMapping(e.Window.ProcessName, e.NewMonitor);
            }
        }
    }

    /// <summary>
    /// Event arguments for notification routing completion events.
    /// Contains the routed notification data for subscribers to process.
    /// </summary>
    public class NotificationRoutedEventArgs : EventArgs
    {
        /// <summary>
        /// The notification that was successfully routed, including target monitor information
        /// </summary>
        public NotificationData Notification { get; }

        /// <summary>
        /// Initializes event arguments with the routed notification data
        /// </summary>
        /// <param name="notification">The notification that was routed</param>
        public NotificationRoutedEventArgs(NotificationData notification)
        {
            Notification = notification;
        }
    }
}