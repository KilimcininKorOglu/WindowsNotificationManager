using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsNotificationManager.src.Utils
{
    /// <summary>
    /// Provides multi-language localization support with automatic system language detection.
    /// Supports Turkish and English languages with intelligent fallback mechanisms.
    /// All UI text is dynamically localized based on Windows system language settings.
    /// </summary>
    public static class LocalizationHelper
    {
        /// <summary>
        /// Complete translation dictionary containing all supported languages and their text mappings.
        /// Uses nested dictionaries: outer key is language code (tr-TR, en-US), inner key is text identifier.
        /// All UI elements reference these keys to display localized text dynamically.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            // Turkish language translations with complete UI text coverage
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
            // English language translations serving as both fallback and primary language for international users
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

        /// <summary>
        /// Currently active language code (tr-TR or en-US) determined by system detection or manual override.
        /// Used by GetString() to retrieve appropriate translations for UI elements.
        /// </summary>
        private static string _currentLanguage;

        /// <summary>
        /// Static constructor that automatically detects and sets the system language on first use.
        /// Ensures localization is ready immediately when any UI component requests text.
        /// </summary>
        static LocalizationHelper()
        {
            DetectSystemLanguage();
        }

        /// <summary>
        /// Automatically detects the Windows system language and sets appropriate localization.
        /// Uses CurrentUICulture to determine user's preferred language from Windows settings.
        /// Supports Turkish (tr-*) detection with English as fallback for all other languages.
        /// Called automatically by static constructor and can be manually invoked to refresh language settings.
        /// </summary>
        public static void DetectSystemLanguage()
        {
            // Get Windows system UI culture settings from current user
            var systemCulture = CultureInfo.CurrentUICulture;
            var languageCode = systemCulture.Name;

            // Language detection logic: Turkish variants vs. everything else defaults to English
            if (languageCode.StartsWith("tr"))
            {
                // Any Turkish locale (tr-TR, tr-CY, etc.) uses Turkish translations
                _currentLanguage = "tr-TR";
            }
            else
            {
                // All other languages default to English for international compatibility
                _currentLanguage = "en-US";
            }
        }

        /// <summary>
        /// Retrieves localized text for the specified key with optional string formatting support.
        /// Implements intelligent 3-tier fallback system: current language → English → raw key.
        /// Supports parameterized strings using standard .NET string.Format() syntax.
        /// This is the main method called by all UI components to get translated text.
        /// </summary>
        /// <param name="key">Text identifier key used to look up translations</param>
        /// <param name="args">Optional formatting arguments for parameterized strings (e.g., "{0} monitors detected")</param>
        /// <returns>Localized and formatted text string, with automatic fallback handling</returns>
        public static string GetString(string key, params object[] args)
        {
            // PRIMARY: Try to get translation in current language (Turkish or English)
            if (Translations.TryGetValue(_currentLanguage, out var translations) &&
                translations.TryGetValue(key, out var translation))
            {
                // Apply string formatting if parameters are provided
                if (args != null && args.Length > 0)
                {
                    return string.Format(translation, args);
                }
                return translation;
            }

            // SECONDARY FALLBACK: If current language is not English, try English translation
            // This handles cases where Turkish translation might be missing
            if (_currentLanguage != "en-US" &&
                Translations.TryGetValue("en-US", out var englishTranslations) &&
                englishTranslations.TryGetValue(key, out var englishTranslation))
            {
                // Apply string formatting to English fallback if parameters are provided
                if (args != null && args.Length > 0)
                {
                    return string.Format(englishTranslation, args);
                }
                return englishTranslation;
            }

            // ULTIMATE FALLBACK: Return the raw key if no translation found in any language
            // This prevents UI from breaking and shows developers which keys need translation
            return key;
        }

        /// <summary>
        /// Gets the currently active language code (tr-TR or en-US).
        /// Used by UI components to determine which language is currently being displayed.
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Convenience property to check if the current language is Turkish.
        /// Used for language-specific UI behavior and conditional display logic.
        /// </summary>
        public static bool IsTurkish => _currentLanguage == "tr-TR";

        /// <summary>
        /// Convenience property to check if the current language is English.
        /// Used for language-specific UI behavior and conditional display logic.
        /// </summary>
        public static bool IsEnglish => _currentLanguage == "en-US";

        /// <summary>
        /// Manually sets the active language, overriding automatic system detection.
        /// Useful for testing different languages or providing user language preferences.
        /// Only accepts language codes that exist in the Translations dictionary.
        /// </summary>
        /// <param name="languageCode">Language code to set (must be "tr-TR" or "en-US")</param>
        public static void SetLanguage(string languageCode)
        {
            // Validate that the requested language is supported before switching
            if (Translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
            }
            // Silently ignore invalid language codes to prevent breaking the UI
        }
    }
}