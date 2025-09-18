using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using WindowsNotificationManager.src.Core;
using WindowsNotificationManager.src.Services;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.UI
{
    /// <summary>
    /// Main application window providing user interface for notification management system.
    /// Displays system status, monitor information, active windows, and settings configuration.
    /// Implements multi-language support and registry-based settings persistence.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Core notification service instance for accessing system functionality
        /// </summary>
        private NotificationService _notificationService;

        /// <summary>
        /// Timer for periodic UI updates every 2 seconds to refresh system status
        /// </summary>
        private DispatcherTimer _uiUpdateTimer;

        /// <summary>
        /// Cache of original settings values for comparison during save operations
        /// </summary>
        private readonly Dictionary<string, object> _originalSettings;

        /// <summary>
        /// Initializes the main window with localized UI elements and loads user settings.
        /// Sets up the UI update timer and prepares the interface for multi-language support.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _originalSettings = new Dictionary<string, object>();

            // Set localized window title
            Title = LocalizationHelper.GetString("WindowsNotificationManager");

            // Set main header texts with localized content
            HeaderTitleText.Text = LocalizationHelper.GetString("WindowsNotificationManager");
            HeaderSubtitleText.Text = LocalizationHelper.GetString("AppSubtitle");

            // Apply localization to all UI elements
            SetLocalizedTexts();

            // Initialize UI components and timers
            InitializeUI();

            // Load user settings from registry
            LoadSettings();
        }

        /// <summary>
        /// Applies localized text to all UI elements throughout the application.
        /// This method supports dynamic language switching by updating all visible text.
        /// Organizes localization by functional groups: status, monitors, windows, and settings.
        /// </summary>
        private void SetLocalizedTexts()
        {
            // System Status section localization
            SystemStatusGroupBox.Header = LocalizationHelper.GetString("SystemStatus");
            ServiceStatusLabel.Text = LocalizationHelper.GetString("ServiceStatus");
            MonitorCountLabel.Text = LocalizationHelper.GetString("MonitorCount");
            TrackedWindowsLabel.Text = LocalizationHelper.GetString("TrackedWindows");

            // Monitors section localization
            MonitorsGroupBox.Header = LocalizationHelper.GetString("Monitors");
            IndexHeader.Header = LocalizationHelper.GetString("Index");
            PrimaryMonitorHeader.Header = LocalizationHelper.GetString("PrimaryMonitor");
            ResolutionHeader.Header = LocalizationHelper.GetString("Resolution");

            // Active Windows section localization
            ActiveWindowsGroupBox.Header = LocalizationHelper.GetString("ActiveWindows");
            ApplicationHeader.Header = LocalizationHelper.GetString("Application");
            TitleHeader.Header = LocalizationHelper.GetString("Title");
            MonitorHeader.Header = LocalizationHelper.GetString("Monitor");
            RefreshWindowsBtn.Content = LocalizationHelper.GetString("RefreshWindows");

            // Settings section localization
            SettingsGroupBox.Header = LocalizationHelper.GetString("Settings");

            StartWithWindowsCheckBox.Content = LocalizationHelper.GetString("StartWithWindowsText");
            EnableDebugLoggingCheckBox.Content = LocalizationHelper.GetString("EnableDebugLogging");

            RestoreDefaultsBtn.Content = LocalizationHelper.GetString("RestoreDefaults");
            ApplySettingsBtn.Content = LocalizationHelper.GetString("Apply");

            MinimizeToTrayBtn.Content = LocalizationHelper.GetString("MinimizeToTray");
        }

        /// <summary>
        /// Initializes UI components including the periodic update timer and initial status text.
        /// Sets up a 2-second timer for refreshing system status and prepares loading state.
        /// </summary>
        private void InitializeUI()
        {
            // Configure periodic UI update timer (2-second intervals for real-time status)
            _uiUpdateTimer = new DispatcherTimer();
            _uiUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            _uiUpdateTimer.Start();

            // Set initial loading state with localized text
            ServiceStatusText.Text = LocalizationHelper.GetString("Loading");
            MonitorCountText.Text = LocalizationHelper.GetString("Calculating");
            TrackedWindowsText.Text = LocalizationHelper.GetString("Loading");

            // Register for window loaded event to initialize notification service
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Event handler for window loaded event. Initializes the notification service connection
        /// and performs initial data loading for monitors and UI updates.
        /// </summary>
        /// <param name="sender">The MainWindow instance</param>
        /// <param name="e">Event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve application instance to access global services
            var app = Application.Current as App;
            if (app != null)
            {
                // Initialize notification service connection
                // Note: In production, this should use proper dependency injection
                _notificationService = GetNotificationServiceFromApp();

                // Perform initial data loading
                UpdateMonitorsList();
                UpdateUI();
            }
        }

        /// <summary>
        /// Creates or retrieves the NotificationService instance for UI communication.
        /// TODO: This should be replaced with proper dependency injection in production.
        /// Currently creates a new instance each time for simplicity.
        /// </summary>
        /// <returns>NotificationService instance for system interaction</returns>
        private NotificationService GetNotificationServiceFromApp()
        {
            // IMPROVEMENT: Replace with proper dependency injection container
            // This approach creates a new service instance which may not match the global service
            return new NotificationService();
        }

        /// <summary>
        /// Timer tick event handler that triggers periodic UI updates every 2 seconds.
        /// Ensures the interface displays current system status and statistics.
        /// </summary>
        /// <param name="sender">The DispatcherTimer instance</param>
        /// <param name="e">Timer event arguments</param>
        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateUI();
        }

        /// <summary>
        /// Updates all UI elements with current system status information.
        /// Refreshes service status, monitor count, tracked windows count, and active windows list.
        /// Handles errors gracefully by displaying error messages in the service status field.
        /// </summary>
        private void UpdateUI()
        {
            if (_notificationService == null)
                return;

            try
            {
                // Update service running status
                ServiceStatusText.Text = LocalizationHelper.GetString("AppRunning");

                // Update monitor count with localized text
                var monitors = _notificationService.GetAvailableMonitors();
                MonitorCountText.Text = LocalizationHelper.GetString("MonitorsDetected", monitors?.Count ?? 0);

                // Update tracked windows count with localized text
                var windows = _notificationService.GetActiveWindows();
                TrackedWindowsText.Text = LocalizationHelper.GetString("WindowsTracked", windows?.Count ?? 0);

                // Refresh the active windows list display
                UpdateWindowsList();
            }
            catch (Exception ex)
            {
                // Display error in service status field with localized error prefix
                ServiceStatusText.Text = $"{LocalizationHelper.GetString("Error")}: {ex.Message}";
            }
        }

        /// <summary>
        /// Updates the monitors list view with current monitor information.
        /// Displays monitor details including index, primary status, and resolution.
        /// Shows localized error message if monitor information cannot be retrieved.
        /// </summary>
        private void UpdateMonitorsList()
        {
            if (_notificationService == null)
                return;

            try
            {
                // Get current monitor configuration and bind to ListView
                var monitors = _notificationService.GetAvailableMonitors();
                MonitorsListView.ItemsSource = monitors;
            }
            catch (Exception ex)
            {
                // Show user-friendly error message for monitor loading failures
                MessageBox.Show(LocalizationHelper.GetString("LoadingMonitorsError", ex.Message), LocalizationHelper.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Updates the active windows list view with currently tracked window information.
        /// Displays application names, window titles, and monitor assignments.
        /// Logs errors silently to avoid disrupting the UI update cycle.
        /// </summary>
        private void UpdateWindowsList()
        {
            if (_notificationService == null)
                return;

            try
            {
                // Get currently tracked windows and bind to ListView
                var windows = _notificationService.GetActiveWindows();
                WindowsListView.ItemsSource = windows;
            }
            catch (Exception ex)
            {
                // Log error silently to prevent UI update interruption
                System.Diagnostics.Debug.WriteLine($"Windows list update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Button click event handler for manual refresh of windows and monitors lists.
        /// Allows users to force an immediate update of system information.
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">Button click event arguments</param>
        private void RefreshWindowsBtn_Click(object sender, RoutedEventArgs e)
        {
            // Trigger manual refresh of both data sources
            UpdateWindowsList();
            UpdateMonitorsList();
        }


        /// <summary>
        /// Loads user settings from Windows registry and caches original values.
        /// Provides error handling with localized error messages for registry access failures.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Load settings from registry
                LoadFromRegistry();

                // Cache original values for comparison during save operations
                SaveOriginalSettings();
            }
            catch (Exception ex)
            {
                // Display localized error message for settings loading failures
                MessageBox.Show(LocalizationHelper.GetString("LoadingSettingsError", ex.Message), LocalizationHelper.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Loads application settings from Windows registry.
        /// Uses HKEY_CURRENT_USER for user-specific settings persistence.
        /// Falls back to default values if registry access fails.
        /// </summary>
        private void LoadFromRegistry()
        {
            try
            {
                // Access user-specific registry key for application settings
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\KorOglansWindowsNotificationManager"))
                {
                    // Load startup preference (default: enabled)
                    StartWithWindowsCheckBox.IsChecked = (int?)key.GetValue("StartWithWindows", 1) == 1;

                    // Load debug logging preference (default: disabled)
                    EnableDebugLoggingCheckBox.IsChecked = (int?)key.GetValue("EnableDebugLogging", 0) == 1;
                }
            }
            catch (Exception ex)
            {
                // Log registry access error and use default values
                System.Diagnostics.Debug.WriteLine($"Registry load error: {ex.Message}");
                SetDefaultValues();
            }
        }

        /// <summary>
        /// Sets default values for all application settings.
        /// Called when registry loading fails or when restoring defaults.
        /// </summary>
        private void SetDefaultValues()
        {
            // Default: Start with Windows enabled for convenience
            StartWithWindowsCheckBox.IsChecked = true;

            // Default: Debug logging disabled for performance
            EnableDebugLoggingCheckBox.IsChecked = false;
        }

        /// <summary>
        /// Caches the current settings values for comparison during save operations.
        /// Used to detect changes and avoid unnecessary registry writes.
        /// </summary>
        private void SaveOriginalSettings()
        {
            _originalSettings.Clear();
            _originalSettings["StartWithWindows"] = StartWithWindowsCheckBox.IsChecked;
            _originalSettings["EnableDebugLogging"] = EnableDebugLoggingCheckBox.IsChecked;
        }

        /// <summary>
        /// Saves current settings to Windows registry and updates Windows startup configuration.
        /// Invalidates debug logger cache to ensure logging changes take effect immediately.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // Save settings to application registry key
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\KorOglansWindowsNotificationManager"))
                {
                    // Convert boolean checkboxes to integer values for registry storage
                    key.SetValue("StartWithWindows", (StartWithWindowsCheckBox.IsChecked ?? false) ? 1 : 0);
                    key.SetValue("EnableDebugLogging", (EnableDebugLoggingCheckBox.IsChecked ?? false) ? 1 : 0);
                }

                // Force debug logger to re-read settings from registry
                DebugLogger.InvalidateCache();

                // Update Windows startup registry based on user preference
                SetStartupRegistry(StartWithWindowsCheckBox.IsChecked ?? false);
            }
            catch (Exception ex)
            {
                // Display localized error message for save failures
                MessageBox.Show(LocalizationHelper.GetString("SavingSettingsError", ex.Message), LocalizationHelper.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Manages Windows startup registry entry for automatic application startup.
        /// Adds or removes the application from Windows Run registry key based on user preference.
        /// </summary>
        /// <param name="startWithWindows">True to enable startup, false to disable</param>
        private void SetStartupRegistry(bool startWithWindows)
        {
            try
            {
                // Windows Run registry key for current user startup applications
                var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                var appName = "WindowsNotificationManager";

                using (var key = Registry.CurrentUser.OpenSubKey(keyName, true))
                {
                    if (startWithWindows)
                    {
                        // Add application to startup with quoted executable path
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key?.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        // Remove application from startup (safe delete)
                        key?.DeleteValue(appName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log startup registry errors silently to avoid disrupting user workflow
                System.Diagnostics.Debug.WriteLine($"Startup registry error: {ex.Message}");
            }
        }

        /// <summary>
        /// Button click event handler for restoring default settings.
        /// Shows confirmation dialog before resetting all settings to default values.
        /// </summary>
        /// <param name="sender">The restore defaults button</param>
        /// <param name="e">Button click event arguments</param>
        private void RestoreDefaultsBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show localized confirmation dialog before resetting settings
            var result = MessageBox.Show(
                LocalizationHelper.GetString("ConfirmRestoreDefaults"),
                LocalizationHelper.GetString("Confirmation"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            // Only restore defaults if user confirms
            if (result == MessageBoxResult.Yes)
            {
                SetDefaultValues();
            }
        }

        /// <summary>
        /// Button click event handler for applying/saving current settings.
        /// Saves settings to registry and shows confirmation message to user.
        /// </summary>
        /// <param name="sender">The apply settings button</param>
        /// <param name="e">Button click event arguments</param>
        private void ApplySettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            // Save current settings to registry
            SaveSettings();

            // Show localized confirmation message
            MessageBox.Show(LocalizationHelper.GetString("SettingsApplied"), LocalizationHelper.GetString("Information"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Button click event handler for minimizing window to system tray.
        /// Hides window from taskbar and makes it accessible only via tray icon.
        /// </summary>
        /// <param name="sender">The minimize to tray button</param>
        /// <param name="e">Button click event arguments</param>
        private void MinimizeToTrayBtn_Click(object sender, RoutedEventArgs e)
        {
            // Minimize window and hide from taskbar
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Window state changed event handler that manages tray behavior.
        /// Automatically hides window from taskbar when minimized.
        /// </summary>
        /// <param name="sender">The MainWindow instance</param>
        /// <param name="e">State changed event arguments</param>
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // When window is minimized, hide from taskbar (accessible via tray icon)
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Override for window closing event to ensure proper cleanup.
        /// Stops the UI update timer to prevent resource leaks.
        /// </summary>
        /// <param name="e">Window closing event arguments</param>
        protected override void OnClosed(EventArgs e)
        {
            // Stop the periodic UI update timer
            _uiUpdateTimer?.Stop();
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// WPF value converter for displaying boolean values as localized "Yes/No" text.
    /// Used in data binding to show user-friendly text instead of true/false values.
    /// Currently uses Turkish text - should be updated to use LocalizationHelper for full localization.
    /// </summary>
    public class BooleanToYesNoConverter : IValueConverter
    {
        /// <summary>
        /// Converts boolean values to Turkish "Yes/No" text for display.
        /// TODO: Update to use LocalizationHelper for proper multi-language support.
        /// </summary>
        /// <param name="value">Boolean value to convert</param>
        /// <param name="targetType">Target type (typically string)</param>
        /// <param name="parameter">Converter parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>"Evet" for true, "Hayır" for false or invalid values</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // TODO: Replace with LocalizationHelper.GetString("Yes"/"No") for proper localization
                return boolValue ? "Evet" : "Hayır";
            }
            return "Hayır";
        }

        /// <summary>
        /// Reverse conversion not implemented as this converter is used for display only.
        /// </summary>
        /// <param name="value">Display value</param>
        /// <param name="targetType">Target type</param>
        /// <param name="parameter">Converter parameter</param>
        /// <param name="culture">Culture information</param>
        /// <returns>Not implemented exception</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}