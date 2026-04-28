<p align="center">
  <img src="assets/MediaMatch_Logo.png" alt="MediaMatch" width="400" />
</p>

<p align="center">
  <strong>A modern, open-source media file organizer — the spiritual successor to FileBot.</strong>
</p>

<p align="center">
  <a href="https://github.com/swigerb/MediaMatch/actions/workflows/ci.yml"><img src="https://github.com/swigerb/MediaMatch/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <a href="https://www.gnu.org/licenses/gpl-3.0"><img src="https://img.shields.io/badge/License-GPLv3-blue.svg" alt="License: GPL v3" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10" /></a>
</p>

---

MediaMatch automatically renames and organizes your TV shows, movies, anime, music, and subtitle files using online metadata from TMDb, TVDb, AniDB, MusicBrainz, and more. Built with .NET 10 and WinUI 3, it offers a polished Fluent 2 desktop GUI, a powerful CLI, and Windows 11 right-click context menu integration.

**Works out of the box** — local file matching, NFO/XML parsing, and expression-based renaming require no API keys. Configure optional provider keys in Settings to unlock TMDb, TVDb, AniDB, and other online sources.

---

## Features

### Dual-Pane File Organization
- **Drag-and-Drop Interface** — Drop files into the left pane, see matched/renamed results on the right
- **6 Operating Modes** — Switch instantly via SelectorBar:
  - **Rename** — Match and rename movies, TV, anime
  - **Episodes** — Episode-focused matching with season/episode detection
  - **Subtitles** — Search and download subtitles from OpenSubtitles
  - **SFV** — Checksum verification (CRC32, MD5, SHA1, SHA256, SHA512) with progress tracking
  - **Filter** — MediaInfo-powered filtering and inspection
  - **List** — Pattern-based export for file lists

### Smart Matching
- **Heuristic Engine** — Edit-distance, Jaccard similarity, and bigram overlap for accurate file identification
- **Opportunistic Matching** — Fallback mode when strict matching fails; presents ranked suggestions (0.60 confidence threshold) for user selection via `MatchSelectionDialog`
- **Multi-Episode Support** — 6 regex patterns for multi-episode detection with Plex/Jellyfin naming output (`S01E01-E02`)
- **Confidence Thresholds** — 0.85 strict / 0.60 opportunistic, fully configurable

### Metadata Providers
- **TMDb** — Movies and TV series with artwork
- **TVDb** — TV series fallback with episode-level metadata
- **AniDB** — Full HTTP API integration with XML parsing, TVDb fallback mapping, dedicated 1-req/2s rate limiter
- **OpenSubtitles** — Subtitle search and download
- **MusicBrainz + AcoustID** — Music fingerprinting and metadata lookup for audio files
- **Local NFO/XML** — Parse `.nfo` and `.xml` sidecar files as the highest-priority metadata source; `MetadataProviderChain` with configurable priority ordering
- **AI/LLM** — Pluggable `ILlmProvider` for complex renaming logic: OpenAI, Azure OpenAI, and Ollama (local) backends

### Expression Editor
- **Visual Expression Builder** — Construct rename patterns with syntax highlighting and token insertion
- **Scriban Templates** — Flexible rename patterns (`{n} - {s00e00} - {t}`)
- **Rich Binding Tokens** — `{jellyfin}` (Jellyfin-compatible naming), `{acf}` (audio channel format: 5.1/7.1), `{dovi}` (Dolby Vision), `{hdr}` (HDR format), `{resolution}` (4K/1080p), `{bitdepth}` (10-bit/8-bit)
- **Live Preview** — See formatted output update in real time as you edit expressions

### MediaInfo Inspector
- **Full ffprobe Analysis** — Detailed per-stream media properties powered by `ffprobe`
- **Stream Tabs** — General, Video, Audio, and Subtitles tabs for organized property display
- **Clipboard Export** — Copy media information to clipboard for easy sharing

### Preset System
- **Save & Load Presets** — Store datasource, language, episode order, match mode, rename action, and expression format
- **Toolbar Dropdown** — Quick preset switching directly from the main toolbar
- **Keyboard Shortcuts** — Assign hotkeys to your most-used presets

### History & Undo
- **History Page** — Full rename history with session tracking and filtering
- **Revert Capability** — Undo individual renames or entire sessions
- **100-Entry Undo Journal** — Parallel processing with progress tracking and cancellation

### Checksum Verification (SFV)
- **Multiple Algorithms** — CRC32, MD5, SHA1, SHA256, SHA512
- **Progress Tracking** — Real-time progress for batch verification operations

### File Handling
- **File Clone Operations** — ReFS Copy-on-Write (instant, zero disk churn), NTFS hardlinks, automatic fallback chain for unsupported filesystems
- **Music Mode** — ID3v2/Vorbis tag parsing, featuring-artist detection, multi-disc folder organization
- **Subtitle Support** — Detect and rename `.srt`, `.sub`, `.idx`, `.ssa`, `.ass` subtitle files alongside video

