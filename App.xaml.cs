using System.Windows;
using WindowsNotificationManager.src.Services;
using WindowsNotificationManager.src.UI;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager
{
    /// <summary>
    /// Main application class managing the WPF application lifecycle and core service initialization.
    /// Orchestrates startup sequence: debug logging → main window → system tray → notification service.
    /// Handles graceful shutdown and resource cleanup for all application components.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Core notification routing service managing multi-monitor notification interception and positioning
        /// </summary>
        private NotificationService _notificationService;

        /// <summary>
        /// System tray icon providing user access when main window is hidden or minimized
        /// </summary>
        private TrayIcon _trayIcon;

        /// <summary>
        /// Application startup event handler orchestrating the complete initialization sequence.
        /// CRITICAL: Must run with administrator privileges for Windows API hook functionality.
        /// Initializes components in dependency order: logging → UI → tray → notification service.
        /// </summary>
        /// <param name="e">Startup event arguments containing command line parameters</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // STEP 1: Initialize debug logging system with fresh log file
            DebugLogger.ClearLog();
            DebugLogger.WriteLine("Application starting...");

            // STEP 2: Create and configure main window for immediate user access
            // Start minimized to reduce visual disruption while maintaining taskbar presence
            MainWindow = new MainWindow();
            MainWindow.ShowInTaskbar = true;           // Ensure taskbar icon is visible
            MainWindow.Show();                         // Make window available for restoration

            // STEP 3: Initialize system tray integration for background operation
            _trayIcon = new TrayIcon();
            _trayIcon.Initialize();

            // STEP 4: Start core notification service (requires administrator privileges)
            // This activates Windows API hooks for real-time notification interception
            _notificationService = new NotificationService();
            _notificationService.Start();
        }

        /// <summary>
        /// Application exit event handler ensuring proper cleanup of all system resources.
        /// CRITICAL: Stops Windows API hooks and releases system tray resources to prevent resource leaks.
        /// Called when user exits via tray menu, window close, or system shutdown.
        /// </summary>
        /// <param name="e">Exit event arguments containing application exit code</param>
        protected override void OnExit(ExitEventArgs e)
        {
            // STEP 1: Stop notification service and release Windows API hooks
            // This is critical to prevent system hooks from remaining active after application exit
            _notificationService?.Stop();

            // STEP 2: Dispose system tray icon and release Windows Forms resources
            // Prevents tray icon from remaining visible after application termination
            _trayIcon?.Dispose();

            // STEP 3: Complete WPF application shutdown sequence
            base.OnExit(e);
        }
    }
}