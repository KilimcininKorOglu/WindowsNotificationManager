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

                // CRITICAL: Start Windows API hooking for real-time notification interception
                // This requires administrator privileges to function
                _apiHook.StartHooking();

                _isRunning = true;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"NotificationService start error: {ex.Message}");
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

                // CRITICAL: Stop Windows API hooking to release system hooks
                _apiHook.StopHooking();

                _isRunning = false;
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"NotificationService stop error: {ex.Message}");
            }
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
        /// Event handler for notification routing completion events.
        /// Logs successful routing decisions for debugging and monitoring purposes.
        /// </summary>
        /// <param name="sender">NotificationRouter that completed the routing</param>
        /// <param name="e">Event arguments containing routing information</param>
        private void OnNotificationRouted(object sender, NotificationRoutedEventArgs e)
        {
            DebugLogger.WriteLine($"Notification routed: {e.Notification.Title} -> Monitor {e.Notification.TargetMonitor?.Index}");
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

            // Unsubscribe event handlers to break circular references
            _windowTracker.WindowMoved -= _notificationRouter.HandleWindowMoved;
            _notificationRouter.NotificationRouted -= OnNotificationRouted;

            // Dispose all components to release system resources
            _windowTracker?.Dispose();
            _apiHook?.Dispose();
            _monitorManager?.StopMonitoring();
        }
    }
}
