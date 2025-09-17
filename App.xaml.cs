using System.Windows;
using WindowsNotificationManager.src.Services;
using WindowsNotificationManager.src.UI;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager
{
    public partial class App : Application
    {
        private NotificationService _notificationService;
        private TrayIcon _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Clear debug log on startup
            DebugLogger.ClearLog();
            DebugLogger.WriteLine("Application starting...");

            // Start minimized but show in taskbar
            MainWindow = new MainWindow();
            MainWindow.ShowInTaskbar = true;
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Minimized;

            // Create system tray icon
            _trayIcon = new TrayIcon();
            _trayIcon.Initialize();

            // Start notification service
            _notificationService = new NotificationService();
            _notificationService.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notificationService?.Stop();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}