# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KorOglan's Windows Notification Manager - A C# WPF application that intercepts Windows notifications and redirects them to the monitor where the source application is located in multi-monitor setups. The application uses Windows API hooks to capture native notifications in real-time and moves them to the appropriate monitor while preserving Windows default positioning.

**Current Version:** 1.4.0

## Architecture

### Core Components

- **WindowsAPIHook** (`src/Core/WindowsAPIHook.cs`): Core component that intercepts system-wide window events using `SetWinEventHook` and repositions notification windows using `SetWindowPos` API
- **MonitorManager** (`src/Core/MonitorManager.cs`): Detects and manages multiple monitors using Win32 APIs (`EnumDisplayMonitors`)
- **WindowTracker** (`src/Core/WindowTracker.cs`): Tracks active windows, their positions, and determines which monitor they are on using a periodic timer. Includes minimized window position caching with `IsIconic()` P/Invoke to maintain accurate monitor tracking for minimized applications
- **NotificationRouter** (`src/Core/NotificationRouter.cs`): Contains logic to decide which monitor a notification should be routed to
- **NotificationService** (`src/Services/NotificationService.cs`): Central service that orchestrates all core components

### Key Features

1. **Real-time Notification Interception**: Uses Windows API hooks to capture and redirect native notifications
2. **Multi-Monitor Detection**: Uses Win32 APIs to enumerate and track multiple monitors
3. **Window Position Tracking**: Continuously monitors application window positions
4. **System Tray Integration**: Runs as background service with system tray interface
5. **Settings Management**: Registry-based configuration storage with integrated UI

## Technical Stack

- **.NET 9.0** with `net9.0-windows` target framework
- **WPF** (`<UseWPF>true</UseWPF>`) for main UI components
- **Windows Forms** (`<UseWindowsForms>true</UseWindowsForms>`) for system tray `NotifyIcon` and `ContextMenuStrip`
- **Win32 APIs** via P/Invoke (`src/Utils/Win32Helper.cs`) for system-level operations
- **No External Dependencies** - Uses built-in .NET and WindowsDesktop SDKs only
- **Windows API Hooks** (`SetWinEventHook`, `SetWindowPos`) for real-time notification interception

## Development Commands

### Prerequisites

- .NET 9.0 SDK or later
- Windows 10/11 (Administrator privileges required)
- Visual Studio 2022 or VS Code with C# extension

### Build

```bash
dotnet build
```

### Run (Debug) - Must be Administrator

```bash
# CRITICAL: Must run in elevated command prompt/PowerShell
dotnet run
```

### Clean Build

```bash
dotnet clean && dotnet build
```

### Publish for Release

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Project File

The main project file is `WindowsNotificationManager.csproj` - no solution file is used.

## Win32 API Integration

The application heavily uses Win32 APIs through P/Invoke (`src/Utils/Win32Helper.cs`):

- `SetWinEventHook` - System-wide window event monitoring
- `SetWindowPos` - Notification window repositioning
- `EnumDisplayMonitors` - Monitor enumeration (`src/Utils/MonitorUtils.cs`)
- `GetMonitorInfo` - Monitor details and properties
- `GetForegroundWindow` - Active window detection
- `GetWindowRect` - Window position and bounds
- `MonitorFromWindow` - Window-to-monitor mapping

Key structures: `RECT`, `MONITORINFO`, `WINEVENTPROC`

## UI Components

### Main Window (`src/UI/MainWindow.xaml`)

The primary UI with distinct sections:

- **System Status**: Service status, monitor count, and tracked window count
- **Monitors ListView**: Index, Primary status, and Resolution for each detected monitor
- **Active Windows ListView**: Application name, window title, and monitor assignment
- **Settings GroupBox**: "Start with Windows" and "Enable debug logging" checkboxes
- **Localized UI**: All text content set via `SetLocalizedTexts()` method in code-behind

### System Tray (`src/UI/TrayIcon.cs`)

Programmatically created system tray integration:

- **Custom Icon**: Embedded `tray.ico` resource (blue notification bell with red indicator dot), fallback to `SystemIcons.Information`
- **ContextMenuStrip**: "Show Main Window", "About", and "Exit" options
- **Double-click**: Opens main window
- **Icon Loading**: Uses `Assembly.GetManifestResourceStream()` for embedded icon with automatic fallback

