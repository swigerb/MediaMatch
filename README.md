# MediaMatch

> A modern, open-source media file organizer — the spiritual successor to FileBot.

MediaMatch automatically renames and organizes your TV shows, movies, anime, music, and subtitle files using online metadata from TMDb, TVDb, AniDB, MusicBrainz, and more. Built with .NET 10 and WinUI 3, it offers a polished Fluent 2 desktop GUI, a powerful CLI, and Windows 11 right-click context menu integration.

---

## Features

### Smart Matching
- **Heuristic Engine** — Edit-distance, Jaccard similarity, and bigram overlap for accurate file identification
- **Opportunistic Matching** — Fallback mode when strict matching fails; presents ranked `MatchSuggestion[]` list (0.60 confidence threshold) for user selection via `MatchSelectionDialog`
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

### Naming & Expression Engine
- **Scriban Templates** — Flexible rename patterns (`{n} - {s00e00} - {t}`)
- **Rich Binding Tokens** — `{jellyfin}` (Jellyfin-compatible naming), `{acf}` (audio channel format: 5.1/7.1), `{dovi}` (Dolby Vision), `{hdr}` (HDR format), `{resolution}` (4K/1080p), `{bitdepth}` (10-bit/8-bit)
- **MediaInfo Extraction** — `MediaInfoExtractor` using `ffprobe` to populate AV-quality tokens

### File Handling
- **Batch Rename with Undo** — Parallel processing with progress tracking, cancellation, and 100-entry undo journal
- **File Clone Operations** — ReFS Copy-on-Write (instant, zero disk churn), NTFS hardlinks, automatic fallback chain for unsupported filesystems
- **Music Mode** — ID3v2/Vorbis tag parsing, featuring-artist detection, multi-disc folder organization
- **Subtitle Support** — Detect and rename `.srt`, `.sub`, `.idx`, `.ssa`, `.ass` subtitle files alongside video

### Desktop UI (WinUI 3)
- **Fluent 2 Design** — Mica backdrop, `ThemeResource` bindings throughout
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

> *Screenshots will be added after the v0.2.0 release.*

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 (1809+) or Windows 11
- Windows App SDK 1.8+
- `ffprobe` on `PATH` (optional — required for `{acf}`, `{dovi}`, `{hdr}`, `{resolution}`, `{bitdepth}` tokens)

### Build & Run

```powershell
# Clone the repository
git clone https://github.com/swigerb/MediaMatch.git
cd MediaMatch

# Build (must specify platform)
dotnet build src\MediaMatch.App\MediaMatch.App.csproj -p:Platform=x64

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

On first launch, MediaMatch opens the Settings page to configure API keys:

| Provider | Key Required | Notes | Get Key |
|----------|-------------|-------|---------|
| TMDb | Yes | Movies & TV | [themoviedb.org/settings/api](https://www.themoviedb.org/settings/api) |
| TVDb | Yes | TV series fallback | [thetvdb.com/dashboard/account/apikey](https://thetvdb.com/dashboard/account/apikey) |
| AniDB | Yes | Anime (HTTP API) | [anidb.net/perl-bin/animedb.pl?show=account](https://anidb.net/perl-bin/animedb.pl?show=account) |
| OpenSubtitles | Optional | Subtitle search | [opensubtitles.com](https://www.opensubtitles.com/) |
| MusicBrainz | No key | Music metadata | Free, User-Agent required (auto-set) |
| AcoustID | Yes | Audio fingerprinting | [acoustid.org/login](https://acoustid.org/login) |
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
MediaMatch.Application    — Services (rename pipeline, batch ops, undo, matching, post-process)
MediaMatch.Infrastructure — Providers (TMDb, TVDb, AniDB, MusicBrainz, AcoustID, LLM, NFO/XML),
                            HTTP client, caching, persistence, MediaInfoExtractor
MediaMatch.App            — WinUI 3 desktop application (MVVM, pages, navigation, shell ext UI)
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
| **UI** | Fluent 2, Mica, dark mode, HiDPI | Legacy Swing UI |
| **Matching** | Configurable multi-metric + opportunistic fallback | Fixed algorithms |
| **Providers** | TMDb, TVDb, AniDB, MusicBrainz, AcoustID, NFO/XML, LLM | TMDb, TVDb, AniDB, AniList, others |
| **Music** | MusicBrainz + AcoustID fingerprinting, ID3v2/Vorbis tags | Limited |
| **Tokens** | `{acf}`, `{dovi}`, `{hdr}`, `{resolution}`, `{bitdepth}`, `{jellyfin}` | Basic AV tokens |
| **File Ops** | ReFS CoW, NTFS hardlinks, copy fallback | Copy/move only |
| **Undo** | Built-in 100-entry journal | Manual rollback |
| **Shell Extension** | Windows 11 right-click integration | None |
| **Post-Process** | Plex/Jellyfin refresh, ffmpeg thumbnails, custom scripts | Groovy scripts |
| **AI/LLM** | OpenAI, Azure OpenAI, Ollama (local) | None |
| **Performance** | Parallel scanner, NAS auto-optimization, lazy metadata | Single-threaded |
| **CLI** | Spectre.Console with `--apply`, `--preset`, `--apply` flags | Basic CLI |
| **Updates** | Velopack auto-update | Manual download |

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