### Desktop UI (WinUI 3)
- **Dual-Pane Layout** — FileBot-inspired source/destination pane design
- **Fluent 2 Design** — Mica backdrop, `ThemeResource` bindings throughout
- **SelectorBar Mode Switching** — 6 operating modes accessible from the top bar
- **Preset Dropdown** — Quick preset selection in the main toolbar
- **MediaInfo Inspector** — One-click media analysis from the toolbar
- **Splash Screen** — Branded splash screen with MediaMatch logo on startup
- **Version Display** — Current version shown in the status bar
- **Dark Mode & HiDPI** — Windows 11 Immersive Dark Mode, 4K/HiDPI support, configurable font scaling
- **Enhanced Dialogs** — `ConflictDialog` with plain-language explanations; `MatchSelectionDialog` with poster thumbnails
- **Accessibility** — `AutomationProperties` on all controls, full keyboard navigation, `AccessKey` bindings, `ProgressRing` feedback, `InfoBar` notifications
- **Keyboard Shortcuts Dialog** — Press `F1` for a full shortcut reference

### Shell Extension
- **Right-Click Context Menu** — Windows 11 File Explorer integration; run any saved preset directly from the context menu
- **Registry-Based** — Registered under `HKCU\Software\Classes\*\shell\MediaMatch`, dispatches to `MediaMatch.CLI.exe`

### Scripting & Automation
- **Post-Process Pipeline** — Trigger Plex/Jellyfin library refresh, generate ffmpeg thumbnails, or run custom PowerShell/bash scripts after renames
- **CLI `--apply` Flag** — Opt-in execution of post-process actions; no actions run unless explicitly requested

### Performance
- **Parallel File Scanner** — `ParallelFileScanner` with `Channel<T>` for high-throughput ingestion
- **NAS Auto-Optimization** — `NetworkPathDetector` automatically adjusts concurrency and timeouts for network paths
- **Lazy Metadata** — `LazyMetadataResolver` defers provider calls until results are needed
- **Configurable Tuning** — Worker count, rate limits, and cache TTL all configurable

---

## Screenshots

> *Screenshots coming soon — MediaMatch features a modern WinUI 3 Fluent 2 interface with Mica backdrop, dark mode support, and a dual-pane file organization layout inspired by FileBot.*

---

## Distribution