## Localization System

Dynamic multi-language support (`src/Utils/LocalizationHelper.cs`):

- Auto-detects Windows system language (Turkish/English) via `CultureInfo.CurrentUICulture`
- ~35 localized strings with parameter formatting support (`string.Format`)
- Fallback chain: Requested language → English → Raw key
- All UI text set programmatically in code-behind via `SetLocalizedTexts()` methods
- Key localization strings: `WindowsNotificationManager`, `AppSubtitle`, `SystemStatus`, settings labels

## Windows API Hook System

The application intercepts native Windows notifications using:

- **WinEvent Hooks**: SetWinEventHook API to monitor window creation and location changes
- **SetWindowPos API**: Redirects notifications to target monitors
- **Process Detection**: Identifies notification windows by process name (explorer.exe, ShellExperienceHost.exe)
- **Class Name Filtering**: Detects Windows.UI.Core.CoreWindow and notification-specific classes
- **Relative Position Preservation**: Maintains Windows default notification positioning while changing monitors
- **Windows 11 Compatibility**: Handles differences between Windows 10/11 notification systems
- **Ultra-Wide Monitor Support**: Bounds checking prevents notifications from appearing outside target monitor boundaries (fixes 49" Samsung monitor issues)

## Settings & Configuration

Settings stored in Windows Registry under:
`HKEY_CURRENT_USER\SOFTWARE\KorOglansWindowsNotificationManager`

**Current Active Settings:**

- `StartWithWindows` (REG_DWORD): `1` to enable, `0` to disable - Controls addition to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `EnableDebugLogging` (REG_DWORD): `1` to enable, `0` to disable - Controls debug log file creation

**Default Values (first-time installation):**

- `StartWithWindows`: `1` (enabled)
- `EnableDebugLogging`: `0` (disabled)

Registry operations managed in `src/UI/MainWindow.xaml.cs` with `LoadSettings()` and `SaveSettings()` methods.

## Admin Requirements

The application requires administrator privileges for:

- System-wide window monitoring (`SetWinEventHook` access)
- Low-level Windows API access for notification repositioning
- Registry modifications for startup configuration

**Configuration**: `app.manifest` with `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`

## Event-Driven Architecture

The system uses events for communication:

- `WindowMoved` - Window position changes
- `MonitorConfigurationChanged` - Display changes
- `NotificationRouted` - Routing confirmations

## Threading Model

- **UI Thread**: WPF interface updates and user interactions
- **Background Timer**: Window position polling in `WindowTracker` (500ms intervals)
- **WinEvent Callbacks**: Real-time system-wide window event processing from `SetWinEventHook`
- **Async Operations**: Notification positioning via `SetWindowPos` API calls
- **Startup Sequence**: `App.xaml.cs` initializes services in order (TrayIcon → NotificationService → MainWindow)

## Performance Optimizations

**Critical performance optimizations implemented to handle high-frequency system-wide hook callbacks:**

### **Hook Performance (`WindowsAPIHook.cs`)**

- **Early Filtering**: `if (idObject != 0 || hwnd == IntPtr.Zero) return;` filters 90%+ of irrelevant events before processing
- **Event Type Filtering**: Only processes `EVENT_OBJECT_CREATE`, `EVENT_OBJECT_SHOW`, and `EVENT_OBJECT_LOCATIONCHANGE` events
- **Process Name Caching**: 5-minute cache with periodic cleanup prevents expensive `Process.GetProcessById()` calls
- **Smart Move Detection**: `return false` when notification already on correct monitor to skip unnecessary `SetWindowPos` calls
- **String Optimization**: Uses `StringComparison.OrdinalIgnoreCase` instead of `ToLower().Contains()` for 80%+ performance gain

### **Logging Performance (`DebugLogger.cs`)**

- **Registry Caching**: 30-second cache for `EnableDebugLogging` setting prevents frequent registry reads
- **Cache Invalidation**: `InvalidateCache()` method called when settings change in MainWindow

### **Statistics & Monitoring**

- **Performance Stats**: Reports every 1 minute with hook event counts, filter efficiency, cache hit rates
- **Cache Monitoring**: Process cache shows hits/misses/size; Registry cache shows hit rate percentage
- **Expected Performance**: 92%+ filter efficiency, 92%+ process cache hit rate, 97%+ registry cache hit rate

### **Performance Impact**

- **CPU Usage**: ~60% reduction in hook callback overhead through early filtering
- **Memory**: Minimal cache overhead (~2MB), eliminates repeated string allocations
- **API Calls**: Prevents unnecessary SetWindowPos calls when notification already on correct monitor

## Development Notes

- **Application Startup**: `App.xaml.cs` creates TrayIcon → NotificationService → MainWindow (shows then minimizes to taskbar)
- **Git Repository**: Remote origin points to `git@github-kilimci:KilimcininKorOglu/WindowsNotificationManager.git`
- **Window Tracking**: Continuous polling via `Timer` in `WindowTracker` (500ms intervals)
- **Monitor Detection**: Dynamic support for plug/unplug via `MonitorManager`
- **Notification Detection**: Process-specific logic - `explorer.exe` (Win11) vs `ShellExperienceHost.exe` (Win10)
- **Localization**: Auto-detects system language via `CultureInfo.CurrentUICulture` on startup
- **UI Architecture**: Single main window with integrated settings (no separate dialogs)
- **Debug Logging**: File location determined by `Assembly.GetExecutingAssembly().Location` in `DebugLogger.cs`
- **Icon Management**: Both `app.ico` (taskbar) and `tray.ico` (system tray) are embedded resources

## Important Dependencies

The project uses built-in .NET 9.0 libraries without external NuGet packages:

- **System.Windows.Forms** - System tray integration and NotifyIcon
- **System.Drawing.Common** - Icon creation and graphics operations
- **WindowsDesktop SDK** - Win32 API access and WPF components

## Service Lifecycle

**1. Application Startup (`App.xaml.cs`)**:

- `DebugLogger.ClearLog()` - Clear previous debug log
- `TrayIcon` creation and initialization
- `NotificationService` instantiation and `Start()`
- `MainWindow` creation (shows then minimizes to taskbar)

**2. NotificationService.Start() Sequence**:

- `MonitorManager` begins display monitoring
- `WindowTracker` starts position polling timer (500ms)
- `WindowsAPIHook` establishes `SetWinEventHook` with events `EVENT_OBJECT_CREATE` to `EVENT_OBJECT_LOCATIONCHANGE`
- Event handlers wire up between components

**3. Application Startup Flow (`App.xaml.cs`)**:

- `DebugLogger.ClearLog()` clears previous session logs
- `MainWindow` created and shown, then immediately minimized to taskbar (`ShowInTaskbar = true`)
- `TrayIcon` initialized with embedded custom icon
- `NotificationService` started with all core components

## Testing the Application

The application intercepts real Windows notifications automatically:

- Generate notifications from any application (WhatsApp, Outlook, Teams, etc.)
- Notifications will be moved to the monitor where the source application is located
- Check `notification_debug.log` for detailed hook activity and process detection
- Use main window to monitor active windows and their monitor assignments
- Enable debug logging from settings to see detailed API hook activity

## Debugging Tips

- **CRITICAL**: Must run with administrator privileges - required for `SetWinEventHook` access
- **Debug Log**: Check `notification_debug.log` (same directory as .exe) for detailed hook activity
- **Log Control**: Toggle logging via "Enable debug logging" checkbox in main window settings
- **Log Management**: File cleared on each app startup via `DebugLogger.ClearLog()`
- **Process Monitoring**: Windows 11 monitors `explorer.exe`, Windows 10 monitors `ShellExperienceHost.exe`
- **System Detection**: `WindowsVersionHelper.cs` automatically detects OS version for appropriate process targeting
- **Service Verification**: Use Task Manager to confirm background operation
- **Performance Monitoring**: High CPU usage indicates hook callback frequency; check process cache hit rate in debug logs
- **Cache Debugging**: Process cache cleanup logs show cache efficiency and cleanup frequency
- **Minimized Window Tracking**: If minimized windows show incorrect monitor in Active Windows list, check `_lastKnownPositions` cache and `IsIconic()` detection in WindowTracker.cs
- **Ultra-Wide Monitor Issues**: If notifications appear in wrong positions on ultra-wide monitors (49" Samsung), check bounds checking logic in `CalculateTargetPosition()` method
