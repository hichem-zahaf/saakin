# Release Notes

All notable changes to the Saakin project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Configuration file support for saving favorite settings
- Tray icon application for easier control
- Hotkey support for starting/stopping modes
- Log file output option

## [1.0.0] - 2025-01-XX

### Added
- Mouse Jitter mode with configurable interval, duration, and range
- Auto-Clicker mode with position locking
- Pixel Guard mode with screen region monitoring
- Smart pause/resume functionality when user mouse movement detected
- Support for all mouse buttons (left, right, middle, x1, x2)
- Colored console output for status updates
- Comprehensive command-line help system
- ASCII art banner
- Graceful shutdown via Ctrl+C
- Explorer launch detection for better UX when double-clicking executable

### Security
- All modes automatically stop when user moves mouse
- No external network communication
- Fully offline operation

### Technical
- Target framework: .NET Framework 4.5.2
- Windows API integration via P/Invoke
- System.Drawing for screen capture functionality
- Single-file architecture for simplicity
- No external dependencies

---

## Version Format

- **[Unreleased]** - Features planned for future releases
- **[1.0.0]** - Initial stable release
- **[0.X.X]** - Pre-release versions (if any)

## Categories

- **Added** - New features
- **Changed** - Changes to existing functionality
- **Deprecated** - Features to be removed in future releases
- **Removed** - Features removed in this release
- **Fixed** - Bug fixes
- **Security** - Security-related changes
- **Technical** - Technical implementation details

## Upgrade Instructions

### From 0.X.X to 1.0.0

No special upgrade instructions. Simply replace the executable with the new version.

All command-line arguments remain backward compatible.
