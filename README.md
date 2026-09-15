# 🇫🇷 Prononce for Windows

[![Build Status](https://github.com/Odion-Sonny/prononce_ws/actions/workflows/build.yml/badge.svg)](https://github.com/Odion-Sonny/prononce_ws/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white)](README.md)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Privacy](https://img.shields.io/badge/Privacy-100%25%20Offline-success)](SECURITY.md)

> **Instant, lightweight native Windows French pronunciation utility for learners.**

Highlight French text anywhere on Windows, press **`Ctrl + Shift + F`**, and immediately hear native French pronunciation powered by the built-in Windows speech engine.

No copying. No pasting. No Electron bloat. No subscriptions. No cloud latency. 100% offline and private.

---

## Key Features

- **Instant Global Shortcut (`Ctrl + Shift + F`)**: Select French text in Edge, Chrome, Firefox, Word, Outlook, VS Code, Slack, Notepad, or PDFs, press the hotkey, and listen immediately.
- **Zero-Polling Selection Capture**: Uses **Microsoft UI Automation (UIA)** with a resilient synthetic copy fallback that **instantly restores your prior clipboard contents**—your clipboard history is never overwritten or lost.
- **Windows High-Quality Natural Voices**: Leverages `Windows.Media.SpeechSynthesis` with local French voices (`fr-FR`, `fr-CA`, etc.) installed on Windows (e.g. Microsoft Hortense, Paul, Julie).
- **Learner-Optimized Playback Speed**: Choose from **0.5x**, **0.75x**, **1.0x**, **1.25x**, or **1.5x**—with a calibrated pedagogical speed curve making **0.75x** and **0.5x** noticeably slow, clear, and easy to follow.
- **Floating Pronunciation HUD**: A sleek, non-activating floating card displays the spoken text with instant **↻ Replay** and speed controls. It auto-dismisses after a few seconds and **never steals keyboard focus** from what you were typing (`WS_EX_NOACTIVATE`).
- **Local On-Device English Translation**: Displays immediate French-to-English definitions and translations directly in the floating card using a bundled local offline lexicon database. Toggle between **Automatic**, **On-Demand**, or **Off** via Tray Icon or Settings with 100% offline privacy.
- **System Tray Utility**: Runs quietly in the notification area with zero taskbar clutter.
- **Tiny Resource Footprint**:
  - Idle CPU usage: **0.0%** (zero polling, purely message-driven)
  - Memory: lightweight and self-contained
- **Launch at Startup**: Windows Registry `Run` integration.
- **100% Offline & Private**: Zero analytics, zero accounts, zero cloud requests, zero data collection.

---

## System Requirements

- **Windows 10 (Build 19041+) or Windows 11**.
- .NET 8.0 Runtime (or run the self-contained single-file `.exe` which requires no prerequisites).
- French Speech Voice installed in Windows (e.g. *Microsoft Hortense* or *Microsoft Paul*). The app automatically detects this on first launch and provides a direct shortcut to install it.

---

## Quick Start & Build

### 1. Build Single-File Executable via PowerShell

From a Windows PowerShell terminal:

```powershell
# Build self-contained Release executable
.\scripts\build.ps1

# The output executable is located at:
# dist\Prononce.exe
```

### 2. Run via .NET CLI

```powershell
dotnet run --project src\Prononce\Prononce.csproj
```

### 3. Build via Visual Studio

Open `Prononce.sln` in Visual Studio 2022 and press **F5** (Debug) or **Ctrl+Shift+B** (Build).

---

## How It Works

```text
User highlights French text
           ↓
    Presses Ctrl + Shift + F
           ↓
Win32 RegisterHotKey Dispatcher (0% idle CPU)
           ↓
SelectionService captures selected text:
  1. Tries Microsoft UI Automation (Instant, no clipboard interaction)
  2. If needed: Fast synthetic Ctrl+C + immediate clipboard format restoration
           ↓
Windows.Media.SpeechSynthesis speaks text in French (fr-FR)
           ↓
Non-activating FloatingHUD appears (WS_EX_NOACTIVATE, auto-dismisses)
```

---

## System Tray Controls

Right-click or click the **🇫🇷** icon in the Windows notification area to access:

- **Prononce (`Ctrl + Shift + F`)**: View active shortcut.
- **Speak Selected Text**: Manually trigger pronunciation.
- **Voice**: Choose between installed French voices (Hortense, Paul, Julie, etc.).
- **Speed**: Select playback rate (`0.5x`, `0.75x`, `1.0x`, `1.25x`, `1.5x`).
- **Translation**: Switch between `Automatic`, `On-Demand`, or `Off`.
- **Show Floating Panel**: Toggle the floating HUD card.
- **Launch at Startup**: Enable or disable automatic startup with Windows.
- **Settings...**: Open the native preferences window.
- **Exit**: Terminate Prononce.

---

## Architecture & Codebase

```text
prononce_ws/
├── Prononce.sln                         # Visual Studio solution
├── src/Prononce/
│   ├── Prononce.csproj                  # .NET 8 WPF project file
│   ├── App.xaml                         # Application entry & lifecycle
│   ├── App.xaml.cs                      # Background coordinator & shortcut wiring
│   ├── Native/
│   │   └── Win32.cs                     # P/Invoke definitions (Hotkeys, Input, Clipboard, Styles)
│   ├── Models/
│   │   ├── AppSettings.cs               # JSON-backed observable settings & speed presets
│   │   └── FrenchVoiceInfo.cs           # Model for detected system French voices
│   ├── Services/
│   │   ├── HotkeyService.cs             # Win32 RegisterHotKey & message hook (0% CPU)
│   │   ├── SelectionService.cs          # Dual-strategy: UI Automation + Clipboard-preserving copy
│   │   ├── SpeechService.cs             # WinRT SpeechSynthesizer with non-linear speed curve
│   │   ├── DictionaryService.cs         # Local offline French-English lexicon lookup
│   │   ├── VoiceSetupService.cs         # Voice detection & deep-linking to ms-settings:speech
│   │   └── LaunchAtStartupService.cs    # Windows Registry Run key manager
│   ├── UI/
│   │   ├── TrayIconController.cs        # System tray icon, status indicators & menu
│   │   ├── FloatingHudWindow.xaml       # Non-activating floating pronunciation card (WS_EX_NOACTIVATE)
│   │   ├── FloatingHudWindow.xaml.cs    # Window lifecycle, timers & replay
│   │   ├── SettingsWindow.xaml          # Modern settings window (voice, speed, hotkey, startup)
│   │   ├── SettingsWindow.xaml.cs
│   │   ├── WelcomeWindow.xaml           # First-run onboarding & French voice checker
│   │   └── WelcomeWindow.xaml.cs
│   └── Resources/
│       ├── app.ico                      # Multi-resolution application & system tray icon
│       └── dictionary.json              # Bundled offline French-English lookup lexicon
├── scripts/
│   ├── build.ps1                        # PowerShell script to build self-contained Release .exe
│   └── package_release.ps1              # Creates a distributable ZIP bundle
├── .github/
│   └── workflows/
│       └── build.yml                    # Automated GitHub Actions CI workflow
└── docs/
    └── ARCHITECTURE.md                  # Comprehensive architectural comparison with macOS
```

---

## Contributing

Contributions, bug reports, and feature requests are very welcome!
- Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) for build instructions and guidelines.
- Review [`SECURITY.md`](SECURITY.md) for our privacy guarantees.

---

## License

This project is licensed under the MIT License - see the [`LICENSE`](LICENSE) file for details.
Designed with visual elegance and engineering discipline for Windows.
