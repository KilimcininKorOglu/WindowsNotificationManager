using System;
using System.Collections.Generic;
using static WindowsNotificationManager.src.Utils.Win32Helper;

namespace WindowsNotificationManager.src.Utils
{
    public class MonitorInfo
    {
        public IntPtr Handle { get; set; }
        public RECT Bounds { get; set; }
        public RECT WorkArea { get; set; }
        public bool IsPrimary { get; set; }
        public int Index { get; set; }

        public bool ContainsPoint(int x, int y)
        {
            return x >= Bounds.Left && x < Bounds.Right &&
                   y >= Bounds.Top && y < Bounds.Bottom;
        }

        public bool ContainsWindow(RECT windowRect)
        {
            // Check which monitor the window center point is on
            int centerX = windowRect.Left + windowRect.Width / 2;
            int centerY = windowRect.Top + windowRect.Height / 2;
            return ContainsPoint(centerX, centerY);
        }
    }

    public static class MonitorUtils
    {
        public static List<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo>();
            int index = 0;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);

                if (GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    monitors.Add(new MonitorInfo
                    {
                        Handle = hMonitor,
                        Bounds = monitorInfo.rcMonitor,
                        WorkArea = monitorInfo.rcWork,
                        IsPrimary = (monitorInfo.dwFlags & MONITORINFO.MONITORINFOF_PRIMARY) != 0,
                        Index = index++
                    });
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            return monitors;
        }

        public static MonitorInfo GetMonitorFromWindow(IntPtr windowHandle)
        {
            IntPtr monitorHandle = MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);

            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);

            if (GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return new MonitorInfo
                {
                    Handle = monitorHandle,
                    Bounds = monitorInfo.rcMonitor,
                    WorkArea = monitorInfo.rcWork,
                    IsPrimary = (monitorInfo.dwFlags & MONITORINFO.MONITORINFOF_PRIMARY) != 0
                };
            }

            return null;
        }

        public static MonitorInfo GetMonitorFromPoint(int x, int y)
        {
            var monitors = GetAllMonitors();
            foreach (var monitor in monitors)
            {
                if (monitor.ContainsPoint(x, y))
                    return monitor;
            }
            return null;
        }
    }
}