using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsNotificationManager.src.Utils;

namespace WindowsNotificationManager.src.Core
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public string ProcessName { get; set; }
        public uint ProcessId { get; set; }
        public Win32Helper.RECT Rectangle { get; set; }
        public MonitorInfo Monitor { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class WindowTracker
    {
        private readonly MonitorManager _monitorManager;
        private readonly Dictionary<uint, WindowInfo> _trackedWindows;
        private readonly Timer _trackingTimer;
        private readonly object _lockObject = new object();

        // Store last known positions for minimized windows
        private readonly Dictionary<IntPtr, Win32Helper.RECT> _lastKnownPositions = new();

        public event EventHandler<WindowMovedEventArgs> WindowMoved;
        public event EventHandler<WindowFocusChangedEventArgs> WindowFocusChanged;

        private IntPtr _lastForegroundWindow = IntPtr.Zero;

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        public WindowTracker(MonitorManager monitorManager)
        {
            _monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
            _trackedWindows = new Dictionary<uint, WindowInfo>();

            // Check active window every 500ms
            _trackingTimer = new Timer(TrackActiveWindows, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        public void StartTracking()
        {
            _trackingTimer?.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        public void StopTracking()
        {
            _trackingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public WindowInfo GetCurrentActiveWindow()
        {
            IntPtr foregroundWindow = Win32Helper.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return null;

            return GetWindowInfo(foregroundWindow);
        }

        public WindowInfo GetWindowByProcessId(uint processId)
        {
            lock (_lockObject)
            {
                return _trackedWindows.TryGetValue(processId, out var windowInfo) ? windowInfo : null;
            }
        }

        public List<WindowInfo> GetAllTrackedWindows()
        {
            lock (_lockObject)
            {
                return new List<WindowInfo>(_trackedWindows.Values);
            }
        }

        public MonitorInfo GetWindowMonitor(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return _monitorManager.GetPrimaryMonitor();

            if (Win32Helper.GetWindowRect(windowHandle, out var rect))
            {
                // Check if window is minimized (Windows puts minimized windows at -32000, -32000)
                if (IsIconic(windowHandle) || rect.Left < -30000 || rect.Top < -30000)
                {
                    // Use last known position for minimized windows
                    if (_lastKnownPositions.TryGetValue(windowHandle, out var lastRect))
                    {
                        return _monitorManager.GetMonitorContainingWindow(lastRect);
                    }
                    // If no last known position, return primary monitor
                    return _monitorManager.GetPrimaryMonitor();
                }
                else
                {
                    // Store current position for future reference
                    _lastKnownPositions[windowHandle] = rect;
                    return _monitorManager.GetMonitorContainingWindow(rect);
                }
            }

            return _monitorManager.GetPrimaryMonitor();
        }

        public MonitorInfo GetProcessMonitor(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return GetWindowMonitor(process.MainWindowHandle);
                }
            }
            return _monitorManager.GetPrimaryMonitor();
        }

        private void TrackActiveWindows(object state)
        {
            try
            {
                IntPtr foregroundWindow = Win32Helper.GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return;

                // Check for focus change
                if (foregroundWindow != _lastForegroundWindow)
                {
                    var windowInfo = GetWindowInfo(foregroundWindow);
                    if (windowInfo != null)
                    {
                        OnWindowFocusChanged(windowInfo);
                        UpdateTrackedWindow(windowInfo);
                    }
                    _lastForegroundWindow = foregroundWindow;
                }

                // Update positions of currently tracked windows
                UpdateTrackedWindowPositions();
            }
            catch (Exception ex)
            {
                // Log the exception in a real application
                Debug.WriteLine($"WindowTracker error: {ex.Message}");
            }
        }

        private WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return null;

            try
            {
                // Get window title
                var titleBuilder = new StringBuilder(256);
                Win32Helper.GetWindowText(windowHandle, titleBuilder, titleBuilder.Capacity);
                string title = titleBuilder.ToString();

                // Get process ID
                Win32Helper.GetWindowThreadProcessId(windowHandle, out uint processId);

                // Get window position
                Win32Helper.GetWindowRect(windowHandle, out var rect);

                // Determine which monitor it's on
                var monitor = _monitorManager.GetMonitorContainingWindow(rect);

                // Get process name
                string processName = "";
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                }
                catch
                {
                    processName = "Unknown";
                }

                return new WindowInfo
                {
                    Handle = windowHandle,
                    Title = title,
                    ProcessName = processName,
                    ProcessId = processId,
                    Rectangle = rect,
                    Monitor = monitor,
                    LastUpdate = DateTime.Now
                };
            }
            catch
            {
                return null;
            }
        }

        private void UpdateTrackedWindow(WindowInfo windowInfo)
        {
            if (windowInfo == null)
                return;

            lock (_lockObject)
            {
                if (_trackedWindows.TryGetValue(windowInfo.ProcessId, out var existingWindow))
                {
                    // Check for monitor change
                    if (existingWindow.Monitor?.Handle != windowInfo.Monitor?.Handle)
                    {
                        OnWindowMoved(windowInfo, existingWindow.Monitor, windowInfo.Monitor);
                    }
                }

                _trackedWindows[windowInfo.ProcessId] = windowInfo;
            }
        }

        private void UpdateTrackedWindowPositions()
        {
            lock (_lockObject)
            {
                var windowsToUpdate = new List<uint>(_trackedWindows.Keys);

                foreach (var processId in windowsToUpdate)
                {
                    var existingWindow = _trackedWindows[processId];

                    // Check if window still exists
                    if (Win32Helper.GetWindowRect(existingWindow.Handle, out var currentRect))
                    {
                        var currentMonitor = _monitorManager.GetMonitorContainingWindow(currentRect);

                        // Has position or monitor changed?
                        if (currentMonitor?.Handle != existingWindow.Monitor?.Handle)
                        {
                            var updatedWindow = new WindowInfo
                            {
                                Handle = existingWindow.Handle,
                                Title = existingWindow.Title,
                                ProcessName = existingWindow.ProcessName,
                                ProcessId = existingWindow.ProcessId,
                                Rectangle = currentRect,
                                Monitor = currentMonitor,
                                LastUpdate = DateTime.Now
                            };

                            OnWindowMoved(updatedWindow, existingWindow.Monitor, currentMonitor);
                            _trackedWindows[processId] = updatedWindow;
                        }
                    }
                    else
                    {
                        // Window no longer exists, stop tracking
                        _trackedWindows.Remove(processId);
                    }
                }
            }
        }

        private void OnWindowMoved(WindowInfo window, MonitorInfo oldMonitor, MonitorInfo newMonitor)
        {
            WindowMoved?.Invoke(this, new WindowMovedEventArgs(window, oldMonitor, newMonitor));
        }

        private void OnWindowFocusChanged(WindowInfo window)
        {
            WindowFocusChanged?.Invoke(this, new WindowFocusChangedEventArgs(window));
        }

        public void Dispose()
        {
            _trackingTimer?.Dispose();
        }
    }

    public class WindowMovedEventArgs : EventArgs
    {
        public WindowInfo Window { get; }
        public MonitorInfo OldMonitor { get; }
        public MonitorInfo NewMonitor { get; }

        public WindowMovedEventArgs(WindowInfo window, MonitorInfo oldMonitor, MonitorInfo newMonitor)
        {
            Window = window;
            OldMonitor = oldMonitor;
            NewMonitor = newMonitor;
        }
    }

    public class WindowFocusChangedEventArgs : EventArgs
    {
        public WindowInfo Window { get; }

        public WindowFocusChangedEventArgs(WindowInfo window)
        {
            Window = window;
        }
    }
}