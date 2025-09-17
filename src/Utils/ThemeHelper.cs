using Microsoft.Win32;
using System;

namespace WindowsNotificationManager.src.Utils
{
    public static class ThemeHelper
    {
        public static bool IsWindowsDarkMode()
        {
            try
            {
                // Check Windows 10/11 theme setting
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                        if (appsUseLightTheme != null)
                        {
                            // 0 = Dark mode, 1 = Light mode
                            return (int)appsUseLightTheme == 0;
                        }
                    }
                }

                // Fallback: check system theme
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var systemUsesLightTheme = key.GetValue("SystemUsesLightTheme");
                        if (systemUsesLightTheme != null)
                        {
                            return (int)systemUsesLightTheme == 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.WriteLine($"Error detecting Windows theme: {ex.Message}");
            }

            // Default to light mode if detection fails
            return false;
        }

        public static NotificationTheme GetNotificationTheme()
        {
            return IsWindowsDarkMode() ? NotificationTheme.Dark : NotificationTheme.Light;
        }
    }

    public enum NotificationTheme
    {
        Light,
        Dark
    }

    public class NotificationColors
    {
        public string Background { get; set; }
        public string Border { get; set; }
        public string AppNameForeground { get; set; }
        public string TitleForeground { get; set; }
        public string MessageForeground { get; set; }
        public string CloseButtonForeground { get; set; }
        public string CloseButtonHoverBackground { get; set; }

        public static NotificationColors Light => new NotificationColors
        {
            Background = "#F3F3F3",
            Border = "#E1E1E1",
            AppNameForeground = "#323130",
            TitleForeground = "#323130",
            MessageForeground = "#605E5C",
            CloseButtonForeground = "#666666",
            CloseButtonHoverBackground = "#E5E5E5"
        };

        public static NotificationColors Dark => new NotificationColors
        {
            Background = "#2D2D30",
            Border = "#3E3E42",
            AppNameForeground = "#FFFFFF",
            TitleForeground = "#FFFFFF",
            MessageForeground = "#CCCCCC",
            CloseButtonForeground = "#CCCCCC",
            CloseButtonHoverBackground = "#404040"
        };
    }
}