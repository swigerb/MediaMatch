# Cross-Platform Strategy

This document describes the current Windows-only dependencies in MediaMatch and the
plan for supporting macOS and Linux via Uno Platform.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    UI Layer (Head Projects)              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  App (WinUI)  │  │ App.macOS    │  │     CLI      │  │
│  │  Windows only │  │ Mac Catalyst │  │ All platforms │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
│         │                 │                  │          │
├─────────┼─────────────────┼──────────────────┼──────────┤
│         └─────────────────┼──────────────────┘          │
│                           ▼                             │
│              ┌────────────────────────┐                  │
│              │      Application       │                  │
│              │   (Pipeline, Services) │                  │
│              └────────────┬───────────┘                  │
│                           ▼                             │
│              ┌────────────────────────┐                  │
│              │         Core           │                  │
│              │ (Models, Interfaces)   │                  │
│              └────────────┬───────────┘                  │
│                           ▲                             │
│              ┌────────────┴───────────┐                  │
│              │    Infrastructure      │                  │
│              │ (Providers, Platform)  │                  │
│              └────────────────────────┘                  │
└─────────────────────────────────────────────────────────┘
```

**Shared across all platforms:** Core, Application, Infrastructure (providers, HTTP, caching).
**Platform-specific:** UI head projects and a small set of platform services.

## Current Windows-Only Dependencies

### 1. Settings Encryption — `SettingsEncryption`

| Aspect | Details |
|--------|---------|
| **File** | `src/MediaMatch.Infrastructure/Persistence/SettingsEncryption.cs` |
| **API** | `System.Security.Cryptography.ProtectedData` (Windows DPAPI) |
| **Interface** | `ISettingsEncryption` in Core — already abstracted |
| **macOS alternative** | macOS Keychain via `Security` framework P/Invoke or `SecKeychain` APIs |
| **Linux alternative** | libsecret / GNOME Keyring via D-Bus, or `age`/`gpg` file encryption |

The interface `ISettingsEncryption` is already defined in Core. Each platform needs
its own implementation registered in the composition root.

### 2. Hard Links — `HardLinkHandler`

| Aspect | Details |
|--------|---------|
| **File** | `src/MediaMatch.Infrastructure/FileSystem/HardLinkHandler.cs` |
| **API** | Win32 `CreateHardLink` via `LibraryImport` P/Invoke |
| **macOS/Linux alternative** | `File.CreateSymbolicLink()` (.NET 7+) or `link()` syscall. .NET doesn't expose `link()` directly, but `Mono.Posix.NETStandard` or P/Invoke to libc works. |

Hard links work on APFS (macOS) and ext4/Btrfs/XFS (Linux). The `IPlatformService.SupportsHardLinks`
flag can be used to gate the UI option.

### 3. ReFS Clone — `ReFsCloneHandler`

| Aspect | Details |
|--------|---------|
| **File** | `src/MediaMatch.Infrastructure/FileSystem/ReFsCloneHandler.cs` |
| **API** | `FSCTL_DUPLICATE_EXTENTS_TO_FILE` via `DeviceIoControl` P/Invoke |
| **macOS alternative** | APFS `clonefile()` syscall — zero-copy CoW, same semantics |
| **Linux alternative** | Btrfs `FICLONE` ioctl or `cp --reflink=auto` |

`IPlatformService.SupportsReFsClone` should be extended or complemented with
a `SupportsCoWClone` flag for cross-platform CoW detection.

### 4. Network Path Detection — `NetworkPathDetector`

| Aspect | Details |
|--------|---------|
| **File** | `src/MediaMatch.Infrastructure/FileSystem/NetworkPathDetector.cs` |
| **API** | Win32 `GetDriveType` P/Invoke |
| **macOS alternative** | Parse `/sbin/mount` output or check `statfs.f_fstypename` for `smbfs`/`nfs` |
| **Linux alternative** | Parse `/proc/mounts` or check `statfs.f_type` for NFS/CIFS magic numbers |

### 5. Shell Extension — `MediaMatch.ShellExtension`

Windows Explorer context menu integration. No direct equivalent on other platforms;
macOS has Finder Extensions (requires a separate app extension target) and Linux
has Nautilus/Dolphin scripts.

## Platform Service

`IPlatformService` (Core) and `PlatformService` (Infrastructure) provide runtime
platform detection. Use this to conditionally enable features:

```csharp
if (platformService.SupportsReFsClone)
    // offer ReFS clone option in UI
```

## Steps to Add Uno Platform Head Project

1. **Install prerequisites**
   ```bash
   dotnet workload install maccatalyst
   dotnet new install Uno.Templates
   ```

2. **Create Uno Platform head** (or use the existing scaffold in `src/MediaMatch.App.macOS/`)
   ```bash
   dotnet new unoapp -o src/MediaMatch.App.Uno --preset blank
   ```

3. **Wire shared projects** — reference Core, Application, Infrastructure from the Uno head.

4. **Implement platform services**:
   - `MacOsSettingsEncryption : ISettingsEncryption` — Keychain integration
   - `PosixHardLinkHandler` — `link()` syscall wrapper
   - `MacOsNetworkPathDetector : INetworkPathDetector` — mount point inspection

5. **Register platform services in composition root** using `IPlatformService` to select
   the correct implementations at startup.

6. **Adapt XAML** — WinUI XAML works with Uno Platform with minor adjustments for
   platform-specific APIs (file pickers, window management, etc.).

## macOS-Specific Considerations

### Code Signing and Notarization
- **Required** for distribution outside the Mac App Store
- Use `codesign` with a Developer ID certificate
- Submit to Apple's notarization service via `notarytool`
- Hardened runtime must be enabled

### Packaging
- **DMG**: Standard distribution format. Use `create-dmg` or `hdiutil`
- **Mac App Store**: Requires sandboxing and App Store review
- **Homebrew Cask**: Community distribution option

### macOS-Specific APIs
- File pickers: `NSOpenPanel` / `NSSavePanel` (exposed by Mac Catalyst)
- Drag and drop: AppKit `NSDraggingDestination`
- Menu bar: Native macOS menu integration via Uno Platform

## Linux Considerations

### Packaging Formats
- **AppImage**: Single-file, no installation needed. Widest compatibility.
- **Flatpak**: Sandboxed, available on Flathub. Recommended for desktop Linux.
- **Snap**: Ubuntu-centric, sandboxed.
- **Native packages**: `.deb` (Debian/Ubuntu), `.rpm` (Fedora/RHEL) via `dotnet-packaging`.

### Linux-Specific APIs
- File pickers: GTK portal via `xdg-desktop-portal`
- System tray: `libappindicator` or D-Bus `StatusNotifierItem`
- Settings encryption: libsecret / GNOME Keyring / KWallet

### Desktop Environment Differences
- GNOME, KDE, and other DEs have different file manager integration
- Test on both Wayland and X11 display servers

## Migration Checklist

- [x] Define `IPlatformService` interface in Core
- [x] Implement `PlatformService` in Infrastructure
- [x] Create `ISettingsEncryption` interface in Core
- [x] Document Windows-only dependencies
- [x] Create macOS scaffold project
- [ ] Install Uno Platform templates and workloads
- [ ] Create Uno Platform head project from scaffold
- [ ] Implement macOS Keychain `ISettingsEncryption`
- [ ] Implement POSIX hard link handler
- [ ] Implement macOS/Linux network path detector
- [ ] Set up macOS CI pipeline (GitHub Actions macOS runner)
- [ ] Code signing and notarization automation
- [ ] Linux packaging pipeline (AppImage + Flatpak)
