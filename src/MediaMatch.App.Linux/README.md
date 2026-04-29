# MediaMatch.App.Linux — Uno Platform Head

> **Status: Sprint 2 — Scaffolded, ready for Linux build testing.**

This project is the Linux head for MediaMatch, using [Uno Platform](https://platform.uno/) 6.x with Skia + X11 to render WinUI 3 XAML on Linux desktops.

## Prerequisites

1. **Linux with X11 display server** (Wayland via XWayland also works)
2. **.NET 10 SDK installed**
3. **X11 development libraries**: `sudo apt install libx11-dev` (Ubuntu/Debian) or equivalent
4. **ffprobe** (optional, for MediaInfo Inspector): Install via `sudo apt install ffmpeg`

## Building

```bash
# From repo root, on Linux:
dotnet build src/MediaMatch.App.Linux/MediaMatch.App.Linux.csproj

# Run:
dotnet run --project src/MediaMatch.App.Linux
```

> **Note:** This project is **not** included in the Visual Studio solution (`MediaMatch.slnx`) because it would fail Windows builds. Build it standalone on Linux.

## Architecture

This project has its own:
- `App.xaml.cs` — Composition root with DI (mirrors Windows/macOS but uses Unix infrastructure)
- `MainWindow.xaml` — NavigationView shell (no Mica backdrop — uses native X11 chrome)
- Pages: HomePage, SettingsPage, HistoryPage, AboutPage
- Controls: EpisodesPanel, SubtitlePanel, SfvPanel, FilterPanel, ListPanel
- Dialogs: LogViewer, PresetEditor, PresetManager, ExpressionEditor, MediaInfoInspector, MatchSelection, Conflict, KeyboardShortcuts

Shared layers (referenced as project dependencies):
- `MediaMatch.Core` — Domain models, interfaces
- `MediaMatch.Application` — Services, pipeline orchestration
- `MediaMatch.Infrastructure` — HTTP clients, providers, caching
- `MediaMatch.Infrastructure.Unix` — POSIX hard links, AES encryption, network detection

## What's Different From Windows

| Feature | Windows | Linux |
|---------|---------|-------|
| Rendering | WinUI 3 (native) | Uno Skia + X11 |
| Encryption | DPAPI (current user) | AES-256-GCM with key file |
| Hard links | `CreateHardLink` (kernel32) | POSIX `link()` syscall |
| Network detection | `GetDriveType` (kernel32) | `/proc/mounts` parsing |
| File clone | ReFS CoW → hard link → copy | Hard link → copy |
| Backdrop | Mica | Native X11 chrome |
| Title bar | Custom WinUI title bar | Platform native |
| Shell extension | Windows context menu | Not available |
| Keyboard shortcuts | Ctrl+key | Ctrl+key (same) |
