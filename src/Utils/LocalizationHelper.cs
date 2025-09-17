using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsNotificationManager.src.Utils
{
    public static class LocalizationHelper
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["tr-TR"] = new Dictionary<string, string>
            {
                ["WindowsNotificationManager"] = "KorOglan'ın Windows Bildirim Yöneticisi",
                ["AppSubtitle"] = "Bildirimleri uygulamaların bulunduğu monitörlerde gösterir",
                ["SystemTrayTooltip"] = "KorOglan'ın Windows Bildirim Yöneticisi",
                ["ShowMainWindow"] = "Ana Pencereyi Göster",
                ["Settings"] = "Ayarlar",
                ["About"] = "Hakkında",
                ["Exit"] = "Çıkış",
                ["AboutMessage"] = "KorOglan'ın Windows Bildirim Yöneticisi v1.0\n\nBu uygulama bildirimleri çoklu monitör kurulumunda uygulamaların bulunduğu monitörlerde gösterir.\n\nGeliştirici: KorOglan\n\nX (Twitter): https://x.com/KorOglan\nGitHub: https://github.com/KilimcininKorOglu",
                ["AppRunning"] = "Çalışıyor",
                ["MonitorsDetected"] = "{0} monitör tespit edildi",
                ["WindowsTracked"] = "{0} pencere izleniyor",
                ["Error"] = "Hata",
                ["Information"] = "Bilgi",
                ["Warning"] = "Uyarı",
                ["SystemStatus"] = "Sistem Durumu",
                ["ServiceStatus"] = "Servis Durumu:",
                ["MonitorCount"] = "Monitör Sayısı:",
                ["TrackedWindows"] = "İzlenen Pencere:",
                ["PrimaryMonitor"] = "Ana Monitör",
                ["Resolution"] = "Çözünürlük",
                ["ActiveWindows"] = "Aktif Pencereler",
                ["Application"] = "Uygulama",
                ["Title"] = "Başlık",
                ["Monitor"] = "Monitör",
                ["RefreshWindows"] = "Pencereleri Yenile",
                ["MinimizeToTray"] = "Sistem Tepsisine Gizle",
                ["LoadingMonitorsError"] = "Monitörler yüklenirken hata: {0}",
                ["GeneralSettings"] = "Genel Ayarlar",
                ["LoadingSettingsError"] = "Ayarlar yüklenirken hata: {0}",
                ["SavingSettingsError"] = "Ayarlar kaydedilirken hata: {0}",
                ["SettingsApplied"] = "Ayarlar uygulandı.",
                ["StartWithWindowsText"] = "Windows ile birlikte başlat",
                ["EnableDebugLogging"] = "Debug log dosyası oluştur",
                ["RestoreDefaults"] = "Varsayılanlara Dön",
                ["Apply"] = "Uygula",
                ["ConfirmRestoreDefaults"] = "Tüm ayarları varsayılan değerlere döndürmek istediğinizden emin misiniz?",
                ["Confirmation"] = "Onay",
                ["Loading"] = "Yükleniyor...",
                ["Calculating"] = "Hesaplanıyor...",
                ["Index"] = "Index",
            },
            ["en-US"] = new Dictionary<string, string>
            {
                ["WindowsNotificationManager"] = "KorOglan's Windows Notification Manager",
                ["AppSubtitle"] = "Displays notifications on the monitor where applications are located",
                ["SystemTrayTooltip"] = "KorOglan's Windows Notification Manager",
                ["ShowMainWindow"] = "Show Main Window",
                ["Settings"] = "Settings",
                ["About"] = "About",
                ["Exit"] = "Exit",
                ["AboutMessage"] = "KorOglan's Windows Notification Manager v1.0\n\nThis application displays notifications on the monitor where applications are located in multi-monitor setups.\n\nDeveloper: KorOglan\n\nX (Twitter): https://x.com/KorOglan\nGitHub: https://github.com/KilimcininKorOglu",
                ["AppRunning"] = "Running",
                ["MonitorsDetected"] = "{0} monitors detected",
                ["WindowsTracked"] = "{0} windows tracked",
                ["Error"] = "Error",
                ["Information"] = "Information",
                ["Warning"] = "Warning",
                ["SystemStatus"] = "System Status",
                ["ServiceStatus"] = "Service Status:",
                ["MonitorCount"] = "Monitor Count:",
                ["TrackedWindows"] = "Tracked Windows:",
                ["PrimaryMonitor"] = "Primary Monitor",
                ["Resolution"] = "Resolution",
                ["ActiveWindows"] = "Active Windows",
                ["Application"] = "Application",
                ["Title"] = "Title",
                ["Monitor"] = "Monitor",
                ["RefreshWindows"] = "Refresh Windows",
                ["MinimizeToTray"] = "Minimize to System Tray",
                ["LoadingMonitorsError"] = "Error loading monitors: {0}",
                ["GeneralSettings"] = "General Settings",
                ["LoadingSettingsError"] = "Error loading settings: {0}",
                ["SavingSettingsError"] = "Error saving settings: {0}",
                ["SettingsApplied"] = "Settings applied.",
                ["StartWithWindowsText"] = "Start with Windows",
                ["EnableDebugLogging"] = "Enable debug logging",
                ["RestoreDefaults"] = "Restore Defaults",
                ["Apply"] = "Apply",
                ["ConfirmRestoreDefaults"] = "Are you sure you want to restore all settings to default values?",
                ["Confirmation"] = "Confirmation",
                ["Loading"] = "Loading...",
                ["Calculating"] = "Calculating...",
                ["Index"] = "Index",
            }
        };

        private static string _currentLanguage;

        static LocalizationHelper()
        {
            DetectSystemLanguage();
        }

        public static void DetectSystemLanguage()
        {
            var systemCulture = CultureInfo.CurrentUICulture;
            var languageCode = systemCulture.Name;

            // Support Turkish and English, default to English
            if (languageCode.StartsWith("tr"))
            {
                _currentLanguage = "tr-TR";
            }
            else
            {
                _currentLanguage = "en-US";
            }
        }

        public static string GetString(string key, params object[] args)
        {
            if (Translations.TryGetValue(_currentLanguage, out var translations) &&
                translations.TryGetValue(key, out var translation))
            {
                if (args != null && args.Length > 0)
                {
                    return string.Format(translation, args);
                }
                return translation;
            }

            // Fallback to English
            if (_currentLanguage != "en-US" &&
                Translations.TryGetValue("en-US", out var englishTranslations) &&
                englishTranslations.TryGetValue(key, out var englishTranslation))
            {
                if (args != null && args.Length > 0)
                {
                    return string.Format(englishTranslation, args);
                }
                return englishTranslation;
            }

            // Ultimate fallback
            return key;
        }

        public static string CurrentLanguage => _currentLanguage;

        public static bool IsTurkish => _currentLanguage == "tr-TR";

        public static bool IsEnglish => _currentLanguage == "en-US";

        public static void SetLanguage(string languageCode)
        {
            if (Translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
            }
        }
    }
}