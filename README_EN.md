# KorOglan's Windows Notification Manager v1.4.0

A desktop application developed to display Windows notifications on the correct monitor in multi-monitor setups.

## Features

- **Smart Notification Routing**: Automatically routes notifications to the monitor where the source application is located
- **Real-time Capture**: Instantly captures native notifications using Windows API hooks
- **Multi-Monitor Support**: Supports unlimited monitor configurations
- **Windows Default Position Preservation**: Maintains Windows default positioning when moving notifications
- **System Tray Integration**: Runs in background and controlled from system tray
- **Multi-language Support**: Turkish and English interface
- **Debug Logging**: Optional detailed debug log system
- **Performance Optimization**: Low CPU usage with 90%+ event filtering and caching system
- **Minimized Window Tracking**: Accurate monitor tracking for applications minimized to taskbar
- **Ultra-Wide Monitor Support**: Proper notification positioning from ultra-wide monitors (49" Samsung) to normal monitors
- **Windows Native Positioning**: Consistent bottom-right corner positioning across all monitor resolutions

## System Requirements

- Windows 10/11 (64-bit)
- .NET 9.0 Runtime
- Administrator privileges (required for Windows API access)
- Multi-monitor setup

## Installation

1. Download the latest version from the [Releases](../../releases) page
2. Run `WindowsNotificationManager.exe`
3. Click "Yes" when Windows requests administrator permissions
4. The application will automatically start in the system tray

## Usage

### First Run

- After startup, the application continues running in the system tray
- Double-click the system tray icon to open the main window
- The main window displays monitor information and active windows

### Settings

From the settings section in the main window:

- **Start with Windows**: Automatically run the application at system startup
- **Enable debug logging**: Detailed logging for troubleshooting

### How It Works

1. The application continuously tracks all open windows
2. When a notification appears, it detects which monitor the source application is on
3. Automatically moves the notification to that monitor
4. Preserves Windows default positioning for natural appearance

## Technical Details

### Architecture

- **WindowsAPIHook** (`src/Core/`): Notification capture with WinEvent hooks
- **MonitorManager** (`src/Core/`): Multi-monitor management
- **WindowTracker** (`src/Core/`): Window position tracking
- **NotificationRouter** (`src/Core/`): Notification routing logic
- **NotificationService** (`src/Services/`): Main orchestrator service

### API Integration

- `SetWinEventHook` - System-wide window event capture
- `SetWindowPos` - Change notification position
- `EnumDisplayMonitors` - Monitor enumeration
- `MonitorFromWindow` - Window-monitor mapping
- `GetForegroundWindow` - Active window detection
- `GetWindowRect` - Window size and position
- `GetMonitorInfo` - Monitor detail information

### Windows Version Support

- **Windows 10**: Works with `ShellExperienceHost.exe` process
- **Windows 11**: Works with `explorer.exe` process
- Automatic version detection and adaptation

## Development

### Build

```bash
dotnet build
```

### Run (Debug) - Administrator Required

```bash
# IMPORTANT: Must run in elevated command prompt/PowerShell
dotnet run
```

### Publish for Release

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Debug Logging

Debug logs are stored in `notification_debug.log` (created next to the executable file).

## Contributing

1. Fork this repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -m 'Add new feature: description'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

## License

This project is released under the MIT license. See the `LICENSE` file for details.

## Contact

- **Twitter/X**: [@KorOglan](https://x.com/KorOglan)
- **GitHub**: [KilimcininKorOglu](https://github.com/KilimcininKorOglu)

## Issue Reporting

If you encounter any issues:

1. Enable debug logging
2. Reproduce the issue
3. Report the issue on the [Issues](../../issues) page with the log file attached

---

**Note**: This application requires administrator privileges as it uses system-level Windows APIs. This is completely safe and only used for notification routing functionality.
