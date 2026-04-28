# MediaMatch.App.macOS — Scaffold

> **Status: Scaffold only — not buildable yet.**

This project is a placeholder for future macOS support via [Uno Platform](https://platform.uno/) and .NET Mac Catalyst.

## What's here

| File | Purpose |
|------|---------|
| `MediaMatch.App.macOS.csproj` | Minimal project targeting `net10.0-maccatalyst` |
| `README.md` | This file |

## What's needed before this builds

1. **Install the Mac Catalyst workload**: `dotnet workload install maccatalyst`
2. **Add Uno Platform packages**: `Uno.WinUI`, `Uno.Extensions.Hosting.WinUI`, etc.
3. **Create platform entry point**: `AppDelegate.cs` or Uno Platform `App.xaml.cs` adapter
4. **Implement macOS-specific services**:
   - `ISettingsEncryption` → macOS Keychain via `Security` framework
   - `INetworkPathDetector` → macOS mount point detection
   - Hard links → `link()` syscall (supported natively on .NET)
5. **Code signing and notarization** for distribution

## Architecture

This project will share all Core, Application, and Infrastructure code with the Windows app.
Only the UI layer and platform-specific service implementations will differ.

See [docs/cross-platform.md](../../docs/cross-platform.md) for the full cross-platform strategy.
