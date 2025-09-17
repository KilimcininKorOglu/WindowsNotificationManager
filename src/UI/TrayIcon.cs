using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WindowsNotificationManager.src.Services;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.UI
{
    public class TrayIcon : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private NotificationService _notificationService;

        public void Initialize()
        {
            CreateTrayIcon();
            CreateContextMenu();
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new NotifyIcon();

            // Create icon (simple bitmap)
            _notifyIcon.Icon = CreateTrayIconBitmap();
            _notifyIcon.Text = LocalizationHelper.GetString("SystemTrayTooltip");
            _notifyIcon.Visible = true;

            // Double click event
            _notifyIcon.DoubleClick += TrayIcon_DoubleClick;
        }

        private Icon CreateTrayIconBitmap()
        {
            try
            {
                // Use embedded tray icon
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
                // Fallback to system icon if embedded icon fails
            }

            // Fallback: Use Windows system notification icon
            return SystemIcons.Information;
        }

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            // Menu items
            var showMainWindow = new ToolStripMenuItem(LocalizationHelper.GetString("ShowMainWindow"));
            showMainWindow.Click += ShowMainWindow_Click;


            var separator1 = new ToolStripSeparator();

            var aboutMenuItem = new ToolStripMenuItem(LocalizationHelper.GetString("About"));
            aboutMenuItem.Click += About_Click;

            var separator3 = new ToolStripSeparator();

            var exitMenuItem = new ToolStripMenuItem(LocalizationHelper.GetString("Exit"));
            exitMenuItem.Click += Exit_Click;

            // Add menu items
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                showMainWindow,
                separator1,
                aboutMenuItem,
                separator3,
                exitMenuItem
            });

            _notifyIcon.ContextMenuStrip = _contextMenu;
        }



        public void SetNotificationService(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow_Click(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            var mainWindow = System.Windows.Application.Current?.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Visibility = System.Windows.Visibility.Visible;
                mainWindow.WindowState = System.Windows.WindowState.Normal;
                mainWindow.ShowInTaskbar = true;
                mainWindow.Activate();
            }
        }



        private void About_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                LocalizationHelper.GetString("AboutMessage"),
                LocalizationHelper.GetString("About"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            System.Windows.Application.Current?.Shutdown();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}