using System;
using System.Collections.Generic;
using System.Linq;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    /// <summary>
    /// Manages multiple monitor detection, configuration changes, and provides monitor-related utilities.
    /// This class is responsible for tracking all connected monitors and their properties in multi-monitor setups.
    /// </summary>
    public class MonitorManager
    {
        /// <summary>
        /// List of all detected monitors in the system
        /// </summary>
        private List<MonitorInfo> _monitors;

        /// <summary>
        /// Reference to the primary monitor (main display)
        /// </summary>
        private MonitorInfo _primaryMonitor;

        /// <summary>
        /// Event fired when monitor configuration changes (monitors added/removed, resolution changed, etc.)
        /// </summary>
        public event EventHandler<MonitorConfigurationChangedEventArgs> MonitorConfigurationChanged;

        /// <summary>
        /// Flag to suppress MonitorConfigurationChanged event during construction
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Initializes a new instance of MonitorManager and discovers all connected monitors
        /// </summary>
        public MonitorManager()
        {
            RefreshMonitors();
            _isInitialized = true;
        }

        /// <summary>
        /// Refreshes the list of monitors by re-detecting all connected displays.
        /// This method should be called when monitors are added/removed or configuration changes.
        /// </summary>
        public void RefreshMonitors()
        {
            // Get all monitors using Win32 API calls
            _monitors = MonitorUtils.GetAllMonitors();
            _primaryMonitor = _monitors.FirstOrDefault(m => m.IsPrimary);

            // Assign sequential index numbers to each monitor for easier identification
            for (int i = 0; i < _monitors.Count; i++)
            {
                _monitors[i].Index = i;
            }

            // Log monitor configuration for debugging purposes
            DebugLogger.WriteLine($"Detected {_monitors.Count} monitors:");
            for (int i = 0; i < _monitors.Count; i++)
            {
                var monitor = _monitors[i];
                DebugLogger.WriteLine($"Monitor {i}: Bounds=({monitor.Bounds.Left},{monitor.Bounds.Top},{monitor.Bounds.Right},{monitor.Bounds.Bottom}), Primary={monitor.IsPrimary}");
            }

            // Notify subscribers that monitor configuration has changed
            OnMonitorConfigurationChanged();
        }

        /// <summary>
        /// Returns a copy of all detected monitors in the system
        /// </summary>
        /// <returns>List of MonitorInfo objects representing all connected monitors</returns>
        public List<MonitorInfo> GetAllMonitors()
        {
            return new List<MonitorInfo>(_monitors);
        }

        /// <summary>
        /// Gets the primary monitor (main display) of the system
        /// </summary>
        /// <returns>MonitorInfo object representing the primary monitor, or null if not found</returns>
        public MonitorInfo GetPrimaryMonitor()
        {
            return _primaryMonitor;
        }

        /// <summary>
        /// Finds the monitor that contains the largest portion of the specified window rectangle.
        /// This is useful for determining which monitor a window primarily belongs to.
        /// </summary>
        /// <param name="windowRect">Rectangle representing the window bounds</param>
        /// <returns>MonitorInfo object for the best matching monitor, or primary monitor as fallback</returns>
        public MonitorInfo GetMonitorContainingWindow(Win32Helper.RECT windowRect)
        {
            // Check each monitor to see if it contains the window
            foreach (var monitor in _monitors)
            {
                if (monitor.ContainsWindow(windowRect))
                    return monitor;
            }
            // If no monitor contains the window, return primary monitor as safe fallback
            return _primaryMonitor;
        }

        /// <summary>
        /// Starts monitoring for display configuration changes (monitors added/removed, resolution changed).
        /// Uses Windows SystemEvents to detect when display settings change.
        /// </summary>
        public void StartMonitoring()
        {
            // Subscribe to Windows system event for display configuration changes
            // This captures WM_DISPLAYCHANGE messages and similar display-related events
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        /// <summary>
        /// Stops monitoring for display configuration changes.
        /// Should be called when the application is shutting down to clean up event handlers.
        /// </summary>
        public void StopMonitoring()
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        }

        /// <summary>
        /// Event handler called when Windows detects display settings have changed.
        /// Automatically refreshes the monitor list to reflect the new configuration.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Refresh monitor configuration when display settings change
            RefreshMonitors();
        }

        /// <summary>
        /// Raises the MonitorConfigurationChanged event to notify subscribers of monitor changes
        /// </summary>
        private void OnMonitorConfigurationChanged()
        {
            if (!_isInitialized)
                return;
            MonitorConfigurationChanged?.Invoke(this, new MonitorConfigurationChangedEventArgs(_monitors));
        }

    }

    /// <summary>
    /// Event arguments for monitor configuration change notifications.
    /// Contains the updated list of monitors after a configuration change.
    /// </summary>
    public class MonitorConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The updated list of monitors after configuration change
        /// </summary>
        public List<MonitorInfo> Monitors { get; }

        /// <summary>
        /// Initializes event arguments with the new monitor configuration
        /// </summary>
        /// <param name="monitors">List of monitors in the new configuration</param>
        public MonitorConfigurationChangedEventArgs(List<MonitorInfo> monitors)
        {
            Monitors = monitors;
        }
    }
}