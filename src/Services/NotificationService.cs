using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsNotificationManager.src.Core;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Services
{
    public class NotificationService
    {
        private readonly MonitorManager _monitorManager;
        private readonly WindowTracker _windowTracker;
        private readonly NotificationRouter _notificationRouter;
        private readonly WindowsAPIHook _apiHook;
        private bool _isRunning;

        public event EventHandler<NotificationReceivedEventArgs> NotificationReceived;

        public NotificationService()
        {
            _monitorManager = new MonitorManager();
            _windowTracker = new WindowTracker(_monitorManager);
            _notificationRouter = new NotificationRouter(_monitorManager, _windowTracker);
            _apiHook = new WindowsAPIHook(_monitorManager, _windowTracker);

            // Wire up events
            _windowTracker.WindowMoved += _notificationRouter.HandleWindowMoved;
            _notificationRouter.NotificationRouted += OnNotificationRouted;
        }

        public void Start()
        {
            if (_isRunning)
                return;

            try
            {
                _monitorManager.StartMonitoring();
                _windowTracker.StartTracking();

                // Start listening for system notifications
                StartListeningForSystemNotifications();

                // Start Windows API hooking for notifications
                _apiHook.StartHooking();

                _isRunning = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationService start error: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                _monitorManager.StopMonitoring();
                _windowTracker.StopTracking();
                StopListeningForSystemNotifications();

                // Stop Windows API hooking
                _apiHook.StopHooking();

                _isRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationService stop error: {ex.Message}");
            }
        }

        private void StartListeningForSystemNotifications()
        {
            // Integration with Windows notification system
            // This part will require more advanced Windows APIs
            // API hook will handle all notification interception
        }

        private void StopListeningForSystemNotifications()
        {
            // Stop listening for notifications
        }

        public async Task<bool> ProcessNotificationAsync(string title, string message, string appName = null, string processName = null)
        {
            try
            {
                var notification = new NotificationData
                {
                    Title = title,
                    Message = message,
                    AppName = appName ?? "Unknown App",
                    ProcessName = processName ?? ExtractProcessNameFromAppName(appName),
                    Timestamp = DateTime.Now
                };

                OnNotificationReceived(notification);

                return await _notificationRouter.RouteNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Process notification error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProcessNotificationAsync(NotificationData notification)
        {
            try
            {
                OnNotificationReceived(notification);
                return await _notificationRouter.RouteNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessNotificationAsync error: {ex.Message}");
                return false;
            }
        }

        private string ExtractProcessNameFromAppName(string appName)
        {
            if (string.IsNullOrEmpty(appName))
                return "unknown";

            // Simple translation table (should be more comprehensive in real application)
            var processMapping = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "WhatsApp", "WhatsApp" },
                { "Discord", "Discord" },
                { "Telegram", "Telegram" },
                { "Microsoft Teams", "Teams" },
                { "Slack", "slack" },
                { "Chrome", "chrome" },
                { "Firefox", "firefox" },
                { "Spotify", "Spotify" },
                { "Visual Studio Code", "Code" },
                { "Microsoft Outlook", "OUTLOOK" }
            };

            return processMapping.TryGetValue(appName, out var processName)
                ? processName
                : appName.Replace(" ", "").ToLower();
        }


        public MonitorInfo GetAppMonitor(string processName)
        {
            return _notificationRouter.GetAppMonitorMapping(processName);
        }

        public void SetAppMonitor(string processName, MonitorInfo monitor)
        {
            _notificationRouter.UpdateAppMonitorMapping(processName, monitor);
        }

        public System.Collections.Generic.List<MonitorInfo> GetAvailableMonitors()
        {
            return _monitorManager.GetAllMonitors();
        }

        public System.Collections.Generic.List<WindowInfo> GetActiveWindows()
        {
            return _windowTracker.GetAllTrackedWindows();
        }

        private void OnNotificationReceived(NotificationData notification)
        {
            NotificationReceived?.Invoke(this, new NotificationReceivedEventArgs(notification));
        }

        private void OnNotificationRouted(object sender, NotificationRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Notification routed: {e.Notification.Title} -> Monitor {e.Notification.TargetMonitor?.Index}");
        }


        public void Dispose()
        {
            Stop();
            _windowTracker?.Dispose();
        }
    }

    public class NotificationReceivedEventArgs : EventArgs
    {
        public NotificationData Notification { get; }

        public NotificationReceivedEventArgs(NotificationData notification)
        {
            Notification = notification;
        }
    }
}