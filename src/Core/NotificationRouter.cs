using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    public class NotificationData
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string AppName { get; set; }
        public string ProcessName { get; set; }
        public MonitorInfo TargetMonitor { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class NotificationRouter
    {
        private readonly MonitorManager _monitorManager;
        private readonly WindowTracker _windowTracker;
        private readonly Dictionary<string, MonitorInfo> _appMonitorMappings;

        public event EventHandler<NotificationRoutedEventArgs> NotificationRouted;

        public NotificationRouter(MonitorManager monitorManager, WindowTracker windowTracker)
        {
            _monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
            _windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
            _appMonitorMappings = new Dictionary<string, MonitorInfo>();
        }

        public Task<bool> RouteNotificationAsync(NotificationData notification)
        {
            try
            {
                // Determine target monitor for mapping purposes
                var targetMonitor = notification.TargetMonitor ?? DetermineTargetMonitor(notification);
                notification.TargetMonitor = targetMonitor;

                // Windows API hook will handle positioning, just log the routing
                DebugLogger.WriteLine($"Notification routed: {notification.Title} -> Monitor {targetMonitor?.Index}");

                OnNotificationRouted(notification);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Notification routing failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private MonitorInfo DetermineTargetMonitor(NotificationData notification)
        {
            DebugLogger.WriteLine($"Determining target monitor for notification: {notification.Title} from {notification.ProcessName}");

            // 1. First check which monitor the application is currently on
            if (!string.IsNullOrEmpty(notification.ProcessName))
            {
                var currentMonitor = _windowTracker.GetProcessMonitor(notification.ProcessName);
                if (currentMonitor != null)
                {
                    DebugLogger.WriteLine($"Found current monitor for process {notification.ProcessName}: Monitor {currentMonitor.Index}");
                    // Update monitor mapping for application
                    _appMonitorMappings[notification.ProcessName] = currentMonitor;
                    return currentMonitor;
                }
                else
                {
                    DebugLogger.WriteLine($"No current monitor found for process: {notification.ProcessName}");
                }
            }

            // 2. Check previously recorded monitor mapping
            if (!string.IsNullOrEmpty(notification.ProcessName) &&
                _appMonitorMappings.TryGetValue(notification.ProcessName, out var mappedMonitor))
            {
                DebugLogger.WriteLine($"Using mapped monitor for process {notification.ProcessName}: Monitor {mappedMonitor.Index}");
                return mappedMonitor;
            }

            // 3. Check active window's monitor
            var activeWindow = _windowTracker.GetCurrentActiveWindow();
            if (activeWindow?.Monitor != null)
            {
                DebugLogger.WriteLine($"Using active window monitor: Monitor {activeWindow.Monitor.Index}");
                return activeWindow.Monitor;
            }

            // 4. Use primary monitor as default
            var primaryMonitor = _monitorManager.GetPrimaryMonitor();
            DebugLogger.WriteLine($"Falling back to primary monitor: Monitor {primaryMonitor?.Index}");
            return primaryMonitor;
        }








        public void UpdateAppMonitorMapping(string processName, MonitorInfo monitor)
        {
            if (!string.IsNullOrEmpty(processName) && monitor != null)
            {
                _appMonitorMappings[processName] = monitor;
            }
        }

        public MonitorInfo GetAppMonitorMapping(string processName)
        {
            return _appMonitorMappings.TryGetValue(processName, out var monitor) ? monitor : null;
        }

        public void ClearAppMonitorMapping(string processName)
        {
            if (!string.IsNullOrEmpty(processName))
            {
                _appMonitorMappings.Remove(processName);
            }
        }

        public void ClearAllMappings()
        {
            _appMonitorMappings.Clear();
        }

        public Dictionary<string, MonitorInfo> GetAllMappings()
        {
            return new Dictionary<string, MonitorInfo>(_appMonitorMappings);
        }

        private void OnNotificationRouted(NotificationData notification)
        {
            NotificationRouted?.Invoke(this, new NotificationRoutedEventArgs(notification));
        }

        public void HandleWindowMoved(object sender, WindowMovedEventArgs e)
        {
            // Update app-monitor mapping when window is moved
            if (e.Window != null && e.NewMonitor != null)
            {
                UpdateAppMonitorMapping(e.Window.ProcessName, e.NewMonitor);
            }
        }
    }


    public class NotificationRoutedEventArgs : EventArgs
    {
        public NotificationData Notification { get; }

        public NotificationRoutedEventArgs(NotificationData notification)
        {
            Notification = notification;
        }
    }
}