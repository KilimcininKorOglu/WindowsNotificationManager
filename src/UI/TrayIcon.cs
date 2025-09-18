using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WindowsNotificationManager.src.Services;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.UI
{
    /// <summary>
    /// Manages the system tray icon and context menu for the Windows notification manager application.
    /// Provides user access to the application when the main window is hidden or minimized.
    /// Uses Windows Forms NotifyIcon for system tray integration with localized menu items.
    /// </summary>
    public class TrayIcon : IDisposable
    {
        /// <summary>
        /// Windows Forms NotifyIcon component for system tray integration
        /// </summary>
        private NotifyIcon _notifyIcon;

        /// <summary>
        /// Context menu displayed when right-clicking the tray icon
        /// </summary>
        private ContextMenuStrip _contextMenu;

        /// <summary>
        /// Reference to the notification service for system interaction (currently unused but available for future features)
        /// </summary>
        private NotificationService _notificationService;

        /// <summary>
        /// Initializes the system tray icon and context menu.
        /// Must be called after creating the TrayIcon instance to set up the tray functionality.
        /// </summary>
        public void Initialize()
        {
            CreateTrayIcon();
            CreateContextMenu();
        }

        /// <summary>
        /// Creates and configures the system tray icon with localized tooltip and event handlers.
        /// Sets up double-click functionality to show the main window.
        /// </summary>
        private void CreateTrayIcon()
        {
            _notifyIcon = new NotifyIcon();

            // Set icon (attempts to use embedded icon with fallback to system icon)
            _notifyIcon.Icon = CreateTrayIconBitmap();

            // Set localized tooltip text that appears when hovering over the tray icon
            _notifyIcon.Text = LocalizationHelper.GetString("SystemTrayTooltip");

            // Make the tray icon visible in the system tray
            _notifyIcon.Visible = true;

            // Register double-click event to show main window
            _notifyIcon.DoubleClick += TrayIcon_DoubleClick;
        }

        /// <summary>
        /// Creates the tray icon from embedded resources with fallback to system icons.
        /// Attempts to load a custom .ico file from embedded resources, falls back to system information icon.
        /// </summary>
        /// <returns>Icon object for the system tray</returns>
        private Icon CreateTrayIconBitmap()
        {
            try
            {
                // Attempt to load embedded tray icon from assembly resources
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("WindowsNotificationManager.tray.ico"))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
            }
            catch
            {
                // Silently handle embedded icon loading failures
                // Fall through to system icon fallback
            }

            // Fallback: Use Windows system information icon if custom icon is unavailable
            return SystemIcons.Information;
        }

        /// <summary>
        /// Creates the context menu that appears when right-clicking the tray icon.
        /// Includes localized menu items for common actions: show window, about, and exit.
        /// </summary>
        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            // Create main menu items with localized text

            // "Show Main Window" - restores the hidden main window
            var showMainWindow = new ToolStripMenuItem(LocalizationHelper.GetString("ShowMainWindow"));
            showMainWindow.Click += ShowMainWindow_Click;

            // Visual separator for menu organization
            var separator1 = new ToolStripSeparator();

            // "About" - displays application information dialog
            var aboutMenuItem = new ToolStripMenuItem(LocalizationHelper.GetString("About"));
            aboutMenuItem.Click += About_Click;

            // Visual separator before exit option
            var separator3 = new ToolStripSeparator();

            // "Exit" - closes the entire application
            var exitMenuItem = new ToolStripMenuItem(LocalizationHelper.GetString("Exit"));
            exitMenuItem.Click += Exit_Click;

            // Add all menu items to the context menu in order
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                showMainWindow,
                separator1,
                aboutMenuItem,
                separator3,
                exitMenuItem
            });

            // Associate the context menu with the tray icon
            _notifyIcon.ContextMenuStrip = _contextMenu;
        }

        /// <summary>
        /// Sets the notification service reference for potential future features.
        /// Currently not used but provides access to system functionality for context menu enhancements.
        /// </summary>
        /// <param name="notificationService">NotificationService instance for system interaction</param>
        public void SetNotificationService(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Event handler for tray icon double-click events.
        /// Provides quick access to show the main window by double-clicking the tray icon.
        /// </summary>
        /// <param name="sender">The NotifyIcon that was double-clicked</param>
        /// <param name="e">Double-click event arguments</param>
        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        /// <summary>
        /// Event handler for "Show Main Window" context menu item click.
        /// Delegates to the ShowMainWindow method for consistent behavior.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Click event arguments</param>
        private void ShowMainWindow_Click(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        /// <summary>
        /// Restores the main window from hidden/minimized state to visible and active.
        /// Handles the transition from tray-only mode back to normal window display.
        /// Ensures the window is visible, restored to normal state, shown in taskbar, and activated.
        /// </summary>
        private void ShowMainWindow()
        {
            var mainWindow = System.Windows.Application.Current?.MainWindow;
            if (mainWindow != null)
            {
                // Make window visible
                mainWindow.Visibility = System.Windows.Visibility.Visible;

                // Restore from minimized state
                mainWindow.WindowState = System.Windows.WindowState.Normal;

                // Show in taskbar for normal window behavior
                mainWindow.ShowInTaskbar = true;

                // Bring window to front and give it focus
                mainWindow.Activate();
            }
        }


        /// <summary>
        /// Event handler for "About" context menu item click.
        /// Displays an information dialog with application details and version information.
        /// </summary>
        /// <param name="sender">The about menu item that was clicked</param>
        /// <param name="e">Click event arguments</param>
        private void About_Click(object sender, EventArgs e)
        {
            // Display localized about dialog with application information
            MessageBox.Show(
                LocalizationHelper.GetString("AboutMessage"),
                LocalizationHelper.GetString("About"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        /// <summary>
        /// Event handler for "Exit" context menu item click.
        /// Completely shuts down the application, terminating all processes and services.
        /// </summary>
        /// <param name="sender">The exit menu item that was clicked</param>
        /// <param name="e">Click event arguments</param>
        private void Exit_Click(object sender, EventArgs e)
        {
            // Gracefully shutdown the entire WPF application
            System.Windows.Application.Current?.Shutdown();
        }

        /// <summary>
        /// Implements IDisposable pattern for proper resource cleanup.
        /// Disposes of the NotifyIcon and ContextMenuStrip to prevent resource leaks.
        /// Should be called when the application is shutting down.
        /// </summary>
        public void Dispose()
        {
            // Dispose of Windows Forms components to release system resources
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}