using System;
using System.Collections.Generic;
using System.Linq;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    public class MonitorManager
    {
        private List<MonitorInfo> _monitors;
        private MonitorInfo _primaryMonitor;

        public event EventHandler<MonitorConfigurationChangedEventArgs> MonitorConfigurationChanged;

        public MonitorManager()
        {
            RefreshMonitors();
        }

        public void RefreshMonitors()
        {
            _monitors = MonitorUtils.GetAllMonitors();
            _primaryMonitor = _monitors.FirstOrDefault(m => m.IsPrimary);

            // Index monitors
            for (int i = 0; i < _monitors.Count; i++)
            {
                _monitors[i].Index = i;
            }

            // Debug log all monitors
            DebugLogger.WriteLine($"Detected {_monitors.Count} monitors:");
            for (int i = 0; i < _monitors.Count; i++)
            {
                var monitor = _monitors[i];
                DebugLogger.WriteLine($"Monitor {i}: Bounds=({monitor.Bounds.Left},{monitor.Bounds.Top},{monitor.Bounds.Right},{monitor.Bounds.Bottom}), Primary={monitor.IsPrimary}");
            }

            OnMonitorConfigurationChanged();
        }

        public List<MonitorInfo> GetAllMonitors()
        {
            return new List<MonitorInfo>(_monitors);
        }

        public MonitorInfo GetPrimaryMonitor()
        {
            return _primaryMonitor;
        }

        public MonitorInfo GetMonitorByIndex(int index)
        {
            if (index >= 0 && index < _monitors.Count)
                return _monitors[index];
            return null;
        }

        public MonitorInfo GetMonitorFromWindowHandle(IntPtr windowHandle)
        {
            return MonitorUtils.GetMonitorFromWindow(windowHandle);
        }

        public MonitorInfo GetMonitorFromPoint(int x, int y)
        {
            return MonitorUtils.GetMonitorFromPoint(x, y);
        }

        public MonitorInfo GetMonitorContainingWindow(Win32Helper.RECT windowRect)
        {
            foreach (var monitor in _monitors)
            {
                if (monitor.ContainsWindow(windowRect))
                    return monitor;
            }
            return _primaryMonitor; // Fallback to primary monitor
        }

        public int GetMonitorCount()
        {
            return _monitors?.Count ?? 0;
        }

        public bool IsMultiMonitorSetup()
        {
            return GetMonitorCount() > 1;
        }

        public void StartMonitoring()
        {
            // To listen for monitor configuration changes in Windows
            // Need to capture WM_DISPLAYCHANGE message
            // This can be done with SystemEvents.DisplaySettingsChanged event in WPF application
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        public void StopMonitoring()
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Refresh when monitor configuration changes
            RefreshMonitors();
        }

        private void OnMonitorConfigurationChanged()
        {
            MonitorConfigurationChanged?.Invoke(this, new MonitorConfigurationChangedEventArgs(_monitors));
        }

        public string GetMonitorDisplayName(MonitorInfo monitor)
        {
            if (monitor.IsPrimary)
                return $"Ana Monitör ({monitor.Bounds.Width}x{monitor.Bounds.Height})";
            else
                return $"Monitör {monitor.Index + 1} ({monitor.Bounds.Width}x{monitor.Bounds.Height})";
        }
    }

    public class MonitorConfigurationChangedEventArgs : EventArgs
    {
        public List<MonitorInfo> Monitors { get; }

        public MonitorConfigurationChangedEventArgs(List<MonitorInfo> monitors)
        {
            Monitors = monitors;
        }
    }
}