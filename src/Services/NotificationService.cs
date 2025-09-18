using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsNotificationManager.src.Core;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Services
{
    /// <summary>
    /// Central orchestrator service that coordinates all notification management components.
    /// Acts as the main entry point for the notification routing system and manages the lifecycle
    /// of monitor detection, window tracking, notification routing, and Windows API hooking.
    /// </summary>
    public class NotificationService
    {
        /// <summary>
        /// Manages multiple monitor detection and configuration changes
        /// </summary>
        private readonly MonitorManager _monitorManager;

        /// <summary>
        /// Tracks active windows and their positions across monitors
        /// </summary>
        private readonly WindowTracker _windowTracker;

        /// <summary>
        /// Handles intelligent routing logic for notifications based on application locations
        /// </summary>
        private readonly NotificationRouter _notificationRouter;

        /// <summary>
        /// Low-level Windows API hook for intercepting and repositioning notification windows
        /// </summary>
        private readonly WindowsAPIHook _apiHook;

        /// <summary>
        /// Flag indicating whether the notification service is currently active
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// Event fired when a notification is received and begins processing
        /// </summary>
        public event EventHandler<NotificationReceivedEventArgs> NotificationReceived;

        /// <summary>
        /// Initializes the NotificationService and sets up all core components with proper dependencies.
        /// Establishes event wiring between components to enable communication and learning capabilities.
        /// </summary>
        public NotificationService()
        {
            // Initialize core components in dependency order
            _monitorManager = new MonitorManager();
            _windowTracker = new WindowTracker(_monitorManager);
            _notificationRouter = new NotificationRouter(_monitorManager, _windowTracker);
            _apiHook = new WindowsAPIHook(_monitorManager, _windowTracker);

            // Wire up inter-component communication events
            // When windows move, the router learns new app-to-monitor preferences
            _windowTracker.WindowMoved += _notificationRouter.HandleWindowMoved;
            // Track routing completions for debugging and monitoring
            _notificationRouter.NotificationRouted += OnNotificationRouted;
        }

        /// <summary>
        /// Starts the notification service and activates all monitoring components.
        /// Must be called with administrator privileges to enable Windows API hooks.
        /// Initializes the complete notification interception and routing pipeline.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            try
            {
                // Start monitor configuration monitoring (detects display changes)
                _monitorManager.StartMonitoring();

                // Begin tracking window positions and focus changes (500ms intervals)
                _windowTracker.StartTracking();

                // Initialize system notification listeners (placeholder for future enhancement)
                StartListeningForSystemNotifications();

                // CRITICAL: Start Windows API hooking for real-time notification interception
                // This requires administrator privileges to function
                _apiHook.StartHooking();

                _isRunning = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationService start error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops the notification service and deactivates all monitoring components.
        /// Properly shuts down all Windows API hooks and releases system resources.
        /// Can be called safely multiple times.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                // Stop monitor configuration monitoring
                _monitorManager.StopMonitoring();

                // Stop window position and focus tracking
                _windowTracker.StopTracking();

                // Stop system notification listeners
                StopListeningForSystemNotifications();

                // CRITICAL: Stop Windows API hooking to release system hooks
                _apiHook.StopHooking();

                _isRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationService stop error: {ex.Message}");
            }
        }

        /// <summary>
        /// Placeholder method for initializing system notification listeners.
        /// Currently, all notification interception is handled by the Windows API hook.
        /// This method could be expanded in the future to integrate with additional notification APIs.
        /// </summary>
        private void StartListeningForSystemNotifications()
        {
            // Future enhancement: Integration with Windows notification system
            // Currently the WindowsAPIHook handles all notification interception
            // This could be expanded to include:
            // - Windows Runtime Toast Notification APIs
            // - WNS (Windows Notification Service) integration
            // - Additional notification sources
        }

        /// <summary>
        /// Placeholder method for stopping system notification listeners.
        /// Matches the StartListeningForSystemNotifications method for completeness.
        /// </summary>
        private void StopListeningForSystemNotifications()
        {
            // Placeholder for stopping future notification listeners
        }

        /// <summary>
        /// Processes a notification by creating a NotificationData object and routing it to the appropriate monitor.
        /// This is the main entry point for programmatic notification processing.
        /// </summary>
        /// <param name="title">Notification title/subject</param>
        /// <param name="message">Notification body content</param>
        /// <param name="appName">Display name of the application (optional)</param>
        /// <param name="processName">Process name for routing logic (optional, will be derived from appName if not provided)</param>
        /// <returns>True if notification was successfully routed, false otherwise</returns>
        public async Task<bool> ProcessNotificationAsync(string title, string message, string appName = null, string processName = null)
        {
            try
            {
                // Create notification data object with provided information
                var notification = new NotificationData
                {
                    Title = title,
                    Message = message,
                    AppName = appName ?? "Unknown App",
                    ProcessName = processName ?? ExtractProcessNameFromAppName(appName),
                    Timestamp = DateTime.Now
                };

                // Notify subscribers that a notification was received
                OnNotificationReceived(notification);

                // Route the notification to the appropriate monitor
                return await _notificationRouter.RouteNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Process notification error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Processes a pre-constructed NotificationData object.
        /// Useful when notification data is already available in the correct format.
        /// </summary>
        /// <param name="notification">Complete notification data object</param>
        /// <returns>True if notification was successfully routed, false otherwise</returns>
        public async Task<bool> ProcessNotificationAsync(NotificationData notification)
        {
            try
            {
                // Notify subscribers that a notification was received
                OnNotificationReceived(notification);

                // Route the notification using the existing data
                return await _notificationRouter.RouteNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessNotificationAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts application display names to process names for routing logic.
        /// Uses a built-in mapping table for common applications with fallback normalization.
        /// This mapping is critical for correct notification routing when only app names are available.
        /// </summary>
        /// <param name="appName">Application display name from notification</param>
        /// <returns>Process name suitable for routing logic</returns>
        private string ExtractProcessNameFromAppName(string appName)
        {
            if (string.IsNullOrEmpty(appName))
                return "unknown";

            // Translation table for common applications (case-insensitive lookup)
            // Maps user-friendly app names to actual process names for routing
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

            // Return mapped process name if found, otherwise normalize the app name
            return processMapping.TryGetValue(appName, out var processName)
                ? processName
                : appName.Replace(" ", "").ToLower(); // Remove spaces and lowercase for consistency
        }

        /// <summary>
        /// Gets the cached monitor preference for a specific application process.
        /// Returns the monitor where notifications for this app should be displayed.
        /// </summary>
        /// <param name="processName">Process name to look up</param>
        /// <returns>MonitorInfo if a preference exists, null otherwise</returns>
        public MonitorInfo GetAppMonitor(string processName)
        {
            return _notificationRouter.GetAppMonitorMapping(processName);
        }

        /// <summary>
        /// Sets the monitor preference for a specific application process.
        /// Used to manually override or set routing preferences for applications.
        /// </summary>
        /// <param name="processName">Process name to set preference for</param>
        /// <param name="monitor">Monitor where notifications should be displayed</param>
        public void SetAppMonitor(string processName, MonitorInfo monitor)
        {
            _notificationRouter.UpdateAppMonitorMapping(processName, monitor);
        }

        /// <summary>
        /// Gets a list of all available monitors in the system.
        /// Useful for UI components that need to display monitor options.
        /// </summary>
        /// <returns>List of all detected monitors</returns>
        public System.Collections.Generic.List<MonitorInfo> GetAvailableMonitors()
        {
            return _monitorManager.GetAllMonitors();
        }

        /// <summary>
        /// Gets a list of all currently tracked windows.
        /// Provides visibility into which applications are being monitored for notification routing.
        /// </summary>
        /// <returns>List of currently tracked window information</returns>
        public System.Collections.Generic.List<WindowInfo> GetActiveWindows()
        {
            return _windowTracker.GetAllTrackedWindows();
        }

        /// <summary>
        /// Raises the NotificationReceived event to notify subscribers when a notification begins processing.
        /// Used for logging, monitoring, and UI updates.
        /// </summary>
        /// <param name="notification">Notification that was received</param>
        private void OnNotificationReceived(NotificationData notification)
        {
            NotificationReceived?.Invoke(this, new NotificationReceivedEventArgs(notification));
        }

        /// <summary>
        /// Event handler for notification routing completion events.
        /// Logs successful routing decisions for debugging and monitoring purposes.
        /// </summary>
        /// <param name="sender">NotificationRouter that completed the routing</param>
        /// <param name="e">Event arguments containing routing information</param>
        private void OnNotificationRouted(object sender, NotificationRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Notification routed: {e.Notification.Title} -> Monitor {e.Notification.TargetMonitor?.Index}");
        }

        /// <summary>
        /// Implements IDisposable pattern for proper resource cleanup.
        /// Stops all monitoring services and releases system resources.
        /// Should be called when the application is shutting down.
        /// </summary>
        public void Dispose()
        {
            // Stop all monitoring services
            Stop();

            // Dispose window tracker to clean up its timer
            _windowTracker?.Dispose();
        }
    }

    /// <summary>
    /// Event arguments for notification received events.
    /// Contains the notification data that was received and is being processed.
    /// Used by UI components to display notification activity and logging systems.
    /// </summary>
    public class NotificationReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The notification that was received and is being processed
        /// </summary>
        public NotificationData Notification { get; }

        /// <summary>
        /// Initializes event arguments with the received notification data
        /// </summary>
        /// <param name="notification">Notification that was received</param>
        public NotificationReceivedEventArgs(NotificationData notification)
        {
            Notification = notification;
        }
    }
}