| Platform | Status | Install |
|----------|--------|---------|
| **Windows** | ✅ Available | `winget install swigerb.MediaMatch` or [direct download](https://github.com/swigerb/MediaMatch/releases) |
| **macOS** | 🔜 Planned | Uno Platform port |
| **NAS** | 🔜 Planned | Synology SPK, QNAP QPKG |

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 (1809+) or Windows 11
- Windows App SDK 1.8+
- `ffprobe` on `PATH` (optional — required for MediaInfo Inspector and `{acf}`, `{dovi}`, `{hdr}`, `{resolution}`, `{bitdepth}` tokens)

### Build & Run

```powershell
# Clone the repository
git clone https://github.com/swigerb/MediaMatch.git
cd MediaMatch

# Build GUI (Windows, requires Windows App SDK)
dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64

# Build CLI only (cross-platform)
dotnet build src\MediaMatch.CLI

# Run tests
dotnet test
```

### CLI Usage

```powershell
# Preview renames for a folder
MediaMatch.CLI.exe match --source "D:\TV Shows" --format "{n}\Season {s}\{n} - {s00e00} - {t}"

# Apply renames with post-process actions
MediaMatch.CLI.exe match --source "D:\TV Shows" --format "{n}\Season {s}\{n} - {s00e00} - {t}" --apply

# Use a saved preset
MediaMatch.CLI.exe apply --preset "My TV Preset" --source "D:\TV Shows"

# Music mode
MediaMatch.CLI.exe match --source "D:\Music" --mode music --format "{albumartist}\{album}\{track} - {title}"
```

---

## Configuration

MediaMatch works out of the box — local file matching, NFO/XML parsing, and expression-based renaming require no API keys. To unlock additional online metadata providers, configure API keys in **Settings**:

| Provider | Required | Notes | Get Key |
|----------|----------|-------|---------|
| TMDb | Optional | Movies & TV | [themoviedb.org/settings/api](https://www.themoviedb.org/settings/api) |
| TVDb | Optional | TV series fallback | [thetvdb.com/dashboard/account/apikey](https://thetvdb.com/dashboard/account/apikey) |
| AniDB | Optional | Anime (HTTP API) | [anidb.net/perl-bin/animedb.pl?show=account](https://anidb.net/perl-bin/animedb.pl?show=account) |
| OpenSubtitles | Optional | Subtitle search | [opensubtitles.com](https://www.opensubtitles.com/) |
| MusicBrainz | No key | Music metadata | Free, User-Agent required (auto-set) |
| AcoustID | Optional | Audio fingerprinting | [acoustid.org/login](https://acoustid.org/login) |
| OpenAI | Optional | LLM renaming | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| Azure OpenAI | Optional | LLM renaming | Azure Portal |
| Ollama | Optional | Local LLM (no key) | [ollama.ai](https://ollama.ai) |

Settings are stored at `%LOCALAPPDATA%\MediaMatch\settings.json`. API keys are encrypted with Windows DPAPI.

---

## Shell Extension

After installing MediaMatch, a **MediaMatch** entry appears in the Windows 11 right-click context menu for all files and folders. Selecting it opens a submenu of your saved presets, which are run via `MediaMatch.CLI.exe` with the selected path as the source.

To register or unregister the shell extension manually:

```powershell
# Register (run once after build)
MediaMatch.CLI.exe shell --register

# Unregister
MediaMatch.CLI.exe shell --unregister
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Add folder |
| `Ctrl+A` | Select all files |
| `Delete` | Remove selected files |
| `Ctrl+Z` | Undo last rename |
| `F5` | Refresh / re-scan folder |
| `F1` | Open keyboard shortcuts reference dialog |

---

## Architecture

```
MediaMatch.Core           — Domain models, interfaces, enums, expression contracts
MediaMatch.Application    — Services (rename pipeline, batch ops, undo, matching, post-process,
                            ChecksumService, MediaInfoService)
MediaMatch.Infrastructure — Providers (TMDb, TVDb, AniDB, MusicBrainz, AcoustID, LLM, NFO/XML),
                            HTTP client, caching, persistence, MediaInfoExtractor
MediaMatch.App            — WinUI 3 desktop application (MVVM, 5 mode panel ViewModels + UserControls,
                            HistoryPage, PresetManager, ExpressionEditor, MediaInfoInspector)
MediaMatch.CLI            — Spectre.Console command-line interface with --apply and --preset flags
MediaMatch.ShellExtension — Windows 11 right-click context menu COM/registry integration
```

Clean architecture: dependencies flow inward (`App/CLI → Application → Core`; `Infrastructure → Core`).

---

## Tech Stack

- **.NET 10 LTS** — Latest long-term support runtime
- **WinUI 3 + Windows App SDK 1.8** — Native Windows UI with Fluent 2 design
- **CommunityToolkit.Mvvm 8.4** — Source-generated MVVM with partial properties (AOT-safe)
- **Serilog** — Structured logging to file and console
- **OpenTelemetry** — Distributed tracing and metrics (OTel spans per provider/action)
- **Scriban** — Expression template engine for rename patterns
- **Velopack** — Auto-update and installer framework
- **Spectre.Console** — Rich CLI output with progress bars and tables
- **ffprobe** — AV stream analysis for media-quality binding tokens

---

## How It Differs from FileBot

| | MediaMatch | FileBot |
|--|-----------|---------|
| **License** | GPL v3 — free and open source | Commercial ($6/year) |
| **Platform** | .NET 10, WinUI 3, Windows | Java, Swing, cross-platform |
| **UI** | Fluent 2, Mica, dark mode, dual-pane layout | Legacy Swing UI |
| **Matching** | Configurable multi-metric + opportunistic fallback | Fixed algorithms |
| **Providers** | TMDb, TVDb, AniDB, MusicBrainz, AcoustID, NFO/XML, LLM | TMDb, TVDb, AniDB, AniList, others |
| **Music** | MusicBrainz + AcoustID fingerprinting, ID3v2/Vorbis tags | Limited |
| **Tokens** | `{acf}`, `{dovi}`, `{hdr}`, `{resolution}`, `{bitdepth}`, `{jellyfin}` | Basic AV tokens |
| **Expression Editor** | Visual builder with syntax highlighting and live preview | Format dialog |
| **File Ops** | ReFS CoW, NTFS hardlinks, copy fallback | Copy/move only |
| **Checksums** | CRC32, MD5, SHA1, SHA256, SHA512 | SFV only |
| **MediaInfo** | Full ffprobe analysis with per-stream tabs | MediaInfo bindings |
| **History** | Built-in history page with session tracking and revert | History dialog |
| **Presets** | Full preset system with toolbar dropdown and hotkeys | Preset editor |
| **Undo** | Built-in 100-entry journal | Manual rollback |
| **Shell Extension** | Windows 11 right-click integration | None |
| **Post-Process** | Plex/Jellyfin refresh, ffmpeg thumbnails, custom scripts | Groovy scripts |
| **AI/LLM** | OpenAI, Azure OpenAI, Ollama (local) | None |
| **Performance** | Parallel scanner, NAS auto-optimization, lazy metadata | Single-threaded |
| **CLI** | Spectre.Console with `--apply` and `--preset` flags | Basic CLI |
| **Updates** | Velopack auto-update | Manual download |
| **Distribution** | winget, direct download | Manual download |

MediaMatch is inspired by [FileBot](https://www.filebot.net/) and the open-source [FB-Mod fork](https://github.com/barry-allen07/FB-Mod) (FileBot v4.7.9). It is a clean-room rewrite — no FileBot code is used.

---

## Contributing

Contributions are welcome! Please open an issue first to discuss what you'd like to change.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes and add tests
4. Run `dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64` and `dotnet test`
5. Submit a pull request

---

## License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.
