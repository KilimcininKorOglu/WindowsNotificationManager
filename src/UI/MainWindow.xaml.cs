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
    public partial class MainWindow : Window
    {
        private NotificationService _notificationService;
        private DispatcherTimer _uiUpdateTimer;
        private readonly Dictionary<string, object> _originalSettings;

        public MainWindow()
        {
            InitializeComponent();
            _originalSettings = new Dictionary<string, object>();

            // Set localized title
            Title = LocalizationHelper.GetString("WindowsNotificationManager");

            // Set header texts
            HeaderTitleText.Text = LocalizationHelper.GetString("WindowsNotificationManager");
            HeaderSubtitleText.Text = LocalizationHelper.GetString("AppSubtitle");

            // Set localized UI texts
            SetLocalizedTexts();

            InitializeUI();
            LoadSettings();
        }

        private void SetLocalizedTexts()
        {
            // Set localized texts for UI elements
            SystemStatusGroupBox.Header = LocalizationHelper.GetString("SystemStatus");
            ServiceStatusLabel.Text = LocalizationHelper.GetString("ServiceStatus");
            MonitorCountLabel.Text = LocalizationHelper.GetString("MonitorCount");
            TrackedWindowsLabel.Text = LocalizationHelper.GetString("TrackedWindows");

            MonitorsGroupBox.Header = LocalizationHelper.GetString("Monitors");
            IndexHeader.Header = LocalizationHelper.GetString("Index");
            PrimaryMonitorHeader.Header = LocalizationHelper.GetString("PrimaryMonitor");
            ResolutionHeader.Header = LocalizationHelper.GetString("Resolution");

            ActiveWindowsGroupBox.Header = LocalizationHelper.GetString("ActiveWindows");
            ApplicationHeader.Header = LocalizationHelper.GetString("Application");
            TitleHeader.Header = LocalizationHelper.GetString("Title");
            MonitorHeader.Header = LocalizationHelper.GetString("Monitor");
            RefreshWindowsBtn.Content = LocalizationHelper.GetString("RefreshWindows");

            // Settings section
            SettingsGroupBox.Header = LocalizationHelper.GetString("Settings");
            GeneralSettingsHeader.Text = LocalizationHelper.GetString("GeneralSettings");

            StartWithWindowsCheckBox.Content = LocalizationHelper.GetString("StartWithWindowsText");
            EnableDebugLoggingCheckBox.Content = LocalizationHelper.GetString("EnableDebugLogging");

            RestoreDefaultsBtn.Content = LocalizationHelper.GetString("RestoreDefaults");
            ApplySettingsBtn.Content = LocalizationHelper.GetString("Apply");

            MinimizeToTrayBtn.Content = LocalizationHelper.GetString("MinimizeToTray");

        }

        private void InitializeUI()
        {
            // Start UI update timer
            _uiUpdateTimer = new DispatcherTimer();
            _uiUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            _uiUpdateTimer.Start();

            // Set initial status
            ServiceStatusText.Text = LocalizationHelper.GetString("Loading");
            MonitorCountText.Text = LocalizationHelper.GetString("Calculating");
            TrackedWindowsText.Text = LocalizationHelper.GetString("Loading");

            // Get NotificationService when application loads
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // App.xaml.cs'den NotificationService'i al
            var app = Application.Current as App;
            if (app != null)
            {
                // We need to find a way to get NotificationService
                // In this example, we'll use a global service reference
                _notificationService = GetNotificationServiceFromApp();
                UpdateMonitorsList();
                UpdateUI();
            }
        }

        private NotificationService GetNotificationServiceFromApp()
        {
            // This method should be done with better dependency injection in real application
            // For now we'll use a simple approach
            return new NotificationService();
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_notificationService == null)
                return;

            try
            {
                // Service status
                ServiceStatusText.Text = LocalizationHelper.GetString("AppRunning");

                // Monitor count
                var monitors = _notificationService.GetAvailableMonitors();
                MonitorCountText.Text = LocalizationHelper.GetString("MonitorsDetected", monitors?.Count ?? 0);

                // Tracked windows
                var windows = _notificationService.GetActiveWindows();
                TrackedWindowsText.Text = LocalizationHelper.GetString("WindowsTracked", windows?.Count ?? 0);

                // Update windows list
                UpdateWindowsList();
            }
            catch (Exception ex)
            {
                ServiceStatusText.Text = $"{LocalizationHelper.GetString("Error")}: {ex.Message}";
            }
        }

        private void UpdateMonitorsList()
        {
            if (_notificationService == null)
                return;

            try
            {
                var monitors = _notificationService.GetAvailableMonitors();
                MonitorsListView.ItemsSource = monitors;
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationHelper.GetString("LoadingMonitorsError", ex.Message), LocalizationHelper.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateWindowsList()
        {
            if (_notificationService == null)
                return;

            try
            {
                var windows = _notificationService.GetActiveWindows();
                WindowsListView.ItemsSource = windows;
            }
            catch (Exception ex)
            {
                // Silently log error to avoid breaking UI
                System.Diagnostics.Debug.WriteLine($"Windows list update error: {ex.Message}");
            }
        }


        private void RefreshWindowsBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindowsList();
            UpdateMonitorsList();
        }


        private void LoadSettings()
        {
            try
            {
                LoadFromRegistry();
                SaveOriginalSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationHelper.GetString("LoadingSettingsError", ex.Message), LocalizationHelper.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\KorOglansWindowsNotificationManager"))
                {
                    StartWithWindowsCheckBox.IsChecked = (int?)key.GetValue("StartWithWindows", 1) == 1;
                    EnableDebugLoggingCheckBox.IsChecked = (int?)key.GetValue("EnableDebugLogging", 0) == 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registry load error: {ex.Message}");
                SetDefaultValues();
            }
        }

        private void SetDefaultValues()
        {
            StartWithWindowsCheckBox.IsChecked = true;
            EnableDebugLoggingCheckBox.IsChecked = false;
        }

        private void SaveOriginalSettings()
        {
            _originalSettings.Clear();
            _originalSettings["StartWithWindows"] = StartWithWindowsCheckBox.IsChecked;
            _originalSettings["EnableDebugLogging"] = EnableDebugLoggingCheckBox.IsChecked;
        }

        private void SaveSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\KorOglansWindowsNotificationManager"))
                {
                    key.SetValue("StartWithWindows", (StartWithWindowsCheckBox.IsChecked ?? false) ? 1 : 0);
                    key.SetValue("EnableDebugLogging", (EnableDebugLoggingCheckBox.IsChecked ?? false) ? 1 : 0);
                }

                // Invalidate debug logger cache when debug logging setting changes
                DebugLogger.InvalidateCache();

                SetStartupRegistry(StartWithWindowsCheckBox.IsChecked ?? false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationHelper.GetString("SavingSettingsError", ex.Message), LocalizationHelper.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetStartupRegistry(bool startWithWindows)
        {
            try
            {
                var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                var appName = "WindowsNotificationManager";

                using (var key = Registry.CurrentUser.OpenSubKey(keyName, true))
                {
                    if (startWithWindows)
                    {
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key?.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key?.DeleteValue(appName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup registry error: {ex.Message}");
            }
        }

        private void RestoreDefaultsBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                LocalizationHelper.GetString("ConfirmRestoreDefaults"),
                LocalizationHelper.GetString("Confirmation"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                SetDefaultValues();
            }
        }

        private void ApplySettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show(LocalizationHelper.GetString("SettingsApplied"), LocalizationHelper.GetString("Information"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MinimizeToTrayBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            Visibility = Visibility.Hidden;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _uiUpdateTimer?.Stop();
            base.OnClosed(e);
        }
    }

    // Boolean to "Yes/No" converter
    public class BooleanToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Evet" : "Hayır";
            }
            return "Hayır";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}