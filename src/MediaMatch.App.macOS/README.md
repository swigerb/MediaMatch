# MediaMatch.App.macOS — Uno Platform Head

> **Status: Sprint 2 — Scaffolded, ready for macOS build testing.**

This project is the macOS head for MediaMatch, using [Uno Platform](https://platform.uno/) 6.x to render WinUI 3 XAML on Mac Catalyst.

## Prerequisites

1. **macOS with .NET 10 SDK installed**
2. **Mac Catalyst workload**: `dotnet workload install maccatalyst`
3. **Xcode** (for Mac Catalyst builds)

## Building

```bash
# From repo root, on macOS:
dotnet build src/MediaMatch.App.macOS/MediaMatch.App.macOS.csproj

# Run:
dotnet run --project src/MediaMatch.App.macOS
```

> **Note:** This project cannot build on Windows (no maccatalyst workload). The solution excludes it from the default Windows build.

## Architecture

This project has its own:
- `App.xaml.cs` — Composition root with DI (mirrors Windows but uses Unix infrastructure)
- `MainWindow.xaml` — NavigationView shell (no Mica backdrop — uses native macOS chrome)
- Pages: HomePage, SettingsPage, HistoryPage

Shared layers (referenced as project dependencies):
- `MediaMatch.Core` — Domain models, interfaces
- `MediaMatch.Application` — Services, pipeline orchestration
- `MediaMatch.Infrastructure` — HTTP clients, providers, caching
- `MediaMatch.Infrastructure.Unix` — POSIX hard links, AES encryption, network detection

## What's Different From Windows

| Feature | Windows | macOS |
|---------|---------|-------|
| Encryption | DPAPI (current user) | AES-256-GCM with key file |
| Hard links | `CreateHardLink` (kernel32) | POSIX `link()` syscall |
| Network detection | `GetDriveType` (kernel32) | `/proc/mounts` parsing |
| File clone | ReFS CoW → hard link → copy | Hard link → copy |
| Backdrop | Mica | Native macOS chrome |
| Title bar | Custom WinUI title bar | Platform native |
| Shell extension | Windows context menu | Not available |

